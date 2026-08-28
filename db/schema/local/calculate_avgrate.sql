CREATE DEFINER=`hcfinancial`@`%` PROCEDURE `calculate_avgrate`(IN `account_id` INT, IN `begin_date` VARCHAR(50), IN `end_date` VARCHAR(50), IN `FromTill` INT, IN `ToTill` INT)
BEGIN  

SET SESSION group_concat_max_len = 1000000;

  SELECT (sum(action.amount * action.Rate) / sum(action.Amount)) as 'value'

  FROM action LEFT JOIN actiontype ON actiontype.id = action.AType_ID 

  LEFT JOIN till t on action.Till_ID = t.ID

  LEFT JOIN till obst on action.ObserveTill_ID = obst.ID

  LEFT JOIN tilltypes ON tilltypes.ID = t.Type_ID

  LEFT JOIN account on t.Account_ID = account.ID 

  WHERE (t.Account_ID = account_id) 

                AND (t.Type_ID = FromTill)

                AND (obst.Type_ID = ToTill) 

                AND (ActionDate between begin_date and end_date) 

                AND actiontype.shortname LIKE 'Ex' 

                AND action.Amount > 0 

                AND action.Rate > 0;

                END
