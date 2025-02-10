using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_MultiPayments_Windows.xaml
    /// </summary>
    public partial class Add_MultiPayments_Windows : Window
    {
        public Add_MultiPayments_Windows(int SubProject_ID, Payments_Window P_W, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.P_W = P_W;
            this.SubProject_ID = SubProject_ID;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        Main_Window_2 main_Window;
        Payments_Window P_W;
        int SubProject_ID;
        Beneficiary b; Account Acc; Payment p;
        private DataTable dt, CashAccounts_dt, Beneficiaries_dt, Payments_dt;

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                Export_btn.CommandTarget = myDataGrid;
            }
        }

        private bool OnLoad()
        {
            try
            {
                b = new Beneficiary();
                Acc = new Account();
                p = new Payment();

                Create_Columns();
                Fill_Datatables();
                Apply_Color_Theme();
                this.myDataGrid.GridCopyPaste = new MultiPayments_CustomCopyPaste(myDataGrid, SubProject_ID, CashAccounts_dt, Beneficiaries_dt, Payments_dt);

                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
                return false;
            }
        }

        private void Apply_Color_Theme()
        {
            System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["HopeRed"];

  
            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UpdateTextBlocksForeground(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        private void Create_Columns()
        {

            Login.MultiPayments_AnyError = false;
            Login.MultiPayments_List.Clear();
            Login.MultiPayments_sc.Parameters.Clear();


            dt = new DataTable();

            dt.Columns.Add("Project number");
            dt.Columns.Add("Payment amount");
            dt.Columns.Add("Description");
            dt.Columns.Add("Cash till");
            dt.Columns.Add("Date");


            System.Data.DataRow newRow = dt.NewRow();
            dt.Rows.Add(newRow);

            myDataGrid.ItemsSource = null;
            myDataGrid.ItemsSource = dt.DefaultView;
        }

        private void Fill_Datatables()
        {
            CashAccounts_dt = Acc.Select_Cash_Account("", SubProject_ID.ToString());
            DataTable Beneficiaries_temp_dt = b.Select("", "", SubProject_ID.ToString());

            if (Settings.Default.Role == "Accountant")
            {
                List<int> UserAccounts_ID = new List<int>();
                if (Settings.Default.User_Accounts != null && !string.IsNullOrEmpty(Settings.Default.User_Accounts.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_Accounts.ToString()))
                {
                    UserAccounts_ID = Settings.Default.User_Accounts.ToString().Split(',').Select(int.Parse).ToList();

                    string filterExpression = string.Join(" OR ", UserAccounts_ID.Select(id => "Account_ID = " + id));
                    System.Data.DataRow[] rows = Beneficiaries_temp_dt.Select(filterExpression);
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        Beneficiaries_dt = selected_dt;
                    }
                }
                else
                {
                    Beneficiaries_dt = null;
                }

            }
            else
            {
                Beneficiaries_dt = Beneficiaries_temp_dt;
            }


            Payments_dt = p.Select_Beneficiary_Payments("", "");

        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Create_Columns();

                this.myDataGrid.GridCopyPaste = new MultiPayments_CustomCopyPaste(myDataGrid, SubProject_ID, CashAccounts_dt, Beneficiaries_dt, Payments_dt);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;

            try
            {
                if (Login.MultiPayments_AnyError)
                {
                    MessageBox.Show("Please make sure there are no errors in the entered data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }
                else
                {
                    if(Login.MultiPayments_List.Count > 0)
                    {
                        if (Login.BuildConnection())
                        {
                            p.Insert_Multi_payments();

                            MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                            Create_Columns();
                            Fill_Datatables();

                            this.myDataGrid.GridCopyPaste = new MultiPayments_CustomCopyPaste(myDataGrid, SubProject_ID, CashAccounts_dt, Beneficiaries_dt, Payments_dt);


                            //if (P_W != null)
                            //    P_W.LoadGrid();
                        }
                    }

                }
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
    }
}
