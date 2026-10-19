-- =====================================================================================
--  P5  --  rename the old lira from 'SYR' to 'SYP' (display name only)
--
--  Touches ONLY the Name text in two lookup tables:
--      tilltypes.Name   'SYR' -> 'SYP'
--      amounttype.name  'SYR' -> 'SYP'
--  Every other table points at these rows by ID (till.Type_ID, action.currency_tilltype_id,
--  account.AmountType_ID ...), so no data moves. is_local_currency / base_divisor are not touched.
--
--  Safe to re-run: when there is no 'SYR' left, every UPDATE changes 0 rows.
--  Take a backup first (or at least run STEP 1 and read it).
-- =====================================================================================


-- ------------------------------------------------------------------ STEP 1: before
SELECT 'tilltypes BEFORE' AS what, ID, Name, is_local_currency, base_divisor FROM tilltypes ORDER BY ID;
SELECT 'amounttype BEFORE' AS what, id, name FROM amounttype ORDER BY id;

-- Both lists must NOT already contain a row called 'SYP' next to a 'SYR' row:
-- Name is UNIQUE, so the UPDATE would fail (nothing is changed if it does).
SELECT 'tilltypes: SYR and SYP both exist (must be 0)' AS check_name,
       (SELECT COUNT(*) FROM tilltypes WHERE Name = 'SYR') > 0
       AND (SELECT COUNT(*) FROM tilltypes WHERE Name = 'SYP') > 0 AS problem;
SELECT 'amounttype: SYR and SYP both exist (must be 0)' AS check_name,
       (SELECT COUNT(*) FROM amounttype WHERE name = 'SYR') > 0
       AND (SELECT COUNT(*) FROM amounttype WHERE name = 'SYP') > 0 AS problem;


-- ------------------------------------------------------------------ STEP 2: rename
START TRANSACTION;

UPDATE tilltypes  SET Name = 'SYP' WHERE Name = 'SYR';
UPDATE amounttype SET name = 'SYP' WHERE name = 'SYR';

COMMIT;


-- ------------------------------------------------------------------ STEP 3: verify
-- 3.1 Names after the change. Expect: EUR, pay, SYP (local, divisor 100), SYPNEW (local, divisor 1), USD.
SELECT 'tilltypes AFTER' AS what, ID, Name, is_local_currency, base_divisor FROM tilltypes ORDER BY ID;
SELECT 'amounttype AFTER' AS what, id, name FROM amounttype ORDER BY id;

-- 3.2 No 'SYR' left in either table (expect 0 and 0).
SELECT (SELECT COUNT(*) FROM tilltypes  WHERE LOWER(Name) = 'syr') AS tilltypes_syr_left,
       (SELECT COUNT(*) FROM amounttype WHERE LOWER(name) = 'syr') AS amounttype_syr_left;

-- 3.3 Exactly one OLD local currency (local, divisor <> 1) and one CURRENT one (local, divisor = 1,
--     not 'pay'). The app finds them by these flags. Expect old = 1, current = 1.
SELECT SUM(is_local_currency = 1 AND base_divisor <> 1)                         AS old_local_currencies,
       SUM(is_local_currency = 1 AND base_divisor = 1 AND LOWER(Name) <> 'pay') AS current_local_currencies
FROM tilltypes;

-- 3.4 The app creates new payment accounts by looking up amounttype by the CURRENT currency's name.
--     Expect one row (no row = new payment accounts would get no amount type).
SELECT 'amounttype row for current currency' AS what, a.id, a.name
FROM amounttype a
JOIN tilltypes t ON LOWER(t.Name) = LOWER(a.name)
WHERE t.is_local_currency = 1 AND t.base_divisor = 1 AND LOWER(t.Name) <> 'pay';

-- 3.5 Any view / procedure / function / trigger / event that still compares with the text 'SYR'.
--     Expect 0 rows, or only 'get_cash_report' (the harmless fallback IFNULL(@old_cur_name,'SYR')).
SELECT 'PROCEDURE/FUNCTION' AS kind, ROUTINE_NAME AS name FROM information_schema.ROUTINES
 WHERE ROUTINE_SCHEMA = DATABASE() AND LOWER(ROUTINE_DEFINITION) REGEXP '[''"]syr[''"]'
UNION ALL
SELECT 'VIEW', TABLE_NAME FROM information_schema.VIEWS
 WHERE TABLE_SCHEMA = DATABASE() AND LOWER(VIEW_DEFINITION) REGEXP '[''"]syr[''"]'
UNION ALL
SELECT 'TRIGGER', TRIGGER_NAME FROM information_schema.TRIGGERS
 WHERE TRIGGER_SCHEMA = DATABASE() AND LOWER(ACTION_STATEMENT) REGEXP '[''"]syr[''"]'
UNION ALL
SELECT 'EVENT', EVENT_NAME FROM information_schema.EVENTS
 WHERE EVENT_SCHEMA = DATABASE() AND LOWER(EVENT_DEFINITION) REGEXP '[''"]syr[''"]';

-- 3.6 Same figures as before the rename: every till still points at a till type that exists
--     (expect 0 orphans). Only names changed, so this must stay 0.
SELECT COUNT(*) AS tills_without_type
FROM till t LEFT JOIN tilltypes tt ON tt.ID = t.Type_ID
WHERE tt.ID IS NULL;

SELECT COUNT(*) AS accounts_with_missing_amounttype
FROM account a LEFT JOIN amounttype am ON am.id = a.AmountType_ID
WHERE a.AmountType_ID IS NOT NULL AND am.id IS NULL;

-- 3.7 Smoke test of the objects the app calls (they must run without error after the rename).
--     Uncomment to run; the first can be slow on a big database.
-- CALL get_cash_report();
-- SELECT COUNT(*) FROM cash_balances;
-- SELECT COUNT(*) FROM all_avgrates;


-- ------------------------------------------------------------------ UNDO (only if needed)
-- UPDATE tilltypes  SET Name = 'SYR' WHERE Name = 'SYP';
-- UPDATE amounttype SET name = 'SYR' WHERE name = 'SYP';
