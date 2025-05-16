using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class Beneficiary
    {

        public Beneficiary()
        { }

        private string name;

        public string Name
        {
            get { return name; }
            set
            {
                if (name != value) name = value;
            }
        }

        public DataTable Select(string Beneficiary_ID, string Account_ID, string SubProject_ID)
        {
            string query = "SELECT beneficiary.`ID`"
                                + ", `FirstName`, `LastName`, CONCAT(`FirstName`,' ',`FatherName`, ' ',`LastName`) as 'Name' "
                                + ", `FatherName`, `NationalNumber`, `Mobile`, `Partners`"
                                + ", `MicroProject_ID`, `Project_Name`, `Amount`"
                                + ", `Date`,`FirstPaymentDate`, (`ToReturn_Amount`/`Amount`)*100 as 'rate', `ToReturn_Amount`"
                                + ", (select sum(action.amount) from action_payment left join payment on payment.id = action_payment.payment_id left join action on action.id = action_payment.action_id  where payment.Beneficiary_ID = beneficiary.`ID`) as 'paidamount'"
                                + ", (select sum(action_not_approved.amount) from action_payment left join payment on payment.id = action_payment.payment_id left join action_not_approved on action_not_approved.id = action_payment.action_id  where payment.Beneficiary_ID = beneficiary.`ID`) as 'NotApproved_paidamount'"
                                + ", `payment`, `Payment_interval`, `Notes`"
                                + ", `ReceiptNumber`, `OrderNumber`, beneficiary.`Account_ID` "
                                + ", account.SubProject_ID "
                                + ", (select account.Name from account left join account_donor on account.id = account_donor.account_id where account_donor.account_id = beneficiary.Account_ID) as 'account' "
                                + ", account_donor.Donor_ID as 'Donor_ID'" 

                                + " FROM `beneficiary` left join `account` on account.ID = beneficiary.Account_ID"
                                + " LEFT JOIN account_donor ON account_donor.account_ID = account.ID";

            string condition = " where 1 ";

            if (Beneficiary_ID != "")
                condition += " and beneficiary.`ID` in (" + Beneficiary_ID + ")";

            if (Account_ID != "")
                condition += " and beneficiary.Account_ID in (" + Account_ID + ")";

            if (SubProject_ID != "" && SubProject_ID != "-1")
                condition += " and account.SubProject_ID in (" + SubProject_ID + ")";


            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public int Insert(string FirstName, string LastName, string FatherName, string NationalNumber, string Mobile, string Partners
            , int MicroProject_ID, string Project_Name, Double Amount, string Date, string FirstPaymentDate, Double ToReturn_Amount, float payment, int Payment_Interval
            , string Notes, string ReceiptNumber, string OrderNumber, int Account_ID, bool RegeneratePayments, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Beneficiary_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@FirstName", FirstName);
                sc.Parameters.AddWithValue("@LastName", LastName);
                sc.Parameters.AddWithValue("@FatherName", FatherName);
                sc.Parameters.AddWithValue("@NationalNumber", NationalNumber);
                sc.Parameters.AddWithValue("@Mobile", Mobile);
                sc.Parameters.AddWithValue("@Partners", Partners);
                if (MicroProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", MicroProject_ID);
                }

                sc.Parameters.AddWithValue("@Project_Name", Project_Name);
                sc.Parameters.AddWithValue("@Amount", Amount);
                if (Date == "")
                {
                    sc.Parameters.AddWithValue("@Date", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Date", Date);
                }
                if (FirstPaymentDate == "")
                {
                    sc.Parameters.AddWithValue("@FirstPaymentDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@FirstPaymentDate", FirstPaymentDate);
                }
                //sc.Parameters.AddWithValue("@rate", rate);
                sc.Parameters.AddWithValue("@ToReturn_Amount", ToReturn_Amount);
                sc.Parameters.AddWithValue("@payment", payment);
                sc.Parameters.AddWithValue("@Payment_Interval", Payment_Interval);
                //sc.Parameters.AddWithValue("@PaymentsCount", PaymentsCount);
                //sc.Parameters.AddWithValue("@remained_payments_count", remained_payments_count);
                sc.Parameters.AddWithValue("@Notes", Notes);
                sc.Parameters.AddWithValue("@ReceiptNumber", ReceiptNumber);
                sc.Parameters.AddWithValue("@OrderNumber", OrderNumber);
                if (Account_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Account_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                }

                sc.Parameters.AddWithValue("@BeginDate", (Convert.ToDateTime(FirstPaymentDate)).ToString("dd/MM/yyyy"));
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"INSERT INTO `beneficiary`(`FirstName`, `LastName`, `FatherName`, `NationalNumber`, `Mobile`, `Partners`,
                            `MicroProject_ID`, `Project_Name`, `Amount`, `Date`,`FirstPaymentDate`, `ToReturn_Amount`, `payment`, `Payment_Interval`, `Notes`, `ReceiptNumber`, `OrderNumber`, `Account_ID`)
                            VALUES(@FirstName, @LastName, @FatherName, @NationalNumber , @Mobile , @Partners , @MicroProject_ID , @Project_Name ,
                            @Amount , @Date , @FirstPaymentDate , @ToReturn_Amount , @payment , @Payment_Interval,
                            @Notes , @ReceiptNumber , @OrderNumber , @Account_ID);"; 

                query += " SET @Beneficiary_ID = Last_Insert_ID();";


                query += " SELECT @Beneficiary_ID;";

                if(RegeneratePayments)
                    query += "Call ReGenerate_allPayments(@Beneficiary_ID);";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'beneficiary', @Beneficiary_ID, CONCAT('beneficiary Name = ', @FirstName, ' ', @FatherName,' ', @LastName), CONCAT('Account ID = ',@Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                Beneficiary_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.MyConn.Close();
            }
            return Beneficiary_ID;
        }

        public void Update(int Beneficiary_ID, string FirstName, string LastName, string FatherName, string NationalNumber, string Mobile, string Partners
            , int MicroProject_ID, string Project_Name, Double Amount, string Date, string FirstPaymentDate, Double ToReturn_Amount, float payment, int Payment_Interval
            , string Notes, string ReceiptNumber, string OrderNumber, int Account_ID, int Original_MicroProject_ID, int SubProject_ID, bool RegeneratePayments, bool DeleteSchedualedPayments, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                sc.Parameters.AddWithValue("@FirstName", FirstName);
                sc.Parameters.AddWithValue("@LastName", LastName);
                sc.Parameters.AddWithValue("@FatherName", FatherName);
                sc.Parameters.AddWithValue("@NationalNumber", NationalNumber);
                sc.Parameters.AddWithValue("@Mobile", Mobile);
                sc.Parameters.AddWithValue("@Partners", Partners);
                if (MicroProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", MicroProject_ID);
                }
                if (Original_MicroProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Original_MicroProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Original_MicroProject_ID", Original_MicroProject_ID);
                }

                sc.Parameters.AddWithValue("@Project_Name", Project_Name);
                sc.Parameters.AddWithValue("@Amount", Amount);
                if (Date == "")
                {
                    sc.Parameters.AddWithValue("@Date", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Date", Date);
                }
                if (FirstPaymentDate == "")
                {
                    sc.Parameters.AddWithValue("@FirstPaymentDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@FirstPaymentDate", FirstPaymentDate);
                }

                sc.Parameters.AddWithValue("@ToReturn_Amount", ToReturn_Amount);
                sc.Parameters.AddWithValue("@payment", payment);
                sc.Parameters.AddWithValue("@Payment_Interval", Payment_Interval);
                //sc.Parameters.AddWithValue("@PaymentsCount", PaymentsCount);
                sc.Parameters.AddWithValue("@Notes", Notes);
                sc.Parameters.AddWithValue("@ReceiptNumber", ReceiptNumber);
                sc.Parameters.AddWithValue("@OrderNumber", OrderNumber);
                if (Account_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Account_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                }
                if (SubProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@SubProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);
                }

                sc.Parameters.AddWithValue("@BeginDate", (Convert.ToDateTime(FirstPaymentDate)).ToString("dd/MM/yyyy"));
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                query = "START TRANSACTION;";

                query += @"UPDATE `beneficiary` SET  `FirstName`= @FirstName ,`LastName`= @LastName ,`FatherName`=@FatherName ,`NationalNumber`= @NationalNumber 
                       ,`Mobile`= @Mobile ,`Partners`= @Partners,`MicroProject_ID`=@MicroProject_ID ,`Project_Name`= @Project_Name ,`Amount`= @Amount 
                       ,`Date`=  @Date  ,`FirstPaymentDate`=   @FirstPaymentDate ,`ToReturn_Amount`=  @ToReturn_Amount ,`payment`=  @payment, `Payment_Interval` = @Payment_Interval
                       ,`Notes`=  @Notes ,`ReceiptNumber`=  @ReceiptNumber ,`OrderNumber`=  @OrderNumber ,`Account_ID` = @Account_ID 
                       WHERE ID = @Beneficiary_ID;";


                query += "UPDATE action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID SET action.MicroProject_ID = @MicroProject_ID WHERE MicroProject_ID = @Original_MicroProject_ID AND account.SubProject_ID = @SubProject_ID;";
                query += "UPDATE action_not_approved LEFT JOIN till ON action_not_approved.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID SET action_not_approved.MicroProject_ID = @MicroProject_ID WHERE MicroProject_ID = @Original_MicroProject_ID AND account.SubProject_ID = @SubProject_ID;";


                if (RegeneratePayments)
                    query += "Call ReGenerate_allPayments(@Beneficiary_ID);";

                if (DeleteSchedualedPayments)
                    query += "DELETE `payment` FROM `payment` LEFT JOIN action_payment ON action_payment.payment_id = payment.ID WHERE payment.Beneficiary_ID = @Beneficiary_ID AND action_payment.action_id IS null;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'beneficiary', @Beneficiary_ID, CONCAT('beneficiary Name = ', @FirstName, ' ', @FatherName,' ', @LastName), CONCAT('account ID = ',@Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (RegeneratePayments)
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'payment', '','Payments Regenerated', CONCAT('beneficiary ID = ', @Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                if (DeleteSchedualedPayments)
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'payment', '','Schedualed Payments Deleted', CONCAT('beneficiary ID = ', @Beneficiary_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }

        public void Delete(int Beneficiary_ID, string SubProject_ID, string Name, int Account_ID, string Form_Name)
        {
         
                string query = "";
                string log_query = "";

                if (Login.BuildConnection())
                {
                    MySqlCommand sc = new MySqlCommand();

                    sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
                    sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);
                    sc.Parameters.AddWithValue("@Name", Name);
                    sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                    sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                    query += "START TRANSACTION;";

                    query += "SET @MicroProject_ID = (SELECT MicroProject_ID FROM beneficiary WHERE ID = @Beneficiary_ID);";

                    query += "DELETE action FROM action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN account on till.Account_ID = account.ID WHERE action.MicroProject_ID =  @MicroProject_ID AND account.SubProject_ID = @SubProject_ID;";

                    query += "DELETE action_not_approved FROM action_not_approved LEFT JOIN till ON action_not_approved.Till_ID = till.ID LEFT JOIN account on till.Account_ID = account.ID WHERE action_not_approved.MicroProject_ID =  @MicroProject_ID AND account.SubProject_ID = @SubProject_ID;";


                    query += "DELETE FROM `beneficiary` WHERE ID = @Beneficiary_ID;";

                    log_query = Regex.Replace(query, "'", "''");

                    foreach (MySqlParameter p in sc.Parameters)
                    {
                        log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                    }

                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'beneficiary', @Beneficiary_ID, CONCAT('beneficiary Name = ', @Name), CONCAT('account ID = ',@Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                    query += " COMMIT;";

                    sc.CommandTimeout = 120;sc.CommandText = query;
                    sc.Connection = Login.MyConn;
                    sc.ExecuteScalar();

                    Login.MyConn.Close();
                }
            
        }

        public void Delete_Multi_Beneficiaries()
        {
            string query = "";


            query += "START TRANSACTION;";

            query += string.Join(Environment.NewLine, Login.DeleteMultiBeneficiary_List.ToArray());
            query += " COMMIT;";
            if (Login.BuildConnection())
            {
                Login.DeleteMultiBeneficiary_sc.CommandTimeout = 120; Login.DeleteMultiBeneficiary_sc.CommandText = query;
                Login.DeleteMultiBeneficiary_sc.Connection = Login.MyConn;

                Login.DeleteMultiBeneficiary_sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }


        public DataTable Select_Account_beneficiares(int Account_ID, int SubProject_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Account_ID", Account_ID);
            sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);

            string query = "CALL `get_account_beneficiaries` (@Account_ID, @SubProject_ID);";

            sc.CommandTimeout = 120;sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public DataTable Select_account_view(string Account_IDs)
        {
            string query = @"SELECT * from account_view ";
            string condition = " where 1 ";

            if (Account_IDs != "")
            {
                condition += " and account_view.ID in (" + Account_IDs + ")";
            }
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

        public DataTable CHeck_MicroProjectNum(int Beneficiary_ID, int Subproject_ID, int MicroPorject_ID)
        {
            string query = @"SELECT beneficiary.`ID`          
                                FROM `beneficiary` left join `account` on account.ID = beneficiary.Account_ID 
                                WHERE beneficiary.MicroProject_ID = @MicroPorject_ID AND account.SubProject_ID = @Subproject_ID";


            if (Beneficiary_ID != -1)
            {
                query += " AND beneficiary.id != @Beneficiary_ID";
            }

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Subproject_ID", Subproject_ID);
                sc.Parameters.AddWithValue("@MicroPorject_ID", MicroPorject_ID);
                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }


        public DataTable Select_Subproject_beneficiares(int SubProject_ID)
        {
            MySqlCommand sc = new MySqlCommand();

            sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);

            string query = @"SELECT beneficiary.ID as 'beneficiary_ID', beneficiary.MicroProject_ID as 'MicroProject_ID'         
                                FROM `beneficiary` left join `account` on account.ID = beneficiary.Account_ID 
                                WHERE account.SubProject_ID = @Subproject_ID";

            sc.CommandTimeout = 120;sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public void Insert_Multi_beneficiares()
        {
            string query = "";


            query += "START TRANSACTION;";
            query += string.Join(Environment.NewLine, Login.MultiBeneficiary_List.ToArray());
            query += " COMMIT;";

            if (Login.BuildConnection())
            {
                Login.MultiBeneficiary_sc.CommandTimeout = 120; Login.MultiBeneficiary_sc.CommandText = query;
                Login.MultiBeneficiary_sc.Connection = Login.MyConn;

                Login.MultiBeneficiary_sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }
    }
}
