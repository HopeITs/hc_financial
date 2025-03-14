using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class LineCategory
    {
        public DataTable Select(string ids)
        {
            //check user from database
            string query = "select ID, Name" +
                ", case Type when 0 then 'Equipping the place' else 'Running cost' end as 'Type'" +
                ", case Kind when 0 then 'Profit' else 'Budget' end as 'Kind' from `linecategory`";

            string condition = " where 1 ";

            if (ids != "")
            {
                condition += " and ID in (" + ids + ") ";
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

        public int Insert(string name, int type, int kind, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int LineCat_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@type", type);
                sc.Parameters.AddWithValue("@kind", kind);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"INSERT INTO `linecategory`(`Name`,`Type`,`Kind`) VALUES (@name,@type,@kind);";

                query += " SET @LineCat_ID = Last_Insert_ID();";

                query += " SELECT @LineCat_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'linecategory', @LineCat_ID, CONCAT('linecategory Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                LineCat_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.MyConn.Close();
            }
            return LineCat_ID;
        }

        public void Update(int LineCat_ID, string name, int type, int kind, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@LineCat_ID", LineCat_ID);
                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@type", type);
                sc.Parameters.AddWithValue("@kind", kind);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"UPDATE `linecategory` SET `Name` = @name , Type = @type , Kind = @kind WHERE ID = @LineCat_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'linecategory', @LineCat_ID, CONCAT('linecategory Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }

        public void Delete(int ID,string Name, string Form_Name)
        {
            if (Login.BuildConnection())
            {
                string query = "";
                string log_query = "";

                if (Login.BuildConnection())
                {
                    MySqlCommand sc = new MySqlCommand();

                    sc.Parameters.AddWithValue("@ID", ID);
                    sc.Parameters.AddWithValue("@name", Name);

                    sc.Parameters.AddWithValue("@Form_Name", Form_Name);


                    query += "START TRANSACTION;";

                    query += "DELETE FROM `linecategory` WHERE ID = @ID;";


                    log_query = Regex.Replace(query, "'", "''");

                    foreach (MySqlParameter p in sc.Parameters)
                    {
                        log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                    }

                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'linecategory', @ID, CONCAT('linecategory Name = ', @name),'' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                    query += " COMMIT;";

                    query += " COMMIT;";

                    sc.CommandTimeout = 120;sc.CommandText = query;
                    sc.Connection = Login.MyConn;
                    sc.ExecuteScalar();

                    Login.MyConn.Close();
                }
            }
        }
    }
}
