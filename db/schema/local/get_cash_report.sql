CREATE DEFINER=`hcfinancial`@`%` PROCEDURE `get_cash_report`()
BEGIN

    SET SESSION group_concat_max_len = 1000000;



    SET @sql1 = NULL;

    SET @sql2 = NULL;

    SET @sql3 = NULL;

    SET @sql4 = NULL;



    SELECT GROUP_CONCAT(DISTINCT 

        CONCAT(

            ' MAX(CASE WHEN cb.CashAccount_id = ',

            cb.CashAccount_id,

            ' THEN cb.Account_Net END) AS `',

            cb.CashAccount_Name,'`'

        )

    ) INTO @sql1

    FROM cash_balances cb;



    SET @sql2 = CONCAT(

        'CREATE TEMPORARY TABLE t1 AS

        SELECT 

            cb.City_Name AS `City`,

            cb.Subproject_Name AS `Project`,

            cb.Account_ID,

            cb.Account_Name AS `Account`,

            cb.Account_Type,

            cb.tilltype_Name,

            cb.Subproject_ID,

            cb.Category_Name AS `Category`,

            CASE WHEN cb.account_IsClosed = 1 THEN ''Closed'' ELSE ''Open'' END AS `Status Account`,

            ', @sql1 ,',

            SUM(cb.Account_Net) AS `Net Account (SYR)`,

            SUM(cb.Account_Net_USD) AS `Net Account (USD)`

        FROM cash_balances cb

        GROUP BY cb.Account_ID

        ORDER BY cb.City_ID, cb.Subproject_Name, cb.Account_Name'

    );



    PREPARE stmt FROM @sql2;

    EXECUTE stmt;

    DEALLOCATE PREPARE stmt;





    SELECT GROUP_CONCAT(DISTINCT

        CONCAT(

        'COALESCE(MAX(CASE 

            WHEN tt.TillTypeName = ''', tt.Name ,''' THEN tt.Net END),0) AS `', tt.Name ,'-Net Account`'

        )

    ) INTO @sql3

    FROM tilltypes tt

    WHERE LOWER(tt.Name) NOT IN ('syr','pay');



    SET @sql4 = CONCAT(

        'SELECT t1.*,

            ', @sql3, '

        FROM t1

        LEFT JOIN (

            SELECT 

                till.Account_ID,

                tilltypes.Name AS TillTypeName,

                SUM(

                    CASE 

                        WHEN actiontype.shortname = ''I'' THEN action.Amount

                        WHEN actiontype.shortname IN (''E'',''Ex'') THEN -action.Amount

                        ELSE 0

                    END

                ) AS Net

            FROM action

            LEFT JOIN till ON action.Till_ID = till.ID

            LEFT JOIN tilltypes ON till.Type_ID = tilltypes.ID

            LEFT JOIN actiontype ON action.atype_id = actiontype.id

            GROUP BY till.Account_ID, tilltypes.Name

        ) tt ON tt.Account_ID = t1.Account_ID

        GROUP BY t1.Account_ID'

    );



    PREPARE stmt FROM @sql4;

    EXECUTE stmt;

    DEALLOCATE PREPARE stmt;





    SELECT 

        CashAccount_id,

        CashAccount_Name,

        CashAccount_Net

    FROM cash_balances

    GROUP BY CashAccount_id

    ORDER BY CashAccount_id;



    DROP TEMPORARY TABLE IF EXISTS t1;



END
