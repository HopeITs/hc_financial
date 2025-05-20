using MySql.Data.MySqlClient;
using System.Data;

namespace Financial_WPF_App.Models
{
    public class Category
    {
        public Category()
        {

        }
        public int ID { get; set; }
        public string Name { get; set; }


        public DataTable Select(string ids, string names)
        {

            string query = "select * from `category`";

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
