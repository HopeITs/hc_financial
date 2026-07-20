using Financial_WPF_App.Controls;
using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    public partial class AddAction_Window2 : Window
    {
        private readonly UC_IncomeOperation _incomeControl = new UC_IncomeOperation();
        private readonly UC_ExpensesOperation _expensesControl = new UC_ExpensesOperation();
        private readonly UC_ExchangeOperation _exchangeControl = new UC_ExchangeOperation();

        private readonly Models.Till _till = new Models.Till();
        private readonly Account _account = new Account();
        private readonly Rate _rate = new Rate();
        private readonly Budget_Lines _budgetLines = new Budget_Lines();
        private readonly Beneficiary _beneficiary = new Beneficiary();
        private readonly Employee _employee = new Employee();
        private readonly Main_Window_2 _mainWindow;

        private DataTable _tills;
        private DataTable _cashTills;
        private DataTable _correspondingTills;
        private DataTable _exchangeAccounts;
        private int _accountId = -1;
        private int _subprojectId = -1;
        private int _tillId = -1;
        private string _accountType = string.Empty;
        private string _projectType = string.Empty;
        private string _tillType = string.Empty;
        private string _tillTypeForSave = string.Empty;
        private bool _isResetting;
        private string[] _attachmentPaths = new string[0];
        private OpenFileDialog _attachmentDialog;
        private Thread _attachmentDialogThread;
        private Brush _themeColor = (SolidColorBrush)Application.Current.Resources["GreenBlue"];

        public AddAction_Window2(Main_Window_2 mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            InitializeComponent();

            Amount_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
            ActionDate_datepicker.SelectedDate = DateTime.Today;
            _expensesControl.CurrencyChanged += ExpensesControl_CurrencyChanged;
            _expensesControl.BudgetChanged += ExpensesControl_BudgetChanged;
            _exchangeControl.CorrespondingTillChanged += ExchangeControl_CorrespondingTillChanged;
            _exchangeControl.ExchangeAccountChanged += ExchangeControl_ExchangeAccountChanged;
            Account_comboBox.IsEnabled = false;
            Till_comboBox.IsEnabled = false;
            Type_comboBox.IsEnabled = false;
            SetCommonInputsEnabled(false);
            FillMainComboBoxes();
        }

        private void FillMainComboBoxes()
        {
            _isResetting = true;
            _tills = FinancialAuthorizationService.FilterTillsByRole(_till.Select_All_tills(), _mainWindow.AllAccounts_ds);
            Subproject_comboBox.ItemsSource = null;
            Subproject_comboBox.SelectedIndex = -1;
            Account_comboBox.ItemsSource = null;
            Account_comboBox.SelectedIndex = -1;
            Till_comboBox.ItemsSource = null;
            Till_comboBox.SelectedIndex = -1;
            Type_comboBox.ItemsSource = null;
            Type_comboBox.SelectedIndex = -1;
            Account_comboBox.IsEnabled = false;
            Till_comboBox.IsEnabled = false;
            Type_comboBox.IsEnabled = false;
            ResetOperationControls();
            ClearCommonEntryFields();
            SetCommonInputsEnabled(false);
            if (_tills == null || _tills.Rows.Count == 0)
            {
                _isResetting = false;
                return;
            }

            Subproject_comboBox.ItemsSource = BuildSubprojectList(_tills);
            Subproject_comboBox.SelectedIndex = -1;
            _isResetting = false;
        }

        private static DataView BuildSubprojectList(DataTable tills)
        {
            DataTable projects = new DataView(tills).ToTable(true, "Subproject_ID", "Subproject_Name");
            foreach (DataRow row in projects.Rows)
            {
                if (row.IsNull("Subproject_ID"))
                    row["Subproject_ID"] = 0;
            }
            return projects.DefaultView;
        }

        private void Subproject_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isResetting)
                return;

            if (Subproject_comboBox.SelectedIndex == -1 || _tills == null)
            {
                ResetBelowProject();
                return;
            }

            ResetBelowProject();

            string subprojectName = GetSelectedSubprojectName();
            _subprojectId = GetSubprojectId(Subproject_comboBox.SelectedValue);
            bool isPersonalExchange = IsPersonalExchangeProject(Subproject_comboBox.SelectedValue, subprojectName);

            DataRow[] rows = (isPersonalExchange
                    ? _tills.AsEnumerable().Where(IsPersonalOrExchangeAccountType)
                    : _tills.AsEnumerable().Where(r =>
                        !r.IsNull("Subproject_ID") && Convert.ToInt32(r["Subproject_ID"]) == _subprojectId))
                .ToArray();
            if (rows.Length == 0)
                return;

            DataView accountView = rows.CopyToDataTable().DefaultView
                .ToTable(true, "Account_ID", "Account_Name", "Accounttype_Name", "Category_Name").DefaultView;
            accountView.Sort = "Account_ID DESC";
            Account_comboBox.ItemsSource = accountView;
            Account_comboBox.SelectedIndex = -1;
            Account_comboBox.IsEnabled = Account_comboBox.Items.Count > 0;
        }

        private string GetSelectedSubprojectName()
        {
            var row = Subproject_comboBox.SelectedItem as DataRowView;
            return row == null ? string.Empty : Convert.ToString(row["Subproject_Name"]);
        }

        private static int GetSubprojectId(object selectedValue)
        {
            if (selectedValue == null || selectedValue == DBNull.Value)
                return 0;
            int id;
            return int.TryParse(Convert.ToString(selectedValue), out id) ? id : 0;
        }

        private static bool IsPersonalExchangeProject(object selectedValue, string subprojectName)
        {
            if (selectedValue == null || selectedValue == DBNull.Value)
                return true;

            if (GetSubprojectId(selectedValue) <= 0)
                return true;

            return IsPersonalExchangeProjectName(subprojectName);
        }

        private static bool IsPersonalExchangeProjectName(string subprojectName)
        {
            string name = (subprojectName ?? string.Empty).Trim();
            return string.Equals(name, "Personal/Exchange", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Personal / Exchange", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Exchange/Personal", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Exchange / Personal", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPersonalOrExchangeAccountType(DataRow row)
        {
            string accountType = Convert.ToString(row["Accounttype_Name"]);
            return string.Equals(accountType, "Personal", StringComparison.OrdinalIgnoreCase)
                || string.Equals(accountType, "Exchange", StringComparison.OrdinalIgnoreCase);
        }

        private void Account_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isResetting)
                return;

            ResetBelowAccount();
            var row = Account_comboBox.SelectedItem as DataRowView;
            if (row == null || _tills == null)
                return;

            _accountId = Convert.ToInt32(row["Account_ID"]);
            _accountType = Convert.ToString(row["Accounttype_Name"]);
            _projectType = Convert.ToString(row["Category_Name"]);
            ApplyColorTheme();

            DataRow[] rows = _tills.Select("Account_ID = " + _accountId);
            if (rows.Length > 0)
                Till_comboBox.ItemsSource = rows.CopyToDataTable().DefaultView
                    .ToTable(true, "Till_ID", "Tilltypes_ID", "Tilltypes_Name").DefaultView;
            Till_comboBox.SelectedIndex = -1;
            Till_comboBox.IsEnabled = Till_comboBox.Items.Count > 0;
        }

        private void ApplyColorTheme()
        {
            // Same mapping as original AddAction_Window.Apply_Color_Theme.
            if (IsBudgetProject)
                _themeColor = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
            else if (IsMicroProject)
                _themeColor = (SolidColorBrush)Application.Current.Resources["HopeRed"];
            else if (IsExchangeAccount)
                _themeColor = (SolidColorBrush)Application.Current.Resources["Orange"];
            else if (IsPersonalAccount)
                _themeColor = (SolidColorBrush)Application.Current.Resources["PastelGreen"];
            else
                _themeColor = (SolidColorBrush)Application.Current.Resources["GreenBlue"];

            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(Content, _themeColor);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Till_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isResetting)
                return;

            var row = Till_comboBox.SelectedItem as DataRowView;
            ResetBelowTill();
            if (row == null)
                return;

            _tillId = Convert.ToInt32(row["Till_ID"]);
            _tillType = Convert.ToString(row["Tilltypes_Name"]).Trim().ToUpperInvariant();
            _tillTypeForSave = Convert.ToString(row["Tilltypes_Name"]).Trim();
            LoadContextData();
            ConfigureAvailableOperations();
        }

        private void ConfigureAvailableOperations()
        {
            var operations = new List<OperationOption>();
            OperationPrivileges privileges = FinancialAuthorizationService.GetOperationPrivileges(
                Settings.Default.Role, _accountType, _tillType);

            if (IsExchangeAccount)
            {
                if (privileges.Exchange)
                    operations.Add(new OperationOption("Exchange"));
            }
            else if (IsPersonalAccount)
            {
                if (privileges.Income)
                    operations.Add(new OperationOption("Income"));
                if (privileges.Expand)
                    operations.Add(new OperationOption("Expenses"));
            }
            else if (IsBudgetProject || IsMicroProject)
            {
                if (_tillType == "SYR" && privileges.Expand)
                    operations.Add(new OperationOption("Expenses"));
                else if (_tillType == "USD" || _tillType == "EUR")
                {
                    if (privileges.Income)
                        operations.Add(new OperationOption("Income"));
                    if (privileges.Exchange)
                        operations.Add(new OperationOption("Exchange"));
                }
            }

            Type_comboBox.ItemsSource = operations;
            Type_comboBox.DisplayMemberPath = "Text";
            Type_comboBox.SelectedValuePath = "Value";
            Type_comboBox.SelectedIndex = -1;
            Type_comboBox.IsEnabled = operations.Count > 0;
        }

        private void Type_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isResetting)
                return;

            ResetOperationControls();
            ClearCommonEntryFields();
            string operation = Convert.ToString(Type_comboBox.SelectedValue);
            SetCommonInputsEnabled(!string.IsNullOrWhiteSpace(operation));
            switch (operation)
            {
                case "Expenses":
                    if (IsPersonalAccount)
                        ConfigurePersonalExpenses();
                    else
                        ConfigureExpenses();
                    DynamicOperationContainer.Content = _expensesControl;
                    break;
                case "Income":
                    if (IsPersonalAccount)
                    {
                        ConfigurePersonalIncome();
                        DynamicOperationContainer.Content = _incomeControl;
                    }
                    else
                    {
                        _incomeControl.Configure(GetDonors()?.DefaultView);
                        DynamicOperationContainer.Content = _incomeControl;
                    }
                    break;
                case "Exchange":
                    ConfigureExchange();
                    DynamicOperationContainer.Content = _exchangeControl;
                    break;
                default:
                    DynamicOperationContainer.Content = null;
                    break;
            }
        }

        private void LoadContextData()
        {
            _cashTills = FilterCashTillsForRole(_account.Select_Cash_Account("", ""));
            _correspondingTills = _till.Select(_accountId.ToString(), _tillId, "");
            _exchangeAccounts = _account.Select_ExAcc_Tills(_tillType, "");
        }

        private void ConfigurePersonalIncome()
        {
            int tillCurrencyId = GetTillTypeId(_tillType);
            // Cash Till stays enabled for SYR/USD/EUR; Save requires it only for SYR.
            _incomeControl.ConfigureForPersonal(
                FilterPersonalCashTills(tillCurrencyId)?.DefaultView,
                enableCashTill: true);
        }

        private void ConfigurePersonalExpenses()
        {
            int tillCurrencyId = GetTillTypeId(_tillType);
            // Cash Till stays enabled for SYR/USD/EUR; Save requires it only for SYR.
            _expensesControl.ConfigureForPersonal(true,
                FilterPersonalCashTills(tillCurrencyId)?.DefaultView);
        }

        private DataTable FilterPersonalCashTills(int currencyTillTypeId)
        {
            if (_cashTills == null || _accountId <= 0)
                return null;

            DataRow[] rows = _cashTills.AsEnumerable().Where(r =>
                ContainsDelimitedId(Convert.ToString(r["PersonalAccount_ID"]), _accountId) &&
                (currencyTillTypeId <= 0 || Convert.ToInt32(r["currency_tilltype_id"]) == currencyTillTypeId)).ToArray();
            return rows.Length == 0 ? _cashTills.Clone() : rows.CopyToDataTable();
        }

        private void ConfigureExpenses()
        {
            DataTable beneficiaries = IsMicroProject ? _beneficiary.Select("", _accountId.ToString(), "") : null;
            DataTable employees = IsBudgetProject ? _employee.Select_Subproject_Monthly_Employee(_subprojectId) : null;

            _expensesControl.Configure(IsMicroProject,
                _till.Select_All_TillTypes_Currency()?.DefaultView,
                IsBudgetProject ? _budgetLines.Select_AllAvilableBudgetLines(_accountId) : null,
                null, null, beneficiaries, employees, ActionDate_datepicker.SelectedDate,
                requireProjectFieldsWithoutCurrency: IsSyrTill(_tillType));
            ExpensesControl_CurrencyChanged(this, EventArgs.Empty);
        }

        private void ExpensesControl_CurrencyChanged(object sender, EventArgs e)
        {
            int currencyId = _expensesControl.SelectedCurrencyId;
            int effectiveCurrencyId = currencyId > 0 ? currencyId : GetTillTypeId(_tillType);
            _expensesControl.SetCashTills(FilterProjectCashTills(effectiveCurrencyId)?.DefaultView);
            RefreshExpenseAverageRates();
        }

        private void ExpensesControl_BudgetChanged(object sender, EventArgs e)
        {
            RefreshExpenseAverageRates();
        }

        private void RefreshExpenseDateDependentData()
        {
            _expensesControl.SetActionDate(ActionDate_datepicker.SelectedDate);
            RefreshExpenseAverageRates();
        }

        private void RefreshExpenseAverageRates()
        {
            int currencyId = _expensesControl.SelectedCurrencyId;
            _expensesControl.SetAverageRates(currencyId > 0 && IsCurrency(currencyId, "SYR")
                ? GetAvailableAverageRates()?.DefaultView
                : null);
        }

        private void ConfigureExchange()
        {
            int tillCurrencyId = GetTillTypeId(_tillType);
            if (IsExchangeAccount)
            {
                _exchangeControl.Configure(_tillType, true, GetExchangeDestinationsForExchangeAccount()?.DefaultView,
                    null, null, null, null, null,
                    FilterExchangeAccountCashTills(tillCurrencyId)?.DefaultView);
                return;
            }

            DataTable destinations = GetExchangeDestinations();
            _exchangeControl.Configure(_tillType, false, destinations?.DefaultView, GetDonors()?.DefaultView,
                null, null, null, null,
                FilterProjectCashTills(tillCurrencyId)?.DefaultView);
        }

        private void ExchangeControl_CorrespondingTillChanged(object sender, EventArgs e)
        {
            string correspondingTillType = _exchangeControl.SelectedCorrespondingTillType;
            string correspondingTypeNormalized = (correspondingTillType ?? string.Empty).Trim().ToUpperInvariant();
            int correspondingCurrencyId = _exchangeControl.SelectedCorrespondingTillTypeId;
            if (correspondingCurrencyId <= 0)
                correspondingCurrencyId = GetTillTypeId(correspondingTillType);

            if (IsExchangeAccount)
            {
                // Match original Add Action: populate Corresponding Cash Till for the selected destination.
                // Prefer the destination till's Type_ID so SYR cash tills resolve even when this account
                // has no SYR till row in the main till list.
                _exchangeControl.SetCorrespondingCashTills(
                    string.IsNullOrWhiteSpace(correspondingTillType)
                        ? null
                        : FilterExchangeAccountCashTills(correspondingCurrencyId, preferCurrencyFilter: true)?.DefaultView);
                return;
            }

            _exchangeControl.SetCorrespondingCashTills(
                correspondingCurrencyId > 0
                    ? FilterProjectCashTills(correspondingCurrencyId)?.DefaultView
                    : null);

            DataTable accounts = null;
            if (_exchangeAccounts != null && !string.IsNullOrWhiteSpace(correspondingTillType))
            {
                DataRow[] rows = _exchangeAccounts.Select("toTill = '" + correspondingTillType.Replace("'", "''").ToLowerInvariant() + "'");
                if (rows.Length > 0)
                    accounts = rows.CopyToDataTable();
            }
            _exchangeControl.SetExchangeAccounts(accounts?.DefaultView);

            if (UC_ExchangeOperation.ShouldEnableOriginAvgRate(_tillType, correspondingTypeNormalized))
                _exchangeControl.SetAverageRates(
                    GetExchangeAverageRates(_tillType, correspondingTypeNormalized)?.DefaultView);
            else
                _exchangeControl.SetAverageRates(null);
        }

        private void ExchangeControl_ExchangeAccountChanged(object sender, EventArgs e)
        {
            int exchangeAccountId = _exchangeControl.SelectedExchangeAccountId;
            _exchangeControl.SetExchangeCashTills(FilterCashTillsByAccount(_cashTills, "ExchangeAccount_ID", exchangeAccountId)?.DefaultView);
        }

        private DataTable GetExchangeDestinationsForExchangeAccount()
        {
            if (_correspondingTills == null)
                return null;

            if (_tillType == "SYR")
                return _correspondingTills;

            OperationPrivileges privileges = FinancialAuthorizationService.GetOperationPrivileges(
                Settings.Default.Role, _accountType, _tillType);
            if (privileges.ExchangeToForeign)
                return _correspondingTills;

            DataRow[] rows = _correspondingTills.Select("Type = 'Syr'");
            return rows.Length == 0 ? _correspondingTills.Clone() : rows.CopyToDataTable();
        }

        private DataTable FilterExchangeAccountCashTills(int currencyTillTypeId, bool preferCurrencyFilter = false)
        {
            if (_cashTills == null || _accountId <= 0)
                return null;

            DataRow[] byAccount = _cashTills.AsEnumerable().Where(r =>
                ContainsDelimitedId(Convert.ToString(r["ExchangeAccount_ID"]), _accountId)).ToArray();

            if (byAccount.Length == 0)
                return _cashTills.Clone();

            if (preferCurrencyFilter && currencyTillTypeId > 0)
            {
                DataRow[] byCurrency = byAccount.Where(r =>
                    Convert.ToInt32(r["currency_tilltype_id"]) == currencyTillTypeId).ToArray();
                // Prefer currency-matched SYR/USD/EUR cash tills; fall back to account list like original.
                if (byCurrency.Length > 0)
                    return byCurrency.CopyToDataTable();
            }
            else if (!preferCurrencyFilter && currencyTillTypeId > 0)
            {
                DataRow[] byCurrency = byAccount.Where(r =>
                    Convert.ToInt32(r["currency_tilltype_id"]) == currencyTillTypeId).ToArray();
                if (byCurrency.Length > 0)
                    return byCurrency.CopyToDataTable();
                return _cashTills.Clone();
            }

            return byAccount.CopyToDataTable();
        }

        private DataTable GetExchangeDestinations()
        {
            if (_correspondingTills == null)
                return null;

            OperationPrivileges privileges = FinancialAuthorizationService.GetOperationPrivileges(
                Settings.Default.Role, _accountType, _tillType);
            if (privileges.ExchangeToForeign)
                return _correspondingTills;

            DataRow[] syrRows = _correspondingTills.AsEnumerable().Where(r =>
                string.Equals(Convert.ToString(r["Type"]), "SYR", StringComparison.OrdinalIgnoreCase)).ToArray();
            return syrRows.Length == 0 ? _correspondingTills.Clone() : syrRows.CopyToDataTable();
        }

        private DataTable GetDonors() => _accountId > 0 ? _account.Select_Account_Donors(_accountId) : null;

        private DataTable GetAvailableAverageRates()
        {
            DataTable rates = _rate.Select_AllAvilableRates(_accountId.ToString(), _tillType, true);
            if (rates == null || rates.Rows.Count == 0 || ActionDate_datepicker.SelectedDate == null)
                return rates;
            DateTime date = ActionDate_datepicker.SelectedDate.Value.Date;
            DataRow[] rows = rates.AsEnumerable().Where(r =>
                (Convert.ToDateTime(r["BeginDate"]).Date <= date && Convert.ToDateTime(r["EndDate"]).Date >= date) ||
                Convert.ToDecimal(r["Expenditures"]) < Convert.ToDecimal(r["Exchanged_Value"])).ToArray();
            if (IsBudgetProject)
            {
                int budgetTypeId = _expensesControl.SelectedBudgetTypeId;
                rows = budgetTypeId <= 0
                    ? new DataRow[0]
                    : rows.Where(r => Convert.ToInt32(r["fromtp_id"]) == budgetTypeId).ToArray();
            }
            return rows.Length == 0 ? rates.Clone() : rows.CopyToDataTable();
        }

        /// <summary>
        /// Origin Avg Rate for a Budget/Micro Project exchange Source Till -> Corresponding Till pair.
        /// </summary>
        private DataTable GetExchangeAverageRates(string fromTill, string toTill)
        {
            if (_accountId <= 0 || string.IsNullOrWhiteSpace(fromTill) || string.IsNullOrWhiteSpace(toTill))
                return null;

            // Select_AllAvilableRates filters by ToTill; restrict the returned rows by FromTill
            // so the ComboBox contains only rates for the currently selected exchange pair.
            DataTable rates = _rate.Select_AllAvilableRates(_accountId.ToString(), toTill, true);
            if (rates == null)
                return null;
            if (rates.Rows.Count == 0)
                return rates.Clone();

            string from = fromTill.Trim();
            DataRow[] pairRows = rates.AsEnumerable().Where(r =>
                string.Equals(Convert.ToString(r["FromTill"]), from, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (pairRows.Length == 0)
                return rates.Clone();

            DataTable pairRates = pairRows.CopyToDataTable();
            if (ActionDate_datepicker.SelectedDate == null)
                return pairRates;

            DateTime date = ActionDate_datepicker.SelectedDate.Value.Date;
            DataRow[] filtered = pairRates.AsEnumerable().Where(r =>
                (Convert.ToDateTime(r["BeginDate"]).Date <= date && Convert.ToDateTime(r["EndDate"]).Date >= date) ||
                Convert.ToDecimal(r["Expenditures"]) < Convert.ToDecimal(r["Exchanged_Value"])).ToArray();
            return filtered.Length == 0 ? pairRates.Clone() : filtered.CopyToDataTable();
        }

        private DataTable FilterProjectCashTills(int currencyId)
        {
            if (_cashTills == null || currencyId <= 0)
                return null;
            DataRow[] rows = _cashTills.AsEnumerable().Where(r =>
                Convert.ToInt32(r["currency_tilltype_id"]) == currencyId &&
                ContainsDelimitedId(Convert.ToString(r["SubProject_ID"]), _subprojectId)).ToArray();
            return rows.Length == 0 ? _cashTills.Clone() : rows.CopyToDataTable();
        }

        private static DataTable FilterCashTillsByAccount(DataTable source, string column, int accountId)
        {
            if (source == null || accountId <= 0)
                return null;
            DataRow[] rows = source.AsEnumerable().Where(r => ContainsDelimitedId(Convert.ToString(r[column]), accountId)).ToArray();
            return rows.Length == 0 ? source.Clone() : rows.CopyToDataTable();
        }

        private static bool ContainsDelimitedId(string values, int id)
        {
            return (values ?? string.Empty).Split(',').Any(value => value.Trim() == id.ToString());
        }

        private static DataTable FilterCashTillsForRole(DataTable source)
        {
            string role = Settings.Default.Role;
            if (source == null || (role != "Accountant" && role != "Financial Manager" && role != "Employee"))
                return source;
            string configured = Convert.ToString(Settings.Default.User_CashAccounts);
            var allowed = new HashSet<int>(configured.Split(',').Select(value =>
            {
                int id;
                return int.TryParse(value, out id) ? id : -1;
            }).Where(id => id > 0));
            if (allowed.Count == 0)
                return source.Clone();
            DataRow[] rows = source.AsEnumerable().Where(r => allowed.Contains(Convert.ToInt32(r["ID"]))).ToArray();
            return rows.Length == 0 ? source.Clone() : rows.CopyToDataTable();
        }

        private int GetTillTypeId(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return -1;

            if (_tills != null)
            {
                DataRow row = _tills.AsEnumerable().FirstOrDefault(r =>
                    string.Equals(Convert.ToString(r["Tilltypes_Name"]), type, StringComparison.OrdinalIgnoreCase));
                if (row != null)
                    return Convert.ToInt32(row["Tilltypes_ID"]);
            }

            // Fallback when the selected account's till list does not include the target currency
            // (e.g. USD Exchange Account looking up SYR for Corresponding Cash Till).
            DataTable currencies = _till.Select_All_TillTypes_Currency();
            if (currencies != null && currencies.Rows.Count > 0)
            {
                DataRow currencyRow = currencies.AsEnumerable().FirstOrDefault(r =>
                    string.Equals(Convert.ToString(r["Name"]), type, StringComparison.OrdinalIgnoreCase));
                if (currencyRow != null)
                    return Convert.ToInt32(currencyRow["ID"]);
            }

            // EUR (and other types) are not in Select_All_TillTypes_Currency — resolve from corresponding tills.
            if (_correspondingTills != null)
            {
                DataRow corresponding = _correspondingTills.AsEnumerable().FirstOrDefault(r =>
                    string.Equals(Convert.ToString(r["Type"]), type, StringComparison.OrdinalIgnoreCase));
                if (corresponding != null && corresponding.Table.Columns.Contains("Type_ID") &&
                    corresponding["Type_ID"] != DBNull.Value)
                    return Convert.ToInt32(corresponding["Type_ID"]);
            }

            return -1;
        }

        private bool IsCurrency(int id, string name)
        {
            return _tills != null && _tills.AsEnumerable().Any(r => Convert.ToInt32(r["Tilltypes_ID"]) == id &&
                string.Equals(Convert.ToString(r["Tilltypes_Name"]), name, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsBudgetProject => string.Equals(_projectType, "Budget", StringComparison.OrdinalIgnoreCase);
        private bool IsMicroProject => string.Equals(_projectType, "Micro Projects", StringComparison.OrdinalIgnoreCase);
        private bool IsExchangeAccount => string.Equals(_accountType, "Exchange", StringComparison.OrdinalIgnoreCase);
        private bool IsPersonalAccount => string.Equals(_accountType, "Personal", StringComparison.OrdinalIgnoreCase);

        private static bool IsSyrTill(string tillType)
        {
            return string.Equals((tillType ?? string.Empty).Trim(), "SYR", StringComparison.OrdinalIgnoreCase);
        }

        private void ResetBelowProject()
        {
            _isResetting = true;
            Account_comboBox.ItemsSource = null;
            Account_comboBox.SelectedIndex = -1;
            Till_comboBox.ItemsSource = null;
            Till_comboBox.SelectedIndex = -1;
            Type_comboBox.ItemsSource = null;
            Type_comboBox.SelectedIndex = -1;
            Account_comboBox.IsEnabled = false;
            Till_comboBox.IsEnabled = false;
            Type_comboBox.IsEnabled = false;
            _subprojectId = -1;
            _accountId = _tillId = -1;
            _accountType = _projectType = _tillType = _tillTypeForSave = string.Empty;
            ResetOperationControls();
            ClearCommonEntryFields();
            SetCommonInputsEnabled(false);
            _isResetting = false;
        }

        private void ResetBelowAccount()
        {
            _isResetting = true;
            Till_comboBox.ItemsSource = null;
            Till_comboBox.SelectedIndex = -1;
            Type_comboBox.ItemsSource = null;
            Type_comboBox.SelectedIndex = -1;
            Till_comboBox.IsEnabled = false;
            Type_comboBox.IsEnabled = false;
            _accountId = -1;
            _accountType = _projectType = string.Empty;
            _tillId = -1;
            _tillType = _tillTypeForSave = string.Empty;
            ResetOperationControls();
            ClearCommonEntryFields();
            SetCommonInputsEnabled(false);
            _isResetting = false;
        }

        private void ResetBelowTill()
        {
            _isResetting = true;
            Type_comboBox.ItemsSource = null;
            Type_comboBox.SelectedIndex = -1;
            Type_comboBox.IsEnabled = false;
            _tillId = -1;
            _tillType = _tillTypeForSave = string.Empty;
            ResetOperationControls();
            ClearCommonEntryFields();
            SetCommonInputsEnabled(false);
            _isResetting = false;
        }

        private void ResetOperationControls()
        {
            _incomeControl.ResetAll();
            _expensesControl.ResetAll();
            _exchangeControl.ResetAll();
            DynamicOperationContainer.Content = null;
        }

        private void ClearCommonEntryFields()
        {
            Amount_txtBox.Clear();
            Notes_txtBox.Clear();
            E_Notes_txtBox.Clear();
            _attachmentPaths = new string[0];
            AddedAttachments_lable.Text = "Count: 0";
            if (DynamicOperationContainer.Content == _exchangeControl)
                _exchangeControl.UpdateExchangedValue(string.Empty);
        }

        private void ClearAfterSave()
        {
            ClearCommonEntryFields();
            ActionDate_datepicker.SelectedDate = DateTime.Today;

            if (DynamicOperationContainer.Content == _incomeControl)
                _incomeControl.ClearSaveFields();
            else if (DynamicOperationContainer.Content == _expensesControl)
                _expensesControl.ClearSaveFields();
            else if (DynamicOperationContainer.Content == _exchangeControl)
                _exchangeControl.ClearSaveFields();
        }

        private AddActionSaveContext BuildSaveContext()
        {
            return new AddActionSaveContext
            {
                ActionId = -1,
                OperationTypeText = Convert.ToString(Type_comboBox.SelectedValue),
                // Save rules key off Account_Type values used by original Add Action ("Project", "Exchange", "Personal").
                AccountType = (IsBudgetProject || IsMicroProject) ? "Project" : _accountType,
                TillType = _tillTypeForSave,
                AccountId = _accountId,
                TillId = _tillId,
                SubprojectId = _subprojectId,
                AmountText = Amount_txtBox.Text,
                ActionDate = ActionDate_datepicker.SelectedDate,
                Notes = Notes_txtBox.Text,
                ENotes = E_Notes_txtBox.Text,
                Operation = BuildOperationFields(),
                CorrespondingTills = _correspondingTills,
                Tills = _tills,
                AttachmentPaths = _attachmentPaths,
                SourceFormName = "AddAction_Window",
                ActionModel = new Models.Action(),
                RateModel = _rate,
                RequireExpenseCurrencySelection =
                    (IsBudgetProject || IsMicroProject) && IsSyrTill(_tillType),
                FocusCurrencySelection = () => _expensesControl.FocusCurrency()
            };
        }

        private AddActionOperationFields BuildOperationFields()
        {
            var fields = new AddActionOperationFields();
            string operation = Convert.ToString(Type_comboBox.SelectedValue);

            if (operation == "Income")
            {
                fields.DonorRequired = _incomeControl.DonorIsEnabled;
                fields.DonorSelectedIndex = _incomeControl.DonorSelectedIndex;
                fields.DonorId = _incomeControl.DonorId;
                // Personal Income: Cash Till mandatory only for SYR; currency comes from the Till.
                fields.CashAccountRequired = _incomeControl.CashAccountIsEnabled && IsSyrTill(_tillType);
                fields.CashAccountSelectedIndex = _incomeControl.CashAccountSelectedIndex;
                fields.CashAccountId = _incomeControl.CashAccountId;
                if (IsPersonalAccount)
                    fields.SelectedCurrencyId = GetTillTypeId(_tillType);
            }
            else if (operation == "Expenses")
            {
                // Personal: currency ComboBox is disabled; use the Till currency automatically.
                fields.SelectedCurrencyId = IsPersonalAccount
                    ? GetTillTypeId(_tillType)
                    : _expensesControl.SelectedCurrencyId;
                // Personal Expenses: Cash Till required only for SYR (USD/EUR optional).
                fields.CashAccountRequired = IsPersonalAccount
                    ? (_expensesControl.CashAccountIsEnabled && IsSyrTill(_tillType))
                    : _expensesControl.CashAccountIsEnabled;
                fields.CashAccountSelectedIndex = _expensesControl.CashAccountSelectedIndex;
                fields.CashAccountId = _expensesControl.CashAccountId;
                fields.MicroProjectRequired = _expensesControl.MicroProjectIsEnabled;
                fields.MicroProjectText = _expensesControl.MicroProjectText;
                fields.BeneficiaryNameText = _expensesControl.BeneficiaryNameText;
                fields.BudgetLineRequired = _expensesControl.BudgetLineIsEnabled;
                fields.BudgetLineSelectedIndex = _expensesControl.BudgetLineSelectedIndex;
                fields.BudgetLineId = _expensesControl.BudgetLineId;
                fields.DocumentTypeRequired = _expensesControl.DocumentTypeIsEnabled;
                fields.DocumentTypeSelectedIndex = _expensesControl.DocumentTypeSelectedIndex;
                fields.DocumentTypeItemText = _expensesControl.DocumentTypeItemText;
                fields.DocumentNumberRequired = _expensesControl.DocumentNumberIsEnabled;
                fields.DocumentNumber = _expensesControl.DocumentNumber;
                fields.EmployeeRequired = _expensesControl.EmployeeIsEnabled;
                fields.EmployeeSelectedIndex = _expensesControl.EmployeeSelectedIndex;
                fields.EmployeeSalaryId = _expensesControl.EmployeeSalaryId;
                fields.OriginalAvgRateRequired = _expensesControl.OriginalAvgRateIsEnabled;
                fields.OriginalAvgRateSelectedIndex = _expensesControl.OriginalAvgRateSelectedIndex;
                fields.OriginalRateId = _expensesControl.OriginalRateId;
            }
            else if (operation == "Exchange")
            {
                bool correspondingIsSyr = IsSyrTill(_exchangeControl.SelectedCorrespondingTillType);

                if (IsExchangeAccount)
                {
                    // Exchange Account: enabled controls stay selectable; only SYR sides are mandatory on Save.
                    fields.CashAccountRequired = IsSyrTill(_tillType);
                    fields.ObserveCashAccountRequired = correspondingIsSyr;
                    fields.ExchangeAccountEnabled = false;
                    fields.ExchangeCashRequired = false;
                }
                else
                {
                    // Budget/Micro Project: source Cash Till remains optional. SYR destinations
                    // require the corresponding Cash Till and, when an Exchange Account is used,
                    // its Exchange Cash Till, matching the original save rules.
                    fields.CashAccountRequired = false;
                    fields.ObserveCashAccountRequired = correspondingIsSyr;
                    fields.ExchangeAccountEnabled = _exchangeControl.ExchangeAccountIsEnabled;
                    fields.ExchangeCashRequired = correspondingIsSyr;
                }

                fields.CashAccountSelectedIndex = _exchangeControl.CashAccountSelectedIndex;
                fields.CashAccountId = _exchangeControl.CashAccountId;
                fields.DonorRequired = _exchangeControl.DonorIsEnabled;
                fields.DonorSelectedIndex = _exchangeControl.DonorSelectedIndex;
                fields.DonorId = _exchangeControl.DonorId;
                fields.OriginalAvgRateRequired = _exchangeControl.OriginalAvgRateIsEnabled;
                fields.OriginalAvgRateSelectedIndex = _exchangeControl.OriginalAvgRateSelectedIndex;
                fields.OriginalRateId = _exchangeControl.OriginalRateId;
                fields.RateRequired = _exchangeControl.RateIsEnabled;
                fields.RateText = _exchangeControl.RateText;
                fields.DestinationTillRequired = _exchangeControl.DestinationTillIsEnabled;
                fields.DestinationTillSelectedIndex = _exchangeControl.DestinationTillSelectedIndex;
                fields.DestinationTillId = _exchangeControl.DestinationTillId;
                fields.DestinationTillDisplayText = _exchangeControl.DestinationTillDisplayText;
                fields.CorrespondingTillType = _exchangeControl.SelectedCorrespondingTillType;
                fields.ObserveCashAccountSelectedIndex = _exchangeControl.ObserveCashAccountSelectedIndex;
                fields.ObserveCashAccountId = _exchangeControl.ObserveCashAccountId;
                fields.ExchangeAccountSelectedIndex = _exchangeControl.ExchangeAccountSelectedIndex;
                fields.ExchangeAccountId = _exchangeControl.ExchangeAccountId;
                fields.ExchangeCashSelectedIndex = _exchangeControl.ExchangeCashSelectedIndex;
                fields.ExchangeCashId = _exchangeControl.ExchangeCashId;
            }

            return fields;
        }

        private void SetCommonInputsEnabled(bool enabled)
        {
            Amount_Label.IsEnabled = Amount_txtBox.IsEnabled = enabled;
            ActionDate_Label.IsEnabled = ActionDate_datepicker.IsEnabled = enabled;
            Attachment_lable.IsEnabled = AddedAttachments_panel.IsEnabled = enabled;
            Notes_Label.IsEnabled = Notes_txtBox.IsEnabled = enabled;
            E_Notes_Label.IsEnabled = E_Notes_txtBox.IsEnabled = enabled;
            Save_btn.IsEnabled = enabled;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => FillMainComboBoxes();
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Amount_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DynamicOperationContainer.Content == _exchangeControl)
                _exchangeControl.UpdateExchangedValue(Amount_txtBox.Text);
        }
        private void ActionDate_datepicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DynamicOperationContainer.Content == _expensesControl)
                RefreshExpenseDateDependentData();
            else if (DynamicOperationContainer.Content == _exchangeControl && !IsExchangeAccount)
            {
                string correspondingType = (_exchangeControl.SelectedCorrespondingTillType ?? string.Empty).Trim().ToUpperInvariant();
                if (UC_ExchangeOperation.ShouldEnableOriginAvgRate(_tillType, correspondingType))
                    _exchangeControl.SetAverageRates(GetExchangeAverageRates(_tillType, correspondingType)?.DefaultView);
                else
                    _exchangeControl.SetAverageRates(null);
            }
        }
        private void AddedAttachments_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            _attachmentPaths = new string[0];
            AddedAttachments_lable.Text = "Count: 0";
        }

        private void Add_Attachment_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _attachmentDialogThread = new Thread(ShowAttachmentDialog);
                _attachmentDialogThread.SetApartmentState(ApartmentState.STA);
                _attachmentDialogThread.Start();
                _attachmentDialogThread.Join();

                AddedAttachments_lable.Text = _attachmentPaths.Length > 0
                    ? "Count: " + _attachmentPaths.Length.ToString()
                    : "Count: 0";
            }
            catch (Exception ex)
            {
                ShowSaveError(ex);
            }
        }

        private void ShowAttachmentDialog()
        {
            _attachmentDialog = new OpenFileDialog { Multiselect = true };
            if (_attachmentDialog.ShowDialog() == true)
                _attachmentPaths = _attachmentDialog.FileNames;
            else
                _attachmentPaths = new string[0];
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Type_comboBox.SelectedIndex == -1 || string.IsNullOrWhiteSpace(Convert.ToString(Type_comboBox.SelectedValue)))
                {
                    MessageBox.Show("Please select the operation type.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                var result = AddActionSaveService.Execute(BuildSaveContext());
                if (result.ValidationFailed)
                    return;

                if (result.Success)
                    ClearAfterSave();
            }
            catch (Exception ex)
            {
                ShowSaveError(ex);
            }
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }

        private static void ShowSaveError(Exception ex)
        {
            if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                Login.MyConn.Close();

            if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                MessageBox.Show("please check your network and try again", "Error");
            else if (ex.Message.Contains("host"))
                MessageBox.Show("please check the server or your network connection and try again", "Error");
            else if (ex.Message.Contains("Duplicate entry"))
                MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
            else
                MessageBox.Show(ex.Message, "Error");
        }

        private sealed class OperationOption
        {
            public OperationOption(string value) { Text = Value = value; }
            public string Text { get; }
            public string Value { get; }
        }
    }
}
