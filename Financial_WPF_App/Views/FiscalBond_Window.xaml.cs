using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using System.Windows.Media;

namespace Financial_WPF_App.Views
{

    public partial class FiscalBond_Window : Window
    {
        public FiscalBond_Window(Main_Window_2 main_Window)
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
        private int FiscalBond_ID;
        private FiscalBond fb;
        private SubProject sub;
        private Account a;
        private Till t;
        private DataTable dt, a_dt, sub_dt, t_dt;
        int Till_ID = -1;
        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();
        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];

        private bool OnLoad()
        {
            try
            {
                sub = new SubProject();
                a = new Account();
                t = new Till();
                fb = new FiscalBond();
                dt = sub_dt = a_dt = t_dt = new DataTable();
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

            FiscalBond_ID = -1;
            Till_ID = -1;

            Date_datepicker.SelectedDate = DateTime.Today;
            Load_Comboboxes_Data();

            LoadGrid();
        }


        private void LoadGrid()
        {

            if (Login.BuildConnection())
            {
                dt = fb.Select("", "", "", "", true);
                if (dt != null && dt.Rows.Count > 0)
                {
                    myDataGrid.ItemsSource = dt.DefaultView;
                    if (HOffset != 0)
                        this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }
                Bind_Show_Hide_Menu();
            }
        }

        private void Fill_FiscalBond(DataRowView dataRow)
        {

            FiscalBond_ID = Convert.ToInt32(dataRow["ID"].ToString());
            Till_ID = Convert.ToInt32(dataRow["Till_ID"].ToString());

            Name_txtBox.Text = dataRow["Name"].ToString();
            Date_datepicker.SelectedDate = Convert.ToDateTime(dataRow["Date"].ToString());
            Notes_txtBox.Text = dataRow["Notes"].ToString();
            if (dataRow["SubProject_ID"] != DBNull.Value)
            {
                SubProject_comboBox.SelectedValue = Convert.ToInt32(dataRow["SubProject_ID"]);
                SubProject_comboBox_SelectionChanged(SubProject_comboBox, null);
            }

            if (dataRow["Account_ID"] != DBNull.Value)
            {
                Account_comboBox.SelectedValue = Convert.ToInt32(dataRow["Account_ID"]);
                Account_comboBox_SelectionChanged(Account_comboBox, null);

            }

            if (dataRow["Till_ID"] != DBNull.Value)
                Till_ComboBox.SelectedValue = Convert.ToInt32(dataRow["Till_ID"]);



        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                Fill_FiscalBond(dataRow);
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
                    FiscalBond_ID = Convert.ToInt32(dataRow[0].ToString());
                    string FiscalBond_Name = dataRow[1].ToString();
                    int Account_ID = Convert.ToInt32(dataRow["Account_ID"].ToString());


                    if (main_Window.main_tabControl.Items != null && main_Window.main_tabControl.Items.Count != 0)
                    {
                        foreach (TabItem tab in main_Window.main_tabControl.Items)
                        {
                            if (tab.Header.ToString() == FiscalBond_Name)
                            {
                                MessageBox.Show("Please close the bond before deleting it.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                return;
                            }
                        }
                    }

                    fb.Delete(FiscalBond_ID, FiscalBond_Name, "FiscalBond_Window");
                    FiscalBond_ID = -1;
                    Till_ID = -1;
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

        private void FiscalBond_KeyDown(object sender, KeyEventArgs e)
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

        private void FiscalBond_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

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

                if (SubProject_comboBox.SelectedIndex == -1 || Account_comboBox.SelectedIndex == -1 || Till_ComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the center, account and till", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
           
                }

                if (Login.BuildConnection())
                {
                    string Name = Name_txtBox.Text;
                    string Date = ((DateTime)Date_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                    string Notes = Notes_txtBox.Text;
                    int SubProject_ID = Convert.ToInt32(SubProject_comboBox.SelectedValue);
                    int Account_ID = Convert.ToInt32(Account_comboBox.SelectedValue);
                    string Account = Account_comboBox.Text.ToString();
                    int Till_ID = Convert.ToInt32(Till_ComboBox.SelectedValue);

                    if (FiscalBond_ID != -1)//Edit
                    {
                        fb.Update(FiscalBond_ID, Name, Date, Notes, Account_ID, Till_ID, "FiscalBond_Window");
                    }
                    else//Add new
                    {
                        fb.Insert(Name, Date, Notes, Account_ID, Till_ID, "FiscalBond_Window");
                    }

                    Clear_Boxes();
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

        private void Clear_Boxes()
        {
            Name_txtBox.Text = Notes_txtBox.Text = "";
            Account_comboBox.SelectedIndex = SubProject_comboBox.SelectedIndex = Till_ComboBox.SelectedIndex = -1;
            Date_datepicker.SelectedDate = DateTime.Now;
            FiscalBond_ID = -1; Till_ID = -1;
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
                            Fill_FiscalBond(rowview);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

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

        private void Export_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Load_Comboboxes_Data()
        {
            SubProject_comboBox.ItemsSource = null;
            sub_dt = sub.Select("", "", "", "");
            if (sub_dt != null && sub_dt.Rows.Count > 0)
            {
                SubProject_comboBox.ItemsSource = sub_dt.DefaultView;
            }


            Account_comboBox.ItemsSource = null;
            a_dt = a.Select("", "", "Project", false, "");
 
        }

        private void SubProject_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                Account_comboBox.ItemsSource = null;
                if (SubProject_comboBox.SelectedValue != null && SubProject_comboBox.SelectedIndex != -1)
                {
                    int SubProject_id = Convert.ToInt32(SubProject_comboBox.SelectedValue);

                    System.Data.DataRow[] rows = a_dt.Select("SubProject_ID=" + SubProject_id);
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        if (selected_dt!= null && selected_dt.Rows.Count > 0)
                            Account_comboBox.ItemsSource = selected_dt.DefaultView;
                    }

                }
                Till_ComboBox.SelectedIndex = -1;
                Till_ComboBox.ItemsSource = null;


            }
            catch (Exception ex)
            { if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) 
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host")) 
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else { MessageBox.Show(ex.Message, "Error"); } 
            }

        }

        private void Account_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (Account_comboBox.SelectedIndex != -1 && Account_comboBox.SelectedValue != null)
                {
                    Till_ComboBox.ItemsSource = null;
                    string Account_ID = Account_comboBox.SelectedValue.ToString();
                    LoadTillComboBox(Account_ID);
                }
                else
                {
                    Till_ComboBox.ItemsSource = null;
                }
            }
            catch (Exception ex)
            { if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) 
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host")) 
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else { MessageBox.Show(ex.Message, "Error"); } 
            }

        }

        private void LoadTillComboBox(string account_ID)
        {
            Till_ComboBox.ItemsSource = null;

            t_dt = t.Select_FiscalBond_Missing_TillTypes(account_ID, Till_ID);
            if (t_dt != null && t_dt.Rows.Count > 0)
                Till_ComboBox.ItemsSource = t_dt.DefaultView;
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

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void SubProject_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SubProject_comboBox.SelectedIndex = -1;

                Account_comboBox.SelectedIndex = -1;
                Account_comboBox.ItemsSource = null;

                Till_ComboBox.SelectedIndex = -1;
                Till_ComboBox.ItemsSource = null;
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

        private void Account_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Account_comboBox.SelectedIndex = -1;

                Till_ComboBox.SelectedIndex = -1;
                Till_ComboBox.ItemsSource = null;
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

        private void Till_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Till_ComboBox.SelectedIndex = -1;
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
