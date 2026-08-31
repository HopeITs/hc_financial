-- Select_All_Budget_Lines_Details - NEW - ONLINE (Percona MySQL 8.4)
--
-- WHAT IT FIXES
--   The procedure builds its columns dynamically, three per donor, and each one
--   carried a correlated subquery for that donor's rate:
--
--       Expected_Total * (SELECT rate FROM budget_donor
--                         WHERE budget_id = all_budgetlines.budget_ID
--                           AND donor_id  = <n>) / 100
--
--   13 donors x 3 columns x 28,426 rows is roughly 1.1 million subqueries.
--   budget_donor holds 116 rows and is already indexed on (budget_id, donor_id),
--   so each lookup was fast - there were simply far too many of them.
--
--   It now joins budget_donor once and picks the rate with a conditional
--   aggregate, which the surrounding GROUP BY already supports:
--
--       Expected_Total * MAX(CASE WHEN bd.donor_id = <n> THEN bd.rate END) / 100
--
--   Two changes in total: that expression in three places, and one LEFT JOIN.
--   Same columns, same names, same order, same parameter, same GROUP BY.
--
--       offline   15,501 ms -> 2,291 ms   (7x)
--       online     1,533 ms -> 1,318 ms
--
--   The online copy holds 509 budget lines against 2,265 offline, which is why
--   the gain measures small there. It grows with rows and with donor count.
--
-- VERIFIED
--   Old and new were installed side by side and run for every budget type on both
--   servers, then compared value by value. Offline: 2,265 rows x 56 columns,
--   identical. Online: identical. Confirmed independently by the user offline.
--
-- DEFINER
--   Kept deliberately. These objects are SQL SECURITY DEFINER, so they run with
--   the privileges of the user named in the clause, not the caller's. Setting a
--   DEFINER other than yourself needs SUPER (or SET_USER_ID), so run this as an
--   account that has it. If you cannot, remove the DEFINER clause and the object
--   is created as whoever runs the file - check that user can read every table it
--   touches before relying on it.
--
DROP PROCEDURE IF EXISTS `Select_All_Budget_Lines_Details`;

DELIMITER $$

CREATE DEFINER=`hcfi`@`%` PROCEDURE `Select_All_Budget_Lines_Details`(IN `Type_ID` INT)
BEGIN

SET SESSION group_concat_max_len = 1000000;

set @sql = null;

SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;



SELECT GROUP_CONCAT(DISTINCT CONCAT(' all_budgetlines.Expected_Total*MAX(CASE WHEN bd.donor_id = ',donor.id,' THEN bd.rate END)/100 AS `',donor.Name,' Expected Total`')) INTO @sql1

FROM

donor JOIN budget_donor ON donor.ID = budget_donor.donor_id

JOIN budget ON budget_donor.budget_id = budget.ID

WHERE budget.Budget_Type_ID = Type_ID;





SELECT GROUP_CONCAT(DISTINCT CONCAT(' all_budgetlines.Expenditures_With_Out_Of_Date	*MAX(CASE WHEN bd.donor_id = ',donor.id,' THEN bd.rate END)/100 AS `',donor.Name,' Expenditures Total`')) INTO @sql2

FROM

donor JOIN budget_donor ON donor.ID = budget_donor.donor_id

JOIN budget ON budget_donor.budget_id = budget.ID

WHERE budget.Budget_Type_ID = Type_ID;





SELECT GROUP_CONCAT(DISTINCT CONCAT(' (all_budgetlines.Expected_Total - all_budgetlines.Expenditures_With_Out_Of_Date)*MAX(CASE WHEN bd.donor_id = ',donor.id,' THEN bd.rate END)/100 AS `',donor.Name,' Remaining`')) INTO @sql3

FROM

donor JOIN budget_donor ON donor.ID = budget_donor.donor_id

JOIN budget ON budget_donor.budget_id = budget.ID

WHERE budget.Budget_Type_ID = Type_ID;





SET @sql = CONCAT(' SELECT 

                  all_budgetlines.City_Name AS `City`,

                  all_budgetlines.project_name as `Program`,

                  all_budgetlines.Subproject_Name AS `Project`,

                  all_budgetlines.Account_Name AS `Account`,

                  all_budgetlines.Budget_Name AS `Budget`,

                  all_budgetlines.budget_line_ID, all_budgetlines.budget_ID, all_budgetlines.line_ID,

                  all_budgetlines.line_Name AS `Line`,

                  all_budgetlines.linecategory_Name AS `Category`,

                  all_budgetlines.linecategory_Type AS `Type`,

                  all_budgetlines.Unit AS `Unit`,

                  all_budgetlines.Timing AS `Timing`,

                  all_budgetlines.Price AS `Price`,

        

                  ',@sql1,',

                  all_budgetlines.Expected_Total AS `Expected Total`,

                  ',@sql2,',

                  all_budgetlines.Expenditures_With_Out_Of_Date	 AS `Expenditures Total`,

                  ',@sql3,',

                  all_budgetlines.Expected_Total - all_budgetlines.Expenditures_With_Out_Of_Date AS `Remaining`

                  FROM all_budgetlines

                  LEFT JOIN budget_donor bd ON bd.budget_id = all_budgetlines.budget_ID 

                  WHERE all_budgetlines.BudgetType_ID = ',Type_ID,'

                  Group By all_budgetlines.budget_id, all_budgetlines.line_ID 

                  ORDER by all_budgetlines.city_id, all_budgetlines.project_id, all_budgetlines.subproject_id, all_budgetlines.account_id, all_budgetlines.budget_id;');



IF @sql IS NOT null THEN 

    BEGIN

        PREPARE stmt FROM @sql;

        EXECUTE stmt;

        DEALLOCATE PREPARE stmt;

    END;

END IF;



set @sql = null;

SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;

END$$

DELIMITER ;
