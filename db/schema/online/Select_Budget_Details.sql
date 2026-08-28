CREATE DEFINER=`hcfi`@`%` PROCEDURE `Select_Budget_Details`(IN `Account_ID` INT)
BEGIN
SET SESSION group_concat_max_len = 1000000;
SET @sql = null;
SET @sql1 = null;
SET @sql2 = null;
SET @sql3 = null;

SELECT GROUP_CONCAT(DISTINCT CONCAT(' ifnull(SUM(E_Total.Expected_Total),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = budget.ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Expected Total`')) INTO @sql1
FROM
donor JOIN budget_donor ON donor.ID = budget_donor.donor_id
JOIN budget ON budget_donor.budget_id = budget.ID
LEFT JOIN account ON budget.Account_ID = account.ID
WHERE account.ID = Account_ID;


SELECT GROUP_CONCAT(DISTINCT CONCAT(' ifnull(SUM(E_Total.Expenditures_With_Out_Of_Date),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = budget.ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Expenditures Total`')) INTO @sql2
FROM
donor JOIN budget_donor ON donor.ID = budget_donor.donor_id
JOIN budget ON budget_donor.budget_id = budget.ID
LEFT JOIN account ON budget.Account_ID = account.ID
WHERE account.ID = Account_ID;


SELECT GROUP_CONCAT(DISTINCT CONCAT(' (ifnull(SUM(E_Total.Expected_Total) - SUM(E_Total.Expenditures_With_Out_Of_Date),0))*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = budget.ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Remaining`')) INTO @sql3
FROM
donor JOIN budget_donor ON donor.ID = budget_donor.donor_id
JOIN budget ON budget_donor.budget_id = budget.ID
LEFT JOIN account ON budget.Account_ID = account.ID
WHERE account.ID = Account_ID;


SET @sql = CONCAT('SELECT budget.ID AS `ID`, budget.Name as `Name`,budget.BeginDate, budget.EndDate, budget.Budget_Type_ID , budget.Account_ID, tilltypes.Name AS `Budget Type`, budget.Budget_Type_ID,',@sql1,'
                            ,ifnull(SUM(E_Total.Expected_Total),0) AS `Expected Total`,',@sql2,'
                            ,ifnull(SUM(E_Total.Expenditures_With_Out_Of_Date),0) AS `Expenditures Total`,',@sql3,'
                            ,ifnull(SUM(E_Total.Expected_Total) - SUM(E_Total.Expenditures_With_Out_Of_Date),0) AS `Remaining`


                            FROM budget LEFT JOIN tilltypes ON budget.Budget_Type_ID = tilltypes.ID
                            LEFT JOIN
                            (SELECT all_budgetlines.Account_ID, all_budgetlines.budget_ID, all_budgetlines.Expected_Total,
                            SUM(all_budgetlines.Expenditures) AS `Expenditures`, all_budgetlines.Expenditures_With_Out_Of_Date
                            FROM all_budgetlines GROUP BY all_budgetlines.budget_ID, all_budgetlines.budget_line_ID) E_Total ON budget.ID = E_Total.budget_ID

                            WHERE budget.Account_ID = ',Account_ID,'

                            GROUP BY budget.Account_ID, budget.ID;');

IF @sql IS NOT null THEN BEGIN
PREPARE stmt FROM @sql;
EXECUTE stmt;
END;
END IF;

SET @sql = null;
SET @sql1 = null;
SET @sql2 = null;
SET @sql3 = null;
END
