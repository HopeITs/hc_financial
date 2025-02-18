using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.Data;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for CashTill_Window.xaml
    /// </summary>
    public partial class CashTill_Window : Window
    {
        public CashTill_Window(int Account_ID, string Account_Name, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Account_ID = Account_ID;
            this.Account_Name = Account_Name;

            Onload();
        }

        int Account_ID;
        string Account_Name;
        Account Acc;
        Models.Action a;

        private DataTable dt, sum_dt;
        Main_Window_2 main_Window;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();


        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"];


        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;

        #endregion

        private void Onload()
        {
            Acc = new Account();
            a = new Models.Action();

            Load_Data();
            Apply_Color_Theme();
        }

        private void Apply_Color_Theme()
        {


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
        public void Load_Data()
        {
            Set_privileges();

            LoadGrid();
            move_Columns();
            AddSums();
            Show_Hide();

        }

        private void Show_Hide()
        {
            if (!Add_privileges)
            {
                AddNewAction_btn.Visibility = AddNewAction_Label.Visibility = Visibility.Collapsed;
            }
            if (!Edit_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Edit")
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
        }

        private void Set_privileges()
        {
            Edit_privileges = Add_privileges = Delete_privileges = false;

            bool IsAccount_Cashier = false;
            List<int> CashAccount_ID = new List<int>();
            if (Settings.Default.CashAccount_Cashier != null && !string.IsNullOrEmpty(Settings.Default.CashAccount_Cashier.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.CashAccount_Cashier.ToString()))
            {
                CashAccount_ID = Settings.Default.CashAccount_Cashier.ToString().Split(',').Select(int.Parse).ToList();
                if (CashAccount_ID.Contains(Account_ID))
                    IsAccount_Cashier = true;
            }

            if (Settings.Default.Role == "Head Office")
            {
                Edit_privileges = Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Project Manager")
            {

            }
            else if (Settings.Default.Role == "Financial Manager")
            {
                if (IsAccount_Cashier)
                    Edit_privileges = Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Accountant")
            {
                if (IsAccount_Cashier)
                    Edit_privileges = Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Cashier")
            {
                if (IsAccount_Cashier)
                    Edit_privileges = Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Employee")
            {
                if (IsAccount_Cashier)
                    Edit_privileges = Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Donor")
            {

            }
        }

        private void LoadGrid()
        {
            if (Login.BuildConnection())
            {
                dt = new DataTable();
                sum_dt = new DataTable();
                DataSet ds = Acc.Select_CashAcc_Tills(Account_ID);
                if (ds.Tables.Count > 0)
                {
                    dt = ds.Tables[0];

                    if (ds.Tables.Count > 1)
                    {
                        sum_dt = ds.Tables[1];
                    }
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

                    if (HOffset != 0)
                        this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);

                    All_textBlock.Text = dt.Rows.Count.ToString();

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
                }
                else
                {
                    myDataGrid.ItemsSource = null;

                    for (int i = 0; i < myDataGrid.Columns.Count; i++)
                    {
                        myDataGrid.Columns[i].IsHidden = true;
                    }
                }

                if (sum_dt != null && sum_dt.Rows.Count > 0)
                {
                    Income_textBlock.Text = sum_dt.Rows[0].Field<double>("CashAccount_I").ToString("N2");
                    Exchpand_textBlock.Text = sum_dt.Rows[0].Field<double>("CashAccount_E").ToString("N2");
                    Net_textBlock.Text = sum_dt.Rows[0].Field<double>("CashAccount_Net").ToString("N2");
                    CashNet_textBlock.Text = sum_dt.Rows[0].Field<double>("AllAccounts_Net").ToString("N2");
                    Difference_textBlock.Text = sum_dt.Rows[0].Field<double>("Difference").ToString("N2");
                }


                Bind_Show_Hide_Menu();
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
                if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") && !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") && !CHeader.Contains("#") && !Originally_HiddenColumns.Contains(CHeader))
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                int Action_ID = Convert.ToInt32(dataRow["id"].ToString());

                CashAction_Window _Window = new CashAction_Window(Account_ID, Account_Name, Action_ID, main_Window, this);
                _Window.ShowDialog();
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
                    DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                    int CashAction_ID = Convert.ToInt32(dataRow["id"].ToString());

                    a.Delete_CashAction(CashAction_ID, Account_Name, "CashTill_Window");

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

        private void AddNewAction_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CashAction_Window _Window = new CashAction_Window(Account_ID, Account_Name, main_Window, this);
                _Window.ShowDialog();
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
                            if (ExactSearch_checkBox.IsChecked == true)
                            {
                                if (r.ItemArray.ElementAt(i).ToString().ToLower() == Search_txtBox.Text.ToLower())
                                {
                                    new_dt.Rows.Add(r.ItemArray);
                                    break;
                                }
                            }
                            else
                            {
                                if (r.ItemArray.ElementAt(i).ToString().ToLower().Contains(Search_txtBox.Text.ToLower()))
                                {
                                    new_dt.Rows.Add(r.ItemArray);
                                    break;
                                }
                            }
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

        private void Search_txtBox_TextChanged(object sender, TextChangedEventArgs e)
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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

                                int Action_ID = Convert.ToInt32(dataRow["id"].ToString());

                                CashAction_Window _Window = new CashAction_Window(Account_ID, Account_Name, Action_ID, main_Window, this);
                                _Window.ShowDialog();


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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;

                All_textBlock.Text = myDataGrid.View.Records.Count().ToString();
                Export_btn.CommandTarget = myDataGrid;

                move_Columns();
                AddSums();
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

        private void myDataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            try
            {

                if (e.Column is GridNumericColumn && !e.Column.MappingName.Contains("id"))
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;



                //HIDE ID column.
                if (e.Column.MappingName.Contains("id"))
                    e.Cancel = true;
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

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void move_Columns()
        {
            if (myDataGrid.Columns.Count > 3)
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

        private void AddSums()
        {
            if (myDataGrid.TableSummaryRows.Count > 0)
            {
                myDataGrid.TableSummaryRows[0].SummaryColumns.Clear();
                myDataGrid.TableSummaryRows.RemoveAt(0);
            }

            ObservableCollection<ISummaryColumn> SummaryColumns = new ObservableCollection<ISummaryColumn>();

            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i] is GridNumericColumn)
                {
                    SummaryColumns.Add(new GridSummaryColumn()
                    {
                        Name = myDataGrid.Columns[i].MappingName,
                        MappingName = myDataGrid.Columns[i].MappingName,
                        SummaryType = SummaryType.DoubleAggregate,
                        Format = "{Sum:N2}"
                    });
                }
            }

            if (SummaryColumns.Count > 0)
            {
                myDataGrid.TableSummaryRows.Add(new GridTableSummaryRow()
                {
                    ShowSummaryInRow = false,
                    TitleColumnCount = 1,
                    Position = TableSummaryRowPosition.Bottom,
                    Title = "Total:",
                    SummaryColumns = SummaryColumns
                });
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Delete_Selected_TextBlock_Click(object sender, RoutedEventArgs e)
        {
            Delete_Selected_TextBlock.IsEnabled = false;
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    Login.DeleteMultiCashActions_sc.Parameters.Clear();
                    Login.DeleteMultiCashActions_List.Clear();

                    DataView newView = new DataView(dt);
                    newView.RowFilter = "IsSelected = 1";

                    if (newView.Count == 0)
                    {
                        MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                        return;
                    }

                    for (int k = 0; k < newView.Count; k++)
                    {
                        string temp_query = "";
                        string log_query = "";

                        var CashAction_ID = newView[k]["id"];

                        Login.DeleteMultiCashActions_sc.Parameters.AddWithValue("@Action_ID" + k, CashAction_ID);
                        Login.DeleteMultiCashActions_sc.Parameters.AddWithValue("@Acc_Name" + k, Account_Name);

                        Login.DeleteMultiCashActions_sc.Parameters.AddWithValue("@Form_Name" + k, "CashTill_Window");

                        temp_query += "DELETE FROM `cashaction` WHERE ID = @Action_ID" + k + ";";
                        temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'Cash action', @Action_ID" + k + ", CONCAT('account Name = ',@Acc_Name" + k + ") ,'', '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                        Login.DeleteMultiCashActions_List.Add(temp_query);
                        temp_query = "";

                    }

                    a.Delete_Multi_CashAction();

                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    LoadGrid();
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
            finally
            {
                Delete_Selected_TextBlock.IsEnabled = true;
            }
        }
    }
}


