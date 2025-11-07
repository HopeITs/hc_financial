using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Till_Balances.xaml
    /// </summary>
    public partial class Till_Balances : Window
    {
        public Till_Balances(int Account_ID, string Account_Type, string Category)
        {
            InitializeComponent();
            this.Account_ID = Account_ID;
            this.Account_Type = Account_Type;
            this.Category = Category;
            if (Onload())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }
        private DataTable cash_dt;
        Models.Action a;
        int Account_ID;
        string Account_Type ,Category;
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";

        
        private void Cash_DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (Cash_DataGrid.View != null)
            {
                Export_btn.CommandTarget = Cash_DataGrid;
            }
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowUtils.TitleBar_MouseDown(this, e);
        }
        private bool Onload()
        {
            try
            {
                a = new Models.Action();

                Load_Data();
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    Apply_DataGrid_Color_Theme();
                }), DispatcherPriority.ApplicationIdle);

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

        public void Load_Data()
        {
            LoadGrid();
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
            Cash_DataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            Cash_DataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            Cash_DataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }
        private void LoadGrid()
        {
            cash_dt = new DataTable();
            List<int> CashAccount_ID = new List<int>();
            DataSet ds = a.Select_Account_Balances(Account_ID);

            if (Settings.Default.User_CashAccounts != null && !string.IsNullOrEmpty(Settings.Default.User_CashAccounts) && !string.IsNullOrWhiteSpace(Settings.Default.User_CashAccounts))
                CashAccount_ID = Settings.Default.User_CashAccounts.ToString().Split(',').Select(int.Parse).ToList();

            if (ds.Tables.Count > 0)
            {
                    DataTable temp_cash = ds.Tables[0];
                    if (temp_cash != null && temp_cash.Rows.Count > 0)
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
                if (cash_dt != null && cash_dt.Rows.Count > 0)
                {
                    Cash_DataGrid.ItemsSource = cash_dt.DefaultView;
                }
                else
                {
                    Cash_DataGrid.ItemsSource = null;
                }
            }
        }
    }

    }
