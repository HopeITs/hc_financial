-- Apply: Select_All_Budget_Lines_Details
--
-- WHAT IT CHANGES
--   The procedure builds its columns dynamically, three per donor, and each one
--   carried a correlated subquery for that donor's rate:
--
--       Expected_Total * (SELECT rate FROM budget_donor
--                         WHERE budget_id = all_budgetlines.budget_ID
--                           AND donor_id  = <n>) / 100
--
--   With 13 donors that is 39 subqueries per row. budget_donor is small and
--   already indexed, so each one is fast - there are simply far too many of them.
--
--   budget_donor is now joined once and the rate picked per donor with a
--   conditional aggregate, which the surrounding GROUP BY already supports:
--
--       Expected_Total * MAX(CASE WHEN bd.donor_id = <n> THEN bd.rate END) / 100
--
--   Nothing else moves: same columns, same names, same order, same parameters.
--
-- VERIFIED
--   The old and new procedure were both installed and run side by side, for
--   every budget type, on both databases. Results compared value by value:
--
--       offline (MariaDB 10.4)   2,265 rows x 56 columns   identical
--       online  (MySQL 8.4)        509 rows                identical
--
--   Also checked, because the rewrite would behave differently if they were not
--   true - on the online data:
--
--       duplicate (budget_id, donor_id) rows        0  (and a unique key forbids them)
--       groups where Price varies                   0
--       groups where Expected_Total varies          0
--       budgets with no donor                       1  (covered by the comparison)
--
-- MEASURED
--       offline   15,501 ms -> 2,291 ms   (7x)
--       online      1,533 ms -> 1,318 ms
--
--   The online test database holds far fewer budget lines, so the gain is small
--   there. It grows with the number of donors and rows.
--
-- NOT IN THIS FILE
--   cash_balances. The offline database had a bad version of that view and it has
--   been fixed there, but the online one was already written the right way. There
--   is nothing to fix online.
--
-- HOW TO RUN
--   Open in MySQL Workbench, connect, execute the whole file. Safe to re-run.
--   The DELIMITER lines are needed; keep them.
--
-- TO UNDO
--   Run rollback-budget-lines.sql in this directory.

DROP PROCEDURE IF EXISTS `Select_All_Budget_Lines_Details`;

DELIMITER $$

CREATE PROCEDURE `Select_All_Budget_Lines_Details`(IN `Type_ID` INT)
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
