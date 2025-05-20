using MySql.Data.MySqlClient;
using System.Data;

namespace Financial_WPF_App.Models
{
    internal class Denominations
    {
        public DataTable Select(bool WithNoDenomination)
        {
            //check user from database
            string query = "select id, case when name = 0 then 'Uncounted' else name end as 'name' from `denominations`";

            if (!WithNoDenomination)
                query += " where denominations.name <> 0 ";

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
