using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Syr_Balances_Window.xaml
    /// </summary>
    public partial class Syr_Balances_Window : Window
    {
        public Syr_Balances_Window(Main_Window_2 main_Window)
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
        Rate r;
        DataTable types_dt;

        private bool OnLoad()
        {
            try
            {
                r = new Rate();
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

            types_dt = new DataTable();
            types_dt = r.Select_unique_toSYR_Rates();
            
            if (types_dt != null && types_dt.Rows.Count > 0)
            {
                for (int i = 0; i < types_dt.Rows.Count; i++)
                {
                    int Type_ID = types_dt.Rows[i].Field<int>("TillType_ID");
                    string Type_Name = types_dt.Rows[i].Field<string>("TillType_Name");


                    Syr_Balances_Sub_Window Syr_Balances_Sub_Window = new Syr_Balances_Sub_Window(Type_ID, Type_Name, main_Window);
                    newTabItem = new TabItem
                    {
                        Header = (Type_Name).ToUpper(),
                        Name = Type_Name + "_tab",
                        Style = this.FindResource("tabItemStyle") as Style,
                        FlowDirection = FlowDirection.LeftToRight
                    };
                    Main_tabControl.Items.Add(newTabItem);
                    newTabItem.Content = null;
                    newTabItem.Content = Syr_Balances_Sub_Window.Content;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }
    }
}
