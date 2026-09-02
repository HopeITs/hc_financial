using Financial_WPF_App.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Revenue_Exchange_Report_Window.xaml
    /// </summary>
    public partial class Revenue_Exchange_Report_Window : Window
    {
        public Revenue_Exchange_Report_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;

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
        DataTable t_dt;

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
            }
        }

        private void Load_Data()
        {
            Fill_Main_Tabs();
        }

        private void Fill_Main_Tabs()
        {
            // The first tab is selected as it is added, so it is built here, on the same
            // call stack as OnLoad - a failure on it must keep propagating as it always did.
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
            Main_tabControl.SelectionChanged -= MainTab_SelectionChanged;
            Main_tabControl.SelectionChanged += MainTab_SelectionChanged;
            _pendingTabs.Clear();
            Main_tabControl.Items.Clear();
            TabItem newTabItem = new TabItem();

            t_dt = new DataTable();
            t_dt = t.Select_Projects_tillTypes();

            if (t_dt != null && t_dt.Rows.Count > 0)
            {
                for (int i = 0; i < t_dt.Rows.Count; i++)
                {
                    int Type_ID = t_dt.Rows[i].Field<int>("Type_ID");
                    string Type_Name = t_dt.Rows[i].Field<string>("Type_Name");


                    newTabItem = new TabItem
                    {
                        Header = (Type_Name).ToUpper(),
                        Name = Type_Name + "_tab",
                        Style = this.FindResource("tabItemStyle") as Style,
                        FlowDirection = FlowDirection.LeftToRight
                    };
                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                    TabItem lazyTab = newTabItem;
                    SetLazyContent(lazyTab, () => new Revenue_Exchange_Sub_Report_Window(Type_ID, Type_Name, main_Window));
                    Main_tabControl.Items.Add(newTabItem);
                }
            }


            Syr_Balances_Window Syr_Balances_Window = new Syr_Balances_Window(main_Window);
            newTabItem = new TabItem
            {
                Header = ("Syr Balances").ToUpper(),
                Name = "Syr_Balances_tab",
                Style = this.FindResource("tabItemStyle") as Style,
                FlowDirection = FlowDirection.LeftToRight
            };
            newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
            newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            Main_tabControl.Items.Add(newTabItem);
            newTabItem.Content = null;
            newTabItem.Content = Syr_Balances_Window.Content;

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

        // Every tab used to be built as the window opened, which meant one sub-window - and
        // its queries - per till type whether or not anyone looked at it. A tab's window is
        // now built the first time that tab is selected. Same pattern as Account_Window.
        private readonly Dictionary<TabItem, Func<Window>> _pendingTabs = new Dictionary<TabItem, Func<Window>>();

        // True while the tabs are being laid out. Adding the first tab selects it, and that
        // build is on the same call stack as OnLoad, so a failure there must keep
        // propagating exactly as it did before.
        private bool _buildingTabs;

        /// <summary>
        /// Registers the factory that builds this tab's content. Must be called before the
        /// TabItem is added to the TabControl, because adding the first item selects it and
        /// that selection is what triggers the build.
        /// </summary>
        private void SetLazyContent(TabItem tab, Func<Window> factory)
        {
            _pendingTabs[tab] = factory;
        }

        private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SelectionChanged bubbles: grids and combo boxes inside a tab raise it too.
            if (!ReferenceEquals(e.Source, Main_tabControl))
                return;

            MaterializeTab(Main_tabControl.SelectedItem as TabItem);
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
