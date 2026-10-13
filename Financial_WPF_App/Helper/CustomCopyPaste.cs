using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using MySql.Data.MySqlClient;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using System;
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
        private const int UsdCurrencyTilltypeId = 2;

        public event EventHandler PasteCompleted;

        /// <summary>
        /// Is this till in a local currency? Was a name match against "syr", which made a
        /// newly added local currency behave like a foreign one when pasting rows.
        /// </summary>
        private static bool IsLocalTill(string tillType)
        {
            return CurrencyService.IsLocalCurrency(tillType, false);
        }

        /// <summary>
        /// Currencies a user may type in the Currency column: any local one, or USD.
        /// The accepted set used to be the literal pair SYR/USD, so a newly added local
        /// currency was rejected as invalid on paste.
        /// </summary>
        private static bool IsAcceptedPasteCurrency(string currency)
        {
            return IsLocalTill(currency)
                || string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The accepted names, for the error message, so it never goes stale.</summary>
        private static string AcceptedPasteCurrencies()
        {
            var names = CurrencyService.All()
                .Where(c => IsAcceptedPasteCurrency(c.Name))
                .Select(c => c.Name)
                .ToList();
            names.Add("USD");
            return string.Join(" or ", names.Distinct());
        }

        private DataTable _all_Original_AvgRate_dt, _all_AvgRate_dt, _all_b_dt, _all_t_dt, _all_ExAcc_dt, _all_Accounts_dt, _all_Lines_dt, _all_budgets_dt, _all_cashAccounts_dt, _all_Donors_dt, _all_Employee_dt, _all_Tills_dt, _currency_dt;
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
        DataTable currncy_dt = new DataTable();




        string _inserted_value;
        int _Subproject_ID = -1;
        int _Till_ID = -1;
        int _Account_ID = -1;
        int _currency_tilltype_id = -1;
        int _SelectedTillType_ID = -1;
        int _FromExchangeTill_ID = -1;
        int _ToExchangeTill_ID = -1;
        string _Form_Name = "Add_MultiActions_Window";
        string _ImportedAccountName = "";
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
        bool isUsdExpenseFromSyr;

        int k = -1;

        DataTable dt;

        List<string> _ShownColumns = new List<string>();

        public CustomCopyPaste(SfDataGrid dataGrid, string Account_Type, string TillType, string inserted_value, List<string> ShownColumns, bool ExchangeToForeign_privileges,
            DataTable Currency_dt, DataTable Original_AvgRate_dt, DataTable AvgRate_dt, DataTable b_dt, DataTable t_dt, DataTable ExAcc_dt, DataTable Accounts_dt, 
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
            _currency_dt = Currency_dt ?? new DataTable();


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
            _SelectedTillType_ID = -1;
            _FromExchangeTill_ID = -1;
            _ToExchangeTill_ID = -1;
            _ImportedAccountName = "";
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
            _currency_tilltype_id = -1;

            _WithExAcc = false;
            _WithExchangeRate = false;
            _WithObserveAcion_ID = false;
            isUsdExpenseFromSyr = false;
        }

        private static string NormalizeLookupName(string value)
        {
            return (value ?? "").Trim().ToLowerInvariant();
        }

        private int ResolveTillTypeIdFromLoadedData(string tillTypeName)
        {
            string normalizedTillType = NormalizeLookupName(tillTypeName);

            return _all_Tills_dt.AsEnumerable()
                .Where(row => NormalizeLookupName(row.Field<string>("Name")) == normalizedTillType)
                .Select(row => row.Field<int>("ID"))
                .DefaultIfEmpty(-1)
                .FirstOrDefault();
        }

        private int ResolveTillIdFromDatabase(int accountId, string tillTypeName, out int typeId)
        {
            typeId = -1;
            int tillId = -1;
            string normalizedTillType = NormalizeLookupName(tillTypeName);

            string query = @"SELECT till.ID AS Till_ID, till.Type_ID
                             FROM till
                             LEFT JOIN tilltypes ON tilltypes.ID = till.Type_ID
                             WHERE till.Account_ID = @Account_ID
                               AND LOWER(tilltypes.Name) = @TillTypeName
                             LIMIT 1;";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Parameters.AddWithValue("@Account_ID", accountId);
                sc.Parameters.AddWithValue("@TillTypeName", normalizedTillType);
                sc.Connection = Login.MyConn;

                using (MySqlDataReader reader = sc.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        tillId = Convert.ToInt32(reader["Till_ID"]);
                        typeId = Convert.ToInt32(reader["Type_ID"]);
                    }
                }

                Login.CloseAndClearConnection();
            }

            return tillId;
        }

        private void LogTillResolution(string stage)
        {
            System.Diagnostics.Debug.WriteLine("==================================================");
            System.Diagnostics.Debug.WriteLine("[Multi Action Import - Till Resolution]");
            System.Diagnostics.Debug.WriteLine("Stage: " + stage);
            System.Diagnostics.Debug.WriteLine("Imported Account Name: " + _ImportedAccountName);
            System.Diagnostics.Debug.WriteLine("Account_ID: " + _Account_ID);
            System.Diagnostics.Debug.WriteLine("Selected Till Type: " + _Till_Type);
            System.Diagnostics.Debug.WriteLine("Type_ID: " + _SelectedTillType_ID);
            System.Diagnostics.Debug.WriteLine("Resolved Till_ID: " + _Till_ID);
            System.Diagnostics.Debug.WriteLine("Lookup criteria: till.Account_ID = " + _Account_ID + " AND LOWER(tilltypes.Name) = '" + NormalizeLookupName(_Till_Type) + "'");
            if (_WithExAcc)
            {
                System.Diagnostics.Debug.WriteLine("Exchange Account_ID: " + _ExchangeAcc_ID);
                System.Diagnostics.Debug.WriteLine("Exchange From Till criteria: till.Account_ID = " + _ExchangeAcc_ID + " AND LOWER(tilltypes.Name) = '" + NormalizeLookupName(_Till_Type) + "'");
                System.Diagnostics.Debug.WriteLine("Exchange From Till_ID: " + _FromExchangeTill_ID);
                System.Diagnostics.Debug.WriteLine("Exchange To Till criteria: till.Account_ID = " + _ExchangeAcc_ID + " AND LOWER(tilltypes.Name) = '" + NormalizeLookupName(_ObserveTill_Type) + "'");
                System.Diagnostics.Debug.WriteLine("Exchange To Till_ID: " + _ToExchangeTill_ID);
            }
            System.Diagnostics.Debug.WriteLine("==================================================");
        }

        private void ValidateTillResolutionBeforeInsert()
        {
            _SelectedTillType_ID = ResolveTillTypeIdFromLoadedData(_Till_Type);

            if (_Till_ID <= 0 && _Account_ID > 0)
            {
                int dbTypeId;
                _Till_ID = ResolveTillIdFromDatabase(_Account_ID, _Till_Type, out dbTypeId);
                if (_SelectedTillType_ID == -1)
                    _SelectedTillType_ID = dbTypeId;
            }

            if (_WithExAcc)
            {
                int fromTypeId;
                int toTypeId;
                _FromExchangeTill_ID = ResolveTillIdFromDatabase(_ExchangeAcc_ID, _Till_Type, out fromTypeId);
                _ToExchangeTill_ID = ResolveTillIdFromDatabase(_ExchangeAcc_ID, _ObserveTill_Type, out toTypeId);
            }

            LogTillResolution("Before build_insert_query");

            if (_Till_ID <= 0)
                throw new InvalidOperationException("No till found for Account_ID = " + _Account_ID + " and Type_ID = " + _SelectedTillType_ID);

            if (_WithExAcc && _FromExchangeTill_ID <= 0)
                throw new InvalidOperationException("No exchange-account till found for Account_ID = " + _ExchangeAcc_ID + " and Till Type = '" + _Till_Type + "'");

            if (_WithExAcc && _ToExchangeTill_ID <= 0)
                throw new InvalidOperationException("No exchange-account till found for Account_ID = " + _ExchangeAcc_ID + " and Till Type = '" + _ObserveTill_Type + "'");
        }


        private void Column_Changing(object sender, DataColumnChangeEventArgs e)
        {
            string Last_Column_Name = e.Row.Table.Columns[e.Row.Table.Columns.Count - 1].ColumnName;

            if (_Account_Type == "Project" && e.Column.ColumnName == "Project")
            {
                bool exists = false;

                _Subproject_ID = _all_Tills_dt.AsEnumerable()
                                     .Where(row => row.Field<string>("Subproject_Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
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
                    e.Row.SetColumnError(e.Column.ColumnName, "This Project does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Account")
            {
                bool exists = false;
                _ImportedAccountName = e.ProposedValue?.ToString() ?? "";

                // 1. البحث عن Account_ID
                if (_Account_Type == "Project")
                {
                    _Account_ID = _all_Tills_dt.AsEnumerable()
                            .Where(row => row.Field<string>("Subproject_Name")?.ToLower().Trim().Equals(e.Row["Project"].ToString().ToLower().Trim()) == true
                            && row.Field<string>("Account_Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
                            .Select(row => row.Field<int>("Account_ID"))
                            .DefaultIfEmpty(-1)
                            .FirstOrDefault();
                }
                else
                {
                    _Account_ID = _all_Tills_dt.AsEnumerable()
                        .Where(row => row.Field<string>("Account_Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
                        .Select(row => row.Field<int>("Account_ID"))
                        .DefaultIfEmpty(-1)
                        .FirstOrDefault();
                }

                // 2. محاولة إيجاد الـ Till_ID فوراً من البيانات المحملة (لغرض الفلترة السريع)
                if (_Account_ID != -1)
                {
                    // نحتفظ بهذا السطر من الكود الجديد لتسجيل النوع
                    _SelectedTillType_ID = ResolveTillTypeIdFromLoadedData(_Till_Type);

                    // استعادة منطق البحث عن Till_ID كما في الكود القديم (لكن باستخدام دالة التطبيع الجديدة للأفضل)
                    _Till_ID = _all_Tills_dt.AsEnumerable()
                        .Where(row => row.Field<string>("Account_Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true
                        && NormalizeLookupName(row.Field<string>("Name")) == NormalizeLookupName(_Till_Type))
                        .Select(row => row.Field<int>("Till_ID"))
                        .DefaultIfEmpty(-1)
                        .FirstOrDefault();
                }

                LogTillResolution("Account column validation");

                // 3. (هذا هو الجزء المفقود) الفلترة الفورية للجداول المساعدة إذا تم العثور على Till
                if (_Till_ID != -1)
                {
                    exists = true;

                    System.Data.DataRow[] rows = null;

                    // فلترة جداول الأسعار
                    if (_all_Original_AvgRate_dt.Rows.Count > 0)
                    {
                        rows = _all_Original_AvgRate_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null && rows.Length != 0) _Original_AvgRate_dt = rows.CopyToDataTable();
                    }

                    if (_all_AvgRate_dt.Rows.Count > 0)
                    {
                        rows = _all_AvgRate_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null && rows.Length != 0) _AvgRate_dt = rows.CopyToDataTable();
                    }

                    // فلترة المشاريع الصغيرة
                    if (_all_b_dt.Rows.Count > 0)
                    {
                        rows = _all_b_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null && rows.Length != 0) _b_dt = rows.CopyToDataTable();
                    }

                    // فلترة الصناديق الأخرى (Tills)
                    if (_all_t_dt.Rows.Count > 0)
                    {
                        rows = _all_t_dt.Select("Account_ID = '" + _Account_ID + "' AND ID <> " + _Till_ID + "");
                        if (rows != null && rows.Length != 0) _t_dt = rows.CopyToDataTable();
                    }

                    _ExAcc_dt = _all_ExAcc_dt.Copy();

                    // فلترة الحسابات الأخرى
                    if (_all_Accounts_dt.Rows.Count > 0)
                    {
                        rows = _all_Accounts_dt.Select("account_id <> '" + _Account_ID + "'");
                        if (rows != null && rows.Length != 0) _Accounts_dt = rows.CopyToDataTable();
                    }

                    // فلترة البنود (Lines)
                    if (_all_Lines_dt.Rows.Count > 0)
                    {
                        rows = _all_Lines_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null && rows.Length != 0) _Lines_dt = rows.CopyToDataTable();
                    }

                    // فلترة الميزانيات (Budgets)
                    if (_all_budgets_dt.Rows.Count > 0)
                    {
                        rows = _all_budgets_dt.Select("Account_ID = '" + _Account_ID + "'");
                        if (rows != null && rows.Length != 0) _budgets_dt = rows.CopyToDataTable();
                    }

                    // فلترة حسابات الصندوق النقدية (Cash Accounts) بناءً على النوع
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
                        if (rows != null && rows.Length != 0) _cashAccounts_dt = rows.CopyToDataTable();
                    }

                    // فلترة المانحين (Donors)
                    if (_all_Donors_dt.Rows.Count > 0)
                    {
                        rows = _all_Donors_dt.Select("account_id = '" + _Account_ID + "'");
                        if (rows != null && rows.Length != 0) _Donors_dt = rows.CopyToDataTable();
                    }

                    // فلترة الموظفين (Employees)
                    if (_all_Employee_dt.Rows.Count > 0)
                    {
                        rows = _all_Employee_dt.Select("subproject_id = '" + _Subproject_ID + "'");
                        if (rows != null && rows.Length != 0) _Employee_dt = rows.CopyToDataTable();
                    }
                }

                // إذا لم يتم العثور على الـ Till
                if (!exists)
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This Account does not exist or does not contains a match till.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Currency")
            {
                string currency = (e.ProposedValue?.ToString() ?? string.Empty).Trim().ToUpperInvariant();
                if (!IsAcceptedPasteCurrency(currency))
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "Currency must be " + AcceptedPasteCurrencies() + ".");
                    e.Row.RowError = "There are errors in this row.";
                    _currency_tilltype_id = -1;
                    isUsdExpenseFromSyr = false;
                }
                // An income or expense on a local till is in that till's currency, or USD - never
                // the other local currency: old lira saved against a new-lira till would come off
                // its balance as it stood, a hundred times off. (An exchange is left alone: its
                // Currency column names the currency exchanged TO.)
                else if ((_inserted_value == "E" || _inserted_value == "I") &&
                         IsLocalTill(_Till_Type) && IsLocalTill(currency) &&
                         !string.Equals(currency, _Till_Type, StringComparison.OrdinalIgnoreCase))
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "A " + _Till_Type.ToUpperInvariant() + " till takes " + _Till_Type.ToUpperInvariant() + " or USD, not " + currency + ".");
                    e.Row.RowError = "There are errors in this row.";
                    _currency_tilltype_id = -1;
                    isUsdExpenseFromSyr = false;
                }
                // Match Add Action 2: Personal Accounts do not allow choosing a foreign expense currency
                // (currency ComboBox is disabled; USD-from-SYR Expense path must not run).
                else if (string.Equals(_Account_Type, "Personal", StringComparison.OrdinalIgnoreCase) &&
                         _inserted_value == "E" &&
                         !string.IsNullOrEmpty(_Till_Type) &&
                         IsLocalTill(_Till_Type) &&
                         currency == "USD")
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "Personal accounts cannot create a USD expense from a SYR till.");
                    e.Row.RowError = "There are errors in this row.";
                    _currency_tilltype_id = -1;
                    isUsdExpenseFromSyr = false;
                }
                else if (_currency_dt.Rows.Count > 0)
                {
                    _currency_tilltype_id = _currency_dt.AsEnumerable()
                    .Where(row => row.Field<string>("Name")?.ToLower().Trim()
                     == currency.ToLowerInvariant())
                    .Select(row => row.Field<int>("ID"))
                    .DefaultIfEmpty(-1)
                    .FirstOrDefault();

                    if (_currency_tilltype_id != -1)
                    {
                        if (IsLocalTill(_Till_Type) &&
                         _currency_tilltype_id == UsdCurrencyTilltypeId &&
                        _inserted_value == "E")
                        {
                            isUsdExpenseFromSyr = true;
                        }
                        else
                        {
                            isUsdExpenseFromSyr = false;
                        }
                    }
                    else
                    {
                        Login.MultiAction_AnyError = true;
                        e.Row.SetColumnError(e.Column.ColumnName, "Currency must be " + AcceptedPasteCurrencies() + ".");
                        e.Row.RowError = "There are errors in this row.";
                        isUsdExpenseFromSyr = false;
                    }
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
                bool isDate = false;
                object val = e.ProposedValue;
                string text = val?.ToString() ?? "";

                // 1) If it's already a DateTime, cast directly
                if (val is DateTime dt)
                {
                    Adate = dt;
                    isDate = true;
                }
                // 2) If it's a numeric OLE date (Excel’s internal serial), convert
                else if (double.TryParse(
                             text,
                             NumberStyles.Any,
                             CultureInfo.InvariantCulture,
                             out double oaSerial))
                {
                    Adate = DateTime.FromOADate(oaSerial);
                    isDate = true;
                }
                // 3) Otherwise let .NET try parsing with CurrentCulture OR InvariantCulture
                else if (
                    DateTime.TryParse(
                        text,
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                        out Adate)
                    ||
                    DateTime.TryParse(
                        text,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                        out Adate)
                )
                {
                    isDate = true;
                }

                if (isDate)
                {
                    _ActionDate = Adate.ToString("yyyy/MM/dd");
                }
                else
                {
                    Login.MultiAction_AnyError = true;
                    e.Row.SetColumnError(
                        e.Column.ColumnName,
                        "This column only accepts dates in any standard format.");
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
                         .Where(row => row.Field<string>("Type")?.ToLower().Trim() == e.ProposedValue.ToString().ToLower().Trim())
                         .Select(row => row.Field<int>("ID"))
                         .DefaultIfEmpty(-1)
                         .FirstOrDefault();
                }
                else
                {
                    if (_t_dt.Rows.Count > 0)
                        _ObserveTill_ID = _t_dt.AsEnumerable()
                          .Where(row => row.Field<string>("Type")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true &&
                            IsLocalTill(row.Field<string>("Type")))
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
                    bool isDate = false;
                    object val = e.Row["Operation date"].ToString();
                    string text = val?.ToString() ?? "";

                    // 1) If it's already a DateTime, cast directly
                    if (val is DateTime dt)
                    {
                        ActionDate = dt;
                        isDate = true;
                    }
                    // 2) If it's a numeric OLE date (Excel’s internal serial), convert
                    else if (double.TryParse(
                                 text,
                                 NumberStyles.Any,
                                 CultureInfo.InvariantCulture,
                                 out double oaSerial))
                    {
                        ActionDate = DateTime.FromOADate(oaSerial);
                        isDate = true;
                    }
                    // 3) Otherwise let .NET try parsing with CurrentCulture OR InvariantCulture
                    else if (
                        DateTime.TryParse(
                            text,
                            CultureInfo.CurrentCulture,
                            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                            out ActionDate)
                        ||
                        DateTime.TryParse(
                            text,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                            out ActionDate)
                    )
                    {
                        isDate = true;
                    }




                    if (isDate)
                    {
                        if (_AvgRate_dt.Rows.Count > 0)
                        {
                            System.Data.DataRow[] rows = _AvgRate_dt.Select("BeginDate <= '" + ActionDate + "' and EndDate >= '" + ActionDate + "' and totp = '" + e.ProposedValue.ToString().ToLower().Trim() + "'");

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
                    if (IsLocalTill(_Till_Type))
                    {
                        _ObserveTill_ID = _Accounts_dt.AsEnumerable()
                                .Where(row => row.Field<string>("Type")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
                                .Select(row => row.Field<int>("ID"))
                                .DefaultIfEmpty(-1)
                                .FirstOrDefault();
                    }
                    else
                    {
                        string donorName = e.Row["Donor"].ToString().Replace("'", "''").ToLower().Trim();
                        System.Data.DataRow[] donorrow = _Donors_dt.Select("Name='" + donorName + "'");

                        int DonorID = Convert.ToInt32(donorrow[0]["ID"].ToString());

                        _ObserveTill_ID = _Accounts_dt.AsEnumerable()
                                .Where(row => (row.Field<string>("Donor_ID")?.StartsWith(DonorID + ",") == true ||
                                row.Field<string>("Donor_ID")?.Contains("," + DonorID + ",") == true ||
                                row.Field<string>("Donor_ID")?.EndsWith("," + DonorID) == true ||
                                row.Field<string>("Donor_ID")?.Equals(DonorID.ToString()) == true) &&
                                row.Field<string>("Type").ToLower().Trim() == e.ProposedValue.ToString().ToLower().Trim())
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
                         .Where(row => row.Field<string>("Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
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

                        System.Data.DataRow[] accrow = _Accounts_dt.AsEnumerable()
                        .Where(r => r["Type"].ToString().ToLower().Trim() == e.Row["Corresponding account"].ToString().ToLower().Trim())
                        .ToArray();

                        if (accrow.Length > 0)
                        {
                            int SubID = Convert.ToInt32(accrow[0]["subproject_ID"].ToString());

                            if (_all_cashAccounts_dt.Rows.Count > 0)
                                _Exchange_Cash_ID = _all_cashAccounts_dt.AsEnumerable()
                                 .Where(row => (row.Field<string>("SubProject_ID")?.StartsWith(SubID + ",") == true ||
                                 row.Field<string>("SubProject_ID")?.Contains("," + SubID + ",") == true ||
                                 row.Field<string>("SubProject_ID")?.EndsWith("," + SubID) == true ||
                                 row.Field<string>("SubProject_ID")?.Equals(SubID.ToString()) == true) &&
                                 row.Field<string>("Name").ToLower().Trim() == e.ProposedValue.ToString().ToLower().Trim())
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
                    if (!IsLocalTill(e.Row["Corresponding till"].ToString()))
                    {
                        exists = true;

                        e.ProposedValue = "";
                    }
                    else
                    {
                        if (_cashAccounts_dt.Rows.Count > 0)
                            _ObserveCashAccount_ID = _cashAccounts_dt.AsEnumerable()
                             .Where(row => row.Field<string>("Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
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
                             .Where(row => row.Field<string>("ToTill")?.ToLower().Trim().Equals(e.Row["Corresponding till"].ToString().ToLower().Trim()) == true
                             && row.Field<string>("Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
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
                    if (!IsLocalTill(_Till_Type) && !IsLocalTill(e.Row["Corresponding till"].ToString()))
                    {
                        exists = true;
                        e.ProposedValue = "";
                    }
                    else
                    {
                        if (_ExAcc_dt.Rows.Count > 0)
                        {
                            System.Data.DataRow[] exaccrow = _ExAcc_dt.AsEnumerable()
                            .Where(r => r["Name"].ToString().ToLower().Trim() == e.Row["Exchange account"].ToString().ToLower().Trim())
                            .ToArray();

                            if (exaccrow.Length > 0)
                            {
                                int ExAcc_ID = Convert.ToInt32(exaccrow[0]["id"].ToString());

                                if (_all_cashAccounts_dt.Rows.Count > 0)
                                    _Exchange_Cash_ID = _all_cashAccounts_dt.AsEnumerable()
                                     .Where(row => (row.Field<string>("ExchangeAccount_ID")?.StartsWith(ExAcc_ID + ",") == true ||
                                     row.Field<string>("ExchangeAccount_ID")?.Contains("," + ExAcc_ID + ",") == true ||
                                     row.Field<string>("ExchangeAccount_ID")?.EndsWith("," + ExAcc_ID) == true ||
                                     row.Field<string>("ExchangeAccount_ID")?.Equals(ExAcc_ID.ToString()) == true) &&
                                     row.Field<string>("Name").ToLower().Trim() == e.ProposedValue.ToString().ToLower().Trim())
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

                string currency = e.Row.Table.Columns.Contains("Currency") ? e.Row["Currency"]?.ToString() : null;
                if (!string.IsNullOrEmpty(currency) && currency.ToUpper() == "USD")
                {
                    exists = true; // إذا العملة USD → السماح بأي قيمة أو فارغة
                }

                if (e.ProposedValue.ToString().IsNullOrWhiteSpace())
                {
                    if (_Account_Type != "Project" || !IsLocalTill(_Till_Type))
                        exists = true;
                }
                else
                {
                    if (_ShownColumns.Contains("Budget"))
                    {
                        if (_Lines_dt.Rows.Count > 0)
                        {
                            //string d = e.Row["Budget"].ToString();
                            System.Data.DataRow[] budgetrow = _Lines_dt.AsEnumerable()
                            .Where(r => r["budget_Name"].ToString().ToLower().Trim() == e.Row["Budget"].ToString().ToLower().Trim())
                            .ToArray();

                            int BudgetType_ID = -1;
                            if (budgetrow.Length > 0)
                                BudgetType_ID = Convert.ToInt32(budgetrow[0]["BudgetType_ID"].ToString());

                            if (_Original_AvgRate_dt.Rows.Count > 0 && BudgetType_ID != -1)
                                _Rate_ID = _Original_AvgRate_dt.AsEnumerable()
                                 .Where(row =>
                                 {
                                     var fromtp_id = row.Field<object>("fromtp_id");
                                     return (fromtp_id is int id && id == BudgetType_ID)
                                         && row.Field<string>("Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true;
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
                              .Where(row => row.Field<string>("Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
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
                    if (e.ProposedValue.ToString().ToLower().Trim() == "receipt")
                    {
                        exists = true;
                        _IsReceipt = 0;
                    }
                    if (e.ProposedValue.ToString().ToLower().Trim() == "invoice")
                    {
                        exists = true;
                        _IsReceipt = 1;
                    }
                    if (e.ProposedValue.ToString().ToLower().Trim() == "payslip")
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
                bool isDate = false;
                object val = e.Row["Operation date"].ToString();
                string text = val?.ToString() ?? "";

                // 1) If it's already a DateTime, cast directly
                if (val is DateTime dt)
                {
                    ActionMonth = dt;
                    isDate = true;
                }
                // 2) If it's a numeric OLE date (Excel’s internal serial), convert
                else if (double.TryParse(
                             text,
                             NumberStyles.Any,
                             CultureInfo.InvariantCulture,
                             out double oaSerial))
                {
                    ActionMonth = DateTime.FromOADate(oaSerial);
                    isDate = true;
                }
                // 3) Otherwise let .NET try parsing with CurrentCulture OR InvariantCulture
                else if (
                    DateTime.TryParse(
                        text,
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                        out ActionMonth)
                    ||
                    DateTime.TryParse(
                        text,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                        out ActionMonth)
                )
                {
                    isDate = true;
                }


                if (e.Row["Document type"].ToString().ToLower().Trim() == "payslip")
                {
                    if (isDate)
                    {
                        if (_Employee_dt.Rows.Count > 0)
                            _Employee_Salary_ID = _Employee_dt.AsEnumerable()
                                 .Where(row => ((DateTime)row["Month"]).ToString("yyyy/MM") == ActionMonth.ToString("yyyy/MM")
                                 && row.Field<string>("Employee_Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
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
                         .Where(row => row.Field<string>("Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
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
                    exists = _budgets_dt.AsEnumerable().Any(row => row.Field<string>("budget_Name").ToLower().Trim() == e.ProposedValue.ToString().ToLower().Trim());

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
                    System.Data.DataRow[] budgetrow = _budgets_dt.AsEnumerable()
                        .Where(r => r["budget_Name"].ToString().ToLower().Trim() == e.Row["Budget"].ToString().ToLower().Trim())
                        .ToArray();

                    if (budgetrow.Length > 0)
                    {
                        Selected_budgetID = Convert.ToInt32(budgetrow[0]["budget_ID"].ToString());

                        if (_Lines_dt.Rows.Count > 0)
                            _BudgetLine_ID = _Lines_dt.AsEnumerable()
                             .Where(row =>
                             {
                                 var budget_ID = row.Field<object>("budget_ID");
                                 return row.Field<string>("line_Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true
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

                if (!e.Row.RowError.IsNullOrWhiteSpace())
                    TotalErrorRows++;


                //if (e.Row.RowError == "There are errors in this row." ||
                //    e.Row.RowError == "Please ensure that there is at least one AvgRate that matches the conversion currency and extends for a period that includes the transaction date.")
                //    TotalErrorRows++;



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
                    if (isUsdExpenseFromSyr)
                    {
                       _ObserveTill_ID = _all_Tills_dt.AsEnumerable()
                       .Where(row =>
                        row.Field<int>("Account_ID") == _Account_ID &&
                        row.Field<string>("Name")?.ToLower().Trim() == "usd")
                       .Select(row => row.Field<int>("Till_ID"))
                       .DefaultIfEmpty(-1)
                       .FirstOrDefault();

                        if (_ObserveTill_ID != -1)
                        {
                            _WithObserveAcion_ID = true;
                        }
                        else
                        {
                            Login.MultiAction_AnyError = true;
                            e.Row.RowError = "USD till does not exist for this account.";
                        }
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
            ValidateTillResolutionBeforeInsert();

            // =========================================================================
            // 1. [VALIDATION & DEBUGGING] التحقق والطباعة قبل البدء ببناء الاستعلام
            // =========================================================================

            // طباعة تفاصيل المطابقة لمعاينة المشكلة في شاشة الـ Output/Console أثناء الفحص
            System.Diagnostics.Debug.WriteLine($"==================================================");
            System.Diagnostics.Debug.WriteLine($"[Exchange Import - Build Query Debug]");
            System.Diagnostics.Debug.WriteLine($" - Account_ID (Input): {_Account_ID}");
            System.Diagnostics.Debug.WriteLine($" - Till_Type (Input Name): {_Till_Type}");
            System.Diagnostics.Debug.WriteLine($" - Till Type_ID: {_SelectedTillType_ID}");
            System.Diagnostics.Debug.WriteLine($" - Resolved Till_ID (Main): {_Till_ID}");
            System.Diagnostics.Debug.WriteLine($" - ObserveTill_ID: {_ObserveTill_ID}");
            System.Diagnostics.Debug.WriteLine($" - ObserveTill_Type: {_ObserveTill_Type}");
            System.Diagnostics.Debug.WriteLine($" - Exchange Account_ID: {_ExchangeAcc_ID}");
            System.Diagnostics.Debug.WriteLine($" - From Exchange Till_ID: {_FromExchangeTill_ID}");
            System.Diagnostics.Debug.WriteLine($" - To Exchange Till_ID: {_ToExchangeTill_ID}");
            System.Diagnostics.Debug.WriteLine($" - Currency TillType ID: {_currency_tilltype_id}");
            System.Diagnostics.Debug.WriteLine($" - Action Type (_inserted_value): {_inserted_value}");
            System.Diagnostics.Debug.WriteLine($" - WithObserveAction: {_WithObserveAcion_ID}, WithExAcc: {_WithExAcc}");
            System.Diagnostics.Debug.WriteLine($"==================================================");

            // التحقق المنعي: إذا كان المعرف الرئيسي فارغاً أو غير صالح، نمنع العملية فوراً
            // (ملاحظة: إذا كانت الدالة ترجع void، نستخدم throw أو MessageBox بناءً على تصميم الـ Architecture لديك)
            if (_Till_ID <= 0)
            {
                string errorMsg = $"Validation Error in Exchange Import:\n" +
                                  $"No matching till found in database for Account_ID = {_Account_ID} and Type_ID/Name = '{_Till_Type}'.\n\n" +
                                  $"The insert operation was canceled to prevent saving a NULL Till_ID.";

                System.Windows.MessageBox.Show(errorMsg, "Database Integrity Protection",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

                throw new InvalidOperationException($"Import Aborted: Till_ID cannot be null for Account_ID = {_Account_ID}");
            }

            // =========================================================================
            // 2. الكود الأصلي بعد التأمين وإضافة المعاملات
            // =========================================================================
            k++;
            string temp_query = "";
            string log_query = "";

            Login.MultiAction_sc.Parameters.AddWithValue("@Till_ID" + k, _Till_ID);
            Login.MultiAction_sc.Parameters.AddWithValue("@Till_Type" + k, _Till_Type);
            Login.MultiAction_sc.Parameters.AddWithValue("@ObserveTill_Type" + k, _ObserveTill_Type);
            Login.MultiAction_sc.Parameters.AddWithValue("@TillIsLocal" + k, Helper.CurrencyService.IsLocalCurrency(_Till_Type, false) ? 1 : 0);
            Login.MultiAction_sc.Parameters.AddWithValue("@ObserveTillIsLocal" + k, Helper.CurrencyService.IsLocalCurrency(_ObserveTill_Type, false) ? 1 : 0);
            Login.MultiAction_sc.Parameters.AddWithValue("@FromExchangeTill_ID" + k, _FromExchangeTill_ID);
            Login.MultiAction_sc.Parameters.AddWithValue("@ToExchangeTill_ID" + k, _ToExchangeTill_ID);

            // فحص دقيق لمنع التداخل العشوائي بين عمليات الـ Exchange العادية والـ Expense المخصصة للـ SYR
            bool isUsdExpenseFromSyr = _inserted_value == "E" &&
                                       IsLocalTill(_Till_Type) &&
                                       _currency_tilltype_id == 2 &&
                                       !_WithExAcc;

            if (_ObserveTill_ID == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ObserveTill_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@ObserveTill_ID" + k, _ObserveTill_ID);
            }

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

            int currencyTilltypeIdForSave = string.Equals(_Account_Type, "Personal", StringComparison.OrdinalIgnoreCase)
                ? -1
                : _currency_tilltype_id;

            if (currencyTilltypeIdForSave == -1)
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Currency_tilltype_id" + k, DBNull.Value);
            }
            else
            {
                Login.MultiAction_sc.Parameters.AddWithValue("@Currency_tilltype_id" + k, currencyTilltypeIdForSave);
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

            temp_query += @"INSERT INTO `action_not_approved`(`id`,`Till_ID`,`ObserveTill_ID`, `AType_ID`,`currency_tilltype_id`, `Amount`, `rate`, `ActionDate`
                    ,  `Note`, `E_Note`, `IsReceipt`, `ReceiptNote`, `Employee_Salary_ID`, `Rate_ID`
                    , `MicroProject_ID`, `BudgetLine_ID`, `IncomeType_ID`, `CashAccount_ID`, `ObserveCashAccount_ID`, `Exchange_Cash_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                    VALUES (@Action_ID" + k + " ,@Till_ID" + k + " ,@ObserveTill_ID" + k + " ,(select id from actiontype where shortname = @AType" + k + "), @Currency_tilltype_id" + k + ", @Amount" + k + ", @rate" + k + ", @ActionDate" + k + ",@Note" + k + ",@E_Note" + k + ", @IsReceipt" + k + ", @ReceiptNote" + k + ",  @Employee_Salary_ID" + k + ",@Rate_ID" + k + ", "
                            + "@MicroProject_ID" + k + ", @BudgetLine_ID" + k + ", @IncomeType_ID" + k + ", @CashAccount_ID" + k + ", @ObserveCashAccount_ID" + k + ", @Exchange_Cash_ID" + k + ", @Donor_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ", '1');";

            temp_query += "SET @Action_ID = @Action_ID" + k + " +1;";

            if (_WithObserveAcion_ID && !isUsdExpenseFromSyr)
            {
                temp_query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                        ,  `Note`,`E_Note`, `IsReceipt`, `ReceiptNote`, `Employee_Salary_ID`, `CashAccount_ID`,`ObserveCashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                        VALUES(@ObserveTill_ID" + k + ", @Till_ID" + k + ", @Action_ID" + k + ", (select id from actiontype where shortname = 'I'), @ConvertedAmount" + k + ",@rate" + k + ", @ActionDate" + k + ", @Note" + k + ",@E_Note" + k + ", @IsReceipt" + k + ", @ReceiptNote" + k + ", @Employee_Salary_ID" + k + ",@ObserveCashAccount_ID" + k + ", @CashAccount_ID" + k + ", @Donor_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ", '0'); ";

                temp_query += " SET @ObserveAcion_ID" + k + " = Last_Insert_ID();";
                temp_query += " UPDATE action_not_approved SET ObserveAcion_ID = @ObserveAcion_ID" + k + " WHERE ID = @Action_ID" + k + ";";
                temp_query += "SET @Action_ID = @Action_ID" + k + " +2;";

                if (_WithExAcc)
                {
                    temp_query += " Set @From_id" + k + " = @FromExchangeTill_ID" + k + ";";
                    temp_query += " Set @To_id" + k + " = @ToExchangeTill_ID" + k + ";";

                    temp_query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            , `Note`,`E_Note`, `IsReceipt`, `ReceiptNote`, `Employee_Salary_ID`, `CashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES(@From_id" + k + ", @Till_ID" + k + ", @Action_ID" + k + ", (select id from actiontype where shortname = 'I'), @Amount" + k + " ,@rate" + k + ", @ActionDate" + k + ", @Note" + k + ",@E_Note" + k + ", @IsReceipt" + k + ", @ReceiptNote" + k + ", @Employee_Salary_ID" + k + ", (case when @TillIsLocal" + k + " = 1 then @Exchange_Cash_ID" + k + " else null end), @Donor_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ", '0'); ";

                    temp_query += " SET @FromExTill_Acc_ID" + k + " = Last_Insert_ID();";

                    temp_query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            , `Note`,`E_Note`, `IsReceipt`, `ReceiptNote`,`Employee_Salary_ID`, `CashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES(@To_id" + k + ", @Till_ID" + k + ", @Action_ID" + k + ", (select id from actiontype where shortname = 'E'), @ConvertedAmount" + k + " ,@rate" + k + ", @ActionDate" + k + ", @Note" + k + ", @E_Note" + k + ", @IsReceipt" + k + ", @ReceiptNote" + k + ", @Employee_Salary_ID" + k + ", (case when @ObserveTillIsLocal" + k + " = 1 then @Exchange_Cash_ID" + k + " else null end), @Donor_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ", '0'); ";

                    temp_query += " SET @ToExTill_Acc_ID" + k + " = Last_Insert_ID();";
                    temp_query += "SET @Action_ID = @Action_ID" + k + " +4;";
                }
            }
            else if (_WithObserveAcion_ID && isUsdExpenseFromSyr)
            {
                temp_query += @"INSERT INTO action_not_approved (`Till_ID`,`ObserveTill_ID`,`ObserveAcion_ID`,`AType_ID`,`currency_tilltype_id`,`Amount`,`rate`,`ActionDate`,`Note`,`E_Note`,`IsReceipt`,`ReceiptNote`,`Employee_Salary_ID`,`CashAccount_ID`,`Donor_ID`,`created_at`,`User_ID`,`SourceAction`,`BudgetLine_ID`) 
                      VALUES (@ObserveTill_ID" + k + ",@Till_ID" + k + ",@Action_ID" + k + ",(select id from actiontype where shortname='O'),@Currency_tilltype_id" + k + ",@Amount" + k + ",@rate" + k + ",@ActionDate" + k + ",@Note" + k + ",@E_Note" + k + ",@IsReceipt" + k + ",@ReceiptNote" + k + ",@Employee_Salary_ID" + k + ",@CashAccount_ID" + k + ",@Donor_ID" + k + ",@Todaydate" + k + ",@Uesr_ID" + k + ",'0',@BudgetLine_ID" + k + ");";

                temp_query += "SET @UsdOutcome_ID" + k + " = Last_Insert_ID();";

                temp_query += @"INSERT INTO action_not_approved (`Till_ID`,`ObserveTill_ID`,`ObserveAcion_ID`,`AType_ID`,`currency_tilltype_id`,`Amount`,`rate`,`ActionDate`,`Note`,`E_Note`,`IsReceipt`,`ReceiptNote`,`Employee_Salary_ID`,`CashAccount_ID`,`Donor_ID`,`created_at`,`User_ID`,`SourceAction`,`BudgetLine_ID`)
                      VALUES (@Till_ID" + k + ",@ObserveTill_ID" + k + ",@Action_ID" + k + ",(select id from actiontype where shortname='I'),@Currency_tilltype_id" + k + ",@Amount" + k + ",@rate" + k + ",@ActionDate" + k + ",@Note" + k + ",@E_Note" + k + ",@IsReceipt" + k + ",@ReceiptNote" + k + ",@Employee_Salary_ID" + k + ",@CashAccount_ID" + k + ",@Donor_ID" + k + ",@Todaydate" + k + ",@Uesr_ID" + k + ",'0',@BudgetLine_ID" + k + ");";

                temp_query += " SET @SyrIncome_ID" + k + " = LAST_INSERT_ID();";
                temp_query += " SET @Action_ID = @Action_ID" + k + " + 3;";
            }

            // بناء الـ Logs
            temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @Action_ID" + k + ", CONCAT('action Type = ', @AType" + k + "), CONCAT('till ID = ',@Till_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

            if (_WithObserveAcion_ID && !isUsdExpenseFromSyr)
            {
                temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @ObserveAcion_ID" + k + ", 'action Type = I', CONCAT('till ID = ', @ObserveTill_ID" + k + " ,' - Auto Created With action ID = ',@Action_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                if (_WithExAcc)
                {
                    temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @FromExTill_Acc_ID" + k + ", 'action Type = I', CONCAT('till ID = ', @From_id" + k + " ,' - Auto Created With action ID = ',@Action_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @ToExTill_Acc_ID" + k + ", 'action Type = E', CONCAT('till ID = ', @To_id" + k + " ,' - Auto Created With action ID = ',@Action_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }
            }
            if (_WithObserveAcion_ID && isUsdExpenseFromSyr)
            {
                temp_query += "INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                              "VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @UsdOutcome_ID" + k + ", 'action Type = O', " +
                              "CONCAT('till ID = ', @ObserveTill_ID" + k + " ,' - Linked to action ID = ', @Action_ID" + k + "), " +
                              "'" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                temp_query += "INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                              "VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @SyrIncome_ID" + k + ", 'action Type = I', " +
                              "CONCAT('till ID = ', @Till_ID" + k + " ,' - Linked to action ID = ', @Action_ID" + k + "), " +
                              "'" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
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
