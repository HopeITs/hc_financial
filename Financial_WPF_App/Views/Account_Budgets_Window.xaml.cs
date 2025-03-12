using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for AddBudgetLines_Windows.xaml
    /// </summary>
    public partial class AddBudgetLines_Windows : Window
    {
        public AddBudgetLines_Windows(string Account_ID, bool IsAdminAccount, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.IsAdminAccount = IsAdminAccount;
            if (Account_ID != "") this.Account_ID = Convert.ToInt32(Account_ID);
            else this.Account_ID = -1;
            OnLoad();

        }

        private int Account_ID, Budget_ID;
        private Budget b; private Account acc;
        Till t;
        private DataTable Budgets_dt, TillType_dt, Donor_dt, all_budget_donor_dt,
            budget_donor_dt = new DataTable(), original_budget_donor_dt = new DataTable();
        Main_Window_2 main_Window;
        bool IsAdminAccount;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        bool ShowTotals;
        bool UserMode;
        StackedHeaderRow StackedHeaderRow1 = new StackedHeaderRow { };

        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
        string HeaderStyle_Name = "PastelDarkBlue_SfDataGrid_header";
        string TableSummaryRowStyle_Name = "PastelDarkBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "PastelDarkBlue_GroupSummaryRowStyle";

        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;

        #endregion

        private void OnLoad()
        {
            Set_privileges();
            Show_Hide();

            UserMode = false;
            acc = new Account();
            b = new Budget();
            t = new Till();
            Rate_TextBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
            TillType_dt = new DataTable();
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
            }

            Load_Data();
            Apply_Color_Theme();
            Apply_DataGrid_Color_Theme();
            UserMode = true;
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
            myDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            myDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            myDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];

            Budget_Dononrs_DataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            Budget_Dononrs_DataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            Budget_Dononrs_DataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
    
        }

        private void Set_privileges()
        {
            Edit_privileges = Add_privileges = Delete_privileges = false;

            if (Settings.Default.Role == "Head Office")
            {
                Edit_privileges = Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Project Manager")
            {

            }
            else if (Settings.Default.Role == "Financial Manager")
            {

            }
            else if (Settings.Default.Role == "Accountant")
            {

            }
            else if (Settings.Default.Role == "Cashier")
            {

            }
            else if (Settings.Default.Role == "Employee")
            {

            }
            else if (Settings.Default.Role == "Donor")
            {

            }
        }

        private void Show_Hide()
        {
            if (!Add_privileges)
            {
                Grid1.Visibility = Visibility.Collapsed;
                First_col.Width = new GridLength(0, GridUnitType.Star);
            }
            if (!Edit_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Approved" || C.HeaderText.ToString() == "Attachment" || C.HeaderText.ToString() == "Edit")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }
            }
            if (!Delete_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Delete")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }
            }
        }

        private void Load_Data()
        {
            Name_txtBox.Text = "";
            beginDate_datepicker.SelectedDate = null;
            endDate_datepicker.SelectedDate = null;
            Budget_ID = -1;
            TillType_comboBox.SelectedIndex = -1;
            Donor_comboBox.SelectedIndex = -1;
            Rate_TextBox.Clear();
            Budget_Dononrs_DataGrid.ItemsSource = null;

            Fill_comboBoxs();
            Fill_Budget(false);
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

        private void Fill_Budget(bool Update)
        {
            myDataGrid.ItemsSource = null;

            if (!Update)
            {
                Budgets_dt = b.Select(Account_ID);
                all_budget_donor_dt = b.Select_budget_donor(Account_ID, -1);
            }

            if (Budgets_dt != null && Budgets_dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = Budgets_dt.DefaultView;

                if (HOffset != 0)
                    this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
            }
            else
            {
                myDataGrid.ItemsSource = null;
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].IsHidden = true;
                }
            }

            if (all_budget_donor_dt != null && all_budget_donor_dt.Rows.Count > 0)
            {
                var duplicateBudgetIds = all_budget_donor_dt.AsEnumerable()
                          .GroupBy(row => row.Field<int>("budget_id"))
                          .Where(group => group.Count() > 1)
                          .Select(group => group.Key)
                          .ToList();

                if (duplicateBudgetIds.Count > 0)
                {
                    Total_RadioButton.Visibility = PerDonor_RadioButton.Visibility = Visibility.Visible;
                    //Panel1.Visibility = Visibility.Visible;
                }
                else
                {
                    Total_RadioButton.Visibility = PerDonor_RadioButton.Visibility = Visibility.Collapsed;

                    //Panel1.Visibility = Visibility.Collapsed;
                    Total_RadioButton.IsChecked = true;
                }
            }
            else
            {
                Total_RadioButton.Visibility = PerDonor_RadioButton.Visibility = Visibility.Collapsed;

                //Panel1.Visibility = Visibility.Collapsed;
                Total_RadioButton.IsChecked = true;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                Budget_ID = Convert.ToInt32(dataRow["ID"]);

                if (dataRow["Name"] != DBNull.Value || dataRow["Name"] != null)
                    Name_txtBox.Text = dataRow["Name"].ToString();

                beginDate_datepicker.SelectedDate = Convert.ToDateTime(dataRow["BeginDate"].ToString());
                endDate_datepicker.SelectedDate = Convert.ToDateTime(dataRow["EndDate"].ToString());

                TillType_comboBox.SelectedValue = Convert.ToInt32(dataRow["Budget_Type_ID"].ToString());


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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                    Budget_ID = Convert.ToInt32(dataRow[0].ToString());
                    string Name = dataRow["Name"].ToString();

                    b.Delete(Budget_ID, Account_ID, Name, "Account_Budgets_Window");
                    Load_Data();
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                { MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
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
                if (Login.BuildConnection())
                {
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
                    }
                    else//Add new
                    {
                        if (!Add_privileges)
                        {
                            MessageBox.Show("Sorry … You do not have add permissions.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;
                            //throw new Exception("Sorry … You do not have add permissions.");
                        }

                        b.Insert(Name, BeginDate, EndDate, Account_ID, Budget_Type_ID, Add_Donors_dt, "Account_Budgets_Window");
                    }
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                    Load_Data();
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

        private void myDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
        {
            try
            {
                int count = myDataGrid.SelectedItems.Count;
                Selected_textBlock.Text = count.ToString("N0");
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Load_Data();
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

        private void myDataGrid_AutoGeneratingColumn(object sender, Syncfusion.UI.Xaml.Grid.AutoGeneratingColumnArgs e)
        {

            try
            {
                if (e.Column is GridNumericColumn && !e.Column.MappingName.Contains("_ID") && e.Column.MappingName != "ID")
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;

                //HIDE ID column.
                if (e.Column.MappingName.Contains("_ID") || e.Column.MappingName == "ID")
                    e.Cancel = true;

                if (ShowTotals)
                {
                    var AccountDononr = Donor_dt.AsEnumerable().Select(row => row.Field<string>("Name"));
                    if (AccountDononr.Any(name => e.Column.MappingName.Contains(name)))
                    {
                        e.Cancel = true;
                    }
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

        private void Bind_Show_Hide_Menu()
        {

            SolidColorBrush MyWhite = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF");
            this.ShowMenu.Items.Clear();
            this.HideMenu.Items.Clear();
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                string CHeader = myDataGrid.Columns[i].HeaderText.ToString();
                MenuItem NewItem = new MenuItem();
                NewItem.Header = CHeader;
                NewItem.Background = MyWhite;
                NewItem.StaysOpenOnClick = true;
                if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") &&
                        !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") &&
                        !CHeader.Contains("Select") && !CHeader.Contains("#") &&
                        !Originally_HiddenColumns.Contains(CHeader))
                {
                    if (myDataGrid.Columns[i].IsHidden == false)
                    {
                        bool found = false;

                        for (int j = 0; j < this.HideMenu.Items.Count; j++)
                        {
                            if (((MenuItem)this.HideMenu.Items[j]).Header.ToString() == CHeader)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            NewItem.Click += new RoutedEventHandler(HideColumn);
                            this.HideMenu.Items.Add(NewItem);
                        }

                    }
                    else
                    {
                        bool found = false;

                        for (int j = 0; j < this.ShowMenu.Items.Count; j++)
                        {
                            if (((MenuItem)this.ShowMenu.Items[j]).Header.ToString() == CHeader)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            NewItem.Click += new RoutedEventHandler(ShowColumn);
                            this.ShowMenu.Items.Add(NewItem);
                        }
                    }
                }
            }
        }
        private void ShowColumn(object sender, RoutedEventArgs e)
        {
            try
            {
                string CHeader = ((MenuItem)sender).Header.ToString();
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    if (myDataGrid.Columns[i].HeaderText.ToString() == CHeader)
                    {
                        myDataGrid.Columns[i].IsHidden = false;

                        HiddenColumns.RemoveAt(HiddenColumns.IndexOf(CHeader));

                        //break;
                    }
                }
                Bind_Show_Hide_Menu();
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
        private void HideColumn(object sender, RoutedEventArgs e)
        {
            try
            {
                string CHeader = ((MenuItem)sender).Header.ToString();
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    if (myDataGrid.Columns[i].HeaderText.ToString() == CHeader)
                    {
                        myDataGrid.Columns[i].IsHidden = true;
                        HiddenColumns.Add(CHeader);

                        //break;
                    }
                }
                Bind_Show_Hide_Menu();
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
        private void ShowAll_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    string CHeader = myDataGrid.Columns[i].HeaderText.ToString();
                    if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") && !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") && !CHeader.Contains("#") && !Originally_HiddenColumns.Contains(CHeader))
                    {
                        myDataGrid.Columns[i].IsHidden = false;
                    }
                }
                HiddenColumns.Clear();
                Bind_Show_Hide_Menu();
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

        private void myDataGrid_ItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e)
        {
            try
            {
                if (myDataGrid.StackedHeaderRows.Count != 0)
                    myDataGrid.StackedHeaderRows.RemoveAt(0);

                if (myDataGrid.ItemsSource != null && !ShowTotals)
                {
                    add_stackedHeader();
                    //rename_Columns();
                }
                move_Columns();
                Bind_Show_Hide_Menu();
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

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;

                int count = myDataGrid.View.Records.Count();
                All_textBlock.Text = count.ToString("N0");

                //All_textBlock.Text = myDataGrid.View.Records.Count().ToString();
                Export_btn.CommandTarget = myDataGrid;
            }
        }

        void ScrollOwner_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange > 0 || e.HorizontalChange < 0)
            {
                HOffset = e.HorizontalOffset;
            }
        }

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void myDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if(Edit_privileges)
                {
                    IInputElement element = e.MouseDevice.DirectlyOver;
                    if (element != null && element is FrameworkElement)
                    {
                        if (((FrameworkElement)element).Parent is GridCell)
                        {
                            var grid = sender as SfDataGrid;
                            if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                            {
                                DataRowView rowview = grid.SelectedItem as DataRowView;

                                Budget_ID = Convert.ToInt32(rowview["ID"].ToString());

                                if (rowview["Name"] != DBNull.Value || rowview["Name"] != null)
                                    Name_txtBox.Text = rowview["Name"].ToString();

                                beginDate_datepicker.SelectedDate = Convert.ToDateTime(rowview["BeginDate"].ToString());
                                endDate_datepicker.SelectedDate = Convert.ToDateTime(rowview["EndDate"].ToString());

                                TillType_comboBox.SelectedValue = Convert.ToInt32(rowview["Budget_Type_ID"].ToString());


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
                            }
                        }
                    }
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

        private void move_Columns()
        {
            if (myDataGrid.Columns.Count > 2)
            {
                bool EditMoved = false;
                bool DeleteMoved = false;
                bool AttachmentMoved = false;
                int k = 0;
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    if (!EditMoved && myDataGrid.Columns[k].HeaderText == "Edit")
                    {
                        if (!Originally_HiddenColumns.Contains("Edit"))
                        {
                            EditMoved = true; myDataGrid.Columns.MoveTo(i, myDataGrid.Columns.Count - 1); i--;
                        }
                        else
                            k++;
                    }
                    else if (!DeleteMoved && myDataGrid.Columns[k].HeaderText == "Delete")
                    {
                        if (!Originally_HiddenColumns.Contains("Delete"))
                        {
                            DeleteMoved = true; myDataGrid.Columns.MoveTo(i, myDataGrid.Columns.Count - 1); i--;
                        }
                        else
                            k++;
                    }
                    else if (!AttachmentMoved && myDataGrid.Columns[k].HeaderText == "Attachment")
                    {
                        if (!Originally_HiddenColumns.Contains("Attachment"))
                        {
                            AttachmentMoved = true; myDataGrid.Columns.MoveTo(i, myDataGrid.Columns.Count - 1); i--;
                        }
                        else
                            k++;
                    }
                }
            }
        }

        private void Total_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = true;
                if (UserMode)
                {
                    Fill_Budget(true);
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

        private void PerDonor_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = false;
                if (UserMode)
                {
                    Fill_Budget(true);
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

        private void add_stackedHeader()
        {
            var StackedHeaderRow1 = new StackedHeaderRow { };

            var ExpectedTotalColumn = new StackedColumn
            {
                HeaderText = "Expected Total",
                MappingName = "ExpectedTotal",
            };
            var ExpendituresTotalColumn = new StackedColumn
            {
                HeaderText = "Expenditures Total",
                MappingName = "ExpendituresTotal"
            };
            var RemainingColumn = new StackedColumn
            {
                HeaderText = "Remaining",
                MappingName = "Remaining"
            };

            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i].MappingName != null)
                {
                    if (myDataGrid.Columns[i].MappingName.Contains("Expected Total"))
                    {
                        if (ExpectedTotalColumn.ChildColumns == "")
                            ExpectedTotalColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            ExpectedTotalColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Expenditures Total"))
                    {
                        if (ExpendituresTotalColumn.ChildColumns == "")
                            ExpendituresTotalColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            ExpendituresTotalColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Remaining"))
                    {
                        if (RemainingColumn.ChildColumns == "")
                            RemainingColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            RemainingColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                }
            }

            StackedHeaderRow1.StackedColumns.Add(ExpectedTotalColumn);
            StackedHeaderRow1.StackedColumns.Add(ExpendituresTotalColumn);
            StackedHeaderRow1.StackedColumns.Add(RemainingColumn);

            myDataGrid.StackedHeaderRows.Add(StackedHeaderRow1);

            //Default GridStackedCellRenderer is removed.
            myDataGrid.CellRenderers.Remove("StackedHeader");

            //Customized GridStackedCellRenderer is added.
            myDataGrid.CellRenderers.Add("StackedHeader", new GridCustomStackedRenderer(Theme_Color));
        }

        private void rename_Columns()
        {
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i].MappingName != null)
                {
                    if (myDataGrid.Columns[i].MappingName.Contains("Expected Total"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Expected Total")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Expected Total", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Expenditures Total"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Expenditures Total")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Expenditures Total", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Remaining"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Remaining")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Remaining", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
                    }
                }
            }
        }

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;
        }
        private void Search_txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Get PlaceholderTextBlock inside the TextBox template
                var placeholder = (TextBlock)Search_txtBox.Template.FindName("PlaceholderText", Search_txtBox);

                if (placeholder != null)
                {
                    // Hide placeholder when user types, show when empty
                    placeholder.Visibility = string.IsNullOrWhiteSpace(Search_txtBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                }

                if (Budgets_dt != null)
                {
                    SearchDataGrid searchHelper = new SearchDataGrid(Budgets_dt);

                    // Call the search method when text changes
                    searchHelper.Search(Search_txtBox.Text, myDataGrid);
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
    }


}
