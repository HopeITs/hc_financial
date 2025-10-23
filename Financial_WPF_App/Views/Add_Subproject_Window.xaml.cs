using Financial_WPF_App.Models;
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
    /// Interaction logic for Add_Subproject_Window.xaml
    /// </summary>
    public partial class Add_Subproject_Window : Window
    {
        public Add_Subproject_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Sudproject_ID = -1;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Add_Subproject_Window(int Sudproject_ID, DataTable subproject_dt, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Sudproject_ID = Sudproject_ID;
            this.subproject_dt = subproject_dt;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        int Sudproject_ID = -1;
        private City c;
        private Project p;
        private SubProject sub; private SubProject_Donor sd;
        private Donors d;
        private DataTable c_dt, p_dt, subproject_dt;
        bool UserMode;
        Main_Window_2 main_Window;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];


        #region listBoxes
        string current_item = string.Empty;
        int index = 0;
        List<string> Original_li_subProject;
        List<string> li_subProject;
        List<string> li;
        #endregion


        private bool OnLoad()
        {
            try
            {
                c = new City();
                p = new Project();
                sub = new SubProject();
                sd = new SubProject_Donor();
                d = new Donors();

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

        private void Load_Data()
        {
            Clear_Boxes();
            LoadCityComboBox();
            LoadProjectComboBox();
            LoadDonors_ListBox();

            if (Sudproject_ID > 0)
            {
                Load_Subproject();
            }
            else
            {
                UserMode = false;
                EndDate_CheckBox.IsChecked = true;
                endDate_datepicker.IsEnabled = false;
                endDate_datepicker.Text = "";
                UserMode = true;
            }

        }

        private void Load_Subproject()
        {
            UserMode = false;

            DataTable SB_dt = subproject_dt.Select("id = " + Sudproject_ID + "").CopyToDataTable();

            Name_txtBox.Text = SB_dt.Rows[0].Field<string>("Name").ToString();
            beginDate_datepicker.SelectedDate = Convert.ToDateTime(SB_dt.Rows[0].Field<DateTime>("BeginDate").ToString());
            if (SB_dt.Rows[0]["EndDate"] == DBNull.Value)
            {
                UserMode = false;
                EndDate_CheckBox.IsChecked = true;
                endDate_datepicker.IsEnabled = false;
                endDate_datepicker.Text = "";
                UserMode = true;
            }
            else
            {
                EndDate_CheckBox.IsChecked = false;
                endDate_datepicker.SelectedDate = Convert.ToDateTime(SB_dt.Rows[0].Field<DateTime>("EndDate").ToString());
            }

            if (SB_dt.Rows[0]["Project_ID"] != DBNull.Value)
            {
                Project_comboBox.SelectedValue = Convert.ToInt32(SB_dt.Rows[0].Field<int>("Project_ID"));
            }

            if (SB_dt.Rows[0]["City_ID"] != DBNull.Value)
            {
                City_comboBox.SelectedValue = Convert.ToInt32(SB_dt.Rows[0].Field<int>("City_ID"));
            }

            LoadDonors_ListBox();

            UserMode = true;

        }

        private void Clear_Boxes()
        {
            Project_comboBox.SelectedIndex = -1;
            City_comboBox.SelectedIndex = -1;
            Name_txtBox.Text = "";
            beginDate_datepicker.SelectedDate = DateTime.Now;
            endDate_datepicker.SelectedDate = null;
            EndDate_CheckBox.IsChecked = true;
            alldonors_listbox.ItemsSource = null;
            donors_listbox.ItemsSource = null;
        }

        private void LoadCityComboBox()
        {
            City_comboBox.ItemsSource = null;
            c_dt = c.Select("", "");
            if (c_dt != null && c_dt.Rows.Count > 0)
            {
                City_comboBox.ItemsSource = c_dt.DefaultView;
            }
        }

        private void LoadProjectComboBox()
        {
            Project_comboBox.ItemsSource = null;
            p_dt = p.Select("", "", "");
            if (p_dt != null && p_dt.Rows.Count > 0)
            {
                Project_comboBox.ItemsSource = p_dt.DefaultView;
            }
        }

        private void LoadDonors_ListBox()
        {
            string selectedItemText;

            li_subProject = new List<string>();
            Original_li_subProject = new List<string>();

            li_subProject = sd.Get_SubProject_Donors(Sudproject_ID);

            Original_li_subProject = li_subProject.ToList();

            li = new List<string>();
            li = d.Select_List();

            for (var i = 0; i < li_subProject.Count; i++)
            {
                selectedItemText = li_subProject.ElementAt(i);
                li.Remove(selectedItemText);
            }

            alldonors_listbox.ItemsSource = li;
            donors_listbox.ItemsSource = li_subProject;
        }

        private void BindSubProjectDonors_ListBox()
        {
            alldonors_listbox.ItemsSource = null;
            alldonors_listbox.ItemsSource = li;

            donors_listbox.ItemsSource = null;
            donors_listbox.ItemsSource = li_subProject;
        }

        private void AddDonor_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (alldonors_listbox.SelectedIndex == -1)
                    return;

                current_item = alldonors_listbox.SelectedValue.ToString();
                index = alldonors_listbox.SelectedIndex;

                li_subProject.Add(current_item);
                if (li != null)
                {
                    li.RemoveAt(index);
                }
                BindSubProjectDonors_ListBox();
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

        private void RemoveDonor_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (donors_listbox.SelectedIndex == -1)
                    return;

                current_item = donors_listbox.SelectedValue.ToString();
                index = donors_listbox.SelectedIndex;

                li.Add(current_item);
                li_subProject.Remove(current_item);

                BindSubProjectDonors_ListBox();

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

        private void EndDate_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                endDate_datepicker.IsEnabled = false;
                if (UserMode)
                {
                    endDate_datepicker.Text = "";
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
        }

        private void EndDate_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                endDate_datepicker.IsEnabled = true;
                if (UserMode)
                {
                    endDate_datepicker.SelectedDate = DateTime.Now;
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

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Project_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the program.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }
                if (City_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the city.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }
                if (beginDate_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please specify the start date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }
                if (donors_listbox.Items.Count < 1)
                {
                    MessageBox.Show("Please select one donor at least.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }

                string Name = Login.ReplaceQuotation(Name_txtBox.Text);
                int City_ID = Convert.ToInt32(City_comboBox.SelectedValue);
                int Project_ID = Convert.ToInt32(Project_comboBox.SelectedValue);
                //
                string beginDate = ((DateTime)beginDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                string endDate = "";
                if (EndDate_CheckBox.IsChecked == true && endDate_datepicker.IsEnabled == false)
                    endDate = "NULL";
                else endDate = ((DateTime)endDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                if (Sudproject_ID > 0)//Edit
                {
                    List<string> Add_donors = new List<string>();
                    List<string> Delete_donors = new List<string>();

                    bool Found;

                    for (int i = 0; i < donors_listbox.Items.Count; i++)
                    {
                        Found = false;
                        for (int j = 0; j < Original_li_subProject.Count; j++)
                        {
                            if (donors_listbox.Items[i].ToString() == Original_li_subProject[j])
                            {
                                Found = true;
                                break;
                            }
                        }
                        if (Found == false)
                            Add_donors.Add(donors_listbox.Items[i].ToString());
                    }

                    for (int j = 0; j < Original_li_subProject.Count; j++)
                    {
                        Found = false;
                        for (int i = 0; i < donors_listbox.Items.Count; i++)
                        {
                            if (Original_li_subProject[j] == donors_listbox.Items[i].ToString())
                            {
                                Found = true;
                                break;
                            }
                        }
                        if (Found == false)
                            Delete_donors.Add(Original_li_subProject[j]);
                    }

                    bool WithPaymentAccount;
                    if (Project_comboBox.Text == "Micro Projects")
                        WithPaymentAccount = true;
                    else
                        WithPaymentAccount = false;
                    sub.Update(Sudproject_ID, Name, beginDate, endDate, Project_ID, City_ID, Add_donors, Delete_donors, WithPaymentAccount, "SubProjectWindow");
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    this.Close();
                }
                else//Add new
                {
                    // Insert new Subproject, Payments Account with till syr, pay till, and donors
                    List<string> All_donors = new List<string>();
                    for (int i = 0; i < donors_listbox.Items.Count; i++)
                    {
                        All_donors.Add(donors_listbox.Items[i].ToString());
                    }
                    bool WithPaymentAccount;
                    if (Project_comboBox.Text == "Micro Projects")
                        WithPaymentAccount = true;
                    else
                        WithPaymentAccount = false;

                    sub.Insert(Name, beginDate, endDate, Project_ID, City_ID, All_donors, WithPaymentAccount, "SubProjectWindow");
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    Clear_Boxes();
                    LoadDonors_ListBox();
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
                    LoadDonors_ListBox();
                }

                else
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
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EndDate_CheckBox.IsChecked = !EndDate_CheckBox.IsChecked;
        }
    }
}