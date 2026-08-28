CREATE DEFINER=`hcfi`@`%` PROCEDURE `Select_Budget_Lines_Details`(IN `Budget_ID` INT)
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
WHERE budget.ID = Budget_ID;


SELECT GROUP_CONCAT(DISTINCT CONCAT(' all_budgetlines.Expenditures_With_Out_Of_Date	*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Expenditures Total`')) INTO @sql2
FROM
donor JOIN budget_donor ON donor.ID = budget_donor.donor_id
JOIN budget ON budget_donor.budget_id = budget.ID
WHERE budget.ID = Budget_ID;


SELECT GROUP_CONCAT(DISTINCT CONCAT(' (all_budgetlines.Expected_Total - all_budgetlines.Expenditures_With_Out_Of_Date)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Remaining`')) INTO @sql3
FROM
donor JOIN budget_donor ON donor.ID = budget_donor.donor_id
JOIN budget ON budget_donor.budget_id = budget.ID
WHERE budget.ID = Budget_ID;


SET @sql = CONCAT(' SELECT all_budgetlines.budget_line_ID, all_budgetlines.budget_ID, all_budgetlines.line_ID,
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
                  FROM all_budgetlines where budget_ID =',Budget_ID,' Group By all_budgetlines.line_ID ;');

IF @sql IS NOT null THEN BEGIN
PREPARE stmt FROM @sql;
EXECUTE stmt;
END;
END IF;

set @sql = null;
SET @sql1 = null;
SET @sql2 = null;
SET @sql3 = null;
END
