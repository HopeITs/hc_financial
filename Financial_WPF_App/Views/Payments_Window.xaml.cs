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
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Payments_Window.xaml
    /// </summary>
    public partial class Payments_Window : Window
    {
        public Payments_Window(Main_Window_2 main_Window, string SubProject_ID)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            if (SubProject_ID != "")
                this.SubProject_ID = Int32.Parse(SubProject_ID);

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private int SubProject_ID;

        Main_Window_2 main_Window;

        private Beneficiary b; Payment p; Models.Action a;

        private DataTable dt, not_approved_dt, b_dt, cash_dt, cashAccounts_dt;

        double HOffset = 0; bool GridFilled = false;


        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["HopeRed"];


        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;
        #endregion

        private bool OnLoad()
        {
            try
            {
                b = new Beneficiary();
                p = new Payment();
                a = new Models.Action();

                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                    myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
                }

                myExpander.IsExpanded = false;

                Load_Data();
                Apply_Color_Theme();
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
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
            Set_privileges();
            Show_Hide();
            Fill_ComboBoxes();
            //LoadGrid();
        }

        private void Fill_ComboBoxes()
        {
            if (Login.BuildConnection())
            {
                DataTable temp_beneficiary_dt = b.Select("", "", SubProject_ID.ToString());
                if (Settings.Default.Role == "Accountant")
                {
                    List<int> UserAccounts_ID = new List<int>();
                    if (Settings.Default.User_Accounts != null && !string.IsNullOrEmpty(Settings.Default.User_Accounts.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_Accounts.ToString()))
                    {
                        UserAccounts_ID = Settings.Default.User_Accounts.ToString().Split(',').Select(int.Parse).ToList();

                        string filterExpression = string.Join(" OR ", UserAccounts_ID.Select(id => "Account_ID = " + id));
                        System.Data.DataRow[] rows = temp_beneficiary_dt.Select(filterExpression);
                        DataTable selected_dt = new DataTable();
                        if (rows.Length != 0)
                        {
                            selected_dt = rows.CopyToDataTable();
                            b_dt = selected_dt;

                        }
                    }
                    else
                    {
                        b_dt = null;
                    }

                }
                else
                {
                    b_dt = temp_beneficiary_dt;
                }

                if (b_dt != null && b_dt.Rows.Count > 0)
                {
                    List<string> IDItems = new List<string>();
                    List<string> Nametems = new List<string>();
                    if (b_dt != null && b_dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < b_dt.Rows.Count; i++)
                        {
                            IDItems.Add(b_dt.Rows[i].Field<Int32>("MicroProject_ID").ToString());
                            Nametems.Add(b_dt.Rows[i].Field<string>("Name").ToString());
                        }
                        MircoProject_ID_autoSuggestionUserControl.AutoSuggestionList = IDItems;
                    }


                    Amount_textBlock.Text = Login.ConvertToDouble(b_dt.Compute("Sum(Amount)", string.Empty).ToString());
                    ToPayAmount_textBlock.Text = Login.ConvertToDouble(b_dt.Compute("Sum(ToReturn_Amount)", string.Empty).ToString());
                    PaidAmount_textBlock.Text = Login.ConvertToDouble(b_dt.Compute("Sum(paidamount)", string.Empty).ToString());
                    if (PaidAmount_textBlock.Text == "")
                    {
                        PaidAmount_textBlock.Text = "0.00";
                    }
                    RemainingAmount_textBlock.Text = Login.ConvertToDouble((double.Parse(ToPayAmount_textBlock.Text) - double.Parse(PaidAmount_textBlock.Text)).ToString());
                }


            }
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
                Edit_privileges = Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Accountant")
            {
                Edit_privileges = Add_privileges = Delete_privileges = true;
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
                AddNewPayment_StackPanel.Visibility = AddMultiPayments_StackPanel.Visibility = Visibility.Collapsed;
            }
            if (!Edit_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Edit")
                    {
                        C.IsHidden = true;
                    }
            }
            if (!Delete_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Delete" || C.HeaderText.ToString() == "Select")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }

                Delete_Selected.Visibility = Visibility.Collapsed;
            }
        }

        public void LoadGrid()
        {
            string FromDate = ""; string ToDate = ""; int Microprject_ID = -1;


            if (FromDate_datepicker.SelectedDate != null && FromDate_datepicker.SelectedDate.ToString() != "")
                FromDate = ((DateTime)FromDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");


            if (ToDate_datepicker.SelectedDate != null && ToDate_datepicker.SelectedDate.ToString() != "")
                ToDate = ((DateTime)ToDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

            if (MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text != "")
                int.TryParse(MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text, out Microprject_ID);


            DataTable temp_dt = p.Select_All_Payments(SubProject_ID.ToString(), -1, FromDate, ToDate, Microprject_ID);
            DataTable Not_Approved_temp_dt = p.Select_All_Not_Approved_Payments(SubProject_ID.ToString(), false, -1, FromDate, ToDate, Microprject_ID);

            if (Settings.Default.Role == "Accountant")
            {
                List<int> UserAccounts_ID = new List<int>();
                if (Settings.Default.User_Accounts != null && !string.IsNullOrEmpty(Settings.Default.User_Accounts.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_Accounts.ToString()))
                {
                    UserAccounts_ID = Settings.Default.User_Accounts.ToString().Split(',').Select(int.Parse).ToList();

                    string filterExpression = string.Join(" OR ", UserAccounts_ID.Select(id => "beneficiary_Account_ID = " + id));
                    System.Data.DataRow[] rows = temp_dt.Select(filterExpression);
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        dt = selected_dt;
                    }
                }
                else
                {
                    dt = null;
                }
            }
            else
            {
                dt = temp_dt;
            }

            if (Settings.Default.Role == "Accountant")
            {
                List<int> UserAccounts_ID = new List<int>();
                if (Settings.Default.User_Accounts != null && !string.IsNullOrEmpty(Settings.Default.User_Accounts.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_Accounts.ToString()))
                {
                    UserAccounts_ID = Settings.Default.User_Accounts.ToString().Split(',').Select(int.Parse).ToList();

                    string filterExpression = string.Join(" OR ", UserAccounts_ID.Select(id => "beneficiary_Account_ID = " + id));
                    System.Data.DataRow[] rows = Not_Approved_temp_dt.Select(filterExpression);
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        not_approved_dt = selected_dt;
                    }
                }
                else
                {
                    not_approved_dt = null;
                }
            }
            else
            {
                not_approved_dt = Not_Approved_temp_dt;
            }

            if (dt != null)
            {
                System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
                dt.Columns.Add(newColumn);
                dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));
            }


            if (not_approved_dt != null)
            {
                System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
                not_approved_dt.Columns.Add(newColumn);
                not_approved_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));
            }

            Set_myDataGrid_ItemSource();


            Bind_Show_Hide_Menu();

            cash_dt = p.Select_Cash_Payments(SubProject_ID);
            if (cash_dt != null && cash_dt.Rows.Count > 0)
            {
                Cash_DataGrid.ItemsSource = cash_dt.DefaultView;
            }
            else
            {
                Cash_DataGrid.ItemsSource = null;
            }
        }

        private void Set_myDataGrid_ItemSource()
        {
            try
            {
                if (NotApproved_RadioButton.IsChecked == false)
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        All_textBlock.Text = dt.Rows.Count.ToString();
                        myDataGrid.ItemsSource = dt.DefaultView;
                    }
                    else
                    {
                        myDataGrid.ItemsSource = null;
                        All_textBlock.Text = "0";
                    }
                }
                else
                {
                    if (not_approved_dt != null && not_approved_dt.Rows.Count > 0)
                    {
                        myDataGrid.ItemsSource = not_approved_dt.DefaultView;
                    }
                    else
                    {
                        myDataGrid.ItemsSource = null;
                    }
                }
                if (myDataGrid.ItemsSource != null)
                {
                    if (HOffset != 0)
                        this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
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

        private void AddNewPayment_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_Payment_Window _Window = new Add_Payment_Window(SubProject_ID, main_Window);
                bool? dialogResult = _Window.ShowDialog();
                if (dialogResult == true)
                {
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void AddMultiPayments_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiPayments_Windows _Window = new Add_MultiPayments_Windows(SubProject_ID, this, main_Window);
                _Window.ShowDialog();

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
        }

        private void NotApproved_RadioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RadioButton radioButton = sender as RadioButton;

                // If the RadioButton was checked before the click, this will uncheck it
                if (bool.Parse((string)radioButton.Tag))
                {
                    radioButton.IsChecked = false;
                }

                // Store the current state of the RadioButton in its Tag property
                radioButton.Tag = radioButton.IsChecked.ToString();


                Set_myDataGrid_ItemSource();

                if (myDataGrid.View != null)
                {
                    Export_btn.CommandTarget = myDataGrid;
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;

                int Action_ID = Convert.ToInt32(dataRow["ID"].ToString());

                bool Approved_Action;
                if (NotApproved_RadioButton.IsChecked == false)
                    Approved_Action = true;
                else { Approved_Action = false; }

                int Till_ID = Convert.ToInt32(dataRow["Till_ID"].ToString());

                Add_Payment_Window _Window = new Add_Payment_Window(SubProject_ID, Action_ID, Approved_Action, main_Window);
                bool? dialogResult = _Window.ShowDialog();
                if (dialogResult == true)
                {
                    Load_Data();
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

        private void myDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Edit_privileges)
                {
                    IInputElement element = e.MouseDevice.DirectlyOver;
                    if (element != null && element is FrameworkElement)
                    {
                        if (((FrameworkElement)element).Parent is GridCell)
                        {
                            var grid = sender as SfDataGrid;
                            if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                            {
                                var dataRow = grid.SelectedItem as dynamic;


                                int Action_ID = Convert.ToInt32(dataRow["ID"].ToString());

                                bool Approved_Action;
                                if (NotApproved_RadioButton.IsChecked == false)
                                    Approved_Action = true;
                                else { Approved_Action = false; }

                                int Till_ID = Convert.ToInt32(dataRow["Till_ID"].ToString());


                                Add_Payment_Window _Window = new Add_Payment_Window(SubProject_ID, Action_ID, Approved_Action, main_Window);
                                bool? dialogResult = _Window.ShowDialog();
                                if (dialogResult == true)
                                {
                                    Load_Data();
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                    int Benef_ID = -1;

                    int.TryParse(dataRow["beneficiary_ID"].ToString(), out Benef_ID);
                    int Action_ID = Convert.ToInt32(dataRow["ID"].ToString());


                    bool Approved_Action = false;

                    if (NotApproved_RadioButton.IsChecked == false)
                        Approved_Action = true;


                    p.Delete_Payment(Action_ID, Benef_ID, Approved_Action, "Payment_Window");

                    Action_ID = -1;
                    Approved_Action = false;
                    LoadGrid();
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

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    scrollViewer.LineLeft();
                }
                else
                {
                    scrollViewer.LineRight();
                }
            }
            else
            {
                if (e.Delta < 0)
                {
                    scrollViewer.LineDown();
                }
                else
                {
                    scrollViewer.LineUp();
                }
            }
            e.Handled = true;
        }

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;
        }

        private void Amount_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Amount_textBlock.ActualWidth > PaidAmount_textBlock1.ActualWidth)
                PaidAmount_textBlock1.MinWidth = Amount_textBlock.ActualWidth;
            else
                Amount_textBlock.MinWidth = PaidAmount_textBlock1.ActualWidth;
        }

        private void Cash_DataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            try
            {
                if (e.Column is GridNumericColumn)
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                if (e.Column.MappingName == "CashAccount_id")
                    e.Cancel = true;

                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;

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

        private void Cash_DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (Cash_DataGrid.View != null)
            {
                Cash_Export_btn.CommandTarget = Cash_DataGrid;
            }
        }

        private void myExpander_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid_col.Width = new GridLength(60, GridUnitType.Star);
                Expander_col.Width = new GridLength(40, GridUnitType.Star);
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

        private void myExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid_col.Width = new GridLength(95, GridUnitType.Star);
                Expander_col.Width = new GridLength(5, GridUnitType.Star);
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GridFilled)
                    LoadGrid();
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

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        void ScrollOwner_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange > 0 || e.HorizontalChange < 0)
            {
                HOffset = e.HorizontalOffset;
            }
        }

        private void Go_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GridFilled = true;
                LoadGrid();
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


        private CheckBox headerCheckbox;

        private void chkHeader_Loaded(object sender, RoutedEventArgs e)
        {
            headerCheckbox = sender as CheckBox;
        }

        private void chkHeader_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataView view = (DataView)myDataGrid.ItemsSource;
                bool Checked = (bool)(sender as CheckBox).IsChecked;

                if (view != null)
                {
                    foreach (DataRowView row in view)
                    {
                        row["IsSelected"] = Checked;
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

        private void Delete_Selected_TextBlock_Click(object sender, RoutedEventArgs e)
        {
            Delete_Selected_TextBlock.IsEnabled = false;
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    Login.DeleteMultiActions_sc.Parameters.Clear();
                    Login.DeleteMultiActions_List.Clear();

                    if (NotApproved_RadioButton.IsChecked == false)
                    {
                        DataView newView = new DataView(dt);
                        newView.RowFilter = "IsSelected = 1";

                        if (newView.Count == 0)
                        {
                            MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                            return;
                        }

                        for (int k = 0; k < newView.Count; k++)
                        {
                            string temp_query = "";
                            string log_query = "";

                            var Action_ID = newView[k]["ID"];
       
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@ID" + k, Action_ID);

                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Form_Name" + k, "Payment_Window");

                            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);


                            temp_query += "INSERT INTO action_deleted SELECT * FROM action WHERE action.id = @ID" + k + " OR action.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "Update action_deleted set `deleted_at` = @Todaydate" + k + ", `deleted_by` = @Uesr_ID" + k + " WHERE action_deleted.id = @ID" + k + " OR action_deleted.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action WHERE action.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action WHERE action.id  = @ID" + k + ";";


                            Login.DeleteMultiActions_List.Add(temp_query);
                            temp_query = "";

                        }

                        a.Delete_Multi_Actions();

                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                        LoadGrid();
                    }
                    else
                    {
                        DataView newView = new DataView(not_approved_dt);
                        newView.RowFilter = "IsSelected = 1";

                        if (newView.Count == 0)
                        {
                            MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                            return;
                        }

                        for (int k = 0; k < newView.Count; k++)
                        {
                            string temp_query = "";
                            string log_query = "";

                            var Action_ID = newView[k]["ID"];

                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@ID" + k, Action_ID);

                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Form_Name" + k, "Payment_Window");

                            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);


                            temp_query += "INSERT INTO action_deleted SELECT * FROM action_not_approved WHERE action_not_approved.id = @ID" + k + " OR action_not_approved.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "Update action_deleted set `deleted_at` = @Todaydate" + k + ", `deleted_by` = @Uesr_ID" + k + " WHERE action_deleted.id = @ID" + k + " OR action_deleted.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action_not_approved WHERE action_not_approved.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action_not_approved WHERE action_not_approved.id  = @ID" + k + ";";


                            Login.DeleteMultiActions_List.Add(temp_query);
                            temp_query = "";

                        }

                        a.Delete_Multi_Actions();

                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                        LoadGrid();
                    }
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
            finally
            {
                Delete_Selected_TextBlock.IsEnabled = true;
            }

        }

        private void myDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
        {
            try
            {
                int count = myDataGrid.SelectedItems.Count;
                Selected_textBlock.Text = count.ToString("N0");

                double selected_amount = 0;
                for (var i = 0; i < count; i++)
                {
                    DataRowView SelectedDataRow = (DataRowView)myDataGrid.SelectedItems[i];
                    if (double.Parse(SelectedDataRow["Income"].ToString()) != 0)
                    {
                        selected_amount += double.Parse(SelectedDataRow["Income"].ToString());
                    }
                }
                PaidAmount_textBlock1.Text = selected_amount.ToString("N2");
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
                    if (myDataGrid.Columns[i].IsHidden == false)
                    {
                        NewItem.Click += new RoutedEventHandler(HideColumn);
                        this.HideMenu.Items.Add(NewItem);
                    }
                    else
                    {
                        NewItem.Click += new RoutedEventHandler(ShowColumn);
                        this.ShowMenu.Items.Add(NewItem);
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

                        break;
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

                        break;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void MircoProject_ID_autoSuggestionUserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text != "")
                {
                    string MircoProject_ID = MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text;
                    System.Data.DataRow[] rows = b_dt.Select("MicroProject_ID=" + MircoProject_ID);
                    if (rows.Length == 0)
                    {
                        MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = "";
                    }
                }

                MircoProject_ID_autoSuggestionUserControl.CloseAutoSuggestionBox();
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

    }
}
