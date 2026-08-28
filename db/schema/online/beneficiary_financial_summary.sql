CREATE DEFINER=`hcfi`@`%` PROCEDURE `beneficiary_financial_summary`(
    IN p_level VARCHAR(20),
    IN p_account_ids TEXT,
    IN p_subproject_ids TEXT
)
BEGIN
    
    DROP TEMPORARY TABLE IF EXISTS tmp_scope_subprojects;
    DROP TEMPORARY TABLE IF EXISTS tmp_scope_accounts;
    DROP TEMPORARY TABLE IF EXISTS tmp_account_microprojects;
    DROP TEMPORARY TABLE IF EXISTS tmp_action_by_account;
    DROP TEMPORARY TABLE IF EXISTS tmp_payment_by_account;
    DROP TEMPORARY TABLE IF EXISTS tmp_loan_by_account;
    DROP TEMPORARY TABLE IF EXISTS tmp_account_summary;

    
    CREATE TEMPORARY TABLE tmp_scope_subprojects (
        ID INT NOT NULL PRIMARY KEY
    ) ENGINE = MEMORY;

    INSERT INTO tmp_scope_subprojects (ID)
    SELECT sp.ID
    FROM subproject sp
    WHERE p_subproject_ids IS NULL
       OR p_subproject_ids = ''
       OR FIND_IN_SET(
            CONVERT(CAST(sp.ID AS CHAR) USING utf8mb4) COLLATE utf8mb4_unicode_ci,
            CONVERT(p_subproject_ids USING utf8mb4) COLLATE utf8mb4_unicode_ci
       ) > 0;

    
    CREATE TEMPORARY TABLE tmp_scope_accounts (
        ID INT NOT NULL PRIMARY KEY,
        Name VARCHAR(255) NULL,
        SubProject_ID INT NULL,
        KEY idx_tmp_scope_accounts_subproject (SubProject_ID)
    ) ENGINE = MEMORY;

    INSERT INTO tmp_scope_accounts (ID, Name, SubProject_ID)
    SELECT a.ID, a.Name, a.SubProject_ID
    FROM account a
    JOIN tmp_scope_subprojects sp ON sp.ID = a.SubProject_ID
    WHERE p_account_ids IS NULL
       OR p_account_ids = ''
       OR FIND_IN_SET(
            CONVERT(CAST(a.ID AS CHAR) USING utf8mb4) COLLATE utf8mb4_unicode_ci,
            CONVERT(p_account_ids USING utf8mb4) COLLATE utf8mb4_unicode_ci
       ) > 0;

    
    CREATE TEMPORARY TABLE tmp_account_microprojects (
        account_id INT NOT NULL,
        MicroProject_ID INT NOT NULL,
        PRIMARY KEY (account_id, MicroProject_ID),
        KEY idx_tmp_account_microprojects_microproject (MicroProject_ID)
    ) ENGINE = MEMORY;

    INSERT IGNORE INTO tmp_account_microprojects (account_id, MicroProject_ID)
    SELECT DISTINCT b.Account_ID, b.MicroProject_ID
    FROM beneficiary b
    JOIN tmp_scope_accounts sa ON sa.ID = b.Account_ID
    WHERE b.MicroProject_ID IS NOT NULL;

    
    CREATE TEMPORARY TABLE tmp_action_by_account (
        account_id INT NOT NULL PRIMARY KEY,
        total_income_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_income_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_invoices_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_invoices_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_expenses_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_expenses_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_usd_value DECIMAL(20, 4) NOT NULL DEFAULT 0
    ) ENGINE = MEMORY;

    INSERT INTO tmp_action_by_account (
        account_id,
        total_income_syr,
        total_income_usd,
        total_invoices_syr,
        total_invoices_usd,
        total_expenses_syr,
        total_expenses_usd,
        total_usd_value
    )
    SELECT
    sa.ID,
    
    SUM(CASE 
        WHEN atype.shortname = 'I' 
        AND till_type.Name = 'SYR' 
        AND (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '' OR ac.currency_tilltype_id IS NULL) 
        THEN ac.Amount ELSE 0 END) AS total_income_syr,
        
    SUM(CASE 
        WHEN atype.shortname = 'I' 
        AND till_type.Name = 'SYR' 
        AND currency_type.Name = 'USD' 
        THEN ac.Amount ELSE 0 END) AS total_income_usd,
        
    
    SUM(CASE 
        WHEN atype.shortname = 'E' 
        AND till_type.Name = 'SYR' 
        AND (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '' OR ac.currency_tilltype_id IS NULL) 
        THEN ac.Amount ELSE 0 END) AS total_invoices_syr,
        
    SUM(CASE 
        WHEN atype.shortname = 'E' 
        AND till_type.Name = 'SYR' 
        AND currency_type.Name = 'USD' 
        THEN ac.Amount ELSE 0 END) AS total_invoices_usd,
        
    
    SUM(CASE 
        WHEN atype.shortname IN ('O', 'E', 'Ex') 
        AND till_type.Name = 'SYR' 
        AND (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '' OR ac.currency_tilltype_id IS NULL) 
        THEN ac.Amount ELSE 0 END) AS total_expenses_syr,
        
    SUM(CASE 
        WHEN atype.shortname IN ('O', 'E', 'Ex') 
        AND till_type.Name = 'SYR' 
        AND currency_type.Name = 'USD' 
        THEN ac.Amount ELSE 0 END) AS total_expenses_usd,
        
    
    SUM(CASE 
        WHEN till_type.Name = 'SYR' AND (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '' OR ac.currency_tilltype_id IS NULL) 
        THEN ac.Amount / NULLIF(avr.Value, 0)
        WHEN till_type.Name = 'SYR' AND currency_type.Name = 'USD' 
        THEN ac.Amount 
        ELSE 0 
    END) AS total_usd_value
