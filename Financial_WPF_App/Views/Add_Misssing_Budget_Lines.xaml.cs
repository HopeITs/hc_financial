using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_Misssing_Budget_Lines.xaml
    /// </summary>
    public partial class Add_Misssing_Budget_Lines : Window
    {
        public Add_Misssing_Budget_Lines(int Budget_ID, string BudgetName, int Project_ID, Main_Window_2 main_Window, AllAccountBudgetLines_Windows Bl_W)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Bl_W = Bl_W;
            this.Budget_ID = Budget_ID;
            this.Budget_Name = BudgetName;
            this.Project_ID = Project_ID;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        Main_Window_2 main_Window;
        AllAccountBudgetLines_Windows Bl_W;

        int Budget_ID;
        string Budget_Name;
        int Project_ID;
        private Budget_Lines bl;

        double HOffset = 0;

        private DataTable MissingLiens_dt;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];

        private bool OnLoad()
        {
            try
            {
                bl = new Budget_Lines();

                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                    myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
                }

                Load_Data();
                Apply_Color_Theme();
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


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Apply_DataGrid_Color_Theme()
        {
            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UpdateDGInContainer(myDataGrid, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
        public void Load_Data()
        {
            LoadGrid();
        }

        void ScrollOwner_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange > 0 || e.HorizontalChange < 0)
            {
                HOffset = e.HorizontalOffset;
            }
        }

        private void LoadGrid()
        {
            MissingLiens_dt = bl.Select_Missing_Lines(Budget_ID, Project_ID);

            System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
            MissingLiens_dt.Columns.Add(newColumn);

            MissingLiens_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));

            if (MissingLiens_dt != null && MissingLiens_dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = MissingLiens_dt.DefaultView;
                if (HOffset != 0)
                    this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
            }
            else
            {
                myDataGrid.ItemsSource = null;
            }
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;
            }
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                Login.MissingBudgetLines_sc.Parameters.Clear();
                Login.MissingBudgetLines_List.Clear();

                DataView newView = new DataView(MissingLiens_dt);
                newView.RowFilter = "IsSelected = 1";

                if(newView.Count == 0)
                {
                    MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                if(Login.MissingBudgetLines_List.Count == 0)
                {
                    for (int k = 0; k < newView.Count; k++)
                    {

                        string temp_query = "";
                        string log_query = "";

                        var Line_ID = newView[k]["Line_ID"];
                        var Line_Name = newView[k]["Line"];
                        var Unit = newView[k]["Unit"];
                        var Timing = newView[k]["Timing"];
                        var Price = newView[k]["Price"];

                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Budget_ID" + k, Budget_ID);
                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Line_Name" + k, Line_Name);

                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Line_ID" + k, Line_ID);
                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Unit" + k, Unit);
                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Timing" + k, Timing);
                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Price" + k, Price);


                        string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);
                        Login.MissingBudgetLines_sc.Parameters.AddWithValue("@Form_Name" + k, "Add_Misssing_Budget_Lines");


                        temp_query += @"INSERT INTO budget_line(budget_line.Budget_ID, budget_line.Line_ID, budget_line.Unit ,budget_line.Timing ,budget_line.Price)
                            VALUES (@Budget_ID" + k + ",@Line_ID" + k + ",@Unit" + k + ",@Timing" + k + ",@Price" + k + ");";

                        temp_query += " SET @budget_line_ID" + k + " = Last_Insert_ID();";

                        temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'budget_line', @budget_line_ID" + k + ", CONCAT('Line = ', @Line_Name" + k + "), CONCAT('Budget ID = ',@Budget_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                        Login.MissingBudgetLines_List.Add(temp_query);
                        temp_query = "";
                    }

                    bl.Insert_Missing_Lines();


                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    LoadGrid();
                    if (Bl_W != null)
                        Bl_W.Fill_myDataGrid(false);
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
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }

        private void myDataGrid_FilterChanged(object sender, Syncfusion.UI.Xaml.Grid.GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);

            DataTable dt = ((DataView)myDataGrid.ItemsSource).ToTable();

            if (headerCheckbox != null)
            {
                var allChecked = dt.AsEnumerable().All(row => row.Field<int>("IsSelected") == 1);
                var anyChecked = dt.AsEnumerable().Any(row => row.Field<int>("IsSelected") == 1);
                headerCheckbox.IsChecked = allChecked ? true : (anyChecked ? null : (bool?)false);
            }
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

        private CheckBox headerCheckbox;

        private void chkHeader_Loaded(object sender, RoutedEventArgs e)
        {
            headerCheckbox = sender as CheckBox;
        }

        private void myDataGrid_CurrentCellValueChanged(object sender, CurrentCellValueChangedEventArgs e)
        {
            var columnIndex = this.myDataGrid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex);
            var column = myDataGrid.Columns[columnIndex];
            if (column.MappingName == "IsSelected")
            {
                if (headerCheckbox != null)
                {
                    var allChecked = MissingLiens_dt.AsEnumerable().All(row => row.Field<int>("IsSelected") == 1);
                    var anyChecked = MissingLiens_dt.AsEnumerable().Any(row => row.Field<int>("IsSelected") == 1);
                    headerCheckbox.IsChecked = allChecked ? true : (anyChecked ? null : (bool?)false);
                }
            }
        }

        private void myDataGrid_ItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e)
        {
            try
            {
                Apply_DataGrid_Color_Theme();
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

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowUtils.MinimizeWindow(this);
        }

        private void Close_Click(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowUtils.CloseWindow(this);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowUtils.TitleBar_MouseDown(this, e);
        }
    }
}
