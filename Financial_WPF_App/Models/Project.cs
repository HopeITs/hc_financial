using ControlzEx.Standard;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class Project
    {
        public Project()
        {

        }
        public int ID { get; set; }
        public string Name { get; set; }
        public int Category_ID { get; set; }

        public DataTable Select(string ids, string names, string category)
        {
            //check user from database
            string query = @"select `ID`, `Name`, `Category_ID` ,(select Name from category where ID = project.Category_ID) as 'Category' from `project`";
            string condition = " where 1 ";
            if (ids != "")
            {
                condition += " and ID in (" + ids + ") ";
            }

            if (names != "")
            {
                condition += " and Name in (" + names + ") ";
            }

            if (category != "")
            {
                condition += " and Category_ID in (SELECT category.ID FROM category WHERE category.Name = '" + category + "') ";
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


        public DataTable Select_Project_Catgory()
        {
            string query = @"select `ID`, `Name` from `category` where name in ('Micro Projects','Budget');";
 
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

        public int Insert(string name, int category_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";


            int ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@name", name);
                if (category_ID == -1)
                {
                    sc.Parameters.AddWithValue("@category_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@category_ID", category_ID);
                }
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                query = "START TRANSACTION;";

                query += @"INSERT INTO `project`(`Name`,`Category_ID`) VALUES (@name,@category_ID);";

                query += " SET @ID = Last_Insert_ID();";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'project', @ID, CONCAT('Project Name = ', @name), CONCAT('Category ID = ', @category_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " SELECT @ID;";

                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.MyConn.Close();
            }
            return ID;
        }

        public void Update(int ID, string name, int category_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";



            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@name", name);

                sc.Parameters.AddWithValue("@ID", ID);

                if (category_ID == -1)
                {
                    sc.Parameters.AddWithValue("@category_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@category_ID", category_ID);
                }

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"UPDATE `project` SET `Name`= @name, `Category_ID` = @category_ID WHERE ID = @ID;";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'project', @ID, CONCAT('Project Name = ', @name), CONCAT('Category ID = ', @category_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.MyConn.Close();
            }

        }

        public void Delete(int ID, string Name, int Category_ID, string Form_Name)
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
                    sc.Parameters.AddWithValue("@category_ID", Category_ID);
                    sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                    query += "START TRANSACTION;";

                    query += "DELETE FROM `project` WHERE ID = @ID;";

                    log_query = Regex.Replace(query, "'", "''");

                    foreach (MySqlParameter p in sc.Parameters)
                    {
                        log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                    }

                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'project', @ID, CONCAT('Project Name = ', @name), CONCAT('Category ID = ', @category_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


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
