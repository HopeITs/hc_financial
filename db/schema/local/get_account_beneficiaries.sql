CREATE DEFINER=`hcfinancial`@`%` PROCEDURE `get_account_beneficiaries`(IN `Account_ID` INT, IN `Subproject_ID` INT)
BEGIN

    SET SESSION group_concat_max_len = 1000000;

    SET @sql = NULL;

    SET @sql1 = NULL;



	IF Account_ID <> -1 THEN 

        SELECT GROUP_CONCAT(DISTINCT 

            CONCAT(

                'SUM((CASE WHEN(actiontype.shortname LIKE ''E'') and tt.Name = ''USD'' THEN IFNULL(action.Amount, 0) ELSE IFNULL(action.Amount / NULLIF(ar.value, 0), 0) END)) AS `Total invoices amount(', ttypes.Name, ')`'

				

				

            )

        ) INTO @sql

        FROM till t

        JOIN tilltypes ttypes ON ttypes.ID = t.Type_ID

        WHERE t.Account_ID = Account_ID

          AND LOWER(ttypes.Name) <> 'syr' AND LOWER(ttypes.Name) <> 'eur';

    ELSE

        SELECT GROUP_CONCAT(DISTINCT 

            CONCAT(

                'SUM((CASE WHEN(actiontype.shortname LIKE ''E'') and tt.Name = ''USD'' THEN IFNULL(action.Amount, 0) ELSE IFNULL(action.Amount / NULLIF(ar.value, 0), 0) END)) AS `Total invoices amount( ', ttypes.Name, ')`'

            )

        ) INTO @sql

        FROM beneficiary b

        LEFT JOIN account a ON b.Account_ID = a.ID

        LEFT JOIN till t ON a.ID = t.Account_ID

        LEFT JOIN tilltypes ttypes ON ttypes.ID = t.Type_ID

        WHERE LOWER(ttypes.Name) <> 'syr' AND LOWER(ttypes.Name) <> 'eur';

    END IF;

	

	

    SET @sql1 = CONCAT(

'WITH payments_agg AS (

    SELECT p.ID AS payment_id,

           p.Beneficiary_ID,

           p.DueDate,

           CASE WHEN IFNULL(SUM(ca.Amount),0) = 0 && IFNULL(SUM(ca.Returned_Amount),0) = 0 THEN ''Unpaid''

                WHEN IFNULL(SUM(ca.Amount),0) < p.Amount THEN ''Incomplete''

                ELSE ''Done'' END AS payment_state,

           CASE WHEN MAX(ca.Source) = ''action_not_approved'' THEN ''No''

                WHEN MAX(ca.Source) = ''action'' THEN ''Yes''

                ELSE NULL END AS Approved

    FROM payment p

    LEFT JOIN action_payment ap ON p.id = ap.payment_id

    LEFT JOIN (

        SELECT ID, Amount,Returned_Amount, ''action'' AS Source FROM action

        UNION ALL

        SELECT ID, Amount,Returned_Amount, ''action_not_approved'' AS Source FROM action_not_approved

    ) ca ON ca.ID = ap.action_id

    GROUP BY p.ID

),

payments_summary AS (

    SELECT pa.Beneficiary_ID,

           COUNT(*) AS `Total payments`,

           SUM(CASE WHEN pa.payment_state = ''Incomplete'' THEN 1 ELSE 0 END) AS `Incomplete payments`,

           SUM(CASE WHEN pa.payment_state = ''Unpaid'' THEN 1 ELSE 0 END) AS `Unpaid payments`,

           SUM(CASE WHEN ((pa.Approved <> ''Yes'' AND pa.DueDate < NOW()) OR (pa.payment_state <> ''Done'' AND pa.DueDate < NOW())) THEN 1 ELSE 0 END) AS `Late payments`,

           SUM(CASE WHEN pa.Approved = ''No'' THEN 1 ELSE 0 END) AS `Not approved payments`

    FROM payments_agg pa

    GROUP BY pa.Beneficiary_ID

)



SELECT

    beneficiary.ID AS `beneficiary_ID`,

    subproject.ID AS `subproject_ID`,

    beneficiary.MicroProject_ID AS `Micro project number`,

    account.id as `Accoun_ID`,

    CONCAT(beneficiary.FirstName, '' '', beneficiary.FatherName, '' '', beneficiary.LastName) AS `Beneficiary Name`,

    beneficiary.Project_Name AS `Project Title`,

    beneficiary.NationalNumber AS `ID number`,

    beneficiary.Mobile AS `Mobile Number`,

    beneficiary.Partners AS `Names of partners`,

    account.Name as `Group account`,

    donor.Name AS `Donor`,

    beneficiary.OrderNumber AS `Order Number`,

    beneficiary.ReceiptNumber AS `Receipt number`,

    beneficiary.Date AS `Financing date`,

    beneficiary.Notes AS `Notes`,

    tt.Name AS `Currency`,

	beneficiary.ExchangeRate AS `Exchange Rate`,

    Round(CASE

    WHEN tt.Name = ''SYR'' THEN beneficiary.Amount

    WHEN tt.Name = ''USD'' THEN beneficiary.Amount * ExchangeRate

END,2) AS `Loan Amount (SYR)`



