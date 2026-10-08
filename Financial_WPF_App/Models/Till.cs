using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class Till
    {
        public Till()
        {

        }

        public DataTable Select(string Account_IDs, int Till_ID, string Without_type)
        {
            //check user from database
            string query = @"SELECT till.`ID`,tilltypes.Name as 'Type', till.`Type_ID`, till.`Account_ID` FROM `till` left join tilltypes on till.type_id = tilltypes.id";
            string condition = " where 1 ";

            if (Without_type != "") condition += " and LOWER(tilltypes.Name) NOT like @Without_type ";

            if (Account_IDs != "")
            {
                condition += " and Account_ID in (" + Account_IDs + ")";
            }
            if (Till_ID != -1)
            {
                condition += " and till.`ID` <> @Till_ID";
            }
            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Without_type", Without_type.ToLower());
                sc.Parameters.AddWithValue("@Till_ID", Till_ID);

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

        public DataTable Select_ForComboBox(string Account_IDs, int Till_ID, string Without_type)
        {
            //check user from database
            string query = @"SELECT till.`ID` as 'till_ID',tilltypes.Name as 'Name', till.`Type_ID` as 'ID', till.`Account_ID` FROM `till` left join tilltypes on till.type_id = tilltypes.id";
            string condition = " where 1 ";

            if (Without_type != "") condition += " and LOWER(tilltypes.Name) NOT like @Without_type ";

            if (Account_IDs != "")
            {
                condition += " and Account_ID in (" + Account_IDs + ")";
            }
            if (Till_ID != -1)
            {
                condition += " and till.`ID` <> @Till_ID";
            }
            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Without_type", Without_type.ToLower());
                sc.Parameters.AddWithValue("@Till_ID", Till_ID);

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

        public DataTable Select_Projects_tillTypes()
        {
            string query = @"SELECT  DISTINCT(tilltypes.Name) as 'Type_Name', tilltypes.ID as 'Type_ID'
                                    FROM account LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                                    LEFT JOIN till on till.Account_ID = account.ID
                                    LEFT JOIN tilltypes on tilltypes.ID = till.Type_ID

                                    WHERE lower(tilltypes.Name) <> 'pay' AND lower(accounttype.name) = 'project';";


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

        public DataTable Select_Account_TillTypes(int Account_ID, string Without_type)
        {
            //check user from database
            string query = @"SELECT tilltypes.ID, tilltypes.Name FROM `till` left join tilltypes on till.type_id = tilltypes.id left join account on till.account_id = account.id";
            string condition = " where 1 ";

            if (Without_type != "") condition += " and LOWER(tilltypes.Name) NOT like @Without_type ";

            if (Account_ID != -1)
            {
                condition += " and Account_ID = @Account_ID;";
            }

            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@Without_type", Without_type.ToLower());

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

        /// <summary>
        /// The account's tills in a foreign currency. Budgets are set in USD or EUR, never
        /// in local money, so the local ones are left out.
        ///
        /// This replaces a call that passed "SYR" as the single name to exclude. That
        /// excluded the old local currency only, so a newly added one would have started
        /// appearing in the budget currency list, which is not what the exclusion was for.
        /// </summary>
        public DataTable Select_Account_ForeignTillTypes(int Account_ID)
        {
            string query = @"SELECT tilltypes.ID, tilltypes.Name
                             FROM `till`
                             LEFT JOIN tilltypes ON till.type_id = tilltypes.id
                             LEFT JOIN account ON till.account_id = account.id
                             WHERE NOT " + Helper.CurrencyService.LocalCurrencySql("tilltypes", true) + @"
                               AND till.Account_ID = @Account_ID;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);

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

        public DataTable Select_All_TillTypes()
        {
            //check user from database
            string query = @"SELECT tilltypes.ID, tilltypes.Name FROM tilltypes ";
            string condition = " where 1 ";


            // Excludes the local currencies and the payments container, so the core
            // types cannot be renamed or deleted from the till-types screen. This named
            // 'syr' and 'pay' directly, which left a newly added local currency
            // unprotected while the old one was safe.
            condition += " and NOT " + Helper.CurrencyService.LocalCurrencySql("tilltypes", true) + ";";


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

        /// <summary>
        /// The currencies a user may pick in a Currency dropdown. Feeds six screens:
        /// Account_Window, AddAction_Window, AddAction_Window2, Add_MultiActions_Window,
        /// Beneficiary_Window and Denominations_Window.
        ///
        /// This used to read WHERE LOWER(Name) IN ('syr','usd'), which meant a newly
        /// added local currency was invisible in all six - the user could create a till
        /// for it but could then neither select it on a transaction nor see its
        /// banknotes.
        ///
        /// The list is now derived from the currency's own properties: every local
        /// currency, plus USD. EUR stays out deliberately, as before - see the note in
        /// AddAction_Window2 about resolving it from the corresponding till instead.
        ///
        /// 'pay' is excluded because it is a till container, not a currency. It carries
        /// is_local_currency = 1 because it does hold local money, which is the right
        /// answer for "is this local?" but the wrong one for "can I pick this?". The
        /// rest of the codebase special-cases it the same way.
        /// </summary>
        public DataTable Select_All_TillTypes_Currency()
        {
            // Falls back to the original hardcoded list while the database is still on
            // the old schema, so this build runs against either.
            string query = Helper.CurrencyService.SchemaSupportsMultipleLocalCurrencies
                ? @"SELECT tilltypes.ID, tilltypes.Name
                    FROM tilltypes
                    WHERE (tilltypes.is_local_currency = 1 OR LOWER(tilltypes.Name) = 'usd')
                      AND LOWER(tilltypes.Name) <> 'pay';"
                : @"SELECT tilltypes.ID, tilltypes.Name
                    FROM tilltypes
                    WHERE LOWER(tilltypes.Name)  IN ('syr','usd');";

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

        /// <summary>
        /// The till types an account does not have yet - the list behind the "Add till"
        /// screen.
        ///
        /// A CLOSED account is not offered the current local currency. Closed accounts
        /// stay entirely in the currency they were closed in; that is what keeps their
        /// figures frozen and reconcilable against the paperwork already issued for them.
        /// The superseded local currency and the foreign ones are still offered, because
        /// blocking those would be a change to how closed accounts behave today.
        /// </summary>
        public DataTable Select_Account_Missing_TillTypes(int Account_ID)
        {
            string closedAccountRule =
                Helper.CurrencyService.SchemaSupportsMultipleLocalCurrencies
                    ? @" AND (account.IsClosed = 0
                             OR NOT (tilltypes.is_local_currency = 1 AND tilltypes.base_divisor = 1))"
                    : string.Empty;

            string query = @"SELECT tilltypes.ID, tilltypes.Name FROM tilltypes LEFT JOIN
                            (SELECT tilltypes.ID FROM tilltypes LEFT JOIN till ON till.Type_ID = tilltypes.ID
                            WHERE till.Account_ID = @Account_ID) t1
                            ON tilltypes.ID = t1.id
                            JOIN account ON account.ID = @Account_ID
                            WHERE tilltypes.Name <> 'pay' AND t1.ID is null" + closedAccountRule;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Account_ID", Account_ID);

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

        public DataTable Select_FiscalBond_Missing_TillTypes(string Account_ID, int Till_ID)
        {
            string query = @"SELECT till.ID, tilltypes.Name as 'Type' FROM
                            till LEFT JOIN tilltypes on tilltypes.ID = till.Type_ID
                            LEFT JOIN fiscalbond ON till.ID = fiscalbond.Till_ID
                            WHERE till.Account_ID = @Account_ID AND tilltypes.Name <> 'pay' AND (fiscalbond.ID IS null OR fiscalbond.Till_ID = @Till_ID);";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@Till_ID", Till_ID);

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

        public DataTable Select_All_tills_ForCombobox()
        {
            string query = @"SELECT 
                                till.ID AS 'Till_ID', tilltypes.ID AS 'ID', tilltypes.Name AS 'Name',
                                account.ID AS 'Account_ID', account.Name AS 'Account_Name',
                                account.is_administrative, account.IsClosed,
                                fiscalbond.ID AS 'Fiscalbond_ID', fiscalbond.Name AS 'Fiscalbond_Name',
                                accounttype.id AS 'Accounttype_ID', accounttype.name AS 'Accounttype_Name',
                                subproject.ID AS 'Subproject_ID', 
                                CASE WHEN subproject.ID IS null then 'Personal/Exchange' else subproject.Name END AS 'Subproject_Name',
                                category.ID AS 'Category_ID', category.Name AS 'Category_Name'

                                FROM
                                till LEFT JOIN tilltypes on till.Type_ID = tilltypes.ID
                                LEFT JOIN fiscalbond ON fiscalbond.Till_ID = till.ID
                                LEFT JOIN account on account.ID = till.Account_ID
                                LEFT JOIN accounttype on account.Accounttype_id = accounttype.id
                                LEFT JOIN subproject on subproject.ID = account.SubProject_ID
                                LEFT JOIN project ON project.ID = subproject.Project_ID
                                LEFT JOIN category on category.ID = project.Category_ID

                                WHERE
                                lower(tilltypes.Name) <> 'pay';";

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

        public DataTable Select_All_tills()
        {
            string query = @"SELECT 
                                till.ID AS 'Till_ID', tilltypes.ID AS 'Tilltypes_ID', tilltypes.Name AS 'Tilltypes_Name',
                                account.ID AS 'Account_ID', account.Name AS 'Account_Name',
                                account.is_administrative, account.IsClosed,
                                fiscalbond.ID AS 'Fiscalbond_ID', fiscalbond.Name AS 'Fiscalbond_Name',
                                accounttype.id AS 'Accounttype_ID', accounttype.name AS 'Accounttype_Name',
                                subproject.ID AS 'Subproject_ID', 
                                CASE WHEN subproject.ID IS null then 'Personal/Exchange' else subproject.Name END AS 'Subproject_Name',
                                category.ID AS 'Category_ID', category.Name AS 'Category_Name'

                                FROM
                                till LEFT JOIN tilltypes on till.Type_ID = tilltypes.ID
                                LEFT JOIN fiscalbond ON fiscalbond.Till_ID = till.ID
                                LEFT JOIN account on account.ID = till.Account_ID
                                LEFT JOIN accounttype on account.Accounttype_id = accounttype.id
                                LEFT JOIN subproject on subproject.ID = account.SubProject_ID
                                LEFT JOIN project ON project.ID = subproject.Project_ID
                                LEFT JOIN category on category.ID = project.Category_ID

                                WHERE
                                lower(tilltypes.Name) <> 'pay';";

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

        public DataTable Get_PaymentTill_ID(string SubProject_ID)
        {

            string query = "select till.ID from `till` left join account on till.Account_ID = account.ID left join tilltypes on tilltypes.id = till.type_id";
            string condition = " where tilltypes.Name = 'pay' ";
            if (SubProject_ID != "" && SubProject_ID != "-1")
                condition += " and account.SubProject_ID = @SubProject_ID";
            query += condition;
            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);

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

        public int insert(int Type_ID, string TillType_Name, int Account_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Till_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();


                sc.Parameters.AddWithValue("@Type_ID", Type_ID);
                sc.Parameters.AddWithValue("@TillType_Name", TillType_Name);
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);


                query = "START TRANSACTION;";

                query += @"INSERT INTO `till`(`Type_ID`,`Account_ID`)  
                            VALUES (@Type_ID ,@Account_ID);";

                query += " SET @till_ID = Last_Insert_ID();";


                query += " SELECT @till_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'till', @till_ID, CONCAT('till Type = ', @TillType_Name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Till_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }
            return Till_ID;
        }

        public void Delete(int ID, string TillType_Name, int Acc_ID, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@TillType_Name", TillType_Name);
                sc.Parameters.AddWithValue("@Acc_ID", Acc_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += "START TRANSACTION;";

                query += "DELETE FROM `till` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'till', @ID, CONCAT('till Type = ', @TillType_Name), CONCAT('account ID = ', @Acc_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

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
