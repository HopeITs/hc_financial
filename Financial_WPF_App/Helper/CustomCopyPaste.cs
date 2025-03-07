using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Google.Protobuf.Collections;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;


namespace Financial_WPF_App.Helper
{
    public class CustomCopyPaste : GridCutCopyPaste
    {
        public event EventHandler PasteCompleted;
        private DataTable _all_Original_AvgRate_dt, _all_AvgRate_dt, _all_b_dt, _all_t_dt, _all_ExAcc_dt, _all_Accounts_dt, _all_Lines_dt, _all_budgets_dt, _all_cashAccounts_dt, _all_Donors_dt, _all_Employee_dt, _all_Tills_dt;
        public int TotalPastedRows { get; private set; } = 0;
        public int TotalErrorRows { get; private set; } = 0;

        string _Account_Type, _Till_Type;


        DataTable _Original_AvgRate_dt = new DataTable();
        DataTable _AvgRate_dt = new DataTable();
        DataTable _b_dt = new DataTable();
        DataTable _t_dt = new DataTable();
        DataTable _ExAcc_dt = new DataTable();
        DataTable _Accounts_dt = new DataTable();
        DataTable _Lines_dt = new DataTable();
        DataTable _budgets_dt = new DataTable();
        DataTable _cashAccounts_dt = new DataTable();
        DataTable _Donors_dt = new DataTable();
        DataTable _Employee_dt = new DataTable();



        string _inserted_value;
        int _Subproject_ID = -1;
        int _Till_ID = -1;
        int _Account_ID = -1;
        string _Form_Name = "Add_MultiActions_Window";
        double _Amount = 0;
        string _ActionDate = "";
        string _Note = "";
        string _E_Note = "";
        string _ObserveTill_Type = "";
        int _ObserveTill_ID = -1;
        int _ExchangeAcc_ID = -1;
        int _Donor_ID = -1;
        int _MicroProject_ID = -1;
        int _BudgetLine_ID = -1;
        int _CashAccount_ID = -1;
        int _ObserveCashAccount_ID = -1;
        int _Exchange_Cash_ID = -1;
        int _IsReceipt = -1;
        int _Employee_Salary_ID = -1;
        int _Rate_ID = -1;
        string _ReceiptNote = "";
        double _rate = 0;

        int _IncomeType_ID = -1;
        bool _WithExAcc = false;
        bool _WithExchangeRate = false;

        bool _WithObserveAcion_ID = false;

        bool _ExchangeToForeign_privileges;

        int k = -1;

        DataTable dt;

        List<string> _ShownColumns = new List<string>();

        public CustomCopyPaste(SfDataGrid dataGrid, string Account_Type, string TillType, string inserted_value, List<string> ShownColumns, bool ExchangeToForeign_privileges,
            DataTable Original_AvgRate_dt, DataTable AvgRate_dt, DataTable b_dt, DataTable t_dt, DataTable ExAcc_dt, DataTable Accounts_dt,
            DataTable Lines_dt, DataTable budgets_dt, DataTable all_cashAccounts_dt, DataTable Donors_dt, DataTable Employee_dt, DataTable AllTills_dt)
            : base(dataGrid)
        {
            _Till_Type = TillType;
            //_Till_ID = Till_ID;
            //_Account_ID = Account_ID;
            _Account_Type = Account_Type;

            _inserted_value = inserted_value;
            _ShownColumns = ShownColumns;

            _all_Original_AvgRate_dt = Original_AvgRate_dt ?? new DataTable();
            _all_AvgRate_dt = AvgRate_dt ?? new DataTable();
            _all_b_dt = b_dt ?? new DataTable();
            _all_t_dt = t_dt ?? new DataTable();
            _all_ExAcc_dt = ExAcc_dt ?? new DataTable();
            _all_Accounts_dt = Accounts_dt ?? new DataTable();
            _all_Lines_dt = Lines_dt ?? new DataTable();
            _all_budgets_dt = budgets_dt ?? new DataTable();
            _all_cashAccounts_dt = all_cashAccounts_dt ?? new DataTable();
            _all_Donors_dt = Donors_dt ?? new DataTable();
            _all_Employee_dt = Employee_dt ?? new DataTable();
            _all_Tills_dt = AllTills_dt ?? new DataTable();


            _ExchangeToForeign_privileges = ExchangeToForeign_privileges;

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
            _Original_AvgRate_dt = new DataTable();
            _AvgRate_dt = new DataTable();
            _b_dt = new DataTable();
            _t_dt = new DataTable();
            _ExAcc_dt = new DataTable();
            _Accounts_dt = new DataTable();
            _Lines_dt = new DataTable();
            _budgets_dt = new DataTable();
            _cashAccounts_dt = new DataTable();
            _Donors_dt = new DataTable();
            _Employee_dt = new DataTable();

            _Subproject_ID = -1;
            _Till_ID = -1;
            _Account_ID = -1;
            _Amount = 0;
            _ActionDate = "";
            _Note = "";
            _E_Note = "";
            _ObserveTill_Type = "";
            _ObserveTill_ID = -1;
            _ExchangeAcc_ID = -1;
            _Donor_ID = -1;
            _MicroProject_ID = -1;
            _BudgetLine_ID = -1;
            _CashAccount_ID = -1;
            _ObserveCashAccount_ID = -1;
            _Exchange_Cash_ID = -1;
            _IsReceipt = -1;
            _Employee_Salary_ID = -1;
            _Rate_ID = -1;
            _ReceiptNote = "";
            _rate = 0;

            _WithExAcc = false;
            _WithExchangeRate = false;
            _WithObserveAcion_ID = false;
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

                if (exists)
                {
                    System.Data.DataRow[] rows = null;
                    if (_all_Original_AvgRate_dt.Rows.Count > 0)
                    {
                        rows = _all_Original_AvgRate_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _Original_AvgRate_dt = rows.CopyToDataTable();
                            }
                        }
                    }

