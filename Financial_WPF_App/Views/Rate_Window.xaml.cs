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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    public partial class Rate_Window : Window
    {
        public Rate_Window(int Account_ID, bool IsAdminAccount, string Account_Type, string Category, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Account_ID = Account_ID;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.IsAdminAccount = IsAdminAccount;
            OnLoad();

        }

        int ID, Account_ID;
        string Account_Type, Category;
        Rate r;
        Till t;
        Budget b;
        Account acc;
        DataTable dt, p_dt, Account_Budgets_dt, Donor_dt;
        Main_Window_2 main_Window;
        bool UserMode = false;
        bool IsAdminAccount;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();
        bool ShowTotals;

        StackedHeaderRow StackedHeaderRow1 = new StackedHeaderRow { };
        double HOffset = 0;

        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;

        #endregion
        private void OnLoad()
        {
            Set_privileges();
            Show_Hide();
            UserMode = false;
            r = new Rate();
            t = new Till();
            b = new Budget();
            acc = new Account();
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
            }
            Load_Data();
            Apply_Color_Theme();
            UserMode = true;
        }

        private void Apply_Color_Theme()
        {
            System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];

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
                WindowUtils.UpdateTextBlocksForeground(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
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

                var converter = new GridLengthConverter();
                Row1.Height = (GridLength)converter.ConvertFromString("0*");

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
            ID = -1;


            EndDate_lable_Copy2.Visibility = ObserveRate_comboBox.Visibility = ObserveRate_clear_btn.Visibility = Visibility.Collapsed;
            From_help.Visibility = Visibility.Collapsed;
            if (Account_Type == "Project")
            {
                if (Category == "Budget")
                {
                    Account_Budgets_dt = b.Select(Account_ID);
                    From_help.Visibility = Visibility.Visible;

                    Donor_dt = acc.Select_Account_Donors(Account_ID);
                    if (Donor_dt.Rows.Count > 1)
                    {
                        Panel1.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Panel1.Visibility = Visibility.Collapsed;
                        Total_RadioButton.IsChecked = true;
                    }
                }
                else
                {
                    Panel1.Visibility = Visibility.Collapsed;
                    Total_RadioButton.IsChecked = true;
                }
            }


            LoadFromToComboBoxs();
            LoadGrid(false);
        }

        private void LoadGrid(bool Update)
        {
            myDataGrid.ItemsSource = null;
            if (!Update)
            {
                if (Category == "Budget")
                {
                    dt = r.Select_PerDonor(Account_ID);
                }
                else if (Category == "Micro Projects")
                {
                    dt = r.Select(Account_ID);
                }
            }

            if (dt != null && dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = dt.DefaultView;
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
                }
            }
        }

        private void BeginDate_datepicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Calculate Value //
            try
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

        private void From_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Calculate Value //
            try
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

                if (From_comboBox.SelectedIndex != -1)
                {
                    LoadObserveRate_comboBox();
                    EndDate_lable_Copy2.Visibility = ObserveRate_comboBox.Visibility = ObserveRate_clear_btn.Visibility = Visibility.Visible;
                }
                else
                {
                    ObserveRate_comboBox.ItemsSource = null;
                    EndDate_lable_Copy2.Visibility = ObserveRate_comboBox.Visibility = ObserveRate_clear_btn.Visibility = Visibility.Collapsed;
                }
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
                //SubProject_comboBox.SelectedIndex == -1

                foreach (System.Data.DataRow R in dt.Rows)
                {
                    if (ID == -1 || R.Field<int>("ID") != ID)
                    {
                        if (From_comboBox.Text == R["From till"].ToString() && To_comboBox.Text == R["To till"].ToString())
                        {
                            if (BeginDate_datepicker.SelectedDate <= Convert.ToDateTime(R.Field<DateTime>("End date").ToString()) && EndDate_datepicker.SelectedDate >= Convert.ToDateTime(R.Field<DateTime>("Start date").ToString()))
                            {
                                MessageBox.Show("There is an overlap between the entered dates and previously specified dates.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                return;
                                //throw new Exception("There is an overlap between the entered dates and previously specified dates.");
                            }
                        }
                    }
                }
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }

                if (EndDate_datepicker.SelectedDate == null || BeginDate_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please specify the start and end date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }

                if (EndDate_datepicker.SelectedDate < BeginDate_datepicker.SelectedDate)
                {
                    MessageBox.Show("Please make sure the end date is after the start date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }

                if (From_comboBox.SelectedIndex == -1 || To_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the Currencies (from - to)", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
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
                                MessageBox.Show("Exchange to SYR can only be made from the budget currency.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                return;

                            }
                        }
                    }
                }


                if (Login.BuildConnection())
                {
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


                    if (ID != -1)//Edit
                    {

                        r.Update(ID, Account_ID, Name, beginDate, endDate, from, to, ObserveRate_ID, "Rate_Window");
                    }
                    else//Add new
                    {
                        if (!Add_privileges)
                        {
                            MessageBox.Show("Sorry … You do not have add permissions.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                            return;
                            //throw new Exception("Sorry … You do not have add permissions.");
                        }
                        r.Insert(Account_ID, Name, beginDate, endDate, from, to, ObserveRate_ID, "Rate_Window");
                    }

                    LoadGrid(false);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }

        private void ClearBoxes()
        {
            ID = -1;
            Name_txtBox.Text = Value_txtBox.Text = "";
            From_comboBox.SelectedIndex = -1; To_comboBox.SelectedIndex = -1;
            BeginDate_datepicker.SelectedDate = null; EndDate_datepicker.SelectedDate = null;
            ObserveRate_comboBox.SelectedIndex = -1;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                ID = Convert.ToInt32(dataRow["ID"]);

                if (dataRow["Rate Group name"] != DBNull.Value || dataRow["Rate Group name"] != null)
                    Name_txtBox.Text = dataRow["Rate Group name"].ToString();

                BeginDate_datepicker.SelectedDate = Convert.ToDateTime(dataRow["Start date"].ToString());
                EndDate_datepicker.SelectedDate = Convert.ToDateTime(dataRow["End date"].ToString());

                From_comboBox.SelectedValue = Convert.ToInt32(dataRow["fromtp_id"].ToString());
                To_comboBox.SelectedValue = Convert.ToInt32(dataRow["totp_id"].ToString());

                if (dataRow["Value"] != DBNull.Value) Value_txtBox.Text = dataRow["Value"].ToString();

                LoadObserveRate_comboBox();

                if (dataRow["ObserveRate_ID"] != DBNull.Value)
                    ObserveRate_comboBox.SelectedValue = Convert.ToInt32(dataRow["ObserveRate_ID"].ToString());
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                    ID = Convert.ToInt32(dataRow[0].ToString());
                    string Rate_Name = dataRow["Rate Group name"].ToString();
                    r.Delete(ID, Account_ID, Rate_Name, "Rate_Window");

                    ID = -1;
                    LoadGrid(false);
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                { MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearBoxes();
                Load_Data();
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

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearBoxes();
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

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;

                All_textBlock.Text = myDataGrid.View.Records.Count().ToString();
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

        private void myDataGrid_AutoGeneratingColumn(object sender, Syncfusion.UI.Xaml.Grid.AutoGeneratingColumnArgs e)
        {
            try
            {
                if (e.Column is GridNumericColumn && e.Column.MappingName != "ID"
                    && !e.Column.MappingName.Contains("_ID") && !e.Column.MappingName.Contains("_id"))
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;

                //HIDE ID column.
                if (e.Column.MappingName == "ID" || e.Column.MappingName.Contains("_ID") || e.Column.MappingName.Contains("_id"))
                    e.Cancel = true;

                if (ShowTotals)
                {
                    if (Donor_dt != null)
                    {
                        var BudgetDononr = Donor_dt.AsEnumerable().Select(row => row.Field<string>("Name"));
                        if (BudgetDononr.Any(name => e.Column.MappingName.Contains(name)))
                        {
                            e.Cancel = true;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_ItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e)
        {
            try
            {
                if (myDataGrid.ItemsSource != null)
                {
                    if (myDataGrid.StackedHeaderRows.Count != 0)
                        myDataGrid.StackedHeaderRows.RemoveAt(0);

                    if (myDataGrid.ItemsSource != null && !ShowTotals)
                    {
                        add_stackedHeader(myDataGrid);
                    }
                }
                Bind_Show_Hide_Menu();
                move_Columns();
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

        private void Total_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = true;
                if (UserMode)
                {
                    LoadGrid(true);
                }
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

        private void PerDonor_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = false;
                if (UserMode)
                {
                    LoadGrid(true);
                }
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

        private void add_stackedHeader(SfDataGrid MyDatagrid)
        {
            var BudgetDononr = Donor_dt.AsEnumerable().Select(row => row.Field<string>("Name"));

            StackedHeaderRow1 = new StackedHeaderRow { };

            for (int i = 0; i < MyDatagrid.Columns.Count; i++)
            {
                if (MyDatagrid.Columns[i].MappingName != null)
                {
                    int moved = 0;
                    if (!BudgetDononr.Any(name => MyDatagrid.Columns[i].MappingName.Contains(name)))
                    {
                        string StacHeader = MyDatagrid.Columns[i].MappingName;
                        var NewStackedColumn = new StackedColumn
                        {
                            HeaderText = StacHeader,
                            MappingName = StacHeader.Replace(" ", "")
                        };
                        if (MyDatagrid.Columns[i].MappingName.ToLower() != "rate group name"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "start date"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "end date"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "from till"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "to till"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "value"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "the corresponding rate group"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "the corresponding rate value")
                        {
                            moved = stack_and_goup_columns(i, StacHeader, NewStackedColumn, MyDatagrid);
                        }
                    }
                    i += moved;
                }
            }
            MyDatagrid.StackedHeaderRows.Add(StackedHeaderRow1);

            //Default GridStackedCellRenderer is removed.
            MyDatagrid.CellRenderers.Remove("StackedHeader");

            //Customized GridStackedCellRenderer is added.
            MyDatagrid.CellRenderers.Add("StackedHeader", new GridCustomStackedRenderer());
        }

        private int stack_and_goup_columns(int i, string StacHeader, StackedColumn CurrentStack, SfDataGrid MyDatagrid)
        {
            int moved = 0;

            for (int j = i; j < MyDatagrid.Columns.Count; j++)
            {
                if (MyDatagrid.Columns[j].MappingName.Contains(StacHeader))
                {
                    if(StacHeader.Contains("In Foreign currency") || StacHeader.Contains("in Foreign currency"))
                    {
                                                    // Add column to StackedColumn
                            if (CurrentStack.ChildColumns == "")
                                CurrentStack.ChildColumns = MyDatagrid.Columns[j].HeaderText;
                            else
                                CurrentStack.ChildColumns += "," + MyDatagrid.Columns[j].HeaderText;

                            // Rename column header
                            if (MyDatagrid.Columns[j].MappingName != StacHeader)
                            {
                                MyDatagrid.Columns[j].HeaderText = MyDatagrid.Columns[j].MappingName.Replace(StacHeader, "");
                                MyDatagrid.Columns.MoveTo(j, i);
                                moved++;
                            }
                            else
                            {
                                MyDatagrid.Columns[j].HeaderText = "Total";
                            }
                    }
                    else
                    {
                        if (!MyDatagrid.Columns[j].MappingName.Contains("In Foreign currency") && !MyDatagrid.Columns[j].MappingName.Contains("in Foreign currency"))
                        {
                            // Add column to StackedColumn
                            if (CurrentStack.ChildColumns == "")
                                CurrentStack.ChildColumns = MyDatagrid.Columns[j].HeaderText;
                            else
                                CurrentStack.ChildColumns += "," + MyDatagrid.Columns[j].HeaderText;

                            // Rename column header
                            if (MyDatagrid.Columns[j].MappingName != StacHeader)
                            {
                                MyDatagrid.Columns[j].HeaderText = MyDatagrid.Columns[j].MappingName.Replace(StacHeader, "");
                                MyDatagrid.Columns.MoveTo(j, i);
                                moved++;
                            }
                            else
                            {
                                MyDatagrid.Columns[j].HeaderText = "Total";
                            }
                        }
                    }            
                }
            }
            StackedHeaderRow1.StackedColumns.Add(CurrentStack);

            return moved;
        }

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
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
                if (!CHeader.Contains("ID") && !CHeader.Contains("id") && !CHeader.Contains("Delete") && !CHeader.Contains("Edit")
                    && !CHeader.Contains("Attachment") && !CHeader.Contains("#") && !Originally_HiddenColumns.Contains(CHeader))
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                    if (!CHeader.Contains("ID") && !CHeader.Contains("id") && !CHeader.Contains("Delete") && !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") && !CHeader.Contains("#") && !Originally_HiddenColumns.Contains(CHeader))
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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

    }
}
