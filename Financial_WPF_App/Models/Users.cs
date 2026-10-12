using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class Users
    {
        public DataTable Get_User(string username)
        {
            //, string password
            string query = @"select user.id, user.username, user.password, role.name,
                            (SELECT GROUP_CONCAT(cashaccount.ID) FROM cashaccount JOIN user_cashaccounts ON cashaccount.ID = user_cashaccounts.cashaccount_id WHERE user_cashaccounts.user_id = user.ID AND user_cashaccounts.Iscashier = 1) AS 'user_IsCashier',
                            (SELECT GROUP_CONCAT(account.ID) FROM account JOIN user_accounts ON account.ID = user_accounts.account_id JOIN accounttype ON account.Accounttype_id = accounttype.id WHERE accounttype.name = 'project' AND user_accounts.user_id = user.id) AS 'user_accounts',
                            (SELECT GROUP_CONCAT(cashaccount.ID) FROM cashaccount JOIN user_cashaccounts ON cashaccount.ID = user_cashaccounts.cashaccount_id WHERE user_cashaccounts.user_id = user.ID) AS 'user_cashaccounts',
                            (SELECT GROUP_CONCAT(DISTINCT subproject_id) FROM user_accounts WHERE user_accounts.user_id = user.id) AS 'user_subproject_id'

                            from `user` 
                            left join role on user.role = role.id 
                            where BINARY Username like @username;";
            //and Password like @password

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@username", username);
                //sc.Parameters.AddWithValue("@password", password);
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

        public DataTable Get_AlUsers()
        {
            string query = @"select user.id as 'user_id',user.username as 'username', user.Password as 'Password', role.ID as 'role_id', role.name as 'role',

                            (SELECT GROUP_CONCAT(user_accounts.subproject_id) FROM user_accounts WHERE user_accounts.user_id = user.id) AS 'user_subprojects',

                            (SELECT GROUP_CONCAT(account.ID) FROM account JOIN user_accounts ON account.ID = user_accounts.account_id JOIN accounttype ON account.Accounttype_id = accounttype.id WHERE accounttype.name = 'project' AND user_accounts.user_id = user.id) AS 'user_accounts',

                            (SELECT GROUP_CONCAT(account.ID) FROM account JOIN user_accounts ON account.ID = user_accounts.account_id JOIN accounttype ON account.Accounttype_id = accounttype.id WHERE accounttype.name = 'Personal' AND user_accounts.user_id = user.id) AS 'user_Personalaccounts',

                            (SELECT GROUP_CONCAT(account.ID) FROM account JOIN user_accounts ON account.ID = user_accounts.account_id JOIN accounttype ON account.Accounttype_id = accounttype.id WHERE accounttype.name = 'Exchange' AND user_accounts.user_id = user.id) AS 'user_Exchangeaccounts',

                            (SELECT GROUP_CONCAT(cashaccount.ID) FROM cashaccount JOIN user_cashaccounts ON cashaccount.ID = user_cashaccounts.cashaccount_id WHERE user_cashaccounts.user_id = user.ID) AS 'user_cashaccounts',

                            (SELECT GROUP_CONCAT(cashaccount.ID) FROM cashaccount JOIN user_cashaccounts ON cashaccount.ID = user_cashaccounts.cashaccount_id WHERE user_cashaccounts.user_id = user.ID AND user_cashaccounts.Iscashier = 1) AS 'user_IsCashier'

                            from `user` left join role on user.role = role.id order by user.username asc;";


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

        /// <summary>The currencies an approval limit can be set in (everything but the pay container).</summary>
        public DataTable Get_LimitCurrencies()
        {
            return Run(@"SELECT ID, Name FROM tilltypes WHERE LOWER(Name) <> 'pay'
                         ORDER BY (is_local_currency = 1) DESC, base_divisor DESC, ID;");
        }

        /// <summary>The per-currency approval limits (P3), one row per role and currency set.</summary>
        public DataTable Get_RoleCurrencyLimits()
        {
            return Run("SELECT Role_ID, TillType_ID, Approval_Limit FROM role_currency_limit;");
        }

        private static DataTable Run(string query)
        {
            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.CommandTimeout = 120;
                sc.CommandText = query;
                sc.Connection = Login.MyConn;
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Get_UserRoles()
        {
            string query = @"select * from `role` ORDER BY `role`.`ID` ASC;";


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

        public DataSet Get_All_Accounts()
        {

            string query = @"SELECT subproject.ID, subproject.Name FROM subproject;";

            query += @"SELECT account.ID, account.Name, account.Subproject_ID
                FROM account
                LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                LEFT JOIN till ON till.Account_ID = account.ID
                LEFT JOIN tilltypes ON tilltypes.ID = till.Type_ID
                WHERE account.Accounttype_id = (select id from accounttype where name = 'project')
				AND tilltypes.Name <> 'pay' AND account.is_administrative = 0
                GROUP BY account.ID;";

            query += @"SELECT account.ID, account.Name
                FROM account
                LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                WHERE account.Accounttype_id = (select id from accounttype where name = 'Exchange')
                GROUP BY account.ID;";

            query += @"SELECT cashaccount.ID, cashaccount.Name, 'Cashier' as 'isCashier'
                FROM cashaccount ORDER BY `ID` ASC;";

            query += @"SELECT account.ID, account.Name
                FROM account
                LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                WHERE account.Accounttype_id = (select id from accounttype where name = 'Personal')
                GROUP BY account.ID;";


            DataSet ds = new DataSet();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(ds);
                Login.CloseAndClearConnection();
            }
            return ds;

        }

        public int Insert_User(string username, string password, string role, int role_id, List<int> Subprojects,
        List<int> Accounts,
        List<int> CashAccounts,
        List<int> Cashier,
        List<int> PersonalAccounts,
        List<int> ExchangeAccounts, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int User_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@username", username);
                sc.Parameters.AddWithValue("@password", password);
                sc.Parameters.AddWithValue("@role_id", role_id);
                sc.Parameters.AddWithValue("@role", role);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"INSERT INTO user (Username, Password, role)
                            VALUES (@username,@password, @role_id);";

                query += " SET @user_ID = Last_Insert_ID();";

                if (Subprojects != null && Subprojects.Count > 0)
                {
                    for (int i = 0; i < Subprojects.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@subproject" + i, Subprojects[i]);
                        query += @" INSERT INTO `user_accounts`(`user_id`, `subproject_id`, `account_id`) VALUES (@user_ID, @subproject" + i + ",  null);";
                    }
                }

                if (Accounts != null && Accounts.Count > 0)
                {
                    for (int i = 0; i < Accounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@account" + i, Accounts[i]);
                        query += @" INSERT INTO `user_accounts`(`user_id`, `subproject_id`, `account_id`) VALUES (@user_ID, (select account.subproject_id from account where account.id = @account" + i + "), @account" + i + ");";
                    }
                }

                if (CashAccounts != null && CashAccounts.Count > 0)
                {
                    for (int i = 0; i < CashAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@cashaccount" + i, CashAccounts[i]);
                        if (Cashier.Contains(CashAccounts[i]))
                        {
                            sc.Parameters.AddWithValue("@IsCashies" + i, 1);
                        }
                        else
                            sc.Parameters.AddWithValue("@IsCashies" + i, 0);

                        query += @" INSERT INTO `user_cashaccounts`(`user_id`, `cashaccount_id`, `Iscashier`) VALUES (@user_ID, @cashaccount" + i + ", @IsCashies" + i + ");";
                    }
                }

                if (PersonalAccounts != null && PersonalAccounts.Count > 0)
                {
                    for (int i = 0; i < PersonalAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@PersonalAccount" + i, PersonalAccounts[i]);
                        query += @" INSERT INTO `user_accounts`(`user_id`, `subproject_id`, `account_id`) VALUES (@user_ID, null, @PersonalAccount" + i + ");";
                    }
                }

                if (ExchangeAccounts != null && ExchangeAccounts.Count > 0)
                {
                    for (int i = 0; i < ExchangeAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@ExchangeAccount" + i, ExchangeAccounts[i]);
                        query += @" INSERT INTO `user_accounts`(`user_id`, `subproject_id`, `account_id`) VALUES (@user_ID, null, @ExchangeAccount" + i + ");";
                    }
                }
                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'user', @user_ID, CONCAT('Username = ', @username), CONCAT('role = ', @role) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " SELECT @user_ID;";
                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                User_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }
            return User_ID;
        }


        public void Update_User(int ID, string username, string password, string role, int role_id,
                    List<int> add_Subprojects,
                    List<int> add_Accounts,
                    List<int> add_CashAccounts,
                    List<int> Cashier,
                    List<int> add_Cashier,
                    List<int> remove_Cashier,
                    List<int> add_PersonalAccounts,
                    List<int> add_ExchangeAccounts,
                    List<int> delete_Subprojects,
                    List<int> delete_Accounts,
                    List<int> delete_CashAccounts,
                    List<int> delete_PersonalAccounts,
                    List<int> delete_ExchangeAccounts, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@user_ID", ID);
                sc.Parameters.AddWithValue("@username", username);
                sc.Parameters.AddWithValue("@password", password);
                sc.Parameters.AddWithValue("@role_id", role_id);
                sc.Parameters.AddWithValue("@role", role);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"UPDATE user SET Username = @username, Password = @password, role = @role_id
                           Where ID = @user_ID;";




                if (add_Subprojects != null && add_Subprojects.Count > 0)
                {
                    for (int i = 0; i < add_Subprojects.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@subproject" + i, add_Subprojects[i]);
                        query += @" INSERT INTO `user_accounts`(`user_id`, `subproject_id`, `account_id`) VALUES (@user_ID, @subproject" + i + ",  null);";
                    }
                }

                if (add_Accounts != null && add_Accounts.Count > 0)
                {
                    for (int i = 0; i < add_Accounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@account" + i, add_Accounts[i]);
                        query += @" INSERT INTO `user_accounts`(`user_id`, `subproject_id`, `account_id`) VALUES (@user_ID, (select account.subproject_id from account where account.id = @account" + i + "),  @account" + i + ");";
                    }
                }


                if (add_CashAccounts != null && add_CashAccounts.Count > 0)
                {
                    for (int i = 0; i < add_CashAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@cashaccount" + i, add_CashAccounts[i]);
                        if (Cashier.Contains(add_CashAccounts[i]))
                        {
                            sc.Parameters.AddWithValue("@IsCashies" + i, 1);
                        }
                        else
                            sc.Parameters.AddWithValue("@IsCashies" + i, 0);

                        query += @" INSERT INTO `user_cashaccounts`(`user_id`, `cashaccount_id`, `Iscashier`) VALUES (@user_ID, @cashaccount" + i + ", @IsCashies" + i + ");";
                    }
                }

                if (add_Cashier != null && add_Cashier.Count > 0)
                {
                    for (int i = 0; i < add_Cashier.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@add_Cashier" + i, add_Cashier[i]);
                        query += @"UPDATE user_cashaccounts set Iscashier = 1 where cashaccount_id = @add_Cashier" + i + " and user_id = @user_ID;";
                    }
                }

                if (remove_Cashier != null && remove_Cashier.Count > 0)
                {
                    for (int i = 0; i < remove_Cashier.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@remove_Cashier" + i, remove_Cashier[i]);
                        query += @"UPDATE user_cashaccounts set Iscashier = 0 where cashaccount_id = @remove_Cashier" + i + " and user_id = @user_ID;";
                    }
                }

                if (add_PersonalAccounts != null && add_PersonalAccounts.Count > 0)
                {
                    for (int i = 0; i < add_PersonalAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@PersonalAccount" + i, add_PersonalAccounts[i]);
                        query += @" INSERT INTO `user_accounts`(`user_id`, `subproject_id`, `account_id`) VALUES (@user_ID,null, @PersonalAccount" + i + ");";
                    }
                }

                if (add_ExchangeAccounts != null && add_ExchangeAccounts.Count > 0)
                {
                    for (int i = 0; i < add_ExchangeAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@ExchangeAccount" + i, add_ExchangeAccounts[i]);
                        query += @" INSERT INTO `user_accounts`(`user_id`, `subproject_id`, `account_id`) VALUES (@user_ID, null,  @ExchangeAccount" + i + ");";
                    }
                }

                if (delete_Subprojects != null && delete_Subprojects.Count > 0)
                {
                    for (int i = 0; i < delete_Subprojects.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@delete_Subproject" + i, delete_Subprojects[i]);
                        query += @"DELETE from user_accounts where user_id = @user_ID and subproject_ID = @delete_Subproject" + i + ";";
                    }
                }
                if (delete_Accounts != null && delete_Accounts.Count > 0)
                {
                    for (int i = 0; i < delete_Accounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@delete_Account" + i, delete_Accounts[i]);
                        query += @"DELETE from user_accounts where user_id = @user_ID and account_id = @delete_Account" + i + ";";
                    }
                }
                if (delete_CashAccounts != null && delete_CashAccounts.Count > 0)
                {
                    for (int i = 0; i < delete_CashAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@delete_CashAccount" + i, delete_CashAccounts[i]);
                        query += @"DELETE from user_cashaccounts where user_id = @user_ID and cashaccount_id = @delete_CashAccount" + i + ";";
                    }
                }
                if (delete_PersonalAccounts != null && delete_PersonalAccounts.Count > 0)
                {
                    for (int i = 0; i < delete_PersonalAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@delete_PersonalAccount" + i, delete_PersonalAccounts[i]);
                        query += @"DELETE from user_accounts where user_id = @user_ID and account_id = @delete_PersonalAccount" + i + ";";
                    }
                }
                if (delete_ExchangeAccounts != null && delete_ExchangeAccounts.Count > 0)
                {
                    for (int i = 0; i < delete_ExchangeAccounts.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@delete_ExchangeAccount" + i, delete_ExchangeAccounts[i]);
                        query += @"DELETE from user_accounts where user_id = @user_ID and account_id = @delete_ExchangeAccount" + i + ";";
                    }
                }


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'user', @user_ID, CONCAT('Username = ', @username), CONCAT('role = ', @role) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " SELECT @user_ID;";
                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }


        public void Delete_User(int UserID, string username, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@UserID", UserID);
                sc.Parameters.AddWithValue("@username", username);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);


                query += "START TRANSACTION;";

                query += "DELETE FROM `user` WHERE ID = @UserID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'user', @UserID, CONCAT('Username = ',@username) ,'', '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }


        public DataTable Check_Cashiers(List<int> CashAccounts_IDs, int UserID)
        {
            string ids = string.Join(",", CashAccounts_IDs);

            string query = $"select * from user_cashaccounts where cashaccount_id in ({ids}) and Iscashier = 1 and user_id != @UserID;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@UserID", UserID);
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
    }
}
