using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class SubProject
    {
        public SubProject()
        {

        }

        public DataTable Select(string ids, string names, string project_ids, string city_ids)
        {
            //check user from database
            string query = @"select subproject.*
                            ,project.Category_ID ,project.Name as 'Project'
                            ,(select Name from city where ID = subproject.City_ID) as 'City'
                            ,(select Name from category where ID = project.Category_ID) as 'category'
                            from `subproject` 
                            left join `project` on subproject.Project_ID = project.ID ";

            string condition = " where 1 ";
            if (ids != "")
            {
                condition += " and subproject.ID in (" + ids + ") ";
            }

            if (names != "")
            {
                condition += " and subproject.Name in (" + names + ") ";
            }

            if (project_ids != "")
            {
                condition += " and subproject.Project_ID in (" + project_ids + ")";
            }

            if (city_ids != "")
            {
                condition += " and subproject.City_ID in (" + city_ids + ")";
            }
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
        } //Checked

        public int Insert(string name, string beginDate, string endDate, int Project_ID, int City_ID, List<string> All_donors, bool WithPaymentAccount, string Form_Name)
        {
            int SUB_ID = -1;
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@name", name);
                if (beginDate == "NULL")
                    sc.Parameters.AddWithValue("@beginDate", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@beginDate", beginDate);
                if (endDate == "NULL")
                    sc.Parameters.AddWithValue("@endDate", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@endDate", endDate);

                if (Project_ID == -1)
                    sc.Parameters.AddWithValue("@Project_ID", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@Project_ID", Project_ID);

                if (City_ID == -1)
                    sc.Parameters.AddWithValue("@City_ID", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@City_ID", City_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += "START TRANSACTION;";

                //subproject
                query += " INSERT INTO `subproject` ( `Name`, `BeginDate`, `EndDate`, `Project_ID`, `City_ID`)" +
                    " VALUES (@name, @beginDate, @endDate, @Project_ID, @City_ID);";

                query += " SET @SUB_ID = Last_Insert_ID();";


                // Insert All donors and payment account for each donor
                if (All_donors.Count > 0)
                    for (int i = 0; i < All_donors.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@donor" + i, All_donors[i]);

                        query += " INSERT INTO subproject_donor (SubProject_ID,Donor_ID) VALUES (@SUB_ID,(select ID from donor where Name like @donor" + i + "));";

                        // A subproject's payment accounts are created in the CURRENT local currency,
            // not the one named in code. The old value was 'SYR', so every new payment
            // account would have kept being created in a superseded currency.
            //
            // Needs the matching row in `amounttype`, which is a separate table from
            // `tilltypes` - Section 3b of the Phase 1 script adds it. Without that row the
            // subquery yields NULL and the account is created with no amount type at all,
            // silently, because the column accepts NULL.
            if (WithPaymentAccount)
                        {
                            query += " INSERT INTO account(Name, ProjectNumber, BeginDate, EndDate, TotalAmount, AmountType_ID, IsClosed, SubProject_ID, Accounttype_id)" +
                                        " VALUES (CONCAT(@donor" + i + ",' Payments'), '' , @beginDate , @endDate, 0, (SELECT ID FROM amounttype WHERE LOWER(amounttype.NAME) = LOWER('" + Helper.CurrencyService.GetCurrentLocalCurrencyName() + "')), 0, @SUB_ID,'1');";

                            query += " SET @Acc_ID" + i + " = Last_Insert_ID();";

                            query += " INSERT INTO account_donor (Account_id, Donor_id) VALUES (@Acc_ID" + i + ", ((select ID from donor where Name like @donor" + i + ")));";


                            // Tills
                            query += " INSERT INTO `till`(`Type_ID`,`Account_ID`)  SELECT ID, @Acc_ID" + i + " FROM tilltypes WHERE LOWER(NAME) = LOWER('pay');";

                            query += " SET @Till_ID" + i + " = Last_Insert_ID();";
                        }

                    }

                query += " SELECT @SUB_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'subproject', @SUB_ID, CONCAT('subproject Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (All_donors.Count > 0)
                    for (int i = 0; i < All_donors.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'subproject_donor', '' ,  CONCAT('Dononr Name = ',  @donor" + i + "), CONCAT('Auto created with subproject ID = ', @SUB_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                        if (WithPaymentAccount)
                        {
                            query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'account', @Acc_ID" + i + ", CONCAT('account Name = ', CONCAT(@donor" + i + ",' Payments')), CONCAT('Auto created with subproject ID = ', @SUB_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                            query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'till', @Till_ID" + i + ", CONCAT('till Type = ', 'pay'), CONCAT('Auto created with subproject ID = ', @SUB_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        }
                    }

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                SUB_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }

            return SUB_ID;
        }  //Checked

        public void Update(int ID, string name, string beginDate, string endDate, int Project_ID, int City_ID, List<string> Add_donors, List<string> Delete_donors, bool WithPaymentAccount, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@name", name);
                if (beginDate == "NULL")
                    sc.Parameters.AddWithValue("@beginDate", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@beginDate", beginDate);
                if (endDate == "NULL")
                    sc.Parameters.AddWithValue("@endDate", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@endDate", endDate);

                if (Project_ID == -1)
                    sc.Parameters.AddWithValue("@Project_ID", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@Project_ID", Project_ID);

                if (City_ID == -1)
                    sc.Parameters.AddWithValue("@City_ID", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@City_ID", City_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "UPDATE subproject SET Name = @name, BeginDate = @beginDate, EndDate = @endDate, Project_ID = @Project_ID, City_ID = @City_ID  WHERE ID = @ID;";


                if (Delete_donors.Count > 0)
                    for (int i = 0; i < Delete_donors.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@Deletedonor" + i, Delete_donors[i]);

                        if (WithPaymentAccount)
                        {
                            query += " set @deletedAccID" + i + " = ( select GROUP_CONCAT(`account`.`id`) FROM `account` left join account_donor on account.id = account_donor.account_id left join till on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where SubProject_ID = @ID AND account_donor.Donor_ID = (select ID from donor where Name like @Deletedonor" + i + ") and tilltypes.name like 'pay');";
                            query += " set @deletedAccName" + i + " = ( select GROUP_CONCAT(`account`.`name`) FROM `account` left join account_donor on account.id = account_donor.account_id left join till on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id  where SubProject_ID = @ID AND account_donor.Donor_ID = (select ID from donor where Name like @Deletedonor" + i + ") and tilltypes.name like 'pay');";

                            //delete payment account
                            query += " DELETE FROM `account_donor` where account_donor.account_id in (@deletedAccID" + i + ");";
                            query += " DELETE FROM `account` where id in (@deletedAccID" + i + ");";
                        }


                        //delete all accounts
                        query += " DELETE `account` FROM `account` left join account_donor on account.id = account_donor.account_id where SubProject_ID = @ID AND account_donor.Donor_ID = (select ID from donor where Name like @Deletedonor" + i + ");";
                        query += " DELETE FROM `subproject_donor` where SubProject_ID = @ID AND Donor_ID = (select ID from donor where Name like @Deletedonor" + i + ");";
                    }

                if (Add_donors.Count > 0)
                    for (int i = 0; i < Add_donors.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@Adddonor" + i, Add_donors[i]);
                        query += " INSERT INTO subproject_donor (SubProject_ID,Donor_ID) VALUES (@ID,(select ID from donor where Name like @Adddonor" + i + "));";

                        if (WithPaymentAccount)
                        {
                            query += " INSERT INTO account (Name, ProjectNumber, BeginDate, EndDate, TotalAmount, AmountType_ID, IsClosed, SubProject_ID, Accounttype_id)" +
                                        " VALUES (CONCAT(@Adddonor" + i + ",' Payments'), '' , @beginDate , @endDate, 0, (SELECT ID FROM amounttype WHERE LOWER(amounttype.NAME) = LOWER('" + Helper.CurrencyService.GetCurrentLocalCurrencyName() + "')), 0, @ID, '1');";

                            query += " SET @Acc_ID" + i + " = Last_Insert_ID();";

                            query += " INSERT INTO account_donor (Account_id, Donor_id) VALUES (@Acc_ID" + i + ", ((select ID from donor where Name like @Adddonor" + i + ")));";


                            // Tills
                            query += " INSERT INTO `till`(`Type_ID`,`Account_ID`)  SELECT ID, @Acc_ID" + i + " FROM tilltypes WHERE LOWER(NAME) = LOWER('pay');";
                            query += " SET @Till_ID" + i + " = Last_Insert_ID();";

                        }

                    }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'subproject', @ID, CONCAT('subproject Name = ', @name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (Delete_donors.Count != 0)
                {
                    for (int i = 0; i < Delete_donors.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'subproject_donor', '', CONCAT('Dononr Name = ', @Deletedonor" + i + "), CONCAT('Auto deleted with subproject ID = ', @ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        if (WithPaymentAccount)
                            query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'account', @deletedAccID" + i + ",  CONCAT('account Name = ', @deletedAccName" + i + "), CONCAT('Auto deleted with subproject ID = ', @ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                if (Add_donors.Count > 0)
                    for (int i = 0; i < Add_donors.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'subproject_donor', '' ,  CONCAT('Dononr Name = ',  @Adddonor" + i + "), CONCAT('Auto created with subproject ID = ', @ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                        if (WithPaymentAccount)
                        {
                            query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'account', @Acc_ID" + i + ", CONCAT('account Name = ', CONCAT(@Adddonor" + i + ",' Payments')), CONCAT('Auto created with subproject ID = ', @ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                            query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'till', @Till_ID" + i + ", CONCAT('till Type = ', 'pay'), CONCAT('Auto created with subproject ID = ', @ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        }
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

        public void Delete(int ID, string Sub_Name, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Sub_Name", Sub_Name);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += "START TRANSACTION;";
                query += " DELETE `account_donor` FROM `account_donor` left join account on account_donor.account_id = account.id WHERE account.SubProject_ID = @ID;";
                query += " DELETE FROM `subproject` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'subproject', @ID, CONCAT('subproject Name = ', @Sub_Name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }

    }
}
