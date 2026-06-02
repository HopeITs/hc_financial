using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Financial_WPF_App.Controls;


namespace Financial_WPF_App.Views
{
    public partial class AddAction_Window2 : Window
    {
        // ================= CONTROLS =================

        private readonly UC_IncomeOperation _incomeControl = new UC_IncomeOperation();
        private readonly UC_ExpensesOperation _expensesControl = new UC_ExpensesOperation();
        private readonly UC_ExchangeOperation _exchangeControl = new UC_ExchangeOperation();
        private readonly UC_TransferOperation _transferControl = new UC_TransferOperation();

        public AddAction_Window2(Main_Window_2 main_Window)
        {
            InitializeComponent();

            InitializeWindow();
        }

        // ================= INITIALIZE =================

        private void InitializeWindow()
        {
            ActionDate_datepicker.SelectedDate = System.DateTime.Today;

            ConfigureAvailableOperations();
        }

        // ================= OPERATION SELECTION =================

        private void Type_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Type_comboBox.SelectedItem == null)
                return;

            ComboBoxItem selectedItem = Type_comboBox.SelectedItem as ComboBoxItem;

            if (selectedItem == null)
                return;

            string operationType = selectedItem.Content.ToString();

            switch (operationType)
            {
                case "Income":
                    ShowIncomeOperation();
                    break;

                case "Expenses":
                    ShowExpensesOperation();
                    break;

                case "Exchange":
                    ShowExchangeOperation();
                    break;

                case "Transfer":
                    ShowTransferOperation();
                    break;

                default:
                    DynamicOperationContainer.Content = null;
                    break;
            }
        }

        // ================= SHOW OPERATIONS =================

        private void ShowOperation(UserControl control)
        {
            DynamicOperationContainer.Content = control;
        }

        private void ShowIncomeOperation()
        {
            ShowOperation(_incomeControl);
        }

        private void ShowExpensesOperation()
        {
            ShowOperation(_expensesControl);
        }

        private void ShowExchangeOperation()
        {
            ShowOperation(_exchangeControl);
        }

        private void ShowTransferOperation()
        {
            ShowOperation(_transferControl);
        }

        // ================= CONFIGURATION =================

        private void ConfigureAvailableOperations()
        {
            // لاحقاً تضيف منطق العملات هون
        }

        // ================= EVENTS =================

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Subproject_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Account_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Till_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Amount_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void ActionDate_datepicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddedAttachments_clear_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Add_Attachment_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
