using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class LineCategoryType
    {
        public LineCategoryType()
        {
        }

        public DataTable Select()
        {
            string query = "SELECT ID, Name FROM `linecategorytype` ORDER BY ID;";
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

        public int Insert(string name, string Form_Name)
        {
            string query = "";
            string log_query = "";
            int Type_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";
                query += " INSERT INTO `linecategorytype`(`Name`) VALUES (@name);";
                query += " SET @Type_ID = Last_Insert_ID();";
                query += " SELECT @Type_ID;";

                log_query = Regex.Replace(query, "'", "''");
                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'linecategorytype', @Type_ID, CONCAT('Budget Line Type Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Type_ID = Convert.ToInt32(sc.ExecuteScalar());
                Login.CloseAndClearConnection();
            }

            return Type_ID;
        }

        public void Update(int Type_ID, string name, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Type_ID", Type_ID);
                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";
                query += " UPDATE `linecategorytype` SET `Name` = @name WHERE ID = @Type_ID;";

                log_query = Regex.Replace(query, "'", "''");
                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'linecategorytype', @Type_ID, CONCAT('Budget Line Type Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();
                Login.CloseAndClearConnection();
            }
        }

        public void Delete(int Type_ID, string name, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Type_ID", Type_ID);
                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";
                query += " SET @UsedCount = (SELECT COUNT(*) FROM `linecategory` WHERE `Type_ID` = @Type_ID);";
                query += " DELETE FROM `linecategorytype` WHERE ID = @Type_ID AND @UsedCount = 0;";
                query += " SELECT @UsedCount;";

                log_query = Regex.Replace(query, "'", "''");
                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) SELECT @Form_Name, 'DELETE', 'linecategorytype', @Type_ID, CONCAT('Budget Line Type Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "' WHERE @UsedCount = 0;";
                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                int usedCount = Convert.ToInt32(sc.ExecuteScalar());
                Login.CloseAndClearConnection();

                if (usedCount > 0)
                {
                    throw new InvalidOperationException("Cannot delete or update a parent row");
                }
            }
        }
    }
}
