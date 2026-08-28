CREATE DEFINER=`hcfinancial`@`%` PROCEDURE `LineTypes_ExpensesReport`(IN `Acc_ID` INT, IN `Budget_ID` INT)
BEGIN

SET SESSION group_concat_max_len = 1000000;



DROP TABLE IF EXISTS LineTypes_Expenses_Table;

SET @donorsql1 = null;

SET @donorsql2 = null;

SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;





SET @sql1 = CONCAT('CREATE TABLE IF NOT EXISTS LineTypes_Expenses_Table

SELECT E_Total.budget_ID AS ''budget_ID'', linecategory.Name AS ''Budget Line Type'',

ifnull(SUM(E_Total.Expected_Total),0) AS ''Expected Total - Budget'',

ifnull(SUM(E_Total.Expenditures_With_Out_Of_Date),0) AS ''Expenditures Total'',

ifnull(SUM(E_Total.Expected_Total) - SUM(E_Total.Expenditures_With_Out_Of_Date),0) AS ''Remaining''

FROM linecategory JOIN

                   (SELECT all_budgetlines.Account_ID, all_budgetlines.budget_ID, all_budgetlines.linecategory_ID,

                   all_budgetlines.Expected_Total, SUM(all_budgetlines.Expenditures) AS ''Expenditures'', 

                   all_budgetlines.Expenditures_With_Out_Of_Date

                   FROM all_budgetlines GROUP BY all_budgetlines.budget_ID, all_budgetlines.budget_line_ID) E_Total

                   ON linecategory.ID = E_Total.linecategory_ID

WHERE E_Total.Account_ID = ',Acc_ID,' AND E_Total.budget_ID = ',Budget_ID,'

GROUP BY E_Total.Account_ID, E_Total.budget_ID, E_Total.linecategory_ID;');

                            

                            

IF @sql1 IS NOT null THEN BEGIN

PREPARE stmt FROM @sql1;

EXECUTE stmt;

END;

END IF;



SELECT GROUP_CONCAT(DISTINCT CONCAT(' LineTypes_Expenses_Table.`Expected Total - Budget`*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = LineTypes_Expenses_Table.budget_ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Expected Total - Budget`,

                                    

LineTypes_Expenses_Table.`Expenditures Total`*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = LineTypes_Expenses_Table.budget_ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Expenditures Total`,

                                    

LineTypes_Expenses_Table.`Remaining`*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = LineTypes_Expenses_Table.budget_ID AND budget_donor.donor_id = ',donor.id,')/100 AS `',donor.Name,' Remaining` 

')) INTO @donorsql1

FROM

donor JOIN budget_donor ON donor.ID = budget_donor.donor_id

JOIN budget ON budget_donor.budget_id = budget.ID

WHERE budget.ID = Budget_ID;



SELECT

GROUP_CONCAT(DISTINCT CONCAT('ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `LineTypes_Expenses_Table`.`Expected Total - Budget` / all_avgrates.Value END),0) AS `Expected Total - Budget - ',tilltypes.Name,'`,



ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `LineTypes_Expenses_Table`.`Expenditures Total` / all_avgrates.Value END),0) AS `Expenditures Total - ',tilltypes.Name,'`,



ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `LineTypes_Expenses_Table`.`Remaining` / all_avgrates.Value END),0) AS `Remaining - ',tilltypes.Name,'`')) INTO @sql2

FROM budget LEFT JOIN budgt_avgrate ON budgt_avgrate.budget_id = budget.id

LEFT JOIN all_avgrates ON budgt_avgrate.AvgRate_ID = all_avgrates.ID

LEFT JOIN tilltypes ON tilltypes.ID = all_avgrates.FromTill

WHERE budget.ID = Budget_ID;



SELECT

GROUP_CONCAT(DISTINCT CONCAT('ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `LineTypes_Expenses_Table`.`Expected Total - Budget` / all_avgrates.Value END),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = LineTypes_Expenses_Table.budget_ID AND budget_donor.donor_id = ',donor.id,')/100  AS `',donor.Name,'  Expected Total - Budget - ',tilltypes.Name,'`,



ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `LineTypes_Expenses_Table`.`Expenditures Total` / all_avgrates.Value END),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = LineTypes_Expenses_Table.budget_ID AND budget_donor.donor_id = ',donor.id,')/100  AS `',donor.Name,' Expenditures Total - ',tilltypes.Name,'`,



ifnull(MAX(CASE WHEN tilltypes.Name = ''',tilltypes.Name,''' THEN `LineTypes_Expenses_Table`.`Remaining` / all_avgrates.Value END),0)*(SELECT budget_donor.rate FROM budget_donor WHERE budget_donor.budget_id = LineTypes_Expenses_Table.budget_ID AND budget_donor.donor_id = ',donor.id,')/100  AS `',donor.Name,' Remaining - ',tilltypes.Name,'`')) INTO @donorsql2

FROM budget JOIN budgt_avgrate ON budgt_avgrate.budget_id = budget.id

JOIN budget_donor ON budget_donor.budget_id = budget.ID

JOIN donor ON budget_donor.donor_id = donor.ID

JOIN all_avgrates ON budgt_avgrate.AvgRate_ID = all_avgrates.ID

JOIN tilltypes ON tilltypes.ID = all_avgrates.FromTill

WHERE budget.ID = Budget_ID;



SET @sql3 = CONCAT('SELECT LineTypes_Expenses_Table.*,',@donorsql1,

(CASE WHEN @sql2 IS null THEN '' ELSE CONCAT(',',@sql2) END), 

(CASE WHEN @donorsql2 IS null THEN '' ELSE CONCAT(',',@donorsql2) END), 

'FROM LineTypes_Expenses_Table LEFT JOIN budgt_avgrate ON budgt_avgrate.budget_id = LineTypes_Expenses_Table.Budget_ID\nLEFT JOIN all_avgrates ON budgt_avgrate.AvgRate_ID = all_avgrates.ID\nLEFT JOIN tilltypes ON tilltypes.ID = all_avgrates.FromTill GROUP BY `LineTypes_Expenses_Table`.`Budget Line Type`;');





IF @sql3 IS NOT null THEN BEGIN

PREPARE stmt FROM @sql3;

EXECUTE stmt;

END;

END IF;







DROP TABLE IF EXISTS LineTypes_Expenses_Table;

SET @donorsql1 = null;

SET @donorsql2 = null;

SET @sql = null;

SET @sql1 = null;

SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;

End
