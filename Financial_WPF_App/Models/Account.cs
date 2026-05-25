using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Financial_WPF_App.Models
{
    public class Account : INotifyPropertyChanged
    {
        public int Account_ID { set; get; }

        public string Account_Name { set; get; }

        public Boolean Check_Status { set; get; }

        public DataTable Select(string SubProject_IDs, string Account_IDs, string Type, bool withPayments, string City_ID)
        {
            //check user from database
            string query = @"SELECT account.ID, account.Name, account.ProjectNumber,
                account.BeginDate, IFNULL(account.EndDate, 0) AS EndDate, accounttype.Name AS 'Account_Type',
                category.Name AS 'Category', account.TotalAmount, amounttype.Name AS 'AmountType', amounttype.ID AS 'AmountType_ID',
                (CASE account.IsClosed WHEN 0 THEN 'Open' WHEN 1 THEN 'CLOSED' ELSE 'N/A' END) AS 'IsClosed',
                account.SubProject_ID,
                (SELECT GROUP_CONCAT(donor.ID) FROM donor LEFT JOIN account_donor ON account_donor.Donor_id = donor.ID WHERE account_donor.Account_id = account.ID) AS 'Donors_ID',
                (SELECT GROUP_CONCAT(donor.Abb_Name) FROM donor LEFT JOIN account_donor ON account_donor.Donor_id = donor.ID WHERE account_donor.Account_id = account.ID) AS 'Donors',
                subproject.Project_ID AS 'Project_ID', city.id as 'City_ID', city.name as 'City_Name', account.is_administrative
                FROM account
                LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                LEFT JOIN till ON account.ID = till.Account_ID
                LEFT JOIN tilltypes ON till.Type_ID = tilltypes.id
                LEFT JOIN amounttype ON amounttype.ID = account.AmountType_ID
                LEFT JOIN subproject ON subproject.ID = account.SubProject_ID
                LEFT JOIN project ON subproject.project_id = project.id
                LEFT JOIN category ON project.category_id = category.id
                LEFT JOIN city on city.id = account.city_id";

            string condition = " where 1 ";
            if (withPayments == false)
            {
                condition += " and (till.Type_ID IS NULL OR tilltypes.Name <> 'pay')";
            }

            if (SubProject_IDs != "")
            {
                condition += " and subproject.ID in (" + SubProject_IDs + ")";
            }
            if (Account_IDs != "")
            {
                condition += " and account.ID in (" + Account_IDs + ")";
            }
            if (Type != "")
            {
                condition += " and account.Accounttype_id = (select id from accounttype where name = @Type)";
            }
            if (City_ID != "")
            {
                condition += " and account.city_id = @City_ID";

            }
            query += condition + " GROUP BY account.ID";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Type", Type);
                sc.Parameters.AddWithValue("@City_ID", City_ID);
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

        public DataTable Select_Cash_Account(string CashAccount_IDs, string SubProject_IDs)
        {
            //check user from database
            string query = @"SELECT cashaccount.ID, cashaccount.Name, city.name as 'city_name', city.id as 'city_id', currency_tilltype_id,
                            (SELECT GROUP_CONCAT(cashaccount_subproject.subproject_id) FROM cashaccount_subproject WHERE cashaccount_subproject.cashaccount_id = cashaccount.ID) AS 'SubProject_ID',

                            (SELECT GROUP_CONCAT(cashaccount_account.account_id) FROM cashaccount_account left join account on cashaccount_account.account_id = account.id left join accounttype on account.Accounttype_id = accounttype.id WHERE cashaccount_account.cashaccount_id = cashaccount.ID AND accounttype.name = 'Personal') AS 'PersonalAccount_ID',

                            (SELECT GROUP_CONCAT(cashaccount_account.account_id) FROM cashaccount_account left join account on cashaccount_account.account_id = account.id left join accounttype on account.Accounttype_id = accounttype.id WHERE cashaccount_account.cashaccount_id = cashaccount.ID AND accounttype.name = 'Exchange') AS 'ExchangeAccount_ID'

                            FROM cashaccount left join cashaccount_subproject on cashaccount.id = cashaccount_subproject.cashaccount_id left join city on city.id = cashaccount.city_id";

            string condition = " where 1 ";

            if (SubProject_IDs != "")
            {
                condition += " and cashaccount_subproject.subproject_id in (" + SubProject_IDs + ")";
            }
            if (CashAccount_IDs != "")
            {
                condition += " and cashaccount.ID in (" + CashAccount_IDs + ")";
            }

            query += condition + " GROUP BY cashaccount.ID";

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

        public DataSet Select_Cash_Report(int CashAccount_ID)
        {
            string query = "";

            query = @"SELECT cash_balances.Account_id AS 'Account_ID', cash_balances.City_Name AS 'City', cash_balances.Subproject_Name AS 'Project', 
                                cash_balances.Account_Name AS 'Account', cash_balances.Account_Type as 'Account_Type', cash_balances.tilltype_Name as 'tilltype_Name', cash_balances.Subproject_ID as 'Subproject_ID', cash_balances.Category_Name as 'Category',
                                Case when cash_balances.account_IsClosed  = 1 then 'Closed' when cash_balances.account_IsClosed  = 0 then 'Open' end as 'Status Account',
                                cash_balances.Account_I AS 'Income', cash_balances.Account_E AS 'Outcome', cash_balances.Account_Net AS 'Difference'
                                FROM cash_balances
                                WHERE cash_balances.CashAccount_id = @CashAccount_ID;

                                SELECT cash_balances.CashAccount_Net, SUM(cash_balances.Account_Net) AS 'AllAccounts_Net', SUM(cash_balances.Account_Net) - cash_balances.CashAccount_Net AS 'Difference'
                                FROM cash_balances
                                WHERE cash_balances.CashAccount_id = @CashAccount_ID
                                GROUP BY cash_balances.CashAccount_id;";



            DataSet ds = new DataSet();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);
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

        public DataSet Select_Cash_Report()
        {
            string query = "";

            //query = "CALL get_cash_report();";

            query = "CALL get_cash_report();";

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

        public DataTable Select_Personal_Accounts()
        {

            string query = @"SELECT account.ID as 'PersonalAcc_ID', account.Name as 'PersonalAcc_Name'
                FROM account
                LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                WHERE account.Accounttype_id = (select id from accounttype where name = 'Personal')";

            query += " GROUP BY account.ID;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.CommandTimeout = 120;

                sc.Connection = Login.MyConn;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_Exchange_Accounts()
        {

            string query = @"SELECT account.ID as 'ExchangeAcc_ID', account.Name as 'ExchangeAcc_Name'
                FROM account
                LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                WHERE account.Accounttype_id = (select id from accounttype where name = 'Exchange')
                GROUP BY account.ID;";

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

        public DataTable Select_ExchangeAccount_Rate(int Acc_ID, int FromTill_ID, int ToTill_ID)
        {
            string query = "";


            query = @"CALL get_ExchangeAccount_Rates(@Acc_ID,@FromTill_ID,@ToTill_ID);";



            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Acc_ID", Acc_ID);
                sc.Parameters.AddWithValue("@FromTill_ID", FromTill_ID);
                sc.Parameters.AddWithValue("@ToTill_ID", ToTill_ID);
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

        public int Insert(string name, string projectNumber, string beginDate, string endDate, float totalAmount, int amountType_ID, int City_ID, int IsClosed, int SubProject_ID, List<string> Add_donors, List<string> Auto_Tills, string Form_Name, string AccType)
        {
            string query = "";
            string log_query = "";

            int Account_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@projectNumber", projectNumber);
                if (beginDate == "NULL")
                    sc.Parameters.AddWithValue("@beginDate", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@beginDate", beginDate);
                if (endDate == "NULL")
                    sc.Parameters.AddWithValue("@endDate", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@endDate", endDate);

                sc.Parameters.AddWithValue("@totalAmount", totalAmount);

                if (amountType_ID == -1)
                {
                    sc.Parameters.AddWithValue("@amountType_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@amountType_ID", amountType_ID);
                }
                if (City_ID == -1)
                {
                    sc.Parameters.AddWithValue("@City_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@City_ID", City_ID);
                }
                sc.Parameters.AddWithValue("@IsClosed", IsClosed);
                if (SubProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@SubProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);
                }

                sc.Parameters.AddWithValue("@AccType", AccType);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "INSERT INTO account(Name, ProjectNumber, BeginDate, EndDate, TotalAmount, AmountType_ID,IsClosed, SubProject_ID, Accounttype_id, city_id )" +
                    " VALUES (@name, @projectNumber , @beginDate , @endDate, @totalAmount, @amountType_ID, @IsClosed, @SubProject_ID, (Select accounttype.id from accounttype where accounttype.name = @AccType), @City_ID);";


                query += " SET @Acc_ID = Last_Insert_ID();";


                if (Add_donors.Count != 0)
                {
                    for (int i = 0; i < Add_donors.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@DonorID" + i, Add_donors[i]);
                        query += "INSERT INTO `account_donor` (`Account_ID`,`Donor_ID`) VALUES " +
                         "(@Acc_ID, @DonorID" + i + ");";

                        query += " SET @D_ID" + i + " = Last_Insert_ID();";

                    }
                }

                if (Auto_Tills.Count != 0)
                {
                    for (int i = 0; i < Auto_Tills.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@till" + i, Auto_Tills[i]);
                        query += "INSERT INTO `till` (`Type_ID`,`Account_ID`) VALUES " +
                         "((SELECT ID FROM tilltypes WHERE LOWER(NAME) = LOWER(@till" + i + ")), @Acc_ID);";


                        query += " SET @T_ID" + i + " = Last_Insert_ID();";
                    }
                }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'account', @Acc_ID, CONCAT('account Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (Auto_Tills.Count != 0)
                {
                    for (int i = 0; i < Auto_Tills.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'till', @T_ID" + i + ", CONCAT('till Type = ', LOWER(@till" + i + ")), CONCAT('Auto created with account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                if (Add_donors.Count != 0)
                {
                    for (int i = 0; i < Add_donors.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'account_donor', @D_ID" + i + ", CONCAT('donor Name = ', (SELECT donor.name from donor where donor.id = @DonorID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                query += " SELECT @Acc_ID;";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Account_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }

            return Account_ID;
        }

        public int Insert_Cash_Account(string name, List<string> Add_SubProject, List<string> Add_Acc, int City_ID, int Currency_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Account_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@City_ID", City_ID);
                sc.Parameters.AddWithValue("@Currency_ID", Currency_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "INSERT INTO cashaccount(Name, city_id,currency_tilltype_id)" +
                    " VALUES (@name, @City_ID,@Currency_ID);";


                query += " SET @Acc_ID = Last_Insert_ID();";


                if (Add_SubProject.Count != 0)
                {
                    for (int i = 0; i < Add_SubProject.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@SubProjectID" + i, Add_SubProject[i]);
                        query += "INSERT INTO `cashaccount_subproject` (`cashaccount_id`,`subproject_id`) VALUES " +
                         "(@Acc_ID, @SubProjectID" + i + ");";

                        query += " SET @S_ID" + i + " = Last_Insert_ID();";

                    }
                }


                if (Add_Acc.Count != 0)
                {
                    for (int i = 0; i < Add_Acc.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@AddAccID" + i, Add_Acc[i]);
                        query += "INSERT INTO `cashaccount_account` (`cashaccount_id`,`account_id`) VALUES " +
                         "(@Acc_ID, @AddAccID" + i + ");";

                        query += " SET @Cash_Acc_ID" + i + " = Last_Insert_ID();";

                    }
                }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'cashaccount', @Acc_ID, CONCAT('account Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (Add_SubProject.Count != 0)
                {
                    for (int i = 0; i < Add_SubProject.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'cashaccount_subproject', @S_ID" + i + ", CONCAT('subproject Name = ', (SELECT subproject.name from subproject where subproject.id = @SubProjectID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                if (Add_Acc.Count != 0)
                {
                    for (int i = 0; i < Add_Acc.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'cashaccount_account', @Cash_Acc_ID" + i + ", CONCAT('account Name = ', (SELECT account.name from account where account.id = @AddAccID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                query += " SELECT @Acc_ID;";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Account_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }

            return Account_ID;
        }

        public DataTable get_attachments(List<int> Account_IDs, int? Action_ID, int? Beneficiary_ID, string FromDate, string ToDate)
        {
            string query = "";
            var accParams = Account_IDs
            .Select((id, i) => $"@acc{i}")
            .ToArray();
            query = @"SELECT action_attachments.id, action_attachments.action_id, action_attachments.attach_name
                      FROM `action_attachments` 
                      LEFT JOIN action on action.ID = action_attachments.action_id
                      LEFT JOIN till on till.ID = action.Till_ID
                      LEFT JOIN account on account.ID = till.Account_ID where 1";

            if (Account_IDs != null && Account_IDs.Count > 0)
            {
                query += $" AND account.ID IN ({string.Join(",", accParams)})";

                
            }
            if (FromDate != "" && ToDate != "" && Action_ID == 0) {
                query += " AND date(action.ActionDate) BETWEEN @FromDate and @ToDate ";
            }
            if (Action_ID != 0)
            {
                query += " and action_id = @Action_ID;";
            }
            if (Beneficiary_ID != -1 && Beneficiary_ID != 0)
            {
                query = " SELECT * FROM beneficiary_attachments WHERE beneficiary_id = @beneficiary_id;";
            }

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                
                for (int i = 0; i < Account_IDs.Count; i++)
                    sc.Parameters.AddWithValue(accParams[i], Account_IDs[i]);
                sc.Parameters.AddWithValue("@FromDate", FromDate);
                sc.Parameters.AddWithValue("@ToDate", ToDate);
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Beneficiary_ID", Beneficiary_ID);
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

        public DataTable get_user_cities(int UserId,string UserRole)
        {
            string query = "";
            if(UserRole != "Head Office" && UserRole != "Financial Manager")
            {
                query = @"SELECT DISTINCT c.id, c.name
                            FROM city c
                            JOIN subproject sp ON sp.city_id = c.id
                            JOIN user_accounts ua ON ua.subproject_id = sp.id
                            WHERE ua.user_id = @UserId;";
            }
            else
            {
                query = @"SELECT DISTINCT c.id, c.name from city c;";
            }


                DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@UserId", UserId);
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable get_user_projects(int UserId, int CityId, string UserRole)
        {
            string query = "";
            if (UserRole != "Head Office" && UserRole != "Financial Manager")
            {
                query = @"SELECT DISTINCT sp.id, sp.name
                            FROM subproject sp
                            JOIN user_accounts ua ON ua.subproject_id = sp.id
                            WHERE ua.user_id = @UserId
                            AND sp.city_id = @CityId;";
            }
            else
            {
                query = @"SELECT DISTINCT sp.id, sp.name
                  FROM subproject sp
                  WHERE sp.city_id = @CityId";
            }

                DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@UserId", UserId);
                sc.Parameters.AddWithValue("@CityId", CityId);
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable get_user_accounts(int UserId,int SubProjectId , string UserRole)
        {
            string query = "";
            if (UserRole != "Head Office" && UserRole != "Financial Manager")
            {
                query = @"SELECT DISTINCT a.id, a.name
                            FROM account a
                            JOIN user_accounts ua ON ua.account_id = a.id
                            WHERE ua.user_id = @UserId
                            AND ua.subproject_id = @SubProjectId;";
            }
            else {
                query = @"SELECT DISTINCT a.id, a.name
                  FROM account a
                  WHERE a.subproject_id = @SubProjectId;";
            }

                DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@UserId", UserId);
                sc.Parameters.AddWithValue("@SubProjectId", SubProjectId);
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_Accounts_By_Subproject_And_TillType(
            string tillName,
            int subprojectId,
            bool destinationIsPersonal,
            int? excludeAccountId = null
        )
        {
            string excludeClause = excludeAccountId.HasValue ? " AND a.ID <> @ExcludeAccountID " : "";

            
            string wherePersonal = @"
              TRIM(LOWER(tt.Name)) = @TillName
          AND atp.id = 2
          AND (a.IsClosed = 0 OR a.IsClosed IS NULL)";

           
            string whereProject = @"
              TRIM(LOWER(tt.Name)) = @TillName
          AND atp.id = 1
          AND a.SubProject_ID = @Subproject_ID
          AND (a.IsClosed = 0 OR a.IsClosed IS NULL)";

            string sql = @"
            SELECT DISTINCT 
                   a.ID   AS Account_ID,
                   a.Name AS Account_Name
            FROM account a
            INNER JOIN accounttype atp ON a.Accounttype_id = atp.id
            INNER JOIN till t          ON t.Account_ID     = a.ID
            INNER JOIN tilltypes tt    ON tt.ID            = t.Type_ID
            WHERE " + (destinationIsPersonal ? wherePersonal : whereProject) + excludeClause + @"
            ORDER BY a.Name;";

            var dt = new DataTable();

            if (Login.BuildConnection())
            {
                using (var cmd = new MySqlCommand(sql, Login.MyConn))
                {
                    cmd.Parameters.AddWithValue("@TillName", (tillName ?? "").Trim().ToLower());
                    cmd.Parameters.AddWithValue("@Subproject_ID", subprojectId);
                    if (excludeAccountId.HasValue)
                        cmd.Parameters.AddWithValue("@ExcludeAccountID", excludeAccountId.Value);

                    cmd.CommandTimeout = 120;
                    using (var da = new MySqlDataAdapter(cmd))
                        da.Fill(dt);
                }
                Login.CloseAndClearConnection();
            }

            return dt;
        }









        public int Insert_Administrative_Account(int Account_ID, List<string> Add_donors, List<string> Auto_Tills, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Admin_Acc_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "Set @name = (SELECT CONCAT(account.Name,' - Administrative - ', (Select count(id) from account acc where acc.ObserveAccount_ID = @Account_ID)) from account where account.id = @Account_ID);";

                query += "INSERT INTO account(Name, ProjectNumber, BeginDate, EndDate, TotalAmount, AmountType_ID,IsClosed, SubProject_ID, Accounttype_id, city_id, is_administrative, ObserveAccount_ID)" +
                    " SELECT @name, account.ProjectNumber, account.BeginDate, account.EndDate, account.TotalAmount, account.AmountType_ID," +
                    " account.IsClosed, account.SubProject_ID, account.Accounttype_id, account.city_id, '1', @Account_ID" +
                    " from account where account.id = @Account_ID;";

                query += " SET @Acc_ID = Last_Insert_ID();";

                if (Add_donors.Count != 0)
                {
                    for (int i = 0; i < Add_donors.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@DonorID" + i, Add_donors[i]);
                        query += "INSERT INTO `account_donor` (`Account_ID`,`Donor_ID`) VALUES " +
                         "(@Acc_ID, @DonorID" + i + ");";

                        query += " SET @D_ID" + i + " = Last_Insert_ID();";
                    }
                }

                if (Auto_Tills.Count != 0)
                {
                    for (int i = 0; i < Auto_Tills.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@till" + i, Auto_Tills[i]);
                        query += "INSERT INTO `till` (`Type_ID`,`Account_ID`) VALUES " +
                         "((SELECT ID FROM tilltypes WHERE LOWER(NAME) = LOWER(@till" + i + ")), @Acc_ID);";


                        query += " SET @T_ID" + i + " = Last_Insert_ID();";
                    }
                }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'account', @Acc_ID, CONCAT('account Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (Auto_Tills.Count != 0)
                {
                    for (int i = 0; i < Auto_Tills.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'till', @T_ID" + i + ", CONCAT('till Type = ', LOWER(@till" + i + ")), CONCAT('Auto created with account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                if (Add_donors.Count != 0)
                {
                    for (int i = 0; i < Add_donors.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'account_donor', @D_ID" + i + ", CONCAT('donor Name = ', (SELECT donor.name from donor where donor.id = @DonorID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                query += " SELECT @Acc_ID;";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Admin_Acc_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();

            }

            return Admin_Acc_ID;
        }

        public int Insert_ExchangeAccount_Rate(int Acc_ID, string Acc_Name, string Date, double Value, int FromTill_ID, int ToTill_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int ExchangeAccount_Rate_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Acc_ID", Acc_ID);
                sc.Parameters.AddWithValue("@Acc_Name", Acc_Name);
                sc.Parameters.AddWithValue("@Date", Date);
                sc.Parameters.AddWithValue("@Value", Value);
                sc.Parameters.AddWithValue("@FromTill_ID", FromTill_ID);
                sc.Parameters.AddWithValue("@ToTill_ID", ToTill_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "INSERT INTO exchangeaccount_rate(Account_ID, ratedate, rate, FromTill_ID, ToTill_ID)" +
                    " VALUES (@Acc_ID, @Date , @Value , @FromTill_ID, @ToTill_ID);";

                query += " SET @ExAccRate_ID = Last_Insert_ID();";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'Exchange account rate', @ExAccRate_ID, CONCAT('account Name = ', @Acc_Name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                query += " SELECT @ExAccRate_ID;";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                ExchangeAccount_Rate_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }

            return ExchangeAccount_Rate_ID;

        }

        public void Update(int Acc_ID, string name, string projectNumber, string beginDate, string endDate, float totalAmount, int amountType_ID, int City_ID, int IsClosed, int SubProject_ID, List<string> Add_donors, List<string> Delete_donors, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Acc_ID", Acc_ID);
                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@projectNumber", projectNumber);
                if (beginDate == "NULL")
                    sc.Parameters.AddWithValue("@beginDate", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@beginDate", beginDate);
                if (endDate == "NULL")
                    sc.Parameters.AddWithValue("@endDate", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@endDate", endDate);
                sc.Parameters.AddWithValue("@totalAmount", totalAmount);

                if (amountType_ID == -1)
                {
                    sc.Parameters.AddWithValue("@amountType_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@amountType_ID", amountType_ID);
                }

                if (City_ID == -1)
                {
                    sc.Parameters.AddWithValue("@City_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@City_ID", City_ID);
                }

                sc.Parameters.AddWithValue("@IsClosed", IsClosed);
                if (SubProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@SubProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);
                }


                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "UPDATE account SET Name = @name, ProjectNumber = @projectNumber, BeginDate = @beginDate, EndDate = @endDate, TotalAmount = @totalAmount, AmountType_ID = @amountType_ID, IsClosed = @IsClosed, SubProject_ID = @SubProject_ID, city_id = @City_ID WHERE ID = @Acc_ID;";

                if (Add_donors.Count != 0)
                {
                    for (int i = 0; i < Add_donors.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@AddDonorID" + i, Add_donors[i]);
                        query += " INSERT INTO `account_donor` (`Account_ID`,`Donor_ID`) VALUES " +
                         "(@Acc_ID, @AddDonorID" + i + ");";

                        query += " SET @D_ID" + i + " = Last_Insert_ID();";
                    }
                }

                if (Delete_donors.Count != 0)
                {
                    for (int i = 0; i < Delete_donors.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@DeleteDonorID" + i, Delete_donors[i]);
                        query += "set @deleted_accountDonor_ID" + i + " = (select account_donor.id from `account_donor` WHERE Account_id = @Acc_ID and Donor_id = @DeleteDonorID" + i + ");";
                        query += " Delete from `account_donor` WHERE Account_id = @Acc_ID and Donor_id = @DeleteDonorID" + i + ";";
                    }
                }


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'account', @Acc_ID, CONCAT('account Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                if (Delete_donors.Count != 0)
                {
                    for (int i = 0; i < Delete_donors.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'account_donor', @deleted_accountDonor_ID" + i + ", CONCAT('Dononr Name = ',(SELECT donor.name from donor where id = @DeleteDonorID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                if (Add_donors.Count != 0)
                {
                    for (int i = 0; i < Add_donors.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'account_donor', @D_ID" + i + ", CONCAT('Dononr Name = ', (SELECT donor.name from donor where id = @AddDonorID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        } //Checked

        public void Update_Cash_Account(int Acc_ID, string name, List<string> Add_SubProject, List<string> Delete_SubProject, List<string> Add_Acc, List<string> Delete_Acc, int City_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Acc_ID", Acc_ID);
                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@City_ID", City_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "UPDATE cashaccount SET Name = @name, city_id = @City_ID WHERE ID = @Acc_ID;";

                if (Delete_SubProject.Count != 0)
                {
                    for (int i = 0; i < Delete_SubProject.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@DeleteSubProjectID" + i, Delete_SubProject[i]);
                        query += "set @deleted_cashaccount_subproject_ID" + i + " = (select cashaccount_subproject.id from `cashaccount_subproject` WHERE cashaccount_id = @Acc_ID and subproject_id = @DeleteSubProjectID" + i + ");";
                        query += " Delete from `cashaccount_subproject` WHERE cashaccount_id = @Acc_ID and subproject_id = @DeleteSubProjectID" + i + ";";
                    }
                }

                if (Add_SubProject.Count != 0)
                {
                    for (int i = 0; i < Add_SubProject.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@AddSubProjectID" + i, Add_SubProject[i]);
                        query += " INSERT INTO `cashaccount_subproject` (`cashaccount_id`,`subproject_id`) VALUES " +
                         "(@Acc_ID, @AddSubProjectID" + i + ");";

                        query += " SET @S_ID" + i + " = Last_Insert_ID();";
                    }

                }


                if (Delete_Acc.Count != 0)
                {
                    for (int i = 0; i < Delete_Acc.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@DeleteAccID" + i, Delete_Acc[i]);
                        query += "set @deleted_cashaccount_account_ID" + i + " = (select cashaccount_account.id from `cashaccount_account` WHERE cashaccount_id = @Acc_ID and account_id = @DeleteAccID" + i + ");";
                        query += " Delete from `cashaccount_account` WHERE cashaccount_id = @Acc_ID and account_id = @DeleteAccID" + i + ";";
                    }
                }


                if (Add_Acc.Count != 0)
                {
                    for (int i = 0; i < Add_Acc.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@AddAccID" + i, Add_Acc[i]);
                        query += " INSERT INTO `cashaccount_account` (`cashaccount_id`,`account_id`) VALUES " +
                         "(@Acc_ID, @AddAccID" + i + ");";

                        query += " SET @cashaccount_account_ID" + i + " = Last_Insert_ID();";
                    }

                }


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'cashaccount', @Acc_ID, CONCAT('account Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                if (Delete_SubProject.Count != 0)
                {
                    for (int i = 0; i < Delete_SubProject.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'cashaccount_subproject', @deleted_cashaccount_subproject_ID" + i + ", CONCAT('subproject Name = ',(SELECT subproject.name from subproject where id = @DeleteSubProjectID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                if (Add_SubProject.Count != 0)
                {
                    for (int i = 0; i < Add_SubProject.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'cashaccount_subproject', @S_ID" + i + ", CONCAT('subproject Name = ', (SELECT subproject.name from subproject where id = @AddSubProjectID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                if (Delete_Acc.Count != 0)
                {
                    for (int i = 0; i < Delete_Acc.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'cashaccount_account', @deleted_cashaccount_account_ID" + i + ", CONCAT('Personal account Name = ',(SELECT account.name from account where id = @DeleteAccID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }


                if (Add_Acc.Count != 0)
                {
                    for (int i = 0; i < Add_Acc.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'cashaccount_account', @cashaccount_account_ID" + i + ",  CONCAT('Personal account Name = ',(SELECT account.name from account where id = @AddAccID" + i + ")), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }


        public void Delete(int ID, string Acc_Name, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                sc.Parameters.AddWithValue("@Acc_Name", Acc_Name);

                query += "START TRANSACTION;";

                query += "DELETE FROM `account_donor` WHERE account_donor.account_ID = @ID;";
                query += "DELETE FROM `account` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'account', @ID, CONCAT('account Name = ', @Acc_Name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }

        public void Delete_Cash_Account(int ID, string Acc_Name, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                sc.Parameters.AddWithValue("@Acc_Name", Acc_Name);

                query += "START TRANSACTION;";

                query += "DELETE FROM `cashaccount` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'cashaccount', @ID, CONCAT('account Name = ', @Acc_Name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }

        public void Delete_ExchangeAccount_Rate(int ID, string Acc_Name, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Acc_Name", Acc_Name);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += "START TRANSACTION;";

                query += "DELETE FROM `exchangeaccount_rate` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'Exchange account rate', @ID, CONCAT('account Name = ', @Acc_Name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChange([CallerMemberName] string propertyname = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        public DataTable Select_AmountType()
        {
            //check user from database
            string query = @"SELECT ID , NAME FROM amounttype";

            string condition = " where 1 ";

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

        public DataTable Select_ExAcc_Tills(string FromTill, string ToTill)
        {
            string query = @"SELECT account.id, account.Name, LOWER(tilltypes.Name) AS 'toTill',
                            (select GROUP_CONCAT(cashaccount_account.cashaccount_id) from  cashaccount_account where cashaccount_account.account_id = account.id) AS 'cashaccount_id'
                            FROM account LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                            LEFT JOIN till ON till.Account_ID = account.ID
                            LEFT JOIN tilltypes on tilltypes.ID = till.Type_ID
                            WHERE accounttype.name = 'Exchange'
                            AND (account.ID IN (SELECT till.Account_ID FROM till LEFT JOIN tilltypes ON till.Type_ID = tilltypes.ID WHERE LOWER(tilltypes.Name) = @FromTill)) GROUP BY account.ID, tilltypes.ID;";
            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@FromTill", FromTill.ToLower());
                sc.Parameters.AddWithValue("@ToTill", ToTill.ToLower());
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

        public DataSet Select_CashAcc_Tills(int CashAccount_ID)
        {
            string query = @"CAll Cash_Flow(@CashAccount_ID);";

            query += @"SELECT cash_balances.CashAccount_I, cash_balances.CashAccount_E, cash_balances.CashAccount_Net,
                        SUM(cash_balances.Account_Net) AS 'AllAccounts_Net',
                        SUM(cash_balances.Account_Net) - cash_balances.CashAccount_Net AS 'Difference'
                        FROM cash_balances
                        WHERE cash_balances.CashAccount_id = @CashAccount_ID
                        GROUP BY cash_balances.CashAccount_id;";
            DataSet ds = new DataSet();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);
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

        public DataTable Select_Accounts_With_Same_TillType(string FromTill, int Acc_ID, int Donor_ID)
        {
            //check user from database
            string query = @"SELECT till.id AS 'ID', account.Name AS 'Type', account.subproject_ID as 'subproject_ID', account.ID as 'account_id',
                            (SELECT Group_CONCAT(account_donor.donor_id) from account_donor where account_donor.account_id = account.ID) AS 'Donor_ID'
                            FROM account LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                            LEFT JOIN till ON till.Account_ID = account.ID
                            LEFT JOIN tilltypes ON till.Type_ID = tilltypes.ID
                            WHERE accounttype.name = 'project' AND Lower(tilltypes.Name) = @FromTill";

            if (Acc_ID != -1)
            {
                query += " AND account.ID <> @Acc_ID";
            }
            if (Donor_ID != -1)
            {
                query += " AND (SELECT COUNT(id) from account_donor where account_donor.account_id = account.ID and account_donor.donor_id = @Donor_ID) > 0";
            }

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@FromTill", FromTill.ToLower());
                sc.Parameters.AddWithValue("@Acc_ID", Acc_ID);
                sc.Parameters.AddWithValue("@Donor_ID", Donor_ID);
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

        public DataTable Select_Account_Donors(int Acc_ID)
        {
            string query = "";
            query += @"SELECT donor.ID, donor.Name, account_donor.account_id
                            FROM account_donor LEFT JOIN donor ON donor.ID = account_donor.Donor_id";

            if (Acc_ID != -1)
            {
                query += " WHERE account_donor.Account_id = @Acc_ID";
            }


            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Acc_ID", Acc_ID);
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
