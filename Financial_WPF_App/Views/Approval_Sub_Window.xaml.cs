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

    public partial class Approval_Sub_Window : Window
    {
        public Approval_Sub_Window(string Tills_ID, string Account_Type, string Category, string TillType_Name, Approval_Window approval_Window, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.approval_Window = approval_Window;
            this.Tills_ID = Tills_ID;
            this.Account_Type = Account_Type;
            this.TillType_Name = TillType_Name;
            this.Category = Category;
            //this.myDataGrid.CurrentCellValueChanged += datagrid_CurrentCellValueChanged;
            Onload();

        }

        Main_Window_2 main_Window;
        Approval_Window approval_Window;
        string Tills_ID;
        string Account_Type, TillType_Name, Category;


        Models.Action a;
        private DataTable Actions_dt, Deleted_Actions_dt;
        bool Deleted_Actions;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        bool GridLoaded = false;
        double HOffset = 0;
        private DataRowView _lastCurrentItem;

        private bool Onload()
        {
            try
            {
                a = new Models.Action();

                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                    myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
                }

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

        public void Load_Data()
        {
            Show_Hide();

            LoadGrid();
        }

        private void Show_Hide()
        {
            if (Account_Type == "Project")
            {
                if (TillType_Name.ToLower() == "syr")
                {
                    if (Category == "Micro Projects")
                    {
                        foreach (var C in myDataGrid.Columns)
                            if (C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget"
                                || C.HeaderText.ToString() == "Donor" || C.HeaderText.ToString() == "Volunteer")
                            {
                                C.IsHidden = true;
                                Originally_HiddenColumns.Add(C.HeaderText.ToString());
                            }
                    }
                    else if (Category == "Budget")
                    {

                        foreach (var C in myDataGrid.Columns)
                            if (C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name")
                            {
                                C.IsHidden = true;
                                Originally_HiddenColumns.Add(C.HeaderText.ToString());
                            }
                    }
                }
                else
                {
                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" ||
                            C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                            C.HeaderText.ToString() == "Document type" || C.HeaderText.ToString() == "Document number"
                            || C.HeaderText.ToString() == "Volunteer")
                        {
                            C.IsHidden = true;
                            Originally_HiddenColumns.Add(C.HeaderText.ToString());
                        }
                }
            }


            if (Account_Type == "Exchange")
            {
                if (TillType_Name.ToLower() != "syr")
                {

                }
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" ||
                        C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                        C.HeaderText.ToString() == "Document type" || C.HeaderText.ToString() == "Document number" ||
                        C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount" || C.HeaderText.ToString() == "Value"
                        || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer" || C.HeaderText.ToString() == "Subproject")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }
            }


            if (Account_Type == "Personal")
            {
                if (TillType_Name.ToLower() == "syr")
                {
                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" || C.HeaderText.ToString() == "Exchange" ||
                            C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                            C.HeaderText.ToString() == "Donor" || C.HeaderText.ToString() == "Rate" || C.HeaderText.ToString() == "Exchanged" ||
                            C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount" || C.HeaderText.ToString() == "Value" ||
                            C.HeaderText.ToString() == "Corresponding till" || C.HeaderText.ToString() == "Corresponding account" || C.HeaderText.ToString() == "Corresponding cash till"
                            || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer" || C.HeaderText.ToString() == "Subproject")
                        {
                            C.IsHidden = true;
                            Originally_HiddenColumns.Add(C.HeaderText.ToString());
                        }

                }
                else
                {
                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" || C.HeaderText.ToString() == "Exchange" ||
                            C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                            C.HeaderText.ToString() == "Donor" || C.HeaderText.ToString() == "Rate" || C.HeaderText.ToString() == "Exchanged" || C.HeaderText.ToString() == "Cash till" ||
                            C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount" || C.HeaderText.ToString() == "Value" ||
                            C.HeaderText.ToString() == "Corresponding till" || C.HeaderText.ToString() == "Corresponding account" || C.HeaderText.ToString() == "Corresponding cash till"
                            || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer" || C.HeaderText.ToString() == "Subproject")
                        {
                            C.IsHidden = true;
                            Originally_HiddenColumns.Add(C.HeaderText.ToString());
                        }
                }
            }
        }

        private void LoadGrid()
        {
            if (Login.BuildConnection())
            {
                Actions_dt = a.Select_action_not_approved("", Tills_ID.ToString(), "", true, true);
                Deleted_Actions_dt = a.Select_Deleted_action(Tills_ID.ToString(), true);

                System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
                System.Data.DataColumn newColumn1 = new System.Data.DataColumn("IsSelected", typeof(Int32));


                Actions_dt.Columns.Add(newColumn);
                Deleted_Actions_dt.Columns.Add(newColumn1);

                Actions_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));
                Deleted_Actions_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));

                Set_myDataGrid_ItemSource();
            }

            GridLoaded = true;
            Bind_Show_Hide_Menu();
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
                if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") && !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") && !CHeader.Contains("#") && !Originally_HiddenColumns.Contains(CHeader))
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

        void ScrollOwner_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange > 0 || e.HorizontalChange < 0)
            {
                HOffset = e.HorizontalOffset;
            }
        }

        #region search
        private void Search_DataGrid()
        {
            DataTable selected_dt = new DataTable();
            selected_dt = Actions_dt.Copy();

            if (Search_txtBox.Text != "")
            {
                if (selected_dt != null)
                {
                    DataTable new_dt = selected_dt.Copy();
                    new_dt.Rows.Clear();

                    foreach (System.Data.DataRow r in selected_dt.Rows)
                    {
                        for (var i = 0; i < selected_dt.Columns.Count; i++)
                        {
                            if (ExactSearch_checkBox.IsChecked == true)
                            {
                                if (r.ItemArray.ElementAt(i).ToString().ToLower() == Search_txtBox.Text.ToLower())
                                {
                                    new_dt.Rows.Add(r.ItemArray);
                                    break;
                                }
                            }
                            else
                            {
                                if (r.ItemArray.ElementAt(i).ToString().ToLower().Contains(Search_txtBox.Text.ToLower()))
                                {
                                    new_dt.Rows.Add(r.ItemArray);
                                    break;
                                }
                            }
                        }
                    }
                    selected_dt = new_dt.Copy();
                }
            }
            myDataGrid.ItemsSource = null;

            if (selected_dt != null)
            {
                myDataGrid.ItemsSource = selected_dt.DefaultView;
            }
        }

        private void Search_txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //this.myDataGrid.SearchHelper.Search(Search_txtBox.Text);
                //this.myDataGrid.SearchHelper.AllowFiltering = true;
                Search_DataGrid();
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

        private void ExactSearch_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Search_DataGrid();
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

        private void ExactSearch_checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                Search_DataGrid();
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
                    DataView newView = new DataView(Actions_dt);
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

                        if (Account_Type == "Exchange")
                        {
                            var Account_ID = newView[k]["Account_ID"];
                            var ActionDate = Convert.ToDateTime(newView[k]["ActionDate"].ToString()).ToString("yyyy/MM/dd");
                            var Rate = newView[k]["Rate"];
                            var Till_Type = newView[k]["Tilltypes_Name"];
                            var ObserveTill_Type = newView[k]["ObserveTill"];


                            Login.ActionApproval_sc.Parameters.AddWithValue("@Account_ID" + k, Account_ID);
                            Login.ActionApproval_sc.Parameters.AddWithValue("@ActionDate" + k, ActionDate);
                            Login.ActionApproval_sc.Parameters.AddWithValue("@Rate" + k, Rate);
                            Login.ActionApproval_sc.Parameters.AddWithValue("@Till_Type" + k, Till_Type);
                            Login.ActionApproval_sc.Parameters.AddWithValue("@ObserveTill_Type" + k, ObserveTill_Type);

                            temp_query += " insert IGNORE into exchangeaccount_rate (Account_ID, ratedate, rate, FromTill_ID, ToTill_ID) values (@Account_ID" + k + ", @ActionDate" + k + ", @Rate" + k + ",(select tilltypes.id from tilltypes where tilltypes.name = @Till_Type" + k + "),(select tilltypes.id from tilltypes where tilltypes.name = @ObserveTill_Type" + k + "));";
                        }

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
                    DataView newView = new DataView(Deleted_Actions_dt);
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

                            Login.ActionApproval_sc.Parameters.AddWithValue("@Action_ID" + k, Action_ID);


                            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                            Login.ActionApproval_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

                            Login.ActionApproval_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);
                            Login.ActionApproval_sc.Parameters.AddWithValue("@Form_Name" + k, "Approval_Window");

                            temp_query += "Delete from action_deleted where ObserveAcion_ID = @Action_ID" + k + ";";
                            temp_query += "Delete from action_deleted where ID = @Action_ID" + k + ";";
                            temp_query += "Delete from action_attachments where action_id = @Action_ID" + k + ";";


                            temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'DELETE', 'action', @Action_ID" + k + ", '', '', '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

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
                    DataView newView = new DataView(Deleted_Actions_dt);
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

        private void Attachment_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                int Action_ID = Convert.ToInt32(dataRow["ID"].ToString());
                if (int.Parse(dataRow["SourceAction"].ToString()) != 1)
                {
                    MessageBox.Show("This operation was created from another till" + Environment.NewLine + "This operation can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                }
                else
                {
                    Window _Window = new Attachments(Action_ID, -1, false, null, null, this);
                    _Window.ShowDialog();
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


        private void Set_myDataGrid_ItemSource()
        {
            try
            {
                if (DeletedActions_RadioButton.IsChecked == false)
                {
                    Deleted_Actions = false;
                    Restore_btn.Visibility = Visibility.Collapsed;

                    foreach (var C in myDataGrid.Columns)
                    {
                        if (C.HeaderText.ToString() == "Deleted By" || C.HeaderText.ToString() == "Date Deleted")
                            C.IsHidden = true;
                        else if (C.HeaderText.ToString() == "Attachment")
                            C.IsHidden = false;
                    }


                    if (Actions_dt != null && Actions_dt.Rows.Count > 0)
                    {
                        All_textBlock.Text = Actions_dt.Rows.Count.ToString();
                        myDataGrid.ItemsSource = Actions_dt.DefaultView;
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
                    {
                        if (C.HeaderText.ToString() == "Deleted By" || C.HeaderText.ToString() == "Date Deleted")
                            C.IsHidden = false;
                        else if(C.HeaderText.ToString() == "Attachment")
                            C.IsHidden = true;
                    }


                    if (Deleted_Actions_dt != null && Deleted_Actions_dt.Rows.Count > 0)
                    {
                        All_textBlock.Text = Deleted_Actions_dt.Rows.Count.ToString();
                        myDataGrid.ItemsSource = Deleted_Actions_dt.DefaultView;
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
                    var allChecked = Actions_dt.AsEnumerable().All(row => row.Field<int>("IsSelected") == 1);
                    var anyChecked = Actions_dt.AsEnumerable().Any(row => row.Field<int>("IsSelected") == 1);
                    headerCheckbox.IsChecked = allChecked ? true : (anyChecked ? null : (bool?)false);
                }
            }
        }
    }
}
