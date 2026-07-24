using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Financial_WPF_App.Controls
{
    /// <summary>
    /// Interaction logic for UC_ExpensesOperation.xaml
    /// </summary>
    public partial class UC_ExpensesOperation : UserControl
    {
        private System.Data.DataTable _budgetLines;
        private System.Data.DataTable _beneficiaries;
        private System.Data.DataTable _employees;
        private bool _isConfiguring;
        private bool _isMicroProject;
        private bool _isPersonalMode;
        private bool _requireProjectFieldsWithoutCurrency;
        private DateTime? _actionDate;
        private string _amountText = string.Empty;
        private double _expandature;
        private double _usdValue;
        private DataTable _rateTable;
        public event EventHandler CurrencyChanged;
        public event EventHandler BudgetChanged;

        public UC_ExpensesOperation()
        {
            InitializeComponent();
        }

        private void CashAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            CashAccount_comboBox.SelectedItem = null;
        }

        private void currency_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isConfiguring || _isPersonalMode)
                return;

            ClearCurrencyDependentTextFields();
            var selectedRow = currency_comboBox.SelectedItem as DataRowView;
            string selectedCurrency = selectedRow == null ? string.Empty : Convert.ToString(selectedRow["Name"]);
            bool hasCurrency = selectedRow != null;
            bool isSyr = string.Equals(selectedCurrency, "SYR", StringComparison.OrdinalIgnoreCase);
            SetExpenseInputsEnabled(hasCurrency);
            SetOriginAverageRateEnabled(isSyr);
            CurrencyChanged?.Invoke(this, EventArgs.Empty);
        }

        public int SelectedCurrencyId
        {
            get
            {
                int id;
                return currency_comboBox.SelectedValue != null && int.TryParse(currency_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public void Configure(bool isMicroProject, object currencies, System.Data.DataTable budgetLines, object cashTills,
            object averageRates, System.Data.DataTable beneficiaries, System.Data.DataTable employees,
            DateTime? actionDate, bool requireProjectFieldsWithoutCurrency)
        {
            _isConfiguring = true;
            _isPersonalMode = false;
            _isMicroProject = isMicroProject;
            _beneficiaries = beneficiaries;
            _employees = employees;
            _actionDate = actionDate;
            _requireProjectFieldsWithoutCurrency = requireProjectFieldsWithoutCurrency;
            currency_comboBox.ItemsSource = currencies as System.Collections.IEnumerable;
            _budgetLines = budgetLines;
            Budget_comboBox.ItemsSource = null;
            Category_comboBox.ItemsSource = null;
            Line_comboBox.ItemsSource = null;
            if (_budgetLines != null && _budgetLines.Rows.Count > 0)
                Budget_comboBox.ItemsSource = new System.Data.DataView(_budgetLines).ToTable(true, "budget_ID", "budget_Name").DefaultView;
            CashAccount_comboBox.ItemsSource = cashTills as System.Collections.IEnumerable;
            Original_AvgRate_comboBox.ItemsSource = averageRates as System.Collections.IEnumerable;
            _rateTable = (averageRates as DataView)?.Table;

            var microProjectNumbers = new List<string>();
            var beneficiaryNames = new List<string>();
            if (_beneficiaries != null && _beneficiaries.Rows.Count > 0)
            {
                for (int i = 0; i < _beneficiaries.Rows.Count; i++)
                {
                    if (_beneficiaries.Rows[i]["MicroProject_ID"] != DBNull.Value)
                        microProjectNumbers.Add(_beneficiaries.Rows[i].Field<int>("MicroProject_ID").ToString());
                    if (_beneficiaries.Rows[i]["Name"] != DBNull.Value)
                        beneficiaryNames.Add(_beneficiaries.Rows[i].Field<string>("Name"));
                }
            }

            MircoProject_ID_autoSuggestionUserControl.AutoSuggestionList = microProjectNumbers;
            BeneficiaryName_txtBox.AutoSuggestionList = beneficiaryNames;

            ResetSelectionsAndText();
            SetCurrencyInputsEnabled(true);
            SetExpenseInputsEnabled(false);
            SetOriginAverageRateEnabled(false);
            payslip_ComboBoxItem.Visibility = !_isMicroProject ? Visibility.Visible : Visibility.Collapsed;
            SetEmployeeInputsEnabled(false);
            USD_Value_lable.IsEnabled = USD_Value_TxtBox.IsEnabled = true;
            USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Visible;
            _isConfiguring = false;
        }

        public void ConfigureForPersonal(bool enableCashTill, object cashTills)
        {
            _isConfiguring = true;
            _isPersonalMode = true;
            _isMicroProject = false;
            _beneficiaries = null;
            _employees = null;
            _requireProjectFieldsWithoutCurrency = false;

            ResetSelectionsAndText();

            currency_comboBox.ItemsSource = null;
            SetCurrencyInputsEnabled(false);

            Budget_lable.IsEnabled = Budget_panel.IsEnabled = Budget_help.IsEnabled = false;
            Category_lable.IsEnabled = Category_panel.IsEnabled = false;
            Line_lable.IsEnabled = Line_panel.IsEnabled = false;
            MircoProject_ID_lable.IsEnabled = MircoProject_ID_autoSuggestionUserControl.IsEnabled = false;
            BeneficiaryName_lable.IsEnabled = BeneficiaryName_txtBox.IsEnabled = false;
            Original_AvgRate_lable.IsEnabled = Original_AvgRate_comboBox.IsEnabled =
                Original_AvgRate_clear_btn.IsEnabled = Original_AvgRate_help.IsEnabled = false;
            USD_Value_lable.IsEnabled = USD_Value_TxtBox.IsEnabled = false;
            USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Collapsed;

            CashAccount_comboBox.ItemsSource = cashTills as System.Collections.IEnumerable;
            CashAccount_comboBox.SelectedIndex = -1;
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = enableCashTill;

            Document_Label.IsEnabled = Document_panel.IsEnabled = true;
            payslip_ComboBoxItem.Visibility = Visibility.Collapsed;
            IsReceipt_comboBox.SelectedIndex = -1;
            ReceiptNum_txtBox.Clear();
            SetEmployeeInputsEnabled(false);

            _isConfiguring = false;
        }

        public void SetCashTills(object cashTills)
        {
            SetItemsSourcePreservingSelection(CashAccount_comboBox, cashTills);
        }

        public void SetAverageRates(object averageRates)
        {
            _rateTable = (averageRates as DataView)?.Table;
            SetItemsSourcePreservingSelection(Original_AvgRate_comboBox, averageRates);
            CalcUsdValue();
        }

        public void UpdateValue(string amountText)
        {
            _amountText = amountText ?? string.Empty;
            CalcUsdValue();
        }

        public void ResetAll()
        {
            _isConfiguring = true;
            _isPersonalMode = false;
            ResetSelectionsAndText();
            SetAllControlsEnabled(false);
            _isConfiguring = false;
            USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Collapsed;
        }

        private void SetAllControlsEnabled(bool enabled)
        {
            SetCurrencyInputsEnabled(enabled);
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = enabled;
            Document_Label.IsEnabled = Document_panel.IsEnabled = enabled;
            Budget_lable.IsEnabled = Budget_panel.IsEnabled = Budget_help.IsEnabled = enabled;
            Category_lable.IsEnabled = Category_panel.IsEnabled = enabled;
            Line_lable.IsEnabled = Line_panel.IsEnabled = enabled;
            MircoProject_ID_lable.IsEnabled = MircoProject_ID_autoSuggestionUserControl.IsEnabled = enabled;
            BeneficiaryName_lable.IsEnabled = BeneficiaryName_txtBox.IsEnabled = enabled;
            Original_AvgRate_lable.IsEnabled = Original_AvgRate_comboBox.IsEnabled =
                Original_AvgRate_clear_btn.IsEnabled = Original_AvgRate_help.IsEnabled = enabled;
            Employee_lable.IsEnabled = Employee_panel.IsEnabled = Employee_comboBox_clear.IsEnabled =
                Employee_help.IsEnabled = enabled;
            USD_Value_lable.IsEnabled = USD_Value_TxtBox.IsEnabled = enabled;
        }

        private void SetExpenseInputsEnabled(bool currencySelected)
        {
            bool requiredInputsEnabled = currencySelected || _requireProjectFieldsWithoutCurrency;
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = requiredInputsEnabled;
            Document_Label.IsEnabled = Document_panel.IsEnabled = requiredInputsEnabled;

            bool budgetEnabled = requiredInputsEnabled && !_isMicroProject;
            Budget_lable.IsEnabled = Budget_panel.IsEnabled = Budget_help.IsEnabled = budgetEnabled;
            Category_lable.IsEnabled = Category_panel.IsEnabled = budgetEnabled && Budget_comboBox.SelectedValue != null;
            Line_lable.IsEnabled = Line_panel.IsEnabled = budgetEnabled && Category_comboBox.SelectedValue != null;

            bool microEnabled = requiredInputsEnabled && _isMicroProject;
            MircoProject_ID_lable.IsEnabled = MircoProject_ID_autoSuggestionUserControl.IsEnabled = microEnabled;
            BeneficiaryName_lable.IsEnabled = BeneficiaryName_txtBox.IsEnabled = microEnabled;
        }

        private void ClearCurrencyDependents()
        {
            _isConfiguring = true;
            CashAccount_comboBox.SelectedIndex = -1;
            Original_AvgRate_comboBox.SelectedIndex = -1;
            Budget_comboBox.SelectedIndex = -1;
            Category_comboBox.SelectedIndex = -1;
            Line_comboBox.SelectedIndex = -1;
            IsReceipt_comboBox.SelectedIndex = -1;
            ReceiptNum_txtBox.Clear();
            Employee_comboBox.SelectedIndex = -1;
            Employee_comboBox.ItemsSource = null;
            SetEmployeeInputsEnabled(false);
            MircoProject_ID_autoSuggestionUserControl.autoTextBox.Clear();
            BeneficiaryName_txtBox.autoTextBox.Clear();
            USD_Value_TxtBox.Content = string.Empty;
            Category_comboBox.ItemsSource = null;
            Line_comboBox.ItemsSource = null;
            _isConfiguring = false;
        }

        private void ClearCurrencyDependentTextFields()
        {
            MircoProject_ID_autoSuggestionUserControl.autoTextBox.Clear();
            BeneficiaryName_txtBox.autoTextBox.Clear();
            USD_Value_TxtBox.Content = string.Empty;
        }

        private void SetCurrencyInputsEnabled(bool enabled)
        {
            Currency_Label.IsEnabled = enabled;
            currency_comboBox.IsEnabled = enabled;
        }

        private static void SetItemsSourcePreservingSelection(ComboBox comboBox, object itemsSource)
        {
            object selectedValue = comboBox.SelectedValue;
            comboBox.ItemsSource = itemsSource as System.Collections.IEnumerable;

            if (selectedValue == null)
            {
                comboBox.SelectedIndex = -1;
                return;
            }

            comboBox.SelectedValue = selectedValue;
            if (comboBox.SelectedItem == null)
                comboBox.SelectedIndex = -1;
        }

        private void ResetSelectionsAndText()
        {
            currency_comboBox.SelectedIndex = -1;
            ClearCurrencyDependents();
        }

        private void SetOriginAverageRateEnabled(bool enabled)
        {
            Original_AvgRate_lable.IsEnabled = Original_AvgRate_comboBox.IsEnabled =
                Original_AvgRate_clear_btn.IsEnabled = Original_AvgRate_help.IsEnabled = enabled;
            if (!enabled)
                Original_AvgRate_comboBox.SelectedIndex = -1;
        }

        private void MircoProject_ID_autoSuggestionUserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text == "")
                {
                    BeneficiaryName_txtBox.autoTextBox.Text = "";
                }
                else if (_beneficiaries != null)
                {
                    string microProjectId = MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text;
                    DataRow[] rows = _beneficiaries.Select("MicroProject_ID=" + microProjectId);
                    if (rows.Length != 0)
                    {
                        BeneficiaryName_txtBox.autoTextBox.Text = rows[0].Field<string>("Name");
                        BeneficiaryName_txtBox.CloseAutoSuggestionBox();
                    }
                    else
                    {
                        BeneficiaryName_txtBox.autoTextBox.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowDataError(ex);
            }
        }

        private void Budget_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isConfiguring)
                return;
            Category_comboBox.ItemsSource = null;
            Line_comboBox.ItemsSource = null;
            Category_comboBox.SelectedIndex = -1;
            Line_comboBox.SelectedIndex = -1;
            Original_AvgRate_comboBox.SelectedIndex = -1;
            USD_Value_TxtBox.Content = string.Empty;
            SetExpenseInputsEnabled(currency_comboBox.SelectedValue != null);
            BudgetChanged?.Invoke(this, EventArgs.Empty);
            if (_budgetLines == null || Budget_comboBox.SelectedValue == null)
                return;

            var rows = _budgetLines.Select("budget_ID = " + Convert.ToInt32(Budget_comboBox.SelectedValue));
            if (rows.Length > 0)
            {
                DataTable categories = rows.CopyToDataTable().DefaultView
                    .ToTable(true, "linecategory_ID", "linecategory_Name");
                Category_comboBox.ItemsSource = categories.DefaultView;
            }
        }

        private void Budget_comboBox_clear_Click(object sender, RoutedEventArgs e)
        {
            Budget_comboBox.SelectedIndex = -1;
        }

        private void Category_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isConfiguring)
                return;
            Line_comboBox.ItemsSource = null;
            Line_comboBox.SelectedIndex = -1;
            SetExpenseInputsEnabled(currency_comboBox.SelectedValue != null);
            if (_budgetLines == null || Budget_comboBox.SelectedValue == null || Category_comboBox.SelectedValue == null)
                return;

            var rows = _budgetLines.Select("budget_ID = " + Convert.ToInt32(Budget_comboBox.SelectedValue) +
                " AND linecategory_ID = " + Convert.ToInt32(Category_comboBox.SelectedValue));
            if (rows.Length > 0)
                Line_comboBox.ItemsSource = rows.CopyToDataTable().DefaultView;
        }

        private void Category_comboBox_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            Category_comboBox.SelectedIndex = -1;
        }

        private void Line_comboBox_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            Line_comboBox.SelectedItem = null;
        }

        private void BeneficiaryName_autoSuggestionUserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BeneficiaryName_txtBox.autoTextBox.Text == "")
                {
                    MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = "";
                }
                else if (_beneficiaries != null)
                {
                    string name = BeneficiaryName_txtBox.autoTextBox.Text;
                    DataRow[] rows = _beneficiaries.Select("Name like '%" + name + "%'");
                    if (rows.Length != 0)
                    {
                        MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = rows[0].Field<int>("MicroProject_ID").ToString();
                        MircoProject_ID_autoSuggestionUserControl.CloseAutoSuggestionBox();
                    }
                    else
                    {
                        MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowDataError(ex);
            }
        }

        private static void ShowDataError(Exception ex)
        {
            if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                MessageBox.Show("please check your network and try again", "Error");
            else if (ex.Message.Contains("host"))
                MessageBox.Show("please check the server or your network connection and try again", "Error");
            else if (ex.Message.Contains("Duplicate entry"))
                MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
            else
                MessageBox.Show(ex.Message, "Error");
        }

        private void IsReceipt_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isConfiguring)
                return;

            ReceiptNum_txtBox.Clear();
            RefreshEmployeeItems();
        }

        private void Employee_comboBox_clear_Click(object sender, RoutedEventArgs e)
        {
            Employee_comboBox.SelectedIndex = -1;
        }

        public void SetActionDate(DateTime? actionDate)
        {
            _actionDate = actionDate;
            RefreshEmployeeItems();
        }

        private void RefreshEmployeeItems()
        {
            Employee_comboBox.ItemsSource = null;
            Employee_comboBox.SelectedIndex = -1;

            bool isPayslip = IsReceipt_comboBox.SelectedItem != null &&
                IsReceipt_comboBox.SelectedItem.ToString().Contains("payslip");
            SetEmployeeInputsEnabled(isPayslip);
            if (!isPayslip || !_actionDate.HasValue || _employees == null || _employees.Rows.Count == 0)
                return;

            string month = _actionDate.Value.ToString("yyyy/MM");
            DataRow[] rows = _employees.AsEnumerable()
                .Where(row => Convert.ToDateTime(row["Month"]).ToString("yyyy/MM") == month)
                .ToArray();
            Employee_comboBox.ItemsSource = rows.Length == 0
                ? _employees.Clone().DefaultView
                : rows.CopyToDataTable().DefaultView;
        }

        private void SetEmployeeInputsEnabled(bool enabled)
        {
            Employee_lable.IsEnabled = Employee_panel.IsEnabled = Employee_comboBox_clear.IsEnabled =
                Employee_help.IsEnabled = enabled;
            if (!enabled)
            {
                Employee_comboBox.ItemsSource = null;
                Employee_comboBox.SelectedIndex = -1;
            }
        }

        private void ReceiptNum_txtBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // TODO: Implement document number tracking or validation logic
        }

        private void Original_AvgRate_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!_isConfiguring)
                CalcUsdValue();
        }

        private void CalcUsdValue()
        {
            if (_amountText != "" && Original_AvgRate_comboBox.SelectedIndex != -1 && _rateTable != null)
            {
                int rateId = Convert.ToInt32(Original_AvgRate_comboBox.SelectedValue.ToString());
                DataRow[] rows = _rateTable.Select("ID=" + rateId);
                if (rows.Length > 0)
                {
                    double avgRate = rows[0].Field<double>("Value");
                    double.TryParse(_amountText, out _expandature);
                    _usdValue = _expandature / avgRate;
                    USD_Value_TxtBox.Content = _usdValue.ToString();
                }
            }
            else
            {
                _usdValue = 0;
                USD_Value_TxtBox.Content = "";
            }
        }

        private void Avgrate_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Original_AvgRate_comboBox.SelectedIndex = -1;
        }

        public bool CurrencySelectionEnabled => currency_comboBox.IsEnabled;

        public bool CashAccountIsEnabled => CashAccount_lable.IsEnabled;

        public int CashAccountSelectedIndex => CashAccount_comboBox.SelectedIndex;

        public int CashAccountId
        {
            get
            {
                int id;
                return CashAccount_comboBox.SelectedValue != null && int.TryParse(CashAccount_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public bool MicroProjectIsEnabled => MircoProject_ID_lable.IsEnabled;

        public string MicroProjectText => MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text;

        public string BeneficiaryNameText => BeneficiaryName_txtBox.autoTextBox.Text;

        public bool BudgetLineIsEnabled => Budget_lable.IsEnabled;

        public int BudgetLineSelectedIndex => Line_comboBox.SelectedIndex;

        public int BudgetLineId
        {
            get
            {
                int id;
                return Line_comboBox.SelectedValue != null && int.TryParse(Line_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public int SelectedBudgetTypeId
        {
            get
            {
                if (_budgetLines == null || Budget_comboBox.SelectedValue == null)
                    return -1;
                DataRow[] rows = _budgetLines.Select("budget_ID = " + Convert.ToInt32(Budget_comboBox.SelectedValue));
                return rows.Length > 0 && rows[0]["BudgetType_ID"] != DBNull.Value
                    ? Convert.ToInt32(rows[0]["BudgetType_ID"])
                    : -1;
            }
        }

        public bool DocumentTypeIsEnabled => Document_Label.IsEnabled;

        public int DocumentTypeSelectedIndex => IsReceipt_comboBox.SelectedIndex;

        public string DocumentTypeItemText
        {
            get
            {
                if (IsReceipt_comboBox.SelectedItem == null)
                    return string.Empty;
                return IsReceipt_comboBox.SelectedItem.ToString();
            }
        }

        public bool DocumentNumberIsEnabled => ReceiptNum_txtBox.IsEnabled;

        public string DocumentNumber => ReceiptNum_txtBox.Text;

        public bool EmployeeIsEnabled => Employee_lable.IsEnabled;

        public int EmployeeSelectedIndex => Employee_comboBox.SelectedIndex;

        public int EmployeeSalaryId
        {
            get
            {
                int id;
                return Employee_comboBox.SelectedValue != null &&
                    int.TryParse(Employee_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public bool OriginalAvgRateIsEnabled => Original_AvgRate_lable.IsEnabled;

        public int OriginalAvgRateSelectedIndex => Original_AvgRate_comboBox.SelectedIndex;

        public int OriginalRateId
        {
            get
            {
                int id;
                return Original_AvgRate_comboBox.SelectedValue != null && int.TryParse(Original_AvgRate_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public void LoadEditValues(int currencyId, int cashAccountId, int microProjectId, string beneficiaryName,
            int budgetId, int categoryId, int budgetLineId, string documentType, string documentNumber,
            int employeeSalaryId, int originalRateId)
        {
            if (!_isPersonalMode)
                currency_comboBox.SelectedValue = currencyId > 0 ? (object)currencyId : null;

            CashAccount_comboBox.SelectedValue = cashAccountId > 0 ? (object)cashAccountId : null;

            if (budgetId > 0)
                Budget_comboBox.SelectedValue = budgetId;
            if (categoryId > 0)
                Category_comboBox.SelectedValue = categoryId;
            if (budgetLineId > 0)
                Line_comboBox.SelectedValue = budgetLineId;

            MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = microProjectId > 0
                ? microProjectId.ToString()
                : string.Empty;
            BeneficiaryName_txtBox.autoTextBox.Text = beneficiaryName ?? string.Empty;
            MircoProject_ID_autoSuggestionUserControl.CloseAutoSuggestionBox();
            BeneficiaryName_txtBox.CloseAutoSuggestionBox();

            if (string.Equals(documentType, "invoice", StringComparison.OrdinalIgnoreCase))
                IsReceipt_comboBox.SelectedIndex = 0;
            else if (string.Equals(documentType, "receipt", StringComparison.OrdinalIgnoreCase))
                IsReceipt_comboBox.SelectedIndex = 1;
            else if (string.Equals(documentType, "payslip", StringComparison.OrdinalIgnoreCase))
                IsReceipt_comboBox.SelectedIndex = 2;
            else
                IsReceipt_comboBox.SelectedIndex = -1;

            ReceiptNum_txtBox.Text = documentNumber ?? string.Empty;
            Employee_comboBox.SelectedValue = employeeSalaryId > 0 ? (object)employeeSalaryId : null;
            Original_AvgRate_comboBox.SelectedValue = originalRateId > 0 ? (object)originalRateId : null;
            CalcUsdValue();
        }

        public void ApplyEditLock()
        {
            SetCurrencyInputsEnabled(false);
        }

        public void ClearSaveFields()
        {
            _isConfiguring = true;
            CashAccount_comboBox.SelectedIndex = -1;
            Original_AvgRate_comboBox.SelectedIndex = -1;
            Budget_comboBox.SelectedIndex = -1;
            Category_comboBox.SelectedIndex = -1;
            Line_comboBox.SelectedIndex = -1;
            IsReceipt_comboBox.SelectedIndex = -1;
            ReceiptNum_txtBox.Clear();
            Employee_comboBox.SelectedIndex = -1;
            Employee_comboBox.ItemsSource = null;
            SetEmployeeInputsEnabled(false);
            MircoProject_ID_autoSuggestionUserControl.autoTextBox.Clear();
            BeneficiaryName_txtBox.autoTextBox.Clear();
            USD_Value_TxtBox.Content = string.Empty;
            Category_comboBox.ItemsSource = null;
            Line_comboBox.ItemsSource = null;
            if (_budgetLines != null && _budgetLines.Rows.Count > 0)
                Budget_comboBox.ItemsSource = new System.Data.DataView(_budgetLines).ToTable(true, "budget_ID", "budget_Name").DefaultView;
            _isConfiguring = false;
        }

        public void FocusCurrency()
        {
            currency_comboBox.Focus();
        }
    }
}
