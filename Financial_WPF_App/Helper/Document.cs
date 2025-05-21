using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Helper
{
    internal class Document
    {
        //public string OnlineRoot = "ftp://hcsyria.org/financial_test_attachments/";

        Tuple<string, string> OnlineserversCredentials = new Tuple<string, string>("judy", "DAda1994@");

        public void GetNameFromPath(string FullName, out string name, out string Type)
        {
            int LastSlash = FullName.LastIndexOf("\\");
            if (LastSlash == -1) { LastSlash = FullName.LastIndexOf("/"); }
            name = FullName.Substring(LastSlash + 1, FullName.Length - LastSlash - 1);
            int LastDot = name.LastIndexOf(".");
            Type = name.Substring(LastDot + 1, name.Length - LastDot - 1);
        }

        public bool SaveDocument(String FullName, string Name)
        {
            try
            {
                string OnlinePath = Login.OnlineRoot + $@"/{Name}";

                //Saving to online Server 
                FtpConnector.WriteFile(OnlinePath, FullName, OnlineserversCredentials);
                return true;
            }
            catch { return false; }

        }

        public int AddAttachmentToDatabase(int action_id, int beneficiary_id, string Name, bool Approved_Action, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Attachment_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@action_id", action_id);
                sc.Parameters.AddWithValue("@beneficiary_id", beneficiary_id);
                sc.Parameters.AddWithValue("@Name", Name);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);

                query = "START TRANSACTION;";

                if (action_id != -1)
                {
                    query += @"INSERT INTO `action_attachments` (`action_id`, `attach_name`) VALUES (@action_id, @Name);";
                    query += " SET @Attachment_ID = Last_Insert_ID();";


                    if (Approved_Action)
                    {
                        query += "INSERT INTO action_not_approved SELECT * FROM action WHERE action.id = @action_id OR action.ObserveAcion_ID = @action_id;";

                        query += "Update action_not_approved set `IsApproved`= 0,`Approved_at`= null,`Approved_by`= null, `updated_at` = @Todaydate, `edited_by` = @Uesr_ID WHERE action_not_approved.id = @action_id OR action_not_approved.ObserveAcion_ID = @action_id;";

                        query += "DELETE FROM action WHERE action.ObserveAcion_ID = @action_id;";

                        query += "DELETE FROM action WHERE action.id  = @action_id;";
                    }
                }
                else if (beneficiary_id != -1)
                {
                    query += @"INSERT INTO `beneficiary_attachments` (`beneficiary_id`, `attach_name`) VALUES (@beneficiary_id, @Name);";
                    query += " SET @Attachment_ID = Last_Insert_ID();";

                }

                query += " SELECT @Attachment_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                if (action_id != -1)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'action_attachments', @Attachment_ID, CONCAT('Attachment Name = ', @Name), CONCAT('action ID = ', @action_id) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }
                else if (beneficiary_id != -1)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'beneficiary_attachments', @Attachment_ID, CONCAT('Attachment Name = ', @Name), CONCAT('beneficiary ID = ', @beneficiary_id) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Attachment_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }
            return Attachment_ID;
        }

        public void DeleteAttachmentTfromDatabase(int ID, string Name, int Action_Id, int Beneficiary_id, bool Approved_Action, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);
                sc.Parameters.AddWithValue("@Name", Name);
                sc.Parameters.AddWithValue("@Action_Id", Action_Id);
                sc.Parameters.AddWithValue("@Beneficiary_id", Beneficiary_id);

                query += "START TRANSACTION;";

                if (Action_Id != -1)
                {
                    query += "DELETE FROM `action_attachments` WHERE ID = @ID;";

                    if (Approved_Action)
                    {
                        query += "INSERT INTO action_not_approved SELECT * FROM action WHERE action.id = @action_id OR action.ObserveAcion_ID = @action_id;";

                        query += "Update action_not_approved set `IsApproved`= 0,`Approved_at`= null,`Approved_by`= null, `updated_at` = @Todaydate, `edited_by` = @Uesr_ID WHERE action_not_approved.id = @action_id OR action_not_approved.ObserveAcion_ID = @action_id;";

                        query += "DELETE FROM action WHERE action.ObserveAcion_ID = @action_id;";

                        query += "DELETE FROM action WHERE action.id  = @action_id;";
                    }
                }
                else if (Beneficiary_id != -1)
                {
                    query += "DELETE FROM `beneficiary_attachments` WHERE ID = @ID;";
                }
                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }
                if (Action_Id != -1)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'action_attachments', @ID, CONCAT('Attachment Name = ', @Name), CONCAT('action ID = ', @Action_Id) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }
                else if (Beneficiary_id != -1)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'beneficiary_attachments', @ID, CONCAT('Attachment Name = ', @Name), CONCAT('beneficiary ID = ', @Beneficiary_id) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public DataTable GetAttachmentsNames(int action_id, int beneficiary_id)
        {
            string query = "";
            if (action_id != -1)
            {
                query = @"SELECT * 
                            FROM action_attachments
                            WHERE action_id = @action_id;";
            }
            else if (beneficiary_id != -1)
            {
                query = @"SELECT * 
                            FROM beneficiary_attachments
                            WHERE beneficiary_id = @beneficiary_id;";
            }

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@action_id", action_id);
                sc.Parameters.AddWithValue("@beneficiary_id", beneficiary_id);

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

        public bool file_Exist(string Name)
        {
            try
            {
                string Path = Login.OnlineRoot + @"/" + Name;
                return FtpConnector.ExistFile(Path, OnlineserversCredentials);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void CopyToLocal(string OnlinePath, string OfflinePath)
        {
            FtpConnector.DowloadToLocal(OnlinePath, OfflinePath, OnlineserversCredentials);
        }

        public void DeleteOnline(string FullName)
        {
            FtpConnector.DeleteFile(FullName, OnlineserversCredentials);
        }
    }
}
