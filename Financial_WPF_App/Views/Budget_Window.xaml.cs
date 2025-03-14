using Financial_WPF_App.Models;
using Financial_WPF_App.Views;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App
{
    /// <summary>
    /// Interaction logic for Budget_Window.xaml
    /// </summary>
    public partial class Budget_Window : Window
    {
        public Budget_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            Cat_ID = ID = -1;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private int Cat_ID, ID;
        private Line line;
        private LineCategory cat;
        private Project project;
        private DataTable dt, cat_dt, projects_dt;
        Main_Window_2 main_Window;

        List<Local_Project> objProjectList;
        List<Local_Project> Original_ProjectList;
        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";
        private class Local_Project
        {
            public int ID { set; get; }

            public string Name { set; get; }

            public Boolean Project_Check_Status { set; get; }
        }

        private bool OnLoad()
        {
            try
            {
                line = new Line();
                cat = new LineCategory();
                dt = new DataTable();
                cat_dt = new DataTable();
                projects_dt = new DataTable();
                project = new Project();

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
        private void Load_Data()
        {
            Load_Comboboxes_Data();
            LoadGrid();
        }

        private void LoadGrid()
        {

            if (Login.BuildConnection())
            {
                dt = line.Select("", "");
                if (dt != null && dt.Rows.Count > 0)
                {
                    myDataGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }
            }

        }

        private void Load_Comboboxes_Data()
        {
            if (Login.BuildConnection())
            {
                cat_dt = cat.Select("");
                if (cat_dt != null && cat_dt.Rows.Count > 0)
                {
                    Category_comboBox.ItemsSource = cat_dt.DefaultView;
                }
                else
                {
                    Category_comboBox.ItemsSource = null;
                }

                Project_comboBox.ItemsSource = null;
                Project_comboBox.Text = "0 Selected";
                projects_dt = project.Select("", "", "");

                if (projects_dt != null && projects_dt.Rows.Count > 0)
                {
                    objProjectList = new List<Local_Project>();

                    for (int i = 0; i < projects_dt.Rows.Count; i++)
                    {
                        Local_Project obj = new Local_Project();
                        obj.ID = projects_dt.Rows[i].Field<int>(0);
                        obj.Name = projects_dt.Rows[i].Field<string>(1);
                        objProjectList.Add(obj);
                    }
                    Project_comboBox.ItemsSource = objProjectList;
                    int count = objProjectList.Count(item => item.Project_Check_Status == true);
                    Project_comboBox.Text = count.ToString() + " Selected";
                }
            }

        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;


                }

                if (Category_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the category.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }

                int ProjectFound = 0;
                if (objProjectList != null && objProjectList.Count > 0)
                {
                    foreach (var Project in objProjectList)
                    {
                        if (Project.Project_Check_Status == true)
                        {
                            ProjectFound++;
                        }
                    }
                }

                if (ProjectFound == 0)
                {
                    MessageBox.Show("Please select the project.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Login.BuildConnection())
                {
                    string Name = Login.ReplaceQuotation(Name_txtBox.Text);
                    int Category_ID = Convert.ToInt32(Category_comboBox.SelectedValue);

                    List<string> Add_Projects = new List<string>();
                    List<string> Delete_Projects = new List<string>();

                    bool Found;

                    if (objProjectList != null && objProjectList.Count > 0)
                        foreach (var Project in objProjectList)
                        {
                            if (Project.Project_Check_Status == true)
                            {
                                Found = false;
                                if (Original_ProjectList != null && Original_ProjectList.Count > 0)
                                    foreach (var OriginalProject in Original_ProjectList)
                                    {
                                        if (OriginalProject.ID == Project.ID && OriginalProject.Project_Check_Status == true)
                                        {
                                            Found = true;
                                            break;
                                        }
                                    }
                                if (Found == false)
                                    Add_Projects.Add(Project.ID.ToString());
                            }
                        }

                    if (Original_ProjectList != null && Original_ProjectList.Count > 0)
                        foreach (var OriginalProject in Original_ProjectList)
                        {
                            if (OriginalProject.Project_Check_Status == true)
                            {
                                Found = false;
                                if (objProjectList != null && objProjectList.Count > 0)
                                    foreach (var Project in objProjectList)
                                    {
                                        if (OriginalProject.ID == Project.ID && Project.Project_Check_Status == true)
                                        {
                                            Found = true;
                                            break;
                                        }
                                    }
                                if (Found == false)
                                    Delete_Projects.Add(OriginalProject.ID.ToString());
                            }
                        }

                    if (ID != -1)//Edit
                    {
                        line.Update(ID, Name, Category_ID, Add_Projects, Delete_Projects, "Budget_Window");
                    }
                    else//Add new
                    {
                        line.Insert(Name, Category_ID, Add_Projects, "Budget_Window");
                    }

                    Name_txtBox.Text = "";
                    Category_comboBox.SelectedIndex = -1;
                    ID = -1;

                    foreach (var Project in objProjectList)
                    {
                        Project.Project_Check_Status = false;
                    }
                    Project_comboBox.Text = "0 Selected";
                    LoadGrid();
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                ID = Convert.ToInt32(dataRow[0].ToString());
                Name_txtBox.Text = dataRow[2].ToString();

                if (dataRow[3] != DBNull.Value)
                    Category_comboBox.SelectedValue = Convert.ToInt32(dataRow[3].ToString());

                List<string> AllProject_ID = new List<string>();

                if (dataRow["Projects_id"] != DBNull.Value && dataRow["Projects_id"] != null)
                {
                    AllProject_ID = dataRow["Projects_id"].ToString().Split(',').ToList();

                    foreach (var Project in objProjectList)
                    {
                        Project.Project_Check_Status = false;
                    }

                    Original_ProjectList = new List<Local_Project>();
                    for (int j = 0; j < AllProject_ID.Count; j++)
                    {
                        if (objProjectList != null && objProjectList.Count > 0)
                        {
                            foreach (var Project in objProjectList)
                            {
                                if (Project.ID.ToString() == AllProject_ID[j])
                                {
                                    Project.Project_Check_Status = true;

                                    Local_Project obj = new Local_Project();
                                    obj.ID = Project.ID;
                                    obj.Name = Project.Name;
                                    obj.Project_Check_Status = true;
                                    Original_ProjectList.Add(obj);
                                }
                            }
                        }

                        Project_comboBox.ItemsSource = null;
                        Project_comboBox.Text = "0 Selected";
                        Project_comboBox.ItemsSource = objProjectList;
                        int count = objProjectList.Count(item => item.Project_Check_Status == true);
                        Project_comboBox.Text = count.ToString() + " Selected";

                        Project_Checkbox_Item_Clicked(Project_comboBox, null);
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

        private void EditCategory_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Line_Window line_Window = new Line_Window(main_Window);
                bool? dialogResult = line_Window.ShowDialog();
                switch (dialogResult)
                {
                    case true:
                        // User accepted dialog box
                        { LoadGrid(); }
                        break;
                    case false:
                        // User canceled dialog box
                        break;
                    default:
                        // Indeterminate
                        { LoadGrid(); }
                        break;
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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                    ID = Convert.ToInt32(dataRow[0].ToString());
                    string Name = dataRow["Name"].ToString();
                    int Cat_ID = Convert.ToInt32(dataRow["Category_ID"].ToString());
                    line.Delete(ID, Name, Cat_ID, "Budget_Window");

                    ID = -1;
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

        private void ProjectWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Return)
                {
                    Save_btn_Click(sender, e);
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

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    scrollViewer.LineLeft();
                }
                else
                {
                    scrollViewer.LineRight();
                }
            }
            else
            {
                if (e.Delta < 0)
                {
                    scrollViewer.LineDown();
                }
                else
                {
                    scrollViewer.LineUp();
                }
            }
            e.Handled = true;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Name_txtBox.Text = "";
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

        private void Project_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objProjectList != null && objProjectList.Count > 0)
                {
                    foreach (var Projec in objProjectList)
                    {
                        Projec.Project_Check_Status = false;
                    }
                }

                Project_comboBox.ItemsSource = null;
                Project_comboBox.Text = "0 Selected";
                Project_comboBox.ItemsSource = objProjectList;
                if (objProjectList != null)
                {
                    int count = objProjectList.Count(item => item.Project_Check_Status == true);
                    Project_comboBox.Text = count.ToString() + " Selected";
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

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                //Export_btn.CommandTarget = myDataGrid;
            }
        }

        private void myDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
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
                            ID = Convert.ToInt32(rowview[0].ToString());
                            Name_txtBox.Text = rowview[2].ToString();

                            if (rowview[3] != DBNull.Value)
                                Category_comboBox.SelectedValue = Convert.ToInt32(rowview[3].ToString());

                            List<string> AllProject_ID = new List<string>();

                            if (rowview["Projects_id"] != DBNull.Value && rowview["Projects_id"] != null)
                            {
                                AllProject_ID = rowview["Projects_id"].ToString().Split(',').ToList();

                                foreach (var Project in objProjectList)
                                {
                                    Project.Project_Check_Status = false;
                                }

                                Original_ProjectList = new List<Local_Project>();
                                for (int j = 0; j < AllProject_ID.Count; j++)
                                {
                                    if (objProjectList != null && objProjectList.Count > 0)
                                    {
                                        foreach (var Project in objProjectList)
                                        {
                                            if (Project.ID.ToString() == AllProject_ID[j])
                                            {
                                                Project.Project_Check_Status = true;

                                                Local_Project obj = new Local_Project();
                                                obj.ID = Project.ID;
                                                obj.Name = Project.Name;
                                                obj.Project_Check_Status = true;
                                                Original_ProjectList.Add(obj);
                                            }
                                        }
                                    }

                                    Project_comboBox.ItemsSource = null;
                                    Project_comboBox.Text = "0 Selected";
                                    Project_comboBox.ItemsSource = objProjectList;
                                    int count = objProjectList.Count(item => item.Project_Check_Status == true);
                                    Project_comboBox.Text = count.ToString() + " Selected";

                                    Project_Checkbox_Item_Clicked(Project_comboBox, null);
                                }
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
        private void Project_Checkbox_Item_Clicked(object sender, RoutedEventArgs e)
        {
            int count = objProjectList.Count(item => item.Project_Check_Status == true);
            Project_comboBox.Text = count.ToString() + " Selected";
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

        #region disable combobox text edit
        private void comboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        private void comboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space || e.Key == Key.Back || e.Key == Key.Delete;
        }
        #endregion

    }
}