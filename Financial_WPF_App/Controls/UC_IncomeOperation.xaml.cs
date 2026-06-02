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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Financial_WPF_App.Controls
{
    /// <summary>
    /// Interaction logic for UC_IncomeOperation.xaml
    /// </summary>
    public partial class UC_IncomeOperation : UserControl
    {
        public UC_IncomeOperation()
        {
            InitializeComponent();
        }

        private void Donor_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Add your logic here for when the selection changes.
        }

        private void Donor_comboBox_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            // Existing or placeholder logic for clear button click.
        }

        private void CashAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            CashAccount_comboBox.SelectedIndex = -1;
        }
    }
}
