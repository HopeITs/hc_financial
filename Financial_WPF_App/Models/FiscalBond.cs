using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class FiscalBond
    {

        public DataTable Select(string ids, string names, string user_id, string Subproject_ID, bool ShowClosed)
        {
            //check user from database
            string query = @"select fiscalbond.* 
                             ,subproject.Name as 'SubProject',subproject.ID as 'SubProject_ID', category.name as 'category'
                            , tilltypes.Name as 'Till', till.ID as 'Till_ID'
                            , account.Name as 'Account', account.ID as 'Account_ID', accounttype.name as 'accounttype'
                            , Case IsClosed when 0 then 'Open' when 1 then 'CLOSED' else 'N/A' end as 'IsClosed' 
                             from `fiscalbond` 
                             left join `account` on fiscalbond.Account_ID = account.ID 
                             left join accounttype on accounttype.id = account.Accounttype_id
                             left join `subproject` on account.SubProject_ID = subproject.ID
                             left join `project` on project.id = subproject.project_id
                             left join `category` on category.id = project.category_id
                             left join till on till.id = fiscalbond.Till_ID
                             left join tilltypes on till.type_id = tilltypes.id";


            string condition = " where 1 ";

            if (ids != "")
            {
                condition += " and fiscalbond.ID in (" + ids + ") ";
            }

            if (names != "")
            {
                condition += " and fiscalbond.Name in (" + names + ") ";
            }
   
            if (Subproject_ID != "")
            {
                condition += " and SubProject_ID = @Subproject_ID";
            }

            if (!ShowClosed) condition += " and account.IsClosed = 0 ";

            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@user_id", user_id);
                sc.Parameters.AddWithValue("@Subproject_ID", Subproject_ID);

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public int Insert(string Name, string Date, string Notes, int Account_ID, int Till_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";
            int fiscalbond = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Name", Name);
                sc.Parameters.AddWithValue("@Date", Date);
                sc.Parameters.AddWithValue("@Notes", Notes);

                if (Account_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Account_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                }


                if (Till_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Till_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Till_ID", Till_ID);
                }

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "INSERT INTO `fiscalbond`(`Name`, `Date`, `Notes`, `Account_ID`, `Till_ID`) VALUES (@Name, @Date, @Notes, @Account_ID, @Till_ID);";

                query += " SET @FiscalBond_ID = Last_Insert_ID();";

                query += " SELECT @FiscalBond_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'fiscalbond', @FiscalBond_ID, CONCAT('fiscalbond Name = ', @Name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                fiscalbond = Convert.ToInt32(sc.ExecuteScalar());

                Login.MyConn.Close();
            }

            return fiscalbond;
        }

        public void Update(int ID, string Name, string Date, string Notes, int Account_ID, int Till_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Name", Name);
                sc.Parameters.AddWithValue("@Date", Date);
                sc.Parameters.AddWithValue("@Notes", Notes);

                if (Account_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Account_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                }
                if (Till_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Till_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Till_ID", Till_ID);
                }
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);


                query = "START TRANSACTION;";

                query += "UPDATE `fiscalbond` SET  `Name`=@Name ,`Date`=@Date ,`Notes`=@Notes ,`Account_ID`=@Account_ID,`Till_ID`=@Till_ID WHERE ID =@ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'fiscalbond', @ID, CONCAT('fiscalbond Name = ', @Name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }

        public void Delete(int ID, string Name, string Form_Name)
        {
            if (Login.BuildConnection())
            {
                string query = "";
                string log_query = "";

                if (Login.BuildConnection())
                {
                    MySqlCommand sc = new MySqlCommand();

                    sc.Parameters.AddWithValue("@ID", ID);
                    sc.Parameters.AddWithValue("@Name", Name);

                    sc.Parameters.AddWithValue("@Form_Name", Form_Name);


                    query += "START TRANSACTION;";

                    query += "set @Account_ID = (select fiscalbond.Account_ID from fiscalbond where fiscalbond.id = @ID);";

                    query += "DELETE FROM `fiscalbond` WHERE ID = @ID;";

                    log_query = Regex.Replace(query, "'", "''");

                    foreach (MySqlParameter p in sc.Parameters)
                    {
                        log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                    }

                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'fiscalbond', @ID, CONCAT('fiscalbond Name = ', @Name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


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
