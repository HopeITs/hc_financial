using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
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
    /// Interaction logic for Approval_Sub_Pay_Window.xaml
    /// </summary>
    public partial class Approval_Sub_Pay_Window : Window
    {
        public Approval_Sub_Pay_Window(string SubProject_ID, Approval_Window approval_Window, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.approval_Window = approval_Window;
            this.SubProject_ID = SubProject_ID;

            //this.myDataGrid.CurrentCellValueChanged += datagrid_CurrentCellValueChanged;
            Onload();

        }

        Main_Window_2 main_Window;
        Approval_Window approval_Window;
        string SubProject_ID;

        Models.Action a; Payment p;
        private DataTable Payments_dt, Deleted_Payments_dt;
        bool Deleted_Actions;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        bool GridLoaded = false;
        double HOffset = 0;
        private DataRowView _lastCurrentItem;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["HopeRed"];


        private bool Onload()
        {
            try
            {
                a = new Models.Action();
                p = new Payment();
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

        public void Load_Data()
        {
            LoadGrid();
        }

        private void LoadGrid()
        {
            if (Login.BuildConnection())
            {
                Payments_dt = p.Select_All_Not_Approved_Payments(SubProject_ID, true, -1, "", "", -1);
                Deleted_Payments_dt = p.Select_All_Deleted_Payments(SubProject_ID, true);

                System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
                System.Data.DataColumn newColumn1 = new System.Data.DataColumn("IsSelected", typeof(Int32));


                Payments_dt.Columns.Add(newColumn);
                Deleted_Payments_dt.Columns.Add(newColumn1);

                Payments_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));
                Deleted_Payments_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));


                Set_myDataGrid_ItemSource();

            }

            GridLoaded = true;
            Bind_Show_Hide_Menu();
        }

        private void Set_myDataGrid_ItemSource()
        {
            try
            {
                if (DeletedActions_RadioButton.IsChecked == false)
                {
                    Deleted_Actions = false;
                    Restore_btn.Visibility = Visibility.Collapsed;

                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Deleted By" || C.HeaderText.ToString() == "Date Deleted")
                            C.IsHidden = true;

                    if (Payments_dt != null && Payments_dt.Rows.Count > 0)
                    {
                        All_textBlock.Text = Payments_dt.Rows.Count.ToString();
                        myDataGrid.ItemsSource = Payments_dt.DefaultView;
                    }
                    else
                    {
                        myDataGrid.ItemsSource = null;
                    }
                }
                else
                {
                    Deleted_Actions = true;
                    Restore_btn.Visibility = Visibility.Visible;

                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Deleted By" || C.HeaderText.ToString() == "Date Deleted")
                            C.IsHidden = false;

                    if (Deleted_Payments_dt != null && Deleted_Payments_dt.Rows.Count > 0)
                    {
                        All_textBlock.Text = Deleted_Payments_dt.Rows.Count.ToString();
                        myDataGrid.ItemsSource = Deleted_Payments_dt.DefaultView;
                    }
                    else
                    {
                        myDataGrid.ItemsSource = null;
                    }
                }

                if (myDataGrid.ItemsSource != null)
                {
                    if (HiddenColumns.Count != 0)
                    {
                        foreach (string CName in HiddenColumns)
                        {
                            for (int i = 0; i < myDataGrid.Columns.Count; i++)
                            {
                                if (myDataGrid.Columns[i].HeaderText.ToString() == CName)
                                {
                                    myDataGrid.Columns[i].IsHidden = true; break;
                                }
                            }
                        }
                    }
                    if (HOffset != 0)
                        this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
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

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
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


        void ScrollOwner_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange > 0 || e.HorizontalChange < 0)
            {
                HOffset = e.HorizontalOffset;
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

                if (Payments_dt != null)
                {
                    SearchDataGrid searchHelper = new SearchDataGrid(Payments_dt);

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

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            Restore_btn.IsEnabled = false;
            try
            {
                Login.ActionApproval_sc.Parameters.Clear();
                Login.ActionApproval_List.Clear();

                if (!Deleted_Actions)
                {
                    DataView newView = new DataView(Payments_dt);
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

                        var Action_ID = newView[k]["ID"];


                        Login.ActionApproval_sc.Parameters.AddWithValue("@Action_ID" + k, Action_ID);


                        string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                        Login.ActionApproval_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

                        Login.ActionApproval_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);
                        Login.ActionApproval_sc.Parameters.AddWithValue("@Form_Name" + k, "Approval_Window");

                        temp_query += "UPDATE `action_not_approved` SET `IsApproved`= 1,`Approved_at`= @Todaydate" + k + ",`Approved_by`= @Uesr_ID" + k + " WHERE ID = @Action_ID" + k + "; ";

                        temp_query += "UPDATE `action_not_approved` SET `IsApproved`= 1,`Approved_at`= @Todaydate" + k + ",`Approved_by`= @Uesr_ID" + k + " WHERE ObserveAcion_ID = @Action_ID" + k + "; ";

                        temp_query += "INSERT INTO action SELECT * FROM action_not_approved WHERE action_not_approved.id = @Action_ID" + k + " OR action_not_approved.ObserveAcion_ID = @Action_ID" + k + ";";

                        temp_query += "DELETE FROM action_not_approved WHERE action_not_approved.ObserveAcion_ID = @Action_ID" + k + ";";

                        temp_query += "DELETE FROM action_not_approved WHERE action_not_approved.id  = @Action_ID" + k + ";";

                        temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'APPROVE', 'action', @Action_ID" + k + ", 'Approved = true', '', '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                        Login.ActionApproval_List.Add(temp_query);
                        temp_query = "";
                    }

                    a.Update_Multi_Approval();

                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    LoadGrid();
                }
                else
                {
                    DataView newView = new DataView(Deleted_Payments_dt);
                    newView.RowFilter = "IsSelected = 1";

                    if (newView.Count == 0)
                    {
                        MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                        return;
                    }

                    var dialogResult = MessageBox.Show("Are you sure you want to delete this data permanently?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        for (int k = 0; k < newView.Count; k++)
                        {

                            string temp_query = "";
                            string log_query = "";

                            var Action_ID = newView[k]["ID"];
                            var Beneficiary_ID = newView[k]["beneficiary_ID"];

                            Login.ActionApproval_sc.Parameters.AddWithValue("@Action_ID" + k, Action_ID);
                            Login.ActionApproval_sc.Parameters.AddWithValue("@Beneficiary_ID" + k, Beneficiary_ID);

                            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                            Login.ActionApproval_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

                            Login.ActionApproval_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);
                            Login.ActionApproval_sc.Parameters.AddWithValue("@Form_Name" + k, "Approval_Window");

                            temp_query += "Delete from action_deleted where ObserveAcion_ID = @Action_ID" + k + ";";
                            temp_query += "Delete from action_deleted where ID = @Action_ID" + k + ";";
                            temp_query += "Delete from action_attachments where action_id = @Action_ID" + k + ";";

                            temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'DELETE', 'action', @Action_ID" + k + ", 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID" + k + "), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                            Login.ActionApproval_List.Add(temp_query);
                            temp_query = "";
                        }

                        a.Update_Multi_Approval();

                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                        LoadGrid();
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
            finally
            {
                Save_btn.IsEnabled = true;
                Restore_btn.IsEnabled = true;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                    var allChecked = Payments_dt.AsEnumerable().All(row => row.Field<int>("IsSelected") == 1);
                    var anyChecked = Payments_dt.AsEnumerable().Any(row => row.Field<int>("IsSelected") == 1);
                    headerCheckbox.IsChecked = allChecked ? true : (anyChecked ? null : (bool?)false);
                }
            }
        }

        private void Restore_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            Restore_btn.IsEnabled = false;
            try
            {
                Login.ActionApproval_sc.Parameters.Clear();
                Login.ActionApproval_List.Clear();

                if (Deleted_Actions)
                {
                    DataView newView = new DataView(Deleted_Payments_dt);
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

                        var Action_ID = newView[k]["ID"];

                        var Approved_Action = newView[k]["IsApproved"];

                        Login.ActionApproval_sc.Parameters.AddWithValue("@Action_ID" + k, Action_ID);


                        string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                        Login.ActionApproval_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

                        Login.ActionApproval_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);
                        Login.ActionApproval_sc.Parameters.AddWithValue("@Form_Name" + k, "Approval_Window");


                        if (Convert.ToInt32(Approved_Action.ToString()) == 1)
                        {
                            temp_query += "INSERT INTO action SELECT * FROM action_deleted WHERE action_deleted.id = @Action_ID" + k + " OR action_deleted.ObserveAcion_ID = @Action_ID" + k + ";";

                            temp_query += "Update action set deleted_at = null, deleted_by = null WHERE action.id = @Action_ID" + k + " OR action.ObserveAcion_ID = @Action_ID" + k + ";";

                            temp_query += "DELETE FROM action_deleted WHERE action_deleted.ObserveAcion_ID = @Action_ID" + k + ";";

                            temp_query += "DELETE FROM action_deleted WHERE action_deleted.id  = @Action_ID" + k + ";";
                        }
                        else
                        {
                            temp_query += "INSERT INTO action_not_approved SELECT * FROM action_deleted WHERE action_deleted.id = @Action_ID" + k + " OR action_deleted.ObserveAcion_ID = @Action_ID" + k + ";";

                            temp_query += "Update action_not_approved set deleted_at = null, deleted_by = null WHERE action_not_approved.id = @Action_ID" + k + " OR action_not_approved.ObserveAcion_ID = @Action_ID" + k + ";";

                            temp_query += "DELETE FROM action_deleted WHERE action_deleted.ObserveAcion_ID = @Action_ID" + k + ";";

                            temp_query += "DELETE FROM action_deleted WHERE action_deleted.id  = @Action_ID" + k + ";";
                        }


                        Login.ActionApproval_List.Add(temp_query);
                        temp_query = "";
                    }

                    a.Update_Multi_Approval();

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
                Restore_btn.IsEnabled = true;

            }
        }

        private void DeletedActions_RadioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RadioButton radioButton = sender as RadioButton;

                // If the RadioButton was checked before the click, this will uncheck it
                if (bool.Parse((string)radioButton.Tag))
                {
                    radioButton.IsChecked = false;
                }

                // Store the current state of the RadioButton in its Tag property
                radioButton.Tag = radioButton.IsChecked.ToString();

                Set_myDataGrid_ItemSource();

                if (myDataGrid.View != null)
                {
                    Export_btn.CommandTarget = myDataGrid;
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
    }
}
