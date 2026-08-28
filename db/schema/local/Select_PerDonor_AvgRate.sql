CREATE DEFINER=`hcfinancial`@`%` PROCEDURE `Select_PerDonor_AvgRate`(IN `Account_ID` INT)
BEGIN

SET SESSION group_concat_max_len = 1000000;

DROP TABLE IF EXISTS PerDonor_Rates;

SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;

SET @sql4 = null;



SELECT GROUP_CONCAT(DISTINCT CONCAT('IFNULL(

(SELECT

SUM(`action`.`Amount`)

FROM `action` LEFT JOIN `actiontype` ON `action`.`AType_ID` = `actiontype`.`id`

LEFT JOIN `till` `t` ON `action`.`Till_ID` = `t`.`ID`

LEFT JOIN `till` `obst` ON `action`.`ObserveTill_ID` = `obst`.`ID`

WHERE `actiontype`.`shortname` = ''Ex'' AND `obst`.`Account_ID` = `all_avgrates`.`Account_ID` 

AND `t`.`Type_ID` = `all_avgrates`.`FromTill` AND `obst`.`Type_ID` = `all_avgrates`.`ToTill` 

AND `action`.`ActionDate` >= `all_avgrates`.`BeginDate` and	`action`.`ActionDate` <= `all_avgrates`.`EndDate` AND action.donor_id = ',donor.ID,'),0) AS `',donor.Name,' Amount exchanged`,

IFNULL(

(SELECT 

	SUM(NullIF(`action`.`Amount`,0) * (SELECT NullIF(budget_donor.rate,0)

							 FROM budget_donor 

							 WHERE budget_donor.budget_id = budget_line.budget_id 

							 AND budget_donor.donor_id = ',donor.id,')

						 /100)

						 

FROM `action` LEFT JOIN `actiontype` ON `action`.`AType_ID` = `actiontype`.`id`

LEFT JOIN budget_line ON budget_line.ID = action.BudgetLine_ID

LEFT JOIN budget_donor ON budget_donor.budget_id = budget_line.Budget_ID

WHERE `actiontype`.`shortname` IN(''E'', ''Ex'') AND `action`.`Rate_ID` = `all_avgrates`.`ID` AND budget_donor.donor_id = ',donor.ID,'),0) AS `',donor.Name,' Expenses Amount`

') ORDER BY donor.id) INTO @sql1

FROM account_donor JOIN donor ON account_donor.Donor_id = donor.ID

WHERE account_donor.Account_id = Account_ID; 





SET @sql2 = CONCAT('CREATE TABLE IF NOT EXISTS `PerDonor_Rates` AS select 

all_avgrates.`ID` as `ID`,

all_avgrates.`ObserveRate_ID` as `ObserveRate_ID`,

all_avgrates.`Name` as `Rate group name`,

all_avgrates.`beginDate` as `Start date`,

 all_avgrates.`EndDate` as `End date`,

fromtp.`name` as `From till`,

totp.`name` as `To till`,

fromtp.`id` as `fromtp_id`,

totp.`id` as `totp_id`,

all_avgrates.`Account_ID`,

all_avgrates.`Value` as `Value`,

all_avgrates.`Exchanged` as `Amount exchanged`,

all_avgrates.`Exchanged` * all_avgrates.`Value` as `Equivalent value`,

all_avgrates.`Expenditures` as `Expenses Amount`,

ifnull(all_avgrates.`Expenditures` / NullIF(all_avgrates.`Value`,0),0) as `Expenses Amount In Foreign currency`,

ifnull((all_avgrates.`Exchanged` * all_avgrates.`Value`) - all_avgrates.`Expenditures`,0) as `Remaining Amount`,

ifnull(((all_avgrates.`Exchanged` * all_avgrates.`Value`) - all_avgrates.`Expenditures`) / NullIF(all_avgrates.`Value`,0),0) as `Remaining Amount in Foreign currency`,

all_avgrates.`ObserveRate` as `The Corresponding Rate Group`,

obs_all_avgrates.value as `The Corresponding Rate Value`,

ifnull(all_avgrates.`Exchanged`/ NullIF(obs_all_avgrates.value,0),0) as `The Corresponding Amount`'

,(CASE WHEN @sql1 IS null THEN '' ELSE CONCAT(',',@sql1) END),

'from all_avgrates left join all_avgrates obs_all_avgrates on obs_all_avgrates.id = all_avgrates.ObserveRate_ID left join tilltypes fromtp on all_avgrates.FromTill =fromtp.id left join tilltypes totp on all_avgrates.ToTill =totp.id 

WHERE all_avgrates.Account_ID = ',Account_ID,';');





IF @sql2 IS NOT null THEN BEGIN

PREPARE stmt FROM @sql2;

EXECUTE stmt;



END;

END IF;



SELECT GROUP_CONCAT(DISTINCT CONCAT('PerDonor_Rates.`',donor.Name,' Amount exchanged` * PerDonor_Rates.`Value` AS `',donor.Name,' Equivalent value`,

ifnull(PerDonor_Rates.`',donor.Name,' Expenses Amount` / NullIF(PerDonor_Rates.`Value`,0) ,0) as `',donor.Name,' Expenses Amount In Foreign currency`,

ifnull((PerDonor_Rates.`',donor.Name,' Amount exchanged` * PerDonor_Rates.`Value`) - PerDonor_Rates.`',donor.Name,' Expenses Amount`,0) as `',donor.Name,' Remaining Amount`,

ifnull(((PerDonor_Rates.`',donor.Name,' Amount exchanged` * PerDonor_Rates.`Value`) - PerDonor_Rates.`',donor.Name,' Expenses Amount`) / NullIF(PerDonor_Rates.`Value`,0),0) as `',donor.Name,' Remaining Amount in Foreign currency`,

ifnull(PerDonor_Rates.`',donor.Name,' Amount exchanged`/NullIF(PerDonor_Rates.`The Corresponding Rate Value`,0),0) as `',donor.Name,' The Corresponding Amount`

') ORDER BY donor.id) INTO @sql3

FROM account_donor JOIN donor ON account_donor.Donor_id = donor.ID

WHERE account_donor.Account_id = Account_ID;





SET @sql4 = CONCAT('select PerDonor_Rates.*, ',@sql3,' FROM PerDonor_Rates;');



IF @sql4 IS NOT null THEN BEGIN

PREPARE stmt FROM @sql4;

EXECUTE stmt;



END;

END IF;



DROP TABLE IF EXISTS PerDonor_Rates;

SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;

SET @sql4 = null;

END
