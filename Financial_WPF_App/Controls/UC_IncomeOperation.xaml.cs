using System.Windows.Controls;

namespace Financial_WPF_App.Controls
{
    public partial class UC_IncomeOperation : UserControl
    {
        private bool _isConfiguring;

        public UC_IncomeOperation()
        {
            InitializeComponent();
            SetBudgetMode();
        }

        private void Donor_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void Donor_comboBox_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Donor_comboBox.SelectedIndex = -1;
        }

        private void CashAccount_clear_btn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CashAccount_comboBox.SelectedIndex = -1;
        }

        public void Configure(object donors)
        {
            _isConfiguring = true;
            SetBudgetMode();
            Donor_comboBox.ItemsSource = donors as System.Collections.IEnumerable;
            Donor_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
        }

        public void ConfigureForPersonal(object cashTills, bool enableCashTill)
        {
            _isConfiguring = true;
            SetPersonalMode(enableCashTill);
            CashAccount_comboBox.ItemsSource = cashTills as System.Collections.IEnumerable;
            CashAccount_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
        }

        public void ResetAll()
        {
            _isConfiguring = true;
            Donor_comboBox.SelectedIndex = -1;
            CashAccount_comboBox.SelectedIndex = -1;
            Donor_lable.IsEnabled = Donor_panel.IsEnabled = Donor_comboBox_clear_btn.IsEnabled = false;
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = false;
            _isConfiguring = false;
        }

        private void SetBudgetMode()
        {
            Donor_lable.IsEnabled = Donor_panel.IsEnabled = Donor_comboBox_clear_btn.IsEnabled = true;
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = false;
            CashAccount_comboBox.ItemsSource = null;
            CashAccount_comboBox.SelectedIndex = -1;
        }

        private void SetPersonalMode(bool enableCashTill)
        {
            Donor_comboBox.ItemsSource = null;
            Donor_comboBox.SelectedIndex = -1;
            Donor_lable.IsEnabled = Donor_panel.IsEnabled = Donor_comboBox_clear_btn.IsEnabled = false;
            CashAccount_lable.IsEnabled = CashAccount_panel.IsEnabled = CashAccount_clear_btn.IsEnabled = enableCashTill;
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

        public void ClearSaveFields()
        {
            _isConfiguring = true;
            Donor_comboBox.SelectedIndex = -1;
            CashAccount_comboBox.SelectedIndex = -1;
            _isConfiguring = false;
        }
    }
}
