using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Financial_WPF_App.Models
{
    public class SubProject_Donor
    {

        public int Donor_ID { set; get; }

        public string DonorName { set; get; }

        public int SubProject_ID { set; get; }

        public Boolean Check_Status { set; get; }

        public DataTable Select(string subproject_ids, string donors_ids)
        {
            //check user from database
            string query = @"select `Donor_ID`
,(select donor.name from donor where donor.ID = subproject_donor.Donor_ID ) as 'DonorName'
,`SubProject_ID`
,(select donor.Abb_Name from donor where donor.ID = subproject_donor.Donor_ID ) as 'DonorAbb_Name'
 from `subproject_donor`";

            string condition = " where 1 ";

            if (subproject_ids != "" && subproject_ids != "-1")
            {
                condition += " and SubProject_ID in (" + subproject_ids + ")";
            }
            if (donors_ids != "" && donors_ids != "-1")
            {
                condition += " and Donor_ID in (" + donors_ids + ")";
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

        public List<string> Get_SubProject_Donors(int SubProject_ID)
        {
            if (Login.BuildConnection())
            {
                string query = @"select Name 
                         from `donor` join subproject_donor on donor.ID = subproject_donor.Donor_ID 
                         where `SubProject_ID` = " + SubProject_ID;


                MySqlCommand sc = new MySqlCommand(query, Login.MyConn);
                using (var reader = sc.ExecuteReader())
                {
                    var list = new List<string>();
                    while (reader.Read())
                        list.Add(reader.GetString(0));

                    reader.Close();
                    Login.MyConn.Close();
                    return list;
                }
            }
            else
                return null;
        }
    }
}
