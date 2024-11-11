using MySql.Data.MySqlClient;
using System.Data;

namespace Financial_WPF_App.Models
{
    public class City
    {
        public City()
        {

        }

        #region Members
        int _ID;
        string _Name;
        #endregion

        #region Properties 
        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        #endregion

        public DataTable Select(string ids, string names)
        {
            //check user from database
            string query = "select * from `city`";
            string condition = " where 1 ";
            if (ids != "")
            {
                condition += " and ID in (" + ids + ") ";
            }

            if (names != "")
            {
                condition += " and Name in (" + names + ") ";
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

        public DataSet Select_Tree_View()
        {

            string Accounts_Condition = "", Cash_Condition = "", Exchange_Condition = "", Personal_Condition = "", fiscalbonds_Condition = "";

            int userID = Properties.Settings.Default.User_ID;
            string role = Properties.Settings.Default.Role;


            if (role == "Head Office")
            {
                //No limits
            }
            else if (role == "Project Manager")
            {
                Accounts_Condition = " and tree_view.SubprojectID in (SELECT subproject_id from user_accounts where user_id = @userID and account_id is null)";

                Cash_Condition = " and cashaccount.ID in (SELECT cashaccount_id from user_cashaccounts where user_id = @userID)";

                Exchange_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Exchange')";

                Personal_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Personal')";

                fiscalbonds_Condition = " and tree_view.SubprojectID in (SELECT subproject_id from user_accounts where user_id = @userID and account_id is null)";
            }
            else if (role == "Financial Manager")
            {
                Accounts_Condition = " and tree_view.SubprojectID in (SELECT subproject_id from user_accounts where user_id = @userID and account_id is null)";

                Cash_Condition = " and cashaccount.ID in (SELECT cashaccount_id from user_cashaccounts where user_id = @userID)";

                Exchange_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Exchange')";

                Personal_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Personal')";

                fiscalbonds_Condition = " and tree_view.SubprojectID in (SELECT subproject_id from user_accounts where user_id = @userID and account_id is null)";
            }
            else if (role == "Accountant")
            {
                Accounts_Condition = " and tree_view.AccountID in (SELECT account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'project')";

                Cash_Condition = " and cashaccount.ID in (SELECT cashaccount_id from user_cashaccounts where user_id = @userID)";

                Exchange_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Exchange')";

                Personal_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Personal')";

                fiscalbonds_Condition = " and tree_view.AccountID in (SELECT account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'project')";
            }
            else if (role == "Cashier")
            {
                Accounts_Condition = " and tree_view.SubprojectID in (SELECT subproject_id from user_accounts where user_id = @userID and account_id is null)";

                Cash_Condition = " and cashaccount.ID in (SELECT cashaccount_id from user_cashaccounts where user_id = @userID)";

                Exchange_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Exchange')";

                Personal_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Personal')";

                fiscalbonds_Condition = " and tree_view.SubprojectID in (SELECT subproject_id from user_accounts where user_id = @userID and account_id is null)";
            }
            else if (role == "Employee")
            {
                Accounts_Condition = " and tree_view.AccountID in (null)"; //No accounts shold be selected

                Cash_Condition = " and cashaccount.ID in (SELECT cashaccount_id from user_cashaccounts where user_id = @userID)";

                Exchange_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Exchange')";

                Personal_Condition = " and account.ID in (SELECT user_accounts.account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'Personal')";

                fiscalbonds_Condition = " and tree_view.AccountID in (SELECT account_id from user_accounts join account on user_accounts.account_id = account.id" +
                    " JOIN accounttype ON account.Accounttype_id = accounttype.id where user_id = @userID and accounttype.Name = 'project') AND LOWER(FiscalbondType) = 'syr'";
            }
            else if (role == "donor")
            {
                //No limits
            }

            string query = @"SELECT * FROM tree_view 
                                WHERE tree_view.SubprojectID IS NOT null ";
            if(Accounts_Condition != "")
            {
                query += Accounts_Condition;
            }
            query += " group by tree_view.SubprojectID, tree_view.AccountID " +
                "ORDER BY tree_view.cityName, tree_view.projectName, tree_view.subprojectName, tree_view.AccountID;";


            query += @"SELECT account.ID, account.Name, city.id as 'CityID', city.name as 'CityName'
                FROM account
                LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                LEFT JOIN city on city.id = account.city_id
                WHERE account.Accounttype_id = (select id from accounttype where name = 'Exchange')";
            if (Exchange_Condition != "")
            {
                query += Exchange_Condition;
            }
            query += " GROUP BY account.ID;";



            query += @"SELECT * FROM tree_view 
                                WHERE tree_view.FiscalbondID IS NOT null ";
            if (fiscalbonds_Condition != "")
            {
                query += fiscalbonds_Condition;
            }
            query += "group by tree_view.SubprojectID, tree_view.FiscalbondID " +
                "ORDER BY tree_view.cityName, tree_view.projectName, tree_view.subprojectName, tree_view.FiscalbondID;";



            query += @"SELECT cashaccount.ID, cashaccount.Name, city.id as 'CityID', city.name as 'CityName'
                FROM cashaccount left join city on cashaccount.city_id = city.id where 1";
            if (Cash_Condition != "")
            {
                query += Cash_Condition;
            }
            query += ";";

            query += @"SELECT account.ID, account.Name, city.id as 'CityID', city.name as 'CityName', account.isClosed
                FROM account
                LEFT JOIN accounttype ON account.Accounttype_id = accounttype.id
                LEFT JOIN city on city.id = account.city_id
                WHERE account.Accounttype_id = (select id from accounttype where name = 'Personal')";
            if (Personal_Condition != "")
            {
                query += Personal_Condition;
            }
            query += "GROUP BY account.ID;";
        

            DataSet ds = new DataSet();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@userID", userID);

                sc.CommandTimeout = 120;sc.CommandText = query;
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(ds);
                Login.MyConn.Close();
            }
            return ds;
        }

    }
}
