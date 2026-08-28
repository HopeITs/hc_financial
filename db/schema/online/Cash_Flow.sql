CREATE DEFINER=`hcfi`@`%` PROCEDURE `Cash_Flow`(IN `Action_Account_ID` INT)
BEGIN
SET SESSION group_concat_max_len = 1000000;
SET @sql = null;
SET @sql1 = null;
SET @sql2 = null;
SET @sql3 = null;

SELECT GROUP_CONCAT(DISTINCT CONCAT(
  ' (SELECT IFNULL(SUM(CASE WHEN actiontype.shortname = ''I'' THEN cashaction_denominations.amount 
                            WHEN actiontype.shortname = ''E'' THEN -1*cashaction_denominations.amount 
                            ELSE 0 END),0) 
     FROM cashaction_denominations 
     LEFT JOIN actiontype ON cashaction_denominations.AType_id = actiontype.id 
     WHERE cashaction_denominations.cashaction_id = cashaction.id 
       AND cashaction_denominations.denominations_id = ', denominations.id, ') AS ',
  CASE WHEN denominations.name = 0 THEN '`Uncounted`' ELSE CONCAT('`', denominations.name, '`') END, '')
ORDER BY denominations.id ASC
SEPARATOR ',') INTO @sql1
FROM denominations
WHERE denominations.currency_tilltype_id = (
  SELECT currency_tilltype_id FROM cashaccount WHERE id = Action_Account_ID
);

SET @sql2 = CONCAT('SELECT cashaction.id
, cashaction.ActionDate as `Counting date`
,',@sql1,'
, cashaction.Note as `Description AR`
, cashaction.E_Note as `Description EN`
, (SELECT SUM( CASE WHEN actiontype.shortname = ''I'' THEN cashaction_denominations.amount 
					WHEN actiontype.shortname = ''E'' THEN -1*cashaction_denominations.amount 
	ELSE 0 END)
	FROM cashaction_denominations
	LEFT JOIN actiontype ON cashaction_denominations.AType_id = actiontype.id
	WHERE cashaction_denominations.cashaction_id = cashaction.id
) AS `Total`
,created_byuser.username as `Created by`
,cashaction.created_at as `Date created`
,edited_byuser.username as `Edited by`
,cashaction.updated_at as `Modification date`

FROM cashaction
LEFT JOIN `user` created_byuser on created_byuser.id = cashaction.User_ID
LEFT JOIN `user` edited_byuser on edited_byuser.id = cashaction.edited_by
WHERE cashaction.cashaccount_id = ',Action_Account_ID,'
GROUP BY cashaction.id;');

IF @sql2 IS NOT null THEN BEGIN
PREPARE stmt FROM @sql2;
EXECUTE stmt;
END;
END IF;

SET @sql = null;
SET @sql1 = null;
SET @sql2 = null;
SET @sql3 = null;
END
