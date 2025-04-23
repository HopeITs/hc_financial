using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for CashBalances_Window.xaml
    /// </summary>
    public partial class CashBalances_Window : Window
    {
        public CashBalances_Window(int CashAccount_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.CashAccount_ID = CashAccount_ID;

            this.main_Window = main_Window;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private int CashAccount_ID;
        private Account acc;
        DataSet ds;
        private DataTable Report_dt, CashAccount_Net_dt, Net_dt;
        Main_Window_2 main_Window;
        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";
        private bool OnLoad()
        {
            try
            {
                acc = new Account();

                if (CashAccount_ID != -1)
                {
                    //Total_Panel.Visibility = Visibility.Visible;
                    Balances_StackPanel.Visibility = Net_StackPanel.Visibility = Differace_StackPanel.Visibility = Visibility.Visible;
                    myDataGrid.Margin = new Thickness(2, 5, 35, 5);
                }
                else
                {
                    Balances_StackPanel.Visibility = Net_StackPanel.Visibility = Differace_StackPanel.Visibility = Visibility.Collapsed;
                    myDataGrid.Margin = new Thickness(2, 5, 5, 5);
                    //Total_Panel.Visibility = Visibility.Collapsed;
                    myDataGrid.TableSummaryRows.Clear();
                }

                Load_Data();
                Apply_Color_Theme();
                Apply_DataGrid_Color_Theme();
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

            if (CashAccount_ID != -1)
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
            if (CashAccount_ID != -1)
            {
                HeaderStyle_Name = "DarkgrayishViolet_SfDataGrid_header";
                TableSummaryRowStyle_Name = "DarkgrayishViolet_TableSummaryRowStyle";
                GroupSummaryRowStyle = "DarkgrayishViolet_GroupSummaryRowStyle";
            }

            myDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            myDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            myDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }

        private void Load_Data()
        {
            if (Login.BuildConnection())
            {
                CashNet_textBlock.Text = AccountsNet_textBlock.Text = Difference_textBlock.Text = "0.00";

                Fill_Grid();
            }
        }

        private void Fill_Grid()
        {
            Report_dt = new DataTable();
            CashAccount_Net_dt = new DataTable();
            Net_dt = new DataTable();

            if(CashAccount_ID > 0)
            {
                ds = acc.Select_Cash_Report(CashAccount_ID);
                Report_dt = ds.Tables[0];
                Net_dt = ds.Tables[1];
            }
            else
            {
                ds = acc.Select_Cash_Report();
                Report_dt = ds.Tables[0];
                CashAccount_Net_dt = ds.Tables[1];


                // Base matching columns from CashAccount_Net_dt
                var validColumns = CashAccount_Net_dt.AsEnumerable()
                                      .Select(r => r.Field<string>("CashAccount_Name"))
                                      .ToHashSet();

                // Only used for netRow and diffRow
                var matchingColumns = Report_dt.Columns.Cast<System.Data.DataColumn>()
                                            .Where(c => validColumns.Contains(c.ColumnName))
                                            .ToList();

                // Extended set: matchingColumns + "Net Account" columns (for totalRow only)
                var totalColumns = Report_dt.Columns.Cast<System.Data.DataColumn>()
                   .Where(c => validColumns.Contains(c.ColumnName) ||
                               c.ColumnName.ToLower().Contains("net account"))
                   .ToList();


                System.Data.DataRow totalRow = Report_dt.NewRow();
                totalRow["Account"] = "Total";

                foreach (var col in totalColumns)
                {
                    totalRow[col.ColumnName] = Report_dt.AsEnumerable()
                        .Sum(r => r[col.ColumnName] != DBNull.Value ? Convert.ToDouble(r[col.ColumnName]) : 0);
                }
                Report_dt.Rows.Add(totalRow);



                System.Data.DataRow netRow = Report_dt.NewRow();
                netRow["Account"] = "Cash till net";

                foreach (var col in matchingColumns)
                {
                    var match = CashAccount_Net_dt.AsEnumerable()
                                   .FirstOrDefault(r => r.Field<string>("CashAccount_Name") == col.ColumnName);
                    if (match != null)
                        netRow[col.ColumnName] = match.Field<double>("CashAccount_Net");
                }
                Report_dt.Rows.Add(netRow);

                // diffRow
                System.Data.DataRow diffRow = Report_dt.NewRow();
                diffRow["Account"] = "Difference";

                foreach (var col in matchingColumns)
                {
                    double total = Convert.ToDouble(totalRow[col.ColumnName]);
                    double net = Convert.ToDouble(netRow[col.ColumnName]);
                    diffRow[col.ColumnName] = total - net;
                }
                Report_dt.Rows.Add(diffRow);

            }



            if (Report_dt != null && Report_dt.Rows.Count > 0)
            {
                int count = Report_dt.Rows.Count;
                All_textBlock.Text = count.ToString("N0");

                //All_textBlock.Text = Report_dt.Rows.Count.ToString();

                foreach (System.Data.DataColumn col in Report_dt.Columns)
                {
                    if (col.ColumnName.Contains("."))
                    {
                        string cleanName = col.ColumnName.Replace(".", "");
                        col.ColumnName = cleanName;
                    }
                }

                myDataGrid.ItemsSource = Report_dt.DefaultView;
                if (HOffset != 0)
                    this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
            }
            else
            {
                myDataGrid.ItemsSource = null;
            }

            if (Net_dt != null && Net_dt.Rows.Count > 0)
            {
                CashNet_textBlock.Text = Net_dt.Rows[0].Field<double>("CashAccount_Net").ToString("N2");
                AccountsNet_textBlock.Text = Net_dt.Rows[0].Field<double>("AllAccounts_Net").ToString("N2");
                Difference_textBlock.Text = Net_dt.Rows[0].Field<double>("Difference").ToString("N2");
            }
            else
            {
                CashNet_textBlock.Text = AccountsNet_textBlock.Text = Difference_textBlock.Text = "0.00";
            }
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;

                int count = myDataGrid.View.Records.Count();
                All_textBlock.Text = count.ToString("N0");

                //All_textBlock.Text = myDataGrid.View.Records.Count().ToString();

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

        private void myDataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            try
            {
                var x = e.Column;
                string y = x.MappingName;

                if (e.Column is GridNumericColumn)
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                if (e.Column.MappingName.Contains("ID") || e.Column.MappingName == "tilltype_Name" 
                    || e.Column.MappingName == "Category" || e.Column.MappingName == "Account_Type")
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }


        DataRowView Selected_DataRow = null;

        private void View_Acount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Selected_DataRow != null)
                {
                    int Acc_ID = Convert.ToInt32(Selected_DataRow["Account_ID"].ToString());
                    string AccName = Selected_DataRow["Account"].ToString();
                    string tilltype_Name = Selected_DataRow["tilltype_Name"].ToString();
                    string Account_type = Selected_DataRow["Account_Type"].ToString();
                    string Acc_Categort = Selected_DataRow["Category"].ToString();

                    if (Acc_ID != 0)
                    {
                        if (tilltype_Name.ToLower() == "pay")
                        {
                            int Sub_ID = Convert.ToInt32(Selected_DataRow["Subproject_ID"].ToString());
                            string Sub_Name = Selected_DataRow["Center"].ToString();
                            Window Payments = new Payments_Window(main_Window, Sub_ID.ToString());
                            main_Window.Create_tabItem(Payments, "Payments Account - " + Sub_Name, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                        }
                        else
                        {
                            Account_Window account_Window = new Account_Window(Acc_ID, main_Window);

                            if(Account_type == "Project")
                            {
                                if (Acc_Categort == "Micro Projects")
                                {
                                    main_Window.Create_tabItem(account_Window, AccName, (SolidColorBrush)Application.Current.Resources["HopeRed"]);

                                }
                                else if (Acc_Categort == "Budget")
                                {
                                    main_Window.Create_tabItem(account_Window, AccName, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);

                                }
                            }
                            else if (Account_type == "Exchange")
                            {
                                main_Window.Create_tabItem(account_Window, AccName, (SolidColorBrush)Application.Current.Resources["Orange"]);

                            }
                            else if (Account_type == "Personal")
                            {
                                main_Window.Create_tabItem(account_Window, AccName, (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                            }
                            else
                            {
                                main_Window.Create_tabItem(account_Window, AccName, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
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

        private void myDataGrid_GridContextMenuOpening(object sender, GridContextMenuEventArgs e)
        {
            try
            {
                Selected_DataRow = (DataRowView)this.myDataGrid.View.Records.GetItemAt(e.RowColumnIndex.RowIndex - 1);
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

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;
        }

        private void myDataGrid_SelectionChanged(object sender, GridSelectionChangedEventArgs e)
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

    }
}
