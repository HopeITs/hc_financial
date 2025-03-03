using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Monthly_Salaries_Window.xaml
    /// </summary>
    public partial class Monthly_Salaries_Window : Window
    {
        public Monthly_Salaries_Window(Main_Window_2 main_Window)
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
        private Employee Emp;
        DataTable dt;
        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        #region privileges
        bool Add_privileges, Delete_privileges;
        #endregion

        private bool OnLoad()
        {
            try
            {
                Emp = new Employee();

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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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

        private void Load_Data()
        {
            Set_privileges();
            Show_Hide();
            LoadGrid();
        }

        private void Set_privileges()
        {
            Add_privileges = Delete_privileges = false;

            if (Settings.Default.Role == "Head Office")
            {
                Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Project Manager")
            {

            }
            else if (Settings.Default.Role == "Financial Manager")
            {
                Add_privileges = Delete_privileges = true;
            }
            else if (Settings.Default.Role == "Accountant")
            {
                Add_privileges = Delete_privileges = true;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Show_Hide()
        {
            if (!Add_privileges)
            {
                AddMultiSalaries_btn.Visibility =  Visibility.Collapsed;
            }
            if (!Delete_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Delete" || C.HeaderText.ToString() == "Select")
                    {
                        C.IsHidden = true;
                    }

                Delete_Selected.Visibility = Visibility.Collapsed;
            }
        }

        public void LoadGrid()
        {

            if (Login.BuildConnection())
            {
                DataTable temp_dt = Emp.Select_Employee_Salary(-1);


                if (Settings.Default.Role == "Accountant" || Settings.Default.Role == "Financial Manager" || Settings.Default.Role == "Project Manager")
                {
                    List<int> User_Subprojects_ID = new List<int>();
                    if (Settings.Default.User_Subprojects != null && !string.IsNullOrEmpty(Settings.Default.User_Subprojects.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_Subprojects.ToString()))
                    {
                        User_Subprojects_ID = Settings.Default.User_Subprojects.ToString().Split(',').Select(int.Parse).ToList();

                        string filterExpression = string.Join(" OR ", User_Subprojects_ID.Select(id => "Subproject_ID = " + id));
                        System.Data.DataRow[] rows = temp_dt.Select(filterExpression);
                        DataTable selected_dt = new DataTable();
                        if (rows.Length != 0)
                        {
                            selected_dt = rows.CopyToDataTable();
                            dt = selected_dt;

                        }
                    }
                    else
                    {
                        dt = null;
                    }

                }
                else
                {
                    dt = temp_dt;
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
                }

            }
        }

        private void myDataGrid_FilterChanged(object sender, Syncfusion.UI.Xaml.Grid.GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;
                Export_btn.CommandTarget = myDataGrid;

                int count = myDataGrid.View.Records.Count();
                All_textBlock.Text = count.ToString("N0");

               // All_textBlock.Text = myDataGrid.View.Records.Count().ToString();

            }
        }

        void ScrollOwner_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange > 0 || e.HorizontalChange < 0)
            {
                HOffset = e.HorizontalOffset;
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

        private void AddMultiSalaries_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiSalaries_Window _Window = new Add_MultiSalaries_Window(this, main_Window);
                _Window.ShowDialog();

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
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                    int Employee_Salary_ID = Convert.ToInt32(dataRow["Employee_Salary_ID"].ToString());
                    int Employee_ID = Convert.ToInt32(dataRow["Employee_ID"].ToString());
                    string Line_Name = dataRow["Line"].ToString();
                    Emp.Delete_Employee_Salary(Employee_Salary_ID, Employee_ID, Line_Name, "Monthly_Salaries_Window");

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

                if (dt != null)
                {
                    SearchDataGrid searchHelper = new SearchDataGrid(dt);

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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Delete_Selected_btn_Click(object sender, RoutedEventArgs e)
        {
            Delete_Selected_btn.IsEnabled = false;
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    Login.DeleteMultiMonthlySalaries_sc.Parameters.Clear();
                    Login.DeleteMultiMonthlySalaries_List.Clear();

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

                        var Employee_Salary_ID = newView[k]["Employee_Salary_ID"];
                        var Employee_ID = newView[k]["Employee_ID"];
                        var Line_Name = newView[k]["Line"];



                        Login.DeleteMultiMonthlySalaries_sc.Parameters.AddWithValue("@Employee_Salary_ID" + k, Employee_Salary_ID);
                        Login.DeleteMultiMonthlySalaries_sc.Parameters.AddWithValue("@Employee_ID" + k, Employee_ID);
                        Login.DeleteMultiMonthlySalaries_sc.Parameters.AddWithValue("@Line_Name" + k, Line_Name);
                        Login.DeleteMultiMonthlySalaries_sc.Parameters.AddWithValue("@Form_Name" + k, "Monthly_Salaries_Window");


                        temp_query += "DELETE FROM `employee_salary` WHERE ID = @Employee_Salary_ID" + k + ";";
                        temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'DELETE', 'employee_salary', @Employee_Salary_ID" + k + ", CONCAT('Employee ID = ', @Employee_ID" + k + "), CONCAT('Line Name = ',  @Line_Name" + k + "), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                        Login.DeleteMultiMonthlySalaries_List.Add(temp_query);
                        temp_query = "";

                    }

                    Emp.Delete_Multi_Employee_Salary();

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
                Delete_Selected_btn.IsEnabled = true;
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
    }
}
