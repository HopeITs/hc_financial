using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Mysqlx.Crud;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Financial_WPF_App.Helper
{
    internal class MultiBudgetLines_CustomCopyPaste : GridCutCopyPaste
    {
        public event EventHandler PasteCompleted;
        private DataTable _MissingLiens_dt;

        public int TotalPastedRows { get; private set; } = 0;
        public int TotalErrorRows { get; private set; } = 0;

        int _Budget_ID;
        string _Budget_Name;


        int _Line_ID = -1;
        string _Line_Name = "";
        string _Form_Name = "Add_Multi_Budget_Lines";
        int _Unit = 0;
        int _Timing = 0;
        double _Price = 0;


        int k = -1;

        DataTable dt;


        public MultiBudgetLines_CustomCopyPaste(SfDataGrid dataGrid, int Budget_ID, string Budget_Name, DataTable MissingLiens_dt)
            : base(dataGrid)
        {
            _Budget_ID = Budget_ID;
            _Budget_Name = Budget_Name;

            _MissingLiens_dt = MissingLiens_dt ?? new DataTable();

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
                MessageBox.Show("Please make sure the number of columns are equal.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
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
            _Line_ID = -1;
            _Line_Name = "";
            _Unit = 0;
            _Timing = 0;
            _Price = 0;
        }


        private void Column_Changing(object sender, DataColumnChangeEventArgs e)
        {
            string Last_Column_Name = e.Row.Table.Columns[e.Row.Table.Columns.Count - 1].ColumnName;

            if (e.Column.ColumnName == "Line")
            {
                bool exists = false;


                if (_MissingLiens_dt.Rows.Count > 0)
                    _Line_ID = _MissingLiens_dt.AsEnumerable()
                             .Where(row => row.Field<string>("Line")?.Equals(e.ProposedValue.ToString()) == true)
                             .Select(row => row.Field<int>("Line_ID"))
                             .DefaultIfEmpty(-1)
                             .FirstOrDefault();

                if (_Line_ID != -1)
                    exists = true;

                if (exists)
                {
                    _Line_Name = e.ProposedValue.ToString();
                }
                if (!exists)
                {
                    Login.MultiBudgetLines_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This line does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Unit")
            {
                bool isNumber = int.TryParse(e.ProposedValue.ToString(), out _Unit);

                if (!isNumber)
                {
                    Login.MultiBudgetLines_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Timing")
            {
                bool isNumber = int.TryParse(e.ProposedValue.ToString(), out _Timing);

                if (!isNumber)
                {
                    Login.MultiBudgetLines_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Price")
            {
                bool isNumber = double.TryParse(e.ProposedValue.ToString(), out _Price);

                if (!isNumber)
                {
                    Login.MultiBudgetLines_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }

            if (e.Column.ColumnName == Last_Column_Name)
            {
                TotalPastedRows++;
                if (e.Row.RowError == "There are errors in this row.")
                    TotalErrorRows++;

                if (!Login.MultiBudgetLines_AnyError)
                {
                    build_insert_query();            
                }
                else
                {
                    Login.MultiBudgetLines_List.Clear();
                    Login.MultiBudgetLines_sc.Parameters.Clear();
                }

                clear_vlaues();
            }
        }


        public void build_insert_query()
        {
            k++;
            string temp_query = "";
            string log_query = "";

            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Budget_ID" + k, _Budget_ID);
            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Budget_Name" + k, _Budget_Name);

            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Line_ID" + k, _Line_ID);
            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Line_Name" + k, _Line_Name);

            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Unit" + k, _Unit);

            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Timing" + k, _Timing);
            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Price" + k, _Price);


            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);
            Login.MultiBudgetLines_sc.Parameters.AddWithValue("@Form_Name" + k, _Form_Name);


            temp_query += @"INSERT INTO budget_line (budget_line.Budget_ID, budget_line.Line_ID, budget_line.Unit, budget_line.Timing, budget_line.Price) 
                            VALUES (@Budget_ID" + k + " ,@Line_ID" + k + " , @Unit" + k + ", @Timing" + k + ", @Price" + k + ");";

            temp_query += " SET @budget_line_ID" + k + " = Last_Insert_ID();";

            temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'Budget_line', @budget_line_ID" + k + ", CONCAT('Budget Name = ', @Budget_Name" + k + "), CONCAT('Line Name = ',@Line_Name" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

            Login.MultiBudgetLines_List.Add(temp_query);
            temp_query = "";

        }

        protected virtual void OnPasteCompleted()
        {
            PasteCompleted?.Invoke(this, EventArgs.Empty);
        }

    }
}
