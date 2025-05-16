using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Financial_WPF_App.Models
{
    internal class Employee
    {

        public DataTable Select(int Employee_ID, int Subproject_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Employee_ID", Employee_ID);
            sc.Parameters.AddWithValue("@Subproject_ID", Subproject_ID);

            string query = @" SELECT employee.id AS 'Employee_ID',  employee.FirstName as 'FName', employee.FatherName as 'FaName',employee.LastName as 'LName',
                        CASE WHEN employee.FatherName = '' THEN CONCAT(employee.FirstName,' ',employee.LastName) ELSE
                        CONCAT(employee.FirstName,' ',employee.FatherName,' ',employee.LastName) END as 'Name',
                        GROUP_CONCAT(subproject.Name) as 'SubprojectName',GROUP_CONCAT(subproject.id) as 'Subproject_ID', GROUP_CONCAT(line.Name) as 'lineName'
                        FROM employee LEFT JOIN employee_subproject ON employee_subproject.employee_id = employee.ID 
                        left join subproject on subproject.id = employee_subproject.subproject_id
                        left join line on line.id = employee_subproject.line_id WHERE 1";

            if (Employee_ID != -1)
                query += " and employee.id = @Employee_ID";
            if (Subproject_ID != -1)
                query += " and subproject.id = @Subproject_ID";

            query += " GROUP BY employee.id;";

            sc.CommandTimeout = 120; sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public DataTable Select_Detailed_Employee(int Employee_ID)
        {
            MySqlCommand sc = new MySqlCommand();

            string query = @"SELECT employee.id as 'Employee_ID',
                    CASE WHEN employee.FatherName = '' THEN CONCAT(employee.FirstName,' ',employee.LastName) ELSE
                    CONCAT(employee.FirstName,' ',employee.FatherName,' ',employee.LastName) END as 'Employee_Name',

                    employee_subproject.id AS 'Emplyee_Subproject_ID', subproject.ID AS 'Subproject_ID', subproject.Name AS 'SubprojectName', line.ID AS 'Line_ID', line.Name AS 'LineName', employee_subproject.BeginDate AS 'BeginDate', employee_subproject.EndDate AS 'EndDate'

                    FROM employee LEFT JOIN employee_subproject ON employee_subproject.employee_id = employee.ID 
                    left join subproject on subproject.id = employee_subproject.subproject_id
                    left join line on line.id = employee_subproject.line_id WHERE 1";

            if(Employee_ID != -1)
            {
                sc.Parameters.AddWithValue("@Employee_ID", Employee_ID);

                query += " and employee.id = @Employee_ID;";
            }
            else
            {
                query += ";";
            }
            sc.CommandTimeout = 120; sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public DataTable Select_Employee_Salary(int Employee_ID)
        {
            MySqlCommand sc = new MySqlCommand();

            string query = @"SELECT employee_salary.id as 'Employee_Salary_ID', employee.id AS 'Employee_ID',

                                CASE WHEN employee.FatherName = '' THEN CONCAT(employee.FirstName,' ',employee.LastName) ELSE
                                CONCAT(employee.FirstName,' ',employee.FatherName,' ',employee.LastName) END as 'Name',

                                subproject.ID AS 'Subproject_ID', subproject.Name AS 'Subproject', line.id AS 'Line_ID', line.Name AS 'Line', employee_subproject.id AS 'employee_subproject_id', employee_salary.id AS 'employee_salary_id',
                                employee_salary.month AS 'Date', employee_salary.basic AS 'Basic', employee_salary.adjustment AS 'Adjustment',
                                employee_salary.basic_usd AS 'USD_Basic', employee_salary.adjustment_usd AS 'USD_Adjustment'
                                FROM employee LEFT JOIN employee_subproject ON employee.id = employee_subproject.employee_id
                                LEFT JOIN subproject ON subproject.ID = employee_subproject.subproject_id 
                                LEFT JOIN line ON line.ID = employee_subproject.line_id 
                                LEFT JOIN employee_salary ON employee_salary.employee_subproject_id = employee_subproject.id
                                Where employee_salary.employee_subproject_id IS NOT null";


            if (Employee_ID != -1)
            {
                sc.Parameters.AddWithValue("@Employee_ID", Employee_ID);
                query += " AND employee.id = @Employee_ID ";
            }

            query += " ORDER BY subproject.Name ASC;";
            sc.CommandTimeout = 120; sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public DataTable Select_Subproject_Monthly_Employee(int Subproject_ID)
        {
            MySqlCommand sc = new MySqlCommand();
            sc.Parameters.AddWithValue("@Subproject_ID", Subproject_ID);

            string query = @" SELECT employee_salary.id as 'Employee_Salary_ID', employee.id AS `Employee_ID`, CASE WHEN employee.FatherName = '' THEN CONCAT(employee.FirstName,' ',employee.LastName) ELSE
                            CONCAT(employee.FirstName,' ',employee.FatherName,' ',employee.LastName) END AS `Employee_Name`,
                            employee_salary.Month, employee_subproject.subproject_id 
                            FROM employee_subproject LEFT JOIN employee ON employee.id = employee_subproject.employee_id
                            LEFT JOIN employee_salary ON employee_salary.employee_subproject_id = employee_subproject.id
                            WHERE 1 AND employee_salary.id is not null";

            if (Subproject_ID != -1)
            {
                query += " AND employee_subproject.subproject_id = @Subproject_ID ";
            }


            sc.CommandTimeout = 120; sc.CommandText = query;
            sc.Connection = Login.MyConn;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.MyConn.Close();
            }
            return dt;
        }

        public int Insert(string FirstName, string FatherName, string LastName, DataTable Add_Subprojects, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Employee_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@FirstName", FirstName);
                sc.Parameters.AddWithValue("@FatherName", FatherName);
                sc.Parameters.AddWithValue("@LastName", LastName);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"INSERT INTO `employee`(`FirstName`, `FatherName`, `LastName`)
                            VALUES(@FirstName, @FatherName, @LastName);";

                query += " SET @Employee_ID = Last_Insert_ID();";


                if (Add_Subprojects.Rows.Count > 0)
                    for (int i = 0; i < Add_Subprojects.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@Addsubproject_id" + i, Add_Subprojects.Rows[i]["Subproject_ID"]);
                        sc.Parameters.AddWithValue("@Addsubproject_Name" + i, Add_Subprojects.Rows[i]["SubprojectName"]);

                        sc.Parameters.AddWithValue("@Addline_id" + i, Add_Subprojects.Rows[i]["Line_ID"]);
                        sc.Parameters.AddWithValue("@Addline_Name" + i, Add_Subprojects.Rows[i]["LineName"]);

                        sc.Parameters.AddWithValue("@AddBeginDate" + i, Add_Subprojects.Rows[i]["BeginDate"]);

                        if (Add_Subprojects.Rows[i]["EndDate"] != null)
                        {
                            sc.Parameters.AddWithValue("@AddEndDate" + i, Add_Subprojects.Rows[i]["EndDate"]);
                        }
                        else
                        {
                            sc.Parameters.AddWithValue("@AddEndDate" + i, DBNull.Value);

                        }

                        query += @" INSERT INTO `employee_subproject`(`employee_id`, `subproject_id`, `line_id`, `BeginDate`, `EndDate`) 
                                        VALUES (@Employee_ID, @Addsubproject_id" + i + ", @Addline_id" + i + ",@AddBeginDate" + i + ",@AddEndDate" + i + ");";

                        query += " SET @employee_subproject_id" + i + " = Last_Insert_ID();";

                    }


                query += " SELECT @Employee_ID;";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'employee', @Employee_ID, CONCAT('Employee Name = ', @FirstName, ' ', @FatherName,' ', @LastName), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (Add_Subprojects.Rows.Count > 0)
                    for (int i = 0; i < Add_Subprojects.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'employee_subproject', @employee_subproject_id" + i + " , CONCAT('Employee ID = ',  @Employee_ID),  CONCAT('Subproject Name = ',  @Addsubproject_Name" + i + "), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }

                query += " COMMIT;";

                sc.CommandTimeout = 120; sc.CommandText = query;
                sc.Connection = Login.MyConn;
                Employee_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.MyConn.Close();
            }
            return Employee_ID;
        }

        public void Update(int Employee_ID, string FirstName, string FatherName, string LastName, DataTable Add_Subprojects, DataTable Delete_Subprojects, DataTable Update_Subprojects, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();


                sc.Parameters.AddWithValue("@Employee_ID", Employee_ID);
                sc.Parameters.AddWithValue("@FirstName", FirstName);
                sc.Parameters.AddWithValue("@FatherName", FatherName);
                sc.Parameters.AddWithValue("@LastName", LastName);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "UPDATE `employee` SET `FirstName` = @FirstName, `LastName` = @LastName,`FatherName` = @FatherName WHERE ID =@Employee_ID;";

                if (Delete_Subprojects.Rows.Count > 0)
                    for (int i = 0; i < Delete_Subprojects.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@Deletesubproject_id" + i, Delete_Subprojects.Rows[i]["Emplyee_Subproject_ID"]);
                        sc.Parameters.AddWithValue("@Deletesubproject_Name" + i, Delete_Subprojects.Rows[i]["SubprojectName"]);
                        sc.Parameters.AddWithValue("@DeleteLine_Name" + i, Delete_Subprojects.Rows[i]["LineName"]);

                        query += @" DELETE FROM employee_subproject where id = @Deletesubproject_id" + i + ";";
                    }

                if (Add_Subprojects.Rows.Count > 0)
                    for (int i = 0; i < Add_Subprojects.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@Addsubproject_id" + i, Add_Subprojects.Rows[i]["Subproject_ID"]);
                        sc.Parameters.AddWithValue("@Addsubproject_Name" + i, Add_Subprojects.Rows[i]["SubprojectName"]);

                        sc.Parameters.AddWithValue("@Addline_id" + i, Add_Subprojects.Rows[i]["Line_ID"]);
                        sc.Parameters.AddWithValue("@Addline_Name" + i, Add_Subprojects.Rows[i]["LineName"]);

                        sc.Parameters.AddWithValue("@AddBeginDate" + i, Add_Subprojects.Rows[i]["BeginDate"]);

                        if (Add_Subprojects.Rows[i]["EndDate"] != null)
                        {
                            sc.Parameters.AddWithValue("@AddEndDate" + i, Add_Subprojects.Rows[i]["EndDate"]);
                        }
                        else
                        {
                            sc.Parameters.AddWithValue("@AddEndDate" + i, DBNull.Value);

                        }

                        query += @" INSERT INTO `employee_subproject`(`employee_id`, `subproject_id`, `line_id`, `BeginDate`, `EndDate`) 
                                        VALUES (@Employee_ID, @Addsubproject_id" + i + ", @Addline_id" + i + ",@AddBeginDate" + i + ",@AddEndDate" + i + ");";

                        query += " SET @employee_subproject_id" + i + " = Last_Insert_ID();";

                    }


                if (Update_Subprojects.Rows.Count > 0)
                    for (int i = 0; i < Update_Subprojects.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@Updatesubproject_id" + i, Update_Subprojects.Rows[i]["Emplyee_Subproject_ID"]);
                        sc.Parameters.AddWithValue("@UpdateBeginDate" + i, Update_Subprojects.Rows[i]["BeginDate"]);
                        sc.Parameters.AddWithValue("@UpdateEndDate" + i, Update_Subprojects.Rows[i]["EndDate"]);


                        query += @" UPDATE `employee_subproject` set BeginDate = @UpdateBeginDate" + i + ", EndDate = @UpdateEndDate" + i + " WHERE id = @Updatesubproject_id" + i + ";";

                    }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'employee', @Employee_ID, CONCAT('Employee Name = ', @FirstName, ' ', @LastName), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                if (Delete_Subprojects.Rows.Count > 0)
                    for (int i = 0; i < Delete_Subprojects.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'employee_subproject', @Deletesubproject_id" + i + " ,  CONCAT('Subproject Name = ',  @Deletesubproject_Name" + i + "), CONCAT('Line Name = ', @DeleteLine_Name" + i + "), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }

                if (Add_Subprojects.Rows.Count > 0)
                    for (int i = 0; i < Add_Subprojects.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'employee_subproject', @employee_subproject_id" + i + " , CONCAT('Employee ID = ',  @Employee_ID),  CONCAT('Subproject Name = ',  @Addsubproject_Name" + i + "), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }

                query += " COMMIT;";

                sc.CommandTimeout = 120; sc.CommandText = query;
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }

        public void Delete(int Employee_ID, string Employee_Name, string Form_Name)
        {
           
                string query = "";
                string log_query = "";

                if (Login.BuildConnection())
                {
                    MySqlCommand sc = new MySqlCommand();

                    sc.Parameters.AddWithValue("@Employee_ID", Employee_ID);
                    sc.Parameters.AddWithValue("@Employee_Name", Employee_Name);
                    sc.Parameters.AddWithValue("@Form_Name", Form_Name);


                    query += "START TRANSACTION;";

                    query += "DELETE FROM `employee` WHERE ID = @Employee_ID;";

                    log_query = Regex.Replace(query, "'", "''");

                    foreach (MySqlParameter p in sc.Parameters)
                    {
                        log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                    }

                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'employee', @Employee_ID, CONCAT('Employee Name = ', @Employee_Name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                    query += " COMMIT;";

                    sc.CommandTimeout = 120; sc.CommandText = query;
                    sc.Connection = Login.MyConn;
                    sc.ExecuteScalar();

                    Login.MyConn.Close();
                }
            
        }

        public void Insert_Multi_Salaries()
        {
            string query = "";


            query += "START TRANSACTION;";
            query += string.Join(Environment.NewLine, Login.MultiSalaries_List.ToArray());
            query += " COMMIT;";

            if (Login.BuildConnection())
            {
                Login.MultiSalaries_sc.CommandTimeout = 120; Login.MultiSalaries_sc.CommandText = query;
                Login.MultiSalaries_sc.Connection = Login.MyConn;

                Login.MultiSalaries_sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }

        public void Delete_Employee_Salary(int Employee_Salary_ID, int Employee_ID, string Line_Name, string Form_Name)
        {
        
                string query = "";
                string log_query = "";

                if (Login.BuildConnection())
                {
                    MySqlCommand sc = new MySqlCommand();

                    sc.Parameters.AddWithValue("@Employee_Salary_ID", Employee_Salary_ID);
                    sc.Parameters.AddWithValue("@Employee_ID", Employee_ID);
                    sc.Parameters.AddWithValue("@Line_Name", Line_Name);
                    sc.Parameters.AddWithValue("@Form_Name", Form_Name);


                    query += "START TRANSACTION;";

                    query += "DELETE FROM `employee_salary` WHERE ID = @Employee_Salary_ID;";

                    log_query = Regex.Replace(query, "'", "''");

                    foreach (MySqlParameter p in sc.Parameters)
                    {
                        log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                    }

                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'employee_salary', @Employee_Salary_ID, CONCAT('Employee ID = ', @Employee_ID), CONCAT('Line Name = ',  @Line_Name), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                    query += " COMMIT;";

                    sc.CommandTimeout = 120; sc.CommandText = query;
                    sc.Connection = Login.MyConn;
                    sc.ExecuteScalar();

                    Login.MyConn.Close();
                }
            
        }

        public void Delete_Multi_Employee_Salary()
        {
            string query = "";


            query += "START TRANSACTION;";

            query += string.Join(Environment.NewLine, Login.DeleteMultiMonthlySalaries_List.ToArray());
            query += " COMMIT;";
            if (Login.BuildConnection())
            {
                Login.DeleteMultiMonthlySalaries_sc.CommandTimeout = 120; Login.DeleteMultiMonthlySalaries_sc.CommandText = query;
                Login.DeleteMultiMonthlySalaries_sc.Connection = Login.MyConn;

                Login.DeleteMultiMonthlySalaries_sc.ExecuteScalar();

                Login.MyConn.Close();
            }
        }

    }
}

