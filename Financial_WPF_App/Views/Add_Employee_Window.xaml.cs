using Financial_WPF_App.Models;
using Syncfusion.Windows.Shared;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_Employee_Window.xaml
    /// </summary>
    public partial class Add_Employee_Window : Window
    {
        public Add_Employee_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.Employee_ID = -1;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Add_Employee_Window(int Employee_ID, DataTable All_Employees_dt, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.Employee_ID = Employee_ID;
            this.All_Employees_dt = All_Employees_dt;
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
        private Financial_WPF_App.Models.Line l;
        Main_Window_2 main_Window;

        int Employee_ID = -1;
        DataTable All_Employees_dt, Employee_Subproject_Table, Original_Employee_Subproject_Table, Subproject_dt, Lines_dt;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";

        private bool OnLoad()
        {
            try
            {
                Emp = new Employee();
                s = new SubProject();
                l = new Financial_WPF_App.Models.Line();
      
                Load_Form();
                if(Employee_ID > 0)
                {
                    Load_Employee();
                }
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

        private void Load_Employee()
        {
            DataTable Emp_dt = All_Employees_dt.Select("Employee_ID = " + Employee_ID + "").CopyToDataTable();

            if (Emp_dt.Rows[0]["FName"] != DBNull.Value || Emp_dt.Rows[0]["FName"] != null)
                FName_txtBox.Text = Emp_dt.Rows[0].Field<string>("FName").ToString();

            if (Emp_dt.Rows[0]["FaName"] != DBNull.Value || Emp_dt.Rows[0]["FaName"] != null)
                FaName_txtBox.Text = Emp_dt.Rows[0].Field<string>("FaName").ToString();

            if (Emp_dt.Rows[0]["LName"] != DBNull.Value || Emp_dt.Rows[0]["LName"] != null)
                LName_txtBox.Text = Emp_dt.Rows[0].Field<string>("LName").ToString();


            Employee_Subproject_Table = Emp.Select_Detailed_Employee(Employee_ID);
            Original_Employee_Subproject_Table = Employee_Subproject_Table.Copy();

            EmployeeDataGrid.ItemsSource = Employee_Subproject_Table;
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
            EmployeeDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            EmployeeDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            EmployeeDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }

        private void Load_Form()
        {
            Load_Comboboxes();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
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
                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        this.Close();
                    }
                    else//Add new
                    {
                        Emp.Insert(FName, FaName, LName, Add_Subprojects_dt, "Employee_Window");
                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        Clear_Data();
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
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }
    }
}
