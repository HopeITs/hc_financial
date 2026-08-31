-- Select_All_Budget_Lines_Details - NEW, in use on the offline database
--
-- 2,291 ms, seven times faster.
--
-- budget_donor is joined once and the rate picked with a conditional aggregate
-- instead of a subquery per row:
--
--     Expected_Total * MAX(CASE WHEN bd.donor_id = <n> THEN bd.rate END) / 100
--
-- Two changes only: that expression in three places, and one LEFT JOIN. Same
-- columns, same names, same order, same parameter, same GROUP BY.
--
-- Verified before use: 2,265 rows x 56 columns identical, for every budget type,
-- on both servers.
--
-- The ONLINE server still has the old one. This file applies there as it is.
--
-- DEFINER is `hcfi`@`%`, the same account the online server uses.
-- Setting a DEFINER other than yourself needs SUPER. If you do not have it,
-- delete the DEFINER= clause and the object is created as whoever runs the file.
--
-- The procedure body is exactly as the server returned it.

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
