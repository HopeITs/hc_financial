using Financial_WPF_App.Properties;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class Payment
    {
        public DataTable Select_Beneficiary_Payments(string Beneficiary_ID, string Subproject_ID)
        {

            string query = @"SELECT 
                                    payment.`ID`, 
                                    payment.`Amount`, 
                                    payment.`DueDate`, 
                                    payment.`Beneficiary_ID`, 
                                    payment.`Num`,
                                    GROUP_CONCAT(DATE_FORMAT(combined_action.ActionDate, ""%Y-%m-%d"")) AS 'action_date',
                                    IFNULL(SUM(combined_action.Amount),0) AS 'Paid_Amount',
                                    CASE 
                                    WHEN IFNULL(SUM(combined_action.Amount),0) = 0 THEN 'Unpaid' 
                                    WHEN IFNULL(SUM(combined_action.Amount),0) < payment.`Amount` THEN 'Incomplete' 
                                    ELSE 'Done' 
                                    END AS 'payment_state',
                                    CASE 
                                    WHEN MAX(combined_action.Source) = 'action_not_approved' THEN 'No'
                                    WHEN MAX(combined_action.Source) = 'action' THEN 'Yes'
                                    WHEN MAX(combined_action.Source) = 'action_deleted' AND MAX(combined_action.IsApproved) =1 THEN 'Yes'
                                    WHEN MAX(combined_action.Source) = 'action_deleted' AND MAX(combined_action.IsApproved) =0 THEN 'No'
                                    END AS 'Approved'

                                    FROM 
                                    payment 
                                    LEFT JOIN action_payment ON payment.id = action_payment.payment_id
                                    LEFT JOIN 
                                    (SELECT *, 'action' AS Source FROM action
                                    UNION ALL
                                    SELECT *, 'action_not_approved' AS Source FROM action_not_approved
                                    UNION ALL
                                    SELECT *, 'action_deleted' AS Source FROM action_deleted) AS combined_action 
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
            query += " GROUP BY payment.ID;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();


                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                sc.Parameters.AddWithValue("@Subproject_ID", Subproject_ID);

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public DataTable Select_All_Payments(string SubProject_ID, int Action_ID, string FromDate, string ToDate, int Microproject_ID)
        {
            string query = @"SELECT subproject.ID as 'SubProjectID', subproject.Name as 'SubProjectName',
                 account.ID as 'AccountID', account.Name as 'AccountName',
                 action.`ID` as 'ID' ,action.`Till_ID`, actiontype.`Name` as 'AType', action.`Amount` AS 'Income', action.`note` as 'Note',
                 action.`MicroProject_ID` ,action.`ActionDate`, DATE_FORMAT(action.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action.ActionDate, ""%Y"") as 'ActionYear', 
                 action.`IsApproved`, approved_byuser.`Username` as 'approved_by', action.`Approved_at`, created_byuser.`Username` as 'created_by', edited_byuser.username as 'edited_by',
                 action.`cashaccount_id` as 'cashaccount_id', cashaccount.`name` as 'cashaccount_name',

                (select payment.Num from payment join action_payment on action_payment.payment_id = payment.id where action_payment.action_id = action.`ID`) AS 'Payment_Num',

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
                 LEFT join `till` on action.Till_ID = till.ID LEFT join `tilltypes` on tilltypes.ID = till.Type_ID
                 LEFT join `account` on account.ID = till.Account_ID
                 LEFT join `subproject` on account.SubProject_ID = subproject.ID
                 LEFT join `cashaccount` on action.cashaccount_id = cashaccount.id
                 LEFT JOIN `account_donor` ON account_donor.account_ID = account.ID
                 LEFT JOIN donor on donor.id = account_donor.donor_id
                 LEFT JOIN `user` approved_byuser on approved_byuser.id = action.Approved_by
                 LEFT JOIN `user` created_byuser on created_byuser.id = action.User_ID
                 LEFT JOIN `user` edited_byuser on edited_byuser.id = edited_by";

            string condition = " where tilltypes.Name = 'Pay' and account.SubProject_ID in ("+ SubProject_ID + ") ";

            if( Action_ID != -1 )
            {
                condition += " AND action.id = @Action_ID";
            }
            if(FromDate != "")
            {
                condition += " AND action.`ActionDate` >= @FromDate";
            }
            if(ToDate != "")
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


                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public DataTable Select_All_Not_Approved_Payments(string SubProject_ID, bool ForApproval, int Action_ID, string FromDate, string ToDate, int Microproject_ID)
        {
            string query = @"SELECT subproject.ID as 'SubProjectID', subproject.Name as 'SubProjectName',
                 account.ID as 'AccountID', account.Name as 'AccountName',
                 action_not_approved.`ID` as 'ID' ,action_not_approved.`Till_ID`, actiontype.`Name` as 'AType', action_not_approved.`Amount` AS 'Income', action_not_approved.`note` as 'Note',
                 action_not_approved.`MicroProject_ID` ,action_not_approved.`ActionDate`, DATE_FORMAT(action_not_approved.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action_not_approved.ActionDate, ""%Y"") as 'ActionYear', 
                 action_not_approved.`IsApproved`, approved_byuser.`Username` as 'approved_by', action_not_approved.`Approved_at`, created_byuser.`Username` as 'created_by', edited_byuser.username as 'edited_by',
                 action_not_approved.`cashaccount_id` as 'cashaccount_id', cashaccount.`name` as 'cashaccount_name',

                 (select payment.Num from payment join action_payment on action_payment.payment_id = payment.id where action_payment.action_id = action_not_approved.`ID`) AS 'Payment_Num',

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

                sc.CommandTimeout = 120; sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public DataTable Select_All_Deleted_Payments(string SubProject_ID, bool ForApproval)
        {
            string query = @"SELECT subproject.ID as 'SubProjectID', subproject.Name as 'SubProjectName',
                 account.ID as 'AccountID', account.Name as 'AccountName',
                 action_deleted.`ID` as 'ID' ,action_deleted.`Till_ID`, actiontype.`Name` as 'AType', action_deleted.`Amount` AS 'Income', action_deleted.`note` as 'Note',
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
      

                sc.CommandTimeout = 120; sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }
        public DataTable Select_Cash_Payments(int SubProject_ID)
        {
            string query = "SELECT cash_balances.CashAccount_id as 'CashAccount_id',cash_balances.Account_Name as 'Account', cash_balances.CashAccount_Name AS 'Cash till', cash_balances.Account_I AS 'Income'," +
                " cash_balances.Account_E AS 'Outcome', cash_balances.Account_Net AS 'Difference'" +
                " FROM cash_balances WHERE cash_balances.Subproject_ID = @Subproject_ID and Lower(cash_balances.tilltype_Name) = 'pay';";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);
                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public void Insert_Payment(Double Amount, string Date, string Note, int MicroProject_ID, int SubProject_ID, int Beneficiary_ID, DataTable payments, int CashAccount_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";


            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Amount", Amount);

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
                if (SubProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@SubProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);
                }
                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);
                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);

                query = "START TRANSACTION;";

                query += @"SET @Till_ID = (SELECT till.ID
                            FROM account LEFT JOIN till ON till.Account_ID = account.ID
                            LEFT JOIN tilltypes ON till.Type_ID = tilltypes.ID
                            LEFT JOIN account_donor on account_donor.Account_id = account.ID
                            LEFT join (SELECT account_donor.Donor_ID AS 'DonorID', account.SubProject_ID AS 'SubProjectID' FROM beneficiary LEFT JOIN account ON beneficiary.Account_ID = account.ID  LEFT JOIN account_donor on account_donor.Account_id = account.ID WHERE beneficiary.MicroProject_ID =  @MicroProject_ID AND account.SubProject_ID =  @SubProject_ID) AS t1 ON account_donor.Donor_ID = t1.DonorID                           
                            WHERE
                            account.SubProject_ID = SubProjectID
                            AND tilltypes.Name = 'pay');";

                int numofinserts = 0;

                if (payments.Rows.Count > 0)
                {
                    for (int i = 0; i < payments.Rows.Count; i++)
                    {
                        if (Amount > 0)
                        {
                            int payment_id = payments.Rows[i].Field<Int32>("ID");

                            Double Payment_Amount = Double.Parse(payments.Rows[i].Field<Single>("Amount").ToString());
                            Double Paid_Amount = payments.Rows[i].Field<Double>("Paid_Amount");
                            Double Current_Payment;

                            if (i < payments.Rows.Count - 1 && Amount > Payment_Amount - Paid_Amount)
                                Current_Payment = Payment_Amount - Paid_Amount;
                            else
                                Current_Payment = Amount;

                            sc.Parameters.AddWithValue("@payment_id" + i, payment_id);
                            sc.Parameters.AddWithValue("@Amount" + i, Current_Payment);

                            if (i == 0)
                            {
                                query += @"SET @Action_ID = (
                                SELECT MAX(max_id) + 1 AS next_id
                                FROM (
                                    SELECT MAX(ID) AS max_id FROM action
                                    UNION ALL
                                    SELECT MAX(ID) FROM action_not_approved
                                    UNION ALL
                                    SELECT MAX(ID) FROM action_deleted
                                ) AS combined_max_ids);";
                            }

                            query += "SET @Action_ID" + i + " = @Action_ID;";


                            query += "INSERT INTO `action_not_approved`(`id`,`Till_ID`, `AType_ID`, `Amount`, `rate`,"
                                    + "`ActionDate`, `Note`, `IsReceipt`, `MicroProject_ID`,cashaccount_id, created_at, User_ID)"
                                    + "VALUES (@Action_ID" + i + ", @Till_ID , (Select id from actiontype where shortname = 'I'), @Amount" + i + ", 0, @Date, @Note, -1,"
                                    + "@MicroProject_ID, @CashAccount_ID, @Todaydate, @Uesr_ID);";

                            query += "SET @Action_ID = @Action_ID" + i + " +1;";

                            query += "INSERT INTO action_payment (action_id, payment_id) VALUES (@Action_ID" + i + ",@payment_id" + i + ");";

                            Amount = Amount - Current_Payment;

                            numofinserts++;
                        }
                    }
                }
                else
                {
                    query += "SET @Action_ID0 = (SELECT CASE WHEN IFNULL(MAX(action.ID),0) > (SELECT IFNULL(MAX(action_not_approved.ID),0) FROM action_not_approved) THEN IFNULL(MAX(action.ID),0)+1 ELSE (SELECT IFNULL(MAX(action_not_approved.ID),0)+1 FROM action_not_approved) END FROM action);";

                    query += "INSERT INTO `action_not_approved`(`id`,`Till_ID`, `AType_ID`, `Amount`, `rate`,"
                            + "`ActionDate`, `Note`, `IsReceipt`, `MicroProject_ID`,cashaccount_id, created_at, User_ID)"
                            + "VALUES (@Action_ID0, @Till_ID , (Select id from actiontype where shortname = 'I'), @Amount, 0, @Date, @Note, -1,"
                            + "@MicroProject_ID, @CashAccount_ID, @Todaydate, @Uesr_ID);";

                    query += "INSERT INTO action_payment (action_id, payment_id) VALUES (@Action_ID0,(select ID from payment where payment.Beneficiary_ID = @Beneficiary_ID and payment.Num = (select Max(num) from payment where payment.Beneficiary_ID = @Beneficiary_ID)));";

                    numofinserts++;
                }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                for (int i = 0; i < numofinserts; i++)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'action_not_approved', @Action_ID" + i + ", 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }

                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();
                Login.MyConn.Close();
            }
        }

        public void Update_Payment(int Action_ID, int Till_ID, Double Amount, string Date, string Note, int MicroProject_ID, int Beneficiary_ID, int CashAccount_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Till_ID", Till_ID);

                sc.Parameters.AddWithValue("@Amount", Amount);

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

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"UPDATE`action` SET `Amount` = @Amount,
                            `ActionDate` = @Date, `Note` = @Note, `MicroProject_ID` = @MicroProject_ID, cashaccount_id =@CashAccount_ID , updated_at =  @Todaydate, `edited_by` = @Uesr_ID  
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


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();
                Login.MyConn.Close();
            }
        }

        public void Update_Not_Approved_Payment(int Action_ID, int Till_ID, Double Amount, string Date, string Note, int MicroProject_ID, int Beneficiary_ID, int CashAccount_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Till_ID", Till_ID);

                sc.Parameters.AddWithValue("@Amount", Amount);

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

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"UPDATE`action_not_approved` SET `Amount` = @Amount,
                            `ActionDate` = @Date, `Note` = @Note, `MicroProject_ID` = @MicroProject_ID, cashaccount_id =@CashAccount_ID , updated_at =  @Todaydate, `edited_by` = @Uesr_ID  
                            WHERE action_not_approved.ID = @Action_ID;";


                query += "UPDATE `action_not_approved` SET " +
                        " `IsApproved`= 0" +
                        " ,`Approved_at`= null" +
                        " ,`Approved_by`= null" +
                        " WHERE ID = @Action_ID;";

                query += "UPDATE `action_not_approved` SET " +
                        " `IsApproved`= 0" +
                        " ,`Approved_at`= null" +
                        " ,`Approved_by`= null" +
                       " WHERE ObserveAcion_ID = @Action_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action_not_approved', @Action_ID, 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120; sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();
                Login.MyConn.Close();
            }
        }

        public void Delete_Payment(int ID, int Beneficiary_ID, bool Approved_Action, string Form_Name)
        {
          
                string query = "";
                string log_query = "";

                if (Login.BuildConnection())
                {
                    MySqlCommand sc = new MySqlCommand();

                    sc.Parameters.AddWithValue("@ID", ID);
                    sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                    sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                    string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                    sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                    sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);

                    query += "START TRANSACTION;";

                    if (Approved_Action)
                    {
                        query += "INSERT INTO action_deleted SELECT * FROM action WHERE action.id = @ID OR action.ObserveAcion_ID = @ID;";

                        query += "Update action_deleted set `deleted_at` = @Todaydate, `deleted_by` = @Uesr_ID WHERE action_deleted.id = @ID OR action_deleted.ObserveAcion_ID = @ID;";

                        query += "DELETE FROM action WHERE action.ObserveAcion_ID = @ID;";

                        query += "DELETE FROM action WHERE action.id  = @ID;";


                        //query += "DELETE FROM `action` WHERE action.ObserveAcion_ID = @ID;";

                        //query += "DELETE FROM `action` WHERE action.ID = @ID;";

                        //query += "DELETE FROM action_payment WHERE action_id = @ID;";

                    }
                    else
                    {
                        query += "INSERT INTO action_deleted SELECT * FROM action_not_approved WHERE action_not_approved.id = @ID OR action_not_approved.ObserveAcion_ID = @ID;";

                        query += "Update action_deleted set `deleted_at` = @Todaydate, `deleted_by` = @Uesr_ID WHERE action_deleted.id = @ID OR action_deleted.ObserveAcion_ID = @ID;";

                        query += "DELETE FROM action_not_approved WHERE action_not_approved.ObserveAcion_ID = @ID;";

                        query += "DELETE FROM action_not_approved WHERE action_not_approved.id  = @ID;";

                        //query += "DELETE FROM `action_not_approved` WHERE action_not_approved.ObserveAcion_ID = @ID;";

                        //query += "DELETE FROM `action_not_approved` WHERE action_not_approved.ID = @ID;";

                        //query += "DELETE FROM action_payment WHERE action_id = @ID;";

                    }


                    log_query = Regex.Replace(query, "'", "''");

                    foreach (MySqlParameter p in sc.Parameters)
                    {
                        log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                    }

                    //if(Approved_Action)
                    //{
                    //    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'action', @ID, 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    //}
                    //else
                    //{
                    //    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'action_not_approved', @ID, 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    //}
                    query += " COMMIT;";

                    sc.CommandTimeout = 120;sc.CommandText = query;
                    sc.Connection = Login.MyConn;
                    sc.ExecuteScalar();

                    Login.MyConn.Close();
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

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();
                Login.MyConn.Close();
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
                Login.MultiPayments_sc.CommandTimeout = 120; Login.MultiPayments_sc.CommandText = query;
                Login.MultiPayments_sc.Connection = Login.MyConn;

                Login.MultiPayments_sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }
    }
}
