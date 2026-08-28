CREATE DEFINER=`hcfi`@`%` PROCEDURE `ReschedulePayments`(
    IN p_Beneficiary_ID INT,
    IN p_ActionDate DATE
)
BEGIN
    DECLARE v_interval INT;
    DECLARE v_base DATE;

    
    SELECT Payment_Interval
    INTO v_interval
    FROM beneficiary
    WHERE ID = p_Beneficiary_ID
    LIMIT 1;

    
    IF v_interval IS NULL THEN
        SET v_interval = 1; 
    END IF;

    
    SET v_base = p_ActionDate;

    
    UPDATE payment p
    JOIN (
        SELECT ID, ROW_NUMBER() OVER (ORDER BY DueDate) AS rn
        FROM payment
        WHERE Beneficiary_ID = p_Beneficiary_ID
        ORDER BY DueDate
    ) t ON p.ID = t.ID
    SET p.DueDate = DATE_ADD(v_base, INTERVAL (t.rn - 1) * v_interval MONTH);

END