FROM tmp_scope_accounts sa
JOIN till t ON t.Account_ID = sa.ID
JOIN `action` ac ON ac.Till_ID = t.ID
JOIN actiontype atype ON atype.ID = ac.AType_ID
JOIN tilltypes till_type ON till_type.ID = t.Type_ID

LEFT JOIN tilltypes currency_type ON currency_type.ID = ac.currency_tilltype_id
LEFT JOIN all_avgrates avr ON avr.ID = ac.Rate_ID
GROUP BY sa.ID;

    
    CREATE TEMPORARY TABLE tmp_payment_by_account (
        account_id INT NOT NULL PRIMARY KEY,
        total_income_pay_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_income_pay_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_returned_pay_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_returned_pay_usd DECIMAL(20, 4) NOT NULL DEFAULT 0
    ) ENGINE = MEMORY;

    INSERT INTO tmp_payment_by_account (
    account_id,
    total_income_pay_syr,
    total_income_pay_usd,
    total_returned_pay_syr,
    total_returned_pay_usd
)
SELECT
    b.Account_ID,

    
    COALESCE(SUM(CASE WHEN LOWER(tt.Name) = 'syr' THEN COALESCE(a.Amount, 0) ELSE 0 END),0) AS total_income_pay_syr,

    
    COALESCE(SUM(CASE WHEN LOWER(tt.Name) = 'usd' THEN COALESCE(a.Amount, 0) ELSE 0 END),0) AS total_income_pay_usd,

    
    COALESCE(SUM(CASE WHEN LOWER(tt.Name) = 'syr' THEN COALESCE(a.Returned_Amount, 0) ELSE 0 END),0) AS total_returned_pay_syr,

    
    COALESCE(SUM(CASE WHEN LOWER(tt.Name) = 'usd' THEN COALESCE(a.Returned_Amount, 0) ELSE 0 END),0) AS total_returned_pay_usd

