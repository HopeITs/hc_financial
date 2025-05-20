using MySql.Data.MySqlClient;
using System.Data;

namespace Financial_WPF_App.Models
{
    internal class Log
    {

        public DataTable Select()
        {

            string query = @"SELECT 
                                    log.id AS 'Log_ID',
                                    log.title AS 'Operation type',
                                    log.affected_table AS 'Process destination in database',
                                    log.affected_id AS 'IDs',
                                    log.details AS 'Names',
                                    log.additionaldetails  AS 'Additional information',
                                    log.form_name  AS 'Window',
                                    log.query,
                                    log.date AS 'Date of operation',
                                    log.user AS 'The user'
                                    FROM `log`
                                    ";

            string condition = " WHERE 1";

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
