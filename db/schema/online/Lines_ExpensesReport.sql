CREATE DEFINER=`hcfi`@`%` PROCEDURE `Lines_ExpensesReport`(IN `Acc_ID` INT, IN `Budget_ID` INT)
BEGIN

SET SESSION group_concat_max_len = 1000000;

DROP TABLE IF EXISTS lines_expenses_table;

SET @donorsql1 = null;

SET @donorsql2 = null;

SET @donorsql3 = null;

SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;

SET @sql4 = null;





SELECT GROUP_CONCAT(DISTINCT CONCAT('max(CASE all_budgetlines.BudgetMonthes WHEN ''',all_budgetlines.BudgetMonthes, ''' THEN ifnull(all_budgetlines.Expenditures,0) END ) AS `', DATE_FORMAT(all_budgetlines.BudgetMonthes,'%b-%Y'),'`')) INTO @sql1

FROM all_budgetlines WHERE all_budgetlines.Account_ID = Acc_ID AND all_budgetlines.budget_ID = Budget_ID;



SELECT GROUP_CONCAT(DISTINCT CONCAT('max(CASE all_budgetlines.BudgetMonthes WHEN ''',all_budgetlines.BudgetMonthes,''' THEN

ifnull(all_budgetlines.Expenditures,0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0) END ) AS `',donor.Name,' ', DATE_FORMAT(all_budgetlines.BudgetMonthes,'%b-%Y'),'`')) INTO @donorsql1

FROM all_budgetlines JOIN budget ON all_budgetlines.budget_ID = budget.ID

JOIN budget_donor on budget_donor.budget_id = budget.ID

JOIN donor ON donor.ID = budget_donor.donor_id

WHERE all_budgetlines.Account_ID = Acc_ID AND all_budgetlines.budget_ID = Budget_ID;



SELECT GROUP_CONCAT(DISTINCT CONCAT(' (all_budgetlines.Expenditures_With_Out_Of_Date - SUM(all_budgetlines.Expenditures))*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0) AS `',donor.Name,' Out of budget date`,

all_budgetlines.Expenditures_With_Out_Of_Date*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0) AS `',donor.Name,' Expenditures Total`,

all_budgetlines.Expected_Total*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0) AS `',donor.Name,' Expected Total - Budget`,

ifnull((all_budgetlines.Expected_Total/NULLIF(all_budgetlines.Timing,0)),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0) AS `',donor.Name,' Expected Total - Month`,

(all_budgetlines.Expected_Total - all_budgetlines.Expenditures_With_Out_Of_Date)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = all_budgetlines.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0) AS `',donor.Name,' Remaining`')) INTO @donorsql2

FROM

donor JOIN budget_donor ON donor.ID = budget_donor.donor_id

JOIN budget ON budget_donor.budget_id = budget.ID

WHERE budget.ID = Budget_ID;







SET @sql2 = CONCAT('CREATE TABLE IF NOT EXISTS lines_expenses_table SELECT 

all_budgetlines.Budget_ID,

all_budgetlines.linecategory_Name AS ''Budget Line Type'',

all_budgetlines.line_Name AS ''Budget Line'', all_budgetlines.Timing AS ''Timing'',

',@sql1,',',@donorsql1,' ,

all_budgetlines.Expenditures_With_Out_Of_Date - SUM(all_budgetlines.Expenditures) AS ''Out of budget date'',

all_budgetlines.Expenditures_With_Out_Of_Date AS ''Expenditures Total'',

all_budgetlines.Expected_Total AS ''Expected Total - Budget'',

ifnull(all_budgetlines.Expected_Total/NULLIF(all_budgetlines.Timing,0),0) AS ''Expected Total - Month'',

all_budgetlines.Expected_Total - all_budgetlines.Expenditures_With_Out_Of_Date AS ''Remaining'',

',@donorsql2,'

FROM all_budgetlines WHERE all_budgetlines.Account_ID = ',Acc_ID,' AND all_budgetlines.budget_ID = ',Budget_ID,' GROUP BY all_budgetlines.line_ID;');

     



IF @sql2 IS NOT null THEN BEGIN

PREPARE stmt FROM @sql2;

EXECUTE stmt;

END;

END IF;



SELECT

GROUP_CONCAT(DISTINCT CONCAT('ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `lines_expenses_table`.`Expenditures Total` / NULLIF(all_avgrates.Value,0) END),0) AS `Expenditures Total - ',tilltypes.Name,'`,

                            

ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `lines_expenses_table`.`Expected Total - Budget` / NULLIF(all_avgrates.Value,0) END),0) AS `Expected Total - Budget - ',tilltypes.Name,'`,

                             

ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `lines_expenses_table`.`Expected Total - Month` / NULLIF(all_avgrates.Value,0) END),0) AS `Expected Total - Month - ',tilltypes.Name,'`,

                             

ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `lines_expenses_table`.`Remaining` / NULLIF(all_avgrates.Value,0) END),0) AS `Remaining - ',tilltypes.Name,'`')) INTO @sql3

FROM budget JOIN budgt_avgrate ON budgt_avgrate.budget_id = budget.id

JOIN all_avgrates ON budgt_avgrate.AvgRate_ID = all_avgrates.ID

JOIN tilltypes ON tilltypes.ID = all_avgrates.FromTill

WHERE budget.ID = Budget_ID;



SELECT

GROUP_CONCAT(DISTINCT CONCAT('ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `lines_expenses_table`.`Expenditures Total` / NULLIF(all_avgrates.Value,0) END),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = lines_expenses_table.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0)  AS `',donor.Name,' Expenditures Total - ',tilltypes.Name,'`,

                          

ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `lines_expenses_table`.`Expected Total - Budget` / NULLIF(all_avgrates.Value,0) END),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = lines_expenses_table.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0)  AS `',donor.Name,' Expected Total - Budget - ',tilltypes.Name,'`,

                             

ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `lines_expenses_table`.`Expected Total - Month` / NULLIF(all_avgrates.Value,0) END),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = lines_expenses_table.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0)  AS `',donor.Name,' Expected Total - Month - ',tilltypes.Name,'`,

                             

ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `lines_expenses_table`.`Remaining` / NULLIF(all_avgrates.Value,0) END),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = lines_expenses_table.budget_ID AND budget_donor.donor_id = ',donor.id,')/NULLIF(100,0)  AS `',donor.Name,' Remaining - ',tilltypes.Name,'`')) INTO @donorsql3

FROM budget JOIN budgt_avgrate ON budgt_avgrate.budget_id = budget.id

JOIN budget_donor ON budget_donor.budget_id = budget.ID

JOIN donor ON budget_donor.donor_id = donor.ID

JOIN all_avgrates ON budgt_avgrate.AvgRate_ID = all_avgrates.ID

JOIN tilltypes ON tilltypes.ID = all_avgrates.FromTill

WHERE budget.ID = Budget_ID;



SET @sql4 = CONCAT('select lines_expenses_table.*',

(CASE WHEN @sql3 IS null THEN '' ELSE CONCAT(',',@sql3) END),

(CASE WHEN @donorsql3 IS null THEN '' ELSE CONCAT(',',@donorsql3) END),

' FROM lines_expenses_table LEFT JOIN budgt_avgrate ON budgt_avgrate.budget_id = lines_expenses_table.Budget_ID LEFT JOIN all_avgrates ON budgt_avgrate.AvgRate_ID = all_avgrates.ID\nLEFT JOIN tilltypes ON tilltypes.ID = all_avgrates.FromTill GROUP BY `lines_expenses_table`.`Budget Line`;');            





IF @sql4 IS NOT null THEN BEGIN

PREPARE stmt FROM @sql4;

EXECUTE stmt;

END;

END IF;







DROP TABLE IF EXISTS lines_expenses_table;

SET @donorsql1 = null;

SET @donorsql2 = null;

SET @donorsql3 = null;

SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;

SET @sql4 = null;





END
