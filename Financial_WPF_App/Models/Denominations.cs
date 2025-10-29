using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    internal class Denominations
    {

        public Denominations() { }
        public int ID { get; set; }
        public int Currency_ID { get; set; }
        public string Denomination { get; set; }


        public DataTable Select(bool WithNoDenomination)
        {
            //check user from database
            string query = "select d.id as 'ID',d.`currency_tilltype_id` as 'Currency_ID', t.name as 'Currency',case when d.name = 0 then 'Uncounted' else d.name end as 'Denomination' from `denominations` d LEFT JOIN tilltypes t on t.ID = d.currency_tilltype_id ";

            if (!WithNoDenomination)
                query += " where d.name <> 0 ";

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
        public DataTable SelectTillTypes()
        {
            string query = "SELECT id as 'ID', name as 'Name' FROM tilltypes where id <> 1 ORDER BY name;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                using (MySqlCommand sc = new MySqlCommand(query, Login.MyConn))
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter(sc))
                    {
                        da.Fill(dt);
                    }
                }
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public int Insert(string denomination, int currency_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";


            int ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@denomination", denomination);
                if (currency_ID == -1)
                {
                    sc.Parameters.AddWithValue("@currency_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@currency_ID", currency_ID);
                }
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                query = "START TRANSACTION;";

                query += @"INSERT INTO `denominations`(`name`,`currency_tilltype_id`) VALUES (@denomination,@currency_ID);";

                query += " SET @ID = Last_Insert_ID();";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'denomination', @ID, CONCAT('Denomination Name = ', @denomination), CONCAT('Currency ID = ', @currency_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " SELECT @ID;";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }
            return ID;
        }

        public void Update(int ID, string denomination, int currency_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";



            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@denomination", denomination);

                sc.Parameters.AddWithValue("@ID", ID);

                if (currency_ID == -1)
                {
                    sc.Parameters.AddWithValue("@currency_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@currency_ID", currency_ID);
                }

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"UPDATE `denominations` SET `Name`= @denomination, `currency_tilltype_id` = @currency_ID WHERE ID = @ID;";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'denomination', @ID, CONCAT('Denomination Name = ', @denomination), CONCAT('Currency ID = ', @currency_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }

        public void Delete(int ID, string denomination, int currency_ID, string Form_Name)
        {

            string query = "";
            string log_query = "";


            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@denomination", denomination);
                sc.Parameters.AddWithValue("@currency_ID", currency_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += "START TRANSACTION;";

                query += "DELETE FROM `denominations` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'denomination', @ID, CONCAT('Denomination Name = ', @denomination), CONCAT('Currency ID = ', @currency_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }
    }
}
