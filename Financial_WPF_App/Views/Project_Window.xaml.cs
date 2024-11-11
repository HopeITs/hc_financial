using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Windows;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Project_Window.xaml
    /// </summary>
    public partial class Project_Window : Window
    {
        public Project_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            Cat_ID = -1;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        Main_Window_2 main_Window;
        private int ID = -1, Cat_ID;
        private Project Project;
        private DataTable dt, cat_dt;

        private bool OnLoad()
        {
            try
            {
                Project = new Project();

                Load_Data();
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

        private void Load_Data()
        {
            Load_Comboboxes_Data();
            LoadGrid();
        }

        private void Load_Comboboxes_Data()
        {
            if (Login.BuildConnection())
            {
                cat_dt = Project.Select_Project_Catgory();
                if (cat_dt != null && cat_dt.Rows.Count > 0)
                {
                    Category_comboBox.ItemsSource = cat_dt.DefaultView;
                }
                else
                {
                    Category_comboBox.ItemsSource = null;
                }
            }
        }

        private void LoadGrid()
        {

            if (Login.BuildConnection())
            {
                dt = Project.Select("", "", "");
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

        private void Category_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Category_comboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else
                { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }

                if (Category_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the category.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;

                }

                if (Login.BuildConnection())
                {
                    string Name = Login.ReplaceQuotation(Name_txtBox.Text);
                    int Category_ID = Convert.ToInt32(Category_comboBox.SelectedValue);

                    if (ID != -1)//Edit
                    {
                        Project.Update(ID, Name, Category_ID, "Project_Window");
                    }
                    else//Add new
                    {
                        Project.Insert(Name, Category_ID, "Project_Window");
                    }

                    Name_txtBox.Text = "";
                    Category_comboBox.SelectedIndex = -1;
                    ID = -1;

                    LoadGrid();
                    Category_comboBox.IsEnabled = true;
                    Category_clear_btn.IsEnabled = true;
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {

                DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                ID = Convert.ToInt32(dataRow["ID"].ToString());
                Name_txtBox.Text = dataRow["Name"].ToString();

                if (dataRow["Category_ID"] != DBNull.Value)
                    Category_comboBox.SelectedValue = Convert.ToInt32(dataRow["Category_ID"].ToString());

                Category_comboBox.IsEnabled = false;
                Category_clear_btn.IsEnabled = false;


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
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                    ID = Convert.ToInt32(dataRow["ID"].ToString());
                    string Name = dataRow["Name"].ToString();
                    int Cat_ID = Convert.ToInt32(dataRow["Category_ID"].ToString());
                    Project.Delete(ID, Name, Cat_ID, "Project_Window");

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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Name_txtBox.Text = "";
                ID = -1;


                Load_Data();
                Category_comboBox.IsEnabled = true;
                Category_clear_btn.IsEnabled = true;
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
