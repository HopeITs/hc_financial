using Financial_WPF_App.Helper;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Financial_WPF_App.Controls
{
    /// <summary>
    /// Interaction logic for UC_ExchangeOperation.xaml
    /// </summary>
    public partial class UC_ExchangeOperation : UserControl
    {
        private string _currentTillType;
        private bool _isConfiguring;
        private bool _isExchangeAccountMode;
        private string _amountText = string.Empty;

        public event EventHandler CorrespondingTillChanged;
        public event EventHandler ExchangeAccountChanged;

        public UC_ExchangeOperation()
        {
            InitializeComponent();
            Rate_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
        }

        private void Original_AvgRate_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // TODO: Recalculate average exchange rates and financial balance adjustments
        }

        private void Avgrate_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Original_AvgRate_comboBox.SelectedIndex = -1;
        }

        private void Donor_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // TODO: Filter project line-items based on selected financial backer/donor allocations
        }

        private void Donor_comboBox_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Donor_comboBox.SelectedIndex = -1;
        }

        private void Rate_txtBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            string value = Rate_txtBox.Text;
            if (string.IsNullOrWhiteSpace(value))
                return;

            double rate;
            if (double.TryParse(value, out rate) && !double.IsNaN(rate) && !double.IsInfinity(rate))
            {
                Rate_txtBox.Text = rate.ToString();
                CalcExchangedValue();
            }
        }

        private void DestinationTill_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isConfiguring)
                return;

            _isConfiguring = true;
            ObserveCashAccount_comboBox.SelectedIndex = -1;
            ExchangeAcc_comboBox.SelectedIndex = -1;
            ExchangeCash_comboBox.SelectedIndex = -1;
            Original_AvgRate_comboBox.SelectedIndex = -1;
            Original_AvgRate_comboBox.ItemsSource = null;
            _isConfiguring = false;
            ApplyCorrespondingTillRules();
            CorrespondingTillChanged?.Invoke(this, EventArgs.Empty);
            CalcExchangedValue();
        }

        private void DestinationTill_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DestinationTill_comboBox.SelectedIndex = -1;
        }

        private void ObserveCashAccount_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ObserveCashAccount_comboBox.SelectedIndex = -1;
        }

        private void ExchangeAcc_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isConfiguring)
                return;

            _isConfiguring = true;
            ExchangeCash_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
            ExchangeAccountChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ExchangeTill_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExchangeAcc_comboBox.SelectedIndex = -1;
        }

        private void ExchangeCash_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExchangeCash_comboBox.SelectedIndex = -1;
        }

        private void CashAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            // Clear the selected value of the CashAccount_comboBox
            CashAccount_comboBox.SelectedIndex = -1;
        }

        public string SelectedCorrespondingTillType
        {
            get
            {
                var row = DestinationTill_comboBox.SelectedItem as System.Data.DataRowView;
                return row == null ? string.Empty : Convert.ToString(row["Type"]);
            }
        }

        public int SelectedCorrespondingTillTypeId
        {
            get
            {
                var row = DestinationTill_comboBox.SelectedItem as System.Data.DataRowView;
                if (row == null)
                    return -1;
                if (row.Row.Table.Columns.Contains("Type_ID") && row["Type_ID"] != null && row["Type_ID"] != DBNull.Value)
                    return Convert.ToInt32(row["Type_ID"]);
                if (row.Row.Table.Columns.Contains("ID") && row["ID"] != null && row["ID"] != DBNull.Value)
                {
                    // Some till lists expose till type id as ID (Select_Account_Missing / currency lists).
                    // Corresponding tills from Till.Select use Type_ID; destination list uses ID as till id.
                }
                return -1;
            }
        }

        public int SelectedExchangeAccountId
        {
            get
            {
                int id;
                return ExchangeAcc_comboBox.SelectedValue != null && int.TryParse(ExchangeAcc_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public void Configure(string currentTillType, bool isExchangeAccountMode, object correspondingTills, object donors,
            object exchangeAccounts, object correspondingCashTills, object exchangeCashTills, object averageRates,
            object mainCashTills = null)
        {
            _isConfiguring = true;
            _isExchangeAccountMode = isExchangeAccountMode;
            _currentTillType = (currentTillType ?? string.Empty).Trim().ToUpperInvariant();
            DestinationTill_comboBox.ItemsSource = correspondingTills as System.Collections.IEnumerable;
            Donor_comboBox.ItemsSource = donors as System.Collections.IEnumerable;
            ExchangeAcc_comboBox.ItemsSource = exchangeAccounts as System.Collections.IEnumerable;
            ObserveCashAccount_comboBox.ItemsSource = correspondingCashTills as System.Collections.IEnumerable;
            ExchangeCash_comboBox.ItemsSource = exchangeCashTills as System.Collections.IEnumerable;
            Original_AvgRate_comboBox.ItemsSource = averageRates as System.Collections.IEnumerable;
            CashAccount_comboBox.ItemsSource = mainCashTills as System.Collections.IEnumerable;

            ResetSelectionsAndText();

            if (_isExchangeAccountMode)
                ConfigureExchangeAccountMode();
            else
                ConfigureBudgetExchangeMode();

            _isConfiguring = false;
            ApplyCorrespondingTillRules();
        }

        private void ConfigureExchangeAccountMode()
        {
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = true;

            Original_AvgRate_lable.IsEnabled = Original_AvgRate_comboBox.IsEnabled =
                Original_AvgRate_clear_btn.IsEnabled = Original_AvgRate_help.IsEnabled = false;
            Donor_lable.IsEnabled = Donor_panel.IsEnabled = Donor_comboBox_clear_btn.IsEnabled = false;
            ExchangeAcc_lable.IsEnabled = ExchangeAcc_panel.IsEnabled = ExchangeAcc_clear_btn.IsEnabled =
                ExchangeAcc_help.IsEnabled = false;
            ExchangeCash_lable.IsEnabled = ExchangeAcc_panel_Copy.IsEnabled = ExchangeCash_clear_btn.IsEnabled = false;

            DestinationTill_lable.IsEnabled = DestinationTill_panel.IsEnabled = DestinationTill_clear_btn.IsEnabled = true;
            Rate_lable.IsEnabled = Rate_txtBox.IsEnabled = true;
            Exchanged_lable.IsEnabled = Exchanged_txtBox.IsEnabled = true;
            DestinationTill_help.IsEnabled = false;
            ExchangeAcc_help.IsEnabled = false;
            Original_AvgRate_help.IsEnabled = false;
        }

        private void ConfigureBudgetExchangeMode()
        {
            // Cash Till remains selectable for Budget/Micro Project exchange, but is optional on save.
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = true;

            // Origin Avg Rate enablement depends on Corresponding Till (see ApplyCorrespondingTillRules).
            Original_AvgRate_lable.IsEnabled = Original_AvgRate_comboBox.IsEnabled =
                Original_AvgRate_clear_btn.IsEnabled = Original_AvgRate_help.IsEnabled = false;

            DestinationTill_lable.IsEnabled = DestinationTill_panel.IsEnabled = DestinationTill_clear_btn.IsEnabled = true;
            Donor_lable.IsEnabled = Donor_panel.IsEnabled = Donor_comboBox_clear_btn.IsEnabled = true;
            Rate_lable.IsEnabled = Rate_txtBox.IsEnabled = true;
            Exchanged_lable.IsEnabled = Exchanged_txtBox.IsEnabled = true;
            DestinationTill_help.IsEnabled = true;
            ExchangeAcc_help.IsEnabled = true;
        }

        public void SetMainCashTills(object cashTills)
        {
            _isConfiguring = true;
            CashAccount_comboBox.ItemsSource = cashTills as System.Collections.IEnumerable;
            CashAccount_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
        }

        public void UpdateExchangedValue(string amountText)
        {
            _amountText = amountText ?? string.Empty;
            CalcExchangedValue();
        }

        private void CalcExchangedValue()
        {
            if (string.IsNullOrWhiteSpace(_amountText) || string.IsNullOrWhiteSpace(Rate_txtBox.Text))
            {
                Exchanged_txtBox.Content = string.Empty;
                return;
            }

            double parsedAmount;
            double parsedRate;
            if (double.TryParse(_amountText, out parsedAmount) && double.TryParse(Rate_txtBox.Text, out parsedRate))
                Exchanged_txtBox.Content = (parsedAmount * parsedRate).ToString();
            else
                Exchanged_txtBox.Content = string.Empty;
        }

        public void SetExchangeAccounts(object exchangeAccounts)
        {
            _isConfiguring = true;
            ExchangeAcc_comboBox.ItemsSource = exchangeAccounts as System.Collections.IEnumerable;
            ExchangeAcc_comboBox.SelectedIndex = -1;
            ExchangeCash_comboBox.ItemsSource = null;
            ExchangeCash_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
            ApplyCorrespondingTillRules();
        }

        public void SetCorrespondingCashTills(object cashTills)
        {
            _isConfiguring = true;
            ObserveCashAccount_comboBox.ItemsSource = cashTills as System.Collections.IEnumerable;
            ObserveCashAccount_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
        }

        public void SetExchangeCashTills(object cashTills)
        {
            _isConfiguring = true;
            ExchangeCash_comboBox.ItemsSource = cashTills as System.Collections.IEnumerable;
            ExchangeCash_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
        }

        public void SetAverageRates(object averageRates)
        {
            _isConfiguring = true;
            Original_AvgRate_comboBox.ItemsSource = averageRates as System.Collections.IEnumerable;
            Original_AvgRate_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
        }

        public void ResetAll()
        {
            _isConfiguring = true;
            _isExchangeAccountMode = false;
            _amountText = string.Empty;
            ResetSelectionsAndText();
            SetAllControlsEnabled(false);
            _isConfiguring = false;
        }

        private void SetAllControlsEnabled(bool enabled)
        {
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = enabled;
            DestinationTill_lable.IsEnabled = DestinationTill_panel.IsEnabled = DestinationTill_clear_btn.IsEnabled = enabled;
            ObserveCashAccount_lable.IsEnabled = ObserveCashAccount_panel.IsEnabled = ObserveCashAccount_clear_btn.IsEnabled = enabled;
            ExchangeAcc_lable.IsEnabled = ExchangeAcc_panel.IsEnabled = ExchangeAcc_clear_btn.IsEnabled = enabled;
            ExchangeAcc_help.IsEnabled = enabled;
            ExchangeCash_lable.IsEnabled = ExchangeAcc_panel_Copy.IsEnabled = ExchangeCash_clear_btn.IsEnabled = enabled;
            Original_AvgRate_lable.IsEnabled = Original_AvgRate_comboBox.IsEnabled =
                Original_AvgRate_clear_btn.IsEnabled = Original_AvgRate_help.IsEnabled = enabled;
            Donor_lable.IsEnabled = Donor_panel.IsEnabled = Donor_comboBox_clear_btn.IsEnabled = enabled;
            Rate_lable.IsEnabled = Rate_txtBox.IsEnabled = enabled;
            Exchanged_lable.IsEnabled = Exchanged_txtBox.IsEnabled = enabled;
            USD_Value_lable.IsEnabled = enabled;
            DestinationTill_help.IsEnabled = enabled;
        }

        private void ResetSelectionsAndText()
        {
            CashAccount_comboBox.SelectedIndex = -1;
            Original_AvgRate_comboBox.SelectedIndex = -1;
            Donor_comboBox.SelectedIndex = -1;
            DestinationTill_comboBox.SelectedIndex = -1;
            ObserveCashAccount_comboBox.SelectedIndex = -1;
            ExchangeAcc_comboBox.SelectedIndex = -1;
            ExchangeCash_comboBox.SelectedIndex = -1;
            Rate_txtBox.Clear();
            USD_Value_TxtBox.Content = string.Empty;
            Exchanged_txtBox.Content = string.Empty;
        }

        private void ApplyCorrespondingTillRules()
        {
            bool hasCorrespondingTill = DestinationTill_comboBox.SelectedValue != null;
            string correspondingType = (SelectedCorrespondingTillType ?? string.Empty).Trim().ToUpperInvariant();

            if (_isExchangeAccountMode)
            {
                // Optional on save for non-SYR; always enabled when a corresponding till is selected
                // so the user can choose a value. Mandatory/optional is enforced in Save mapping.
                ObserveCashAccount_lable.IsEnabled = ObserveCashAccount_panel.IsEnabled =
                    ObserveCashAccount_clear_btn.IsEnabled = hasCorrespondingTill;
                ExchangeAcc_lable.IsEnabled = ExchangeAcc_panel.IsEnabled = ExchangeAcc_clear_btn.IsEnabled = false;
                ExchangeCash_lable.IsEnabled = ExchangeAcc_panel_Copy.IsEnabled = ExchangeCash_clear_btn.IsEnabled = false;

                if (!hasCorrespondingTill)
                    ObserveCashAccount_comboBox.SelectedIndex = -1;
                return;
            }

            // Budget/Micro Project: keep optional fields enabled whenever applicable.
            // Save validation decides what is mandatory vs optional.
            ObserveCashAccount_lable.IsEnabled = ObserveCashAccount_panel.IsEnabled =
                ObserveCashAccount_clear_btn.IsEnabled = hasCorrespondingTill;
            ExchangeAcc_lable.IsEnabled = ExchangeAcc_panel.IsEnabled = ExchangeAcc_clear_btn.IsEnabled =
                hasCorrespondingTill;
            ExchangeCash_lable.IsEnabled = ExchangeAcc_panel_Copy.IsEnabled = ExchangeCash_clear_btn.IsEnabled =
                hasCorrespondingTill;

            // Origin Avg Rate: enable for USD→SYR, USD→EUR, EUR→SYR; keep empty for EUR→USD.
            bool enableOriginAvgRate = hasCorrespondingTill && ShouldEnableOriginAvgRate(_currentTillType, correspondingType);
            Original_AvgRate_lable.IsEnabled = Original_AvgRate_comboBox.IsEnabled =
                Original_AvgRate_clear_btn.IsEnabled = Original_AvgRate_help.IsEnabled = enableOriginAvgRate;

            if (!hasCorrespondingTill)
            {
                ObserveCashAccount_comboBox.SelectedIndex = -1;
                ExchangeCash_comboBox.SelectedIndex = -1;
            }
            if (!enableOriginAvgRate)
            {
                Original_AvgRate_comboBox.SelectedIndex = -1;
                Original_AvgRate_comboBox.ItemsSource = null;
            }
        }

        /// <summary>
        /// Budget/Micro Project Origin Avg Rate pairs that should be available for selection.
        /// EUR→USD remains empty (disabled).
        /// </summary>
        public static bool ShouldEnableOriginAvgRate(string fromTill, string toTill)
        {
            string from = (fromTill ?? string.Empty).Trim().ToUpperInvariant();
            string to = (toTill ?? string.Empty).Trim().ToUpperInvariant();
            if (from == "USD" && (to == "SYR" || to == "EUR"))
                return true;
            if (from == "EUR" && to == "SYR")
                return true;
            return false;
        }

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

        public bool DonorIsEnabled => Donor_lable.IsEnabled;

        public int DonorSelectedIndex => Donor_comboBox.SelectedIndex;

        public int DonorId
        {
            get
            {
                int id;
                return Donor_comboBox.SelectedValue != null && int.TryParse(Donor_comboBox.SelectedValue.ToString(), out id) ? id : -1;
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

        public bool DestinationTillIsEnabled => DestinationTill_lable.IsEnabled;

        public int DestinationTillSelectedIndex => DestinationTill_comboBox.SelectedIndex;

        public int DestinationTillId
        {
            get
            {
                int id;
                return DestinationTill_comboBox.SelectedValue != null && int.TryParse(DestinationTill_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public string DestinationTillDisplayText => DestinationTill_comboBox.Text;

        public bool ObserveCashAccountIsEnabled => ObserveCashAccount_lable.IsEnabled;

        public int ObserveCashAccountSelectedIndex => ObserveCashAccount_comboBox.SelectedIndex;

        public int ObserveCashAccountId
        {
            get
            {
                int id;
                return ObserveCashAccount_comboBox.SelectedValue != null && int.TryParse(ObserveCashAccount_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public bool ExchangeAccountIsEnabled => ExchangeAcc_lable.IsEnabled;

        public int ExchangeAccountSelectedIndex => ExchangeAcc_comboBox.SelectedIndex;

        public int ExchangeAccountId
        {
            get
            {
                int id;
                return ExchangeAcc_comboBox.SelectedValue != null && int.TryParse(ExchangeAcc_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public bool ExchangeCashIsEnabled => ExchangeCash_lable.IsEnabled;

        public int ExchangeCashSelectedIndex => ExchangeCash_comboBox.SelectedIndex;

        public int ExchangeCashId
        {
            get
            {
                int id;
                return ExchangeCash_comboBox.SelectedValue != null && int.TryParse(ExchangeCash_comboBox.SelectedValue.ToString(), out id) ? id : -1;
            }
        }

        public bool RateIsEnabled => Rate_lable.IsEnabled;

        public string RateText => Rate_txtBox.Text;

        public void ClearSaveFields()
        {
            _isConfiguring = true;
            ResetSelectionsAndText();
            _isConfiguring = false;
        }
    }
}
