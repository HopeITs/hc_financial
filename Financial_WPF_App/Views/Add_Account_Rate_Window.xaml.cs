using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_Account_Rate_Window.xaml
    /// </summary>
    public partial class Add_Account_Rate_Window : Window
    {
        public Add_Account_Rate_Window(int Account_ID, bool IsAdminAccount, string Account_Type, string Category, DataTable dt, Rate_Window RT, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.Account_ID = Account_ID;
            this.Rate_ID = -1;
            this.IsAdminAccount = IsAdminAccount;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.dt = dt;
            this.RT = RT;
            this.main_Window = main_Window;


            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Add_Account_Rate_Window(int Account_ID, int Rate_ID, bool IsAdminAccount, string Account_Type, string Category, DataTable dt, Rate_Window RT, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.Account_ID = Account_ID;
            this.Rate_ID = Rate_ID;
            this.IsAdminAccount = IsAdminAccount;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.dt = dt;
            this.RT = RT;
            this.main_Window = main_Window;


            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        int Account_ID;
        int Rate_ID;
        bool IsAdminAccount;
        string Account_Type;
        string Category;
        Rate_Window RT;
        Main_Window_2 main_Window;
        bool UserMode = false;

        Rate r;
        Till t;
        Budget b;
        Account acc;
        DataTable dt, p_dt, Account_Budgets_dt;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];


        private bool OnLoad()
        {
            try
            {
                UserMode = false;
                r = new Rate();
                t = new Till();
                b = new Budget();
                acc = new Account();

                ObserveRate_lable.Visibility = ObserveRate_StackPanel.Visibility = Visibility.Collapsed;
                From_help.Visibility = Visibility.Collapsed;
                if (Account_Type == "Project")
                {
                    if (Category == "Budget")
                    {
                        Account_Budgets_dt = b.Select(Account_ID);
                        From_help.Visibility = Visibility.Visible;
                    }
                }

                LoadFromToComboBoxs();
                if (Rate_ID != -1)
                {
                    Fill_Rate();
                }

                Apply_Color_Theme();
                UserMode = true;


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

            if (Account_Type == "Project")
            {
                if (Category == "Micro Projects")
                {
                    Theme_Color = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                }
                else if (Category == "Budget")
                {
                    Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                }
            }
            else if (Account_Type == "Exchange")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["Orange"];
            }
            else if (Account_Type == "Personal")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelGreen"];
            }
            else if (Account_Type == "Cash")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"];
            }


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Fill_Rate()
        {
            UserMode = false;

            DataTable Rate_dt = dt.Select("id = " + Rate_ID + "").CopyToDataTable();

            Name_txtBox.Text = Rate_dt.Rows[0].Field<string>("Rate group name").ToString();

            BeginDate_datepicker.SelectedDate = Convert.ToDateTime(Rate_dt.Rows[0].Field<DateTime>("Start date").ToString());

            EndDate_datepicker.SelectedDate = Convert.ToDateTime(Rate_dt.Rows[0].Field<DateTime>("End date").ToString());

            if (Rate_dt.Rows[0]["fromtp_id"] != DBNull.Value)
            {
                From_comboBox.SelectedValue = Convert.ToInt32(Rate_dt.Rows[0].Field<int>("fromtp_id"));
            }
            else From_comboBox.SelectedIndex = -1;

            if (Rate_dt.Rows[0]["totp_id"] != DBNull.Value)
            {
                To_comboBox.SelectedValue = Convert.ToInt32(Rate_dt.Rows[0].Field<int>("totp_id"));
            }
            else To_comboBox.SelectedIndex = -1;

            if (Rate_dt.Rows[0]["ObserveRate_ID"] != DBNull.Value)
            {
                LoadObserveRate_comboBox();
                ObserveRate_comboBox.SelectedValue = Convert.ToInt32(Rate_dt.Rows[0].Field<int>("ObserveRate_ID"));
            }

            Value_txtBox.Text = Rate_dt.Rows[0].Field<Double>("Value").ToString();

            UserMode = true;

        }

        private void LoadFromToComboBoxs()
        {
            From_comboBox.ItemsSource = null;
            To_comboBox.ItemsSource = null;
            p_dt = t.Select_Account_TillTypes(Account_ID, "");
            if (p_dt != null && p_dt.Rows.Count > 0)
            {
                From_comboBox.ItemsSource = p_dt.DefaultView;
                To_comboBox.ItemsSource = p_dt.DefaultView;
            }
        }

        private void BeginDate_datepicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (UserMode)
                    CalcValue();
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

        private void From_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (UserMode)
                {
                    if (From_comboBox.SelectedIndex != -1)
                    {
                        LoadObserveRate_comboBox();
                    }
                    else
                    {
                        ObserveRate_comboBox.ItemsSource = null;
                        ObserveRate_lable.Visibility = ObserveRate_StackPanel.Visibility = Visibility.Collapsed;
                    }

                    CalcValue();
                }
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

        private void CalcValue()
        {
            if (BeginDate_datepicker.SelectedDate != null && EndDate_datepicker.SelectedDate != null
                    && From_comboBox.SelectedIndex != -1 && To_comboBox.SelectedIndex != -1)
            {
                string BeginDate = ((DateTime)BeginDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                string EndDate = ((DateTime)EndDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                int from = Convert.ToInt32(From_comboBox.SelectedValue);
                int to = Convert.ToInt32(To_comboBox.SelectedValue);
                Double value = r.Calculate_Value(Account_ID.ToString(), BeginDate, EndDate, from, to);
                Value_txtBox.Text = value.ToString();
            }
        }

        private void LoadObserveRate_comboBox()
        {
            ObserveRate_comboBox.ItemsSource = null;

            if (From_comboBox.SelectedValue != null)
            {
                System.Data.DataRow[] rows = dt.Select("totp_id=" + Convert.ToInt32(From_comboBox.SelectedValue));
                DataTable selected_dt = new DataTable();
                if (rows.Length != 0)
                {
                    selected_dt = rows.CopyToDataTable();
                    if (selected_dt != null && selected_dt.Rows.Count > 0)
                        ObserveRate_comboBox.ItemsSource = selected_dt.DefaultView;

                    ObserveRate_lable.Visibility = ObserveRate_StackPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ObserveRate_lable.Visibility = ObserveRate_StackPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (dt != null && dt.Rows.Count > 0 && Rate_ID == -1)
                {
                    foreach (System.Data.DataRow R in dt.Rows)
                    {
                        if (R.Field<int>("ID") != Rate_ID)
                        {
                            if (From_comboBox.Text == R["From till"].ToString() && To_comboBox.Text == R["To till"].ToString())
                            {
                                if (BeginDate_datepicker.SelectedDate <= Convert.ToDateTime(R.Field<DateTime>("End date").ToString()) && EndDate_datepicker.SelectedDate >= Convert.ToDateTime(R.Field<DateTime>("Start date").ToString()))
                                {
                                    MessageBox.Show("There is an overlap between the entered dates and previously specified dates.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                                    return;
                                }
                            }
                        }
                    }
                }

                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (EndDate_datepicker.SelectedDate == null || BeginDate_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please specify the start and end date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (EndDate_datepicker.SelectedDate < BeginDate_datepicker.SelectedDate)
                {
                    MessageBox.Show("Please make sure the end date is after the start date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (From_comboBox.SelectedIndex == -1 || To_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the Currencies (from - to)", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }


                if (Account_Type == "Project")
                {
                    if (Category == "Budget")
                    {
                        if (To_comboBox.Text.ToLower() == "syr")
                        {
                            System.Data.DataRow[] row = null;
                            if (Account_Budgets_dt != null && Account_Budgets_dt.Rows.Count > 0)
                            {
                                row = Account_Budgets_dt.Select("Budget_Type_ID=" + Convert.ToInt32(From_comboBox.SelectedValue));
                            }

                            if (row == null || row.Count() <= 0)
                            {
                                MessageBox.Show("Exchange to SYR can only be made from the budget currency.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                                return;

                            }
                        }
                    }
                }

                string Name = Login.ReplaceQuotation(Name_txtBox.Text);

                string beginDate = ((DateTime)BeginDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                string endDate = ((DateTime)EndDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                string text = Value_txtBox.Text.Replace(",", "");

                int from = Convert.ToInt32(From_comboBox.SelectedValue);
                int to = Convert.ToInt32(To_comboBox.SelectedValue);

                int ObserveRate_ID;
                if (Convert.ToInt32(ObserveRate_comboBox.SelectedIndex) != -1 && Convert.ToInt32(To_comboBox.SelectedIndex) != -1)
                    ObserveRate_ID = Convert.ToInt32(ObserveRate_comboBox.SelectedValue);
                else
                    ObserveRate_ID = -1;


                if (Rate_ID != -1)//Edit
                {
                    r.Update(Rate_ID, Account_ID, Name, beginDate, endDate, from, to, ObserveRate_ID, "Rate_Window");
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    this.Close();
                }
                else//Add new
                {
                    r.Insert(Account_ID, Name, beginDate, endDate, from, to, ObserveRate_ID, "Rate_Window");
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    ClearBoxes();
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

        private void To_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (UserMode)
                    CalcValue();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
        }

        private void ClearBoxes()
        {
            Name_txtBox.Text = Value_txtBox.Text = "";
            From_comboBox.SelectedIndex = -1; To_comboBox.SelectedIndex = -1;
            BeginDate_datepicker.SelectedDate = null; EndDate_datepicker.SelectedDate = null;
            ObserveRate_comboBox.SelectedIndex = -1;
        }

        private void ObserveRate_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ObserveRate_comboBox.SelectedIndex = -1;
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

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowUtils.MinimizeWindow(this);
        }

        private void Close_Click(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowUtils.CloseWindow(this);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowUtils.TitleBar_MouseDown(this, e);
        }
    }
}
