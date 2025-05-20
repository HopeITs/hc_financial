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


        public static void Set_ConnectionString(bool status, char selected_hope)
        {

            database = "hcsyria_financial";
            test_database = "hcsyria_financial_test";

            bool test_online = false;
            bool test_offline = false;
            bool test_Attachments = false;
            bool PC_Admin = true;

            string localserver;

            if (!PC_Admin)
                localserver = "localhost";
            else
                localserver = "HC-IT1";

            if (!test_online)
                ConStr_Online = @"Server=mysql.s807.sureserver.com;Port=3306;Database=" + database +
                            ";Uid=hcfinancial;Password=Center2018Hope;Convert Zero Datetime=True;CHARSET=utf8;Connect Timeout=21600;AllowUserVariables=True;";
            else
                ConStr_Online = @"Server=mysql.s807.sureserver.com;Port=3306;Database=" + test_database +
                            ";Uid=hcfinancial;Password=Center2018Hope;Convert Zero Datetime=True;CHARSET=utf8;Connect Timeout=21600;AllowUserVariables=True;";

            if (!test_offline)
                ConStr_Offline = @"Server=" + localserver + ";username=jj;password=pp;database=" + database +
                                     ";Convert Zero Datetime=True;CHARSET=utf8;Connect Timeout=21600;AllowUserVariables=True;";
            else
               ConStr_Offline = @"Server=" + localserver + ";username=jj;password=pp;database=" + test_database +
                         ";Convert Zero Datetime=True;CHARSET=utf8;Connect Timeout=21600;AllowUserVariables=True;";

            if (!test_Attachments)
                OnlineRoot = "ftp://hcsyria.org/financial_attachments/";
            else
                OnlineRoot = "ftp://hcsyria.org/financial_test_attachments/";


            //HC-JUDY1 // PC-ADMIN //port=3306;

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
