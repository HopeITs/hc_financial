using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{

    public partial class Till_Window : Window
    {
        public Till_Window(int Account_ID, bool IsAdminAccount, string Account_Name, string Account_Type, string Category, int SubProject_ID, int Till_ID, string TillType_Name, Account_Window account_Window, Main_Window_2 main_Window)
        {
            InitializeComponent();
            AsFiscalBond = false;
            this.main_Window = main_Window;
            this.account_Window = account_Window;
            this.Account_ID = Account_ID;
            this.Account_Name = Account_Name;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.SubProject_ID = SubProject_ID;
            this.Till_ID = Till_ID;
            this.TillType_Name = TillType_Name;
            this.IsAdminAccount = IsAdminAccount;
            Onload();

        }

        public Till_Window(int FiscalBond_ID, string FiscalBond_Name, Main_Window_2 main_Window)
        {
            InitializeComponent();
            AsFiscalBond = true;
            this.main_Window = main_Window;
            this.FiscalBond_ID = FiscalBond_ID;
            this.FiscalBond_Name = FiscalBond_Name;
            DataContext = this;
            this.IsAdminAccount = false;

            if (Onload())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        Account_Window account_Window;
        int ID, Account_ID, Till_ID, SubProject_ID;
        string Account_Name, Account_Type, Category, TillType_Name;
        Till t;
        Models.Action a;
        private Beneficiary b;
        private Rate r;
        private Budget bud;
        private FiscalBond fiscalBond;
        private DataTable b_dt, dt, not_approved_dt, sum_dt, cash_dt, fb_dt; //Account_Budgets_dt;
        Main_Window_2 main_Window;
        bool IsAdminAccount;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();

        bool GridLoaded = false;
        double HOffset = 0;
        private DataRowView _lastCurrentItem;

        int FiscalBond_ID;
        string FiscalBond_Name;
        bool AsFiscalBond;

        #region privileges
        bool Edit_privileges, Add_privileges, Delete_privileges;

        #endregion

        private bool Onload()
        {
            try
            {
                b = new Beneficiary();
                t = new Till();
                a = new Models.Action();
                r = new Rate();
                bud = new Budget();
                fiscalBond = new FiscalBond();
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                    myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
                }
                myExpander.IsExpanded = false;

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
            if (AsFiscalBond)
            {
                fb_dt = fiscalBond.Select(FiscalBond_ID.ToString(), "", Properties.Settings.Default.User_ID.ToString(), "", false);

                Account_Type = fb_dt.Rows[0].Field<string>("accounttype");
                TillType_Name = fb_dt.Rows[0].Field<string>("Till");
                Account_ID = fb_dt.Rows[0].Field<int>("Account_ID");
                Account_Name = fb_dt.Rows[0].Field<string>("Account");
                Till_ID = fb_dt.Rows[0].Field<int>("Till_ID");
                SubProject_ID = fb_dt.Rows[0].Field<int>("SubProject_ID");
                Category = fb_dt.Rows[0].Field<string>("category");
            }

            //Account_Budgets_dt = bud.Select(Account_ID);
            Set_privileges();

            Show_Hide();

            ID = -1;
            LoadGrid();
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
                if (Account_Type == "Project")
                {
                    if (TillType_Name.ToLower() == "syr")
                    {
                        Edit_privileges = Add_privileges = Delete_privileges = true;
                    }
                    else
                    {
                        Add_privileges = true;
                    }
                }
                else if (Account_Type == "Personal")
                {
                    if (TillType_Name.ToLower() == "syr")
                    {
                        Edit_privileges = Add_privileges = Delete_privileges = true;
                    }
                }
                else if (Account_Type == "Exchange")
                {

                }
            }
            else if (Settings.Default.Role == "Accountant")
            {
                if (Account_Type == "Project")
                {
                    if (TillType_Name.ToLower() == "syr")
                    {
                        Edit_privileges = Add_privileges = Delete_privileges = true;
                    }
                }
                else if (Account_Type == "Personal")
                {
                    if (TillType_Name.ToLower() == "syr")
                    {
                        Edit_privileges = Add_privileges = Delete_privileges = true;
                    }
                }
                else if (Account_Type == "Exchange")
                {

                }
            }
            else if (Settings.Default.Role == "Cashier")
            {

            }
            else if (Settings.Default.Role == "Employee")
            {
                if (AsFiscalBond)
                    Add_privileges = true;
            }
            else if (Settings.Default.Role == "Donor")
            {

            }
        }

        private void Show_Hide()
        {
            if (AsFiscalBond)
            {
                Net_lable.Visibility = Net_Border.Visibility = CashNet_label.Visibility = CashNet_Border.Visibility = Visibility.Collapsed;
            }
            else
            {
                Net_lable.Visibility = Net_Border.Visibility = CashNet_label.Visibility = CashNet_Border.Visibility = Visibility.Visible;
            }

            if (!Add_privileges)
            {
                AddNewAction_btn.Visibility = AddNewAction_Label.Visibility = Visibility.Collapsed;
                AddMultiAction_btn.Visibility = AddMultiAction_Label.Visibility = Visibility.Collapsed;
                AddMultiAttachments_btn.Visibility = AddMultiAttachments_Label.Visibility = Visibility.Collapsed;

            }
            if (!Edit_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Approved" || C.HeaderText.ToString() == "Attachment" || C.HeaderText.ToString() == "Edit")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }
            }
            if (!Delete_privileges)
            {
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Delete" || C.HeaderText.ToString() == "Select")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }

                Delete_Selected.Visibility = Visibility.Collapsed;
            }


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
                    myExpander.Visibility = Visibility.Collapsed;
                    Grid_col.Width = new GridLength(100, GridUnitType.Star);
                    Expander_col.Width = new GridLength(0, GridUnitType.Star);

                    Outcome_Label.Visibility = Outcome_textBlock.Visibility = Outcome_Border.Visibility =
                        Returned_Label.Visibility = Returned_textBlock.Visibility = Returned_Border.Visibility = Visibility.Collapsed;

                    Outcome_Label1.Visibility = Outcome_textBlock1.Visibility =
                    Returned_Label1.Visibility = Returned_textBlock1.Visibility = Returned_Border1.Visibility = Visibility.Collapsed;


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
                Outcome_Label.Visibility = Outcome_textBlock.Visibility = Outcome_Border.Visibility =
                Returned_Label.Visibility = Returned_textBlock.Visibility = Returned_Border.Visibility = Visibility.Collapsed;

                Outcome_Label1.Visibility = Outcome_textBlock1.Visibility =
                Returned_Label1.Visibility = Returned_textBlock1.Visibility = Returned_Border1.Visibility = Visibility.Collapsed;
                if (TillType_Name.ToLower() != "syr")
                {
                    myExpander.Visibility = Visibility.Collapsed;
                    Grid_col.Width = new GridLength(100, GridUnitType.Star);
                    Expander_col.Width = new GridLength(0, GridUnitType.Star);
                }
                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" ||
                        C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                        C.HeaderText.ToString() == "Document type" || C.HeaderText.ToString() == "Document number" ||
                        C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount" || C.HeaderText.ToString() == "Value"
                        || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer")
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }
            }


            if (Account_Type == "Personal")
            {
                Outcome_Label.Visibility = Outcome_textBlock.Visibility = Outcome_Border.Visibility =
                Returned_Label.Visibility = Returned_textBlock.Visibility = Returned_Border.Visibility =
                Exchange_Label.Visibility = Exchange_textBlock.Visibility = Exchange_Border.Visibility = Visibility.Collapsed;

                Outcome_Label1.Visibility = Outcome_textBlock1.Visibility =
                Returned_Label1.Visibility = Returned_textBlock1.Visibility = Returned_Border1.Visibility =
                Exchange_Label1.Visibility = Exchange_textBlock1.Visibility = Exchange_Border1.Visibility = Visibility.Collapsed;

                if (TillType_Name.ToLower() == "syr")
                {
                    foreach (var C in myDataGrid.Columns)
                        if (C.HeaderText.ToString() == "Adv. Payment" || C.HeaderText.ToString() == "Returned" || C.HeaderText.ToString() == "Exchange" ||
                            C.HeaderText.ToString() == "Micro project number" || C.HeaderText.ToString() == "Beneficiary name" || C.HeaderText.ToString() == "Line" || C.HeaderText.ToString() == "Budget" ||
                            C.HeaderText.ToString() == "Donor" || C.HeaderText.ToString() == "Rate" || C.HeaderText.ToString() == "Exchanged" ||
                            C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount" || C.HeaderText.ToString() == "Value" ||
                            C.HeaderText.ToString() == "Corresponding till" || C.HeaderText.ToString() == "Corresponding account" || C.HeaderText.ToString() == "Corresponding cash till"
                            || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer")
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
                            || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchange cash till" || C.HeaderText.ToString() == "Volunteer")
                        {
                            C.IsHidden = true;
                            Originally_HiddenColumns.Add(C.HeaderText.ToString());
                        }

                    myExpander.Visibility = Visibility.Collapsed;
                    Grid_col.Width = new GridLength(100, GridUnitType.Star);
                    Expander_col.Width = new GridLength(0, GridUnitType.Star);
                }
            }
        }

        private void LoadGrid()
        {
            if (Login.BuildConnection())
            {
                DataTable temp_dt = a.Select("", Till_ID.ToString(), SubProject_ID.ToString(), true);

                DataTable Not_Approved_temp_dt = a.Select_action_not_approved("", Till_ID.ToString(), SubProject_ID.ToString(), true, false);

                int User_ID = Settings.Default.User_ID;
                List<int> CashAccount_ID = new List<int>();


                if (Settings.Default.User_CashAccounts != null && !string.IsNullOrEmpty(Settings.Default.User_CashAccounts) && !string.IsNullOrWhiteSpace(Settings.Default.User_CashAccounts))
                    CashAccount_ID = Settings.Default.User_CashAccounts.ToString().Split(',').Select(int.Parse).ToList();


                if (temp_dt != null && temp_dt.Rows.Count > 0)
                {
                    if (!AsFiscalBond)
                    {
                        dt = temp_dt;
                    }
                    else
                    {
                        if (Settings.Default.Role == "Employee")
                        {
                            string filterExpression = " created_byuser_ID = '" + User_ID + "'";

                            if (CashAccount_ID.Count > 0)
                            {
                                filterExpression += " OR ";
                                filterExpression += string.Join(" OR ", CashAccount_ID.Select(id => "CashAccount_ID = " + id));
                            }

                            System.Data.DataRow[] rows = temp_dt.Select(filterExpression);
                            DataTable selected_dt = new DataTable();
                            if (rows.Length != 0)
                            {
                                selected_dt = rows.CopyToDataTable();
                                dt = selected_dt;

                            }
                        }
                        else
                        {
                            dt = temp_dt;
                        }
                    }
                }
                else
                {
                    dt = null;
                }
                if (Not_Approved_temp_dt != null && Not_Approved_temp_dt.Rows.Count > 0)
                {
                    if (!AsFiscalBond)
                    {
                        not_approved_dt = Not_Approved_temp_dt;
                    }
                    else
                    {
                        if (Settings.Default.Role == "Employee")
                        {
                            string filterExpression = " created_byuser_ID = '" + User_ID + "'";

                            if (CashAccount_ID.Count > 0)
                            {
                                filterExpression += " OR ";
                                filterExpression += string.Join(" OR ", CashAccount_ID.Select(id => "CashAccount_ID = " + id));

                            }

                            System.Data.DataRow[] rows = Not_Approved_temp_dt.Select(filterExpression);
                            DataTable selected_dt = new DataTable();
                            if (rows.Length != 0)
                            {
                                selected_dt = rows.CopyToDataTable();
                                not_approved_dt = selected_dt;

                            }
                        }
                        else
                        {
                            not_approved_dt = Not_Approved_temp_dt;
                        }
                    }
                }
                else
                {
                    not_approved_dt = null;
                }

                if (dt != null)
                {
                    System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
                    dt.Columns.Add(newColumn);
                    dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));
                }


                if (not_approved_dt != null)
                {
                    System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(Int32));
                    not_approved_dt.Columns.Add(newColumn);
                    not_approved_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", 0));
                }

                Set_myDataGrid_ItemSource();

                sum_dt = new DataTable();
                cash_dt = new DataTable();
                DataSet ds = a.Select_Sums(Till_ID.ToString(), Account_ID, "");
                if (ds.Tables.Count > 0)
                {
                    sum_dt = ds.Tables[0];

                    if (ds.Tables.Count > 1)
                    {
                        DataTable temp_cash = ds.Tables[1];
                        if (temp_cash != null && temp_cash.Rows.Count > 0)
                        {
                            if (!AsFiscalBond)
                            {
                                cash_dt = temp_cash;
                            }
                            else
                            {
                                if (Settings.Default.Role == "Employee")
                                {
                                    string cash_filterExpression = "";

                                    if (CashAccount_ID.Count > 0)
                                    {
                                        cash_filterExpression += string.Join(" OR ", CashAccount_ID.Select(id => "CashAccount_id = " + id));

                                        System.Data.DataRow[] rows = temp_cash.Select(cash_filterExpression);
                                        DataTable selected_dt = new DataTable();
                                        if (rows.Length != 0)
                                        {
                                            selected_dt = rows.CopyToDataTable();
                                            cash_dt = selected_dt;
                                        }
                                    }
                                    else
                                    {
                                        cash_dt = null;
                                    }

                                }
                                else
                                {
                                    cash_dt = temp_cash;
                                }
                            }
                        }

                    }
                }
                if (!AsFiscalBond)
                {
                    if (sum_dt != null && sum_dt.Rows.Count > 0)
                    {
                        Income_textBlock.Text = sum_dt.Rows[0].Field<double>(0).ToString("N2");
                        Outcome_textBlock.Text = sum_dt.Rows[0].Field<double>(1).ToString("N2");
                        Returned_textBlock.Text = sum_dt.Rows[0].Field<double>(2).ToString("N2");
                        Exchange_textBlock.Text = sum_dt.Rows[0].Field<double>(3).ToString("N2");
                        Exchpand_textBlock.Text = sum_dt.Rows[0].Field<double>(4).ToString("N2");

                        CashNet_textBlock.Text = sum_dt.Rows[0].Field<double>(5).ToString("N2");
                        Net_textBlock.Text = sum_dt.Rows[0].Field<double>(6).ToString("N2");
                    }
                    else
                    {
                        Income_textBlock.Text = Outcome_textBlock.Text = Returned_textBlock.Text = Exchange_textBlock.Text =
                            Exchpand_textBlock.Text = CashNet_textBlock.Text = Net_textBlock.Text = "00,00";

                    }
                }
                else
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        double incomes = 0;
                        double outcomes = 0;
                        double returned = 0;
                        double exchange = 0;
                        double expaned = 0;
                        for (var i = 0; i < dt.Rows.Count; i++)
                        {
                            System.Data.DataRow R = dt.Rows[i];
                            if (double.Parse(R["Income"].ToString()) != 0)
                            {
                                incomes += double.Parse(R["Income"].ToString());
                            }
                            else if (double.Parse(R["Outcome"].ToString()) != 0)
                            {
                                outcomes += double.Parse(R["Outcome"].ToString());
                            }
                            else if (double.Parse(R["Returned"].ToString()) != 0)
                            {
                                returned += double.Parse(R["Returned"].ToString());
                            }
                            else if (double.Parse(R["Exchange"].ToString()) != 0)
                            {
                                exchange += double.Parse(R["Exchange"].ToString());
                            }
                            else if (double.Parse(R["Expand"].ToString()) != 0)
                            {
                                expaned += double.Parse(R["Expand"].ToString());
                            }
                        }
                        Income_textBlock.Text = incomes.ToString("N2");
                        Outcome_textBlock.Text = outcomes.ToString("N2");
                        Returned_textBlock.Text = returned.ToString("N2");
                        Exchange_textBlock.Text = exchange.ToString("N2");
                        Exchpand_textBlock.Text = expaned.ToString("N2");
                    }
                }

                if (Account_Type == "Project" || Account_Type == "Exchange" || Account_Type == "Personal")
                {
                    if (TillType_Name.ToLower() == "syr")
                    {
                        if (cash_dt != null && cash_dt.Rows.Count > 0)
                        {
                            Cash_DataGrid.ItemsSource = cash_dt.DefaultView;
                        }
                        else
                        {
                            Cash_DataGrid.ItemsSource = null;
                        }
                    }
                }
                GridLoaded = true;
                Bind_Show_Hide_Menu();
            }

        }


        private void Set_myDataGrid_ItemSource()
        {
            try
            {
                if (NotApproved_RadioButton.IsChecked == false)
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        All_textBlock.Text = dt.Rows.Count.ToString();
                        myDataGrid.ItemsSource = dt.DefaultView;
                    }
                    else
                    {
                        myDataGrid.ItemsSource = null;
                    }
                }
                else
                {
                    if (not_approved_dt != null && not_approved_dt.Rows.Count > 0)
                    {
                        //All_textBlock.Text = dt.Rows.Count.ToString();
                        myDataGrid.ItemsSource = not_approved_dt.DefaultView;
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


        #region datagrid functions
        private void myDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
        {
            try
            {
                if (myDataGrid.CurrentItem != null)
                {
                    _lastCurrentItem = (DataRowView)myDataGrid.CurrentItem;
                }

                int count = myDataGrid.SelectedItems.Count;
                Selected_textBlock.Text = count.ToString("N0");

                double selected_incomes = 0;
                double selected_outcomes = 0;
                double selected_returned = 0;
                double selected_Exchange = 0;
                double selected_expaned = 0;
                for (var i = 0; i < count; i++)
                {
                    DataRowView SelectedDataRow = (DataRowView)myDataGrid.SelectedItems[i];
                    if (double.Parse(SelectedDataRow["Income"].ToString()) != 0)
                    {
                        selected_incomes += double.Parse(SelectedDataRow["Income"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Outcome"].ToString()) != 0)
                    {
                        selected_outcomes += double.Parse(SelectedDataRow["Outcome"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Returned"].ToString()) != 0)
                    {
                        selected_returned += double.Parse(SelectedDataRow["Returned"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Exchange"].ToString()) != 0)
                    {
                        selected_Exchange += double.Parse(SelectedDataRow["Exchange"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Expand"].ToString()) != 0)
                    {
                        selected_expaned += double.Parse(SelectedDataRow["Expand"].ToString());
                    }
                }
                Income_textBlock1.Text = selected_incomes.ToString("N2");
                Outcome_textBlock1.Text = selected_outcomes.ToString("N2");
                Returned_textBlock1.Text = selected_returned.ToString("N2");
                Exchange_textBlock1.Text = selected_Exchange.ToString("N2");
                Exchpand_textBlock1.Text = selected_expaned.ToString("N2");
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

                    bool Approved_Action = false;

                    if (NotApproved_RadioButton.IsChecked == false)
                        Approved_Action = true;


                    Window _Window = new Attachments(Action_ID, -1, Approved_Action, this, null, null);
                    _Window.Show();
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

        #endregion


        #region datagrid btn clicks

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                int Action_ID = Convert.ToInt32(dataRow["ID"].ToString());
                int ObserveTill_ID = -1;
                int.TryParse(dataRow["ObserveTill_ID"].ToString(), out ObserveTill_ID);

                if (int.Parse(dataRow["SourceAction"].ToString()) != 1)
                {
                    MessageBox.Show("This operation was created from another till" + Environment.NewLine + "This operation can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                }
                else
                {
                    bool Approved_Action = false;
                    if (NotApproved_RadioButton.IsChecked == false)
                    {
                        Approved_Action = true;
                    }
                    AddAction_Window _Window = new AddAction_Window(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, Action_ID, SubProject_ID, true, Approved_Action, main_Window, this);

                    _Window.Show();
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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                    ID = Convert.ToInt32(dataRow["ID"].ToString());
                    string AType = "";


                    int ObserveTill_ID = -1;
                    int.TryParse(dataRow["ObserveTill_ID"].ToString(), out ObserveTill_ID);

                    if (int.Parse(dataRow["SourceAction"].ToString()) != 1)
                    {
                        MessageBox.Show("This operation was created from another till" + Environment.NewLine + "This operation can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                        ID = -1;
                    }
                    else
                    {
                        if (dataRow["Income"].ToString() != "0")
                            AType = "I";
                        if (dataRow["Outcome"].ToString() != "0")
                            AType = "O";
                        if (dataRow["Returned"].ToString() != "0")
                            AType = "R";
                        if (dataRow["Exchange"].ToString() != "0")
                            AType = "Ex";
                        if (dataRow["Expand"].ToString() != "0")
                            AType = "E";


                        bool Approved_Action = false;

                        if (NotApproved_RadioButton.IsChecked == false)
                            Approved_Action = true;


                        a.Delete(ID, Till_ID, AType, Approved_Action, "Till_Window");

                        ID = -1;

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
        #endregion
        private void AddNewAction_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddAction_Window _Window = new AddAction_Window(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, SubProject_ID, true, main_Window, this);
                _Window.ShowDialog();
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

        #region search
        private void Search_DataGrid()
        {
            DataTable selected_dt = new DataTable();
            selected_dt = dt.Copy();

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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    t.Delete(Till_ID, TillType_Name, Account_ID, "Till_Window");
                    account_Window.Fill_Account_Tabs("", "");
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

        private void myDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Edit_privileges)
                {
                    IInputElement element = e.MouseDevice.DirectlyOver;
                    if (element != null && element is FrameworkElement)
                    {
                        if (((FrameworkElement)element).Parent is GridCell)
                        {
                            var grid = sender as SfDataGrid;
                            if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                            {
                                var dataRow = grid.SelectedItem as dynamic;
                                int Action_ID = Convert.ToInt32(dataRow["ID"].ToString());
                                int ObserveTill_ID = -1;
                                int.TryParse(dataRow["ObserveTill_ID"].ToString(), out ObserveTill_ID);

                                if (int.Parse(dataRow["SourceAction"].ToString()) != 1)
                                {
                                    MessageBox.Show("This operation was created from another till" + Environment.NewLine + "This operation can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                }
                                else
                                {
                                    bool Approved_Action = false;
                                    if (NotApproved_RadioButton.IsChecked == false)
                                    {
                                        Approved_Action = true;
                                    }

                                    AddAction_Window _Window = new AddAction_Window(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, Action_ID, SubProject_ID, true, Approved_Action, main_Window, this);
                                    _Window.Show();
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

        #region Sizing TextBlock 

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;

        }

        private void Income_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Income_textBlock1.ActualWidth > Income_textBlock.ActualWidth)
                Income_textBlock.MinWidth = Income_textBlock1.ActualWidth;

            else
                Income_textBlock1.MinWidth = Income_textBlock.ActualWidth;

        }

        private void Outcome_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Outcome_textBlock1.ActualWidth > Outcome_textBlock.ActualWidth)
                Outcome_textBlock.MinWidth = Outcome_textBlock1.ActualWidth;
            else
                Outcome_textBlock1.MinWidth = Outcome_textBlock.ActualWidth;

        }

        private void Returned_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Returned_textBlock1.ActualWidth > Returned_textBlock.ActualWidth)
                Returned_textBlock.MinWidth = Returned_textBlock1.ActualWidth;
            else
                Returned_textBlock1.MinWidth = Returned_textBlock.ActualWidth;

        }

        private void Exchange_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Exchange_textBlock1.ActualWidth > Exchange_textBlock.ActualWidth)
                Exchange_textBlock.MinWidth = Exchange_textBlock1.ActualWidth;
            else
                Exchange_textBlock1.MinWidth = Exchange_textBlock.ActualWidth;

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

        private void AddMultiAttachments_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiActions_Window _Window = new Add_MultiActions_Window(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, SubProject_ID, true, main_Window, this, true);
                _Window.ShowDialog();
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

        private void Exchpand_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Exchpand_textBlock1.ActualWidth > Exchpand_textBlock.ActualWidth)
                Exchpand_textBlock.MinWidth = Exchpand_textBlock1.ActualWidth;

            else
                Exchpand_textBlock1.MinWidth = Exchpand_textBlock.ActualWidth;

        }
        #endregion


        private void Cash_DataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            try
            {
                if (e.Column is GridNumericColumn)
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                if (e.Column.MappingName == "CashAccount_id")
                    e.Cancel = true;

                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;

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

        private void Cash_DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (Cash_DataGrid.View != null)
            {
                Cash_Export_btn.CommandTarget = Cash_DataGrid;
            }
        }

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void NotApproved_RadioButton_Click(object sender, RoutedEventArgs e)
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

        private void AddMultiAction_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiActions_Window _Window = new Add_MultiActions_Window(Account_ID, IsAdminAccount, Account_Name, Account_Type, Category, Till_ID, TillType_Name, SubProject_ID, true, main_Window, this, false);
                _Window.ShowDialog();
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

        private void myExpander_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid_col.Width = new GridLength(60, GridUnitType.Star);
                Expander_col.Width = new GridLength(40, GridUnitType.Star);
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

        private void myExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid_col.Width = new GridLength(97, GridUnitType.Star);
                Expander_col.Width = new GridLength(3, GridUnitType.Star);
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


        private void Delete_Selected_TextBlock_Click(object sender, RoutedEventArgs e)
        {
            Delete_Selected_TextBlock.IsEnabled = false;
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    Login.DeleteMultiActions_sc.Parameters.Clear();
                    Login.DeleteMultiActions_List.Clear();

                    if (NotApproved_RadioButton.IsChecked == false)
                    {
                        DataView newView = new DataView(dt);
                        newView.RowFilter = "IsSelected = 1";

                        if (newView.Count == 0)
                        {
                            MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                            return;
                        }

                        DataView SourceActions = new DataView(dt);
                        SourceActions.RowFilter = "IsSelected = 1 AND SourceAction <> 1";
                        if (SourceActions.Count > 0)
                        {
                            MessageBox.Show("Some operations were created from another till" + Environment.NewLine + "This operations can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                            return;
                        }


                        for (int k = 0; k < newView.Count; k++)
                        {
                            string temp_query = "";
                            string log_query = "";



                            var Action_ID = newView[k]["ID"];
                            string AType = "";

                            if (newView[k]["Income"].ToString() != "0")
                                AType = "I";
                            if (newView[k]["Outcome"].ToString() != "0")
                                AType = "O";
                            if (newView[k]["Returned"].ToString() != "0")
                                AType = "R";
                            if (newView[k]["Exchange"].ToString() != "0")
                                AType = "Ex";
                            if (newView[k]["Expand"].ToString() != "0")
                                AType = "E";


                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@ID" + k, Action_ID);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Till_ID" + k, Till_ID);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@AType" + k, AType);

                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Form_Name" + k, "Till_Window");

                            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);


                            temp_query += "INSERT INTO action_deleted SELECT * FROM action WHERE action.id = @ID" + k + " OR action.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "Update action_deleted set `deleted_at` = @Todaydate" + k + ", `deleted_by` = @Uesr_ID" + k + " WHERE action_deleted.id = @ID" + k + " OR action_deleted.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action WHERE action.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action WHERE action.id  = @ID" + k + ";";

                            Login.DeleteMultiActions_List.Add(temp_query);
                            temp_query = "";

                        }

                        a.Delete_Multi_Actions();

                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                        LoadGrid();
                    }
                    else
                    {
                        DataView newView = new DataView(not_approved_dt);
                        newView.RowFilter = "IsSelected = 1";

                        if (newView.Count == 0)
                        {
                            MessageBox.Show("No item has been selected.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                            return;
                        }

                        DataView SourceActions = new DataView(not_approved_dt);
                        SourceActions.RowFilter = "IsSelected = 1 AND SourceAction <> 1";
                        if (SourceActions.Count > 0)
                        {
                            MessageBox.Show("Some operations were created from another till" + Environment.NewLine + "This operations can be modified and deleted from the corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                            return;
                        }


                        for (int k = 0; k < newView.Count; k++)
                        {
                            string temp_query = "";
                            string log_query = "";

                            var Action_ID = newView[k]["ID"];
                            string AType = "";

                            if (newView[k]["Income"].ToString() != "0")
                                AType = "I";
                            if (newView[k]["Outcome"].ToString() != "0")
                                AType = "O";
                            if (newView[k]["Returned"].ToString() != "0")
                                AType = "R";
                            if (newView[k]["Exchange"].ToString() != "0")
                                AType = "Ex";
                            if (newView[k]["Expand"].ToString() != "0")
                                AType = "E";


                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@ID" + k, Action_ID);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Till_ID" + k, Till_ID);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@AType" + k, AType);

                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Form_Name" + k, "Till_Window");

                            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);
                            Login.DeleteMultiActions_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);

                            temp_query += "INSERT INTO action_deleted SELECT * FROM action_not_approved WHERE action_not_approved.id = @ID" + k + " OR action_not_approved.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "Update action_deleted set `deleted_at` = @Todaydate" + k + ", `deleted_by` = @Uesr_ID" + k + " WHERE action_deleted.id = @ID" + k + " OR action_deleted.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action_not_approved WHERE action_not_approved.ObserveAcion_ID = @ID" + k + ";";

                            temp_query += "DELETE FROM action_not_approved WHERE action_not_approved.id  = @ID" + k + ";";

                            Login.DeleteMultiActions_List.Add(temp_query);
                            temp_query = "";
                        }

                        a.Delete_Multi_Actions();

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
            finally
            {
                Delete_Selected_TextBlock.IsEnabled = true;
            }
        }

    }
}
