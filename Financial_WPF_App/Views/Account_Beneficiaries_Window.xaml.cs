using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    public partial class Account_Beneficiaries_Window : Window
    {
        public Account_Beneficiaries_Window(int Account_ID, int SubProject_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();

            //

            this.Account_ID = Account_ID;
            this.main_Window = main_Window;
            this.SubProject_ID = SubProject_ID;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private int Account_ID, SubProject_ID;
        private Beneficiary b;
        private Till t;
        private DataTable dt, account_dt, till_dt;
        private Payment p;
        Main_Window_2 main_Window;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["HopeRed"];


        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;

        #endregion

        private bool OnLoad()
        {
            try
            {
                b = new Beneficiary();
                t = new Till();
                p = new Payment();
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
                Edit_privileges = true;
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
                Add_btn.Visibility = Visibility.Collapsed;
                AddMultiBeneficiary_btn.Visibility = Visibility.Collapsed;
            }
            if (!Edit_privileges)
            {
                Postpone_StackPanel.Visibility = Visibility.Collapsed;
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Approved" || C.HeaderText.ToString() == "Attachment" || C.HeaderText.ToString() == "Edit")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString().Trim());
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

        public void Load_Data()
        {
            Set_privileges();
            Show_Hide();
            if (Login.BuildConnection())
            {
                sum_imports_lbl.Content = sum_BAmounts_lbl.Content = sum_BAmounts_lbl.Content = sum_RAmounts_lbl.Content =
                         sum_RemainedAmounts_lbl.Content = sum_NetAmounts_lbl.Content = sum_RemainedAfterPay_lbl.Content =
                            avg_amounts_lbl.Content = avg_amounts_usd_lbl.Content = num_Done_lbl.Content = num_Ongoing_lbl.Content = num_Allprojects_lbl.Content = "0.0";


                myExpander.Visibility = Visibility.Hidden;
                Grid_col.Width = new GridLength(100, GridUnitType.Star);
                Expander_col.Width = new GridLength(0, GridUnitType.Star);

                Fill_Till_comboBox();
                Fill_Grid();
                if (Account_ID != -1)
                {
                    Fill_AccountView();
                    myExpander.Visibility = Visibility.Visible;
                    Grid_col.Width = new GridLength(95, GridUnitType.Star);
                    Expander_col.Width = new GridLength(5, GridUnitType.Star);
                }

            }
        }

        private void Attachment_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                int Beneficiary_ID = Convert.ToInt32(dataRow["beneficiary_ID"].ToString());
                Window _Window = new Attachments(-1, Beneficiary_ID, false, "Project", "Micro Projects", null, this, null);
                _Window.ShowDialog();
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                int Beneficiary_ID = Convert.ToInt32(dataRow["beneficiary_ID"].ToString());
                int SubProject_ID = Convert.ToInt32(dataRow["subproject_ID"].ToString());

                Beneficiary_Window beneficiary_Window = new Beneficiary_Window(Beneficiary_ID, SubProject_ID, main_Window);
                bool? dialogResult = beneficiary_Window.ShowDialog();
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "confirmation", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    var dialogResult1 = MessageBox.Show("Alert!!! All operations and payments related to this beneficiary will be erased … Do you want to proceed??", "confirmation", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                    if (dialogResult1 == MessageBoxResult.Yes)
                    {

                        DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                        int Beneficiary_ID = Convert.ToInt32(dataRow["beneficiary_ID"].ToString());
                        int SubProject_ID = Convert.ToInt32(dataRow["subproject_ID"].ToString());
                        string Name = dataRow["Beneficiary"].ToString();
                        int Account_ID = Convert.ToInt32(dataRow["Accoun_ID"].ToString());

                        b.Delete(Beneficiary_ID, SubProject_ID.ToString(), Name, Account_ID, "Account_Beneficiaries_Window");

                        Load_Data();
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

        private void Fill_AccountView()
        {
            account_dt = b.Select_account_view(Account_ID.ToString());
            if (account_dt != null && account_dt.Rows.Count > 0)
            {

                if (account_dt.Rows[0]["Total income"] != DBNull.Value)
                    sum_imports_lbl.Content = account_dt.Rows[0].Field<double>("Total income").ToString("N", new CultureInfo("en-US"));

                if (account_dt.Rows[0]["Total loans amount"] != DBNull.Value)
                    sum_BAmounts_lbl.Content = account_dt.Rows[0].Field<double>("Total loans amount").ToString("N", new CultureInfo("en-US"));

                if (account_dt.Rows[0]["Total invoices amount"] != DBNull.Value)
                    sum_RAmounts_lbl.Content = account_dt.Rows[0].Field<double>("Total invoices amount").ToString("N", new CultureInfo("en-US"));

                if (account_dt.Rows[0]["Total unbilled amounts"] != DBNull.Value)
                    sum_RemainedAmounts_lbl.Content = account_dt.Rows[0].Field<double>("Total unbilled amounts").ToString("N", new CultureInfo("en-US"));

                if (account_dt.Rows[0]["Net account"] != DBNull.Value)
                    sum_NetAmounts_lbl.Content = account_dt.Rows[0].Field<double>("Net account").ToString("N", new CultureInfo("en-US"));

                if (account_dt.Rows[0]["Remaining after paying"] != DBNull.Value)
                    sum_RemainedAfterPay_lbl.Content = account_dt.Rows[0].Field<double>("Remaining after paying").ToString("N", new CultureInfo("en-US"));

                if (account_dt.Rows[0]["Average micro project cost"] != DBNull.Value)
                    avg_amounts_lbl.Content = account_dt.Rows[0].Field<double>("Average micro project cost").ToString("N", new CultureInfo("en-US"));
            }
        }

        public void Fill_Grid()
        {
            DataTable temp_dt = b.Select_Account_beneficiares(Account_ID, SubProject_ID);
            if (Settings.Default.Role == "Accountant")
            {
                List<int> UserAccounts_ID = new List<int>();
                if (Settings.Default.User_Accounts != null && !string.IsNullOrEmpty(Settings.Default.User_Accounts.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_Accounts.ToString()))
                {
                    UserAccounts_ID = Settings.Default.User_Accounts.ToString().Split(',').Select(int.Parse).ToList();

                    string filterExpression = string.Join(" OR ", UserAccounts_ID.Select(id => "Accoun_ID = " + id));
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

            if (dt != null)
            {
                System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
                dt.Columns.Add(newColumn);
                dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));
            }

            if (dt != null && dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = dt.DefaultView;

                num_Done_lbl.Content = dt.Compute("count([Financed])", "[Financed]='Done'");
                num_Ongoing_lbl.Content = dt.Compute("count([Financed])", "[Financed]='Ongoing'");
                num_Allprojects_lbl.Content = dt.Rows.Count.ToString();

                if (HOffset != 0)
                    this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);

                move_Columns();

            }
            else
            {
                myDataGrid.ItemsSource = null;

                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].IsHidden = true;
                }

                num_Done_lbl.Content = "0";
                num_Ongoing_lbl.Content = "0";
                num_Allprojects_lbl.Content = "0";
            }
            Bind_Show_Hide_Menu();
        }

        private void Fill_Till_comboBox()
        {
            Till_comboBox.ItemsSource = null;
            till_dt = t.Select(Account_ID.ToString(), -1, "syr");
            if (till_dt != null && till_dt.Rows.Count > 0)
            {
                Till_comboBox.ItemsSource = till_dt.DefaultView;
            }
        }

        private void myExpander_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid_col.Width = new GridLength(70, GridUnitType.Star);
                Expander_col.Width = new GridLength(30, GridUnitType.Star);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
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

        private void Till_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (Till_comboBox.SelectedValue != null)
                {
                    var Amount = dt.Compute("Sum([Total invoices amount " + Till_comboBox.SelectedValue + "])", string.Empty);
                    var Num = dt.Compute("count([Total invoices amount " + Till_comboBox.SelectedValue + "])", "[Total invoices amount " + Till_comboBox.SelectedValue + "]<>'0.00'");

                    float Amount1 = float.Parse(Amount.ToString());
                    int Num1 = int.Parse(Num.ToString());

                    avg_amounts_usd_lbl.Content = Login.ConvertToDouble((Amount1 / Num1).ToString());
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

        private void myDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
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
                            int B_ID = Convert.ToInt32(rowview[0].ToString());

                            Beneficiary_Window beneficiary_Window = new Beneficiary_Window(B_ID, SubProject_ID, main_Window);
                            bool? dialogResult = beneficiary_Window.ShowDialog();
                            if (dialogResult == true)
                            {
                                Load_Data();
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

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;

                int count = myDataGrid.View.Records.Count();
                All_textBlock.Text = count.ToString("N0");

                Export_btn.CommandTarget = myDataGrid;
                move_Columns();
                Bind_Show_Hide_Menu();
            }
        }

        void ScrollOwner_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange > 0 || e.HorizontalChange < 0)
            {
                HOffset = e.HorizontalOffset;
            }
        }

        private void move_Columns()
        {
            if (myDataGrid.Columns.Count > 2)
            {
                bool EditMoved = false;
                bool DeleteMoved = false;
                bool AttachmentMoved = false;
                bool SelectMoved = false;
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
                    else if (!SelectMoved && myDataGrid.Columns[k].HeaderText == "Select")
                    {
                        if (!Originally_HiddenColumns.Contains("Select"))
                        {
                            SelectMoved = true; myDataGrid.Columns.MoveTo(i, myDataGrid.Columns.Count - 1); i--;
                        }
                        else
                            k++;
                    }
                }
            }
        }

        private void myDataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            try
            {
                if (e.Column is GridNumericColumn && e.Column.MappingName != "Recovery percentage" && e.Column.MappingName != "Micro project number"
                    && e.Column.MappingName != "beneficiary_ID" && e.Column.MappingName != "subproject_ID" && e.Column.MappingName != "Accoun_ID")
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;

                //HIDE ID column.
                if (e.Column.MappingName == "ID" || e.Column.MappingName == "subproject_ID" || e.Column.MappingName == "beneficiary_ID" || e.Column.MappingName == "Accoun_ID")
                    e.Cancel = true;

                if (Account_ID != -1)
                {
                    if (e.Column.MappingName == "The account")
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

        private void NewBeneficiary_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                Beneficiary_Window beneficiary_Window = new Beneficiary_Window(SubProject_ID.ToString(), Account_ID, main_Window);
                bool? dialogResult = beneficiary_Window.ShowDialog();
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void AddMultiBeneficiary_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiBeneficiary_Windows _Window = new Add_MultiBeneficiary_Windows(SubProject_ID, this, main_Window);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
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

                if (dt != null)
                {
                    SearchDataGrid searchHelper = new SearchDataGrid(dt);

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

        private void monthes_txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Get PlaceholderTextBlock inside the TextBox template
                var placeholder = (TextBlock)monthes_txtBox.Template.FindName("PlaceholderText", monthes_txtBox);

                if (placeholder != null)
                {
                    // Hide placeholder when user types, show when empty
                    placeholder.Visibility = string.IsNullOrWhiteSpace(monthes_txtBox.Text) ? Visibility.Visible : Visibility.Collapsed;
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

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void ExactSearch_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Search_DataGrid();
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

        private void ExactSearch_checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                Search_DataGrid();
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

        private void Search_DataGrid()
        {
            DataTable selected_dt = new DataTable();
            selected_dt = dt.Copy();

            if (Search_txtBox.Text != "")
            {
                if (selected_dt != null)
                {
                    DataTable new_dt = selected_dt.Copy();
                    new_dt.Rows.Clear();

                    foreach (System.Data.DataRow r in selected_dt.Rows)
                    {

                        for (var i = 0; i < selected_dt.Columns.Count; i++)
                        {
                            if (r.ItemArray.ElementAt(i).ToString().ToLower().Contains(Search_txtBox.Text.ToLower()))
                            {
                                new_dt.Rows.Add(r.ItemArray);
                                break;
                            }
                            //if (ExactSearch_checkBox.IsChecked == true)
                            //{
                            //    if (r.ItemArray.ElementAt(i).ToString().ToLower() == Search_txtBox.Text.ToLower())
                            //    {
                            //        new_dt.Rows.Add(r.ItemArray);
                            //        break;
                            //    }
                            //}
                            //else
                            //{
                            //    if (r.ItemArray.ElementAt(i).ToString().ToLower().Contains(Search_txtBox.Text.ToLower()))
                            //    {
                            //        new_dt.Rows.Add(r.ItemArray);
                            //        break;
                            //    }
                            //}
                        }
                    }
                    selected_dt = new_dt.Copy();
                }
            }
            myDataGrid.ItemsSource = null;

            if (selected_dt != null)
            {
                myDataGrid.ItemsSource = selected_dt.DefaultView;
            }

            move_Columns();
        }

        private void delaypayment_btn_Click(object sender, RoutedEventArgs e)
        {
            delaypayment_btn.IsEnabled = false;
            try
            {
                int monthes;
                if (string.IsNullOrEmpty(monthes_txtBox.Text) || string.IsNullOrWhiteSpace(monthes_txtBox.Text) || !int.TryParse(monthes_txtBox.Text, out monthes))
                {
                    MessageBox.Show("Please make sure to enter the duration correctly before saving.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    List<int> Beneficiary_IDs = new List<int>();
                    Beneficiary_IDs = dt.Rows.OfType<System.Data.DataRow>()
                      .Select(dr => dr.Field<int?>("beneficiary_ID"))
                      .Where(i => i.HasValue)
                      .Select(i => i.Value)
                      .ToList();

                    p.Delay_Payments(monthes, Beneficiary_IDs);
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                    Fill_Grid();
                }
                else
                {
                    MessageBox.Show("No beneficiaries", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
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
                delaypayment_btn.IsEnabled = true;
            }
        }

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (e.Text != "")
                {
                    Regex regex = new Regex("[^0-9]+");
                    e.Handled = regex.IsMatch(e.Text);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Delete_Selected_btn_Click(object sender, RoutedEventArgs e)
        {
            Delete_Selected_btn.IsEnabled = false;

            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    Login.DeleteMultiBeneficiary_sc.Parameters.Clear();
                    Login.DeleteMultiBeneficiary_List.Clear();

                    DataView newView = new DataView(dt);
                    newView.RowFilter = "IsSelected = 1";

                    if (newView.Count == 0)
                    {
                        MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }

                    for (int k = 0; k < newView.Count; k++)
                    {
                        string temp_query = "";
                        string log_query = "";

                        var Beneficiary_ID = newView[k]["beneficiary_ID"];
                        var SubProject_ID = newView[k]["subproject_ID"];
                        var Name = newView[k]["Beneficiary"];
                        var Account_ID = newView[k]["Accoun_ID"];

                        Login.DeleteMultiBeneficiary_sc.Parameters.AddWithValue("@Beneficiary_ID" + k, Beneficiary_ID);
                        Login.DeleteMultiBeneficiary_sc.Parameters.AddWithValue("@SubProject_ID" + k, SubProject_ID);
                        Login.DeleteMultiBeneficiary_sc.Parameters.AddWithValue("@Name" + k, Name);
                        Login.DeleteMultiBeneficiary_sc.Parameters.AddWithValue("@Account_ID" + k, Account_ID);
                        Login.DeleteMultiBeneficiary_sc.Parameters.AddWithValue("@Form_Name" + k, "Account_Beneficiaries_Window");



                        temp_query += "SET @MicroProject_ID = (SELECT MicroProject_ID FROM beneficiary WHERE ID = @Beneficiary_ID" + k + ");";

                        temp_query += "DELETE action FROM action LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN account on till.Account_ID = account.ID WHERE action.MicroProject_ID =  @MicroProject_ID" + k + " AND account.SubProject_ID = @SubProject_ID" + k + ";";

                        temp_query += "DELETE action_not_approved FROM action_not_approved LEFT JOIN till ON action_not_approved.Till_ID = till.ID LEFT JOIN account on till.Account_ID = account.ID WHERE action_not_approved.MicroProject_ID =  @MicroProject_ID" + k + " AND account.SubProject_ID = @SubProject_ID" + k + ";";

                        temp_query += "DELETE FROM `beneficiary` WHERE ID = @Beneficiary_ID" + k + ";";

                        temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'DELETE', 'beneficiary', @Beneficiary_ID" + k + ", CONCAT('beneficiary Name = ', @Name" + k + "), CONCAT('account ID = ',@Account_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                        Login.DeleteMultiBeneficiary_List.Add(temp_query);
                        temp_query = "";

                    }

                    b.Delete_Multi_Beneficiaries();

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
            finally
            {
                Delete_Selected_btn.IsEnabled = true;
            }
        }

    }
}

