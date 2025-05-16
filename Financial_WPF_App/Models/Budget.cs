using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class Budget
    {
        public DataTable Select(int Account_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Account_ID", Account_ID);

            string query = @" CALL Select_Budget_Details(@Account_ID);";

            sc.CommandTimeout = 120;sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_budget_donor(int Account_ID, int Budget_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Account_ID", Account_ID);
            sc.Parameters.AddWithValue("@Budget_ID", Budget_ID);



            string query = @" SELECT budget_donor.id AS 'Budget_Donor_ID', donor.id as 'Donor_ID', donor.Name AS 'Donor',
                        budget_donor.rate AS 'Rate', budget_donor.budget_id AS 'budget_id' 
                        FROM account LEFT JOIN budget ON budget.Account_ID = account.ID 
                        LEFT JOIN budget_donor on budget_donor.budget_id = budget.ID 
                        LEFT JOIN donor ON budget_donor.donor_id = donor.ID WHERE budget_donor.id is not null";

            if (Budget_ID != -1)
                query += " and budget.id = @Budget_ID";

            if(Account_ID != -1)
                query += " and account.ID = @Account_ID";

            sc.CommandTimeout = 120;sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public int Insert(string Name, string BeginDate, string EndDate, int Account_ID, int Budget_Type_ID, DataTable Add_Donors, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Budget_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Name", Name);

                if (BeginDate == "")
                {
                    sc.Parameters.AddWithValue("@BeginDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@BeginDate", BeginDate);
                }

                if (EndDate == "")
                {
                    sc.Parameters.AddWithValue("@EndDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@EndDate", EndDate);
                }

                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@Budget_Type_ID", Budget_Type_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = " START TRANSACTION;";

                query += @" INSERT INTO `budget`(`Name`, `BeginDate`, `EndDate`, `Account_ID`, `Budget_Type_ID`) VALUES (@Name, @BeginDate, @EndDate, @Account_ID, @Budget_Type_ID);";

                query += " SET @Budget_ID = Last_Insert_ID();";

                if (Add_Donors.Rows.Count > 0)
                    for (int i = 0; i < Add_Donors.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@AddDonor_ID" + i, Add_Donors.Rows[i]["Donor_ID"]);
                        sc.Parameters.AddWithValue("@AddDonor_Name" + i, Add_Donors.Rows[i]["donor"]);
                        sc.Parameters.AddWithValue("@AddDonor_Rate" + i, Add_Donors.Rows[i]["rate"]);

                        query += @" INSERT INTO `budget_donor`(`budget_id`, `donor_id`, `rate`) VALUES (@Budget_ID, @AddDonor_ID" + i + ", @AddDonor_Rate" + i + ");";
                        query += " SET @budget_donor_id" + i + " = Last_Insert_ID();";

                    }

                query += " SELECT @Budget_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'budget', @Budget_ID, CONCAT('budget Name = ', @Name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (Add_Donors.Rows.Count > 0)
                    for (int i = 0; i < Add_Donors.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'budget_donor', @budget_donor_id" + i + " ,  CONCAT('Dononr Name = ',  @AddDonor_Name" + i + "), CONCAT('Auto created with budget ID = ', @Budget_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }

                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                Budget_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }
            return Budget_ID;
        }

        public void Update(int ID, string Name, int Account_ID, string BeginDate, string EndDate, int Budget_Type_ID, DataTable Add_Donors, DataTable Delete_Donors_dt, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();


                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Name", Name);
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);

                if (BeginDate == "")
                {
                    sc.Parameters.AddWithValue("@BeginDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@BeginDate", BeginDate);
                }

                if (EndDate == "")
                {
                    sc.Parameters.AddWithValue("@EndDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@EndDate", EndDate);
                }
                sc.Parameters.AddWithValue("@Budget_Type_ID", Budget_Type_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "UPDATE `budget` SET `Name` = @Name, `BeginDate`= @BeginDate, `EndDate`= @EndDate, `Budget_Type_ID` = @Budget_Type_ID WHERE ID =@ID;";

                if (Delete_Donors_dt.Rows.Count > 0)
                    for (int i = 0; i < Delete_Donors_dt.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@DeleteBudgetDonor_ID" + i, Delete_Donors_dt.Rows[i]["Budget_Donor_ID"]);
                        sc.Parameters.AddWithValue("@DeleteDonor_ID" + i, Delete_Donors_dt.Rows[i]["Donor_ID"]);
                        sc.Parameters.AddWithValue("@DeleteDonor_Name" + i, Delete_Donors_dt.Rows[i]["donor"]);

                        query += @" DELETE FROM budget_donor where id = @DeleteBudgetDonor_ID" + i + ";";
                    }

                if (Add_Donors.Rows.Count > 0)
                    for (int i = 0; i < Add_Donors.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@AddDonor_ID" + i, Add_Donors.Rows[i]["Donor_ID"]);
                        sc.Parameters.AddWithValue("@AddDonor_Name" + i, Add_Donors.Rows[i]["donor"]);
                        sc.Parameters.AddWithValue("@AddDonor_Rate" + i, Add_Donors.Rows[i]["rate"]);

                        query += @" INSERT INTO `budget_donor`(`budget_id`, `donor_id`, `rate`) VALUES (@ID, @AddDonor_ID" + i + ", @AddDonor_Rate" + i + ");";
                        query += " SET @budget_donor_id" + i + " = Last_Insert_ID();";

                    }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'budget', @ID, CONCAT('budget Name = ', @Name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                if (Delete_Donors_dt.Rows.Count > 0)
                    for (int i = 0; i < Delete_Donors_dt.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'budget_donor', @DeleteBudgetDonor_ID" + i + " ,  CONCAT('Dononr Name = ',  @DeleteDonor_Name" + i + "), CONCAT('Auto created with budget ID = ', @ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }

                if (Add_Donors.Rows.Count > 0)
                    for (int i = 0; i < Add_Donors.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'budget_donor', @budget_donor_id" + i + " ,  CONCAT('Dononr Name = ',  @AddDonor_Name" + i + "), CONCAT('Auto created with budget ID = ', @ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }

                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public void Delete(int ID, int Account_ID, string Name, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@Name", Name);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += "START TRANSACTION;";

                query += "DELETE FROM `budget` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'budget', @ID, CONCAT('budget Name = ', @Name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }

        public void Insert_Budgt_AvgetRate(int Budget_ID, int AvgRate_ID, int Account_ID, string BudgetName, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Budget_ID", Budget_ID);
                sc.Parameters.AddWithValue("@AvgRate_ID", AvgRate_ID);
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@BudgetName", BudgetName);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"INSERT INTO `budgt_avgrate`(`budget_id`, `AvgRate_ID`) VALUES (@Budget_ID, @AvgRate_ID);";

                query += " SET @Budget_AvgRate_ID = Last_Insert_ID();";

                query += " SELECT @Budget_AvgRate_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'budgt_avgrate', @Budget_AvgRate_ID, CONCAT('budget Name = ', @BudgetName), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public void Delete_Budgt_AvgetRate(int Budget_AvgRate_ID, int Account_ID, string BudgetName, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Budget_AvgRate_ID", Budget_AvgRate_ID);
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@BudgetName", BudgetName);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += "START TRANSACTION;";

                query += "DELETE FROM `budgt_avgrate` WHERE ID = @Budget_AvgRate_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'budgt_avgrate', @Budget_AvgRate_ID, CONCAT('budget BudgetName = ', @BudgetName), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }

        public DataTable Select_Budgt_AvgetRate(string Budget_ID)
        {
            string query = @"SELECT budgt_avgrate.id AS 'budget_avgrate_id', budgt_avgrate.budget_id AS 'Budget_ID',
                                all_avgrates.ID AS 'AvgRate_ID', all_avgrates.Name AS 'AvgRate', all_avgrates.Value AS 'Value',
                                tilltypes.Name as 'FromTill'
                                FROM budgt_avgrate LEFT JOIN all_avgrates ON budgt_avgrate.AvgRate_ID = all_avgrates.id
                                left join tilltypes on all_avgrates.FromTill = tilltypes.id";

            string condition = " where budgt_avgrate.budget_id = @Budget_ID ";

            query += condition;
            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Budget_ID", Budget_ID);


                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_Lines_ExpensesReport(int Account_ID, int Budget_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Account_ID", Account_ID);
            sc.Parameters.AddWithValue("@Budget_ID", Budget_ID);

            string query = "CALL `Lines_ExpensesReport`(@Account_ID,@Budget_ID);";

            sc.CommandTimeout = 120;sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_LineTypes_ExpensesReport(int Account_ID, int Budget_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Account_ID", Account_ID);
            sc.Parameters.AddWithValue("@Budget_ID", Budget_ID);

            string query = "CALL `LineTypes_ExpensesReport`(@Account_ID,@Budget_ID);";

            sc.CommandTimeout = 120;sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }



    }
}