,Round(CASE

    WHEN tt.Name = ''SYR'' THEN beneficiary.Amount / ExchangeRate

    WHEN tt.Name = ''USD'' THEN beneficiary.Amount

END,2) AS `Loan Amount (USD)`,

    (beneficiary.ToReturn_Amount / beneficiary.Amount) * 100 AS `Returned percentage`,

    beneficiary.ToReturn_Amount AS `Amount owed`,

	(select sum(action.amount) from action_payment left join payment on payment.id = action_payment.payment_id left join action on action.id = action_payment.action_id  where payment.Beneficiary_ID = beneficiary.`ID`) as `Amount Paid`,

	(select sum(action.Returned_Amount) from action_payment left join payment on payment.id = action_payment.payment_id left join action on action.id = action_payment.action_id  where payment.Beneficiary_ID = beneficiary.`ID`) as `Amount Returned`,

    beneficiary.Payment AS `Payment`,

    SUM((CASE WHEN(actiontype.shortname LIKE ''O'') THEN action.Amount ELSE 0 END)) AS `Advance Payment amount`,

    SUM((CASE WHEN(actiontype.shortname LIKE ''R'') THEN action.Returned_Amount ELSE 0 END)) AS `Returned amount`,

    (SUM((CASE WHEN(actiontype.shortname LIKE ''O'') THEN action.Amount ELSE 0 END))

     - SUM((CASE WHEN(actiontype.shortname LIKE ''R'') THEN action.Amount ELSE 0 END))) AS `Paid in advance for purchases`,

    (beneficiary.Amount - (SUM((CASE WHEN(actiontype.shortname LIKE ''O'') THEN action.Amount ELSE 0 END))

     - SUM((CASE WHEN(actiontype.shortname LIKE ''R'') THEN action.Amount ELSE 0 END)))) AS `Remaining from the loan for purchases`,

	SUM((CASE WHEN(actiontype.shortname LIKE ''E'') and tt.Name = ''SYR'' THEN action.Amount ELSE 0 END)) AS `Invoices Amounts (SYR)`,

	SUM((CASE WHEN(actiontype.shortname LIKE ''E'') and tt.Name = ''USD'' THEN action.Amount ELSE 0 END)) AS `Invoices Amounts (USD)`,

    (beneficiary.Amount - SUM((CASE WHEN(actiontype.shortname LIKE ''E'') and tt.Name = ''SYR'' THEN action.Amount ELSE 0 END))) AS `Net Remaining from the loan (SYR)`',

	

    IF(@sql IS NULL OR @sql = '''', '', CONCAT(', ', @sql)),

', 

    IFNULL(ps.`Total payments`, 0) AS `Total payments`,

    IFNULL(ps.`Incomplete payments`, 0) AS `Incomplete payments`,

    IFNULL(ps.`Unpaid payments`, 0) AS `Unpaid payments`,

    IFNULL(ps.`Late payments`, 0) AS `Late payments`,

    IFNULL(ps.`Not approved payments`, 0) AS `Not approved payments`,

    (CASE WHEN (beneficiary.Amount <= SUM((CASE WHEN(actiontype.shortname LIKE ''E'') THEN action.Amount ELSE 0 END))) THEN ''Done'' ELSE ''Ongoing'' END) AS `Financed`,

    (SELECT COUNT(*) FROM beneficiary_attachments WHERE beneficiary_attachments.beneficiary_id = beneficiary.id) AS `Attachments Count`

FROM beneficiary

LEFT JOIN account ON beneficiary.Account_ID = account.ID

LEFT JOIN account_donor ON account_donor.account_ID = account.ID

LEFT JOIN donor ON donor.ID = account_donor.Donor_id

LEFT JOIN till ON account.ID = till.Account_ID

LEFT JOIN tilltypes ON tilltypes.id = till.Type_ID

LEFT JOIN action ON action.Till_ID = till.ID AND beneficiary.MicroProject_ID = action.MicroProject_ID

LEFT JOIN actiontype ON actiontype.id = action.AType_ID

LEFT JOIN subproject ON subproject.id = account.subproject_id

LEFT JOIN rate r ON action.Rate_ID = r.ID

LEFT JOIN all_avgrates ar ON ar.ID = action.Rate_ID

LEFT JOIN tilltypes ftp ON ftp.ID = r.FromTill

LEFT JOIN tilltypes tt ON tt.ID = beneficiary.currency_tilltype_id

LEFT JOIN payments_summary ps ON ps.Beneficiary_ID = beneficiary.ID

WHERE LOWER(tilltypes.Name) = ''syr'' 

  AND subproject.id = ', Subproject_ID, 

    IF(Account_ID = -1, '', CONCAT(' AND account.id = ', Account_ID)),

' GROUP BY beneficiary.ID, CONCAT(beneficiary.FirstName, '' '', beneficiary.LastName)'

);



    PREPARE stmt FROM @sql1;

    EXECUTE stmt;

    DEALLOCATE PREPARE stmt;



    SET @sql = NULL;

    SET @sql1 = NULL;

END