FROM tmp_scope_accounts sa
JOIN beneficiary b ON b.Account_ID = sa.ID
LEFT JOIN payment p ON p.Beneficiary_ID = b.ID
LEFT JOIN action_payment ap ON ap.payment_id = p.ID
LEFT JOIN `action` a ON a.ID = ap.action_id
LEFT JOIN tilltypes tt ON tt.ID = b.currency_tilltype_id
GROUP BY b.Account_ID;

    
    DROP TEMPORARY TABLE IF EXISTS tmp_loan_by_account;
    CREATE TEMPORARY TABLE tmp_loan_by_account (
        account_id INT NOT NULL PRIMARY KEY,
        total_loan_amount_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_loan_amount_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        micro_count_syr INT NOT NULL DEFAULT 0,
        micro_count_usd INT NOT NULL DEFAULT 0
    ) ENGINE = MEMORY;

    INSERT INTO tmp_loan_by_account (
        account_id,
        total_loan_amount_syr,
        total_loan_amount_usd,
        micro_count_syr,
        micro_count_usd
    )
    SELECT
        b.Account_ID,
        SUM(CASE WHEN b.currency_tilltype_id = '4' THEN b.Amount ELSE 0 END),
        SUM(CASE WHEN b.currency_tilltype_id = '2' THEN b.Amount ELSE 0 END),
        SUM(CASE WHEN b.currency_tilltype_id = '4' THEN 1 ELSE 0 END),
        SUM(CASE WHEN b.currency_tilltype_id = '2' THEN 1 ELSE 0 END)
    FROM beneficiary b
    JOIN tmp_scope_accounts sa ON sa.ID = b.Account_ID
    GROUP BY b.Account_ID;

    
    CREATE TEMPORARY TABLE tmp_account_summary (
        ID INT NOT NULL PRIMARY KEY,
        Name VARCHAR(255) NULL,
        SubProject_ID INT NULL,
        total_loan_amount_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        average_micro_program_cost_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_income_pay_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_returned_pay_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_invoices_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_income_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        net_account_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        remaining_after_paying_syr DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_loan_amount_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        average_micro_program_cost_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_income_pay_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_returned_pay_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_invoices_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_income_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        net_account_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        remaining_after_paying_usd DECIMAL(20, 4) NOT NULL DEFAULT 0,
        total_micro_projects INT NOT NULL DEFAULT 0,
        micro_count_syr INT NOT NULL DEFAULT 0,
        micro_count_usd INT NOT NULL DEFAULT 0,
        total_usd_value DECIMAL(20, 4) NOT NULL DEFAULT 0,
        KEY idx_tmp_account_summary_subproject (SubProject_ID)
    ) ENGINE = MEMORY;

    INSERT INTO tmp_account_summary (
        ID, Name, SubProject_ID,
        total_loan_amount_syr, average_micro_program_cost_syr, total_income_pay_syr, total_returned_pay_syr,
        total_invoices_syr, total_income_syr, net_account_syr, remaining_after_paying_syr,
        total_loan_amount_usd, average_micro_program_cost_usd, total_income_pay_usd, total_returned_pay_usd,
        total_invoices_usd, total_income_usd, net_account_usd, remaining_after_paying_usd,
        total_micro_projects, micro_count_syr, micro_count_usd, total_usd_value
    )
    SELECT
        sa.ID,
        sa.Name,
        sa.SubProject_ID,
       COALESCE(loan.total_loan_amount_syr, 0),
       COALESCE(loan.total_loan_amount_syr / NULLIF(loan.micro_count_syr, 0), 0),
        COALESCE(pay.total_income_pay_syr, 0),
        COALESCE(pay.total_returned_pay_syr, 0),
        COALESCE(act.total_invoices_syr, 0),
        COALESCE(act.total_income_syr, 0),
        
        COALESCE(act.total_income_syr, 0) - COALESCE(act.total_expenses_syr, 0),
        COALESCE(act.total_income_syr, 0) - COALESCE(act.total_expenses_syr, 0) - COALESCE(pay.total_returned_pay_syr, 0),
        
        COALESCE(loan.total_loan_amount_usd, 0),
        COALESCE(loan.total_loan_amount_usd / NULLIF(loan.micro_count_usd, 0), 0),
        COALESCE(pay.total_income_pay_usd, 0),
        COALESCE(pay.total_returned_pay_usd, 0),
        COALESCE(act.total_invoices_usd, 0),
        COALESCE(act.total_income_usd, 0),
        
        COALESCE(act.total_income_usd, 0) - COALESCE(act.total_expenses_usd, 0),
        COALESCE(act.total_income_usd, 0) - COALESCE(act.total_expenses_usd, 0) - COALESCE(pay.total_returned_pay_usd, 0),
        
        COALESCE(loan.micro_count_syr, 0) + COALESCE(loan.micro_count_usd, 0),
        COALESCE(loan.micro_count_syr, 0),
        COALESCE(loan.micro_count_usd, 0),
        COALESCE(act.total_usd_value, 0)
    FROM tmp_scope_accounts sa
    LEFT JOIN tmp_action_by_account act ON act.account_id = sa.ID
    LEFT JOIN tmp_payment_by_account pay ON pay.account_id = sa.ID
    LEFT JOIN tmp_loan_by_account loan ON loan.account_id = sa.ID;

    
    IF LOWER(COALESCE(p_level, 'account')) IN ('account', 'accounts', 'both') THEN
        SELECT
            ID,
            Name,
            total_income_pay_syr AS `Total income`,
            total_income_pay_usd AS `Total income usd`,
            total_income_syr AS `Total action income`,
            total_income_usd AS `Total action income usd`,
            total_returned_pay_syr AS `Total returned amount`,
            total_returned_pay_usd AS `Total returned amount usd`,
            total_loan_amount_syr AS `Total loans amount`,
            total_loan_amount_usd AS `Total loans amount usd`,
            total_invoices_syr AS `Total invoices amount`,
            total_invoices_usd AS `Total invoices amount usd`,
            net_account_syr AS `Net account`,
            net_account_usd AS `Net account usd`,
            remaining_after_paying_syr AS `Remaining after paying`,
            remaining_after_paying_usd AS `Remaining after paying usd`,
            total_micro_projects AS `Total micro projects`,
            average_micro_program_cost_syr AS `Average micro project cost`,
            average_micro_program_cost_usd AS `Average micro project cost usd`,
            total_usd_value AS `Total USD_Value`,
            total_loan_amount_syr,
            average_micro_program_cost_syr,
            total_income_pay_syr,
            total_returned_pay_syr,
            total_invoices_syr,
            total_income_syr,
            net_account_syr,
            remaining_after_paying_syr,
            total_loan_amount_usd,
            average_micro_program_cost_usd,
            total_income_pay_usd,
            total_returned_pay_usd,
            total_invoices_usd,
            total_income_usd,
            net_account_usd,
            remaining_after_paying_usd
        FROM tmp_account_summary
        ORDER BY ID;
    END IF;

    IF LOWER(COALESCE(p_level, 'account')) IN ('subproject', 'subprojects', 'both') THEN
        SELECT
            sp.ID,
            sp.Name,
            COALESCE(SUM(s.total_income_pay_syr), 0) AS `Total income`,
            COALESCE(SUM(s.total_income_pay_usd), 0) AS `Total income usd`,
            COALESCE(SUM(s.total_income_syr), 0) AS `Total action income`,
            COALESCE(SUM(s.total_income_usd), 0) AS `Total action income usd`,
            COALESCE(SUM(s.total_returned_pay_syr), 0) AS `Total returned amount`,
            COALESCE(SUM(s.total_returned_pay_usd), 0) AS `Total returned amount usd`,
            COALESCE(SUM(s.total_loan_amount_syr), 0) AS `Total loans amount`,
            COALESCE(SUM(s.total_loan_amount_usd), 0) AS `Total loans amount usd`,
            COALESCE(SUM(s.total_invoices_syr), 0) AS `Total invoices amount`,
            COALESCE(SUM(s.total_invoices_usd), 0) AS `Total invoices amount usd`,
            COALESCE(SUM(s.net_account_syr), 0) AS `Net account`,
            COALESCE(SUM(s.net_account_usd), 0) AS `Net account usd`,
            COALESCE(SUM(s.remaining_after_paying_syr), 0) AS `Remaining after paying`,
            COALESCE(SUM(s.remaining_after_paying_usd), 0) AS `Remaining after paying usd`,
            COALESCE(SUM(s.total_micro_projects), 0) AS `Total micro projects`,
            SUM(s.total_loan_amount_syr) / NULLIF(SUM(s.micro_count_syr), 0) AS `Average micro project cost`,
            SUM(s.total_loan_amount_usd) / NULLIF(SUM(s.micro_count_usd), 0) AS `Average micro project cost usd`,
            COALESCE(SUM(s.total_usd_value), 0) AS `Total USD_Value`
        FROM subproject sp
        JOIN tmp_scope_subprojects ssp ON ssp.ID = sp.ID
        LEFT JOIN tmp_account_summary s ON s.SubProject_ID = sp.ID
        GROUP BY sp.ID, sp.Name
        ORDER BY sp.ID;
    END IF;
END
