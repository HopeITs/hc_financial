using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for All_Budgetlins_Window.xaml
    /// </summary>
    public partial class All_Budgetlins_Window : Window
    {
        public All_Budgetlins_Window(Main_Window_2 main_Window)
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
        Budget_Lines bl;
        DataTable Budget_Typs_dt;

        private bool OnLoad()
        {
            try
            {
                bl = new Budget_Lines();
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
            Main_tabControl.Items.Clear();
            TabItem newTabItem = new TabItem();

            Budget_Typs_dt = new DataTable();
            Budget_Typs_dt = bl.Select_unique_Budget_types();

            if (Budget_Typs_dt != null && Budget_Typs_dt.Rows.Count > 0)
            {
                for (int i = 0; i < Budget_Typs_dt.Rows.Count; i++)
                {
                    int Type_ID = Budget_Typs_dt.Rows[i].Field<int>("BudgetType_ID");
                    string Type_Name = Budget_Typs_dt.Rows[i].Field<string>("BudgetType");

                    All_BidgetLine_Sub_Window All_BudgetLine_Sub_Window = new All_BidgetLine_Sub_Window(Type_ID, main_Window);
                    newTabItem = new TabItem
                    {
                        Header = (Type_Name).ToUpper() + " Budgets",
                        Name = Type_Name + "_tab",
                        Style = this.FindResource("tabItemStyle") as Style,
                        FlowDirection = FlowDirection.LeftToRight
                    };
                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                    Main_tabControl.Items.Add(newTabItem);
                    newTabItem.Content = null;
                    newTabItem.Content = All_BudgetLine_Sub_Window.Content;

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
