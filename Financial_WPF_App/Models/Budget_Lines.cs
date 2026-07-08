using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace Financial_WPF_App.Models
{
    public class Budget_Lines
    {
        public DataTable Select(int Budget_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Budget_ID", Budget_ID);

            string query = @"CALL Select_Budget_Lines_Details(@Budget_ID)";

            sc.CommandTimeout = 120;
            sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
            sc.Parameters.AddWithValue("@t", 600);
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_All(int Budget_Type_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Budget_Type_ID", Budget_Type_ID);

            string query = @"CALL Select_All_Budget_Lines_Details(@Budget_Type_ID)";

            sc.CommandTimeout = 120;
            sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
            sc.Parameters.AddWithValue("@t", 600);
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_unique_Budget_types()
        {
            MySqlCommand sc = new MySqlCommand();

            string query = @"SELECT DISTINCT (all_budgetlines.BudgetType_ID), all_budgetlines.BudgetType FROM all_budgetlines;";

            sc.CommandTimeout = 120;
            sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
            sc.Parameters.AddWithValue("@t", 600);
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_AllAvilableBudgetLines_ForCombobox(int Account_ID)
        {
            //check user from database
            string query = @"SELECT all_budgetlines.budget_ID as `ID`,all_budgetlines.budget_Name as `Name`,
                                all_budgetlines.BudgetType_ID 
                                FROM all_budgetlines";

            string condition = " where 1 ";

            if (Account_ID != -1)
                condition += " and Account_ID = @Account_ID";

            query += condition;
            query += " Group By all_budgetlines.line_ID, all_budgetlines.budget_ID";


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

        public DataTable Select_AllAvilableBudgetLines(int Account_ID)
        {
            //check user from database
            string query = @"SELECT all_budgetlines.* FROM all_budgetlines";

            string condition = " where 1 ";

            if (Account_ID != -1)
                condition += " and Account_ID = @Account_ID";

            query += condition;
            query += " Group By all_budgetlines.line_ID, all_budgetlines.budget_ID";


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

        public DataTable Select_Missing_Lines(int Budget_ID, int Project_ID)
        {

            string query = @"SELECT line.id as 'line_ID', line.Name as 'Line', linecategory.id as 'Category_ID', linecategory.Name as 'Category', '0' as 'Unit', '0' as 'Timing', '0' as 'Price',
                linecategorytype.Name as 'Type',
                case linecategory.Kind when 0 then 'Profit' else 'Budget' end as 'Kind'
                            FROM line LEFT JOIN
                            (SELECT budget_line.Budget_ID, budget_line.Line_ID FROM budget_line WHERE budget_line.Budget_ID = @Budget_ID) t1 ON line.ID = t1.Line_ID
                            left join line_project on line.id = line_project.line_id     
                            left join linecategory on line.Category_id = linecategory.id
                            left join linecategorytype on linecategory.Type_ID = linecategorytype.ID
                            WHERE t1.Budget_ID IS null and line_project.project_id = @Project_ID;";


            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Budget_ID", Budget_ID);
                sc.Parameters.AddWithValue("@Project_ID", Project_ID);
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

        public void Insert_Multi_Lines()
        {
            string query = "";


            query += "START TRANSACTION;";

            //query += Login.MultiAction_Query;

            query += string.Join(Environment.NewLine, Login.MultiBudgetLines_List.ToArray());
            query += " COMMIT;";

            if (Login.BuildConnection())
            {
                Login.MultiBudgetLines_sc.CommandTimeout = 120;
                Login.MultiBudgetLines_sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                Login.MultiBudgetLines_sc.Parameters.AddWithValue("@t", 600);
                Login.MultiBudgetLines_sc.Connection = Login.MyConn;

                Login.MultiBudgetLines_sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public void Insert_Missing_Lines()
        {
            string query = "";


            query += "START TRANSACTION;";

            //query += Login.MultiAction_Query;

            query += string.Join(Environment.NewLine, Login.MissingBudgetLines_List.ToArray());
            query += " COMMIT;";
            if (Login.BuildConnection())
            {
                Login.MissingBudgetLines_sc.CommandTimeout = 120;
                Login.MissingBudgetLines_sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                Login.MissingBudgetLines_sc.Parameters.AddWithValue("@t", 600);
                Login.MissingBudgetLines_sc.Connection = Login.MyConn;

                Login.MissingBudgetLines_sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

    }
}
