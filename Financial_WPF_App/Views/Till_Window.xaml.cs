using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;

namespace Financial_WPF_App.Views
{

    public partial class Till_Window : Window
    {
        public Till_Window(int Account_ID, bool IsAdminAccount, string Account_Name, string Account_Type, string Category, int SubProject_ID, int Till_ID, string TillType_Name, Account_Window account_Window, Main_Window_2 main_Window)
        {
            InitializeComponent();
            TillNotifier.RefreshTill += OnTillRefresh;
            //Main_Window_2.WindowStateChanged += OnMainWindowStateChanged; // Subscribe to event
            AsFiscalBond = false;
            this.main_Window = main_Window;
            this.account_Window = account_Window;
            this.Account_ID = Account_ID;
            this.Account_Name = Account_Name;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.SubProject_ID = SubProject_ID;
            this.Till_ID = Till_ID;
            this.TillType_Name = TillType_Name;
            this.IsAdminAccount = IsAdminAccount;
            Onload();
            HookContentVisibility();
        }

        public Till_Window(int FiscalBond_ID, string FiscalBond_Name, Main_Window_2 main_Window)
        {
            InitializeComponent();
            TillNotifier.RefreshTill += OnTillRefresh;
            //Main_Window_2.WindowStateChanged += OnMainWindowStateChanged; // Subscribe to event
            AsFiscalBond = true;
            this.main_Window = main_Window;
            this.FiscalBond_ID = FiscalBond_ID;
            this.FiscalBond_Name = FiscalBond_Name;
            myDataGrid.Margin = new Thickness(2, 5, 5, 5);
            Search_StackPanel.Margin = new Thickness(0, 0, 5, 0);
            DataContext = this;
            this.IsAdminAccount = false;

            if (Onload())
            {
                HookContentVisibility();
            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        Account_Window account_Window;
        int ID, Account_ID, Till_ID, SubProject_ID;
        string Account_Name, Account_Type, Category, TillType_Name;
        Till t;
        Models.Action a;
        private Beneficiary b;
        private Rate r;
        private Budget bud;
        private FiscalBond fiscalBond;
        private DataTable b_dt, dt, not_approved_dt, sum_dt, cash_dt, fb_dt; //Account_Budgets_dt;
        Main_Window_2 main_Window;
        bool IsAdminAccount;
        private WindowState _currentWindowState = WindowState.Maximized; // Track window state

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        bool GridLoaded = false;
        double HOffset = 0;
        private DataRowView _lastCurrentItem;

        int FiscalBond_ID;
        string FiscalBond_Name;
        bool AsFiscalBond;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";

        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;

        #endregion

        // TillNotifier.RefreshTill is a broadcast: saving one action told every Till_Window
        // ever built to reload. Those windows are never closed - only their Content is moved
        // into a TabItem - so the list only ever grows, and the content keeps them alive
        // through its event handlers, which is why unsubscribing on OnClosed never happened.
        //
        // Measured in a real session: one Action.Update was followed by 12-13 full grid
        // reloads, 466 s of the session's database time, 58% of the total. All but one of
        // those reloads were for tills sitting in a tab nobody was looking at.
        //
        // A hidden till now records that it is stale and reloads the moment it becomes
        // visible again, so what you see on screen is still always current.
        private bool _refreshPending;

        private void OnTillRefresh()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsContentVisible())
                    Load_Data();
                else
                    _refreshPending = true;
            });
        }

        private bool IsContentVisible()
        {
            FrameworkElement content = Content as FrameworkElement;

            // No content element to judge by - behave as before and reload.
            if (content == null)
                return true;

            return content.IsVisible;
        }

        private void HookContentVisibility()
        {
            FrameworkElement content = Content as FrameworkElement;
            if (content == null)
                return;

            content.IsVisibleChanged += (s, e) =>
            {
                if (!_refreshPending || !((FrameworkElement)s).IsVisible)
                    return;

                _refreshPending = false;
                Load_Data();
            };
        }

        private bool Onload()
        {
            try
            {
                b = new Beneficiary();
                t = new Till();
                a = new Models.Action();
                r = new Rate();
                bud = new Budget();
                fiscalBond = new FiscalBond();
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                    myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
                }

                myExpander_Collapsed(this, new RoutedEventArgs());
                //myExpander.IsExpanded = false;
                Balances.Visibility = Visibility.Hidden;

                // Open on the newest month the till has movements in. The server works out
                // which month that is inside the same query, so this costs one round trip.
                //
                // It used to be two: ask which months exist, then ask for that month's rows.
                // Measured, one round trip costs about as much as 1,500 rows of this grid, so
                // the extra trip cancelled the filter out on small tills and ate into the gain
                // on large ones.
                //
                // Only here, not in Load_Data: a refresh after saving must keep whatever
                // period the user is looking at.
                SetUpFilter();

                _openingOnNewest = true;
                Load_Data();
                _openingOnNewest = false;
                Apply_Color_Theme();
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    Apply_DataGrid_Color_Theme();
                }), DispatcherPriority.ApplicationIdle);
                myDataGrid.InvalidateVisual();

                //Apply_DataGrid_Color_Theme();
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

        private void Apply_DataGrid_Color_Theme()
        {
            if (Account_Type == "Project")
            {
                if (Category == "Micro Projects")
                {
                    HeaderStyle_Name = "HopeRed_SfDataGrid_header";
                    TableSummaryRowStyle_Name = "HopeRed_TableSummaryRowStyle";
                    GroupSummaryRowStyle = "HopeRed_GroupSummaryRowStyle";

                }
                else if (Category == "Budget")
                {
                    HeaderStyle_Name = "PastelDarkBlue_SfDataGrid_header";
                    TableSummaryRowStyle_Name = "PastelDarkBlue_TableSummaryRowStyle";
                    GroupSummaryRowStyle = "PastelDarkBlue_GroupSummaryRowStyle";
                }
            }
            else if (Account_Type == "Exchange")
            {
                HeaderStyle_Name = "Orange_SfDataGrid_header";
                TableSummaryRowStyle_Name = "Orange_TableSummaryRowStyle";
                GroupSummaryRowStyle = "Orange_GroupSummaryRowStyle";
            }
            else if (Account_Type == "Personal")
            {
                HeaderStyle_Name = "PastelGreen_SfDataGrid_header";
                TableSummaryRowStyle_Name = "PastelGreen_TableSummaryRowStyle";
                GroupSummaryRowStyle = "PastelGreen_GroupSummaryRowStyle";
            }
            else if (Account_Type == "Cash")
            {
                HeaderStyle_Name = "DarkgrayishViolet_SfDataGrid_header";
                TableSummaryRowStyle_Name = "DarkgrayishViolet_TableSummaryRowStyle";
                GroupSummaryRowStyle = "DarkgrayishViolet_GroupSummaryRowStyle";
            }

            myDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            myDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            myDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];

            //Cash_DataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            //Cash_DataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            //Cash_DataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }

        public void Load_Data()
        {
            if (AsFiscalBond)
            {
                fb_dt = fiscalBond.Select(FiscalBond_ID.ToString(), "", Properties.Settings.Default.User_ID.ToString(), "", true);

                Account_Type = fb_dt.Rows[0].Field<string>("accounttype");
                TillType_Name = fb_dt.Rows[0].Field<string>("Till");
                Account_ID = fb_dt.Rows[0].Field<int>("Account_ID");
                Account_Name = fb_dt.Rows[0].Field<string>("Account");
                Till_ID = fb_dt.Rows[0].Field<int>("Till_ID");
                SubProject_ID = fb_dt.Rows[0].Field<int>("SubProject_ID");
                Category = fb_dt.Rows[0].Field<string>("category");
               
            }

            //Account_Budgets_dt = bud.Select(Account_ID);
            Set_privileges();

            Show_Hide();

            ID = -1;
            LoadGrid();
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
                if (Account_Type == "Project")
                {
                    if (IsLocalTill(TillType_Name))
                    {
                        Edit_privileges = Add_privileges = Delete_privileges = true;
                    }
                    else
                    {
                        Add_privileges = true;
                    }
                }
                else if (Account_Type == "Personal")
                {
                    if (IsLocalTill(TillType_Name))
                    {
                        Edit_privileges = Add_privileges = Delete_privileges = true;
                    }
                }
                else if (Account_Type == "Exchange")
                {

                }
            }
            else if (Settings.Default.Role == "Accountant")
            {
                if (Account_Type == "Project")
                {
                    if (IsLocalTill(TillType_Name))
                    {
                        Edit_privileges = Add_privileges = Delete_privileges = true;
                    }
                }
                else if (Account_Type == "Personal")
                {
                    if (IsLocalTill(TillType_Name))
                    {
                        Edit_privileges = Add_privileges = Delete_privileges = true;
                    }
                }
                else if (Account_Type == "Exchange")
                {

                }
            }
            else if (Settings.Default.Role == "Cashier")
            {

            }
            else if (Settings.Default.Role == "Employee")
            {
                if (AsFiscalBond)
                    Add_privileges = true;
            }
            else if (Settings.Default.Role == "Donor")
            {

            }
        }

        private void Show_Hide()
        {
            if (AsFiscalBond)
            {
                Net_lable.Visibility = Net_Border.Visibility = CashNet_label.Visibility = CashNet_Border.Visibility = Visibility.Collapsed;
            }
            else
            {
                Net_lable.Visibility = Net_Border.Visibility = CashNet_label.Visibility = CashNet_Border.Visibility = Visibility.Visible;
            }

            if (!Add_privileges)
            {
                AddNewAction_btn.Visibility = Visibility.Collapsed;
                AddMultiAction_btn.Visibility = Visibility.Collapsed;
                AddMultiAttachments_btn.Visibility = Visibility.Collapsed;

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
                    if (C.HeaderText.ToString() == "Delete" || C.HeaderText.ToString() == "Select")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }

                Delete_Selected.Visibility = Visibility.Collapsed;
            }

            // Hide expenses & exchange for PAY till type
            if (IsPayTill())
            {
                // A pay till holds both local currencies, so the single Income and
                // Transfer columns are swapped for one pair per currency.
                ConfigurePayCurrencyColumns();

                // A count of rows, not an amount, and it never was in one currency.
                All_Label.Content = "All(Local):";
                USD_StatsPanel.Visibility = Visibility.Visible;

                string oldName = Helper.CurrencyService.GetLegacyLocalCurrencyName();
                string newName = Helper.CurrencyService.GetCurrentLocalCurrencyName();
            if (string.IsNullOrWhiteSpace(newName)) newName = "New";
                Income_Label.Content = "Income(" + oldName + "):";
                Transfered_Label.Content = "Transfered(" + oldName + "):";
                PayIncomeCurrent_Label.Content = "Income(" + newName + "):";
                PayTransferCurrent_Label.Content = "Transfered(" + newName + "):";
                PayIncomeUnified_Label.Content = "Total Income(" + newName + "):";
                PayTransferUnified_Label.Content = "Total Transfered(" + newName + "):";
                PayIncomeCurrent_Panel.Visibility = PayTransferCurrent_Panel.Visibility =
                PayIncomeUnified_Panel.Visibility = PayTransferUnified_Panel.Visibility = Visibility.Visible;

                Exchange_Label.Visibility = Visibility.Collapsed;
                Exchange_textBlock.Visibility = Visibility.Collapsed;

                Exchpand_Label.Visibility = Visibility.Collapsed;
                Exchpand_textBlock.Visibility = Visibility.Collapsed;

                Exchpand_Labelusd.Visibility = Visibility.Collapsed;
                Exchpand_textBlockusd.Visibility = Visibility.Collapsed;

                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" || C.HeaderText.ToString() == "Exchange" || C.HeaderText.ToString() == "Document type"
                        || C.HeaderText.ToString() == "Donor" || C.HeaderText.ToString() == "Volunteer" || C.HeaderText.ToString() == "Expenses(USD)" || C.HeaderText.ToString() == "Expenses" ||
                        C.HeaderText.ToString() == "Document number" || C.HeaderText.ToString() == "Corresponding account" || C.HeaderText.ToString() == "Corresponding till" || C.HeaderText.ToString() == "Corresponding Operation ID"
                            || C.HeaderText.ToString() == "Corresponding cash till" || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till"
                        // Bound to "Outcomeusd", which this screen's query does not select
                        // at all, so the column has always drawn empty.
                        || C.HeaderText.ToString() == "Transfer(USD)")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }
            }

            if (Account_Type == "Project")
            {
                if (IsLocalTill(TillType_Name))
                {
                    if (Category == "Micro Projects")
                    {
                        All_Label.Content = "All(" + TillType_Name + "):";
                        USD_StatsPanel.Visibility = Visibility.Visible;
                        Exchange_Label.Visibility = Visibility.Collapsed;
                        Exchange_textBlock.Visibility = Visibility.Collapsed;
                        Transfered_textBlockusd.Visibility = Visibility.Collapsed;
                        Transfered_Labelusd.Visibility = Visibility.Collapsed;

                        foreach (var C in myDataGrid.Columns)
                            if (C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" || C.HeaderText.ToString() == "Exchange"
                                || C.HeaderText.ToString() == "Donor" || C.HeaderText.ToString() == "Volunteer" || C.HeaderText.ToString() == "Transfer(USD)")
                            {
                                C.IsHidden = true;
                                Originally_HiddenColumns.Add(C.HeaderText.ToString());
                            }
                    }
                    else if (Category == "Budget")
                    {
                        All_Label.Content = "All(" + TillType_Name + "):";
                        USD_StatsPanel.Visibility = Visibility.Visible;
                        Exchange_Label.Visibility = Visibility.Collapsed;
                        Exchange_textBlock.Visibility = Visibility.Collapsed;
                        Transfered_textBlockusd.Visibility = Visibility.Collapsed;
                        Transfered_Labelusd.Visibility = Visibility.Collapsed;

                        foreach (var C in myDataGrid.Columns)
                            if (C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Exchange" || C.HeaderText.ToString() == "Transfer(USD)")
                            {
                                C.IsHidden = true;
                                Originally_HiddenColumns.Add(C.HeaderText.ToString());
                            }
                    }
                    Balances.Visibility = Visibility.Visible;
                }
                else
                {
                    //myExpander.Visibility = Visibility.Collapsed;
                    //myExpander.Width = 0;
                    Balances.Visibility = Visibility.Hidden;
                    myDataGrid.Margin = new Thickness(2, 5, 35, 5);
                    Search_StackPanel.Margin = new Thickness(0, 0, 35, 0);
                    Exchpand_Label.Visibility = Visibility.Collapsed;
                    Exchpand_Border.Visibility = Visibility.Collapsed;
                    Transfered_textBlockusd.Visibility = Visibility.Collapsed;
                    Transfered_Labelusd.Visibility = Visibility.Collapsed;

                    //  Outcome_Label.Visibility = Outcome_textBlock.Visibility = Outcome_Border.Visibility =
                    //    Returned_Label.Visibility = Returned_textBlock.Visibility = Returned_Border.Visibility = Visibility.Collapsed;

                    // Outcome_Label1.Visibility = Outcome_textBlock1.Visibility =
                    //  Returned_Label1.Visibility = Returned_textBlock1.Visibility = Returned_Border1.Visibility = Visibility.Collapsed;


                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" ||
                            C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                            C.HeaderText.ToString() == "Document type" || C.HeaderText.ToString() == "Document number" || C.HeaderText.ToString() == "Expenses" || C.HeaderText.ToString() == "Transfer(USD)"
                            || C.HeaderText.ToString() == "Volunteer" || C.HeaderText.ToString() == "Income(USD)" || C.HeaderText.ToString() == "Expenses(USD)" || C.HeaderText.ToString() == "Currency" )
                        {
                            C.IsHidden = true;
                            Originally_HiddenColumns.Add(C.HeaderText.ToString());
                        }

                    if (TillType_Name.ToLower() == "eur")
                    {
                        foreach (var C in myDataGrid.Columns)
                            if (C.HeaderText.ToString() == "Transfer" || C.HeaderText.ToString() == "Transfer(USD)")
                            {
                                C.IsHidden = true;
                                Originally_HiddenColumns.Add(C.HeaderText.ToString());
                            }
                        Transfered_Label.Visibility = Visibility.Collapsed;
                        Transfered_textBlock.Visibility = Visibility.Collapsed;
                        Transfered_textBlockusd.Visibility = Visibility.Collapsed;
                        Transfered_Labelusd.Visibility = Visibility.Collapsed;
                    }

                    //var colOutcome = myDataGrid.Columns.FirstOrDefault(c => c.MappingName == "Expand");
                    //if (colOutcome != null)
                    //    colOutcome.HeaderText = "Transfer";
                }
            }


            if (Account_Type == "Exchange")
            {
               // Outcome_Label.Visibility = Outcome_textBlock.Visibility = Outcome_Border.Visibility =
                //Returned_Label.Visibility = Returned_textBlock.Visibility = Returned_Border.Visibility = Visibility.Collapsed;

               // Outcome_Label1.Visibility = Outcome_textBlock1.Visibility =
              //  Returned_Label1.Visibility = Returned_textBlock1.Visibility = Returned_Border1.Visibility = Visibility.Collapsed;
                if (!IsLocalTill(TillType_Name))
                {
                    //myExpander.Visibility = Visibility.Collapsed;
                    //myExpander.Width = 0;
                    Balances.Visibility = Visibility.Hidden;
                    myDataGrid.Margin = new Thickness(2, 5, 5, 5);
                    Search_StackPanel.Margin = new Thickness(0, 0, 5, 0);
                    Transfered_textBlockusd.Visibility = Visibility.Collapsed;
                    Transfered_Labelusd.Visibility = Visibility.Collapsed;
                }
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" || C.HeaderText.ToString() == "Transfer(USD)" || 
                        C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                        C.HeaderText.ToString() == "Document type" || C.HeaderText.ToString() == "Document number" ||
                        C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount" || C.HeaderText.ToString() == "Value"
                        || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer"
                        || C.HeaderText.ToString() == "Income(USD)" || C.HeaderText.ToString() == "Expenses(USD)" || C.HeaderText.ToString() == "Currency" )
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }
            }


            if (Account_Type == "Personal")
            {
                Transfered_textBlockusd.Visibility = Visibility.Collapsed;
                Transfered_Labelusd.Visibility = Visibility.Collapsed;
                // Outcome_Label.Visibility = Outcome_textBlock.Visibility = Outcome_Border.Visibility =
                //   Returned_Label.Visibility = Returned_textBlock.Visibility = Returned_Border.Visibility =
                Exchange_Label.Visibility = Exchange_textBlock.Visibility = Exchange_Border.Visibility = Visibility.Collapsed;

              //  Outcome_Label1.Visibility = Outcome_textBlock1.Visibility =
              //  Returned_Label1.Visibility = Returned_textBlock1.Visibility = Returned_Border1.Visibility =
                //Exchange_Label1.Visibility = Exchange_textBlock1.Visibility = Exchange_Border1.Visibility = Visibility.Collapsed;

                if (IsLocalTill(TillType_Name))
                {
                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" || C.HeaderText.ToString() == "Exchange" || C.HeaderText.ToString() == "Transfer(USD)" || 
                            C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                            C.HeaderText.ToString() == "Donor" || C.HeaderText.ToString() == "Rate" || C.HeaderText.ToString() == "Exchanged" ||
                            C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount" || C.HeaderText.ToString() == "Value" ||
                            C.HeaderText.ToString() == "Corresponding till" || C.HeaderText.ToString() == "Corresponding account" || C.HeaderText.ToString() == "Corresponding cash till"
                            || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer"
                            || C.HeaderText.ToString() == "Income(USD)" || C.HeaderText.ToString() == "Expenses(USD)" || C.HeaderText.ToString() == "Currency")
                        {
                            C.IsHidden = true;
                            Originally_HiddenColumns.Add(C.HeaderText.ToString());
                        }
                    Balances.Visibility = Visibility.Visible;

                }
                else
                {
                    Transfered_textBlockusd.Visibility = Visibility.Collapsed;
                    Transfered_Labelusd.Visibility = Visibility.Collapsed;

                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" || C.HeaderText.ToString() == "Exchange" || C.HeaderText.ToString() == "Transfer(USD)" ||
                            C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                            C.HeaderText.ToString() == "Donor" || C.HeaderText.ToString() == "Rate" || C.HeaderText.ToString() == "Exchanged" || C.HeaderText.ToString() == "Cash till" ||
                            C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount" || C.HeaderText.ToString() == "Value" ||
                            C.HeaderText.ToString() == "Corresponding till" || C.HeaderText.ToString() == "Corresponding account" || C.HeaderText.ToString() == "Corresponding cash till"
                            || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer"
                             || C.HeaderText.ToString() == "Income(USD)" || C.HeaderText.ToString() == "Expenses(USD)" || C.HeaderText.ToString() == "Currency")
                        {
                            C.IsHidden = true;
                            Originally_HiddenColumns.Add(C.HeaderText.ToString());
                        }

                    //myExpander.Visibility = Visibility.Collapsed;
                    //myExpander.Width = 0;
                    Balances.Visibility = Visibility.Hidden;
                    myDataGrid.Margin = new Thickness(2, 5, 5, 5);
                    Search_StackPanel.Margin = new Thickness(0, 0, 5, 0);
                }
            }

            if (Account_Type == "Project" && IsLocalTill(TillType_Name))
            {
               
                    var colIncome = myDataGrid.Columns.FirstOrDefault(c => c.MappingName == "Income");
                    if (colIncome != null)
                        colIncome.HeaderText = "Income(" + TillType_Name + ")";

                    var colOutcome = myDataGrid.Columns.FirstOrDefault(c => c.MappingName == "Expand");
                    if (colOutcome != null)
                        colOutcome.HeaderText = "Expenses(" + TillType_Name + ")";
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Transfer" || C.HeaderText.ToString() == "Transfer(USD)")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }
                Transfered_Label.Visibility = Visibility.Collapsed;
                Transfered_textBlock.Visibility = Visibility.Collapsed;
                Transfered_textBlockusd.Visibility = Visibility.Collapsed;
                Transfered_Labelusd.Visibility = Visibility.Collapsed;

            }
        }

        #region period filter

        /// <summary>One month in the period picker.</summary>
        // Set only while the window is opening. It makes LoadGrid ask for "the newest month
        // this till has", which the server resolves inside the query, instead of the screen
        // having to find that out in a round trip of its own first.
        private bool _openingOnNewest;

        // The month list, the tick boxes, the caption and the clear button were written
        // out here. They are now Controls/UC_MonthFilter, shared with the five approval
        // screens - this window only says where the months come from and what to do when
        // the selection changes.
        /// <summary>
        /// The X drops the period entirely. Nothing is asked for, so nothing is shown - the
        /// grid empties without a query rather than falling back to the whole till, which on
        /// the largest one is ten thousand rows nobody asked for.
        /// </summary>
        private void ShowNothing()
        {
            myDataGrid.ItemsSource = null;
            UpdateAllCounters();
        }

        private void SetUpFilter()
        {
            // The list arrives inside the batch that fills the grid, so the drop-down never
            // has to fetch it. LoadMonths is only the fallback if it is opened before a load.
            MonthFilter.ShowDateRange = false;
            MonthFilter.LoadMonths = () => a.Select_Till_Periods(Till_ID.ToString());
            MonthFilter.Apply += (s2, e2) => Load_Data();
            MonthFilter.Cleared += (s2, e2) => ShowNothing();
            MonthFilter.Initialise();
        }


        #endregion

        private void LoadGrid()
        {
            // One connection and one round trip instead of three. The three statements are
            // unchanged - see Action.Select_Till_Screen - only the number of trips differs.
            // The range narrows the two grids; the totals stay for the whole till.
            List<string> periods = _openingOnNewest
                ? new List<string> { Models.Action.NewestPeriod }
                : MonthFilter.SelectedMonths();

            DataSet screen = a.Select_Till_Screen(Till_ID.ToString(), SubProject_ID.ToString(), true, Account_ID,
                periods);

            if (screen.Tables.Count < Models.Action.TillScreenTableCount)
                throw new InvalidOperationException(
                    "The till screen query returned " + screen.Tables.Count +
                    " result sets instead of " + Models.Action.TillScreenTableCount + ".");

            // Came back in the same batch - keeps the list current when an action creates a
            // month that did not exist when the till was opened.
            MonthFilter.Fill(screen.Tables[Models.Action.TillScreenPeriods]);

            // The rows above are already the newest month's; this only makes the picker say
            // so. The list arrives newest first, and it came from the same round trip.
            if (_openingOnNewest)
                MonthFilter.SelectNewest();

            DataTable temp_dt = screen.Tables[Models.Action.TillScreenGrid];

            DataTable Not_Approved_temp_dt = screen.Tables[Models.Action.TillScreenNotApproved];

            int User_ID = Settings.Default.User_ID;
            List<int> CashAccount_ID = new List<int>();


            if (Settings.Default.User_CashAccounts != null && !string.IsNullOrEmpty(Settings.Default.User_CashAccounts) && !string.IsNullOrWhiteSpace(Settings.Default.User_CashAccounts))
                CashAccount_ID = Settings.Default.User_CashAccounts.ToString().Split(',').Select(int.Parse).ToList();


            if (temp_dt != null && temp_dt.Rows.Count > 0)
            {
                if (!AsFiscalBond)
                {
                    dt = temp_dt;
                }
                else
                {
                    if (Settings.Default.Role == "Employee")
                    {
                        string filterExpression = " created_byuser_ID = '" + User_ID + "'";

                        if (CashAccount_ID.Count > 0)
                        {
                            filterExpression += " OR ";
                            filterExpression += string.Join(" OR ", CashAccount_ID.Select(id => "CashAccount_ID = " + id));
                        }

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
                        dt = temp_dt;
                    }
                }
            }
            else
            {
                dt = null;
            }
            if (Not_Approved_temp_dt != null && Not_Approved_temp_dt.Rows.Count > 0)
            {
                if (!AsFiscalBond)
                {
                    not_approved_dt = Not_Approved_temp_dt;
                }
                else
                {
                    if (Settings.Default.Role == "Employee")
                    {
                        string filterExpression = " created_byuser_ID = '" + User_ID + "'";

                        if (CashAccount_ID.Count > 0)
                        {
                            filterExpression += " OR ";
                            filterExpression += string.Join(" OR ", CashAccount_ID.Select(id => "CashAccount_ID = " + id));

                        }

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
                        not_approved_dt = Not_Approved_temp_dt;
                    }
                }
            }
            else
            {
                not_approved_dt = null;
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

            sum_dt = new DataTable();
            cash_dt = new DataTable();
            // the totals and cash balances came back in the same batch as the grid above
            DataSet ds = screen;
            if (ds.Tables.Count > Models.Action.TillScreenSums)
            {
                sum_dt = ds.Tables[Models.Action.TillScreenSums];

                if (ds.Tables.Count > Models.Action.TillScreenCashBalances)
                {
                    DataTable temp_cash = ds.Tables[Models.Action.TillScreenCashBalances];
                    if (temp_cash != null && temp_cash.Rows.Count > 0)
                    {
                        if (!AsFiscalBond)
                        {
                            cash_dt = temp_cash;
                        }
                        else
                        {
                            if (Settings.Default.Role == "Employee")
                            {
                                string cash_filterExpression = "";

                                if (CashAccount_ID.Count > 0)
                                {
                                    cash_filterExpression += string.Join(" OR ", CashAccount_ID.Select(id => "CashAccount_id = " + id));

                                    System.Data.DataRow[] rows = temp_cash.Select(cash_filterExpression);
                                    DataTable selected_dt = new DataTable();
                                    if (rows.Length != 0)
                                    {
                                        selected_dt = rows.CopyToDataTable();
                                        cash_dt = selected_dt;
                                    }
                                }
                                else
                                {
                                    cash_dt = null;
                                }

                            }
                            else
                            {
                                cash_dt = temp_cash;
                            }
                        }
                    }

                }
            }
            if (!AsFiscalBond)
            {
                if (sum_dt != null && sum_dt.Rows.Count > 0)
                {
                    // A pay till holds both local currencies at once, so its local boxes read
                    // the common-unit totals (SumIBase / SumOBase, the last two columns) -
                    // SumI would be adding old lira to new. Every other till is unaffected.
                    bool pay = IsPayTill() && sum_dt.Columns.Count > 16;

                    // Columns 11-16 are the pay-till figures, appended after the rest so
                    // nothing that reads this row by position moved.
                    if (pay)
                    {
                        PayIncomeUnified_textBlock.Text = sum_dt.Rows[0].Field<double>(11).ToString("N2");
                        PayTransferUnified_textBlock.Text = sum_dt.Rows[0].Field<double>(12).ToString("N2");
                        PayIncomeCurrent_textBlock.Text = sum_dt.Rows[0].Field<double>(15).ToString("N2");
                        PayTransferCurrent_textBlock.Text = sum_dt.Rows[0].Field<double>(16).ToString("N2");
                    }

                    Income_textBlock.Text = sum_dt.Rows[0].Field<double>(pay ? 13 : 0).ToString("N2");
                    Income_textBlockusd.Text = sum_dt.Rows[0].Field<double>(1).ToString("N2");
                    //Outcome_textBlock.Text = sum_dt.Rows[0].Field<double>(1).ToString("N2");
                    // Returned_textBlock.Text = sum_dt.Rows[0].Field<double>(2).ToString("N2");
                    Transfered_textBlock.Text = sum_dt.Rows[0].Field<double>(pay ? 14 : 2).ToString("N2");
                    Exchange_textBlock.Text = sum_dt.Rows[0].Field<double>(4).ToString("N2");
                    Exchpand_textBlock.Text = sum_dt.Rows[0].Field<double>(5).ToString("N2");
                    Exchpand_textBlockusd.Text = sum_dt.Rows[0].Field<double>(6).ToString("N2");
                    //Transfered_textBlock.Text sum_dt.Rows[0].Field<double>(4).ToString("N2");

                    CashNet_textBlock.Text = sum_dt.Rows[0].Field<double>(7).ToString("N2");
                    Net_textBlock.Text = sum_dt.Rows[0].Field<double>(8).ToString("N2");
                    CashNet_textBlockusd.Text = sum_dt.Rows[0].Field<double>(9).ToString("N2");
                    Net_textBlockusd.Text = sum_dt.Rows[0].Field<double>(10).ToString("N2");
                }
                else
                { // Outcome_textBlock.Text = Returned_textBlock.Text =
                    Income_textBlock.Text = Exchange_textBlock.Text = Income_textBlockusd.Text = Transfered_textBlock.Text =
                    Exchpand_textBlockusd.Text = Exchpand_textBlock.Text = CashNet_textBlock.Text = Net_textBlock.Text =
                    CashNet_textBlockusd.Text = Net_textBlockusd.Text = "00,00";

                }
            }
            else
            {
                if (dt != null && dt.Rows.Count > 0)
                {
                    double incomes = 0;
                    double outcomes = 0;
                    double returned = 0;
                    double exchange = 0;
                    double expaned = 0;
                    for (var i = 0; i < dt.Rows.Count; i++)
                    {
                        System.Data.DataRow R = dt.Rows[i];
                        if (double.Parse(R["Income"].ToString()) != 0)
                        {
                            incomes += double.Parse(R["Income"].ToString());
                        }
                        else if (double.Parse(R["Outcome"].ToString()) != 0)
                        {
                            outcomes += double.Parse(R["Outcome"].ToString());
                        }
                        else if (double.Parse(R["Returned"].ToString()) != 0)
                        {
                            returned += double.Parse(R["Returned"].ToString());
                        }
                        else if (double.Parse(R["Exchange"].ToString()) != 0)
                        {
                            exchange += double.Parse(R["Exchange"].ToString());
                        }
                        else if (double.Parse(R["Expand"].ToString()) != 0)
                        {
                            expaned += double.Parse(R["Expand"].ToString());
                        }
                    }
                    Income_textBlock.Text = incomes.ToString("N2");
                    //Outcome_textBlock.Text = outcomes.ToString("N2");
                   // Returned_textBlock.Text = returned.ToString("N2");
                    Exchange_textBlock.Text = exchange.ToString("N2");
                    Exchpand_textBlock.Text = expaned.ToString("N2");
                }
            }

            if (Account_Type == "Project" || Account_Type == "Exchange" || Account_Type == "Personal")
            {
                if (IsLocalTill(TillType_Name))
                {
                    //if (cash_dt != null && cash_dt.Rows.Count > 0)
                    //{
                    //    Cash_DataGrid.ItemsSource = cash_dt.DefaultView;
                    //}
                    //else
                    //{
                    //    Cash_DataGrid.ItemsSource = null;
                    //}
                    Balances.Visibility = Visibility.Visible;
                }
            }
            GridLoaded = true;
            Bind_Show_Hide_Menu();
        }

        private void Set_myDataGrid_ItemSource()
        {
            try
            {
                if (NotApproved_RadioButton.IsChecked == false)
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        // Counted after binding, not before: UpdateAllCounters reads
                        // myDataGrid.View, so running it first counted the PREVIOUS contents
                        // and the number stayed one step behind every filter change.
                        myDataGrid.ItemsSource = dt.DefaultView;
                        UpdateAllCounters();
                    }
                    else
                    {
                        myDataGrid.ItemsSource = null;
                        UpdateAllCounters();
                    }
                }
                else
                {
                    if (not_approved_dt != null && not_approved_dt.Rows.Count > 0)
                    {
                        myDataGrid.ItemsSource = not_approved_dt.DefaultView;
                        UpdateAllCounters();
                    }
                    else
                    {
                        myDataGrid.ItemsSource = null;
                        UpdateAllCounters();
                    }
                }

                if (myDataGrid.ItemsSource != null)
                {
                    if (HiddenColumns.Count != 0)
                    {
                        foreach (string CName in HiddenColumns)
                        {
                            for (int i = 0; i < myDataGrid.Columns.Count; i++)
                            {
                                if (myDataGrid.Columns[i].HeaderText.ToString() == CName)
                                {
                                    myDataGrid.Columns[i].IsHidden = true; break;
                                }
                            }
                        }
                    }
                    if (HOffset != 0)
                        this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
                }

                UpdateSelectedStatistics();
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

        private void UpdateAllCounters()
        {
            // An empty grid must read zero, not keep the last number it showed.
            if (myDataGrid.View == null)
            {
                All_textBlock.Text = "0";
                All_textBlockusd.Text = "0";
                return;
            }

            // غير SYR → عدّ عادي
            if (!ShowsLocalAndUsd())
            {
                All_textBlock.Text = myDataGrid.View.Records.Count().ToString("N0");
                return;
            }

            int allUSD = 0;
            int allSYR = 0;

            foreach (var record in myDataGrid.View.Records)
            {
                if (record.Data is DataRowView rowView)
                {
                    string currency = rowView["Currency"]?.ToString();

                    if (string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
                        allUSD++;
                    else
                        allSYR++;
                }
            }

            All_textBlockusd.Text = allUSD.ToString("N0");
            All_textBlock.Text = allSYR.ToString("N0");
        }


        #region datagrid functions
        private void myDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
        {
            try
            {
                if (myDataGrid.CurrentItem != null)
                {
                    _lastCurrentItem = (DataRowView)myDataGrid.CurrentItem;
                }

                int count = myDataGrid.SelectedItems.Count;
                //Selected_textBlock.Text = count.ToString("N0");

                double selected_incomes = 0;
                double selected_outcomes = 0;
                double selected_returned = 0;
                double selected_Exchange = 0;
                double selected_expaned = 0;
                for (var i = 0; i < count; i++)
                {
                    DataRowView SelectedDataRow = (DataRowView)myDataGrid.SelectedItems[i];
                    if (double.Parse(SelectedDataRow["Income"].ToString()) != 0)
                    {
                        selected_incomes += double.Parse(SelectedDataRow["Income"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Outcome"].ToString()) != 0)
                    {
                        selected_outcomes += double.Parse(SelectedDataRow["Outcome"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Returned"].ToString()) != 0)
                    {
                        selected_returned += double.Parse(SelectedDataRow["Returned"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Exchange"].ToString()) != 0)
                    {
                        selected_Exchange += double.Parse(SelectedDataRow["Exchange"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Expand"].ToString()) != 0)
                    {
                        selected_expaned += double.Parse(SelectedDataRow["Expand"].ToString());
                    }
                }
               // Income_textBlock1.Text = selected_incomes.ToString("N2");
               // Outcome_textBlock1.Text = selected_outcomes.ToString("N2");
              //  Returned_textBlock1.Text = selected_returned.ToString("N2");
              //  Exchange_textBlock1.Text = selected_Exchange.ToString("N2");
               // Exchpand_textBlock1.Text = selected_expaned.ToString("N2");
                UpdateSelectedStatistics();
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

        private void Attachment_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                int Action_ID = Convert.ToInt32(dataRow["ID"].ToString());
                if (int.Parse(dataRow["SourceAction"].ToString()) != 1)
                {
                    MessageBox.Show("This operation was created from another till" + Environment.NewLine + "This operation can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                }
                else
                {

                    bool Approved_Action = false;

                    if (NotApproved_RadioButton.IsChecked == false)
                        Approved_Action = true;


                    Window _Window = new Attachments(Action_ID, -1, Approved_Action, Account_Type, Category, this, null, null, null);
                    _Window.ShowDialog();
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

        #endregion


        #region datagrid btn clicks

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                int Action_ID = Convert.ToInt32(dataRow["ID"].ToString());
                int ObserveTill_ID = -1;
                int.TryParse(dataRow["ObserveTill_ID"].ToString(), out ObserveTill_ID);

                if (int.Parse(dataRow["SourceAction"].ToString()) != 1)
                {
                    MessageBox.Show("This operation was created from another till" + Environment.NewLine + "This operation can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                }
                else
                {
                    bool Approved_Action = false;
                    if (NotApproved_RadioButton.IsChecked == false)
                    {
                        Approved_Action = true;
                    }
                    AddAction_Window2 _Window = new AddAction_Window2(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, Action_ID, SubProject_ID, true, Approved_Action, main_Window, this);

                    _Window.ShowDialog();
                    TillNotifier.RaiseRefresh();

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
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                    ID = Convert.ToInt32(dataRow["ID"].ToString());
                    string AType = "";


                    int ObserveTill_ID = -1;
                    int.TryParse(dataRow["ObserveTill_ID"].ToString(), out ObserveTill_ID);

                    if (int.Parse(dataRow["SourceAction"].ToString()) != 1)
                    {
                        MessageBox.Show("This operation was created from another till" + Environment.NewLine + "This operation can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        ID = -1;
                    }
                    else
                    {
                        if (dataRow["Income"].ToString() != "0")
                            AType = "I";
                        if (dataRow["Outcome"].ToString() != "0")
                            AType = "O";
                        if (dataRow["Returned"].ToString() != "0")
                            AType = "R";
                        if (dataRow["Exchange"].ToString() != "0")
                            AType = "Ex";
                        if (dataRow["Expand"].ToString() != "0")
                            AType = "E";


                        bool Approved_Action = false;

                        if (NotApproved_RadioButton.IsChecked == false)
                            Approved_Action = true;


                        a.Delete(ID, Till_ID, AType, Approved_Action, "Till_Window");

                        ID = -1;

                        TillNotifier.RaiseRefresh();

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
        #endregion
        private void AddNewAction_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddAction_Window2 _Window = new AddAction_Window2(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, SubProject_ID, true, main_Window, this);
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

        private void Balances_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Till_Balances _Window = new Till_Balances(Account_ID, Account_Type, Category);
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

        private void Attachments_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window _AttachmentWindow = new Attachments(Account_ID);
                _AttachmentWindow.ShowDialog();
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

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    t.Delete(Till_ID, TillType_Name, Account_ID, "Till_Window");
                    account_Window.Fill_Account_Tabs("", "");
                    TillNotifier.RaiseRefresh();

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
                                int ObserveTill_ID = -1;
                                int.TryParse(dataRow["ObserveTill_ID"].ToString(), out ObserveTill_ID);

                                if (int.Parse(dataRow["SourceAction"].ToString()) != 1)
                                {
                                    MessageBox.Show("This operation was created from another till" + Environment.NewLine + "This operation can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                                }
                                else
                                {
                                    bool Approved_Action = false;
                                    if (NotApproved_RadioButton.IsChecked == false)
                                    {
                                        Approved_Action = true;
                                    }

                                    AddAction_Window2 _Window = new AddAction_Window2(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, Action_ID, SubProject_ID, true, Approved_Action, main_Window, this);
                                    _Window.ShowDialog();
                                    TillNotifier.RaiseRefresh();

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

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;
                UpdateAllCounters();
                //if (TillType_Name.ToLower() == "syr")
                //{
                //    int allUSD = 0;
                //    int allSYR = 0;

                //    foreach (var record in myDataGrid.View.Records)
                //    {
                //        var rowView = record.Data as System.Data.DataRowView;
                //        if (rowView == null)
                //            continue;

                //        string currency = rowView["Currency"]?.ToString().ToUpper();

                //        if (currency == "USD")
                //            allUSD++;
                //        else
                //            allSYR++;
                //    }

                //    All_textBlockusd.Text = allUSD.ToString("N0");
                //    All_textBlock.Text = allSYR.ToString("N0");
                //}
                //else
                //{
                //    int count = myDataGrid.View.Records.Count();
                //    All_textBlock.Text = count.ToString("N0");
                //}
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

        #region Sizing TextBlock 

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
            //    All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            //else
            //    Selected_textBlock.MinWidth = All_textBlock.ActualWidth;
        }

        private void Income_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        //    if (Income_textBlock1.ActualWidth > Income_textBlock.ActualWidth)
        //        Income_textBlock.MinWidth = Income_textBlock1.ActualWidth;

        //    else
        //        Income_textBlock1.MinWidth = Income_textBlock.ActualWidth;

        }

        //private void Outcome_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    if (Outcome_textBlock1.ActualWidth > Outcome_textBlock.ActualWidth)
        //        Outcome_textBlock.MinWidth = Outcome_textBlock1.ActualWidth;
        //    else
        //        Outcome_textBlock1.MinWidth = Outcome_textBlock.ActualWidth;

        //}

        //private void Returned_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    if (Returned_textBlock1.ActualWidth > Returned_textBlock.ActualWidth)
        //        Returned_textBlock.MinWidth = Returned_textBlock1.ActualWidth;
        //    else
        //        Returned_textBlock1.MinWidth = Returned_textBlock.ActualWidth;

        //}

        private void Exchange_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if (Exchange_textBlock1.ActualWidth > Exchange_textBlock.ActualWidth)
            //    Exchange_textBlock.MinWidth = Exchange_textBlock1.ActualWidth;
            //else
            //    Exchange_textBlock1.MinWidth = Exchange_textBlock.ActualWidth;

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
                    UpdateSelectedStatistics();
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

        private void AddMultiAttachments_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiActions_Window _Window = new Add_MultiActions_Window(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, SubProject_ID, true, main_Window, this, true);
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

        private void Delete_Selected_btn_Click(object sender, RoutedEventArgs e)
        {
            Delete_Selected_btn.IsEnabled = false;
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

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
                            MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;
                        }

                        DataView SourceActions = new DataView(dt);
                        SourceActions.RowFilter = "IsSelected = 1 AND SourceAction <> 1";
                        if (SourceActions.Count > 0)
                        {
                            MessageBox.Show("Some operations were created from another till" + Environment.NewLine + "This operations can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;
                        }


                        for (int k = 0; k < newView.Count; k++)
                        {
                            string temp_query = "";
                            string log_query = "";



                            var Action_ID = newView[k]["ID"];
                            string AType = "";

                            if (newView[k]["Income"].ToString() != "0")
                                AType = "I";
                            if (newView[k]["Outcome"].ToString() != "0")
                                AType = "O";
                            if (newView[k]["Returned"].ToString() != "0")
                                AType = "R";
                            if (newView[k]["Exchange"].ToString() != "0")
                                AType = "Ex";
                            if (newView[k]["Expand"].ToString() != "0")
                                AType = "E";


                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@ID" + k, Action_ID);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Till_ID" + k, Till_ID);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@AType" + k, AType);

                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Form_Name" + k, "Till_Window");

                            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);


                            temp_query += "INSERT INTO action_deleted SELECT * FROM action WHERE action.id = @ID" + k + " OR action.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "Update action_deleted set `deleted_at` = @Todaydate" + k + ", `deleted_by` = @Uesr_ID" + k + " WHERE action_deleted.id = @ID" + k + " OR action_deleted.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += Financial_WPF_App.Models.Action.DeleteActionPaymentForDeletedInstallmentActionsSql("@ID" + k);

                            temp_query += "DELETE FROM action WHERE action.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action WHERE action.id  = @ID" + k + ";";

                            Login.DeleteMultiActions_List.Add(temp_query);
                            temp_query = "";

                        }

                        a.Delete_Multi_Actions();

                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                        TillNotifier.RaiseRefresh();

                        LoadGrid();
                    }
                    else
                    {
                        DataView newView = new DataView(not_approved_dt);
                        newView.RowFilter = "IsSelected = 1";

                        if (newView.Count == 0)
                        {
                            MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;
                        }

                        DataView SourceActions = new DataView(not_approved_dt);
                        SourceActions.RowFilter = "IsSelected = 1 AND SourceAction <> 1";
                        if (SourceActions.Count > 0)
                        {
                            MessageBox.Show("Some operations were created from another till" + Environment.NewLine + "This operations can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;
                        }


                        for (int k = 0; k < newView.Count; k++)
                        {
                            string temp_query = "";
                            string log_query = "";

                            var Action_ID = newView[k]["ID"];
                            string AType = "";

                            if (newView[k]["Income"].ToString() != "0")
                                AType = "I";
                            if (newView[k]["Outcome"].ToString() != "0")
                                AType = "O";
                            if (newView[k]["Returned"].ToString() != "0")
                                AType = "R";
                            if (newView[k]["Exchange"].ToString() != "0")
                                AType = "Ex";
                            if (newView[k]["Expand"].ToString() != "0")
                                AType = "E";


                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@ID" + k, Action_ID);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Till_ID" + k, Till_ID);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@AType" + k, AType);

                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Form_Name" + k, "Till_Window");

                            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);

                            temp_query += "INSERT INTO action_deleted SELECT * FROM action_not_approved WHERE action_not_approved.id = @ID" + k + " OR action_not_approved.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "Update action_deleted set `deleted_at` = @Todaydate" + k + ", `deleted_by` = @Uesr_ID" + k + " WHERE action_deleted.id = @ID" + k + " OR action_deleted.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += Financial_WPF_App.Models.Action.DeleteActionPaymentForDeletedInstallmentActionsSql("@ID" + k);

                            temp_query += "DELETE FROM action_not_approved WHERE action_not_approved.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action_not_approved WHERE action_not_approved.id  = @ID" + k + ";";

                            Login.DeleteMultiActions_List.Add(temp_query);
                            temp_query = "";
                        }

                        a.Delete_Multi_Actions();

                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
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

        private void Exchpand_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if (Exchpand_textBlock1.ActualWidth > Exchpand_textBlock.ActualWidth)
            //    Exchpand_textBlock.MinWidth = Exchpand_textBlock1.ActualWidth;

            //else
            //    Exchpand_textBlock1.MinWidth = Exchpand_textBlock.ActualWidth;

        }
        #endregion





        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void myDataGrid_CurrentCellValueChanged(object sender, CurrentCellValueChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(UpdateSelectedStatistics), DispatcherPriority.Background);
        }

        private void UpdateSelectedStatistics()
        {
            List<DataRowView> selectedRows = GetUnifiedSelectedRows();
            if (selectedRows.Count < 2)
            {
                ResetSelectedStatistics();
                SelectedStatisticsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            ResetSelectedStatistics();
            ConfigureSelectedStatisticsPanel();
            SelectedStatisticsCountTextBlock.Text = selectedRows.Count.ToString("N0");

            if (ShowsLocalAndUsd())
            {
                if (IsPayTill())
                {
                    // A pay till holds both local currencies, so each has its own figure,
                    // read from the columns the grid shows for it (zero outside its currency).
                    SelectedIncomeSyrTextBlock.Text = SumSelectedColumn(selectedRows, "Incomelegacy").ToString("N2");
                    SelectedIncomeCurrentTextBlock.Text = SumSelectedColumn(selectedRows, "Incomecurrent").ToString("N2");
                    SelectedTransferTextBlock.Text = SumSelectedColumn(selectedRows, "Outcomelegacy").ToString("N2");
                    SelectedTransferCurrentTextBlock.Text = SumSelectedColumn(selectedRows, "Outcomecurrent").ToString("N2");
                }
                else
                {
                    SelectedIncomeSyrTextBlock.Text = SumSelectedColumn(selectedRows.Where(IsSyrRow), "Income").ToString("N2");
                    SelectedExpensesSyrTextBlock.Text = SumSelectedColumn(selectedRows.Where(IsSyrRow), "Expand").ToString("N2");
                }

                // Was "Income", which is zero on a USD row by construction, so these two
                // boxes always read 0.00.
                SelectedIncomeUsdTextBlock.Text = SumSelectedColumn(selectedRows.Where(IsUsdRow), "Incomeusd").ToString("N2");
                SelectedExpensesUsdTextBlock.Text = SumSelectedColumn(selectedRows.Where(IsUsdRow), "Expandusd").ToString("N2");
            }
            else
            {
                SelectedIncomeSyrTextBlock.Text = SumSelectedColumn(selectedRows, "Income").ToString("N2");
                SelectedExchangeTextBlock.Text = SumSelectedColumn(selectedRows, "Exchange").ToString("N2");

                if (string.Equals(TillType_Name, "usd", StringComparison.OrdinalIgnoreCase))
                {
                    SelectedTransferTextBlock.Text = SumSelectedColumn(selectedRows, "Outcome").ToString("N2");
                }
            }

            SelectedValueTextBlock.Text = SumSelectedColumn(selectedRows, "USD_Value").ToString("N2");
            SelectedStatisticsPanel.Visibility = Visibility.Visible;
        }

        private List<DataRowView> GetUnifiedSelectedRows()
        {
            Dictionary<string, DataRowView> rowsById = new Dictionary<string, DataRowView>();
            DataView currentView = myDataGrid.ItemsSource as DataView;

            if (currentView != null)
            {
                foreach (DataRowView row in currentView)
                {
                    if (IsCheckedRow(row))
                    {
                        AddSelectedRow(rowsById, row);
                    }
                }
            }

            if (currentView == null)
                return rowsById.Values.ToList();

            foreach (object item in myDataGrid.SelectedItems)
            {
                DataRowView row = item as DataRowView;
                if (row != null && row.Row.Table == currentView.Table)
                {
                    AddSelectedRow(rowsById, row);
                }
            }

            return rowsById.Values.ToList();
        }

        private static void AddSelectedRow(IDictionary<string, DataRowView> rowsById, DataRowView row)
        {
            string id = row.Row.Table.Columns.Contains("ID")
                ? Convert.ToString(row["ID"])
                : row.Row.GetHashCode().ToString();

            if (!rowsById.ContainsKey(id))
            {
                rowsById.Add(id, row);
            }
        }

        private static bool IsCheckedRow(DataRowView row)
        {
            if (!row.Row.Table.Columns.Contains("IsSelected") || row["IsSelected"] == DBNull.Value)
                return false;

            object value = row["IsSelected"];
            return value is bool ? (bool)value : Convert.ToInt32(value) == 1;
        }

        /// <summary>
        /// Is this till in a local currency? Was a name match against "syr", which made a
        /// newly added local currency behave like a foreign one on this screen. The
        /// payments container is excluded; the code branches on it separately.
        /// </summary>
        private static bool IsLocalTill(string tillType)
        {
            return Helper.CurrencyService.IsLocalCurrency(tillType, false);
        }

        /// <summary>
        /// The payments container. Not a currency: it holds amounts in whichever currency
        /// the beneficiary is paid in, so it needs the local/USD split a local till gets.
        /// </summary>
        private bool IsPayTill()
        {
            return string.Equals(TillType_Name, "pay", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Does this screen show local and USD amounts side by side? A local till does,
        /// and so does the payments container - which used to fall through to the
        /// foreign-currency path and leave its USD boxes reading zero.
        /// </summary>
        private bool ShowsLocalAndUsd()
        {
            return IsLocalTill(TillType_Name) || IsPayTill();
        }

        /// <summary>
        /// A pay till holds both local currencies, and adding old lira to new in one column
        /// is meaningless. So the existing Income and Transfer columns are narrowed to the
        /// old currency (and renamed), and a twin for the current currency is added next to
        /// each. The twins copy their formatting and alignment from the column beside them,
        /// so they cannot drift from the rest of the grid.
        /// </summary>
        private void ConfigurePayCurrencyColumns()
        {
            string oldName = Helper.CurrencyService.GetLegacyLocalCurrencyName();
            string newName = Helper.CurrencyService.GetCurrentLocalCurrencyName();
            if (string.IsNullOrWhiteSpace(newName)) newName = "New";

            AddCurrentCurrencyTwin("Income", "Incomelegacy", "Income(" + oldName + ")", "Incomecurrent", "Income(" + newName + ")");
            AddCurrentCurrencyTwin("Outcome", "Outcomelegacy", "Transfer(" + oldName + ")", "Outcomecurrent", "Transfer(" + newName + ")");
        }

        private void AddCurrentCurrencyTwin(string originalMapping, string legacyMapping, string legacyHeader,
                                            string currentMapping, string currentHeader)
        {
            GridNumericColumn original = myDataGrid.Columns.OfType<GridNumericColumn>()
                .FirstOrDefault(c => c.MappingName == originalMapping);
            if (original == null) return;

            original.MappingName = legacyMapping;
            original.HeaderText = legacyHeader;

            if (myDataGrid.Columns.Any(c => c.MappingName == currentMapping)) return;

            GridNumericColumn twin = new GridNumericColumn
            {
                MappingName = currentMapping,
                HeaderText = currentHeader,
                NumberDecimalDigits = original.NumberDecimalDigits,
                NumberDecimalSeparator = original.NumberDecimalSeparator,
                NumberGroupSizes = original.NumberGroupSizes,
                NumberGroupSeparator = original.NumberGroupSeparator,
                TextAlignment = original.TextAlignment,
                HorizontalHeaderContentAlignment = original.HorizontalHeaderContentAlignment,
                Width = original.Width,
                ColumnSizer = original.ColumnSizer,
                MinimumWidth = original.MinimumWidth,
                AllowEditing = original.AllowEditing,
                AllowFiltering = original.AllowFiltering,
                AllowSorting = original.AllowSorting,
                AllowGrouping = original.AllowGrouping,
            };

            // Only when the original sets one itself. Assigning null here is not "inherit":
            // it overrides the grid's HeaderStyle/CellStyle with nothing, and the column
            // then draws with the default look - pale, thin header - next to its neighbours.
            if (original.HeaderStyle != null) twin.HeaderStyle = original.HeaderStyle;
            if (original.CellStyle != null) twin.CellStyle = original.CellStyle;

            myDataGrid.Columns.Insert(myDataGrid.Columns.IndexOf(original) + 1, twin);
        }

        private static bool IsSyrRow(DataRowView row)
        {
            return !IsUsdRow(row);
        }

        private static bool IsUsdRow(DataRowView row)
        {
            return row.Row.Table.Columns.Contains("Currency")
                && string.Equals(Convert.ToString(row["Currency"]), "USD", StringComparison.OrdinalIgnoreCase);
        }

        private static decimal SumSelectedColumn(IEnumerable<DataRowView> rows, string columnName)
        {
            decimal total = 0;

            foreach (DataRowView row in rows)
            {
                if (row.Row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
                {
                    total += Convert.ToDecimal(row[columnName]);
                }
            }

            return total;
        }

        private void ConfigureSelectedStatisticsPanel()
        {
            bool isPay = IsPayTill();
            bool splits = ShowsLocalAndUsd();
            bool isUsd = string.Equals(TillType_Name, "usd", StringComparison.OrdinalIgnoreCase);

            string oldName = Helper.CurrencyService.GetLegacyLocalCurrencyName();
            string newName = Helper.CurrencyService.GetCurrentLocalCurrencyName();
            if (string.IsNullOrWhiteSpace(newName)) newName = "New";

            // A pay till shows each local currency on its own, named after the currency; a
            // single-currency till names its own.
            string localName = isPay ? oldName : TillType_Name;

            SelectedIncomePrimaryLabel.Content = splits ? "Income(" + localName + "):" : "Income:";
            SelectedExpensesPrimaryLabel.Content = splits ? "Expenses(" + localName + "):" : "Expenses:";

            SelectedIncomeCurrentLabel.Content = "Income(" + newName + "):";
            SelectedTransferLabel.Content = isPay ? "Transfer(" + oldName + "):" : "Transfer:";
            SelectedTransferCurrentLabel.Content = "Transfer(" + newName + "):";
            SelectedIncomeCurrentPanel.Visibility = isPay ? Visibility.Visible : Visibility.Collapsed;
            SelectedTransferCurrentPanel.Visibility = isPay ? Visibility.Visible : Visibility.Collapsed;

            // Expenses are hidden in the grid of a pay till, so do not offer a total for them.
            SelectedExpensesSyrPanel.Visibility = splits && !isPay ? Visibility.Visible : Visibility.Collapsed;
            SelectedExpensesUsdPanel.Visibility = splits && !isPay ? Visibility.Visible : Visibility.Collapsed;
            SelectedIncomeUsdPanel.Visibility = splits ? Visibility.Visible : Visibility.Collapsed;
            SelectedExchangePanel.Visibility = splits ? Visibility.Collapsed : Visibility.Visible;
            SelectedTransferPanel.Visibility = isUsd || isPay ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ResetSelectedStatistics()
        {
            SelectedStatisticsCountTextBlock.Text = "0";
            SelectedIncomeSyrTextBlock.Text = "0.00";
            SelectedExpensesSyrTextBlock.Text = "0.00";
            SelectedIncomeUsdTextBlock.Text = "0.00";
            SelectedExpensesUsdTextBlock.Text = "0.00";
            SelectedExchangeTextBlock.Text = "0.00";
            SelectedTransferTextBlock.Text = "0.00";
            SelectedIncomeCurrentTextBlock.Text = "0.00";
            SelectedTransferCurrentTextBlock.Text = "0.00";
            SelectedValueTextBlock.Text = "0.00";
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
                UpdateAllCounters();

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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void AddMultiAction_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiActions_Window _Window = new Add_MultiActions_Window(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, SubProject_ID, true, main_Window, this, false);
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

        private void myExpander_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                //myExpander.Width = double.NaN;

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
                //myExpander.Width = 30;

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

        private void OnMainWindowStateChanged(WindowState newState)
        {
            //_currentWindowState = newState;
            //if (newState == WindowState.Normal && myExpander.IsExpanded)
            //{
            //    Grid_col.Width = new GridLength(30, GridUnitType.Star);
            //    Expander_col.Width = new GridLength(50, GridUnitType.Star);
            //}
            //else if (newState == WindowState.Maximized && myExpander.IsExpanded)
            //{
            //    Grid_col.Width = new GridLength(50, GridUnitType.Star);
            //    Expander_col.Width = new GridLength(30, GridUnitType.Star);
            //}
            //else if (newState == WindowState.Normal && !myExpander.IsExpanded)
            //{
            //    Grid_col.Width = new GridLength(130, GridUnitType.Star);
            //    Expander_col.Width = new GridLength(7, GridUnitType.Star);
            //}
            //else if (_currentWindowState == WindowState.Maximized && !myExpander.IsExpanded)
            //{
            //    Grid_col.Width = new GridLength(130, GridUnitType.Star);
            //    Expander_col.Width = new GridLength(6, GridUnitType.Star);
            //}
        }

        private void Delete_Selected_TextBlock_Click(object sender, RoutedEventArgs e)
        {

        }

        protected override void OnClosed(EventArgs e)
        {
            TillNotifier.RefreshTill -= OnTillRefresh;
            base.OnClosed(e);
        }


    }
}
