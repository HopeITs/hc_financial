CREATE DEFINER=`hcfinancial`@`%` PROCEDURE `get_ExchangeAccount_Rates`(IN `Account_ID` INT, IN `FromTill_ID` INT, IN `Totill_ID` INT)
BEGIN

SET SESSION group_concat_max_len = 1000000;



SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;



SELECT CONCAT('SELECT exchangeaccount_rate.id, exchangeaccount_rate.ratedate AS `Date`, exchangeaccount_rate.rate AS `Rate`,



(SELECT ifnull(SUM(action.Amount),0) From action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN actiontype on action.AType_ID = actiontype.id WHERE till.Account_ID = exchangeaccount_rate.Account_ID AND till.Type_ID = exchangeaccount_rate.FromTill_ID AND actiontype.shortname = ''EX'' AND action.Rate = exchangeaccount_rate.rate) AS ` ',tilltypes.Name,' - Exchange`,



(SELECT ifnull(SUM(action.Amount),0) From action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN actiontype on action.AType_ID = actiontype.id WHERE till.Account_ID = exchangeaccount_rate.Account_ID AND till.Type_ID = exchangeaccount_rate.FromTill_ID AND actiontype.shortname = ''I'' AND action.Rate = exchangeaccount_rate.rate) AS ` ',tilltypes.Name,' - Income`,



(SELECT ifnull(SUM(action.Amount),0) From action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN actiontype on action.AType_ID = actiontype.id WHERE till.Account_ID = exchangeaccount_rate.Account_ID AND till.Type_ID = exchangeaccount_rate.FromTill_ID AND actiontype.shortname = ''EX'' AND action.Rate = exchangeaccount_rate.rate) - (SELECT ifnull(SUM(action.Amount),0) From action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN actiontype on action.AType_ID = actiontype.id WHERE till.Account_ID = exchangeaccount_rate.Account_ID AND till.Type_ID = exchangeaccount_rate.FromTill_ID AND actiontype.shortname = ''I'' AND action.Rate = exchangeaccount_rate.rate) AS `',tilltypes.Name,' - Difference`') FROM tilltypes WHERE tilltypes.ID = FromTill_ID INTO @sql1;





SELECT CONCAT('

(SELECT ifnull(SUM(action.Amount),0) From action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN actiontype on action.AType_ID = actiontype.id WHERE till.Account_ID = exchangeaccount_rate.Account_ID AND till.Type_ID = exchangeaccount_rate.ToTill_ID AND actiontype.shortname = ''I'' AND action.Rate = exchangeaccount_rate.rate) AS ` ',tilltypes.Name,' - Income`,



(SELECT ifnull(SUM(action.Amount),0) From action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN actiontype on action.AType_ID = actiontype.id WHERE till.Account_ID = exchangeaccount_rate.Account_ID AND till.Type_ID = exchangeaccount_rate.ToTill_ID AND actiontype.shortname = ''E'' AND action.Rate = exchangeaccount_rate.rate) AS ` ',tilltypes.Name,' - Outcome`,





(SELECT ifnull(SUM(action.Amount),0) From action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN actiontype on action.AType_ID = actiontype.id WHERE till.Account_ID = exchangeaccount_rate.Account_ID AND till.Type_ID = exchangeaccount_rate.ToTill_ID AND actiontype.shortname = ''I'' AND action.Rate = exchangeaccount_rate.rate) - (SELECT ifnull(SUM(action.Amount),0) From action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN actiontype on action.AType_ID = actiontype.id WHERE till.Account_ID = exchangeaccount_rate.Account_ID AND till.Type_ID = exchangeaccount_rate.ToTill_ID AND actiontype.shortname = ''E'' AND action.Rate = exchangeaccount_rate.rate) AS `',tilltypes.Name,' - Difference`') FROM tilltypes WHERE tilltypes.ID = ToTill_ID INTO @sql2;



SET @sql3 = CONCAT(@sql1,', ',@sql2,' From exchangeaccount_rate WHERE exchangeaccount_rate.Account_ID = ',Account_ID,' AND exchangeaccount_rate.FromTill_ID = ',FromTill_ID,' AND exchangeaccount_rate.ToTill_ID = ',Totill_ID,';');







 IF @sql3 IS NOT null THEN BEGIN

 PREPARE stmt FROM @sql3;

 EXECUTE stmt;

 END;

 END IF;



SET @sql1 = null;

SET @sql2 = null;

SET @sql3 = null;



END
