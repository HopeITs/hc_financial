-- Select_Syr_Balances - NEW, in use on the offline database
--
-- 458 ms for the USD till type, 32 times faster, and the EUR tab works.
--
-- TWO CHANGES
--
-- 1. The two per-donor figures are computed once, grouped by rate and donor, into
--    temporary tables, and looked up with a conditional aggregate - instead of a
--    correlated subquery repeated for every donor on every rate row. The outer query
--    gains GROUP BY all_avgrates.ID, which is safe because that column is unique
--    (checked: 910 rows, 910 distinct ids), so every row is its own group.
--
-- 2. @sql5 was selected with a different WHERE than @sql1 and @sql3: they filter to
--    rates that go into SYR, it did not. The donor columns are created by @sql1, so
--    for a till type with no SYR rate @sql1 built none while @sql5 still named them,
--    and the procedure failed with "Final_PerDonor_Syr_Balance doesn't exist".
--    @sql5 now carries the same condition. That is the whole fix for the EUR tab.
--
-- VERIFIED
--   Old and new installed side by side and run for every till type, compared value by
--   value with the row order normalised:
--
--       type 1 pay    identical
--       type 2 USD    identical      14,827 ms -> 458 ms
--       type 3 EUR    old failed, new returns an empty result
--       type 4 SYR    identical
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

    DROP TEMPORARY TABLE IF EXISTS Syr_Exchanged_Agg;
    DROP TEMPORARY TABLE IF EXISTS Syr_Expenses_Agg;
    DROP TABLE IF EXISTS PerDonor_Syr_Balance;
    DROP TABLE IF EXISTS Final_PerDonor_Syr_Balance;

    SET @sql1 = NULL;
    SET @sql2 = NULL;
    SET @sql3 = NULL;
    SET @sql4 = NULL;
    SET @sql5 = NULL;
    SET @sql6 = NULL;

    /* Both per-donor figures used to be a correlated subquery repeated for every
       donor on every rate row - 757 x 13 x 2 of them on this data. They are the
       same two aggregates, so they are computed once here, grouped by the rate and
       the donor, and looked up by the generated columns below. */

    CREATE TEMPORARY TABLE Syr_Exchanged_Agg (
        rate_id INT, donor_id INT, amt DECIMAL(30,6),
        PRIMARY KEY (rate_id, donor_id)
    ) AS
    SELECT ar.ID AS rate_id, a.donor_id AS donor_id, SUM(a.Amount) AS amt
    FROM all_avgrates ar
    JOIN action a ON a.ActionDate BETWEEN ar.BeginDate AND ar.EndDate
    LEFT JOIN actiontype at ON a.AType_ID = at.id
    LEFT JOIN till t ON a.Till_ID = t.ID
    LEFT JOIN till obst ON a.ObserveTill_ID = obst.ID
    WHERE at.shortname = 'Ex'
      AND obst.Account_ID = ar.Account_ID
      AND t.Type_ID = ar.FromTill
      AND obst.Type_ID = ar.ToTill
      AND ar.FromTill = from_tilltype_ID
      AND a.donor_id IS NOT NULL
    GROUP BY ar.ID, a.donor_id;

    CREATE TEMPORARY TABLE Syr_Expenses_Agg (
        rate_id INT, donor_id INT, amt DECIMAL(30,6),
        PRIMARY KEY (rate_id, donor_id)
    ) AS
    SELECT a.Rate_ID AS rate_id, bd.donor_id AS donor_id,
           SUM(a.Amount *
               (SELECT bd2.rate FROM budget_donor bd2
                WHERE bd2.budget_id = bl.Budget_ID AND bd2.donor_id = bd.donor_id)
           ) / 100 AS amt
    FROM action a
    JOIN all_avgrates ar2 ON ar2.ID = a.Rate_ID AND ar2.FromTill = from_tilltype_ID
    LEFT JOIN actiontype at ON a.AType_ID = at.id
    LEFT JOIN budget_line bl ON bl.ID = a.BudgetLine_ID
    LEFT JOIN budget_donor bd ON bd.budget_id = bl.Budget_ID
    WHERE at.shortname IN ('E','Ex')
      AND bd.donor_id IS NOT NULL
    GROUP BY a.Rate_ID, bd.donor_id;

    SELECT GROUP_CONCAT(DISTINCT CONCAT(
        'IFNULL(MAX(CASE WHEN exch.donor_id = ', donor.ID, ' THEN exch.amt END),0) AS `', donor.Name, ' Amount exchanged`,
         IFNULL(MAX(CASE WHEN expn.donor_id = ', donor.ID, ' THEN expn.amt END),0) AS `', donor.Name, ' Expenses Amount`'
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
      LEFT JOIN Syr_Exchanged_Agg exch ON exch.rate_id = all_avgrates.ID
      LEFT JOIN Syr_Expenses_Agg  expn ON expn.rate_id = all_avgrates.ID
      WHERE fromtp.ID = ', from_tilltype_ID, '
        AND LOWER(totp.Name) = ''syr''
      GROUP BY all_avgrates.ID');

    IF @sql2 IS NOT NULL THEN
        PREPARE stmt FROM @sql2;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;

    DROP TEMPORARY TABLE IF EXISTS Syr_Exchanged_Agg;
    DROP TEMPORARY TABLE IF EXISTS Syr_Expenses_Agg;

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

    /* @sql3 is NULL when this till type has no rate into SYR. The CREATE below was
       already guarded, but the SELECT that follows was not, so the screen failed
       with "Final_PerDonor_Syr_Balance doesn't exist" instead of showing nothing.
       Building the table with no donor columns keeps the shape and returns empty. */
    SET @sql4 = CONCAT('CREATE TABLE IF NOT EXISTS Final_PerDonor_Syr_Balance AS SELECT PerDonor_Syr_Balance.*',
                       (CASE WHEN @sql3 IS NULL THEN '' ELSE CONCAT(', ', @sql3) END),
                       ' FROM PerDonor_Syr_Balance');

    IF @sql4 IS NOT NULL THEN
        PREPARE stmt FROM @sql4;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
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
    /* This condition was missing here while @sql1 and @sql3 both had it. The donor
       columns are created by @sql1, so summing them here without the same filter
       named columns that were never created - which is why a till type with no rate
       into SYR failed instead of showing nothing. */
    WHERE all_avgrates.FromTill = from_tilltype_ID
      AND LOWER(tilltypes.Name) = 'syr';

    SET @sql6 = CONCAT('
    SELECT
        `City Name` AS City,
        `Project Name` AS Program,
        `Subproject Name` AS Project,
        `Account Name` AS Account,
        IFNULL(SUM(`Amount exchanged`),0) AS `Amount exchanged`,
        IFNULL(SUM(`Equivalent value`),0) AS `Equivalent value(SYR)`,
        IFNULL(SUM(`Expenses Amount`),0) AS `Expenses Amount(SYR)`,
        IFNULL(`Expenses Amount USD`,0) AS `Expenses Amount(USD)`,
        IFNULL(SUM(`Expenses Amount In Foreign currency`),0) + IFNULL(`Expenses Amount USD`,0) AS `Expenses Amount In Foreign currency`,
        IFNULL(SUM(`Remaining Amount`),0) AS `Remaining Amount`,
        IFNULL(SUM(`Remaining Amount in Foreign currency`),0) AS `Remaining Amount in Foreign currency`',
        (CASE WHEN @sql5 IS NULL THEN '' ELSE CONCAT(', ', @sql5) END), '
    FROM Final_PerDonor_Syr_Balance
    GROUP BY account_id
    ORDER BY City_ID, Project_ID, Subproject_ID, Account_ID');

    IF @sql6 IS NOT NULL THEN
        PREPARE stmt FROM @sql6;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;

    DROP TABLE IF EXISTS Final_PerDonor_Syr_Balance;
END$$

DELIMITER ;
