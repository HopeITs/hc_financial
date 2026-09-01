-- Select_Syr_Balances - OLD, the slow one
--
-- 14,827 ms for the USD till type; the other three were already fast because they
-- have no rate into SYR.
--
-- Each donor column carried two correlated subqueries over the action table, one per
-- donor per rate row: 757 rate rows x 13 donors x 2 is about 19,700 of them.
--
-- It also fails outright for a till type that has no rate into SYR - see the note in
-- the NEW file - so the EUR tab has never worked.
--
-- Run this only to go back to the old behaviour.
--
-- DEFINER is `hcfi`@`%`, the same account the online server uses.
-- Setting a DEFINER other than yourself needs SUPER. If you do not have it, delete
-- the DEFINER= clause and the procedure is created as whoever runs the file.
--
-- The body is as the server returned it; only the parts described above changed.

DROP PROCEDURE IF EXISTS `Select_Syr_Balances`;

DELIMITER $$

CREATE DEFINER=`hcfi`@`%` PROCEDURE `Select_Syr_Balances`(IN `from_tilltype_ID` INT)
BEGIN
    SET SESSION group_concat_max_len = 100000000;

    DROP TABLE IF EXISTS PerDonor_Syr_Balance;
    DROP TABLE IF EXISTS Final_PerDonor_Syr_Balance;

    SET @sql1 = NULL;
    SET @sql2 = NULL;
    SET @sql3 = NULL;
    SET @sql4 = NULL;
    SET @sql5 = NULL;
    SET @sql6 = NULL;

    SELECT GROUP_CONCAT(DISTINCT CONCAT(
        'IFNULL(
            (SELECT SUM(a.Amount)
             FROM action a
             LEFT JOIN actiontype at ON a.AType_ID = at.id
             LEFT JOIN till t ON a.Till_ID = t.ID
             LEFT JOIN till obst ON a.ObserveTill_ID = obst.ID
             WHERE at.shortname = ''Ex''
               AND obst.Account_ID = all_avgrates.Account_ID
               AND t.Type_ID = all_avgrates.FromTill
               AND obst.Type_ID = all_avgrates.ToTill
               AND a.ActionDate BETWEEN all_avgrates.BeginDate AND all_avgrates.EndDate
               AND a.donor_id = ', donor.ID, '
            ),0) AS `', donor.Name, ' Amount exchanged`,
        IFNULL(
            (SELECT SUM(a.Amount * 
                         (SELECT bd.rate 
                          FROM budget_donor bd 
                          WHERE bd.budget_id = bl.Budget_ID AND bd.donor_id = ', donor.ID, ')
                        )/100
             FROM action a
             LEFT JOIN actiontype at ON a.AType_ID = at.id
             LEFT JOIN budget_line bl ON bl.ID = a.BudgetLine_ID
             LEFT JOIN budget_donor bd ON bd.budget_id = bl.Budget_ID
             WHERE at.shortname IN (''E'',''Ex'') 
               AND a.Rate_ID = all_avgrates.ID
               AND bd.donor_id = ', donor.ID, '
            ),0) AS `', donor.Name, ' Expenses Amount`'
    ) ORDER BY donor.id) INTO @sql1
    FROM account_donor 
    JOIN donor ON account_donor.Donor_id = donor.ID
    LEFT JOIN account ON account.ID = account_donor.Account_id
    LEFT JOIN all_avgrates ON all_avgrates.Account_ID = account.ID
    LEFT JOIN tilltypes ON tilltypes.ID = all_avgrates.ToTill
    WHERE all_avgrates.FromTill = from_tilltype_ID 
      AND LOWER(tilltypes.Name) = 'syr';

    SET @sql2 = CONCAT('
    CREATE TABLE IF NOT EXISTS PerDonor_Syr_Balance AS
    SELECT
        all_avgrates.City_ID AS City_ID,
        all_avgrates.City_Name AS `City Name`,
        all_avgrates.Project_ID AS Project_ID,
        all_avgrates.Project_Name AS `Project Name`,
        all_avgrates.Subproject_ID AS Subproject_ID,
        all_avgrates.Subproject_Name AS `Subproject Name`,
        all_avgrates.Account_Name AS `Account Name`,
        all_avgrates.ID AS ID,
        all_avgrates.ObserveRate_ID AS ObserveRate_ID,
        all_avgrates.Name AS `Rate group name`,
        all_avgrates.BeginDate AS `Start date`,
        all_avgrates.EndDate AS `End date`,
        fromtp.name AS `From till`,
        totp.name AS `To till`,
        fromtp.id AS fromtp_id,
        totp.id AS totp_id,
        all_avgrates.Account_ID,
        all_avgrates.Value AS Value,
        all_avgrates.Exchanged AS `Amount exchanged`,
        all_avgrates.Exchanged * all_avgrates.Value AS `Equivalent value`, 
        all_avgrates.Expenditures AS `Expenses Amount`,
        all_avgrates.Expenditures_USD AS `Expenses Amount USD`,
        IF(all_avgrates.Value=0,0,all_avgrates.Expenditures/all_avgrates.Value) AS `Expenses Amount In Foreign currency`,
        IFNULL((all_avgrates.Exchanged * all_avgrates.Value) - all_avgrates.Expenditures,0) AS `Remaining Amount`,
        IF(all_avgrates.Value=0,0, ((all_avgrates.Exchanged*all_avgrates.Value)-all_avgrates.Expenditures)/all_avgrates.Value) AS `Remaining Amount in Foreign currency`,
		
        all_avgrates.ObserveRate AS `The Corresponding Rate Group`,
        obs_all_avgrates.Value AS `The Corresponding Rate Value`,
        IF(obs_all_avgrates.Value=0,0, all_avgrates.Exchanged/obs_all_avgrates.Value) AS `The Corresponding Amount`',
        (CASE WHEN @sql1 IS NULL THEN '' ELSE CONCAT(',',@sql1) END),
    ' FROM all_avgrates
      LEFT JOIN all_avgrates obs_all_avgrates ON obs_all_avgrates.ID = all_avgrates.ObserveRate_ID
      LEFT JOIN tilltypes fromtp ON all_avgrates.FromTill = fromtp.ID
      LEFT JOIN tilltypes totp ON all_avgrates.ToTill = totp.ID
      WHERE fromtp.ID = ', from_tilltype_ID, ' 
        AND LOWER(totp.Name) = ''syr''');

    IF @sql2 IS NOT NULL THEN
        PREPARE stmt FROM @sql2;
        EXECUTE stmt;
    END IF;

    SELECT GROUP_CONCAT(DISTINCT CONCAT(
        'PerDonor_Syr_Balance.`', donor.Name, ' Amount exchanged` * PerDonor_Syr_Balance.Value AS `', donor.Name, ' Equivalent value`,
         IF(PerDonor_Syr_Balance.Value=0,0, PerDonor_Syr_Balance.`', donor.Name, ' Expenses Amount`/PerDonor_Syr_Balance.Value) AS `', donor.Name, ' Expenses Amount In Foreign currency`,
         IFNULL((PerDonor_Syr_Balance.`', donor.Name, ' Amount exchanged`*PerDonor_Syr_Balance.Value)-PerDonor_Syr_Balance.`', donor.Name, ' Expenses Amount`,0) AS `', donor.Name, ' Remaining Amount`,
         IF(PerDonor_Syr_Balance.Value=0,0, ((PerDonor_Syr_Balance.`', donor.Name, ' Amount exchanged`*PerDonor_Syr_Balance.Value)-PerDonor_Syr_Balance.`', donor.Name, ' Expenses Amount`)/PerDonor_Syr_Balance.Value) AS `', donor.Name, ' Remaining Amount in Foreign currency`,
         IF(PerDonor_Syr_Balance.`The Corresponding Rate Value`=0,0,PerDonor_Syr_Balance.`', donor.Name, ' Amount exchanged`/PerDonor_Syr_Balance.`The Corresponding Rate Value`) AS `', donor.Name, ' The Corresponding Amount`'
    ) ORDER BY donor.id) INTO @sql3
    FROM account_donor
    JOIN donor ON account_donor.Donor_id = donor.ID
    LEFT JOIN account ON account.ID = account_donor.Account_id
    LEFT JOIN all_avgrates ON all_avgrates.Account_ID = account.ID
    LEFT JOIN tilltypes ON tilltypes.ID = all_avgrates.ToTill
    WHERE all_avgrates.FromTill = from_tilltype_ID AND LOWER(tilltypes.Name) = 'syr';

    SET @sql4 = CONCAT('CREATE TABLE IF NOT EXISTS Final_PerDonor_Syr_Balance AS SELECT PerDonor_Syr_Balance.*, ', @sql3, ' FROM PerDonor_Syr_Balance');

    IF @sql4 IS NOT NULL THEN
        PREPARE stmt FROM @sql4;
        EXECUTE stmt;
    END IF;

    DROP TABLE IF EXISTS PerDonor_Syr_Balance;

    SELECT GROUP_CONCAT(DISTINCT CONCAT(
        'IFNULL(SUM(`', donor.Name, ' Amount exchanged`),0) AS `', donor.Name, ' Amount exchanged`,
         IFNULL(SUM(`', donor.Name, ' Expenses Amount`),0) AS `', donor.Name, ' Expenses Amount`,
         IFNULL(SUM(`', donor.Name, ' Equivalent value`),0) AS `', donor.Name, ' Equivalent value`,
         IFNULL(SUM(`', donor.Name, ' Expenses Amount In Foreign currency`),0) AS `', donor.Name, ' Expenses Amount In Foreign currency`,
         IFNULL(SUM(`', donor.Name, ' Remaining Amount`),0) AS `', donor.Name, ' Remaining Amount`,
         IFNULL(SUM(`', donor.Name, ' Remaining Amount in Foreign currency`),0) AS `', donor.Name, ' Remaining Amount in Foreign currency`'
    ) ORDER BY donor.id) INTO @sql5
    FROM account_donor
    JOIN donor ON account_donor.Donor_id = donor.ID
    LEFT JOIN account ON account.ID = account_donor.Account_id
    LEFT JOIN all_avgrates ON all_avgrates.Account_ID = account.ID
    LEFT JOIN tilltypes ON tilltypes.ID = all_avgrates.ToTill
    WHERE all_avgrates.FromTill = from_tilltype_ID;

    SET @sql6 = CONCAT('
    SELECT
        `City Name` AS City,
        `Project Name` AS Program,
        `Subproject Name` AS Project,
        `Account Name` AS Account,
        IFNULL(SUM(`Amount exchanged`),0) AS `Amount exchanged`,
        IFNULL(SUM(`Equivalent value`),0) AS `Equivalent value(SYR)`,
        IFNULL(SUM(`Expenses Amount`),0) AS `Expenses Amount(SYR)`,
		
		-- edited by Judy
        IFNULL(`Expenses Amount USD`,0) AS `Expenses Amount(USD)`,
        -- edited by Judy
		IFNULL(SUM(`Expenses Amount In Foreign currency`),0) + IFNULL(`Expenses Amount USD`,0) AS `Expenses Amount In Foreign currency`,
        
		IFNULL(SUM(`Remaining Amount`),0) AS `Remaining Amount`,
        IFNULL(SUM(`Remaining Amount in Foreign currency`),0) AS `Remaining Amount in Foreign currency`, ', @sql5, '
    FROM Final_PerDonor_Syr_Balance
    GROUP BY account_id
    ORDER BY City_ID, Project_ID, Subproject_ID, Account_ID
    ');

    IF @sql6 IS NOT NULL THEN
        PREPARE stmt FROM @sql6;
        EXECUTE stmt;
    END IF;

    DROP TABLE IF EXISTS Final_PerDonor_Syr_Balance;

    SET @sql1 = NULL; SET @sql2 = NULL; SET @sql3 = NULL; 
    SET @sql4 = NULL; SET @sql5 = NULL; SET @sql6 = NULL;
END$$

DELIMITER ;
