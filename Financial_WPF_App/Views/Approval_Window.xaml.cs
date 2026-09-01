using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{

    public partial class Approval_Window : Window
    {
        public Approval_Window(int CityID, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.CityID = CityID;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }


        }

        Main_Window_2 main_Window;
        Till t;
        int CityID;

        #region privileges
        bool BudgetAccounts_Syr_privileges, BudgetAccounts_foreign_privileges,
            MicroAccounts_Syr_privileges, MicroAccounts_foreign_privileges, MicroAccounts_Payments_privileges,
            PersonalAccounts_Syr_privileges, PersonalAccounts_foreign_privileges, ExchangeTills_Syr_privileges, Exchange_foreign_privileges,
            Administrative_privileges;

        #endregion

        private bool OnLoad()
        {
            try
            {
                t = new Till();
                Load_Data();

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
                this.Close();
                return false;
                //main_Window.main_tabControl.Items.RemoveAt(main_Window.main_tabControl.SelectedIndex);
            }
        }

        private void Load_Data()
        {
            Set_privileges();

            Fill_Main_Tabs();

            //Show_Hide();
        }

        private void Set_privileges()
        {
            BudgetAccounts_Syr_privileges = BudgetAccounts_foreign_privileges =
            MicroAccounts_Syr_privileges = MicroAccounts_foreign_privileges = MicroAccounts_Payments_privileges =
            PersonalAccounts_Syr_privileges = PersonalAccounts_foreign_privileges = ExchangeTills_Syr_privileges = Exchange_foreign_privileges = Administrative_privileges = false;

            if (Settings.Default.Role == "Head Office")
            {
                BudgetAccounts_Syr_privileges = BudgetAccounts_foreign_privileges =
                MicroAccounts_Syr_privileges = MicroAccounts_foreign_privileges = MicroAccounts_Payments_privileges =
                PersonalAccounts_Syr_privileges = PersonalAccounts_foreign_privileges = ExchangeTills_Syr_privileges = Exchange_foreign_privileges = Administrative_privileges = true;
            }
            else if (Settings.Default.Role == "Project Manager")
            {
                BudgetAccounts_Syr_privileges =
                MicroAccounts_Syr_privileges = MicroAccounts_Payments_privileges = true;
            }
            else if (Settings.Default.Role == "Financial Manager")
            {
                BudgetAccounts_Syr_privileges =
                MicroAccounts_Syr_privileges = MicroAccounts_Payments_privileges =
                PersonalAccounts_Syr_privileges = true;
            }
            else if (Settings.Default.Role == "Accountant")
            {
                BudgetAccounts_Syr_privileges =
                MicroAccounts_Syr_privileges = true;
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

        private void Fill_Main_Tabs()
        {
            _buildingTabs = true;
            try
            {
                Fill_Main_Tabs_Core();
            }
            finally
            {
                _buildingTabs = false;
            }
        }

        private void Fill_Main_Tabs_Core()
        {
            _pendingTabs.Clear();
            Main_tabControl.Items.Clear();
            TabItem newTabItem = new TabItem();

            DataTable Projects_dt = main_Window.AllAccounts_ds.Tables[0];
            DataTable ExchangeAccounts_dt = main_Window.AllAccounts_ds.Tables[1];
            DataTable PersonalAccounts_dt = main_Window.AllAccounts_ds.Tables[4];


            if (Projects_dt != null && Projects_dt.Rows.Count > 0)
            {
                if (BudgetAccounts_Syr_privileges || BudgetAccounts_foreign_privileges)
                {
                    IEnumerable<int> rows = null;
                    if (Administrative_privileges)
                    {
                        rows = Projects_dt.AsEnumerable()
                             .Where(row => row.Field<string>("CategoryName") == "Budget" && row.Field<int?>("AccountID") != null && row.Field<int?>("CityID") == CityID)
                             .Select(row => row.Field<int>("AccountID"));
                    }
                    else
                    {
                        rows = Projects_dt.AsEnumerable()
                             .Where(row => row.Field<string>("CategoryName") == "Budget" && row.Field<int?>("AccountID") != null && row.Field<int?>("CityID") == CityID && row.Field<int?>("is_administrative") == 0)
                             .Select(row => row.Field<int>("AccountID"));
                    }

                    if (rows.Any())
                    {
                        string Accounts_ID = string.Join(",", rows);

                        newTabItem = new TabItem
                        {
                            Header = "Budget Accounts",
                            Name = "Budget_Accounts_tab",
                            Style = this.FindResource("tabItemStyle") as Style,
                            FlowDirection = FlowDirection.RightToLeft
                        };
                        Main_tabControl.Items.Add(newTabItem);
                        newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                        newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);

                        TabControl SubTabControler = new TabControl();
                        SubTabControler.Style = (Style)FindResource("TabControlStyle");
                        SubTabControler.SelectionChanged += SubTab_SelectionChanged;
                        newTabItem.Content = SubTabControler;

                        DataTable t_dt = new DataTable();
                        t_dt = t.Select(Accounts_ID, -1, "");

                        DataTable uniqueNames = t_dt.DefaultView.ToTable(true, "Type");

                        if (uniqueNames != null && uniqueNames.Rows.Count > 0)
                        {
                            for (int i = 0; i < uniqueNames.Rows.Count; i++)
                            {
                                string TillType_Name = uniqueNames.Rows[i].Field<string>("Type");
                                bool addtill = false;
                                if (TillType_Name.ToLower() == "syr" && BudgetAccounts_Syr_privileges)
                                {
                                    if (BudgetAccounts_Syr_privileges)
                                        addtill = true;
                                }
                                else
                                {
                                    if (BudgetAccounts_foreign_privileges)
                                        addtill = true;
                                }
                                if (addtill)
                                {
                                    newTabItem = new TabItem
                                    {
                                        Header = (uniqueNames.Rows[i].Field<string>(0)).ToUpper(),
                                        Name = uniqueNames.Rows[i].Field<string>(0) + "_tab",
                                        Style = this.FindResource("tabItemStyle") as Style,
                                        FlowDirection = FlowDirection.LeftToRight
                                    };
                                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                                    SubTabControler.Items.Add(newTabItem);
                                    newTabItem.Content = null;


                                    IEnumerable<int> Tills_ID_rows = t_dt.AsEnumerable()
                                                    .Where(row => row.Field<string>("Type").ToLower() == TillType_Name.ToLower())
                                                    .Select(row => row.Field<int>("ID"));

                                    if (Tills_ID_rows.Any())
                                    {
                                        string Tills_ID = string.Join(",", Tills_ID_rows);

                                        TabItem lazyTab = newTabItem;
                                        SetLazyContent(lazyTab, () => new Approval_Sub_Window(Tills_ID, "Project", "Budget", TillType_Name, this, main_Window));
                                    }
                                }
                            }
                        }

                    }
                }

                if (MicroAccounts_Syr_privileges || MicroAccounts_foreign_privileges || MicroAccounts_Payments_privileges)
                {
                    IEnumerable<int> rows = Projects_dt.AsEnumerable()
                                .Where(row => row.Field<string>("CategoryName") == "Micro Projects" && row.Field<int?>("AccountID") != null && row.Field<int?>("CityID") == CityID)
                                .Select(row => row.Field<int>("AccountID"));

                    if (rows.Any())
                    {
                        string Accounts_ID = string.Join(",", rows);

                        newTabItem = new TabItem
                        {
                            Header = "Micro Accounts",
                            Name = "Micro_Accounts_tab",
                            Style = this.FindResource("tabItemStyle") as Style,
                            FlowDirection = FlowDirection.RightToLeft
                        };
                        Main_tabControl.Items.Add(newTabItem);
                        newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                        newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["HopeRed"]);



                        TabControl SubTabControler = new TabControl();
                        SubTabControler.Style = (Style)FindResource("TabControlStyle");
                        SubTabControler.SelectionChanged += SubTab_SelectionChanged;
                        newTabItem.Content = SubTabControler;

                        DataTable t_dt = new DataTable();
                        t_dt = t.Select(Accounts_ID, -1, "");

                        DataTable uniqueNames = t_dt.DefaultView.ToTable(true, "Type");

                        if (uniqueNames != null && uniqueNames.Rows.Count > 0)
                        {
                            for (int i = 0; i < uniqueNames.Rows.Count; i++)
                            {
                                string TillType_Name = uniqueNames.Rows[i].Field<string>("Type");
                                bool addtill = false;
                                bool addPaytill = false;
                                if (TillType_Name.ToLower() == "syr" && MicroAccounts_Syr_privileges)
                                {
                                    if (MicroAccounts_Syr_privileges)
                                        addtill = true;
                                }
                                else if (TillType_Name.ToLower() == "pay" && MicroAccounts_Payments_privileges)
                                {
                                    if (MicroAccounts_Payments_privileges)
                                        addPaytill = true;
                                }
                                else
                                {
                                    if (MicroAccounts_foreign_privileges)
                                        addtill = true;
                                }
                                if (addtill)
                                {
                                    newTabItem = new TabItem
                                    {
                                        Header = (uniqueNames.Rows[i].Field<string>(0)).ToUpper(),
                                        Name = uniqueNames.Rows[i].Field<string>(0) + "_tab",
                                        Style = this.FindResource("tabItemStyle") as Style,
                                        FlowDirection = FlowDirection.LeftToRight
                                    };
                                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                    SubTabControler.Items.Add(newTabItem);
                                    newTabItem.Content = null;


                                    IEnumerable<int> Tills_ID_rows = t_dt.AsEnumerable()
                                                    .Where(row => row.Field<string>("Type").ToLower() == TillType_Name.ToLower())
                                                    .Select(row => row.Field<int>("ID"));

                                    if(Tills_ID_rows.Any())
                                    {
                                        string Tills_ID = string.Join(",", Tills_ID_rows);

                                        TabItem lazyTab = newTabItem;
                                        SetLazyContent(lazyTab, () => new Approval_Sub_Window(Tills_ID, "Project", "Micro Projects", TillType_Name, this, main_Window));
                                    }

                                }
                                else if (addPaytill)
                                {
                                    newTabItem = new TabItem
                                    {
                                        Header = (uniqueNames.Rows[i].Field<string>(0)).ToUpper(),
                                        Name = uniqueNames.Rows[i].Field<string>(0) + "_tab",
                                        Style = this.FindResource("tabItemStyle") as Style,
                                        FlowDirection = FlowDirection.LeftToRight
                                    };
                                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                    SubTabControler.Items.Add(newTabItem);
                                    newTabItem.Content = null;

                                    IEnumerable<int> Subprojects_ID_rows = Projects_dt.AsEnumerable()
                                                    .Where(row => row.Field<string>("CategoryName") == "Micro Projects" && row.Field<int?>("AccountID") != null && row.Field<int?>("CityID") == CityID)
                                                    .Select(row => row.Field<int>("SubprojectID"));

                                    if (Subprojects_ID_rows.Any())
                                    {
                                        string Subprojects_ID = string.Join(",", Subprojects_ID_rows);

                                        TabItem lazyPayTab = newTabItem;
                                        SetLazyContent(lazyPayTab, () => new Approval_Sub_Pay_Window(Subprojects_ID, this, main_Window));
                                    }
                                }
                            }
                        }

                    }
                }
            }

            if (ExchangeAccounts_dt != null && ExchangeAccounts_dt.Rows.Count > 0)
            {
                if (ExchangeTills_Syr_privileges || Exchange_foreign_privileges)
                {
                    IEnumerable<int> rows = ExchangeAccounts_dt.AsEnumerable()
                            .Where(row => row.Field<int?>("ID") != null && row.Field<int?>("CityID") == CityID)
                            .Select(row => row.Field<int>("ID"));

                    if (rows.Any())
                    {
                        string Accounts_ID = string.Join(",", rows);

                        newTabItem = new TabItem
                        {
                            Header = "Exchange Tills",
                            Name = "Exchange_Tills_tab",
                            Style = this.FindResource("tabItemStyle") as Style,
                            FlowDirection = FlowDirection.RightToLeft
                        };
                        newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["Orange"];
                        newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["Orange"]);
                        Main_tabControl.Items.Add(newTabItem);


                        TabControl SubTabControler = new TabControl();
                        SubTabControler.Style = (Style)FindResource("TabControlStyle");
                        SubTabControler.SelectionChanged += SubTab_SelectionChanged;
                        newTabItem.Content = SubTabControler;

                        DataTable t_dt = new DataTable();
                        t_dt = t.Select(Accounts_ID, -1, "");

                        DataTable uniqueNames = t_dt.DefaultView.ToTable(true, "Type");

                        if (uniqueNames != null && uniqueNames.Rows.Count > 0)
                        {
                            for (int i = 0; i < uniqueNames.Rows.Count; i++)
                            {
                                string TillType_Name = uniqueNames.Rows[i].Field<string>("Type");
                                bool addtill = false;
                                if (TillType_Name.ToLower() == "syr" && ExchangeTills_Syr_privileges)
                                {
                                    if (ExchangeTills_Syr_privileges)
                                        addtill = true;
                                }
                                else
                                {
                                    if (Exchange_foreign_privileges)
                                        addtill = true;
                                }
                                if (addtill)
                                {
                                    newTabItem = new TabItem
                                    {
                                        Header = (uniqueNames.Rows[i].Field<string>(0)).ToUpper(),
                                        Name = uniqueNames.Rows[i].Field<string>(0) + "_tab",
                                        Style = this.FindResource("tabItemStyle") as Style,
                                        FlowDirection = FlowDirection.LeftToRight
                                    };
                                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["Orange"];
                                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["Orange"]);
                                    SubTabControler.Items.Add(newTabItem);
                                    newTabItem.Content = null;

                                    IEnumerable<int> Tills_ID_rows = t_dt.AsEnumerable()
                                                    .Where(row => row.Field<string>("Type").ToLower() == TillType_Name.ToLower())
                                                    .Select(row => row.Field<int>("ID"));

                                    if (Tills_ID_rows.Any())
                                    {
                                        string Tills_ID = string.Join(",", Tills_ID_rows);

                                        TabItem lazyTab = newTabItem;
                                        SetLazyContent(lazyTab, () => new Approval_Sub_Window(Tills_ID, "Exchange", "", TillType_Name, this, main_Window));
                                    }
                                }
                            }
                        }

                    }
                }
            }

            if (PersonalAccounts_dt != null && PersonalAccounts_dt.Rows.Count > 0)
            {
                if (PersonalAccounts_Syr_privileges || PersonalAccounts_foreign_privileges)
                {
                    IEnumerable<int> rows = PersonalAccounts_dt.AsEnumerable()
                    .Where(row => row.Field<int?>("ID") != null && row.Field<int?>("CityID") == CityID)
                    .Select(row => row.Field<int>("ID"));

                    if (rows.Any())
                    {
                        string Accounts_ID = string.Join(",", rows);

                        newTabItem = new TabItem
                        {
                            Header = "Personal Accounts",
                            Name = "Personal_Accounts_tab",
                            Style = this.FindResource("tabItemStyle") as Style,
                            FlowDirection = FlowDirection.RightToLeft
                        };
                        Main_tabControl.Items.Add(newTabItem);
                        newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelGreen"];
                        newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                        newTabItem.Content = null;



                        TabControl SubTabControler = new TabControl();
                        SubTabControler.Style = (Style)FindResource("TabControlStyle");
                        SubTabControler.SelectionChanged += SubTab_SelectionChanged;
                        newTabItem.Content = SubTabControler;

                        DataTable t_dt = new DataTable();
                        t_dt = t.Select(Accounts_ID, -1, "");

                        DataTable uniqueNames = t_dt.DefaultView.ToTable(true, "Type");

                        if (uniqueNames != null && uniqueNames.Rows.Count > 0)
                        {
                            for (int i = 0; i < uniqueNames.Rows.Count; i++)
                            {
                                string TillType_Name = uniqueNames.Rows[i].Field<string>("Type");
                                bool addtill = false;
                                if (TillType_Name.ToLower() == "syr" && PersonalAccounts_Syr_privileges)
                                {
                                    if (PersonalAccounts_Syr_privileges)
                                        addtill = true;
                                }
                                else
                                {
                                    if (PersonalAccounts_foreign_privileges)
                                        addtill = true;
                                }
                                if (addtill)
                                {
                                    newTabItem = new TabItem
                                    {
                                        Header = (uniqueNames.Rows[i].Field<string>(0)).ToUpper(),
                                        Name = uniqueNames.Rows[i].Field<string>(0) + "_tab",
                                        Style = this.FindResource("tabItemStyle") as Style,
                                        FlowDirection = FlowDirection.LeftToRight
                                    };
                                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelGreen"];
                                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                                    SubTabControler.Items.Add(newTabItem);
                                    newTabItem.Content = null;

                                    IEnumerable<int> Tills_ID_rows = t_dt.AsEnumerable()
                                                    .Where(row => row.Field<string>("Type").ToLower() == TillType_Name.ToLower())
                                                    .Select(row => row.Field<int>("ID"));

                                    if (Tills_ID_rows.Any())
                                    {
                                        string Tills_ID = string.Join(",", Tills_ID_rows);

                                        TabItem lazyTab = newTabItem;
                                        SetLazyContent(lazyTab, () => new Approval_Sub_Window(Tills_ID, "Personal", "", TillType_Name, this, main_Window));
                                    }

                                }
                            }
                        }
                    }
                }
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


        #region lazy tab content

        // Approval has four main tabs and one sub-tab per till type inside each: sixteen
        // sub-windows, each running two queries, built the moment the screen opens. That is
        // about 37 round trips to show the one tab the user is looking at, and on real data
        // it also fetches every waiting row for fifteen tabs nobody opened.
        //
        // A sub-tab's window is now built the first time that sub-tab is selected. Same
        // pattern as Account_Window.
        private readonly Dictionary<TabItem, Func<Window>> _pendingTabs = new Dictionary<TabItem, Func<Window>>();

        // True while the tabs are being laid out. The first sub-tab of each control is
        // selected as it is added, and that build is on the same call stack as OnLoad, so a
        // failure there must keep propagating exactly as it did before.
        private bool _buildingTabs;

        /// <summary>
        /// Registers the factory that builds this tab's content. Must be called before the
        /// TabItem is added to its TabControl, because adding the first item selects it and
        /// that selection is what triggers the build.
        /// </summary>
        private void SetLazyContent(TabItem tab, Func<Window> factory)
        {
            _pendingTabs[tab] = factory;
        }

        /// <summary>
        /// Every sub-tab control gets this. Selecting a sub-tab builds its window if it has
        /// not been built yet.
        /// </summary>
        private void SubTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SelectionChanged bubbles: grids and combo boxes inside a tab raise it too.
            TabControl control = sender as TabControl;
            if (control == null || !ReferenceEquals(e.Source, control))
                return;

            MaterializeTab(control.SelectedItem as TabItem);
        }

        private void MaterializeTab(TabItem tab)
        {
            Func<Window> factory;
            if (tab == null || !_pendingTabs.TryGetValue(tab, out factory))
                return;

            if (_buildingTabs)
            {
                // Same call stack as the old eager code - let OnLoad handle any failure.
                tab.Content = factory().Content;
                _pendingTabs.Remove(tab);
                return;
            }

            try
            {
                tab.Content = factory().Content;
                // Only drop the factory once the content is really there, so a tab that
                // failed on a transient network error can be retried by selecting it again.
                _pendingTabs.Remove(tab);
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

        private void NewTabItem_Loaded(object sender, RoutedEventArgs e, SolidColorBrush color)
        {
            TabItem tabItem = sender as TabItem;
            if (tabItem != null)
            {
                // Find the BottomBorder inside the TabItem template
                Border bottomBorder = tabItem.Template.FindName("BottomBorder", tabItem) as Border;
                if (bottomBorder != null)
                {
                    // Set the new color for the bottom border
                    bottomBorder.Background = color;  // Replace with your desired color
                }
            }
        }
    }
}
