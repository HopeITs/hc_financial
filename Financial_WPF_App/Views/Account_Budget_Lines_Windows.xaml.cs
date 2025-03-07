using Financial_WPF_App.Helper;
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
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for AddBudgetLines_Windows.xaml
    /// </summary>
    public partial class AllAccountBudgetLines_Windows : Window
    {
        public AllAccountBudgetLines_Windows(string Account_ID,int Project_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.Project_ID = Project_ID;
            if (Account_ID != "") this.Account_ID = Convert.ToInt32(Account_ID);
            else this.Account_ID = -1;
            PassException = true;
            OnLoad();
            PassException = false;
        }

        private int Account_ID, Budget_ID, Project_ID;
        private Account a;
        private Budget b; private Budget_Lines bl;
        private DataTable Budgets_dt, BudgetLines_dt, BudgetLinesCopy_dt, a_dt, Donor_dt;
        Main_Window_2 main_Window;
        List<int> isDirty_list; List<int> Deleted_Lines;
        bool BudgetLines_IsDirty = false;
        string Query, Log_Query;
        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();
        bool PassException = false;
        bool ShowTotals = true;
        bool UserMode;

        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];


        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;
        #endregion

        private void OnLoad()
        {
            Set_privileges();
            Show_Hide();

            UserMode = false;
            a = new Account();
            b = new Budget();
            bl = new Budget_Lines();
            a_dt = new DataTable();

            Budget_ID = -1;
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
            }
            Total_RadioButton.Visibility = PerDonor_RadioButton.Visibility = Visibility.Collapsed;
            //Panel1.Visibility = Visibility.Collapsed;
            Load_Data();
            Apply_Color_Theme();
            UserMode = true;
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
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                myDataGrid.Columns[i].IsHidden = true;
            }
            Fill_Budgets();
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
                Edit_privileges = true;
            }
            else if (Settings.Default.Role == "Accountant")
            {

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

        private void Show_Hide()
        {
            if (!Add_privileges)
            {
                AddAllLines_btn.Visibility =  Visibility.Collapsed;
                AddMultiLines_btn.Visibility = Visibility.Collapsed;
            }
            if (!Edit_privileges)
            {
                Save_btn.Visibility = Visibility.Collapsed;
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

        private void Fill_Budgets()
        {
            Budgets_comboBox.ItemsSource = null;
            Budgets_dt = b.Select(Account_ID);

            if (Budgets_dt != null && Budgets_dt.Rows.Count > 0)
            {
                Budgets_comboBox.ItemsSource = Budgets_dt.DefaultView;
            }

            if (Budget_ID != -1)
            {
                Budgets_comboBox.SelectedValue = Budget_ID;
                Budgets_comboBox_SelectionChanged(Budgets_comboBox, null);
            }
        }

        public void Fill_myDataGrid(bool Update)
        {
            myDataGrid.ItemsSource = null;

            if (!Update)
            {
                BudgetLines_dt = bl.Select(Budget_ID);

                if (BudgetLines_dt != null)
                {
                    System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
                    BudgetLines_dt.Columns.Add(newColumn);
                    BudgetLines_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));
                }

                BudgetLinesCopy_dt = BudgetLines_dt.Copy();
                Donor_dt = b.Select_budget_donor(Account_ID, Budget_ID);

                Deleted_Lines = new List<int>();
                isDirty_list = new List<int>();
                BudgetLines_IsDirty = false;

                if (Donor_dt.Rows.Count > 1)
                {
                    //Panel1.Visibility = Visibility.Visible;
                    Total_RadioButton.Visibility = PerDonor_RadioButton.Visibility = Visibility.Visible;

                }
                else
                {
                    //Panel1.Visibility = Visibility.Collapsed;
                    Total_RadioButton.Visibility = PerDonor_RadioButton.Visibility = Visibility.Collapsed;

                }
            }

            if (BudgetLines_dt != null && BudgetLines_dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = BudgetLines_dt.DefaultView;

                if (HOffset != 0)
                    this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
            }
            else
            {
                myDataGrid.ItemsSource = null;
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].IsHidden = true;
                }
            }
        }

        private void AddAllLines_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Budget_ID != -1)
                {
                    string BudgetName = Budgets_dt.AsEnumerable()
                         .Where(row => row.Field<int>("ID") == Budget_ID)
                         .Select(row => row.Field<string>("Name"))
                         .FirstOrDefault();

                    Add_Misssing_Budget_Lines _Window = new Add_Misssing_Budget_Lines(Budget_ID, BudgetName, Project_ID, main_Window, this);
                    _Window.ShowDialog();

                }
                else
                {
                    MessageBox.Show("Please select the budget first.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {

                    DataRowView row = (DataRowView)myDataGrid.CurrentItem;
                    int Bud_Line_ID = Convert.ToInt32(row.Row["budget_line_ID"].ToString());

                    Deleted_Lines.Add(Bud_Line_ID);
                    //isDirty_list.RemoveAt(myDataGrid.SelectedIndex);
                    isDirty_list.Remove(Bud_Line_ID);
                    //BudgetLines_dt.Rows.RemoveAt(myDataGrid.SelectedIndex);

                    System.Data.DataRow[] dtr = BudgetLines_dt.Select("budget_line_ID='" + Bud_Line_ID + "'"); //name is the column in the data table
                    foreach (var drow in dtr)
                    {
                        drow.Delete();
                    }
                    BudgetLines_dt.AcceptChanges();

                    BudgetLines_IsDirty = true;
                    Fill_myDataGrid(true);
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                {
                    MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    Fill_myDataGrid(false);
                }
                else if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Budgets_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (Budgets_comboBox.SelectedIndex != -1)
                {
                    Budget_ID = Convert.ToInt32(Budgets_comboBox.SelectedValue);
                    Fill_myDataGrid(false);
                    Export_btn.CommandTarget = myDataGrid;
                }
                else
                {
                    Budget_ID = -1;
                    myDataGrid.ItemsSource = null;
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
                if (PassException)
                    throw;
            }
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (!BudgetLines_IsDirty)
                {
                    MessageBox.Show("There are no changes to save.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                else
                {
                    Query = "";
                    Log_Query = "";

                    if (myDataGrid.ItemsSource != null)
                    {
                        DataView dataView = myDataGrid.ItemsSource as DataView;
                        for (int i = 0; i < dataView.Count; i++)
                        {
                            DataRowView row = dataView[i];
                            if (isDirty_list.Contains((int)row.Row["budget_line_ID"]))
                            {
                                Query += "UPDATE `budget_line` SET `Unit`='" + row.Row["Unit"].ToString() + "', `Timing`='" + row.Row["Timing"].ToString() + "', `Price`='" + row.Row["Price"].ToString() + "' WHERE ID = " + (int)row.Row["budget_line_ID"] + ";";
                                Log_Query = "UPDATE `budget_line` SET `Unit`=''" + row.Row["Unit"].ToString() + "'', `Timing`=''" + row.Row["Timing"].ToString() + "'', `Price`=''" + row.Row["Price"].ToString() + "'' WHERE ID = " + (int)row.Row["budget_line_ID"] + ";";
                                Query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES ('AllAccountBudgetLines_Windows', 'UPDATE', 'Budget_line', '" + (int)row.Row["budget_line_ID"] + "', CONCAT('Line Name = ', '" + Login.ReplaceQuotation(row.Row["line"].ToString()) + "'), CONCAT('Account ID = ', '" + Account_ID + "') , '" + Log_Query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                            }
                        }
                    }


                    for (int i = 0; i < Deleted_Lines.Count; i++)
                    {
                        Query += "DELETE FROM `budget_line` WHERE ID = " + Deleted_Lines[i] + ";";
                        Log_Query = "DELETE FROM `budget_line` WHERE ID = " + Deleted_Lines[i] + ";";
                        System.Data.DataRow[] rows = BudgetLinesCopy_dt.Select("budget_line_ID =" + Deleted_Lines[i]);
                        string Name = rows[0]["line"].ToString();
                        Query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES ('AllAccountBudgetLines_Windows', 'DELETE', 'Budget_line', '" + Deleted_Lines[i] + "', CONCAT('Line Name = ', '" + Name + "'), CONCAT('Account ID = ', '" + Account_ID + "') , '" + Log_Query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                    Login.Execute_Query(Query);
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    Fill_myDataGrid(false);
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                {
                    MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    Fill_myDataGrid(false);
                }
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
                Save_btn.IsEnabled = true;
            }
        }

        private void myDataGrid_AutoGeneratingColumn(object sender, Syncfusion.UI.Xaml.Grid.AutoGeneratingColumnArgs e)
        {

            try
            {
                if (e.Column is GridNumericColumn && !e.Column.MappingName.Contains("_ID") && e.Column.MappingName != "ID" &&
                    e.Column.MappingName != "Unit" && e.Column.MappingName != "Timing")
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;

                //HIDE ID column.
                if (e.Column.MappingName.Contains("_ID") || e.Column.MappingName == "ID")
                    e.Cancel = true;


                if (e.Column.MappingName == "Unit" || e.Column.MappingName == "Timing" || e.Column.MappingName == "Price")
                {
                    if (Edit_privileges)
                        e.Column.AllowEditing = true;
                    else
                        e.Column.AllowEditing = false;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void move_Columns()
        {
            if (myDataGrid.Columns.Count > 2)
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

        private void myDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
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

        private void myDataGrid_CurrentCellEndEdit(object sender, Syncfusion.UI.Xaml.Grid.CurrentCellEndEditEventArgs e)
        {
            try
            {
                BudgetLines_IsDirty = true;

                var recordIndex = this.myDataGrid.ResolveToRecordIndex(e.RowColumnIndex.RowIndex);
                var columnIndex = this.myDataGrid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex);
                var col_Name = this.myDataGrid.Columns[columnIndex].MappingName;
                var record = this.myDataGrid.View.Records.GetItemAt(recordIndex);


                int Unit = 0, Timing = 0;
                decimal Price = 0;
                //DataRowView row = myDataGrid.Items[row_index] as DataRowView;

                DataRowView row = (DataRowView)myDataGrid.CurrentItem;
                isDirty_list.Add(Int32.Parse(row.Row["budget_line_ID"].ToString()));

                if (col_Name == "Unit")
                {
                    Unit = Int32.Parse(this.myDataGrid.View.GetPropertyAccessProvider().GetValue(record, col_Name).ToString());
                    Timing = Int32.Parse(row.Row["Timing"].ToString());
                    Price = decimal.Parse(row.Row["Price"].ToString());
                }
                else if (col_Name == "Timing")
                {
                    Unit = Int32.Parse(row.Row["Unit"].ToString());
                    Timing = Int32.Parse(this.myDataGrid.View.GetPropertyAccessProvider().GetValue(record, col_Name).ToString());
                    Price = decimal.Parse(row.Row["Price"].ToString());
                }
                else if (col_Name == "Price")
                {
                    Unit = Int32.Parse(row.Row["Unit"].ToString());
                    Timing = Int32.Parse(row.Row["Timing"].ToString());
                    Price = Decimal.Parse(this.myDataGrid.View.GetPropertyAccessProvider().GetValue(record, col_Name).ToString());
                }

                decimal ExpTotal = Unit * Timing * Price;

                System.Data.DataRow datarow = BudgetLines_dt.Select("budget_line_ID=" + row.Row["budget_line_ID"]).FirstOrDefault();
                datarow["Unit"] = Unit.ToString();
                datarow["Timing"] = Timing.ToString();
                datarow["Price"] = Price.ToString();
                datarow["Expected Total"] = ExpTotal.ToString();
                datarow["Remaining"] = (ExpTotal - decimal.Parse(datarow["Expenditures Total"].ToString())).ToString();


                for (int i = 0; i < Donor_dt.Rows.Count; i++)
                {
                    string donor = Donor_dt.Rows[i]["Donor"].ToString();
                    decimal rate = decimal.Parse(Donor_dt.Rows[i]["Rate"].ToString());
                    datarow[donor + " Expected Total"] = (ExpTotal * rate / 100).ToString();
                    datarow[donor + " Remaining"] = ((ExpTotal - decimal.Parse(datarow[donor + " Expenditures Total"].ToString())) * rate / 100).ToString(); ;
                }

                myDataGrid.ItemsSource = null;
                if (BudgetLines_dt != null && BudgetLines_dt.Rows.Count > 0)
                {
                    myDataGrid.ItemsSource = BudgetLines_dt.DefaultView;
                    myDataGrid.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
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

        private void myDataGrid_ItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e)
        {
            try
            {
                if (myDataGrid.StackedHeaderRows.Count != 0)
                    myDataGrid.StackedHeaderRows.RemoveAt(0);

                if (myDataGrid.ItemsSource != null && !ShowTotals)
                {
                    add_stackedHeader();
                    rename_Columns();
                }
                move_Columns();
                Bind_Show_Hide_Menu();

                AddSums();

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
                {
                    if (myDataGrid.Columns[i].IsHidden == false)
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
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    if (myDataGrid.Columns[i].HeaderText.ToString() == CHeader)
                    {
                        myDataGrid.Columns[i].IsHidden = false;

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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void Budget_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Budgets_comboBox.SelectedIndex = -1;
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

        private void AddMultiLines_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Budget_ID != -1)
                {
                    string BudgetName = Budgets_dt.AsEnumerable()
                     .Where(row => row.Field<int>("ID") == Budget_ID)
                     .Select(row => row.Field<string>("Name"))
                     .FirstOrDefault();

                    Add_Multi_Budget_Lines _Window = new Add_Multi_Budget_Lines(Budget_ID, BudgetName, Project_ID,  main_Window, this);
                    _Window.ShowDialog();

                }
                else
                {
                    MessageBox.Show("Please select the budget first.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
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

        private void Total_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = true;
                if (UserMode)
                {
                    Fill_myDataGrid(true);
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

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;
        }

        private void PerDonor_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = false;
                if (UserMode)
                {
                    Fill_myDataGrid(true);
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

        private void add_stackedHeader()
        {
            var StackedHeaderRow1 = new StackedHeaderRow { };

            var ExpectedTotalColumn = new StackedColumn
            {
                HeaderText = "Expected Total",
                MappingName = "ExpectedTotal",
            };
            var ExpendituresTotalColumn = new StackedColumn
            {
                HeaderText = "Expenditures Total",
                MappingName = "ExpendituresTotal"
            };
            var RemainingColumn = new StackedColumn
            {
                HeaderText = "Remaining",
                MappingName = "Remaining"
            };

            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i].MappingName != null)
                {
                    if (myDataGrid.Columns[i].MappingName.Contains("Expected Total"))
                    {
                        if (ExpectedTotalColumn.ChildColumns == "")
                            ExpectedTotalColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            ExpectedTotalColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Expenditures Total"))
                    {
                        if (ExpendituresTotalColumn.ChildColumns == "")
                            ExpendituresTotalColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            ExpendituresTotalColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Remaining"))
                    {
                        if (RemainingColumn.ChildColumns == "")
                            RemainingColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            RemainingColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                }
            }

            StackedHeaderRow1.StackedColumns.Add(ExpectedTotalColumn);
            StackedHeaderRow1.StackedColumns.Add(ExpendituresTotalColumn);
            StackedHeaderRow1.StackedColumns.Add(RemainingColumn);

            myDataGrid.StackedHeaderRows.Add(StackedHeaderRow1);

            //Default GridStackedCellRenderer is removed.
            myDataGrid.CellRenderers.Remove("StackedHeader");

            //Customized GridStackedCellRenderer is added.
            myDataGrid.CellRenderers.Add("StackedHeader", new GridCustomStackedRenderer());
        }

        private void rename_Columns()
        {
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i].MappingName != null)
                {
                    if (myDataGrid.Columns[i].MappingName.Contains("Expected Total"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Expected Total")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Expected Total", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Expenditures Total"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Expenditures Total")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Expenditures Total", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Remaining"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Remaining")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Remaining", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
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

            if (myDataGrid.GroupSummaryRows.Count > 0)
            {
                myDataGrid.GroupSummaryRows[0].SummaryColumns.Clear();
                myDataGrid.GroupSummaryRows.RemoveAt(0);
            }

            ObservableCollection<ISummaryColumn> SummaryColumns = new ObservableCollection<ISummaryColumn>();
            ObservableCollection<ISummaryColumn> GroupSummaryColumns = new ObservableCollection<ISummaryColumn>();


            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i] is GridNumericColumn && myDataGrid.Columns[i].MappingName != "Unit" &&
                    myDataGrid.Columns[i].MappingName != "Timing" && myDataGrid.Columns[i].MappingName != "Price")
                {
                    var summaryColumn = new GridSummaryColumn()
                    {
                        Name = myDataGrid.Columns[i].MappingName,
                        MappingName = myDataGrid.Columns[i].MappingName,
                        SummaryType = SummaryType.DoubleAggregate,
                        Format = "{Sum:N2}"
                    };
                    SummaryColumns.Add(summaryColumn);
                    GroupSummaryColumns.Add(summaryColumn);
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

            if (GroupSummaryColumns.Count > 0)
            {
                myDataGrid.GroupSummaryRows.Add(new GridSummaryRow()
                {
                    ShowSummaryInRow = false,
                    TitleColumnCount = 1,
                    Title = "Total:",
                    SummaryColumns = GroupSummaryColumns
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

                if (BudgetLines_dt != null)
                {
                    SearchDataGrid searchHelper = new SearchDataGrid(BudgetLines_dt);

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

        private void Delete_Selected_btn_Click(object sender, RoutedEventArgs e)
        {
            Delete_Selected_btn.IsEnabled = false;
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    Login.DeleteMultiCashActions_sc.Parameters.Clear();
                    Login.DeleteMultiCashActions_List.Clear();

                    DataView newView = new DataView(BudgetLines_dt);
                    newView.RowFilter = "IsSelected = 1";

                    if (newView.Count == 0)
                    {
                        MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }


                    for (int k = 0; k < newView.Count; k++)
                    {
                        var Bud_Line_ID = newView[k]["budget_line_id"];

                        Deleted_Lines.Add(Convert.ToInt32(Bud_Line_ID.ToString()));
                        isDirty_list.Remove(Convert.ToInt32(Bud_Line_ID.ToString()));

                        System.Data.DataRow[] dtr = BudgetLines_dt.Select("budget_line_ID='" + Bud_Line_ID + "'"); //name is the column in the data table
                        foreach (var drow in dtr)
                        {
                            drow.Delete();
                        }
                        BudgetLines_dt.AcceptChanges();

                        BudgetLines_IsDirty = true;
                        Fill_myDataGrid(true);
                        k--;
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
                {
                    MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    Fill_myDataGrid(false);
                }
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
    }
}
