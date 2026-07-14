using Financial_WPF_App.Properties;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Financial_WPF_App.Models
{
    public class Payment
    {
        public DataTable Select_Beneficiary_Payments(string Beneficiary_ID, string Subproject_ID)
        {

            string query = @"SELECT 
                                payment.ID, 
                                payment.Amount, 
                                payment.DueDate, 
                                payment.Beneficiary_ID, 
                                payment.Num,
                                GROUP_CONCAT(DATE_FORMAT(combined_action.ActionDate, '%Y-%m-%d')) AS 'action_date',
                                -- Net Paid Amount = sum(Amount) - sum(Returned_Amount)
                                IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0) AS 'Paid_Amount',
                                CASE 
                                    WHEN (payment.Amount) > 0 THEN SUM(combined_action.Amount)
                                    WHEN (payment.Amount) < 0 THEN -SUM(combined_action.Returned_Amount)
                                END AS 'Amount_Paid',
                                -- Payment state based on net paid amount
                                CASE 
                                    WHEN (payment.Amount) < 0 THEN 'Returned'
                                    WHEN (IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0)) <= 0 THEN 'Unpaid'
                                    WHEN (IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0)) < payment.Amount THEN 'Incomplete'
                                    
                                    ELSE 'Done'
                                END AS 'payment_state',
                            
                                -- Approval status
                                CASE 
                                    WHEN MAX(combined_action.Source) = 'action_not_approved' THEN 'No'
                                    WHEN MAX(combined_action.Source) = 'action' THEN 'Yes'
                                    WHEN MAX(combined_action.Source) = 'action_deleted' AND MAX(combined_action.IsApproved) =1 THEN 'Yes'
                                    WHEN MAX(combined_action.Source) = 'action_deleted' AND MAX(combined_action.IsApproved) =0 THEN 'No'
                                END AS 'Approved'
                            
                            FROM 
                                payment 
                            LEFT JOIN action_payment ON payment.id = action_payment.payment_id
                            LEFT JOIN (
                                SELECT *, 'action' AS Source FROM action
                                UNION ALL
                                SELECT *, 'action_not_approved' AS Source FROM action_not_approved
                            ) AS combined_action 
                                ON combined_action.ID = action_payment.action_id
                            LEFT JOIN beneficiary ON payment.beneficiary_id = beneficiary.id
                            LEFT JOIN account ON beneficiary.account_id = account.id
                            WHERE 1";

            if (Beneficiary_ID != "" && Beneficiary_ID != "-1")
            {
                query += " and payment.Beneficiary_ID = @Beneficiary_ID";
            }
            if (Subproject_ID != "" && Subproject_ID != "-1")
            {
                query += " and account.Subproject_ID = @Subproject_ID";
            }
            query += " GROUP BY payment.ID ORDER BY payment.Num, payment.DueDate, payment.ID;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();


                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                sc.Parameters.AddWithValue("@Subproject_ID", Subproject_ID);

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        private bool IsPayablePayment(DataRow row)
        {
            string paymentState = row["payment_state"]?.ToString();
            return paymentState == "Unpaid" || paymentState == "Incomplete";
        }

        private DateTime GetPaymentDueDate(DataRow row)
        {
            return row["DueDate"] == DBNull.Value ? DateTime.MaxValue : Convert.ToDateTime(row["DueDate"]);
        }

        private DataTable SelectPayablePaymentsForUpdate(MySqlConnection connection, int Beneficiary_ID)
        {
            string query = @"SELECT 
                                payment.ID,
                                payment.Amount,
                                payment.DueDate,
                                payment.Beneficiary_ID,
                                payment.Num,
                                IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0) AS 'Paid_Amount',
                                CASE
                                    WHEN payment.Amount < 0 THEN 'Returned'
                                    WHEN (IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0)) <= 0 THEN 'Unpaid'
                                    WHEN (IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0)) < payment.Amount THEN 'Incomplete'
                                    ELSE 'Done'
                                END AS 'payment_state'
                            FROM payment
                            LEFT JOIN action_payment ON payment.ID = action_payment.payment_id
                            LEFT JOIN (
                                SELECT ID, Amount, Returned_Amount FROM action
                                UNION ALL
                                SELECT ID, Amount, Returned_Amount FROM action_not_approved
                            ) AS combined_action ON combined_action.ID = action_payment.action_id
                            WHERE payment.Beneficiary_ID = @Beneficiary_ID
                            GROUP BY payment.ID
                            HAVING payment_state IN ('Unpaid', 'Incomplete')
                            ORDER BY payment.Num, payment.DueDate, payment.ID
                            FOR UPDATE;";

            DataTable dt = new DataTable();
            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                cmd.CommandTimeout = 120;
                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            return dt;
        }

        private DataTable SelectInstallmentPaymentActionsForReallocation(MySqlConnection connection, int Beneficiary_ID)
        {
            string query = @"SELECT active_action.ID,
                                active_action.Amount,
                                active_action.ActionDate
                            FROM (
                                SELECT ID, Amount, Returned_Amount, ActionDate, MicroProject_ID, AType_ID
                                FROM action
                                UNION ALL
                                SELECT ID, Amount, Returned_Amount, ActionDate, MicroProject_ID, AType_ID
                                FROM action_not_approved
                            ) AS active_action
                            JOIN actiontype ON actiontype.ID = active_action.AType_ID
                            JOIN beneficiary ON beneficiary.MicroProject_ID = active_action.MicroProject_ID
                            WHERE beneficiary.ID = @Beneficiary_ID
                            AND actiontype.shortname = 'I'
                            AND active_action.Amount > 0
                            AND IFNULL(active_action.Returned_Amount, 0) = 0
                            ORDER BY active_action.ActionDate, active_action.ID;";

            DataTable dt = new DataTable();
            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                cmd.CommandTimeout = 120;
                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            return dt;
        }

        private void DeleteInstallmentPaymentAllocations(MySqlConnection connection, int Beneficiary_ID)
        {
            string query = @"DELETE ap
                            FROM action_payment ap
                            JOIN payment ON payment.ID = ap.payment_id
                            JOIN (
                                SELECT ID, MicroProject_ID, AType_ID FROM action
                                UNION ALL
                                SELECT ID, MicroProject_ID, AType_ID FROM action_not_approved
                            ) AS active_action ON active_action.ID = ap.action_id
                            JOIN actiontype ON actiontype.ID = active_action.AType_ID
                            JOIN beneficiary ON beneficiary.MicroProject_ID = active_action.MicroProject_ID
                            WHERE beneficiary.ID = @Beneficiary_ID
                            AND payment.Beneficiary_ID = @Beneficiary_ID
                            AND actiontype.shortname = 'I';";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }
        }

        private int InsertInstallmentPaymentAllocation(MySqlConnection connection, int Action_ID, int Beneficiary_ID, double Amount)
        {
            DataTable currentPayablePayments = SelectPayablePaymentsForUpdate(connection, Beneficiary_ID);

            DataRow payablePayment = currentPayablePayments.AsEnumerable()
                .Where(IsPayablePayment)
                .OrderBy(r => Convert.ToInt32(r["Num"]))
                .ThenBy(GetPaymentDueDate)
                .ThenBy(r => Convert.ToInt32(r["ID"]))
                .FirstOrDefault();

            if (payablePayment == null)
                return 0;

            double scheduled = Convert.ToDouble(payablePayment["Amount"]);
            double paidAmount = Convert.ToDouble(payablePayment["Paid_Amount"]);
            double need = Math.Max(0, scheduled - paidAmount);

            if (need <= 0 || Amount > need)
                return 0;

            using (MySqlCommand cmd = new MySqlCommand("INSERT INTO action_payment(action_id, payment_id) VALUES(@Action_ID, @Payment_ID);", connection))
            {
                cmd.Parameters.AddWithValue("@Action_ID", Action_ID);
                cmd.Parameters.AddWithValue("@Payment_ID", Convert.ToInt32(payablePayment["ID"]));
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }

            return 1;
        }

        private int ReallocateInstallmentPaymentActions(MySqlConnection connection, int Beneficiary_ID)
        {
            DeleteInstallmentPaymentAllocations(connection, Beneficiary_ID);

            int allocations = 0;
            DataTable actions = SelectInstallmentPaymentActionsForReallocation(connection, Beneficiary_ID);
            foreach (DataRow action in actions.Rows)
            {
                int actionId = Convert.ToInt32(action["ID"]);
                double amount = Convert.ToDouble(action["Amount"]);
                int inserted = InsertInstallmentPaymentAllocation(connection, actionId, Beneficiary_ID, amount);
                if (inserted == 0)
                    throw new Exception("Cannot reallocate payment action " + actionId + " because no matching payable installment can accept the full action amount.");

                allocations += inserted;
            }

            return allocations;
        }

        private int ReallocateInstallmentPaymentActions(int Beneficiary_ID)
        {
            if (!Login.BuildConnection())
                return 0;

            try
            {
                using (MySqlCommand cmd = new MySqlCommand("START TRANSACTION;", Login.MyConn))
                {
                    cmd.CommandTimeout = 120;
                    cmd.ExecuteNonQuery();
                }

                int allocations = ReallocateInstallmentPaymentActions(Login.MyConn, Beneficiary_ID);

                using (MySqlCommand cmd = new MySqlCommand("COMMIT;", Login.MyConn))
                {
                    cmd.CommandTimeout = 120;
                    cmd.ExecuteNonQuery();
                }

                return allocations;
            }
            catch
            {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand("ROLLBACK;", Login.MyConn))
                    {
                        cmd.CommandTimeout = 120;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }

                throw;
            }
            finally
            {
                Login.CloseAndClearConnection();
            }
        }

        public DataTable Select_All_Payments(string SubProject_ID, int Action_ID, string FromDate, string ToDate, int Microproject_ID)
        {
            string query = @"SELECT  subproject.ID as 'SubProjectID', subproject.Name as 'SubProjectName',
                 account.ID as 'AccountID', account.Name as 'AccountName', currency_tilltype.Name as 'Currency',
                 action.`ID` as 'ID' ,action.`Till_ID`, actiontype.`Name` as 'AType',action.Amount as 'Income',
                (CASE WHEN Lower(currency_tilltype.Name) = 'syr' THEN action.Amount
                ELSE 0 END) AS 'Incomesyr',
                (CASE WHEN Lower(currency_tilltype.Name) = 'usd' THEN action.Amount
                ELSE 0 END) AS 'Incomeusd',action.Returned_Amount as 'Outcome',
                (CASE WHEN Lower(currency_tilltype.Name) = 'syr' THEN action.Returned_Amount
                ELSE 0 END) AS 'Outcomesyr',
                (CASE WHEN Lower(currency_tilltype.Name) = 'usd' THEN action.Returned_Amount
                ELSE 0 END) AS 'Outcomeusd',
                action.`note` as 'Note',
                 action.`MicroProject_ID` ,action.`ActionDate`, DATE_FORMAT(action.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action.ActionDate, ""%Y"") as 'ActionYear', 
                 action.`IsApproved`, approved_byuser.`Username` as 'approved_by', action.`Approved_at`, created_byuser.`Username` as 'created_by', edited_byuser.username as 'edited_by',
                 action.`cashaccount_id` as 'cashaccount_id', cashaccount.`name` as 'cashaccount_name',
                (select DISTINCT payment.Num from payment join action_payment on action_payment.payment_id = payment.id where action_payment.action_id = action.`ID`) AS 'Payment_Num',

                 (select CONCAT(`FirstName`,' ',`FatherName`, ' ',`LastName`) from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'Name',

                 (select beneficiary.ID from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_ID',

                 (select beneficiary.Account_ID from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_Account_ID',

                 (select account.name from beneficiary left join account on beneficiary.Account_ID = account.id
                 where beneficiary.MicroProject_ID = action.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_Account',

                 action.created_at, action.updated_at, action.`SourceAction`,
                 donor.`ID` AS 'beneficiaryDonor_ID', donor.`Name` AS 'beneficiaryDonor_Name' 

                 FROM `action` LEFT JOIN actiontype on actiontype.id = action.atype_id
                 LEFT join `till` on action.Till_ID = till.ID 
                 LEFT join `tilltypes` on tilltypes.ID = till.Type_ID
                 LEFT JOIN `tilltypes` as currency_tilltype on currency_tilltype.id = action.currency_tilltype_id
                 LEFT join `account` on account.ID = till.Account_ID
                 LEFT join `subproject` on account.SubProject_ID = subproject.ID
                 LEFT join `cashaccount` on action.cashaccount_id = cashaccount.id
                 LEFT JOIN `account_donor` ON account_donor.account_ID = account.ID
                 LEFT JOIN donor on donor.id = account_donor.donor_id
                 LEFT JOIN `user` approved_byuser on approved_byuser.id = action.Approved_by
                 LEFT JOIN `user` created_byuser on created_byuser.id = action.User_ID
                 LEFT JOIN `user` edited_byuser on edited_byuser.id = edited_by";

            string condition = " where tilltypes.Name = 'Pay' and account.SubProject_ID in (" + SubProject_ID + ") ";

            if (Action_ID != -1)
            {
                condition += " AND action.id = @Action_ID";
            }
            if (FromDate != "")
            {
                condition += " AND action.`ActionDate` >= @FromDate";
            }
            if (ToDate != "")
            {
                condition += " AND action.`ActionDate` <= @ToDate";
            }
            if (Microproject_ID != -1)
            {
                condition += " AND action.`MicroProject_ID` = @Microproject_ID";
            }
            condition += " order by action.id";
            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Microproject_ID", Microproject_ID);
                sc.Parameters.AddWithValue("@FromDate", FromDate);
                sc.Parameters.AddWithValue("@ToDate", ToDate);


                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_All_Not_Approved_Payments(string SubProject_ID, bool ForApproval, int Action_ID, string FromDate, string ToDate, int Microproject_ID)
        {
            string query = @"SELECT subproject.ID as 'SubProjectID', subproject.Name as 'SubProjectName',
                 account.ID as 'AccountID', account.Name as 'AccountName',currency_tilltype.Name as 'Currency',
                 action_not_approved.`ID` as 'ID' ,action_not_approved.`Till_ID`, actiontype.`Name` as 'AType',
                action_not_approved.Amount as 'Income',
                (CASE WHEN Lower(currency_tilltype.Name) = 'syr' THEN action_not_approved.Amount
                ELSE 0 END) AS 'Incomesyr',
                (CASE WHEN Lower(currency_tilltype.Name) = 'usd' THEN action_not_approved.Amount
                ELSE 0 END) AS 'Incomeusd',action_not_approved.Returned_Amount as 'Outcome',
                (CASE WHEN Lower(currency_tilltype.Name) = 'syr' THEN action_not_approved.Returned_Amount
                ELSE 0 END) AS 'Outcomesyr',
                (CASE WHEN Lower(currency_tilltype.Name) = 'usd' THEN action_not_approved.Returned_Amount
                ELSE 0 END) AS 'Outcomeusd',

                 action_not_approved.`note` as 'Note',
                 action_not_approved.`MicroProject_ID` ,action_not_approved.`ActionDate`, DATE_FORMAT(action_not_approved.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action_not_approved.ActionDate, ""%Y"") as 'ActionYear', 
                 action_not_approved.`IsApproved`, approved_byuser.`Username` as 'approved_by', action_not_approved.`Approved_at`, created_byuser.`Username` as 'created_by', edited_byuser.username as 'edited_by',
                 action_not_approved.`cashaccount_id` as 'cashaccount_id', cashaccount.`name` as 'cashaccount_name',

                 (select DISTINCT payment.Num from payment join action_payment on action_payment.payment_id = payment.id where action_payment.action_id = action_not_approved.`ID`) AS 'Payment_Num',

                 (select CONCAT(`FirstName`,' ',`FatherName`, ' ',`LastName`) from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action_not_approved.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'Name',

                 (select beneficiary.ID from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action_not_approved.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_ID',

                 (select beneficiary.Account_ID from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action_not_approved.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_Account_ID',

                 (select account.name from beneficiary left join account on beneficiary.Account_ID = account.id
                 where beneficiary.MicroProject_ID = action_not_approved.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_Account',

                 action_not_approved.created_at, action_not_approved.updated_at, action_not_approved.`SourceAction`,
                 donor.`ID` AS 'beneficiaryDonor_ID', donor.`Name` AS 'beneficiaryDonor_Name' 

                 FROM `action_not_approved` LEFT JOIN actiontype on actiontype.id = action_not_approved.atype_id
                 LEFT join `till` on action_not_approved.Till_ID = till.ID LEFT join `tilltypes` on tilltypes.ID = till.Type_ID
                 LEFT JOIN `tilltypes` as currency_tilltype on currency_tilltype.id = action_not_approved.currency_tilltype_id
                 LEFT join `account` on account.ID = till.Account_ID
                 LEFT join `subproject` on account.SubProject_ID = subproject.ID
                 LEFT join `cashaccount` on action_not_approved.cashaccount_id = cashaccount.id
                 LEFT JOIN `account_donor` ON account_donor.account_ID = account.ID
                 LEFT JOIN donor on donor.id = account_donor.donor_id
                 LEFT JOIN `user` approved_byuser on approved_byuser.id = action_not_approved.Approved_by
                 LEFT JOIN `user` created_byuser on created_byuser.id = action_not_approved.User_ID
                 LEFT JOIN `user` edited_byuser on edited_byuser.id = edited_by";

            string condition = " where tilltypes.Name = 'Pay' and account.SubProject_ID in (" + SubProject_ID + ") ";

            if (ForApproval)
            {
                condition += " and action_not_approved.SourceAction = 1";
                condition += " and (edited_byuser.ID <> " + Properties.Settings.Default.User_ID + " or edited_byuser.ID is null) and created_byuser.`ID` <> " + Properties.Settings.Default.User_ID;

                if (Properties.Settings.Default.Role != "Head Office")
                    condition += " and action_not_approved.`Amount` <= (select role.Approval_Limit from role where role.Name = '" + Properties.Settings.Default.Role + "')";
            }

            if (Action_ID != -1)
            {
                condition += " AND action_not_approved.id = @Action_ID";
            }
            if (FromDate != "")
            {
                condition += " AND action_not_approved.`ActionDate` >= @FromDate";
            }
            if (ToDate != "")
            {
                condition += " AND action_not_approved.`ActionDate` <= @ToDate";
            }
            if (Microproject_ID != -1)
            {
                condition += " AND action_not_approved.`MicroProject_ID` = @Microproject_ID";
            }

            condition += " order by action_not_approved.id";
            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Microproject_ID", Microproject_ID);
                sc.Parameters.AddWithValue("@FromDate", FromDate);
                sc.Parameters.AddWithValue("@ToDate", ToDate);

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_All_Deleted_Payments(string SubProject_ID, bool ForApproval)
        {
            string query = @"SELECT subproject.ID as 'SubProjectID', subproject.Name as 'SubProjectName',
                 account.ID as 'AccountID', account.Name as 'AccountName',currency_tilltype.Name as 'Currency',
                 action_deleted.`ID` as 'ID' ,action_deleted.`Till_ID`, actiontype.`Name` as 'AType',action_deleted.Amount as 'Income',
(CASE WHEN Lower(currency_tilltype.Name) = 'syr' THEN action_deleted.Amount
ELSE 0 END) AS 'Incomesyr',
(CASE WHEN Lower(currency_tilltype.Name) = 'usd' THEN action_deleted.Amount
ELSE 0 END) AS 'Incomeusd',action_deleted.Returned_Amount as 'Outcome',
(CASE WHEN Lower(currency_tilltype.Name) = 'syr' THEN action_deleted.Returned_Amount
ELSE 0 END) AS 'Outcomesyr',
(CASE WHEN Lower(currency_tilltype.Name) = 'usd' THEN action_deleted.Returned_Amount
ELSE 0 END) AS 'Outcomeusd',

action_deleted.`note` as 'Note',
                 action_deleted.`MicroProject_ID` ,action_deleted.`ActionDate`, DATE_FORMAT(action_deleted.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action_deleted.ActionDate, ""%Y"") as 'ActionYear', 
                 action_deleted.`IsApproved`, approved_byuser.`Username` as 'approved_by', action_deleted.`Approved_at`, created_byuser.`Username` as 'created_by', edited_byuser.username as 'edited_by',
                 deleted_byuser.username as 'deleted_by', action_deleted.`deleted_at` as 'deleted_at',
                 action_deleted.`cashaccount_id` as 'cashaccount_id', cashaccount.`name` as 'cashaccount_name',

                 (select payment.Num from payment join action_payment on action_payment.payment_id = payment.id where action_payment.action_id = action_deleted.`ID`) AS 'Payment_Num',

                 (select CONCAT(`FirstName`,' ',`FatherName`, ' ',`LastName`) from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action_deleted.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'Name',

                 (select beneficiary.ID from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action_deleted.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_ID',

                 (select beneficiary.Account_ID from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                 where beneficiary.MicroProject_ID = action_deleted.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_Account_ID',

                 (select account.name from beneficiary left join account on beneficiary.Account_ID = account.id
                 where beneficiary.MicroProject_ID = action_deleted.MicroProject_ID and account.SubProject_ID = SubProjectID ) as 'beneficiary_Account',

                 action_deleted.created_at, action_deleted.updated_at, action_deleted.`SourceAction`,
                 donor.`ID` AS 'beneficiaryDonor_ID', donor.`Name` AS 'beneficiaryDonor_Name' 

                 FROM `action_deleted` LEFT JOIN actiontype on actiontype.id = action_deleted.atype_id
                 LEFT join `till` on action_deleted.Till_ID = till.ID LEFT join `tilltypes` on tilltypes.ID = till.Type_ID
                 LEFT JOIN `tilltypes` as currency_tilltype on currency_tilltype.id = action_deleted.currency_tilltype_id
                 LEFT join `account` on account.ID = till.Account_ID
                 LEFT join `subproject` on account.SubProject_ID = subproject.ID
                 LEFT join `cashaccount` on action_deleted.cashaccount_id = cashaccount.id
                 LEFT JOIN `account_donor` ON account_donor.account_ID = account.ID
                 LEFT JOIN donor on donor.id = account_donor.donor_id
                 LEFT JOIN `user` approved_byuser on approved_byuser.id = action_deleted.Approved_by
                 LEFT JOIN `user` created_byuser on created_byuser.id = action_deleted.User_ID
                 LEFT JOIN `user` edited_byuser on edited_byuser.id = edited_by
                 LEFT JOIN `user` deleted_byuser on deleted_byuser.id = deleted_by";

            string condition = " where tilltypes.Name = 'Pay' and account.SubProject_ID in (" + SubProject_ID + ") ";

            if (ForApproval)
            {
                condition += " and action_deleted.SourceAction = 1";
                condition += " and (edited_byuser.ID <> " + Properties.Settings.Default.User_ID + " or edited_byuser.ID is null) and created_byuser.`ID` <> " + Properties.Settings.Default.User_ID + " and deleted_by <> " + Properties.Settings.Default.User_ID + "";

                if (Properties.Settings.Default.Role != "Head Office")
                    condition += " and action_deleted.`Amount` <= (select role.Approval_Limit from role where role.Name = '" + Properties.Settings.Default.Role + "')";
            }

            condition += " order by action_deleted.id";
            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();


                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_Cash_Payments(int SubProject_ID)
        {
            string query = "SELECT cash_balances.CashAccount_id as 'CashAccount_id',cash_balances.Account_Name as 'Account', cash_balances.CashAccount_Name AS 'Cash till', cash_balances.Account_I AS 'Income(SYR)'," +
                " cash_balances.Account_E AS 'Outcome(SYR)',cash_balances.Account_Net AS 'Difference(SYR)', cash_balances.Account_I_USD AS 'Income(USD)',cash_balances.Account_E_USD AS 'Outcome(USD)',cash_balances.Account_Net_USD AS 'Difference(USD)'" +
                " FROM cash_balances WHERE cash_balances.Subproject_ID = @Subproject_ID and cash_balances.tilltype_Name = 'pay';";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);
                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public void Insert_Payment(double Amount, double Returned_Amount, string Date, string Note,
                int MicroProject_ID, int SubProject_ID, int Beneficiary_ID,
                DataTable payments, int CashAccount_ID, int Currency_TillType_ID, string Form_Name)
        {
            if (!Login.BuildConnection()) return;
            try
            {
                using (MySqlCommand sc = new MySqlCommand())
                {
                    sc.Connection = Login.MyConn;
                    sc.CommandTimeout = 120;
                    InsertPaymentCore(sc, Amount, Returned_Amount, Date, Note, MicroProject_ID, SubProject_ID, Beneficiary_ID, CashAccount_ID, Currency_TillType_ID, Form_Name, true);
                }
            }
            finally
            {
                Login.CloseAndClearConnection();
            }
        }

        private int InsertPaymentCore(MySqlCommand sc, double Amount, double Returned_Amount, string Date, string Note,
                int MicroProject_ID, int SubProject_ID, int Beneficiary_ID,
                int CashAccount_ID, int Currency_TillType_ID, string Form_Name, bool manageTransaction)
        {
                string sql = "";

                sc.Parameters.Clear();
                sc.Parameters.AddWithValue("@MicroProject_ID", MicroProject_ID == -1 ? (object)DBNull.Value : MicroProject_ID);
                sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID == -1 ? (object)DBNull.Value : SubProject_ID);
                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                sc.Parameters.AddWithValue("@Returned_Amount", Returned_Amount);
                sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);
                sc.Parameters.AddWithValue("@Currency_TillType_ID", Currency_TillType_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                sc.Parameters.AddWithValue("@User_ID", Settings.Default.User_ID);
                sc.Parameters.AddWithValue("@Note", Note);
                sc.Parameters.AddWithValue("@Date", Date == "Null" ? (object)DBNull.Value : Date);
                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);

                // Get Till ID for the current SubProject
                sc.CommandText = "SELECT till.ID"
                            +" FROM account LEFT JOIN till ON till.Account_ID = account.ID"
                            +" LEFT JOIN tilltypes ON till.Type_ID = tilltypes.ID"
                            +" LEFT JOIN account_donor on account_donor.Account_id = account.ID"
                            +" LEFT join (SELECT account_donor.Donor_ID AS 'DonorID', account.SubProject_ID AS 'SubProjectID' FROM beneficiary LEFT JOIN account ON beneficiary.Account_ID = account.ID  LEFT JOIN account_donor on account_donor.Account_id = account.ID WHERE beneficiary.MicroProject_ID =  "+MicroProject_ID +" AND account.SubProject_ID =  "+ SubProject_ID + ") AS t1 ON account_donor.Donor_ID = t1.DonorID"                   
                            + " WHERE account.SubProject_ID = SubProjectID AND tilltypes.Name = 'pay';";


                object tillObj = sc.ExecuteScalar();
                if (tillObj == null || tillObj == DBNull.Value)
                {
                    throw new Exception("No Till exists for this SubProject. Please create a Till with type 'pay' first.");
                }
                int tillID = Convert.ToInt32(tillObj);

                // Get the next available action ID
                sc.CommandText = "SELECT IFNULL(MAX(x.max_id),0) + 1 " +
                                 "FROM (SELECT MAX(ID) AS max_id FROM action " +
                                 "      UNION ALL SELECT MAX(ID) FROM action_not_approved " +
                                 "      UNION ALL SELECT MAX(ID) FROM action_deleted) AS x;";
                object aObj = sc.ExecuteScalar();
                int actionID = (aObj == DBNull.Value || aObj == null) ? 1 : Convert.ToInt32(aObj);

                
                int numofinserts = 0;


                if (Amount == 0 && Returned_Amount > 0)
                {
                    sql = manageTransaction ? "START TRANSACTION;" : "";

                    sql += @"SET @num = (
                       SELECT p.Num
                       FROM payment p
                       WHERE p.Beneficiary_ID = @Beneficiary_ID
                       ORDER BY (
                           SELECT MAX(active_action.ID)
                           FROM (
                               SELECT ID, AType_ID FROM action
                               UNION ALL
                               SELECT ID, AType_ID FROM action_not_approved
                           ) AS active_action
                           JOIN action_payment ap3 ON ap3.action_id = active_action.ID
                           WHERE ap3.payment_id = p.ID
                           AND active_action.AType_ID = (SELECT ID FROM actiontype WHERE shortname = 'I' LIMIT 1)
                       ) DESC, p.ID ASC
                       LIMIT 1);";
                    sql += @"INSERT INTO payment(Amount, DueDate, Beneficiary_ID, Num)
                            VALUES(-@Returned_Amount, @Date, @Beneficiary_ID, IFNULL(@num, 0) + 1);";
                    sql += @"SET @payId = Last_Insert_ID() ;";
                    sql += "INSERT INTO action_not_approved(id, Till_ID, AType_ID, currency_tilltype_id, Amount, Returned_Amount, rate, ActionDate, Note, " +
                                   "IsReceipt, MicroProject_ID, cashaccount_id, created_at, User_ID) VALUES (" +
                                   actionID + ", " + tillID + ", (SELECT id FROM actiontype WHERE shortname='R' LIMIT 1), @Currency_TillType_ID, " +
                                   " 0," + Returned_Amount + ", 0, @Date, @Note, -1, @MicroProject_ID, @CashAccount_ID, @Todaydate, @User_ID);";
                    sql += "INSERT INTO action_payment(action_id, payment_id) VALUES(" + actionID + ",@payId);";

                    
                    sql += "CALL ReGenerate_AllPayments("+ Beneficiary_ID + ");";
                    if (manageTransaction)
                        sql += "COMMIT;";
                    numofinserts++;
                    // Execute the SQL commands
                    sc.CommandText = "SET SESSION wait_timeout = @t;" + sql;
                    sc.Parameters.AddWithValue("@t", 600);
                    sc.ExecuteNonQuery();

                    // Log the actions that were inserted
                    int firstInsertedActionIdLog = actionID - numofinserts;
                    string escapedSql = Regex.Replace(sql, "'", "''");
                    for (int i = 0; i < numofinserts; i++)
                    {
                        int insertedId = firstInsertedActionIdLog + i;
                        string details = "payment - I (Return)";
                        string logQ = "INSERT INTO log(form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                                     "VALUES (@Form_Name, 'INSERT', 'action_not_approved', " + insertedId + ", '" + details + "', CONCAT('beneficiary ID = ',@Beneficiary_ID), '" +
                                     escapedSql + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        sc.CommandText = logQ;
                        sc.ExecuteNonQuery();
                    }

                    return numofinserts;
                }

                else if (Amount > 0 && Returned_Amount == 0)
                {
                    if (manageTransaction)
                    {
                        sc.CommandText = "START TRANSACTION;";
                        sc.ExecuteNonQuery();
                    }

                    try
                    {
                        DataTable currentPayablePayments = SelectPayablePaymentsForUpdate(sc.Connection, Beneficiary_ID);

                        List<DataRow> payablePayments = currentPayablePayments.AsEnumerable()
                            .Where(IsPayablePayment)
                            .OrderBy(r => Convert.ToInt32(r["Num"]))
                            .ThenBy(GetPaymentDueDate)
                            .ThenBy(r => Convert.ToInt32(r["ID"]))
                            .ToList();

                        sql = "";
                        foreach (DataRow r in payablePayments)
                        {
                            if (Amount <= 0) break; 

                            int payId = Convert.ToInt32(r["ID"]);
                            double scheduled = Convert.ToDouble(r["Amount"]);
                            double paidAmount = Convert.ToDouble(r["Paid_Amount"]);
                            double need = Math.Max(0, scheduled - paidAmount);

                            if (need <= 0 || !IsPayablePayment(r)) continue;

                            double apply = Math.Min(Amount, need);

                            sql += "INSERT INTO action_not_approved(id, Till_ID, AType_ID, currency_tilltype_id, Amount, Returned_Amount, rate, ActionDate, Note, " +
                                   "IsReceipt, MicroProject_ID, cashaccount_id, created_at, User_ID) VALUES (" +
                                   actionID + ", " + tillID + ", (SELECT id FROM actiontype WHERE shortname='I' LIMIT 1), " +
                                  " @Currency_TillType_ID, " +
                                   apply + ", 0, 0, @Date, @Note, -1, @MicroProject_ID, @CashAccount_ID, @Todaydate, @User_ID);";

                            sql += "INSERT INTO action_payment(action_id, payment_id) VALUES(" + actionID + ", " + payId + ");";

                            actionID++;
                            numofinserts++;
                            Amount -= apply;

                            if (Amount <= 0) break;
                        }

                        if (manageTransaction)
                            sql += "COMMIT;";

                        sc.CommandText = "SET SESSION wait_timeout = @t;" + sql;
                        sc.Parameters.AddWithValue("@t", 600);
                        sc.ExecuteNonQuery();
                    }
                    catch
                    {
                        if (manageTransaction && Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                        {
                            try
                            {
                                sc.CommandText = "ROLLBACK;";
                                sc.ExecuteNonQuery();
                            }
                            catch { }
                        }

                        throw;
                    }

                    int firstInsertedActionIdLog = actionID - numofinserts;
                    string escapedSql = Regex.Replace(sql, "'", "''");
                    for (int i = 0; i < numofinserts; i++)
                    {
                        int insertedId = firstInsertedActionIdLog + i;
                        string details = "payment - I";
                        string logQ = "INSERT INTO log(form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                                     "VALUES (@Form_Name, 'INSERT', 'action_not_approved', " + insertedId + ", '" + details + "', CONCAT('beneficiary ID = ',@Beneficiary_ID), '" +
                                     escapedSql + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        sc.CommandText = logQ;
                        sc.ExecuteNonQuery();
                    }

                    return numofinserts;
                }

                throw new Exception("Please enter either Amount (income) or Returned Amount (refund), not both or none.");
        }

        public void Update_Payment(int Action_ID, int Till_ID, Double Amount, Double ReturnedAmount, string Date, string Note, int MicroProject_ID, int Beneficiary_ID, int CashAccount_ID, int Currency_TillType_ID, string Form_Name)
        {
            if ((Amount > 0 && ReturnedAmount > 0) || (Amount <= 0 && ReturnedAmount <= 0))
                throw new Exception("Please enter either Amount (income) or Returned Amount (refund), not both or none.");

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Till_ID", Till_ID);

                sc.Parameters.AddWithValue("@Amount", Amount);
                sc.Parameters.AddWithValue("@ReturnedAmount", ReturnedAmount);

                if (Date == "Null")
                {
                    sc.Parameters.AddWithValue("@Date", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Date", Date);
                }

                sc.Parameters.AddWithValue("@Note", Note);

                if (MicroProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", MicroProject_ID);
                }
                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);

                sc.Parameters.AddWithValue("@Currency_TillType_ID", Currency_TillType_ID);

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                sc.Parameters.AddWithValue("@User_ID", Settings.Default.User_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += @"UPDATE`action` SET `Amount` = @Amount,`Returned_Amount` = @ReturnedAmount,
                            `ActionDate` = @Date, `Note` = @Note, `MicroProject_ID` = @MicroProject_ID, cashaccount_id =@CashAccount_ID, `currency_tilltype_id` = @Currency_TillType_ID, updated_at =  @Todaydate, `edited_by` = @User_ID  
                            WHERE action.ID = @Action_ID;";


                query += "UPDATE `action` SET " +
                        " `IsApproved`= 0" +
                        " ,`Approved_at`= null" +
                        " ,`Approved_by`= null" +
                        " WHERE ID = @Action_ID;";

                query += "UPDATE `action` SET " +
                        " `IsApproved`= 0" +
                        " ,`Approved_at`= null" +
                        " ,`Approved_by`= null" +
                       " WHERE ObserveAcion_ID = @Action_ID;";

                query += "INSERT INTO action_not_approved SELECT * FROM action WHERE action.id = @Action_ID OR action.ObserveAcion_ID = @Action_ID;";

                query += "DELETE FROM action WHERE action.ObserveAcion_ID = @Action_ID;";

                query += "DELETE FROM action WHERE action.id  = @Action_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action', @Action_ID, 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;START TRANSACTION;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                try
                {
                    sc.ExecuteNonQuery();

                    if (ReturnedAmount > 0 && Amount == 0)
                    {
                        sc.CommandText = @"UPDATE payment
                                           JOIN action_payment ON payment.ID = action_payment.payment_id
                                           SET payment.Amount = -@ReturnedAmount,
                                               payment.DueDate = @Date
                                           WHERE action_payment.action_id = @Action_ID
                                             AND payment.Beneficiary_ID = @Beneficiary_ID
                                             AND payment.Amount < 0;";
                        if (sc.ExecuteNonQuery() == 0)
                            throw new Exception("Cannot update this returned payment because its linked negative payment row was not found.");

                        sc.CommandText = "CALL ReGenerate_AllPayments(@Beneficiary_ID);";
                        sc.ExecuteNonQuery();
                    }
                    else
                    {
                        ReallocateInstallmentPaymentActions(Login.MyConn, Beneficiary_ID);
                    }

                    sc.CommandText = "COMMIT;";
                    sc.ExecuteNonQuery();
                }
                catch
                {
                    try
                    {
                        sc.CommandText = "ROLLBACK;";
                        sc.ExecuteNonQuery();
                    }
                    catch { }

                    throw;
                }
                finally
                {
                    Login.CloseAndClearConnection();
                }
            }
        }

        public void Update_Not_Approved_Payment(int Action_ID, int Till_ID, Double Amount, Double ReturnedAmount, string Date, string Note, int MicroProject_ID, int Beneficiary_ID, int CashAccount_ID, int Currency_TillType_ID, string Form_Name)
        {
            if ((Amount > 0 && ReturnedAmount > 0) || (Amount <= 0 && ReturnedAmount <= 0))
                throw new Exception("Please enter either Amount (income) or Returned Amount (refund), not both or none.");

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Till_ID", Till_ID);
                sc.Parameters.AddWithValue("@Amount", Amount);
                sc.Parameters.AddWithValue("@ReturnedAmount", ReturnedAmount); // تم توحيد المسمى هنا

                if (Date == "Null")
                {
                    sc.Parameters.AddWithValue("@Date", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Date", Date);
                }

                sc.Parameters.AddWithValue("@Note", Note);

                if (MicroProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", MicroProject_ID);
                }
                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);
                sc.Parameters.AddWithValue("@Currency_TillType_ID", Currency_TillType_ID);

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                sc.Parameters.AddWithValue("@User_ID", Settings.Default.User_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                // تعديل البارامتر هنا ليتوافق مع الإدخال @ReturnedAmount
                query += @"UPDATE `action_not_approved` SET `Amount` = @Amount, `Returned_Amount` = @ReturnedAmount,
                     `ActionDate` = @Date, `Note` = @Note, `MicroProject_ID` = @MicroProject_ID, cashaccount_id = @CashAccount_ID, `currency_tilltype_id` = @Currency_TillType_ID, updated_at = @Todaydate, `edited_by` = @User_ID  
                     WHERE action_not_approved.ID = @Action_ID;";

                query += "UPDATE `action_not_approved` SET `IsApproved` = 0, `Approved_at` = null, `Approved_by` = null WHERE ID = @Action_ID;";
                query += "UPDATE `action_not_approved` SET `IsApproved` = 0, `Approved_at` = null, `Approved_by` = null WHERE ObserveAcion_ID = @Action_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action_not_approved', @Action_ID, 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;START TRANSACTION;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                try
                {
                    sc.ExecuteNonQuery();

                    if (ReturnedAmount > 0 && Amount == 0)
                    {
                        sc.CommandText = @"UPDATE payment
                                           JOIN action_payment ON payment.ID = action_payment.payment_id
                                           SET payment.Amount = -@ReturnedAmount,
                                               payment.DueDate = @Date
                                           WHERE action_payment.action_id = @Action_ID
                                             AND payment.Beneficiary_ID = @Beneficiary_ID
                                             AND payment.Amount < 0;";
                        if (sc.ExecuteNonQuery() == 0)
                            throw new Exception("Cannot update this returned payment because its linked negative payment row was not found.");

                        sc.CommandText = "CALL ReGenerate_AllPayments(@Beneficiary_ID);";
                        sc.ExecuteNonQuery();
                    }
                    else
                    {
                        ReallocateInstallmentPaymentActions(Login.MyConn, Beneficiary_ID);
                    }

                    sc.CommandText = "COMMIT;";
                    sc.ExecuteNonQuery();
                }
                catch
                {
                    try
                    {
                        sc.CommandText = "ROLLBACK;";
                        sc.ExecuteNonQuery();
                    }
                    catch { }

                    throw;
                }
                finally
                {
                    Login.CloseAndClearConnection();
                }
            }
        }

        private string BuildDeletePaymentActionSql(string actionIdParameter, string beneficiaryIdParameter, string todayDateParameter, string userIdParameter, bool approvedAction)
        {
            string sourceTable = approvedAction ? "action" : "action_not_approved";

            string query = "INSERT INTO action_deleted SELECT * FROM " + sourceTable + " WHERE " + sourceTable + ".id = " + actionIdParameter + " OR " + sourceTable + ".ObserveAcion_ID = " + actionIdParameter + ";";
            query += "UPDATE action_deleted SET `deleted_at` = " + todayDateParameter + ", `deleted_by` = " + userIdParameter + " WHERE action_deleted.id = " + actionIdParameter + " OR action_deleted.ObserveAcion_ID = " + actionIdParameter + ";";
            query += @"SET @DeletedReturnedPayment_ID = (
                            SELECT payment.ID
                            FROM payment
                            JOIN action_payment ON action_payment.payment_id = payment.ID
                            JOIN action_deleted ON action_deleted.ID = action_payment.action_id
                            WHERE (action_deleted.ID = " + actionIdParameter + @" OR action_deleted.ObserveAcion_ID = " + actionIdParameter + @")
                              AND action_deleted.AType_ID = 3
                              AND payment.Beneficiary_ID = " + beneficiaryIdParameter + @"
                              AND payment.Amount < 0
                            LIMIT 1
                        );";
            query += Financial_WPF_App.Models.Action.DeleteActionPaymentForDeletedInstallmentActionsSql(actionIdParameter);
            query += "DELETE FROM payment WHERE ID = @DeletedReturnedPayment_ID AND Beneficiary_ID = " + beneficiaryIdParameter + " AND Amount < 0;";
            query += "DELETE FROM " + sourceTable + " WHERE " + sourceTable + ".ObserveAcion_ID = " + actionIdParameter + ";";
            query += "DELETE FROM " + sourceTable + " WHERE " + sourceTable + ".id = " + actionIdParameter + ";";
            query += "CALL ReGenerate_AllPayments(" + beneficiaryIdParameter + ");";

            return query;
        }

        public void Delete_Payment(int ID, int Beneficiary_ID, bool Approved_Action, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Connection = Login.MyConn;

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                sc.Parameters.AddWithValue("@User_ID", Settings.Default.User_ID);

                string targetTable = Approved_Action ? "action" : "action_not_approved";
                query += BuildDeletePaymentActionSql("@ID", "@Beneficiary_ID", "@Todaydate", "@User_ID", Approved_Action);

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += $" INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', '{targetTable}', @ID, 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;START TRANSACTION;" + query;
                sc.Parameters.AddWithValue("@t", 600);

                try
                {
                    sc.ExecuteNonQuery();

                    sc.CommandText = "COMMIT;";
                    sc.ExecuteNonQuery();
                }
                catch
                {
                    try
                    {
                        sc.CommandText = "ROLLBACK;";
                        sc.ExecuteNonQuery();
                    }
                    catch { }

                    throw;
                }
                finally
                {
                    Login.CloseAndClearConnection();
                }
            }
        }

        public void Delete_Multi_Payments(List<int> Action_IDs, List<int> Beneficiary_IDs, bool Approved_Action, string Form_Name)
        {
            if (Action_IDs == null || Beneficiary_IDs == null || Action_IDs.Count != Beneficiary_IDs.Count || Action_IDs.Count == 0)
                return;

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Connection = Login.MyConn;

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                sc.Parameters.AddWithValue("@User_ID", Settings.Default.User_ID);

                string targetTable = Approved_Action ? "action" : "action_not_approved";
                for (int i = 0; i < Action_IDs.Count; i++)
                {
                    sc.Parameters.AddWithValue("@ID" + i, Action_IDs[i]);
                    sc.Parameters.AddWithValue("@Beneficiary_ID" + i, Beneficiary_IDs[i]);
                    sc.Parameters.AddWithValue("@Todaydate" + i, Todaydate);
                    query += BuildDeletePaymentActionSql("@ID" + i, "@Beneficiary_ID" + i, "@Todaydate" + i, "@User_ID", Approved_Action);
                }

                log_query = Regex.Replace(query, "'", "''");
                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                for (int i = 0; i < Action_IDs.Count; i++)
                {
                    query += $" INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', '{targetTable}', @ID" + i + ", 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID" + i + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;START TRANSACTION;" + query;
                sc.Parameters.AddWithValue("@t", 600);

                try
                {
                    sc.ExecuteNonQuery();

                    sc.CommandText = "COMMIT;";
                    sc.ExecuteNonQuery();
                }
                catch
                {
                    try
                    {
                        sc.CommandText = "ROLLBACK;";
                        sc.ExecuteNonQuery();
                    }
                    catch { }

                    throw;
                }
                finally
                {
                    Login.CloseAndClearConnection();
                }
            }
        }

        public bool Restore_Deleted_Payment_Action(int DeletedAction_ID, string Form_Name)
        {
            DataTable deletedAction = new DataTable();

            if (!Login.BuildConnection())
                return false;

            using (MySqlCommand sc = new MySqlCommand())
            {
                sc.Connection = Login.MyConn;
                sc.CommandTimeout = 120;

                try
                {
                    sc.CommandText = "START TRANSACTION;";
                    sc.ExecuteNonQuery();

                    sc.Parameters.Clear();
                    sc.Parameters.AddWithValue("@DeletedAction_ID", DeletedAction_ID);
                    sc.CommandText = @"SELECT ad.ID, ad.Amount, ad.Returned_Amount, ad.ActionDate, ad.Note,
                                               ad.MicroProject_ID, ad.cashaccount_id, ad.currency_tilltype_id,
                                               ad.IsApproved, ad.AType_ID,
                                               beneficiary.ID AS Beneficiary_ID, account.SubProject_ID
                                        FROM action_deleted ad
                                        LEFT JOIN till ON till.ID = ad.Till_ID
                                        LEFT JOIN account ON account.ID = till.Account_ID
                                        LEFT JOIN beneficiary ON beneficiary.MicroProject_ID = ad.MicroProject_ID
                                        LEFT JOIN account beneficiary_account ON beneficiary_account.ID = beneficiary.Account_ID
                                        WHERE ad.ID = @DeletedAction_ID
                                          AND ad.MicroProject_ID IS NOT NULL
                                          AND ad.AType_ID IN (1, 3)
                                          AND (beneficiary_account.SubProject_ID = account.SubProject_ID OR account.SubProject_ID IS NULL)
                                        LIMIT 1
                                        FOR UPDATE;";

                    MySqlDataAdapter da = new MySqlDataAdapter(sc);
                    da.Fill(deletedAction);

                    if (deletedAction.Rows.Count == 0)
                    {
                        sc.CommandText = "ROLLBACK;";
                        sc.ExecuteNonQuery();
                        return false;
                    }

                    DataRow row = deletedAction.Rows[0];
                    if (row["Beneficiary_ID"] == DBNull.Value)
                        throw new Exception("Cannot restore this payment action because no beneficiary was found for its microproject.");
                    if (row["SubProject_ID"] == DBNull.Value)
                        throw new Exception("Cannot restore this payment action because no subproject was found for its till.");

                    double amount = row["Amount"] == DBNull.Value ? 0 : Convert.ToDouble(row["Amount"]);
                    double returnedAmount = row["Returned_Amount"] == DBNull.Value ? 0 : Convert.ToDouble(row["Returned_Amount"]);
                    int beneficiaryId = Convert.ToInt32(row["Beneficiary_ID"]);
                    int isApproved = row["IsApproved"] == DBNull.Value ? 0 : Convert.ToInt32(row["IsApproved"]);
                    int actionTypeId = row["AType_ID"] == DBNull.Value ? -1 : Convert.ToInt32(row["AType_ID"]);

                    sc.Parameters.Clear();
                    sc.Parameters.AddWithValue("@DeletedAction_ID", DeletedAction_ID);
                    sc.Parameters.AddWithValue("@Beneficiary_ID", beneficiaryId);
                    sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                    sc.Parameters.AddWithValue("@ReturnedPaymentAmount", -returnedAmount);
                    sc.Parameters.AddWithValue("@ReturnedDueDate", row["ActionDate"] == DBNull.Value ? (object)DBNull.Value : Convert.ToDateTime(row["ActionDate"]).ToString("yyyy/MM/dd"));
                    sc.Parameters.AddWithValue("@PaymentNum", 0);

                    string restoreTargetTable = isApproved == 1 ? "action" : "action_not_approved";
                    string restoreSql = "INSERT INTO " + restoreTargetTable + " SELECT * FROM action_deleted WHERE action_deleted.id = @DeletedAction_ID OR action_deleted.ObserveAcion_ID = @DeletedAction_ID;";
                    restoreSql += "UPDATE " + restoreTargetTable + " SET deleted_at = null, deleted_by = null WHERE " + restoreTargetTable + ".id = @DeletedAction_ID OR " + restoreTargetTable + ".ObserveAcion_ID = @DeletedAction_ID;";

                    if (actionTypeId == 3 || (returnedAmount > 0 && amount == 0))
                    {
                        restoreSql += @"SET @RestorePayment_ID = (
                                            SELECT payment.ID
                                            FROM payment
                                            LEFT JOIN action_payment ON action_payment.payment_id = payment.ID
                                            WHERE payment.Beneficiary_ID = @Beneficiary_ID
                                              AND payment.Amount = @ReturnedPaymentAmount
                                              AND ((payment.DueDate = @ReturnedDueDate) OR (payment.DueDate IS NULL AND @ReturnedDueDate IS NULL))
                                              AND action_payment.id IS NULL
                                            ORDER BY payment.ID
                                            LIMIT 1
                                        );
                                        INSERT INTO payment(Amount, DueDate, Beneficiary_ID, Num)
                                        SELECT @ReturnedPaymentAmount, @ReturnedDueDate, @Beneficiary_ID, @PaymentNum
                                        WHERE @RestorePayment_ID IS NULL;
                                        SET @RestorePayment_ID = IFNULL(@RestorePayment_ID, LAST_INSERT_ID());
                                        INSERT INTO action_payment(action_id, payment_id)
                                        SELECT @DeletedAction_ID, @RestorePayment_ID
                                        WHERE NOT EXISTS (SELECT 1 FROM action_payment WHERE action_id = @DeletedAction_ID);";
                    }
                    else
                    {
                        restoreSql += @"CALL ReGenerate_AllPayments(@Beneficiary_ID);";
                        restoreSql += @"SET @RestorePayment_ID = (
                                            SELECT payment.ID
                                            FROM payment
                                            LEFT JOIN action_payment ON action_payment.payment_id = payment.ID
                                            LEFT JOIN (
                                                SELECT ID, Amount, Returned_Amount FROM action
                                                UNION ALL
                                                SELECT ID, Amount, Returned_Amount FROM action_not_approved
                                            ) AS combined_action ON combined_action.ID = action_payment.action_id
                                            WHERE payment.Beneficiary_ID = @Beneficiary_ID
                                              AND payment.Amount > 0
                                            GROUP BY payment.ID, payment.Amount, payment.Num, payment.DueDate
                                            HAVING payment.Amount - (IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0)) >= (
                                                SELECT Amount FROM " + restoreTargetTable + @" WHERE ID = @DeletedAction_ID
                                            )
                                            ORDER BY payment.Num, payment.DueDate, payment.ID
                                            LIMIT 1
                                        );
                                        INSERT INTO action_payment(action_id, payment_id)
                                        SELECT @DeletedAction_ID, @RestorePayment_ID
                                        WHERE @RestorePayment_ID IS NOT NULL
                                          AND NOT EXISTS (SELECT 1 FROM action_payment WHERE action_id = @DeletedAction_ID);";
                    }

                    sc.CommandText = restoreSql + @"
                                      DELETE FROM action_deleted WHERE ObserveAcion_ID = @DeletedAction_ID;
                                      DELETE FROM action_deleted WHERE ID = @DeletedAction_ID;
                                      CALL ReGenerate_AllPayments(@Beneficiary_ID);
                                      INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user)
                                      VALUES (@Form_Name, 'RESTORE', 'action_deleted', @DeletedAction_ID, 'payment action restored', '', '', DATE(NOW()), '" + Properties.Settings.Default.username + @"');";
                    sc.ExecuteNonQuery();

                    sc.CommandText = "SELECT COUNT(*) FROM action_payment WHERE action_id = @DeletedAction_ID;";
                    object restoredLinks = sc.ExecuteScalar();
                    if (restoredLinks == null || restoredLinks == DBNull.Value || Convert.ToInt32(restoredLinks) == 0)
                        throw new Exception("Cannot restore this payment action because no payment row could be linked to it.");

                    sc.CommandText = "COMMIT;";
                    sc.ExecuteNonQuery();

                    return true;
                }
                catch
                {
                    try
                    {
                        sc.CommandText = "ROLLBACK;";
                        sc.ExecuteNonQuery();
                    }
                    catch { }

                    throw;
                }
                finally
                {
                    Login.CloseAndClearConnection();
                }
            }
        }

        public void Delay_Payments(int Monthes, List<int> Beneficiary_IDs)
        {
            string query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Monthes", Monthes);

                query = "START TRANSACTION;";

                query += @"UPDATE payment LEFT JOIN action_payment ON payment.ID = action_payment.payment_id
                            SET payment.DueDate = DATE_ADD(payment.DueDate, INTERVAL @Monthes MONTH)
                            WHERE action_payment.id IS null";


                string Ids = "";
                for (int i = 0; i < Beneficiary_IDs.Count; i++)
                {
                    Ids += Beneficiary_IDs[i].ToString() + ",";
                }

                Ids = Ids.Remove(Ids.Length - 1);


                query += " and payment.Beneficiary_ID in (" + Ids + ");";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();
                Login.CloseAndClearConnection();
            }
        }

        public void Insert_Multi_payments()
        {
            string query = "";


            query += "START TRANSACTION;";
            query += string.Join(Environment.NewLine, Login.MultiPayments_List.ToArray());
            query += " COMMIT;";

            if (Login.BuildConnection())
            {
                try
                {
                    Login.MultiPayments_sc.CommandTimeout = 120;
                    Login.MultiPayments_sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                    Login.MultiPayments_sc.Parameters.AddWithValue("@t", 600);
                    Login.MultiPayments_sc.Connection = Login.MyConn;

                    Login.MultiPayments_sc.ExecuteNonQuery();
                }
                catch
                {
                    try
                    {
                        using (MySqlCommand cmd = new MySqlCommand("ROLLBACK;", Login.MyConn))
                        {
                            cmd.CommandTimeout = 120;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { }

                    throw;
                }
                finally
                {
                    Login.CloseAndClearConnection();
                }
            }
        }
    }
}
