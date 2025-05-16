using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_Account_Budget_Window.xaml
    /// </summary>
    public partial class Add_Account_Budget_Window : Window
    {
        public Add_Account_Budget_Window(int Account_ID, DataTable Budgets_dt, DataTable all_budget_donor_dt, AddBudgetLines_Windows BL, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.Account_ID = Account_ID;
            this.BL = BL;
            this.main_Window = main_Window;
            this.Budgets_dt = Budgets_dt;
            this.all_budget_donor_dt = all_budget_donor_dt;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Add_Account_Budget_Window(int Account_ID, int Budget_ID, DataTable Budgets_dt, DataTable all_budget_donor_dt, AddBudgetLines_Windows BL, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.Budget_ID = Budget_ID;
            this.Account_ID = Account_ID;
            this.BL = BL;
            this.main_Window = main_Window;
            this.Budgets_dt = Budgets_dt;
            this.all_budget_donor_dt = all_budget_donor_dt;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }


        int Budget_ID = -1; int Account_ID = -1;
        AddBudgetLines_Windows BL;
        Main_Window_2 main_Window;
        private Account acc;
        private Till t;
        private Budget b;
        private DataTable TillType_dt, Donor_dt, all_budget_donor_dt, Budgets_dt,
                        budget_donor_dt = new DataTable(), original_budget_donor_dt = new DataTable();
        bool UserMode = false;


        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
        string HeaderStyle_Name = "PastelDarkBlue_SfDataGrid_header";
        string TableSummaryRowStyle_Name = "PastelDarkBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "PastelDarkBlue_GroupSummaryRowStyle";

        private bool OnLoad()
        {
            try
            {
                UserMode = false;
                acc = new Account();
                b = new Budget();
                t = new Till();

                Rate_TextBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
                TillType_dt = new DataTable();


                Load_Data();
                Apply_Color_Theme();
                Apply_DataGrid_Color_Theme();
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


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Apply_DataGrid_Color_Theme()
        {
            Budget_Dononrs_DataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            Budget_Dononrs_DataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            Budget_Dononrs_DataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }

        private void Load_Data()
        {
            ClearBoxes();

            Fill_comboBoxs();
            if (Budget_ID != -1)
            {
                Fill_Budget_Details();
            }
        }

        private void Fill_comboBoxs()
        {
            TillType_dt = t.Select_Account_TillTypes(Account_ID, "SYR");
            if (TillType_dt != null && TillType_dt.Rows.Count > 0)
            {
                TillType_comboBox.ItemsSource = TillType_dt.DefaultView;
            }
            else
            {
                TillType_comboBox.ItemsSource = null;
            }

            Donor_dt = acc.Select_Account_Donors(Account_ID);
            if (Donor_dt != null && Donor_dt.Rows.Count > 0)
            {
                Donor_comboBox.ItemsSource = Donor_dt.DefaultView;
            }
            else
            {
                Donor_comboBox.ItemsSource = null;
            }
        }

        private void Rate_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Rate_TextBox.Text != "")
                {
                    Rate_TextBox.Text = Login.ConvertToDouble(Rate_TextBox.Text);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
        }

        private void Fill_Budget_Details()
        {
            try
            {
                UserMode = false;

                DataTable this_Budget_dt = Budgets_dt.Select("ID = " + Budget_ID + "").CopyToDataTable();


                if (this_Budget_dt.Rows[0]["Name"] != DBNull.Value || this_Budget_dt.Rows[0]["Name"] != null)
                    Name_txtBox.Text = this_Budget_dt.Rows[0].Field<string>("Name").ToString();

                beginDate_datepicker.SelectedDate = Convert.ToDateTime(this_Budget_dt.Rows[0].Field<DateTime>("BeginDate").ToString());

                endDate_datepicker.SelectedDate = Convert.ToDateTime(this_Budget_dt.Rows[0].Field<DateTime>("EndDate").ToString());

                TillType_comboBox.SelectedValue = Convert.ToInt32(this_Budget_dt.Rows[0].Field<int>("Budget_Type_ID").ToString());


                System.Data.DataRow[] rows = all_budget_donor_dt.Select("budget_id = '" + Budget_ID + "'");
                if (rows.Length != 0)
                {
                    budget_donor_dt = rows.CopyToDataTable();
                    original_budget_donor_dt = rows.CopyToDataTable();
                    if (budget_donor_dt != null && budget_donor_dt.Rows.Count > 0)
                        Budget_Dononrs_DataGrid.ItemsSource = budget_donor_dt.DefaultView;

                }

                var budget_donor_ids = budget_donor_dt.AsEnumerable().Select(row => row.Field<int>("Donor_ID"));

                // Select rows from dt1 where id is not in dt2Ids
                var result = Donor_dt.AsEnumerable()
                                .Where(row => !budget_donor_ids.Contains(row.Field<int>("ID")));


                if (result.Count() > 0)
                {
                    Donor_dt = result.CopyToDataTable();
                    Donor_comboBox.ItemsSource = Donor_dt.DefaultView;
                }
                else
                {
                    Donor_dt.Clear();
                    Donor_comboBox.ItemsSource = null;
                }
                UserMode = true;
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

        private void AddDonor_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Donor_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the donor before entering.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Please select the donor before entering.");

                }

                double rate;

                if (Rate_TextBox.Text == "" || !double.TryParse(Rate_TextBox.Text, out rate))
                {
                    MessageBox.Show("Please make sure to enter the percentage correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Please make sure to enter the percentage correctly.");
                }

                if (Budget_Dononrs_DataGrid.ItemsSource == null)
                {
                    budget_donor_dt = new DataTable();
                    budget_donor_dt.Columns.Add("Budget_Donor_ID", typeof(int));
                    budget_donor_dt.Columns.Add("Donor_ID", typeof(int));
                    budget_donor_dt.Columns.Add("Donor", typeof(string));
                    budget_donor_dt.Columns.Add("Rate", typeof(double));

                    Budget_Dononrs_DataGrid.ItemsSource = budget_donor_dt;
                }

                System.Data.DataRow dr = budget_donor_dt.NewRow();

                dr["Budget_Donor_ID"] = -1;
                dr["Donor_ID"] = Convert.ToInt32(Donor_comboBox.SelectedValue);
                dr["Donor"] = Donor_comboBox.Text;
                dr["Rate"] = rate;

                budget_donor_dt.Rows.Add(dr);
                budget_donor_dt.AcceptChanges();


                //remove added donor from donor Combobox
                System.Data.DataRow[] rowsToDelete = Donor_dt.Select("ID = '" + Donor_comboBox.SelectedValue + "'");
                foreach (System.Data.DataRow row in rowsToDelete)
                {
                    Donor_dt.Rows.Remove(row);
                    Donor_dt.AcceptChanges();
                }
                Donor_comboBox.ItemsSource = Donor_dt.DefaultView;
                Donor_comboBox.SelectedIndex = -1;
                Rate_TextBox.Clear();
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

        private void Donor_DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)Budget_Dononrs_DataGrid.CurrentItem;
                int Current_donor_id = Convert.ToInt32(dataRow["Donor_ID"].ToString());
                string Current_donor_name = dataRow["Donor"].ToString();

                System.Data.DataRow[] dtr = budget_donor_dt.Select("Donor_ID='" + Current_donor_id + "'");
                foreach (var drow in dtr)
                {
                    drow.Delete();
                }
                budget_donor_dt.AcceptChanges();

                //resore deleted donor to donor Combobox
                System.Data.DataRow dr = Donor_dt.NewRow();
                dr["ID"] = Current_donor_id;
                dr["Name"] = Current_donor_name;
                Donor_dt.Rows.Add(dr);
                Donor_dt.AcceptChanges();

                Donor_comboBox.ItemsSource = Donor_dt.DefaultView;
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

        private void Rate_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Get PlaceholderTextBlock inside the TextBox template
                var placeholder = (TextBlock)Rate_TextBox.Template.FindName("PlaceholderText", Rate_TextBox);

                if (placeholder != null)
                {
                    // Hide placeholder when user types, show when empty
                    placeholder.Visibility = string.IsNullOrWhiteSpace(Rate_TextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please make sure to enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Please make sure to enter the name.");

                }

                if (beginDate_datepicker.SelectedDate == null || endDate_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please make sure to specify the start and end dates.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Please make sure to specify the start and end dates.");

                }

                if (beginDate_datepicker.SelectedDate >= endDate_datepicker.SelectedDate)
                {
                    MessageBox.Show("Error in start and end dates.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Error in start and end dates.");

                }

                foreach (System.Data.DataRow R in Budgets_dt.Rows)
                {
                    if (Budget_ID == -1 || R.Field<int>("ID") != Budget_ID)
                    {
                        if (beginDate_datepicker.SelectedDate <= Convert.ToDateTime(R.Field<DateTime>("EndDate").ToString()) && endDate_datepicker.SelectedDate >= Convert.ToDateTime(R.Field<DateTime>("BeginDate").ToString()))
                        {
                            MessageBox.Show("There is an overlap between the entered dates and previously specified dates.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;
                            //throw new Exception("There is an overlap between the entered dates and previously specified dates.");
                        }
                    }
                }

                if (TillType_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the budget currency.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Please select the budget currency.");

                }

                if (budget_donor_dt.Rows.Count == 0)
                {
                    MessageBox.Show("Please select at least one donor for the budget.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Please select at least one donor for the budget.");
                }

                object sumObject;
                sumObject = budget_donor_dt.Compute("Sum(Rate)", string.Empty);

                if (Convert.ToDouble(sumObject) != 100)
                {
                    if (MessageBox.Show("Alert!!!" + Environment.NewLine + "The total percentage of all donors does not equal 100%" + Environment.NewLine + "Are you sure you want to continue??", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.None) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                string Name = Name_txtBox.Text;
                string BeginDate = ((DateTime)beginDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                string EndDate = ((DateTime)endDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                int Budget_Type_ID = Convert.ToInt32(TillType_comboBox.SelectedValue);

                DataTable Add_Donors_dt = new DataTable();
                System.Data.DataRow[] rowsToAdd = budget_donor_dt.Select("Budget_Donor_ID = '-1'");
                if (rowsToAdd.Count() > 0)
                    Add_Donors_dt = rowsToAdd.CopyToDataTable();

                if (Budget_ID != -1)//Edit
                {
                    DataTable Delete_Donors_dt = new DataTable();
                    var kept_Donors = budget_donor_dt.AsEnumerable()
                        .Where(row => row.Field<int>("Budget_Donor_ID") != -1)
                        .Select(row => row.Field<int>("Donor_ID"));

                    // Select rows from dt1 where id is not in dt2Ids
                    var result = original_budget_donor_dt.AsEnumerable()
                                    .Where(row => !kept_Donors.Contains(row.Field<int>("Donor_ID")));

                    if (result.Count() > 0)
                    {
                        Delete_Donors_dt = result.CopyToDataTable();
                    }


                    if (Delete_Donors_dt.Rows.Count > 0 || Add_Donors_dt.Rows.Count > 0)
                    {
                        if (MessageBox.Show("Alert!!!" + Environment.NewLine + "Making any changes to the donors will be accompanied by a change in the calculations of all previous reports for this budget." + Environment.NewLine + "Are you sure you want to continue؟؟", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                    b.Update(Budget_ID, Name, Account_ID, BeginDate, EndDate, Budget_Type_ID, Add_Donors_dt, Delete_Donors_dt, "Account_Budgets_Window");
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    this.Close();
                }
                else//Add new
                {
                    b.Insert(Name, BeginDate, EndDate, Account_ID, Budget_Type_ID, Add_Donors_dt, "Account_Budgets_Window");
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    ClearBoxes();
                }

            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.CloseAndClearConnection();
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

        private void ClearBoxes()
        {
            Name_txtBox.Text = "";
            beginDate_datepicker.SelectedDate = null;
            endDate_datepicker.SelectedDate = null;
            TillType_comboBox.SelectedIndex = -1;
            Donor_comboBox.SelectedIndex = -1;
            Rate_TextBox.Clear();
            Budget_Dononrs_DataGrid.ItemsSource = null;
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
