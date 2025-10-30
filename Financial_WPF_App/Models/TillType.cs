using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial_WPF_App.Models
{
    internal class TillType
    {
        public TillType() { }

        public int ID { get; set; }
        public string Name { get; set; }
    

    public DataTable SelectTillTypes()
        {
            string query = "SELECT id as 'ID', name as 'Name' FROM tilltypes where id <> 1 ORDER BY name;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                using (MySqlCommand sc = new MySqlCommand(query, Login.MyConn))
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter(sc))
                    {
                        da.Fill(dt);
                    }
                }
                Login.CloseAndClearConnection();
            }
            return dt;
        }
    }
}
