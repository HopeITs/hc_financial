using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using static Financial_WPF_App.Models.Login;

namespace Financial_WPF_App.Helper
{
    internal class MultiAttachments_CustomCopyPaste : GridCutCopyPaste
    {
        private DataTable _all_Actions_dt, _all_Tills_dt;

        int _Action_ID = -1;
        int _isApproved = -1;
        int _Subproject_ID = -1;
        int _Account_ID = -1;
        int _Till_ID = -1;

        string _Account_Type, _Till_Type;

        List<string> paths = new List<string>(); // Declare a list to store the paths
        string _Form_Name = "Add_MultiActions_Window";

        DataTable _Actions_dt = new DataTable();

        int k = -1;

        DataTable dt;
        List<string> _ShownColumns = new List<string>();


        public MultiAttachments_CustomCopyPaste(SfDataGrid dataGrid, DataTable all_Actions_dt, DataTable all_Tills_dt, string Account_Type, string Till_Type) : base(dataGrid)
        {
            _all_Actions_dt = all_Actions_dt ?? new DataTable();
            _all_Tills_dt = all_Tills_dt ?? new DataTable();
            _Account_Type = Account_Type;
            _Till_Type = Till_Type;
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
        }


        private void clear_vlaues()
        {
            _Actions_dt = null;

            _Action_ID = -1;
            _isApproved = -1;
            _Subproject_ID = -1;
            _Account_ID = -1;
            _Till_ID = -1;

            paths.Clear();
        }

        private void Column_Changing(object sender, DataColumnChangeEventArgs e)
        {
            string Last_Column_Name = e.Row.Table.Columns[e.Row.Table.Columns.Count - 1].ColumnName;

            if (_Account_Type == "Project" && e.Column.ColumnName == "Subproject")
            {
                bool exists = false;

                _Subproject_ID = _all_Tills_dt.AsEnumerable()
                                     .Where(row => row.Field<string>("Subproject_Name")?.ToLower().Equals(e.ProposedValue.ToString().ToLower()) == true)
                                     .Select(row => row.Field<int>("Subproject_ID"))
                                     .DefaultIfEmpty(-1)
                                     .FirstOrDefault();


                if (_Subproject_ID != -1)
                    exists = true;

                if (exists)
                {

                }
                else
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This Subproject does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Account")
            {
                bool exists = false;

                if (_Account_Type == "Project")
                {
                    _Account_ID = _all_Tills_dt.AsEnumerable()
                            .Where(row => row.Field<string>("Subproject_Name")?.Equals(e.Row["Subproject"].ToString()) == true
                            && row.Field<string>("Account_Name")?.Equals(e.ProposedValue.ToString()) == true)
                            .Select(row => row.Field<int>("Account_ID"))
                            .DefaultIfEmpty(-1)
                            .FirstOrDefault();
                }
                else
                {
                    _Account_ID = _all_Tills_dt.AsEnumerable()
                        .Where(row => row.Field<string>("Account_Name")?.Equals(e.ProposedValue.ToString()) == true)
                        .Select(row => row.Field<int>("Account_ID"))
                        .DefaultIfEmpty(-1)
                        .FirstOrDefault();
                }


                if (_Account_ID != -1)
                {
                    _Till_ID = _all_Tills_dt.AsEnumerable()
                        .Where(row => row.Field<string>("Account_Name")?.Equals(e.ProposedValue.ToString()) == true
                        && row.Field<string>("Tilltypes_Name")?.Equals(_Till_Type.ToString()) == true)
                        .Select(row => row.Field<int>("Till_ID"))
                        .DefaultIfEmpty(-1)
                        .FirstOrDefault();
                }

                if (_Till_ID != -1)
                {
                    exists = true;
                }

                if(exists)
                {
                    System.Data.DataRow[] rows = null;
                    if (_all_Actions_dt.Rows.Count > 0)
                    {
                        rows = _all_Actions_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _Actions_dt = rows.CopyToDataTable();
                            }
                        }
                    }
                }
                else
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This Account does not exist or does not contains a match till.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Action ID")
            {
                bool isNumber = Int32.TryParse(e.ProposedValue.ToString(), out _Action_ID);
                bool exists = false;
                bool SourceAction = false;

                if (isNumber)
                {
                    if (_Actions_dt != null && _Actions_dt.Rows.Count > 0)
                    {
                        exists = _Actions_dt.AsEnumerable().Any(row => row.Field<int>("Action_ID") == Convert.ToInt32(e.ProposedValue.ToString()));
                        SourceAction = _Actions_dt.AsEnumerable().Any(row => row.Field<int>("Action_ID") == Convert.ToInt32(e.ProposedValue.ToString()) && row.Field<int>("SourceAction") == 1);
                    }
                }
                if (!isNumber || !exists)
                {
                    Login.MultiAttachment_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This Action ID does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
                else if (!SourceAction)
                {
                    Login.MultiAttachment_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This Action Is not a source action.");
                    e.Row.RowError = "There are errors in this row.";
                }
                else
                {
                    _isApproved = _Actions_dt.AsEnumerable()
                        .Where(row => row.Field<int>("Action_ID") == Convert.ToInt32(e.ProposedValue.ToString()))
                        .Select(row => row.Field<int>("IsApproved"))
                        .DefaultIfEmpty(-1)
                        .FirstOrDefault();
                }
            }
            else if (e.Column.ColumnName == "Attachments pathes")
            {
                var x = e.ProposedValue.ToString();

                var regex = new Regex(@"\""([^\""]*)\""");
                var matches = regex.Matches(x);


                foreach (Match match in matches)
                {
                    if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        paths.Add(match.Groups[1].Value); // Add each path to the list
                    }
                }

                if (paths.Count > 0)
                {
                    foreach (var path in paths)
                    {
                        if (File.Exists(path))
                        {
                            //Login.MultiAttachment_Dictionary[_Action_ID] = paths;
                        }
                        else
                        {
                            Login.MultiAttachment_AnyError = true;
                            e.Row.SetColumnError(e.Column.ColumnName, "Some files do not exist.");
                            e.Row.RowError = "There are errors in this row.";
                        }
                    }
                }
            }

            if (e.Column.ColumnName == Last_Column_Name)
            {
                if (!Login.MultiAttachment_AnyError)
                {
                    Login.MultiAttachment_Dictionary[_Action_ID] = new AttachmentData
                    {
                        Paths = new List<string>(paths),
                        IsApproved = _isApproved,
                    };
                }
                else
                {
                    Login.MultiAttachment_Dictionary.Clear();
                }

                clear_vlaues();
            }

        }
    }
}

