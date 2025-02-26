using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Syncfusion.Data;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for ExpensesReport_Window.xaml
    /// </summary>
    public partial class ExpensesReport_Window : Window
    {
        public ExpensesReport_Window(int Account_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Account_ID = Account_ID;


            OnLoad();
        }

        Main_Window_2 main_Window;
        int Account_ID = -1;
        private DataTable Budgets_dt, AvgRate_dt, Budgetrate_dt, Donor_dt;
        private Budget b; private Budget_Lines bl;
        Till t;
        Rate r;
        int Budget_AvgRate_ID = -1;
        int Budget_ID = -1;
        bool UserMode = false;
        bool PassException = false;
        bool ShowTotals;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        StackedHeaderRow StackedHeaderRow1 = new StackedHeaderRow { };

        DataTable Report_dt = new DataTable();

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];



        private void OnLoad()
        {
            b = new Budget();
            t = new Till();
            r = new Rate();
            bl = new Budget_Lines();
            myExpander.IsExpanded = false;
            PassException = true;
            Load_Data();
            Apply_Color_Theme();
            PassException = false;
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
            Budget_AvgRate_ID = -1;
            Load_Comboboxes_Data();
            Load_Budget_AvgRate_Datagridview();
            Load_Expenses_DataGridView(true);
        }

        private void Load_Comboboxes_Data()
        {
            UserMode = false;

            AvgRate_comboBox.ItemsSource = null;
            AvgRate_dt = r.Select(Account_ID);


            Budgets_comboBox.ItemsSource = null;
            Budgets_dt = bl.Select_AllAvilableBudgetLines(Account_ID);

            if (Budgets_dt != null && Budgets_dt.Rows.Count > 0)
            {
                DataView view = new DataView(Budgets_dt);
                DataTable selected_dt = new DataTable();
                selected_dt = view.ToTable(true, "budget_ID", "budget_Name");

                if (selected_dt != null && selected_dt.Rows.Count > 0)
                {
                    Budgets_comboBox.ItemsSource = selected_dt.DefaultView;
                    Budgets_comboBox.SelectedIndex = 0;
                    UserMode = true;
                    Budgets_comboBox_SelectionChanged(Budgets_comboBox, null);
                }
            }
        }

        private void Load_Budget_AvgRate_Datagridview()
        {
            if (Login.BuildConnection())
            {
                Budgetrate_dt = b.Select_Budgt_AvgetRate(Budget_ID.ToString());
                if (Budgetrate_dt != null && Budgetrate_dt.Rows.Count > 0)
                {
                    Budget_AvgRate_Datagridview.ItemsSource = Budgetrate_dt.DefaultView;
                }
                else
                {
                    Budget_AvgRate_Datagridview.ItemsSource = null;
                }
            }
        }

        private void Load_Expenses_DataGridView(bool Update)
        {
            Expenses_DataGridView.ItemsSource = null;

            if (Update)
            {
                Donor_dt = b.Select_budget_donor(Account_ID, Budget_ID);

                if (Donor_dt.Rows.Count > 1)
                {
                    Panel1.Visibility = Visibility.Visible;
                }
                else
                {
                    Panel1.Visibility = Visibility.Collapsed;
                    Total_RadioButton.IsChecked = true;
                }

                if (Budget_ID != -1 && View_comboBox.SelectedIndex != -1)
                {
                    if (View_comboBox.SelectedIndex == 0)
                    {
                        Report_dt = b.Select_Lines_ExpensesReport(Account_ID, Budget_ID);
                        Expenses_DataGridView.FrozenColumnCount = 2;
                    }
                    else if (View_comboBox.SelectedIndex == 1)
                    {
                        Report_dt = b.Select_LineTypes_ExpensesReport(Account_ID, Budget_ID);
                        Expenses_DataGridView.FrozenColumnCount = 1;
                    }
                }
            }
            if (Report_dt != null && Report_dt.Rows.Count > 0)
            {
                Expenses_DataGridView.ItemsSource = Report_dt.DefaultView;
            }
            Expenses_DataGridView.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
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

        private void Budgets_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                Budget_ID = Convert.ToInt32(Budgets_comboBox.SelectedValue);
                if (UserMode)
                {
                    Load_Budget_AvgRate_Datagridview();
                    Load_Expenses_DataGridView(true);

                    AvgRate_comboBox.ItemsSource = null;

                    System.Data.DataRow[] row = Budgets_dt.Select("budget_ID=" + Convert.ToInt32(Budget_ID));

                    int Budet_Type_ID = Convert.ToInt32(row[0]["BudgetType_ID"].ToString());

                    System.Data.DataRow[] rows = AvgRate_dt.Select("totp_id = " + Budet_Type_ID + "");
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                            AvgRate_comboBox.ItemsSource = selected_dt.DefaultView;
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
                if (PassException)
                    throw;
            }
        }

        private void View_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                Load_Expenses_DataGridView(true);
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

        private void DataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            try
            {
                if (e.Column is GridNumericColumn && e.Column.MappingName != "ID" && e.Column.MappingName != "Timing")
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }
                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;

                //HIDE ID column.
                if (e.Column.MappingName == "budget_ID")
                    e.Cancel = true;

                if (e.Column.MappingName == "Budget Line Type" || e.Column.MappingName == "Budget Line" || e.Column.MappingName == "Timing")
                {
                    e.Column.AllowFiltering = true; e.Column.AllowGrouping = true;
                }
                else
                {
                    e.Column.AllowFiltering = false; e.Column.AllowGrouping = false;
                }
                if (ShowTotals)
                {
                    var BudgetDononr = Donor_dt.AsEnumerable().Select(row => row.Field<string>("Donor"));
                    if (BudgetDononr.Any(name => e.Column.MappingName.Contains(name)))
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);

                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)Budget_AvgRate_Datagridview.SelectedItem;
                    Budget_AvgRate_ID = Convert.ToInt32(dataRow["Budget_AvgRate_ID"]);
                    string BudgetName = Budgets_comboBox.Text;

                    b.Delete_Budgt_AvgetRate(Budget_AvgRate_ID, Account_ID, BudgetName, "ExpensesReport_Window");

                    Budget_AvgRate_ID = -1;
                    AvgRate_comboBox.SelectedIndex = -1;
                    Load_Budget_AvgRate_Datagridview();
                    Load_Expenses_DataGridView(true);
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

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Budgets_comboBox.SelectedIndex == -1 || Budget_ID == -1)
                {
                    MessageBox.Show("Please select the budget.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }

                if (AvgRate_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the AvgRate", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }


                if (Login.BuildConnection())
                {
                    int AvgRate_ID = Convert.ToInt32(AvgRate_comboBox.SelectedValue);
                    string BudgetName = Budgets_comboBox.Text;


                    b.Insert_Budgt_AvgetRate(Budget_ID, AvgRate_ID, Account_ID, BudgetName, "ExpensesReport_Window");

                    Budget_AvgRate_ID = -1;
                    Load_Budget_AvgRate_Datagridview();
                    Load_Expenses_DataGridView(true);
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
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }

        private void DataGridView_Loaded(object sender, RoutedEventArgs e)
        {
            if (Expenses_DataGridView.View != null)
            {
                Export_btn.CommandTarget = Expenses_DataGridView;

                AddSums();
            }
        }

        private void Total_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = true;
                if (UserMode)
                {
                    Load_Expenses_DataGridView(false);
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

        private void PerDonor_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = false;
                if (UserMode)
                {
                    Load_Expenses_DataGridView(false);
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

        private void Expenses_DataGridView_ItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e)
        {
            try
            {
                if (Expenses_DataGridView.ItemsSource != null)
                {
                    if (Expenses_DataGridView.StackedHeaderRows.Count != 0)
                        Expenses_DataGridView.StackedHeaderRows.RemoveAt(0);

                    if (Expenses_DataGridView.ItemsSource != null && !ShowTotals)
                    {
                        add_stackedHeader(Expenses_DataGridView);
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

        private void add_stackedHeader(SfDataGrid MyDatagrid)
        {
            var BudgetDononr = Donor_dt.AsEnumerable().Select(row => row.Field<string>("Donor"));
            var BudgetAvgs = Budgetrate_dt.AsEnumerable().Select(row => row.Field<string>("FromTill"));

            StackedHeaderRow1 = new StackedHeaderRow { };

            for (int i = 0; i < MyDatagrid.Columns.Count; i++)
            {
                if (MyDatagrid.Columns[i].MappingName != null)
                {
                    int moved = 0;
                    if (!BudgetDononr.Any(name => MyDatagrid.Columns[i].MappingName.Contains(name)))
                    {
                        string StacHeader = MyDatagrid.Columns[i].MappingName;
                        var MonthColumn = new StackedColumn
                        {
                            HeaderText = StacHeader,
                            MappingName = StacHeader.Replace(" ", "")
                        };
                        if (MyDatagrid.Columns[i].MappingName != "Budget Line" &&
                            MyDatagrid.Columns[i].MappingName != "Budget Line Type" &&
                            MyDatagrid.Columns[i].MappingName != "Timing")
                        {
                            if (!BudgetAvgs.Any(name => MyDatagrid.Columns[i].MappingName.Contains(name)))
                            {
                                moved = stack_and_goup_columns(i, StacHeader, MonthColumn, MyDatagrid);
                            }
                            else
                            {
                                moved = stack_and_group_AvgRate_columns(i, StacHeader, MonthColumn, MyDatagrid);
                            }
                        }
                    }
                    i += moved;
                }
            }
            MyDatagrid.StackedHeaderRows.Add(StackedHeaderRow1);

            //Default GridStackedCellRenderer is removed.
            MyDatagrid.CellRenderers.Remove("StackedHeader");

            //Customized GridStackedCellRenderer is added.
            MyDatagrid.CellRenderers.Add("StackedHeader", new GridCustomStackedRenderer());
        }

        private int stack_and_goup_columns(int i, string StacHeader, StackedColumn CurrentStack, SfDataGrid MyDatagrid)
        {
            int moved = 0;
            var BudgetAvgs = Budgetrate_dt.AsEnumerable().Select(row => row.Field<string>("FromTill"));

            for (int j = i; j < MyDatagrid.Columns.Count; j++)
            {
                if (MyDatagrid.Columns[j].MappingName.Contains(StacHeader) &&
                    !BudgetAvgs.Any(name => MyDatagrid.Columns[j].MappingName.Contains(name)))
                
                {
                    // Add column to StackedColumn
                    if (CurrentStack.ChildColumns == "")
                        CurrentStack.ChildColumns = MyDatagrid.Columns[j].HeaderText;
                    else
                        CurrentStack.ChildColumns += "," + MyDatagrid.Columns[j].HeaderText;

                    // Rename column header
                    if (MyDatagrid.Columns[j].MappingName != StacHeader)
                    {
                        MyDatagrid.Columns[j].HeaderText = MyDatagrid.Columns[j].MappingName.Replace(StacHeader, "");
                        MyDatagrid.Columns.MoveTo(j, i);
                        moved++;
                    }
                    else
                    {
                        MyDatagrid.Columns[j].HeaderText = "Total";
                    }
                }
            }
            StackedHeaderRow1.StackedColumns.Add(CurrentStack);

            return moved;
        }

        private int stack_and_group_AvgRate_columns(int i, string StacHeader, StackedColumn CurrentStack, SfDataGrid MyDatagrid)
        {
            int moved = 0;

            for (int j = i; j < MyDatagrid.Columns.Count; j++)
            {
                if (MyDatagrid.Columns[j].MappingName.Contains(StacHeader))
                {
                    // Add column to StackedColumn
                    if (CurrentStack.ChildColumns == "")
                        CurrentStack.ChildColumns = MyDatagrid.Columns[j].HeaderText;
                    else
                        CurrentStack.ChildColumns += "," + MyDatagrid.Columns[j].HeaderText;

                    // Rename column header
                    if (MyDatagrid.Columns[j].MappingName != StacHeader)
                    {
                        MyDatagrid.Columns[j].HeaderText = MyDatagrid.Columns[j].MappingName.Replace(StacHeader, "");
                        MyDatagrid.Columns.MoveTo(j, i);
                        moved++;
                    }
                    else
                    {
                        MyDatagrid.Columns[j].HeaderText = "Total";
                    }
                }
            }
            StackedHeaderRow1.StackedColumns.Add(CurrentStack);

            return moved;
        }


        private void Bind_Show_Hide_Menu()
        {

            SolidColorBrush MyWhite = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF");
            this.ShowMenu.Items.Clear();
            this.HideMenu.Items.Clear();
            for (int i = 0; i < Expenses_DataGridView.Columns.Count; i++)
            {
                string CHeader = Expenses_DataGridView.Columns[i].HeaderText.ToString();
                MenuItem NewItem = new MenuItem();
                NewItem.Header = CHeader;
                NewItem.Background = MyWhite;
                NewItem.StaysOpenOnClick = true;
                if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") &&
                    !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") &&
                    !CHeader.Contains("Select") && !CHeader.Contains("#") &&
                    !Originally_HiddenColumns.Contains(CHeader))
                {


                    if (Expenses_DataGridView.Columns[i].IsHidden == false)
                    {
                        bool found = false;

                        for (int j = 0; j < this.HideMenu.Items.Count; j++)
                        {
                            if (((MenuItem)this.HideMenu.Items[j]).Header.ToString() == CHeader)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            NewItem.Click += new RoutedEventHandler(HideColumn);
                            this.HideMenu.Items.Add(NewItem);
                        }

                    }
                    else
                    {
                        bool found = false;

                        for (int j = 0; j < this.ShowMenu.Items.Count; j++)
                        {
                            if (((MenuItem)this.ShowMenu.Items[j]).Header.ToString() == CHeader)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            NewItem.Click += new RoutedEventHandler(ShowColumn);
                            this.ShowMenu.Items.Add(NewItem);
                        }
                    }
                }
            }
        }
        private void ShowColumn(object sender, RoutedEventArgs e)
        {
            try
            {
                string CHeader = ((MenuItem)sender).Header.ToString();
                for (int i = 0; i < Expenses_DataGridView.Columns.Count; i++)
                {
                    if (Expenses_DataGridView.Columns[i].HeaderText.ToString() == CHeader)
                    {
                        Expenses_DataGridView.Columns[i].IsHidden = false;

                        HiddenColumns.RemoveAt(HiddenColumns.IndexOf(CHeader));

                        //break;
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

        private void Expenses_DataGridView_GroupExpanded(object sender, GroupChangedEventArgs e)
        {
            try
            {
                var expandedGroup = e.Group; // Get the expanded group
                if (expandedGroup.Groups != null && expandedGroup.Groups.Count <= 0)
                {
                    MessageBox.Show("dc");
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

        private void HideColumn(object sender, RoutedEventArgs e)
        {
            try
            {
                string CHeader = ((MenuItem)sender).Header.ToString();
                for (int i = 0; i < Expenses_DataGridView.Columns.Count; i++)
                {
                    if (Expenses_DataGridView.Columns[i].HeaderText.ToString() == CHeader)
                    {
                        Expenses_DataGridView.Columns[i].IsHidden = true;
                        HiddenColumns.Add(CHeader);
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
                for (int i = 0; i < Expenses_DataGridView.Columns.Count; i++)
                {
                    string CHeader = Expenses_DataGridView.Columns[i].HeaderText.ToString();
                    if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") && !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") && !CHeader.Contains("#") && !Originally_HiddenColumns.Contains(CHeader))
                    {
                        Expenses_DataGridView.Columns[i].IsHidden = false;
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
        
        private void AddSums()
        {
            if (Expenses_DataGridView.TableSummaryRows.Count > 0)
            {
                Expenses_DataGridView.TableSummaryRows[0].SummaryColumns.Clear();
                Expenses_DataGridView.TableSummaryRows.RemoveAt(0);
            }

            if(Expenses_DataGridView.GroupSummaryRows.Count > 0)
            {
                Expenses_DataGridView.GroupSummaryRows[0].SummaryColumns.Clear();
                Expenses_DataGridView.GroupSummaryRows.RemoveAt(0);
            }

            ObservableCollection<ISummaryColumn> SummaryColumns = new ObservableCollection<ISummaryColumn>();
            ObservableCollection<ISummaryColumn> GroupSummaryColumns = new ObservableCollection<ISummaryColumn>();


            for (int i = 0; i < Expenses_DataGridView.Columns.Count; i++)
            {
                if (Expenses_DataGridView.Columns[i] is GridNumericColumn && Expenses_DataGridView.Columns[i].MappingName != "Timing")
                {
                    var summaryColumn = new GridSummaryColumn()
                    {
                        Name = Expenses_DataGridView.Columns[i].MappingName,
                        MappingName = Expenses_DataGridView.Columns[i].MappingName,
                        SummaryType = SummaryType.DoubleAggregate,
                        Format = "{Sum:N2}"
                    };
                    SummaryColumns.Add(summaryColumn);
                    GroupSummaryColumns.Add(summaryColumn);
                }
            }

            if (SummaryColumns.Count > 0)
            {
                Expenses_DataGridView.TableSummaryRows.Add(new GridTableSummaryRow()
                {
                    ShowSummaryInRow = false,
                    TitleColumnCount = 1,
                    Position = TableSummaryRowPosition.Bottom,
                    Title = "Total:",
                    SummaryColumns = SummaryColumns
                });
            }

            if (GroupSummaryColumns.Count > 0)
            {
                Expenses_DataGridView.GroupSummaryRows.Add(new GridSummaryRow()
                {
                    ShowSummaryInRow = false,
                    TitleColumnCount = 1,
                    Title = "Total:",
                    SummaryColumns = GroupSummaryColumns
                });
            }
        }
        
    }


}
