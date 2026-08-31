-- Compare the old definitions against the new ones, yourself
--
-- This installs the ORIGINALS beside the current ones under _old names. Nothing
-- in use is replaced or removed: cash_balances and Select_All_Budget_Lines_Details
-- keep working exactly as they are. The _old copies are extra objects you can
-- query, and section 3 deletes them again.
--
-- Run section 1, then the queries in section 2, then section 3 when you are done.

-- ============================================================
-- 1. install the originals under _old names
-- ============================================================

CREATE OR REPLACE ALGORITHM=UNDEFINED SQL SECURITY DEFINER VIEW `cash_balances_old` AS select `ca`.`id` AS `CashAccount_id`,`ca`.`Name` AS `CashAccount_Name`,ifnull(sum(case when `acttype`.`shortname` = 'I' then `cad`.`amount` else 0 end),0) AS `CashAccount_I`,ifnull(sum(case when `acttype`.`shortname` = 'E' then `cad`.`amount` else 0 end),0) AS `CashAccount_E`,ifnull(sum(case when `acttype`.`shortname` = 'I' then `cad`.`amount` when `acttype`.`shortname` = 'E' then -`cad`.`amount` end),0) AS `CashAccount_Net`,`hcsyria_financial_test`.`city`.`ID` AS `City_ID`,`hcsyria_financial_test`.`city`.`Name` AS `City_Name`,`sp`.`ID` AS `Subproject_ID`,`sp`.`Name` AS `Subproject_Name`,`cat`.`Name` AS `Category_Name`,`acc`.`ID` AS `Account_ID`,`acc`.`Name` AS `Account_Name`,`atype`.`name` AS `Account_Type`,`tt`.`Name` AS `tilltype_Name`,`acc`.`IsClosed` AS `account_IsClosed`,ifnull(`agg`.`SYR_I`,0) AS `Account_I`,ifnull(`agg`.`SYR_E`,0) AS `Account_E`,ifnull(`agg`.`SYR_I` - `agg`.`SYR_E`,0) AS `Account_Net`,ifnull(`agg`.`USD_I`,0) AS `Account_I_USD`,ifnull(`agg`.`USD_E`,0) AS `Account_E_USD`,ifnull(`agg`.`USD_I` - `agg`.`USD_E`,0) AS `Account_Net_USD` from (((((((((((((`hcsyria_financial_test`.`cashaccount` `ca` left join `hcsyria_financial_test`.`cashaction` `caa` on(`caa`.`cashaccount_id` = `ca`.`id`)) left join `hcsyria_financial_test`.`cashaction_denominations` `cad` on(`cad`.`cashaction_id` = `caa`.`id`)) left join `hcsyria_financial_test`.`actiontype` `acttype` on(`acttype`.`id` = `cad`.`AType_id`)) left join `hcsyria_financial_test`.`cashaccount_subproject` `cas` on(`cas`.`cashaccount_id` = `ca`.`id`)) left join `hcsyria_financial_test`.`subproject` `sp` on(`sp`.`ID` = `cas`.`subproject_id`)) left join `hcsyria_financial_test`.`account` `acc` on(`acc`.`SubProject_ID` = `sp`.`ID`)) left join `hcsyria_financial_test`.`accounttype` `atype` on(`atype`.`id` = `acc`.`Accounttype_id`)) left join `hcsyria_financial_test`.`city` on(`hcsyria_financial_test`.`city`.`ID` = `sp`.`City_ID`)) left join `hcsyria_financial_test`.`project` `p` on(`p`.`ID` = `sp`.`Project_ID`)) left join `hcsyria_financial_test`.`category` `cat` on(`cat`.`ID` = `p`.`Category_ID`)) left join `hcsyria_financial_test`.`till` on(`hcsyria_financial_test`.`till`.`Account_ID` = `acc`.`ID`)) left join `hcsyria_financial_test`.`tilltypes` `tt` on(`tt`.`ID` = `hcsyria_financial_test`.`till`.`Type_ID`)) left join (select `a`.`cashaccount_id` AS `cashaccount_id`,`a`.`Till_ID` AS `Till_ID`,sum(case when `at`.`shortname` = 'I' and `tt`.`Name` = 'SYR' then `a`.`Amount` else 0 end) AS `SYR_I`,sum(case when `at`.`shortname` in ('E','Ex') and `tt`.`Name` = 'SYR' then `a`.`Amount` else 0 end) AS `SYR_E`,sum(case when `at`.`shortname` = 'I' and `tt`.`Name` = 'USD' then `a`.`Amount` else 0 end) AS `USD_I`,sum(case when `at`.`shortname` in ('E','Ex') and `tt`.`Name` = 'USD' then `a`.`Amount` else 0 end) AS `USD_E` from ((`hcsyria_financial_test`.`action` `a` left join `hcsyria_financial_test`.`actiontype` `at` on(`at`.`id` = `a`.`AType_ID`)) left join `hcsyria_financial_test`.`tilltypes` `tt` on(`tt`.`ID` = `a`.`currency_tilltype_id`)) group by `a`.`cashaccount_id`,`a`.`Till_ID`) `agg` on(`agg`.`cashaccount_id` = `ca`.`id` and `agg`.`Till_ID` = `hcsyria_financial_test`.`till`.`ID`)) where lcase(`tt`.`Name`) in ('syr','pay') group by `ca`.`id`,`acc`.`ID` order by `ca`.`id`;

