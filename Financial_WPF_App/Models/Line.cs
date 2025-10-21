using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class Line
    {
        public DataTable Select(string ids, string categories)
        {
            //check user from database
            string query = "select ID " +
                ", (Select Name from linecategory where linecategory.ID = line.Category_ID) as 'Category', Name, Category_ID " +
                ", (Select GROUP_CONCAT(project.Name) from project left join line_project on line_project.project_id = project.id where line_project.line_id = line.ID) as 'Projects'" +
                 ", (Select GROUP_CONCAT(project.id) from project left join line_project on line_project.project_id = project.id where line_project.line_id = line.ID) as 'Projects_id'" +
                " from `line`";
            string condition = " where 1 ";
            if (ids != "")
            {
                condition += " and ID in (" + ids + ") ";
            }

            if (categories != "")
            {
                condition += " and Category_ID in (" + categories + ") ";
            }
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

        public DataTable Select_Fixed_Assets()
        {
            //check user from database
            string query = @"SELECT 
                            line.id AS `Line_ID`,
                            line.Name AS `Line`,
                            line.estimated_lifetime AS `Estimated Lifetime` 
                            FROM 
                            line 
                            LEFT JOIN linecategory ON linecategory.ID = line.Category_ID
                            WHERE 
                            lower(linecategory.Name) = 'fixed assets';";

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

        public DataTable Select_Fixed_Assets_Report(int TillType_ID)
        {
            //check user from database
            string query = @"SELECT 
                                city.Name AS 'City',
                                project.Name AS 'Program',
                                subproject.Name AS 'Project',
                                line.Name AS 'Item',
                                action.ActionDate AS 'Entry Date',
                                DATE_FORMAT(action.ActionDate, ""%c/%Y"") as 'Entry Month',
                                DATE_FORMAT(action.ActionDate, ""%Y"") as 'Entry Year',
                                action.Amount AS 'Purchase Price - Syr', 
                                action.Amount / all_avgrates.Value AS 'Purchase Price - Foreign currency',
                                line.estimated_lifetime AS 'Estimated lifetime - Months',
                                TIMESTAMPDIFF(MONTH, action.ActionDate, NOW()) AS 'Current lifetime - Months',
                                action.Amount - ((action.Amount/line.estimated_lifetime) * TIMESTAMPDIFF(MONTH, action.ActionDate, NOW())) AS 'Current Estimated Price - Syr',
                                (action.Amount / all_avgrates.Value) - (((action.Amount / all_avgrates.Value)/line.estimated_lifetime) * TIMESTAMPDIFF(MONTH, action.ActionDate, NOW())) AS 'Current Estimated Price - Foreign currency'
                                FROM action LEFT JOIN till ON till.ID = action.Till_ID
                                LEFT JOIN budget_line on action.BudgetLine_ID = budget_line.ID
                                LEFT JOIN line ON budget_line.Line_ID = line.ID
                                LEFT JOIN linecategory on line.Category_ID = linecategory.ID
                                LEFT JOIN all_avgrates ON action.Rate_ID = all_avgrates.id
                                LEFT JOIN tilltypes ON all_avgrates.FromTill = tilltypes.ID
                                LEFT JOIN account ON till.Account_ID = account.ID
                                LEFT JOIN subproject on subproject.ID = account.SubProject_ID
                                LEFT JOIN project on subproject.Project_ID = project.ID
                                LEFT JOIN city on subproject.City_ID = city.ID

                                WHERE LOWER(linecategory.Name) = 'fixed assets' AND tilltypes.id = @TillType_ID

                                ORDER BY city.ID, project.ID, subproject.ID, account.ID;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {

                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@TillType_ID", TillType_ID);

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


        public DataTable Select_Lines_As_Employee_Position()
        {

            string query = @"SELECT line.id as 'line_ID', line.Name as 'Line'
                            FROM line
                            left join linecategory on line.Category_id = linecategory.id
                            WHERE linecategory.Name = 'Human Resources';";


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

        public int Insert(string name, int category_ID, List<string> Add_Projects, string Form_Name)
        {
            string query = "";
            string log_query = "";


            int Line_ID = -1;

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

                query += @"INSERT INTO `line`(`Name`,`Category_ID`) VALUES (@name,@category_ID);";

                query += " SET @Line_ID = Last_Insert_ID();";


                if (Add_Projects.Count != 0)
                {
                    for (int i = 0; i < Add_Projects.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@ProjectID" + i, Add_Projects[i]);
                        query += "INSERT INTO `line_project` (`Line_ID`,`Project_ID`) VALUES " +
                         "(@Line_ID, @ProjectID" + i + ");";

                        query += " SET @P_ID" + i + " = Last_Insert_ID();";
                    }
                }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'line', @Line_ID, CONCAT('line Name = ', @name), CONCAT('category ID = ', @category_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " SELECT @Line_ID;";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Line_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }
            return Line_ID;
        }

        public void Update(int Line_ID, string name, int category_ID, List<string> Add_Projects, List<string> Delete_Projects, string Form_Name)
        {
            string query = "";
            string log_query = "";



            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@name", name);

                sc.Parameters.AddWithValue("@Line_ID", Line_ID);

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

                query += @"UPDATE `line` SET `Name`= @name, Category_ID = @category_ID WHERE ID = @Line_ID;";

                if (Add_Projects.Count != 0)
                {
                    for (int i = 0; i < Add_Projects.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@ProjectID" + i, Add_Projects[i]);
                        query += "INSERT INTO `line_project` (`Line_ID`,`Project_ID`) VALUES " +
                         "(@Line_ID, @ProjectID" + i + ");";
                    }
                }

                if (Delete_Projects.Count != 0)
                {
                    for (int i = 0; i < Delete_Projects.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@DeleteProjectID" + i, Delete_Projects[i]);
                        query += " Delete from `line_project` WHERE Line_ID = @Line_ID and Project_ID = @DeleteProjectID" + i + ";";
                    }
                }


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'line', @Line_ID, CONCAT('line Name = ', @name), CONCAT('category ID = ', @category_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }

        public void Delete(int ID, string Name, int Category_ID, string Form_Name)
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

                query += "DELETE FROM `line` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'line', @ID, CONCAT('line Name = ', @name), CONCAT('category ID = ', @category_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


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
