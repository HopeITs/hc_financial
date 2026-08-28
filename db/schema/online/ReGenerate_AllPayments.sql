CREATE DEFINER=`hcfi`@`%` PROCEDURE `ReGenerate_AllPayments`(IN `p_Beneficiary_ID` INT)
BEGIN
    SET SESSION group_concat_max_len = 1000000;

    SET @Payment_Interval = (
        SELECT beneficiary.Payment_Interval
        FROM beneficiary
        WHERE beneficiary.ID = p_Beneficiary_ID
    );

    SET @Payment_Interval = IFNULL(@Payment_Interval, 1);

    SET @Payment = (
        SELECT beneficiary.Payment
        FROM beneficiary
        WHERE beneficiary.ID = p_Beneficiary_ID
    );

    SET @Residual_Amount = (
        SELECT beneficiary.ToReturn_Amount
            - (
                SELECT IFNULL(SUM(t1.res), 0)
                FROM (
                    SELECT
                        CASE
                            WHEN payment.Amount <= IFNULL(SUM(combined_action.Amount), 0)
                                THEN IFNULL(SUM(combined_action.Amount), 0)
                            ELSE payment.Amount
                        END AS res
                    FROM payment
                    JOIN action_payment ON payment.ID = action_payment.payment_id
                    JOIN (
                        SELECT ID, Amount, Returned_Amount FROM action
                        UNION ALL
                        SELECT ID, Amount, Returned_Amount FROM action_not_approved
                    ) AS combined_action ON combined_action.ID = action_payment.action_id
                    WHERE payment.Beneficiary_ID = p_Beneficiary_ID
                      AND payment.Amount > 0
                    GROUP BY payment.ID, payment.Amount
                ) t1
            )
            + (
                SELECT IFNULL(SUM(t2.res), 0)
                FROM (
                    SELECT IFNULL(SUM(combined_action.Returned_Amount), 0) AS res
                    FROM payment
                    JOIN action_payment ON payment.ID = action_payment.payment_id
                    JOIN (
                        SELECT ID, Amount, Returned_Amount FROM action
                        UNION ALL
                        SELECT ID, Amount, Returned_Amount FROM action_not_approved
                    ) AS combined_action ON combined_action.ID = action_payment.action_id
                    WHERE payment.Beneficiary_ID = p_Beneficiary_ID
                    GROUP BY payment.ID
                ) t2
            )
        FROM beneficiary
        WHERE beneficiary.ID = p_Beneficiary_ID
    );

    SET @Residual_Amount = IFNULL(@Residual_Amount, 0);

    SET @latest_Payment_date = (
        SELECT MAX(payment.DueDate)
        FROM payment
        JOIN action_payment ON action_payment.payment_id = payment.ID
        WHERE payment.Beneficiary_ID = p_Beneficiary_ID
    );

    SET @date = (
        SELECT CASE
            WHEN @latest_Payment_date IS NULL THEN beneficiary.FirstPaymentDate
            WHEN beneficiary.FirstPaymentDate > DATE_ADD(@latest_Payment_date, INTERVAL @Payment_Interval MONTH)
                THEN beneficiary.FirstPaymentDate
            ELSE DATE_ADD(@latest_Payment_date, INTERVAL @Payment_Interval MONTH)
        END
        FROM beneficiary
        WHERE beneficiary.ID = p_Beneficiary_ID
    );

    DELETE payment
    FROM payment
    LEFT JOIN action_payment ON action_payment.payment_id = payment.ID
    WHERE payment.Beneficiary_ID = p_Beneficiary_ID
      AND action_payment.action_id IS NULL;

    WHILE @Residual_Amount > 0 DO
        IF @Residual_Amount > @Payment THEN
            SET @CurrentPayment = @Payment;
        ELSE
            SET @CurrentPayment = @Residual_Amount;
        END IF;

        INSERT INTO payment(Amount, DueDate, Beneficiary_ID, Num)
        VALUES(@CurrentPayment, @date, p_Beneficiary_ID, 0);

        SET @date = DATE_ADD(@date, INTERVAL @Payment_Interval MONTH);
        SET @Residual_Amount = @Residual_Amount - @CurrentPayment;
    END WHILE;

    SET @PaymentNum = 0;
    UPDATE payment
    JOIN (
        SELECT ordered_payment.ID, (@PaymentNum := @PaymentNum + 1) AS NewNum
        FROM (
            SELECT ID
            FROM payment
            WHERE Beneficiary_ID = p_Beneficiary_ID
            ORDER BY COALESCE(DueDate, '9999-12-31'), ID
        ) AS ordered_payment
    ) AS numbered_payment ON numbered_payment.ID = payment.ID
    SET payment.Num = numbered_payment.NewNum
    WHERE payment.Beneficiary_ID = p_Beneficiary_ID;
END
