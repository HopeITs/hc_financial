using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid.Helpers;
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
    public partial class SubProjectWindow : Window
    {
        public SubProjectWindow(Main_Window_2 main_Window)
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

        private int ID;
        private City c;
        private Project p;
        private SubProject sub; private SubProject_Donor sd;
        private Donors d;
        private Account a; private Till t;
        private DataTable SUB_dt, c_dt, p_dt;
        bool UserMode;
        Main_Window_2 main_Window;
        double HOffset = 0;

        private bool OnLoad()
        {
            try
            {
                c = new City();
                p = new Project();
                sub = new SubProject();
                sd = new SubProject_Donor();
                d = new Donors();
                a = new Account();
                t = new Till();
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
            System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];

            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UpdateTextBlocksForeground(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void Load_Data()
        {
            ID = -1;
            Clear_Boxes();
            LoadCityComboBox();
            LoadProjectComboBox();
            LoadDonors_ListBox();
            LoadGrid();

            UserMode = false;
            EndDate_CheckBox.IsChecked = true;
            endDate_datepicker.IsEnabled = false;
            endDate_datepicker.Text = "";
            UserMode = true;
        }

        private void LoadGrid()
        {

            myDataGrid.ItemsSource = null;
            if (Login.BuildConnection())
            {
                SUB_dt = sub.Select("", "", "", "");
                if (SUB_dt != null && SUB_dt.Rows.Count > 0)
                {
                    myDataGrid.ItemsSource = SUB_dt.DefaultView;
                    if (HOffset != 0)
                        this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }
            }

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

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Project_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the project.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
   
                }
                if (City_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the city.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
               
                }
                if (beginDate_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please specify the start date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                
                }
                if (donors_listbox.Items.Count < 1)
                {
                    MessageBox.Show("Please select one donor at least.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
    
                }


                if (Login.BuildConnection())
                {
                    string Name = Login.ReplaceQuotation(Name_txtBox.Text);
                    int City_ID = Convert.ToInt32(City_comboBox.SelectedValue);
                    int Project_ID = Convert.ToInt32(Project_comboBox.SelectedValue);
                    //
                    string beginDate = ((DateTime)beginDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                    string endDate = "";
                    if (EndDate_CheckBox.IsChecked == true && endDate_datepicker.IsEnabled == false)
                        endDate = "NULL";
                    else endDate = ((DateTime)endDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                    if (ID != -1)//Edit
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
                        sub.Update(ID, Name, beginDate, endDate, Project_ID, City_ID, Add_donors, Delete_donors, WithPaymentAccount, "SubProjectWindow");
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
                    }

                    Clear_Boxes();
                    LoadDonors_ListBox();
                    LoadGrid();
                    main_Window.Fill_TreeView(true);
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
                    MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    LoadDonors_ListBox();
                }

                else
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }


        }

        private void SubProjectWindow_KeyDown(object sender, KeyEventArgs e)
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        #region listBoxes
        string current_item = string.Empty;
        int index = 0;
        List<string> Original_li_subProject;
        List<string> li_subProject;
        List<string> li;

        private void LoadDonors_ListBox()
        {
            string selectedItemText;

            li_subProject = new List<string>();
            Original_li_subProject = new List<string>();

            li_subProject = sd.Get_SubProject_Donors(ID);

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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }
        #endregion

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                ID = Convert.ToInt32(dataRow[0].ToString());
                Name_txtBox.Text = dataRow[1].ToString();
                beginDate_datepicker.SelectedDate = Convert.ToDateTime(dataRow[2].ToString());
                if (dataRow[3] == DBNull.Value || dataRow[3] == null)
                    EndDate_CheckBox.IsChecked = true;
                else
                {
                    EndDate_CheckBox.IsChecked = false;
                    endDate_datepicker.SelectedDate = Convert.ToDateTime(dataRow[3].ToString());
                }
                if (dataRow[4] != DBNull.Value)
                    Project_comboBox.SelectedValue = Convert.ToInt32(dataRow[4].ToString());


                if (dataRow[5] != DBNull.Value)
                    City_comboBox.SelectedValue = Convert.ToInt32(dataRow[5].ToString());

                LoadDonors_ListBox();
                //donors_grid.Visibility = Visibility.Visible;
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
                    ID = Convert.ToInt32(dataRow[0].ToString());
                    string Sub_Name = dataRow[1].ToString();


                    if (main_Window.main_tabControl.Items != null && main_Window.main_tabControl.Items.Count != 0)
                    {
                        DataTable accounts_dt = a.Select(ID.ToString(), "", "", false, "");

                        if (accounts_dt != null && accounts_dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < accounts_dt.Rows.Count; i++)
                            {
                                foreach (TabItem tab in main_Window.main_tabControl.Items)
                                {
                                    if (tab.Header.ToString() == accounts_dt.Rows[i]["Name"].ToString())
                                    {
                                        MessageBox.Show("Please close all this center accounts before deleting it.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    sub.Delete(ID, Sub_Name, "SubProjectWindow");

                    ID = -1;
                    LoadGrid();
                    main_Window.Fill_TreeView(true);
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

        private void myDataGrid_FilterChanged(object sender, Syncfusion.UI.Xaml.Grid.GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clear_Boxes();
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

        private void Clear_Boxes()
        {
            ID = -1;
            Project_comboBox.SelectedIndex = -1;
            City_comboBox.SelectedIndex = -1;
            Name_txtBox.Text = "";
            beginDate_datepicker.SelectedDate = DateTime.Now;
            endDate_datepicker.SelectedDate = null;
            EndDate_CheckBox.IsChecked = true;
            alldonors_listbox.ItemsSource = null;
            donors_listbox.ItemsSource = null;
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;

                All_textBlock.Text = myDataGrid.View.Records.Count().ToString();
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
    }
}
