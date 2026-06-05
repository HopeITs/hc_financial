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
    /// Interaction logic for UC_TransferOperation.xaml
    /// </summary>
    public partial class UC_TransferOperation : UserControl
    {
        public UC_TransferOperation()
        {
            InitializeComponent();
        }

        public void FillDestinationProjects(DataView destinationData)
        {
            if (DestinationProject_comboBox != null)
            {
                DestinationProject_comboBox.ItemsSource = destinationData;
                DestinationProject_comboBox.DisplayMemberPath = "Display_Name";
                DestinationProject_comboBox.SelectedValuePath = "Subproject_ID";
            }
        }

        private void CashAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            CashAccount_comboBox.SelectedItem = null;
        }

        private void ObserveCashAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            // Clear the selection of the ObserveCashAccount_comboBox
            ObserveCashAccount_comboBox.SelectedIndex = -1;
        }

        private void DestinationTill_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            // Clear the selection of the DestinationTill_comboBox
            DestinationTill_comboBox.SelectedIndex = -1;
        }

        private void DestinationProjectl_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // TODO: Implement logic for when the DestinationProject_comboBox selection changes
        }

        private void DestinationTill_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // TODO: Add your logic here for when the selection changes in DestinationTill_comboBox
        }
    }
}