                    if (_all_AvgRate_dt.Rows.Count > 0)
                    {
                        rows = _all_AvgRate_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _AvgRate_dt = rows.CopyToDataTable();
                            }
                        }
                    }

                    if (_all_b_dt.Rows.Count > 0)
                    {
                        rows = _all_b_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _b_dt = rows.CopyToDataTable();
                            }
                        }
                    }

                    if (_all_t_dt.Rows.Count > 0)
                    {
                        rows = _all_t_dt.Select("Account_ID = '" + _Account_ID + "' AND ID <> " + _Till_ID + "");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _t_dt = rows.CopyToDataTable();
                            }
                        }
                    }


                    _ExAcc_dt = _all_ExAcc_dt.Copy();

                    if (_all_Accounts_dt.Rows.Count > 0)
                    {
                        rows = _all_Accounts_dt.Select("account_id <> '" + _Account_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _Accounts_dt = rows.CopyToDataTable();
                            }
                        }
                    }

                    if (_all_Lines_dt.Rows.Count > 0)
                    {
                        rows = _all_Lines_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _Lines_dt = rows.CopyToDataTable();
                            }
                        }
                    }

                    if (_all_budgets_dt.Rows.Count > 0)
                    {
                        rows = _all_budgets_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _budgets_dt = rows.CopyToDataTable();
                            }
                        }
                    }


                    if (_all_cashAccounts_dt.Rows.Count > 0)
                    {
                        if (_Account_Type == "Project")
                        {
                            rows = _all_cashAccounts_dt.Select("SubProject_ID like '" + _Subproject_ID + ",%' or SubProject_ID like '%," + _Subproject_ID + ",%' or SubProject_ID like '%," + _Subproject_ID + "' or SubProject_ID like '" + _Subproject_ID + "'");
                        }
                        else if (_Account_Type == "Exchange")
                        {
                            rows = _all_cashAccounts_dt.Select("ExchangeAccount_ID like '" + _Account_ID + ",%' or ExchangeAccount_ID like '%," + _Account_ID + ",%' or ExchangeAccount_ID like '%," + _Account_ID + "' or ExchangeAccount_ID like '" + _Account_ID + "'");
                        }
                        else if (_Account_Type == "Personal")
                        {
                            rows = _all_cashAccounts_dt.Select("PersonalAccount_ID like '" + _Account_ID + ",%' or PersonalAccount_ID like '%," + _Account_ID + ",%' or PersonalAccount_ID like '%," + _Account_ID + "' or PersonalAccount_ID like '" + _Account_ID + "'");
                        }
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _cashAccounts_dt = rows.CopyToDataTable();
                            }
                        }
                    }


                    if (_all_Donors_dt.Rows.Count > 0)
                    {
                        rows = _all_Donors_dt.Select("account_id = '" + _Account_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _Donors_dt = rows.CopyToDataTable();
                            }
                        }
                    }

                    if (_all_Employee_dt.Rows.Count > 0)
                    {
                        rows = _all_Employee_dt.Select("subproject_id = '" + _Subproject_ID + "'");
                        if (rows != null)
                        {
                            if (rows.Length != 0)
                            {
                                _Employee_dt = rows.CopyToDataTable();
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
            else if (e.Column.ColumnName == "Amount")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _Amount);

                if (!isNumber)
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Operation date")
            {
                DateTime Adate;
                bool isDate = DateTime.TryParseExact(
                         e.ProposedValue.ToString(),
                         "dd/MM/yyyy",
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.None,
                         out Adate);
                if (isDate)
                {
                    _ActionDate = Adate.ToString("yyyy/MM/dd");
                }
                else
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts Dates.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Description (AR)")
            {
                _Note = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "Description (Eng)")
            {
                _E_Note = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "Micro Project number")
            {
                bool isNumber = Int32.TryParse(e.ProposedValue.ToString(), out _MicroProject_ID);
                bool exists = false;
                if (isNumber)
                {
                    if (_b_dt.Rows.Count > 0)
                        exists = _b_dt.AsEnumerable().Any(row => row.Field<int>("MicroProject_ID") == Convert.ToInt32(e.ProposedValue.ToString()));
                }
                if (!isNumber || !exists)
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This project number does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Corresponding till")
            {
                bool exists = false;


                if (_ExchangeToForeign_privileges)
                {
                    if (_t_dt.Rows.Count > 0)
                        _ObserveTill_ID = _t_dt.AsEnumerable()
                         .Where(row => row.Field<string>("Type")?.ToLower() == e.ProposedValue.ToString().ToLower())
                         .Select(row => row.Field<int>("ID"))
                         .DefaultIfEmpty(-1)
                         .FirstOrDefault();
                }
                else
                {
                    if (_t_dt.Rows.Count > 0)
                        _ObserveTill_ID = _t_dt.AsEnumerable()
                          .Where(row => row.Field<string>("Type")?.ToLower().Equals(e.ProposedValue.ToString().ToLower()) == true &&
                            row.Field<string>("Type")?.ToLower().Equals("syr") == true)
                          .Select(row => row.Field<int>("ID"))
                          .DefaultIfEmpty(-1)
                          .FirstOrDefault();
                }


                if (_ObserveTill_ID != -1)
                    exists = true;

                if (exists)
                {
                    _ObserveTill_Type = e.ProposedValue.ToString();
                }
                else
                {
                    Login.MultiAction_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This till does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }

                if (_inserted_value == "Ex" & _Account_Type == "Project")
                {
                    bool Avgexists = false;
                    DateTime ActionDate;
                    bool isDate = DateTime.TryParseExact(
                             e.Row["Operation date"].ToString(),
                             "dd/MM/yyyy",
                             CultureInfo.InvariantCulture,
                             DateTimeStyles.None,
                             out ActionDate);

                    if (isDate)
                    {
                        if (_AvgRate_dt.Rows.Count > 0)
                        {
                            System.Data.DataRow[] rows = _AvgRate_dt.Select("BeginDate <= '" + ActionDate + "' and EndDate >= '" + ActionDate + "' and totp = '" + e.ProposedValue.ToString().ToLower() + "'");

                            // Create a HashSet with the values from the 'Name' column
                            var names = new HashSet<string>(rows?.Select(row => row.Field<string>("Name")) ?? Enumerable.Empty<string>());

                            if (names.Count > 0)
                                Avgexists = true;
                        }
                    }

                    if (!Avgexists)
                    {
                        Login.MultiAction_AnyError = true;
                        e.Row.RowError = "Please ensure that there is at least one AvgRate that matches the conversion currency and extends for a period that includes the transaction date.";
                    }

                }

            }
            else if (e.Column.ColumnName == "Corresponding account")
            {
                bool exists = false;

                if (_Accounts_dt.Rows.Count > 0)
                {
                    if (_Till_Type.ToLower() == "syr")
                    {
                        _ObserveTill_ID = _Accounts_dt.AsEnumerable()
                                .Where(row => row.Field<string>("Type")?.Equals(e.ProposedValue.ToString()) == true)
                                .Select(row => row.Field<int>("ID"))
                                .DefaultIfEmpty(-1)
                                .FirstOrDefault();
                    }
                    else
                    {
                        string donorName = e.Row["Donor"].ToString().Replace("'", "''");
                        System.Data.DataRow[] donorrow = _Donors_dt.Select("Name='" + donorName + "'");

                        int DonorID = Convert.ToInt32(donorrow[0]["ID"].ToString());

                        _ObserveTill_ID = _Accounts_dt.AsEnumerable()
                                .Where(row => (row.Field<string>("Donor_ID")?.StartsWith(DonorID + ",") == true ||
                                row.Field<string>("Donor_ID")?.Contains("," + DonorID + ",") == true ||
                                row.Field<string>("Donor_ID")?.EndsWith("," + DonorID) == true ||
                                row.Field<string>("Donor_ID")?.Equals(DonorID.ToString()) == true) &&
                                row.Field<string>("Type") == e.ProposedValue.ToString())
                                .Select(row => row.Field<int>("ID"))
                                .DefaultIfEmpty(-1)
                                .FirstOrDefault();
                    }
                }

                if (_ObserveTill_ID != -1)
                    exists = true;

                if (exists)
                {
                    _ObserveTill_Type = _Till_Type;
                }
                else
                {
                    Login.MultiAction_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This account does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Cash till")
            {
                bool exists = false;
                // bool exists = _cashAccounts_dt.AsEnumerable().Any(row => row.Field<string>("Name") == e.ProposedValue.ToString());

                if (_cashAccounts_dt.Rows.Count > 0)
                    _CashAccount_ID = _cashAccounts_dt.AsEnumerable()
                         .Where(row => row.Field<string>("Name")?.Equals(e.ProposedValue.ToString()) == true)
                         .Select(row => row.Field<int>("ID"))
                         .DefaultIfEmpty(-1)
                         .FirstOrDefault();


                if (_CashAccount_ID != -1)
                    exists = true;

                if (exists)
                {

                }
                else
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cash account does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Corresponding cash till")
            {
                bool exists = false;

                if (_inserted_value == "M")
                {
                    if (_Accounts_dt.Rows.Count > 0)
                    {
                        System.Data.DataRow[] accrow = _Accounts_dt.Select("Type='" + e.Row["Corresponding account"].ToString() + "'");

                        if (accrow.Length > 0)
                        {
                            int SubID = Convert.ToInt32(accrow[0]["subproject_ID"].ToString());

                            if (_all_cashAccounts_dt.Rows.Count > 0)
                                _Exchange_Cash_ID = _all_cashAccounts_dt.AsEnumerable()
                                 .Where(row => (row.Field<string>("SubProject_ID")?.StartsWith(SubID + ",") == true ||
                                 row.Field<string>("SubProject_ID")?.Contains("," + SubID + ",") == true ||
                                 row.Field<string>("SubProject_ID")?.EndsWith("," + SubID) == true ||
                                 row.Field<string>("SubProject_ID")?.Equals(SubID.ToString()) == true) &&
                                 row.Field<string>("Name") == e.ProposedValue.ToString())
                                 .Select(row => row.Field<int>("ID"))
                                 .DefaultIfEmpty(-1)
                                 .FirstOrDefault();


                            if (_Exchange_Cash_ID != -1)
                            {
                                exists = true;
                            }
                        }
                    }

                }
                else
                {
                    if (e.Row["Corresponding till"].ToString().ToLower() != "syr")
                    {
                        exists = true;

                        e.ProposedValue = "";
                    }
                    else
                    {
                        if (_cashAccounts_dt.Rows.Count > 0)
                            _ObserveCashAccount_ID = _cashAccounts_dt.AsEnumerable()
                             .Where(row => row.Field<string>("Name")?.Equals(e.ProposedValue.ToString()) == true)
                             .Select(row => row.Field<int>("ID"))
                             .DefaultIfEmpty(-1)
                             .FirstOrDefault();


                        if (_ObserveCashAccount_ID != -1)
                        {
                            exists = true;
                        }
                    }
                }

                if (exists)
                {

                }
                else
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cash account does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }


            }
            else if (e.Column.ColumnName == "Exchange account")
            {
                bool exists = false;
                if (e.ProposedValue.ToString().IsNullOrWhiteSpace())
                {
                    exists = true;
                }
                else
                {
                    if (_ExAcc_dt.Rows.Count > 0)
                        _ExchangeAcc_ID = _ExAcc_dt.AsEnumerable()
                             .Where(row => row.Field<string>("ToTill")?.ToLower().Equals(e.Row["Corresponding till"].ToString().ToLower()) == true
                             && row.Field<string>("Name")?.Equals(e.ProposedValue.ToString()) == true)
                             .Select(row => row.Field<int>("ID"))
                             .DefaultIfEmpty(-1)
                             .FirstOrDefault();


                    if (_ExchangeAcc_ID != -1)
                        exists = true;
                }

                if (exists)
                {

                }
                else
                {
                    Login.MultiAction_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This exchange account does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Exchange cash till")
            {
                bool exists = false;
                if (e.Row["Exchange account"].ToString() == "")
                {
                    exists = true;
                    e.ProposedValue = "";
                }
                else
                {
                    if (_Till_Type.ToLower() != "syr" && e.Row["Corresponding till"].ToString().ToLower() != "syr")
                    {
                        exists = true;
                        e.ProposedValue = "";
                    }
                    else
                    {
                        if (_ExAcc_dt.Rows.Count > 0)
                        {
                            System.Data.DataRow[] exaccrow = _ExAcc_dt.Select("Name='" + e.Row["Exchange account"].ToString() + "'");

                            if (exaccrow.Length > 0)
                            {
                                int ExAcc_ID = Convert.ToInt32(exaccrow[0]["id"].ToString());

                                if (_all_cashAccounts_dt.Rows.Count > 0)
                                    _Exchange_Cash_ID = _all_cashAccounts_dt.AsEnumerable()
                                     .Where(row => (row.Field<string>("ExchangeAccount_ID")?.StartsWith(ExAcc_ID + ",") == true ||
                                     row.Field<string>("ExchangeAccount_ID")?.Contains("," + ExAcc_ID + ",") == true ||
                                     row.Field<string>("ExchangeAccount_ID")?.EndsWith("," + ExAcc_ID) == true ||
                                     row.Field<string>("ExchangeAccount_ID")?.Equals(ExAcc_ID.ToString()) == true) &&
                                     row.Field<string>("Name") == e.ProposedValue.ToString())
                                     .Select(row => row.Field<int>("ID"))
                                     .DefaultIfEmpty(-1)
                                     .FirstOrDefault();

                                if (_Exchange_Cash_ID != -1)
                                    exists = true;
                            }
                        }

                    }

                }

                if (!exists)
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cash account does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Origin Avg Rate")
            {
                bool exists = false;

                if (e.ProposedValue.ToString().IsNullOrWhiteSpace())
                {
                    if (_Account_Type != "Project" || _Till_Type.ToLower() != "syr")
                        exists = true;
                }
                else
                {
                    if (_ShownColumns.Contains("Budget"))
                    {
                        if (_Lines_dt.Rows.Count > 0)
                        {
                            //string d = e.Row["Budget"].ToString();
                            System.Data.DataRow[] budgetrow = _Lines_dt.Select("budget_Name = '" + e.Row["Budget"].ToString() + "'");

                            int BudgetType_ID = -1;
                            if (budgetrow.Length > 0)
                                BudgetType_ID = Convert.ToInt32(budgetrow[0]["BudgetType_ID"].ToString());

                            if (_Original_AvgRate_dt.Rows.Count > 0 && BudgetType_ID != -1)
                                _Rate_ID = _Original_AvgRate_dt.AsEnumerable()
                                 .Where(row =>
                                 {
                                     var fromtp_id = row.Field<object>("fromtp_id");
                                     return (fromtp_id is int id && id == BudgetType_ID)
                                         && row.Field<string>("Name")?.Equals(e.ProposedValue.ToString()) == true;
                                 })
                                 .Select(row => row.Field<int>("ID"))
                                 .DefaultIfEmpty(-1)
                                 .FirstOrDefault();
                        }

                    }
                    else
                    {
                        if (_Original_AvgRate_dt.Rows.Count > 0)
                            _Rate_ID = _Original_AvgRate_dt.AsEnumerable()
                              .Where(row => row.Field<string>("Name")?.Equals(e.ProposedValue.ToString()) == true)
                              .Select(row => row.Field<int>("ID"))
                              .DefaultIfEmpty(-1)
                              .FirstOrDefault();
                    }

                    if (_Rate_ID != -1)
                        exists = true;
                }


                if (!exists)
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This Avg Rate does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Rate")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _rate);

                if (!isNumber)
                {
                    Login.MultiAction_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Document type")
            {
                bool exists = false;
                if (e.ProposedValue.ToString().IsNullOrWhiteSpace())
                {
                    if (_Account_Type == "Personal")
                    {
                        exists = true;
                    }
                }
                else
                {
                    if (e.ProposedValue.ToString().ToLower() == "receipt")
                    {
                        exists = true;
                        _IsReceipt = 0;
                    }
                    if (e.ProposedValue.ToString().ToLower() == "invoice")
                    {
                        exists = true;
                        _IsReceipt = 1;
                    }
                    if (e.ProposedValue.ToString().ToLower() == "payslip")
                    {
                        exists = true;
                        _IsReceipt = 2;
                    }
                }

                if (!exists)
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This type does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Volunteer")
            {
                bool exists = false;

                DateTime ActionMonth;
                bool isDate = DateTime.TryParseExact(
                         e.Row["Operation date"].ToString(),
                         "dd/MM/yyyy",
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.None,
                         out ActionMonth);

                if (e.Row["Document type"].ToString() == "payslip")
                {
                    if (isDate)
                    {
                        if (_Employee_dt.Rows.Count > 0)
                            _Employee_Salary_ID = _Employee_dt.AsEnumerable()
                                 .Where(row => ((DateTime)row["Month"]).ToString("yyyy/MM") == ActionMonth.ToString("yyyy/MM")
                                 && row.Field<string>("Employee_Name")?.ToLower().Equals(e.ProposedValue.ToString().ToLower()) == true)
                                 .Select(row => row.Field<int>("Employee_Salary_ID"))
                                 .DefaultIfEmpty(-1)
                                 .FirstOrDefault();
                    }

                    if (_Employee_Salary_ID != -1)
                        exists = true;
                }
                else
                {
                    exists = true;

                    e.ProposedValue = "";
                }

                if (exists)
                {

                }
                else
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This employee does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }

            }
            else if (e.Column.ColumnName == "Document number")

            {
                bool exists = false;
                if (e.ProposedValue.ToString().IsNullOrWhiteSpace())
                {
                    if (_Account_Type == "Personal")
                    {
                        exists = true;
                    }
                }
                else
                {
                    exists = true;
                    _ReceiptNote = e.ProposedValue.ToString();
                }

                if (!exists)
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cell can not be empty.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Donor")
            {
                bool exists = false;
                if (_Donors_dt.Rows.Count > 0)
                    _Donor_ID = _Donors_dt.AsEnumerable()
                         .Where(row => row.Field<string>("Name")?.Equals(e.ProposedValue.ToString()) == true)
                         .Select(row => row.Field<int>("ID"))
                         .DefaultIfEmpty(-1)
                         .FirstOrDefault();

                if (_Donor_ID != -1)
                    exists = true;

                if (exists)
                {

                }
                if (!exists)
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This donor does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Budget")
            {
                bool exists = false;
                if (_budgets_dt.Rows.Count > 0)
                    exists = _budgets_dt.AsEnumerable().Any(row => row.Field<string>("budget_Name") == e.ProposedValue.ToString());

                if (!exists)
                {
                    Login.MultiAction_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This budget does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Line")
            {
                bool exists = false;
                int Selected_budgetID = -1;

                if (_budgets_dt.Rows.Count > 0)
                {
                    System.Data.DataRow[] budgetrow = _budgets_dt.Select("budget_Name='" + e.Row["Budget"].ToString() + "'");

                    if (budgetrow.Length > 0)
                    {
                        Selected_budgetID = Convert.ToInt32(budgetrow[0]["budget_ID"].ToString());

                        if (_Lines_dt.Rows.Count > 0)
                            _BudgetLine_ID = _Lines_dt.AsEnumerable()
                             .Where(row =>
                             {
                                 var budget_ID = row.Field<object>("budget_ID");
                                 return row.Field<string>("line_Name")?.Equals(e.ProposedValue.ToString()) == true
                                     && (budget_ID is int id && id == Selected_budgetID);
                             })
                             .Select(row => row.Field<int>("budget_line_ID"))
                             .DefaultIfEmpty(-1)
                             .FirstOrDefault();



                        if (_BudgetLine_ID != -1)
                            exists = true;
                    }
                }


                if (exists)
                {

                }
                else
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This line does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }




            if (e.Column.ColumnName == Last_Column_Name)
            {
                TotalPastedRows++;
                if (e.Row.RowError == "There are errors in this row.")
                    TotalErrorRows++;

                if (!Login.MultiAction_AnyError)
                {
                    if (_inserted_value == "Ex")
                    {
                        if (_ObserveTill_ID != -1)
                        {
                            if (_Account_Type == "Project" && !e.Row["Exchange account"].ToString().IsNullOrWhiteSpace())
                                _WithExAcc = true;
                            else
                                _WithExAcc = false;

                            if (_Account_Type == "Exchange")
                                _WithExchangeRate = true;
                            else
                                _WithExchangeRate = false;
                        }
                    }
                    else
                    {
                        _WithExAcc = false;
                        _WithExchangeRate = false;
                    }

                    if (_inserted_value == "Ex" || _inserted_value == "M")
                    {
                        _WithObserveAcion_ID = true;
                    }
                    else
                    {
                        _WithObserveAcion_ID = false;
                    }
                    build_insert_query();
                }
                else
                {
                    Login.MultiAction_List.Clear();
                    Login.MultiAction_sc.Parameters.Clear();
                }

                clear_vlaues();
            }
        }


        public void build_insert_query()
        {
            k++;
            string temp_query = "";
            string log_query = "";

            Login.MultiAction_sc.Parameters.AddWithValue("@Till_ID" + k, _Till_ID);
            Login.MultiAction_sc.Parameters.AddWithValue("@Till_Type" + k, _Till_Type);
            if (_ObserveTill_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ObserveTill_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ObserveTill_ID" + k, _ObserveTill_ID);
            }
            Login.MultiAction_sc.Parameters.AddWithValue("@ObserveTill_Type" + k, _ObserveTill_Type);

            if (_Donor_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Donor_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Donor_ID" + k, _Donor_ID);
            }

            if (_ExchangeAcc_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ExchangeAcc_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ExchangeAcc_ID" + k, _ExchangeAcc_ID);
            }

            if (_inserted_value == "M")
                Login.MultiAction_sc.Parameters.AddWithValue("@AType" + k, "E");
            else
                Login.MultiAction_sc.Parameters.AddWithValue("@AType" + k, _inserted_value);

            Login.MultiAction_sc.Parameters.AddWithValue("@Amount" + k, _Amount);
            Login.MultiAction_sc.Parameters.AddWithValue("@rate" + k, _rate);

            if (_inserted_value == "M")
                Login.MultiAction_sc.Parameters.AddWithValue("@ConvertedAmount" + k, _Amount);
            else
                Login.MultiAction_sc.Parameters.AddWithValue("@ConvertedAmount" + k, _Amount * _rate);


            Login.MultiAction_sc.Parameters.AddWithValue("@Note" + k, _Note);
            Login.MultiAction_sc.Parameters.AddWithValue("@E_Note" + k, _E_Note);

            Login.MultiAction_sc.Parameters.AddWithValue("@IsReceipt" + k, _IsReceipt);
            Login.MultiAction_sc.Parameters.AddWithValue("@ReceiptNote" + k, _ReceiptNote);
            if (_Employee_Salary_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Employee_Salary_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Employee_Salary_ID" + k, _Employee_Salary_ID);
            }

            if (_Rate_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Rate_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Rate_ID" + k, _Rate_ID);
            }
            if (_ActionDate == "")
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ActionDate" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ActionDate" + k, _ActionDate);
            }
            if (_MicroProject_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@MicroProject_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@MicroProject_ID" + k, _MicroProject_ID);
            }
            if (_BudgetLine_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@BudgetLine_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@BudgetLine_ID" + k, _BudgetLine_ID);
            }
            if (_IncomeType_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@IncomeType_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@IncomeType_ID" + k, _IncomeType_ID);
            }
            if (_CashAccount_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@CashAccount_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@CashAccount_ID" + k, _CashAccount_ID);
            }
            if (_ObserveCashAccount_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ObserveCashAccount_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ObserveCashAccount_ID" + k, _ObserveCashAccount_ID);
            }
            if (_Exchange_Cash_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Exchange_Cash_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Exchange_Cash_ID" + k, _Exchange_Cash_ID);
            }
            if (_Account_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Account_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Account_ID" + k, _Account_ID);
            }

            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
            Login.MultiAction_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

            Login.MultiAction_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);
            Login.MultiAction_sc.Parameters.AddWithValue("@Form_Name" + k, _Form_Name);


            if (k == 0)
            {
                temp_query += @"SET @Action_ID = (
                                SELECT MAX(max_id) + 1 AS next_id
                                FROM (
                                    SELECT MAX(ID) AS max_id FROM action
                                    UNION ALL
                                    SELECT MAX(ID) FROM action_not_approved
                                    UNION ALL
                                    SELECT MAX(ID) FROM action_deleted
                                ) AS combined_max_ids);";
            }

            temp_query += "SET @Action_ID" + k + " = @Action_ID;";

            temp_query += @"INSERT INTO `action_not_approved`(`id`,`Till_ID`,`ObserveTill_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            ,  `Note`, `E_Note`, `IsReceipt`, `ReceiptNote`, `Employee_Salary_ID`, `Rate_ID`
                            , `MicroProject_ID`, `BudgetLine_ID`, `IncomeType_ID`, `CashAccount_ID`, `ObserveCashAccount_ID`, `Exchange_Cash_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES (@Action_ID" + k + " ,@Till_ID" + k + " ,@ObserveTill_ID" + k + " ,(select id from actiontype where shortname = @AType" + k + "), @Amount" + k + ", @rate" + k + ", @ActionDate" + k + ",@Note" + k + ",@E_Note" + k + ", @IsReceipt" + k + ", @ReceiptNote" + k + ",  @Employee_Salary_ID" + k + ",@Rate_ID" + k + ", "
                            + "@MicroProject_ID" + k + ", @BudgetLine_ID" + k + ", @IncomeType_ID" + k + ", @CashAccount_ID" + k + ", @ObserveCashAccount_ID" + k + ", @Exchange_Cash_ID" + k + ", @Donor_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ", '1');";

            temp_query += "SET @Action_ID = @Action_ID" + k + " +1;";


            if (_WithObserveAcion_ID)
            {
                temp_query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            ,  `Note`,`E_Note`, `IsReceipt`, `ReceiptNote`, `Employee_Salary_ID`, `CashAccount_ID`,`ObserveCashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES(@ObserveTill_ID" + k + ", @Till_ID" + k + ", @Action_ID" + k + ", (select id from actiontype where shortname = 'I'), @ConvertedAmount" + k + ",@rate" + k + ", @ActionDate" + k + ", @Note" + k + ",@E_Note" + k + ", @IsReceipt" + k + ", @ReceiptNote" + k + ", @Employee_Salary_ID" + k + ",@ObserveCashAccount_ID" + k + ", @CashAccount_ID" + k + ", @Donor_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ", '0'); ";

                temp_query += " SET @ObserveAcion_ID" + k + " = Last_Insert_ID();";

                temp_query += " UPDATE action_not_approved SET ObserveAcion_ID = @ObserveAcion_ID" + k + " WHERE ID = @Action_ID" + k + ";";

                temp_query += "SET @Action_ID = @Action_ID" + k + " +2;";


                if (_WithExAcc)
                {
                    temp_query += " Set @From_id" + k + " = (select till.id from till left join account on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where account.id = @ExchangeAcc_ID" + k + " and tilltypes.name = @Till_Type" + k + ");";
                    temp_query += " Set @To_id" + k + " = (select till.id from till left join account on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where account.id = @ExchangeAcc_ID" + k + " and tilltypes.name = @ObserveTill_Type" + k + ");";

                    temp_query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            , `Note`,`E_Note`, `IsReceipt`, `ReceiptNote`, `Employee_Salary_ID`, `CashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES(@From_id" + k + ", @Till_ID" + k + ", @Action_ID" + k + ", (select id from actiontype where shortname = 'I'), @Amount" + k + " ,@rate" + k + ", @ActionDate" + k + ", @Note" + k + ",@E_Note" + k + ", @IsReceipt" + k + ", @ReceiptNote" + k + ", @Employee_Salary_ID" + k + ", (case when LOWER(@Till_Type" + k + ") = 'syr' then @Exchange_Cash_ID" + k + " else null end), @Donor_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ", '0'); ";

                    temp_query += " SET @FromExTill_Acc_ID" + k + " = Last_Insert_ID();";

                    temp_query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            , `Note`,`E_Note`, `IsReceipt`, `ReceiptNote`,`Employee_Salary_ID`, `CashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES(@To_id" + k + ", @Till_ID" + k + ", @Action_ID" + k + ", (select id from actiontype where shortname = 'E'), @ConvertedAmount" + k + " ,@rate" + k + ", @ActionDate" + k + ", @Note" + k + ", @E_Note" + k + ", @IsReceipt" + k + ", @ReceiptNote" + k + ", @Employee_Salary_ID" + k + ", (case when LOWER(@ObserveTill_Type" + k + ") = 'syr' then @Exchange_Cash_ID" + k + " else null end), @Donor_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ", '0'); ";

                    temp_query += " SET @ToExTill_Acc_ID" + k + " = Last_Insert_ID();";

                    temp_query += "SET @Action_ID = @Action_ID" + k + " +4;";
                }
            }

            temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @Action_ID" + k + ", CONCAT('action Type = ', @AType" + k + "), CONCAT('till ID = ',@Till_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

            if (_WithObserveAcion_ID)
            {
                temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @ObserveAcion_ID" + k + ", 'action Type = I', CONCAT('till ID = ', @ObserveTill_ID" + k + " ,' - Auto Created With action ID = ',@Action_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                if (_WithExAcc)
                {
                    temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @FromExTill_Acc_ID" + k + ", 'action Type = I', CONCAT('till ID = ', @From_id" + k + " ,' - Auto Created With action ID = ',@Action_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @ToExTill_Acc_ID" + k + ", 'action Type = E', CONCAT('till ID = ', @To_id" + k + " ,' - Auto Created With action ID = ',@Action_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }
            }


            Login.MultiAction_List.Add(temp_query);
            temp_query = "";

        }


        protected virtual void OnPasteCompleted()
        {
            PasteCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
