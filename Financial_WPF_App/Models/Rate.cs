
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    public class Rate
    {
        public Rate()
        {

        }

        public DataTable Select(int Account_ID, int Rate_ID)
        {
            //check user from database
            string query = @"select all_avgrates.`ID` as 'ID',
                            all_avgrates.`ObserveRate_ID` as 'ObserveRate_ID',
                            all_avgrates.`Name` as `Rate group name`,
                            all_avgrates.`BeginDate` as `Start date`,
                            all_avgrates.`EndDate` as `End date`,
                            fromtp.`name` as `From till`,
                            totp.`name` as `To till`,
                            fromtp.`id` as 'fromtp_id',
                            totp.`id` as 'totp_id',
                            all_avgrates.`Account_ID`,
                            all_avgrates.`Value`as `Value`,
                            all_avgrates.`Exchanged` as `Amount exchanged`,
                            all_avgrates.`Exchanged` * all_avgrates.`Value` as `Equivalent amount`,
                            all_avgrates.`Expenditures` as `Expenses Amount`,
                            ifnull(all_avgrates.`Expenditures` / all_avgrates.`Value`,0) as `Expenses Amount In Foreign currency`,
                            ifnull((all_avgrates.`Exchanged` * all_avgrates.`Value`) - all_avgrates.`Expenditures`,0) as `Remaining Amount`,
                            ifnull(((all_avgrates.`Exchanged` * all_avgrates.`Value`) - all_avgrates.`Expenditures`) / all_avgrates.`Value`,0) as `Remaining Amount in Foreign currency`,
                            all_avgrates.`ObserveRate` as `The Corresponding Rate Group`,
                            obs_all_avgrates.value as `The Corresponding Rate Value`,
                            ifnull(all_avgrates.`Exchanged`/obs_all_avgrates.value,0) as `The Corresponding Amount`

                            from all_avgrates left join all_avgrates obs_all_avgrates on obs_all_avgrates.id = all_avgrates.ObserveRate_ID left join tilltypes fromtp on all_avgrates.FromTill =fromtp.id left join tilltypes totp on all_avgrates.ToTill =totp.id ";

            string condition = " where 1 ";

            if (Account_ID != -1) condition += " and all_avgrates.Account_ID = @Account_ID";

            if (Rate_ID != -1) condition += " and all_avgrates.ID = @Rate_ID";

            query += condition;
            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Account_ID", Account_ID);

                sc.Parameters.AddWithValue("@Rate_ID", Rate_ID);

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

        public DataTable Select_unique_toSYR_Rates()
        {
            //check user from database
            string query = @"SELECT DISTINCT (f_tilltypes.ID) as 'TillType_ID' , f_tilltypes.Name as 'TillType_Name'
                            FROM all_avgrates 
                            LEFT JOIN tilltypes f_tilltypes ON all_avgrates.FromTill = f_tilltypes.ID
                            LEFT JOIN tilltypes t_tilltypes ON all_avgrates.ToTill = t_tilltypes.ID
                            WHERE LOWER(t_tilltypes.Name) = 'syr';";


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

        public DataTable Select_Syr_Balances(int TillType_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@TillType_ID", TillType_ID);

            string query = @" CALL Select_Syr_Balances(@TillType_ID);";

            sc.CommandTimeout = 120;
            sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
            sc.Parameters.AddWithValue("@t", 600);
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_PerDonor(int Account_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Account_ID", Account_ID);

            string query = @" CALL Select_PerDonor_AvgRate(@Account_ID);";

            sc.CommandTimeout = 120;
            sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
            sc.Parameters.AddWithValue("@t", 600);
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Check_AvgRate_Availability(string Account_ID, string Date, string FromTill, string ToTill)
        {
            //check user from database
            string query = @"select all_avgrates.`ID` as 'ID', all_avgrates.`Name`, all_avgrates.BeginDate, all_avgrates.EndDate,LOWER(fromtp.Name) as 'fromtp', LOWER(totp.Name) as 'totp', all_avgrates.Account_ID
                        from all_avgrates
                        left join tilltypes fromtp on all_avgrates.FromTill =fromtp.id
                        left join tilltypes totp on all_avgrates.ToTill =totp.id ";

            string condition = " where 1 ";

            if (Account_ID != "") condition += " and all_avgrates.Account_ID = @Account_ID";
            if (Date != "") condition += " and all_avgrates.BeginDate <= @Date and all_avgrates.EndDate >=@Date";

            if (FromTill != "")
            {
                condition += " and fromtp.`name` =@FromTill";
            }
            if (ToTill != "")
            {
                condition += " and totp.`name` =@ToTill";
            }

            query += condition;
            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@Date", Date);
                sc.Parameters.AddWithValue("@FromTill", FromTill);
                sc.Parameters.AddWithValue("@ToTill", ToTill);

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

        public DataTable Select_AllAvilableRates(string Account_ID, string ToTill, bool WithResidual)
        {
            //check user from database
            string query = "select all_avgrates.`ID` as 'ID',";

            if (WithResidual)
            {
                query += " CONCAT(all_avgrates.`Name`,' Remaining ', ifnull((all_avgrates.`Exchanged` * all_avgrates.`Value`) - all_avgrates.`Expenditures`,0)) as 'Name',";
            }
            else
            {
                query += " all_avgrates.`Name` as 'Name',";
            }

            query += " all_avgrates.`BeginDate`, all_avgrates.`EndDate`, fromtp.`name` as 'FromTill'," +
                " totp.`name` as 'ToTill', fromtp.`id` as 'fromtp_id', totp.`id` as 'totp_id'," +
                " all_avgrates.`Value`,all_avgrates.`Expenditures`," +
                " all_avgrates.`Exchanged` * all_avgrates.`Value` AS 'Exchanged_Value'," +
                " all_avgrates.`Account_ID` " +
                " from all_avgrates left join tilltypes fromtp on all_avgrates.FromTill = fromtp.id " +
                " left join tilltypes totp on all_avgrates.ToTill =totp.id ";

            string condition = " where 1 and totp.`name` =@ToTill";

            if (Account_ID != "")
            {
                condition += " and Account_ID = @Account_ID";
            }

            query += condition;
            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@ToTill", ToTill);

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

        public int Insert(int Account_ID, string name, string beginDate, string endDate, int FromTill, int ToTill, int ObserveRate_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Rate_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@name", name);

                sc.Parameters.AddWithValue("@beginDate", beginDate);
                sc.Parameters.AddWithValue("@endDate", endDate);
                sc.Parameters.AddWithValue("@FromTill", FromTill);
                sc.Parameters.AddWithValue("@ToTill", ToTill);

                if (ObserveRate_ID == -1)
                    sc.Parameters.AddWithValue("@ObserveRate_ID", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@ObserveRate_ID", ObserveRate_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"INSERT INTO `rate`(`Account_ID` ,`Name`, `BeginDate`, `EndDate`, `FromTill`, `ToTill`, `ObserveRate_ID`)
                            VALUES (@Account_ID,@name,@beginDate,@endDate,@FromTill,@ToTill, @ObserveRate_ID); ";

                query += " SET @Rate_ID = Last_Insert_ID();";

                query += " SELECT @Rate_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'rate', @Rate_ID, CONCAT('rate Name = ', @name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Rate_ID = Convert.ToInt32(sc.ExecuteScalar());
                Login.CloseAndClearConnection();
            }
            return Rate_ID;
        }

        public void Update(int ID, int Account_ID, string name, string beginDate, string endDate, int FromTill, int ToTill, int ObserveRate_ID, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@name", name);
                sc.Parameters.AddWithValue("@beginDate", beginDate);
                sc.Parameters.AddWithValue("@endDate", endDate);
                sc.Parameters.AddWithValue("@FromTill", FromTill);
                sc.Parameters.AddWithValue("@ToTill", ToTill);

                if (ObserveRate_ID == -1)
                    sc.Parameters.AddWithValue("@ObserveRate_ID", DBNull.Value);
                else
                    sc.Parameters.AddWithValue("@ObserveRate_ID", ObserveRate_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @" UPDATE `rate` SET `Name` = @name ,`BeginDate`= @beginDate,`EndDate`= @endDate,`FromTill`= @FromTill,`ToTill`= @ToTill, `ObserveRate_ID` = @ObserveRate_ID WHERE ID = @ID;";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'rate', @ID, CONCAT('rate Name = ', @name), CONCAT('account ID = ', @Account_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public void Delete(int ID, int Account_ID, string Rate_Name, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@Rate_Name", Rate_Name);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query += "START TRANSACTION;";

                query += "DELETE FROM `rate` WHERE ID = @ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'rate', @ID, CONCAT('rate Name = ', @Rate_Name), CONCAT('account ID = ', @Account_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }


        }

        public Double Calculate_Value(string Account_ID, string BeginDate, string EndDate, int FromTill, int ToTill)
        {
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                string query = @"CALL calculate_avgrate(" + Account_ID + ",'" + BeginDate + "','" + EndDate + "','" + FromTill + "','" + ToTill + "')";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                var n = 0.0;
                Double.TryParse(sc.ExecuteScalar().ToString(), out n);
                Login.CloseAndClearConnection();
                return n;
            }
            return -1;
        }

        public DataTable check_rateDateChange_Avilibility(int Rate_ID, string beginDate, string endDate)
        {
            string query = @"SELECT * FROM action
                                WHERE action.Rate_ID = @Rate_ID
                                and (action.ActionDate > @endDate OR action.ActionDate < @beginDate);";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Rate_ID", Rate_ID);
                sc.Parameters.AddWithValue("@beginDate", beginDate);
                sc.Parameters.AddWithValue("@endDate", endDate);

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
