using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_MultiSalaries_Window.xaml
    /// </summary>
    public partial class Add_MultiSalaries_Window : Window
    {
        public Add_MultiSalaries_Window(Monthly_Salaries_Window MS_W, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.MS_W = MS_W;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }


        Main_Window_2 main_Window;
        Monthly_Salaries_Window MS_W;
        private DataTable dt, Employee_Subprojects_dt;
        Employee Emp;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";
        private MultiSalaries_CustomCopyPaste copyPasteHandler;
        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                Export_btn.CommandTarget = myDataGrid;
            }
        }

        private bool OnLoad()
        {
            try
            {
                Emp = new Employee();
                Create_Columns();
                Fill_Datatables();
                InitializeCopyPaste();
                //this.myDataGrid.GridCopyPaste = new MultiSalaries_CustomCopyPaste(myDataGrid, Employee_Subprojects_dt);
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

        void InitializeCopyPaste()
        {
            copyPasteHandler = new MultiSalaries_CustomCopyPaste(myDataGrid, Employee_Subprojects_dt);
            this.myDataGrid.GridCopyPaste = copyPasteHandler;
            copyPasteHandler.PasteCompleted += CopyPasteHandler_PasteCompleted;
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
            myDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            myDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            myDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }
        private void Create_Columns()
        {

            Login.MultiSalaries_AnyError = false;
            Login.MultiSalaries_List.Clear();
            Login.MultiSalaries_sc.Parameters.Clear();


            dt = new DataTable();

            dt.Columns.Add("Name");
            dt.Columns.Add("Project");
            dt.Columns.Add("Position");
            dt.Columns.Add("Month");
            dt.Columns.Add("Basic");
            dt.Columns.Add("Adjustment");
            dt.Columns.Add("USD Basic");
            dt.Columns.Add("USD Adjustment");

            System.Data.DataRow newRow = dt.NewRow();
            dt.Rows.Add(newRow);

            myDataGrid.ItemsSource = null;
            myDataGrid.ItemsSource = dt.DefaultView;
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Create_Columns();
                InitializeCopyPaste();
                // this.myDataGrid.GridCopyPaste = new MultiSalaries_CustomCopyPaste(myDataGrid, Employee_Subprojects_dt);
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
                if (Login.MultiSalaries_AnyError)
                {
                    MessageBox.Show("Please make sure there are no errors in the entered data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                else
                {
                    if (Login.MultiSalaries_List.Count > 0)
                    {
                        Emp.Insert_Multi_Salaries();

                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                        Create_Columns();
                        Fill_Datatables();
                        InitializeCopyPaste();
                        //this.myDataGrid.GridCopyPaste = new MultiSalaries_CustomCopyPaste(myDataGrid, Employee_Subprojects_dt);


                        if (MS_W != null)
                            MS_W.LoadGrid();
                    }
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

        private void Fill_Datatables()
        {
            DataTable temp_dt = Emp.Select_Detailed_Employee(-1);

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
                        Employee_Subprojects_dt = selected_dt;
                    }
                }
                else
                {
                    Employee_Subprojects_dt = null;
                }
            }
            else
            {
                Employee_Subprojects_dt = temp_dt;
            }
        }

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Error_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Error_textBlock.ActualWidth;
            else
                Error_textBlock.MinWidth = All_textBlock.ActualWidth;
        }

        private void CopyPasteHandler_PasteCompleted(object sender, EventArgs e)
        {
            // Now you can access the copyPasteHandler
            All_textBlock.Text = copyPasteHandler.TotalPastedRows.ToString("N0");
            Error_textBlock.Text = copyPasteHandler.TotalErrorRows.ToString("N0");
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
