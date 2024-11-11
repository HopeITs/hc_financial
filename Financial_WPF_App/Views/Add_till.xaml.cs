using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Windows;


namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_till.xaml
    /// </summary>
    public partial class Add_till : Window
    {
        public Add_till(int Account_ID, Account_Window Ac, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Account_ID = Account_ID;
            this.Ac = Ac;
            OnLoad();
        }

        int Account_ID;
        DataTable dt;
        private Till t;
        Account_Window Ac;
        Main_Window_2 main_Window;

        private void OnLoad()
        {

            t = new Till();
            Load_Data();

        }

        private void Load_Data()
        {
            Fill_tilltypes_ComboBox();
        }

        public void Fill_tilltypes_ComboBox()
        {
            if (Login.BuildConnection())
            {
                tilltypes_ComboBox.ItemsSource = null;
                dt = t.Select_Account_Missing_TillTypes(Account_ID);
                if (dt != null && dt.Rows.Count > 0)
                {
                    tilltypes_ComboBox.ItemsSource = dt.DefaultView;
                }
            }
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (tilltypes_ComboBox.SelectedIndex == -1 || tilltypes_ComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Please select the currency of the new till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }

                t.insert(Convert.ToInt32(tilltypes_ComboBox.SelectedValue), tilltypes_ComboBox.Text, Account_ID, "Add_till");
                Ac.Fill_Account_Tabs("", "");
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }

        private void AddNewTillType_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window _Window = new AddTillType(this, main_Window);
                _Window.Show();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else 
                { MessageBox.Show(ex.Message, "Error"); }
            }

        }
    }
}
