using MySql.Data.MySqlClient;
using System.Data;

namespace Financial_WPF_App.Models
{
    internal class Log
    {

        /// <summary>
        /// Pass "" for both dates to read the whole log, which is how this screen behaved
        /// before the range was added. Dates are "yyyy/MM/dd", as on the other screens.
        /// </summary>
        public DataTable Select(string FromDate = "", string ToDate = "")
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

            // Optional range. Both empty reads the whole table, which is what this screen
            // always did and what the "All" item in the year box relies on. Compared against
            // the column directly rather than wrapped in DATE(), so an index on log.date
            // stays usable as the table grows.
            if (FromDate != "")
                condition += " AND log.date >= @FromDate";
            if (ToDate != "")
                condition += " AND log.date <= @ToDate";

            condition += " ORDER BY log.date DESC, log.id DESC";

            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@FromDate", FromDate);
                sc.Parameters.AddWithValue("@ToDate", ToDate);

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
