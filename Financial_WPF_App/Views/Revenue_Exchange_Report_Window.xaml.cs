using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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

            t_dt = new DataTable();
            t_dt = t.Select_Projects_tillTypes();

            if (t_dt != null && t_dt.Rows.Count > 0)
            {
                for (int i = 0; i < t_dt.Rows.Count; i++)
                {
                    int Type_ID = t_dt.Rows[i].Field<int>("Type_ID");
                    string Type_Name = t_dt.Rows[i].Field<string>("Type_Name");


                    Revenue_Exchange_Sub_Report_Window Revenue_Exchange_Sub_Report_Window = new Revenue_Exchange_Sub_Report_Window(Type_ID, Type_Name, main_Window);
                    newTabItem = new TabItem
                    {
                        Header = (Type_Name).ToUpper(),
                        Name = Type_Name + "_tab",
                        Style = this.FindResource("tabItemStyle") as Style,
                        FlowDirection = FlowDirection.LeftToRight
                    };
                    Main_tabControl.Items.Add(newTabItem);
                    newTabItem.Content = null;
                    newTabItem.Content = Revenue_Exchange_Sub_Report_Window.Content;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }
    }
}
