using Financial_WPF_App.Models;
using Syncfusion.Windows.Shared;
using Syncfusion.XlsIO.Implementation.PivotAnalysis;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_FiscalBond_Window.xaml
    /// </summary>
    public partial class Add_FiscalBond_Window : Window
    {
        public Add_FiscalBond_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.FiscalBond_ID = -1;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Add_FiscalBond_Window(int FiscalBond_ID, DataTable dt, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.FiscalBond_ID = FiscalBond_ID;
            this.dt = dt;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private SubProject sub;
        private Account a;
        private FiscalBond fb;
        private DataTable dt, a_dt, sub_dt, t_dt;
        Main_Window_2 main_Window;
        private Till t;
        private int FiscalBond_ID = -1;
        int Till_ID = -1;

        bool UserMode;


        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];

        private bool OnLoad()
        {
            try
            {
                sub = new SubProject();
                a = new Account();
                fb = new FiscalBond();
                t = new Till();
                Load_Data();
                Apply_Color_Theme();
                if(FiscalBond_ID > 0)
                {
                    Fill_FiscalBond();
                }
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
                return false;
            }
        }

        private void Apply_Color_Theme()
        {


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Load_Data()
        {
            Date_datepicker.SelectedDate = DateTime.Today;
            Load_Comboboxes_Data();

            if(FiscalBond_ID > 0)
            {
                Fill_FiscalBond();
            }
        }

        private void Load_Comboboxes_Data()
        {
            SubProject_comboBox.ItemsSource = null;
            sub_dt = sub.Select("", "", "", "");
            if (sub_dt != null && sub_dt.Rows.Count > 0)
            {
                SubProject_comboBox.ItemsSource = sub_dt.DefaultView;
            }


            Account_comboBox.ItemsSource = null;
            a_dt = a.Select("", "", "Project", false, "");

        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }

                if (SubProject_comboBox.SelectedIndex == -1 || Account_comboBox.SelectedIndex == -1 || Till_ComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the center, account and till", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }

                if (Login.BuildConnection())
                {
                    string Name = Name_txtBox.Text;
                    string Date = ((DateTime)Date_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                    string Notes = Notes_txtBox.Text;
                    int SubProject_ID = Convert.ToInt32(SubProject_comboBox.SelectedValue);
                    int Account_ID = Convert.ToInt32(Account_comboBox.SelectedValue);
                    string Account = Account_comboBox.Text.ToString();
                    int Till_ID = Convert.ToInt32(Till_ComboBox.SelectedValue);

                    if (FiscalBond_ID != -1)//Edit
                    {
                        fb.Update(FiscalBond_ID, Name, Date, Notes, Account_ID, Till_ID, "FiscalBond_Window");
                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        this.Close();
                    }
                    else//Add new
                    {
                        fb.Insert(Name, Date, Notes, Account_ID, Till_ID, "FiscalBond_Window");
                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        Clear_Boxes();
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
        }

        private void Clear_Boxes()
        {
            Name_txtBox.Text = Notes_txtBox.Text = "";
            Account_comboBox.SelectedIndex = SubProject_comboBox.SelectedIndex = Till_ComboBox.SelectedIndex = -1;
            Date_datepicker.SelectedDate = DateTime.Now;
        }

        private void Fill_FiscalBond()
        {
            UserMode = false;

            DataTable Bond_dt = dt.Select("id = " + FiscalBond_ID + "").CopyToDataTable();

            Till_ID = Convert.ToInt32(Bond_dt.Rows[0].Field<int>("Till_ID").ToString());
            Name_txtBox.Text = Bond_dt.Rows[0].Field<string>("Name").ToString();
            Date_datepicker.SelectedDate = Convert.ToDateTime(Bond_dt.Rows[0].Field<DateTime>("Date").ToString());
            Notes_txtBox.Text = Bond_dt.Rows[0].Field<string>("Notes").ToString();


   
            if (Bond_dt.Rows[0]["SubProject_ID"] != DBNull.Value)
            {
                SubProject_comboBox.SelectedValue = Convert.ToInt32(Bond_dt.Rows[0].Field<int>("SubProject_ID").ToString());
                SubProject_comboBox_SelectionChanged(SubProject_comboBox, null);
            }

            if (Bond_dt.Rows[0]["Account_ID"] != DBNull.Value)
            {
                Account_comboBox.SelectedValue = Convert.ToInt32(Bond_dt.Rows[0].Field<int>("Account_ID").ToString());
                Account_comboBox_SelectionChanged(Account_comboBox, null);
            }

            if (Bond_dt.Rows[0]["Till_ID"] != DBNull.Value)
                Till_ComboBox.SelectedValue = Convert.ToInt32(Bond_dt.Rows[0].Field<int>("Till_ID"));

            UserMode = true;
        }

        private void Account_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Account_comboBox.SelectedIndex = -1;

                Till_ComboBox.SelectedIndex = -1;
                Till_ComboBox.ItemsSource = null;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Account_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (Account_comboBox.SelectedIndex != -1 && Account_comboBox.SelectedValue != null)
                {
                    Till_ComboBox.ItemsSource = null;
                    string Account_ID = Account_comboBox.SelectedValue.ToString();
                    LoadTillComboBox(Account_ID);
                }
                else
                {
                    Till_ComboBox.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void LoadTillComboBox(string account_ID)
        {
            Till_ComboBox.ItemsSource = null;

            t_dt = t.Select_FiscalBond_Missing_TillTypes(account_ID, Till_ID);
            if (t_dt != null && t_dt.Rows.Count > 0)
                Till_ComboBox.ItemsSource = t_dt.DefaultView;
        }

        private void SubProject_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SubProject_comboBox.SelectedIndex = -1;

                Account_comboBox.SelectedIndex = -1;
                Account_comboBox.ItemsSource = null;

                Till_ComboBox.SelectedIndex = -1;
                Till_ComboBox.ItemsSource = null;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void SubProject_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                Account_comboBox.ItemsSource = null;
                if (SubProject_comboBox.SelectedValue != null && SubProject_comboBox.SelectedIndex != -1)
                {
                    int SubProject_id = Convert.ToInt32(SubProject_comboBox.SelectedValue);

                    System.Data.DataRow[] rows = a_dt.Select("SubProject_ID=" + SubProject_id);
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                            Account_comboBox.ItemsSource = selected_dt.DefaultView;
                    }

                }
                Till_ComboBox.SelectedIndex = -1;
                Till_ComboBox.ItemsSource = null;


            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void Till_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Till_ComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }
    }
}
