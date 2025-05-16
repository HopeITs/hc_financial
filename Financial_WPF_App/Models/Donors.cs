using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Financial_WPF_App.Models
{
    public class Donors
    {
        public Donors()
        {

        }
        public int ID { get; set; }
        public string Name { get; set; }
        public string Abb_Name { get; set; }
        public Boolean Check_Status { set; get; }

        public List<string> Select_List()
        {
            if (Login.BuildConnection())
            {
                string query = "select Name from `donor`";
                MySqlCommand sc = new MySqlCommand(query, Login.MyConn);
                using (var reader = sc.ExecuteReader())
                {
                    var list = new List<string>();
                    while (reader.Read())
                        list.Add(reader.GetString(0));

                    reader.Close();
                    Login.CloseAndClearConnection();
                    return list;
                }
            }
            else
                return null;
        }

        public DataTable Select(string ids, string names, string abb_names)
        {
            //check user from database
            string query = "select * from `donor`";
            string condition = " where 1 ";
            if (ids != "")
            {
                condition += " and ID in (" + ids + ") ";
            }

            if (names != "")
            {
                condition += " and Name in (" + names + ") ";
            }

            if (abb_names != "")
            {
                condition += " and Abb_Name in (" + abb_names + ") ";
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
                Login.CloseAndClearConnection();

            }
            return dt;
        }


    }
}
