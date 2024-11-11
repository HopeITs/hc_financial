using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Windows;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_MultiBeneficiary_Windows.xaml
    /// </summary>
    public partial class Add_MultiBeneficiary_Windows : Window
    {
        public Add_MultiBeneficiary_Windows(int SubProject_ID, Account_Beneficiaries_Window AB_W, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.AB_W = AB_W;
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
        Account_Beneficiaries_Window AB_W;
        int SubProject_ID;
        Beneficiary b; Account Acc;
        private DataTable dt, Accounts_dt, Microrojects_dt;

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

                Create_Columns();
                Fill_Datatables();
                this.myDataGrid.GridCopyPaste = new MultiBeneficiares_CustomCopyPaste(myDataGrid, Microrojects_dt, Accounts_dt);

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

        private void Create_Columns()
        {

            Login.MultiBeneficiary_AnyError = false;
            Login.MultiBeneficiary_List.Clear();
            Login.MultiBeneficiary_sc.Parameters.Clear();


            dt = new DataTable();

            dt.Columns.Add("Project Number");
            dt.Columns.Add("Project Title");
            dt.Columns.Add("First name");
            dt.Columns.Add("Father name");
            dt.Columns.Add("Last name");
            dt.Columns.Add("Names of partners");
            dt.Columns.Add("ID number");
            dt.Columns.Add("Mobile number");
            dt.Columns.Add("Loan amount");
            dt.Columns.Add("Amount owed");
            dt.Columns.Add("Payment");
            dt.Columns.Add("Payments interval (M)");
            dt.Columns.Add("Financing date");
            dt.Columns.Add("Payments start date");
            dt.Columns.Add("Receipt number");
            dt.Columns.Add("Order Number");
            dt.Columns.Add("Group account");
            dt.Columns.Add("Notes");

            System.Data.DataRow newRow = dt.NewRow();
            dt.Rows.Add(newRow);

            myDataGrid.ItemsSource = null;
            myDataGrid.ItemsSource = dt.DefaultView;
        }

        private void Fill_Datatables()
        {
            Accounts_dt = Acc.Select(SubProject_ID.ToString(), "", "", false, "");
            Microrojects_dt = b.Select_Subproject_beneficiares(SubProject_ID);
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Create_Columns();

                this.myDataGrid.GridCopyPaste = new MultiBeneficiares_CustomCopyPaste(myDataGrid, Microrojects_dt, Accounts_dt);
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
                if (Login.MultiBeneficiary_AnyError)
                {
                    MessageBox.Show("Please make sure there are no errors in the entered data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }
                else
                {
                    if(Login.MultiBeneficiary_List.Count > 0)
                    {
                        if (Login.BuildConnection())
                        {
                            b.Insert_Multi_beneficiares();

                            MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                            Create_Columns();
                            Fill_Datatables();

                            this.myDataGrid.GridCopyPaste = new MultiBeneficiares_CustomCopyPaste(myDataGrid, Microrojects_dt, Accounts_dt);


                            if (AB_W != null)
                                AB_W.Fill_Grid();
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
