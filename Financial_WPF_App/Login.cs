using MySql.Data.MySqlClient;
using Syncfusion.XlsIO.Implementation.XmlSerialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace Financial_WPF_App.Models
{
    internal static class Login
    {
        public static MySqlConnection MyConn;

        //CHARSET=utf8 for recognizing Arabic in c#
        public static string ConStr_Online = "";
        public static string ConStr_Offline = "";
        public static string database = "";
        public static string onlineserver = "";
        public static string test_database = "";
        public static string ConnectionString = "";
        public static string OnlineRoot = "";

        public static ShutdownMode Shutdown_Mode = new ShutdownMode();

        public static MySqlCommand MultiAction_sc = new MySqlCommand();
        public static bool MultiAction_AnyError = false;
        public static List<string> MultiAction_List = new List<string>();

        public static MySqlCommand MultiBudgetLines_sc = new MySqlCommand();
        public static bool MultiBudgetLines_AnyError = false;
        public static List<string> MultiBudgetLines_List = new List<string>();

        public static MySqlCommand MissingBudgetLines_sc = new MySqlCommand();
        public static List<string> MissingBudgetLines_List = new List<string>();

        public static MySqlCommand ActionApproval_sc = new MySqlCommand();
        public static List<string> ActionApproval_List = new List<string>();

        public static MySqlCommand MultiBeneficiary_sc = new MySqlCommand();
        public static bool MultiBeneficiary_AnyError = false;
        public static List<string> MultiBeneficiary_List = new List<string>();

        public static MySqlCommand MultiPayments_sc = new MySqlCommand();
        public static bool MultiPayments_AnyError = false;
        public static List<string> MultiPayments_List = new List<string>();

        public static MySqlCommand MultiSalaries_sc = new MySqlCommand();
        public static bool MultiSalaries_AnyError = false;
        public static List<string> MultiSalaries_List = new List<string>();

        public static MySqlCommand DeleteMultiActions_sc = new MySqlCommand();
        public static bool DeleteMultiActions_AnyError = false;
        public static List<string> DeleteMultiActions_List = new List<string>();

        public static MySqlCommand DeleteMultiMonthlySalaries_sc = new MySqlCommand();
        public static bool DeleteMultiMonthlySalaries_AnyError = false;
        public static List<string> DeleteMultiMonthlySalaries_List = new List<string>();

        public static MySqlCommand DeleteMultiCashActions_sc = new MySqlCommand();
        public static bool DeleteMultiCashActions_AnyError = false;
        public static List<string> DeleteMultiCashActions_List = new List<string>();

        public static MySqlCommand DeleteMultiBeneficiary_sc = new MySqlCommand();
        public static bool DeleteMultiBeneficiary_AnyError = false;
        public static List<string> DeleteMultiBeneficiary_List = new List<string>();


        public class AttachmentData
        {
            public List<string> Paths { get; set; }
            public int IsApproved { get; set; }
        }
        public static bool MultiAttachment_AnyError = false;

        public static Dictionary<int, AttachmentData> MultiAttachment_Dictionary = new Dictionary<int, AttachmentData>();


        public static void Set_ConnectionString(bool status)
        {
            //Should Change Before Publishing
            database = "hcsyria_financial";      // hcsyria_financial_test";
            test_database = "hcsyria_financial"; // hcsyria_financial_test";
            onlineserver  = "45.9.62.87";        // hcfinancial
            string localserver = "localhost";    // password@11

            ConStr_Online = @"Server=" + onlineserver + ";Port=3306;Database=" + test_database +
                            ";Uid=hcfinancial;Password=Center2018Hope;Convert Zero Datetime=True;CHARSET=utf8;Connect Timeout=21600;AllowUserVariables=True;";

            ConStr_Offline = @"Server=" + localserver + ";username=jj;password=pp;database=" + test_database +
                         ";Convert Zero Datetime=True;CHARSET=utf8;Connect Timeout=21600;AllowUserVariables=True;";

            OnlineRoot = "sftp://45.9.62.87/files"; // sftp://45.9.62.87/files/test

            if (status) //online
                ConnectionString = ConStr_Online;
            else //offline
                ConnectionString = ConStr_Offline;
        }

        public static bool Get_ConnectionString()
        {
            var status = false;
            if (ConnectionString == ConStr_Online)
                status = true; //online
            else
                status = false; //offline
            return status;
        }

        public static bool BuildConnection()
        {
            MyConn = new MySqlConnection(ConnectionString);
            if (MyConn.State == ConnectionState.Closed)
            {
                MyConn.Open();
                return true;
            }
            else
                return false;
        }

        public static bool Login_BuildConnection()
        {
            MyConn = new MySqlConnection(ConnectionString);
            if (MyConn.State == ConnectionState.Closed)
            {
                MyConn.Open();
                return true;
            }
            else return false;
        }

        public static void CloseAndClearConnection()
        {
            MyConn.Close();
            MyConn.ClearAllPoolsAsync();
        }

        public static string ReplaceQuotation(string value)
        {
            value = value.Replace("'", "''");
            return value;
        }

        public static string ConvertToDouble(string value)
        {
            double d;
            if (double.TryParse(value, out d) && !Double.IsNaN(d) && !Double.IsInfinity(d))
                return d.ToString("N2");
            else
                return "";
        }

        public static void Execute_Query(string qqq)
        {
            if (Login.BuildConnection())
            {
                string query = " START TRANSACTION;";
                query += qqq;
                query += " COMMIT;";

                MySqlCommand sc = new MySqlCommand();
                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                sc.ExecuteNonQuery();
                Login.CloseAndClearConnection();
            }
        }
        
    }
}