DROP PROCEDURE IF EXISTS `Select_All_Budget_Lines_Details_old`;

DELIMITER $$

CREATE PROCEDURE `Select_All_Budget_Lines_Details_old`(IN `Type_ID` INT)
BEGIN
SET SESSION group_concat_max_len = 1000000;
set @sql = null;
SET @sql1 = null;
SET @sql2 = null;
SET @sql3 = null;

SELECT GROUP_CONCAT(DISTINCT CONCAT(' all_budgetlines.Expected_Total*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Expected Total`')) INTO @sql1
FROM
donor JOIN budget_donor ON donor.ID = budget_donor.donor_id
JOIN budget ON budget_donor.budget_id = budget.ID
WHERE budget.Budget_Type_ID = Type_ID;


SELECT GROUP_CONCAT(DISTINCT CONCAT(' all_budgetlines.Expenditures_With_Out_Of_Date	*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Expenditures Total`')) INTO @sql2
FROM
donor JOIN budget_donor ON donor.ID = budget_donor.donor_id
JOIN budget ON budget_donor.budget_id = budget.ID
WHERE budget.Budget_Type_ID = Type_ID;


SELECT GROUP_CONCAT(DISTINCT CONCAT(' (all_budgetlines.Expected_Total - all_budgetlines.Expenditures_With_Out_Of_Date)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Remaining`')) INTO @sql3
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


-- ============================================================
-- 2. the comparisons - run these one at a time
-- ============================================================

-- 2a. THE ROW COUNTS. Both numbers must be the same.
SELECT (SELECT COUNT(*) FROM cash_balances)     AS `now`,
       (SELECT COUNT(*) FROM cash_balances_old) AS `before`;

-- 2b. ANY ROW THAT DIFFERS IN ANY COLUMN.
--     An empty result is the answer you want. Anything listed is a real
--     difference: `side` says which version that row came from.
SELECT 'now' AS side, t.* FROM (
    SELECT * FROM cash_balances
    UNION ALL SELECT * FROM cash_balances_old
) t GROUP BY CashAccount_id, Account_ID, CashAccount_I, CashAccount_E, CashAccount_Net,
             Account_I, Account_E, Account_Net, Account_I_USD, Account_E_USD, Account_Net_USD
HAVING COUNT(*) <> 2;

-- 2c. THE NUMBER THAT MATTERS MOST - what CashTill_Window shows at the top.
--     Change the 1 to whichever cash account you know the balance of.
SELECT 'now' AS side, CashAccount_Net, SUM(Account_Net) AS AllAccounts_Net,
       SUM(Account_Net) - CashAccount_Net AS Difference
FROM cash_balances     WHERE CashAccount_id = 1 GROUP BY CashAccount_id
UNION ALL
SELECT 'before', CashAccount_Net, SUM(Account_Net),
       SUM(Account_Net) - CashAccount_Net
FROM cash_balances_old WHERE CashAccount_id = 1 GROUP BY CashAccount_id;

-- 2d. EVERY CASH ACCOUNT AT ONCE, the same three numbers.
--     Read down the pairs: each account should appear twice with equal numbers.
SELECT CashAccount_id, 'now' AS side, CashAccount_Net,
       SUM(Account_Net) AS AllAccounts_Net, SUM(Account_Net) - CashAccount_Net AS Difference
FROM cash_balances GROUP BY CashAccount_id
UNION ALL
SELECT CashAccount_id, 'before', CashAccount_Net,
       SUM(Account_Net), SUM(Account_Net) - CashAccount_Net
FROM cash_balances_old GROUP BY CashAccount_id
ORDER BY CashAccount_id, side DESC;

-- 2e. THE BUDGET LINES REPORT. Same count, and the timing side by side.
--     Run each on its own and watch the duration Workbench reports.
CALL Select_All_Budget_Lines_Details(2);
CALL Select_All_Budget_Lines_Details_old(2);


-- ============================================================
-- 3. remove the _old copies when you are finished
-- ============================================================

-- DROP VIEW IF EXISTS `cash_balances_old`;
-- DROP PROCEDURE IF EXISTS `Select_All_Budget_Lines_Details_old`;
