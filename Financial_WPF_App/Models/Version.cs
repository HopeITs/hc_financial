using MySql.Data.MySqlClient;
using System.Data;

namespace Financial_WPF_App.Models
{
    internal class Version
    {

        public DataTable Get_Version()
        {
            string query = "SELECT l_version FROM version WHERE id = 1;";

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


        public void Update_Version(string NewVersion)
        {
            string query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@NewVersion", NewVersion);

                query = " START TRANSACTION;";
                query += "UPDATE version SET l_version = @NewVersion WHERE id = 1;";
                query += " COMMIT;";


                sc.CommandTimeout = 120; sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }
    }
}
