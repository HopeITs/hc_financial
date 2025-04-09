using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Financial_WPF_App.Helper
{
    internal class MultiSalaries_CustomCopyPaste : GridCutCopyPaste
    {
        public event EventHandler PasteCompleted;

        private DataTable _Employee_Subprojects_dt;

        public int TotalPastedRows { get; private set; } = 0;
        public int TotalErrorRows { get; private set; } = 0;

        int _Employee_ID = -1;
        int _Employee_Subproject_ID = -1;
        string _Line_Name = "";
        string _Month = "";
        Double _Basic = 0;
        Double _Adjustment = 0;
        Double _USD_Basic = 0;
        Double _USD_Adjustment = 0;
        string _Form_Name = "Add_MultiSalaries_Window";


        int k = -1;

        DataTable dt;

        public MultiSalaries_CustomCopyPaste(SfDataGrid dataGrid, DataTable Employee_Subprojects_dt) : base(dataGrid)
        {
            _Employee_Subprojects_dt = Employee_Subprojects_dt ?? new DataTable();

            if (dataGrid.ItemsSource != null)
            {
                dt = ((DataView)this.dataGrid.ItemsSource).Table;
                dt.ColumnChanging += new DataColumnChangeEventHandler(Column_Changing);
            }

        }

        protected override void PasteToRows(object clipBoardRows)
        {
            var copiedRecord = (string[])clipBoardRows;
            int copiedRecordsCount = copiedRecord.Count();


            if (dt.Rows.Count == 1)
            {
                bool onlyEmpty = true;

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (!String.IsNullOrEmpty(dt.Rows[0][i].ToString()) || !String.IsNullOrWhiteSpace(dt.Rows[0][i].ToString()))
                    {
                        onlyEmpty = false;
                    }
                }
                if (onlyEmpty)
                {
                    dt.Rows.Clear();
                }
            }

            var splitRecords = copiedRecord.Where(row => !string.IsNullOrWhiteSpace(row))
                                         .Select(row => Regex.Split(row, @"\t"))
                                         .ToList();

            if (!splitRecords.All(values => values.Length == dt.Columns.Count))
            {
                MessageBox.Show("Please make sure the number of columns are equal.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                return;
            }

            splitRecords.ForEach(values =>
            {
                var row = dt.NewRow();
                for (int i = 0; i < values.Length; i++)
                {
                    row[i] = values[i];
                }
                dt.Rows.Add(row);
            });

            this.dataGrid.ItemsSource = dt.DefaultView;
            OnPasteCompleted();
        }

        private void clear_vlaues()
        {
            _Employee_ID = -1;
            _Employee_Subproject_ID = -1;
            _Line_Name = "";
            _Month = "";
            _Basic = 0;
            _Adjustment = 0;
            _USD_Basic = 0;
            _USD_Adjustment = 0;
        }

        private void Column_Changing(object sender, DataColumnChangeEventArgs e)
        {
            string Last_Column_Name = e.Row.Table.Columns[e.Row.Table.Columns.Count - 1].ColumnName;

            if (e.Column.ColumnName == "Name")
            {
                bool exists = false;
                if (_Employee_Subprojects_dt.Rows.Count > 0)
                    _Employee_ID = _Employee_Subprojects_dt.AsEnumerable()
                            .Where(row => row.Field<string>("Employee_Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
                            .Select(row => row.Field<int>("Employee_ID"))
                            .DefaultIfEmpty(-1)
                            .FirstOrDefault();

                if (_Employee_ID != -1)
                    exists = true;

                if (exists)
                {

                }
                else
                {
                    Login.MultiSalaries_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This Name does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Center")
            {
                bool exists = false;

                if (_Employee_Subprojects_dt.Rows.Count > 0)
                    exists = _Employee_Subprojects_dt.AsEnumerable().Any(row => row.Field<string>("Employee_Name")?.ToLower().Trim().Equals(e.Row["Name"].ToString().ToLower().Trim()) == true
                && row.Field<string>("SubprojectName")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true);

                if (exists)
                {

                }
                else
                {
                    Login.MultiSalaries_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This Center does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Position")
            {
                bool exists = false;
                if (_Employee_Subprojects_dt.Rows.Count > 0)
                    _Employee_Subproject_ID = _Employee_Subprojects_dt.AsEnumerable()
                            .Where(row => row.Field<string>("Employee_Name")?.ToLower().Trim().Equals(e.Row["Name"].ToString().ToLower().Trim()) == true
                             && row.Field<string>("SubprojectName")?.ToLower().Trim().Equals(e.Row["Center"].ToString().ToLower().Trim()) == true
                             && row.Field<string>("LineName")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
                            .Select(row => row.Field<int>("Emplyee_Subproject_ID"))
                            .DefaultIfEmpty(-1)
                            .FirstOrDefault();

                if (_Employee_Subproject_ID != -1)
                {
                    exists = true;
                    _Line_Name = e.ProposedValue.ToString();
                }

                if (exists)
                {

                }
                else
                {
                    Login.MultiSalaries_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This Position does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Month")
            {
                bool exists = false;
                string[] formats = {
                                        "M-yyyy", "MM-yyyy", "MMM-yyyy", "MMMM-yyyy",
                                        "M/yyyy", "MM/yyyy", "MMM/yyyy", "MMMM/yyyy",
                                        "yyyy-M", "yyyy-MM", "yyyy/MM", "yyyy-MMM", "yyyy-MMMM",
                                        "MMMyyyy", "MMM yyyy", "MMMM yyyy",
                                        "MMM-yy", "MMMM-yy", "MM-yy", "M-yy",
                                        "MMM/yy", "MMMM/yy", "yy-MMM", "yy-MM", "yy/MM",
                                        "MMM yy", "MMMM yy"
                                    };

                DateTime Adate;
                bool isDate = DateTime.TryParseExact(
                         e.ProposedValue.ToString(),
                         formats,
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.None,
                         out Adate);

                if (isDate)
                {
                    exists = true;
                    _Month = Adate.ToString("yyyy/MM/01");
                }

                if (!exists)
                {
                    Login.MultiSalaries_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts Dates.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Basic")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _Basic);

                if (!isNumber)
                {
                    Login.MultiSalaries_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Adjustment")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _Adjustment);

                if (!isNumber)
                {
                    Login.MultiSalaries_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "USD Basic")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _USD_Basic);

                if (!isNumber)
                {
                    Login.MultiSalaries_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "USD Adjustment")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _USD_Adjustment);

                if (!isNumber)
                {
                    Login.MultiSalaries_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }


            if (e.Column.ColumnName == Last_Column_Name)
            {
                TotalPastedRows++;
                if (e.Row.RowError == "There are errors in this row.")
                    TotalErrorRows++;

                if (!Login.MultiSalaries_AnyError)
                {
                    build_insert_query();
                }
                else
                {
                    Login.MultiSalaries_List.Clear();
                    Login.MultiSalaries_sc.Parameters.Clear();
                }

                clear_vlaues();
            }
        }

        public void build_insert_query()
        {
            k++;
            string temp_query = "";
            string log_query = "";

            Login.MultiSalaries_sc.Parameters.AddWithValue("@Employee_ID" + k, _Employee_ID);
            Login.MultiSalaries_sc.Parameters.AddWithValue("@Employee_Subproject_ID" + k, _Employee_Subproject_ID);
            Login.MultiSalaries_sc.Parameters.AddWithValue("@Line_Name" + k, _Line_Name);
            Login.MultiSalaries_sc.Parameters.AddWithValue("@Month" + k, _Month);
            Login.MultiSalaries_sc.Parameters.AddWithValue("@Basic" + k, _Basic);
            Login.MultiSalaries_sc.Parameters.AddWithValue("@Adjustment" + k, _Adjustment);
            Login.MultiSalaries_sc.Parameters.AddWithValue("@USD_Basic" + k, _USD_Basic);
            Login.MultiSalaries_sc.Parameters.AddWithValue("@USD_Adjustment" + k, _USD_Adjustment);


            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
            Login.MultiSalaries_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

            Login.MultiSalaries_sc.Parameters.AddWithValue("@Form_Name" + k, _Form_Name);

            Login.MultiSalaries_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);

            temp_query = @" INSERT INTO `employee_salary` (`employee_subproject_id`, `basic`, `basic_usd`, `adjustment`, `adjustment_usd`, `month`) 
                                        VALUES (@Employee_Subproject_ID" + k + ", @Basic" + k + ", @USD_Basic" + k + ",@Adjustment" + k + ",@USD_Adjustment" + k + ",@Month" + k + ");";


            temp_query += " SET @employee_salary_ID" + k + " = Last_Insert_ID();";

            temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'employee_salary', @employee_salary_id" + k + ", CONCAT('Employee ID = ', @Employee_ID" + k + "), CONCAT('Line Name = ',  @Line_Name" + k + "), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

            Login.MultiSalaries_List.Add(temp_query);
            temp_query = "";

        }

        protected virtual void OnPasteCompleted()
        {
            PasteCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
