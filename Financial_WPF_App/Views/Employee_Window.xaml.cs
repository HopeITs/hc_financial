using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Employee_Window.xaml
    /// </summary>
    public partial class Employee_Window : Window
    {
        public Employee_Window(Main_Window_2 main_Window)
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

        private Employee Emp;
        private SubProject s;
        private Line l;
        Main_Window_2 main_Window;

        int Employee_ID = -1;
        DataTable All_Employees_dt, Employee_Subproject_Table, Original_Employee_Subproject_Table, Subproject_dt, Lines_dt;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];


        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;
        #endregion

        private bool OnLoad()
        {
            try
            {
                Emp = new Employee();
                s = new SubProject();
                l = new Line();
                //Set_privileges();
                Load_Form();
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

        private void Load_Form()
        {
            Set_privileges();
            Show_Hide();
            Load_Comboboxes();
            Load_Grid();
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
                Save_btn.Visibility = Visibility.Collapsed;
            }
            if (!Edit_privileges)
            {
                Save_btn.Visibility = Visibility.Collapsed;
                FName_txtBox.IsReadOnly = FaName_txtBox.IsReadOnly = LName_txtBox.IsReadOnly = true;

                Subproject_label.Visibility = Subproject_comboBox.Visibility = Subproject_clear_btn.Visibility =
                    Line_label.Visibility = Line_comboBox.Visibility = Line_clear_btn.Visibility =
                    BeginDate_lable.Visibility = BeginDate_datepicker.Visibility =
                    EndDate_label.Visibility = EndDate_datepicker.Visibility =
                    Add_btn.Visibility = Visibility.Collapsed;

                foreach (var C in EmployeeDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Edit")
                    {
                        C.IsHidden = true;
                    }
            }
            if (!Delete_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Delete")
                    {
                        C.IsHidden = true;
                    }
            }
        }

        private void Load_Grid()
        {
            myDataGrid.ItemsSource = null;

            DataTable temp_dt = Emp.Select(-1, -1);


            if (Settings.Default.Role == "Financial Manager" || Settings.Default.Role == "Accountant" || Settings.Default.Role == "Project Manager")
            {
                List<int> User_Subprojects_ID = new List<int>();
                if (Settings.Default.User_Subprojects != null && !string.IsNullOrEmpty(Settings.Default.User_Subprojects.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_Subprojects.ToString()))
                {
                    User_Subprojects_ID = Settings.Default.User_Subprojects.ToString().Split(',').Select(int.Parse).ToList();

                    string filterExpression = string.Join(" OR ", User_Subprojects_ID.Select(id => "Subproject_ID = '" + id + "' " +
                    " OR Subproject_ID LIKE '%," + id + ",%'" +
                    " OR Subproject_ID LIKE '%," + id + "'" +
                    " OR Subproject_ID LIKE '" + id + ",%'"));

                    System.Data.DataRow[] rows = temp_dt.Select(filterExpression);
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        All_Employees_dt = selected_dt;

                    }
                }
                else
                {
                    All_Employees_dt = null;
                }

            }
            else
            {
                All_Employees_dt = temp_dt;
            }



            if (All_Employees_dt != null && All_Employees_dt.Rows.Count > 0)
                myDataGrid.ItemsSource = All_Employees_dt.DefaultView;
        }

        private void Load_Comboboxes()
        {
            Subproject_dt = s.Select("", "", "", "");

            Subproject_comboBox.ItemsSource = Subproject_dt.DefaultView;


            Lines_dt = l.Select_Lines_As_Employee_Position();
            Line_comboBox.ItemsSource = Lines_dt.DefaultView;
        }

        private void Clear_Data()
        {
            Employee_ID = -1;
            FName_txtBox.Text = ""; FaName_txtBox.Text = ""; LName_txtBox.Text = "";

            Subproject_comboBox.SelectedIndex = -1;
            Line_comboBox.SelectedIndex = -1;

            BeginDate_datepicker.SelectedDate = null;
            EndDate_datepicker.SelectedDate = null;

            EmployeeDataGrid.ItemsSource = null;
        }

        private void Clear_Subproject()
        {
            Subproject_comboBox.SelectedIndex = -1;
            Line_comboBox.SelectedIndex = -1;

            BeginDate_datepicker.SelectedDate = null;
            EndDate_datepicker.SelectedDate = null;
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                Export_btn.CommandTarget = myDataGrid;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                Employee_ID = Convert.ToInt32(dataRow["Employee_ID"].ToString());

                if (dataRow["FName"] != DBNull.Value || dataRow["FName"] != null)
                    FName_txtBox.Text = dataRow["FName"].ToString();

                if (dataRow["FaName"] != DBNull.Value || dataRow["FaName"] != null)
                    FaName_txtBox.Text = dataRow["FaName"].ToString();

                if (dataRow["LName"] != DBNull.Value || dataRow["LName"] != null)
                    LName_txtBox.Text = dataRow["LName"].ToString();


                Employee_Subproject_Table = Emp.Select_Detailed_Employee(Employee_ID);
                Original_Employee_Subproject_Table = Employee_Subproject_Table.Copy();

                EmployeeDataGrid.ItemsSource = Employee_Subproject_Table;
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

        private void Employee_DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                    Employee_ID = Convert.ToInt32(dataRow["Employee_ID"].ToString());
                    string Employee_Name = dataRow["Name"].ToString();
                    Emp.Delete(Employee_ID, Employee_Name, "Employee_Window");

                    MessageBox.Show("Deleted successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                    Clear_Data();
                    Load_Form();
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                { MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if(Edit_privileges)
                {
                    IInputElement element = e.MouseDevice.DirectlyOver;
                    if (element != null && element is FrameworkElement)
                    {
                        if (((FrameworkElement)element).Parent is GridCell)
                        {
                            var grid = sender as SfDataGrid;
                            if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                            {
                                DataRowView rowview = grid.SelectedItem as DataRowView;
                                Employee_ID = Convert.ToInt32(rowview["Employee_ID"].ToString());

                                if (rowview["FName"] != DBNull.Value || rowview["FName"] != null)
                                    FName_txtBox.Text = rowview["FName"].ToString();

                                if (rowview["FaName"] != DBNull.Value || rowview["FaName"] != null)
                                    FaName_txtBox.Text = rowview["FaName"].ToString();

                                if (rowview["LName"] != DBNull.Value || rowview["LName"] != null)
                                    LName_txtBox.Text = rowview["LName"].ToString();


                                Employee_Subproject_Table = Emp.Select_Detailed_Employee(Employee_ID);
                                Original_Employee_Subproject_Table = Employee_Subproject_Table.Copy();

                                EmployeeDataGrid.ItemsSource = Employee_Subproject_Table;
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

        private void Subproject_DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)EmployeeDataGrid.CurrentItem;
                int Current_Subproject_id = Convert.ToInt32(dataRow["Subproject_ID"].ToString());
                int Current_Line_id = Convert.ToInt32(dataRow["Line_ID"].ToString());

                System.Data.DataRow[] dtr = Employee_Subproject_Table.Select("Subproject_ID='" + Current_Subproject_id + "' and Line_ID='" + Current_Line_id + "'");
                foreach (var drow in dtr)
                {
                    drow.Delete();
                }
                Employee_Subproject_Table.AcceptChanges();
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
                Clear_Data();
                Load_Form();
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

        private void Subproject_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Subproject_comboBox.SelectedIndex = -1;
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

        private void Line_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Line_comboBox.SelectedIndex = -1;
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

        private void Add_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Subproject_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the center.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Line_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the position.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (BeginDate_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please select the start date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }



                if (EmployeeDataGrid.ItemsSource == null)
                {
                    Employee_Subproject_Table = new DataTable();
                    Employee_Subproject_Table.Columns.Add("Emplyee_Subproject_ID", typeof(int));
                    Employee_Subproject_Table.Columns.Add("Subproject_ID", typeof(int));
                    Employee_Subproject_Table.Columns.Add("SubprojectName", typeof(string));
                    Employee_Subproject_Table.Columns.Add("Line_ID", typeof(int));
                    Employee_Subproject_Table.Columns.Add("LineName", typeof(string));
                    Employee_Subproject_Table.Columns.Add("BeginDate", typeof(DateTime));
                    Employee_Subproject_Table.Columns.Add("EndDate", typeof(DateTime));

                    EmployeeDataGrid.ItemsSource = Employee_Subproject_Table;
                }

                System.Data.DataRow dr = Employee_Subproject_Table.NewRow();

                dr["Emplyee_Subproject_ID"] = -1;
                dr["Subproject_ID"] = Convert.ToInt32(Subproject_comboBox.SelectedValue);
                dr["SubprojectName"] = Subproject_comboBox.Text;
                dr["Line_ID"] = Convert.ToInt32(Line_comboBox.SelectedValue);
                dr["LineName"] = Line_comboBox.Text;
                dr["BeginDate"] = ((DateTime)BeginDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                if (EndDate_datepicker.SelectedDate != null)
                    dr["EndDate"] = ((DateTime)EndDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                Employee_Subproject_Table.Rows.Add(dr);
                Employee_Subproject_Table.AcceptChanges();

                Clear_Subproject();

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

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (FName_txtBox.Text.IsNullOrWhiteSpace() || FaName_txtBox.Text.IsNullOrWhiteSpace() || LName_txtBox.Text.IsNullOrWhiteSpace())
                {
                    MessageBox.Show("Please enter the firt name, father name and last name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Employee_Subproject_Table == null || Employee_Subproject_Table.Rows.Count == 0)
                {
                    MessageBox.Show("Please select one center at least.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Login.BuildConnection())
                {
                    string FName = FName_txtBox.Text;
                    string FaName = FaName_txtBox.Text;
                    string LName = LName_txtBox.Text;


                    DataTable Add_Subprojects_dt = new DataTable();
                    System.Data.DataRow[] rowsToAdd = Employee_Subproject_Table.Select("Emplyee_Subproject_ID = '-1'");
                    if (rowsToAdd.Count() > 0)
                        Add_Subprojects_dt = rowsToAdd.CopyToDataTable();

                    if (Employee_ID != -1)//Edit
                    {
                        DataTable Delete_Subprojects_dt = new DataTable();

                        var kept_Subprojects = Employee_Subproject_Table.AsEnumerable()
                            .Where(row => row.Field<int>("Emplyee_Subproject_ID") != -1)
                            .Select(row => row.Field<int>("Emplyee_Subproject_ID"));

                        // Select rows from dt1 where id is not in dt2Ids
                        var result = Original_Employee_Subproject_Table.AsEnumerable()
                                        .Where(row => !kept_Subprojects.Contains(row.Field<int>("Emplyee_Subproject_ID")));

                        if (result.Count() > 0)
                        {
                            Delete_Subprojects_dt = result.CopyToDataTable();
                        }



                        DataTable Update_Subprojects_dt = new DataTable();

                        var employeeTable = Employee_Subproject_Table.AsEnumerable();
                        var originalTable = Original_Employee_Subproject_Table.AsEnumerable();

                        var Updated_result = from emp in employeeTable
                                             join orig in originalTable
                                             on emp.Field<int>("Emplyee_Subproject_ID") equals orig.Field<int>("Emplyee_Subproject_ID")
                                             where emp.Field<int>("Emplyee_Subproject_ID") != -1
                                             && ((emp.IsNull("BeginDate") && !orig.IsNull("BeginDate"))
                                             || (!emp.IsNull("BeginDate") && orig.IsNull("BeginDate"))
                                                 || (emp.IsNull("EndDate") && !orig.IsNull("EndDate"))
                                                 || (!emp.IsNull("EndDate") && orig.IsNull("EndDate"))
                                                 || (!emp.IsNull("BeginDate") && !orig.IsNull("BeginDate") && emp.Field<DateTime>("BeginDate") != orig.Field<DateTime>("BeginDate"))
                                                 || (!emp.IsNull("EndDate") && !orig.IsNull("EndDate") && emp.Field<DateTime>("EndDate") != orig.Field<DateTime>("EndDate")))
                                             select emp;



                        if (Updated_result.Count() > 0)
                        {
                            Update_Subprojects_dt = Updated_result.CopyToDataTable();
                        }

                        Emp.Update(Employee_ID, FName, FaName, LName, Add_Subprojects_dt, Delete_Subprojects_dt, Update_Subprojects_dt, "Employee_Window");
                    }
                    else//Add new
                    {
                        Emp.Insert(FName, FaName, LName, Add_Subprojects_dt, "Employee_Window");
                    }
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);


                    Clear_Data();
                    Load_Form();
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
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }
    }
}
