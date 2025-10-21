using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Login = Financial_WPF_App.Models.Login;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_MultiActions_Window.xaml
    /// </summary>
    public partial class Add_MultiActions_Window : Window
    {
        public Add_MultiActions_Window(int Account_ID, bool IsAdminAccount, string Account_Name, string Account_Type, string Category, int Till_ID, string TillType, int SubProject_ID, bool FromTillForm, Main_Window_2 main_Window, Till_Window TW, bool MultiAttachments)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.TW = TW;
            this.Account_ID = Account_ID;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.Till_ID = Till_ID;
            this.TillType = TillType;
            this.SubProject_ID = SubProject_ID;
            this.IsAdminAccount = IsAdminAccount;
            this.MultiAttachments = MultiAttachments;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Add_MultiActions_Window(Main_Window_2 main_Window, bool MultiAttachments)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.MultiAttachments = MultiAttachments;
            this.Tag = "Add Multi-Attachments Window";
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        int Account_ID, Till_ID, SubProject_ID;
        string Account_Type; string Category;
        string inserted_value;


        Till t;
        private Models.Action a;
        private Account Acc;
        private Beneficiary b;
        private Rate r;
        private Budget_Lines bl;
        private Employee Emp;
        private Document d;
        bool IsAdminAccount;
        bool MultiAttachments;

        Double expandature, USD_Value;
        string TillType;
        private DataTable dt, Original_AvgRate_dt, AvgRate_dt, b_dt, t_dt, ExAcc_dt, Accounts_dt, Lines_dt,
            budgets_dt, all_cashAccounts_dt, Donors_dt, Employee_dt, Actions_dt;
        private DataTable tills_dt, tilltypes_dt;



        List<string> ShownColumns = new List<string>();
        Main_Window_2 main_Window;

        Till_Window TW;



        string Selected_budgetID;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";

        private MultiAttachments_CustomCopyPaste Attachment_copyPasteHandler;
        private CustomCopyPaste copyPasteHandler;

        #region privileges
        bool Income_privileges, Outcome_privileges, Returned_privileges,
            Exchange_privileges, ExchangeToForeign_privileges,
            Expand_privileges, Move_privileges;

        #endregion


        string failurText = "";
        int success = 0;
        int totalFiles = 0;


        DataTable OperationType_dt = new DataTable();
        DataTable AccountType_dt = new DataTable();


        private bool OnLoad()
        {
            try
            {
                b = new Beneficiary();
                t = new Till();
                Acc = new Account();
                r = new Rate();
                bl = new Budget_Lines();
                a = new Models.Action();
                Emp = new Employee();
                d = new Document();

                circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Collapsed;

                Set_privileges();

                Fill_Main_Comboboxs();

                if (!Account_Type.IsNullOrWhiteSpace() && !Account_Type.GetIsDynamicBound())
                {
                    if (Account_Type == "Project")
                    {
                        if (Category == "Micro Projects")
                        {
                            AccountType_comboBox.IsEnabled = false;
                            AccountType_comboBox.SelectedValue = "ComboBox_MicroProject_Item";
                        }
                        else if (Category == "Budget")
                        {
                            AccountType_comboBox.IsEnabled = false;
                            AccountType_comboBox.SelectedValue = "ComboBox_Budget_Item";
                        }
                    }
                    else if (Account_Type == "Exchange")
                    {
                        AccountType_comboBox.IsEnabled = false;
                        AccountType_comboBox.SelectedValue = "ComboBox_Exchange_Item";
                    }
                    else if (Account_Type == "Personal")
                    {
                        AccountType_comboBox.IsEnabled = false;
                        AccountType_comboBox.SelectedValue = "ComboBox_Personal_Item";
                    }

                    if (Till_ID > 0)
                    {
                        Tilltype_comboBox.IsEnabled = false;

                        int tillyype_ID = tilltypes_dt.AsEnumerable()
                         .Where(row => row.Field<string>("Name")?.ToLower() == TillType.ToLower())
                         .Select(row => row.Field<int>("ID"))
                         .DefaultIfEmpty(-1)
                         .FirstOrDefault();

                        Tilltype_comboBox.SelectedValue = tillyype_ID;
                    }
                }

                Load_Form();

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


            if (!MultiAttachments)
            {
                copyPasteHandler = new CustomCopyPaste(myDataGrid, Account_Type, TillType, inserted_value, ShownColumns, ExchangeToForeign_privileges,
                        Original_AvgRate_dt, AvgRate_dt, b_dt, t_dt, ExAcc_dt, Accounts_dt,
                        Lines_dt, budgets_dt, all_cashAccounts_dt, Donors_dt, Employee_dt, tills_dt);
                this.myDataGrid.GridCopyPaste = copyPasteHandler;
                copyPasteHandler.PasteCompleted += CopyPasteHandler_PasteCompleted;

            }
            else
            {
                Attachment_copyPasteHandler = new MultiAttachments_CustomCopyPaste(myDataGrid, Actions_dt, tills_dt, Account_Type, TillType);
                this.myDataGrid.GridCopyPaste = Attachment_copyPasteHandler;
                Attachment_copyPasteHandler.PasteCompleted += CopyPasteHandler_PasteCompleted;
            }

        }

        private void Apply_Color_Theme()
        {

            if (Account_Type == "Project")
            {
                if (Category == "Micro Projects")
                {
                    Theme_Color = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                }
                else if (Category == "Budget")
                {
                    Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                }
            }
            else if (Account_Type == "Exchange")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["Orange"];
            }
            else if (Account_Type == "Personal")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelGreen"];
            }
            else if (Account_Type == "Cash")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"];
            }


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Apply_DataGrid_Color_Theme()
        {
            if (Account_Type == "Project")
            {
                if (Category == "Micro Projects")
                {
                    HeaderStyle_Name = "HopeRed_SfDataGrid_header";
                    TableSummaryRowStyle_Name = "HopeRed_TableSummaryRowStyle";
                    GroupSummaryRowStyle = "HopeRed_GroupSummaryRowStyle";

                }
                else if (Category == "Budget")
                {
                    HeaderStyle_Name = "PastelDarkBlue_SfDataGrid_header";
                    TableSummaryRowStyle_Name = "PastelDarkBlue_TableSummaryRowStyle";
                    GroupSummaryRowStyle = "PastelDarkBlue_GroupSummaryRowStyle";
                }
            }
            else if (Account_Type == "Exchange")
            {
                HeaderStyle_Name = "Orange_SfDataGrid_header";
                TableSummaryRowStyle_Name = "Orange_TableSummaryRowStyle";
                GroupSummaryRowStyle = "Orange_GroupSummaryRowStyle";
            }
            else if (Account_Type == "Personal")
            {
                HeaderStyle_Name = "PastelGreen_SfDataGrid_header";
                TableSummaryRowStyle_Name = "PastelGreen_TableSummaryRowStyle";
                GroupSummaryRowStyle = "PastelGreen_GroupSummaryRowStyle";
            }
            else if (Account_Type == "Cash")
            {
                HeaderStyle_Name = "DarkgrayishViolet_SfDataGrid_header";
                TableSummaryRowStyle_Name = "DarkgrayishViolet_TableSummaryRowStyle";
                GroupSummaryRowStyle = "DarkgrayishViolet_GroupSummaryRowStyle";
            }

            myDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            myDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            myDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];

        }

        private void Fill_Main_Comboboxs()
        {

            OperationType_dt.Columns.AddRange(new System.Data.DataColumn[] {
                        new System.Data.DataColumn("ID", typeof(string)),
                        new System.Data.DataColumn("Name", typeof(string))});

            OperationType_dt.Rows.Add("Type_ComboBox_Income_Item", "Income");
            OperationType_dt.Rows.Add("Type_ComboBox_Outcome_Item", "Adv. Payment");
            OperationType_dt.Rows.Add("Type_ComboBox_Returned_Item", "Returned");
            OperationType_dt.Rows.Add("Type_ComboBox_Exchange_Item", "Exchange");
            OperationType_dt.Rows.Add("Type_ComboBox_Expand_Item", "Expenses");
            OperationType_dt.Rows.Add("Type_ComboBox_Move_Item", "Move");



            AccountType_comboBox.ItemsSource = null;

            AccountType_dt.Columns.AddRange(new System.Data.DataColumn[] {
                        new System.Data.DataColumn("ID", typeof(string)),
                        new System.Data.DataColumn("Name", typeof(string))});


            AccountType_dt.Rows.Add("ComboBox_MicroProject_Item", "Micro Project");
            AccountType_dt.Rows.Add("ComboBox_Budget_Item", "Budget");
            AccountType_dt.Rows.Add("ComboBox_Exchange_Item", "Exchange");
            AccountType_dt.Rows.Add("ComboBox_Personal_Item", "Personal");

            AccountType_comboBox.ItemsSource = AccountType_dt.DefaultView;



            tills_dt = null;

            DataTable temp_tills_dt = t.Select_All_tills_ForCombobox();

            DataTable Projects_dt = main_Window.AllAccounts_ds.Tables[0];
            DataTable Fiscalbonds = main_Window.AllAccounts_ds.Tables[2];
            DataTable PersonalAccounts_dt = main_Window.AllAccounts_ds.Tables[4];

            List<int> User_Subprojects = new List<int>();
            List<int> User_Accounts = new List<int>();
            List<int> User_PersonalAccounts = new List<int>();
            List<int> User_Fiscalbonds = new List<int>();


            string Subprojects_filterExpression = "";
            string Accounts_filterExpression = "";
            string PersonalAccounts_filterExpression = "";
            string Fiscalbonds_filterExpression = "";
            string filterExpression = "";

            if (temp_tills_dt != null && temp_tills_dt.Rows.Count > 0)
            {
                if (Settings.Default.Role == "Head Office")
                {
                    tills_dt = temp_tills_dt;
                }
                else if (Settings.Default.Role == "Financial Manager")
                {
                    if (Projects_dt != null && Projects_dt.Rows.Count > 0)
                    {
                        User_Subprojects = Projects_dt.AsEnumerable()
                               .Select(row => row.Field<int>("SubprojectID"))
                               .Distinct()
                               .ToList();

                        Subprojects_filterExpression = string.Join(" OR ", User_Subprojects.Select(id => "Subproject_ID = " + id));

                        filterExpression = "(" + Subprojects_filterExpression + ")";
                    }

                    if (PersonalAccounts_dt != null && PersonalAccounts_dt.Rows.Count > 0)
                    {
                        User_PersonalAccounts = PersonalAccounts_dt.AsEnumerable()
                               .Select(row => row.Field<int>("ID"))
                               .Distinct()
                               .ToList();

                        PersonalAccounts_filterExpression = string.Join(" OR ", User_PersonalAccounts.Select(id => "Account_ID = " + id));
                        PersonalAccounts_filterExpression = "(" + PersonalAccounts_filterExpression + ")";
                        PersonalAccounts_filterExpression += " AND Name = 'SYR'";

                        if (filterExpression == "")
                        {
                            filterExpression = "(" + PersonalAccounts_filterExpression + ")";
                        }
                        else
                        {
                            filterExpression += " OR (" + PersonalAccounts_filterExpression + ")";
                        }
                    }

                    if (filterExpression != "")
                        filterExpression = "(" + filterExpression + ") AND is_administrative = 0";

                    System.Data.DataRow[] temprows = temp_tills_dt.Select(filterExpression);
                    DataTable selected_dt = new DataTable();
                    if (temprows.Length != 0)
                    {
                        selected_dt = temprows.CopyToDataTable();
                        tills_dt = selected_dt;
                    }

                }
                else if (Settings.Default.Role == "Accountant")
                {
                    if (Projects_dt != null && Projects_dt.Rows.Count > 0)
                    {
                        User_Accounts = Projects_dt.AsEnumerable()
                               .Select(row => row.Field<int>("AccountID"))
                               .Distinct()
                               .ToList();

                        Accounts_filterExpression = string.Join(" OR ", User_Accounts.Select(id => "Account_ID = " + id));
                        Accounts_filterExpression = "(" + Accounts_filterExpression + ")";
                        Accounts_filterExpression += " AND Name = 'SYR'";


                        filterExpression = "(" + Accounts_filterExpression + ")";
                    }

                    if (PersonalAccounts_dt != null && PersonalAccounts_dt.Rows.Count > 0)
                    {
                        User_PersonalAccounts = PersonalAccounts_dt.AsEnumerable()
                               .Select(row => row.Field<int>("ID"))
                               .Distinct()
                               .ToList();

                        PersonalAccounts_filterExpression = string.Join(" OR ", User_PersonalAccounts.Select(id => "Account_ID = " + id));
                        PersonalAccounts_filterExpression = "(" + PersonalAccounts_filterExpression + ")";
                        PersonalAccounts_filterExpression += " AND Name = 'SYR'";

                        if (filterExpression == "")
                        {
                            filterExpression = "(" + PersonalAccounts_filterExpression + ")";
                        }
                        else
                        {
                            filterExpression += " OR (" + PersonalAccounts_filterExpression + ")";
                        }
                    }

                    if (filterExpression != "")
                        filterExpression = "(" + filterExpression + ") AND is_administrative = 0 AND IsClosed = 0";


                    System.Data.DataRow[] temprows = temp_tills_dt.Select(filterExpression);
                    DataTable selected_dt = new DataTable();
                    if (temprows.Length != 0)
                    {
                        selected_dt = temprows.CopyToDataTable();
                        tills_dt = selected_dt;
                    }


                }
                else if (Settings.Default.Role == "Employee")
                {
                    if (Fiscalbonds != null && Fiscalbonds.Rows.Count > 0)
                    {
                        User_Fiscalbonds = Fiscalbonds.AsEnumerable()
                               .Select(row => row.Field<int>("FiscalbondID"))
                               .Distinct()
                               .ToList();

                        Fiscalbonds_filterExpression = string.Join(" OR ", User_Fiscalbonds.Select(id => "Fiscalbond_ID = " + id));
                        Fiscalbonds_filterExpression = "(" + Fiscalbonds_filterExpression + ")";
                        Fiscalbonds_filterExpression += " AND Name = 'SYR'";


                        filterExpression = "(" + Fiscalbonds_filterExpression + ")";
                    }

                    if (filterExpression != "")
                        filterExpression = "(" + filterExpression + ") AND is_administrative = 0 AND IsClosed = 0";


                    System.Data.DataRow[] temprows = temp_tills_dt.Select(filterExpression);
                    DataTable selected_dt = new DataTable();
                    if (temprows.Length != 0)
                    {
                        selected_dt = temprows.CopyToDataTable();
                        tills_dt = selected_dt;
                    }
                }
            }
        }

        private void Load_Form()
        {
            Set_Available_Operations();
        }

        private void Set_privileges()
        {
            Income_privileges = Outcome_privileges = Returned_privileges = Exchange_privileges = ExchangeToForeign_privileges
                = Expand_privileges = Move_privileges = false;

            if (Settings.Default.Role == "Head Office")
            {
                Income_privileges = Outcome_privileges = Returned_privileges = Exchange_privileges = ExchangeToForeign_privileges
                    = Expand_privileges = Move_privileges = true;
            }
            else if (Settings.Default.Role == "Project Manager")
            {

            }
            else if (Settings.Default.Role == "Financial Manager")
            {
                if (Account_Type == "Project")
                {
                    if (TillType.ToLower() == "syr")
                    {
                        Income_privileges = Outcome_privileges = Returned_privileges = Exchange_privileges = Expand_privileges = Move_privileges = true;
                    }
                    else
                    {
                        Exchange_privileges = true;
                    }
                }
                else if (Account_Type == "Personal")
                {
                    if (TillType.ToLower() == "syr")
                    {
                        Income_privileges = Outcome_privileges = Returned_privileges = Exchange_privileges = Expand_privileges = Move_privileges = true;
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
                    if (TillType.ToLower() == "syr")
                    {
                        Income_privileges = Outcome_privileges = Returned_privileges = Exchange_privileges = Expand_privileges = Move_privileges = true;
                    }
                }
                else if (Account_Type == "Personal")
                {
                    if (TillType.ToLower() == "syr")
                    {
                        Income_privileges = Outcome_privileges = Returned_privileges = Exchange_privileges = Expand_privileges = Move_privileges = true;
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
                if (TillType.ToLower() == "syr")
                {
                    Income_privileges = Outcome_privileges = Returned_privileges = Expand_privileges = true;
                }
            }
            else if (Settings.Default.Role == "Donor")
            {

            }
        }

        private void Set_Available_Operations()
        {
            OperationType_dt.Clear();
            OperationType_dt.Rows.Add("Type_ComboBox_Income_Item", "Income");
            OperationType_dt.Rows.Add("Type_ComboBox_Outcome_Item", "Adv. Payment");
            OperationType_dt.Rows.Add("Type_ComboBox_Returned_Item", "Returned");
            OperationType_dt.Rows.Add("Type_ComboBox_Exchange_Item", "Exchange");
            OperationType_dt.Rows.Add("Type_ComboBox_Expand_Item", "Expenses");
            OperationType_dt.Rows.Add("Type_ComboBox_Move_Item", "Move");

            var valuesToRemove = new HashSet<string>();

            if (Account_Type == "Project" && TillType != null)
            {
                if (TillType.ToLower() == "syr")
                {
                    valuesToRemove.Add("Type_ComboBox_Income_Item");

                    if (!Outcome_privileges)
                        valuesToRemove.Add("Type_ComboBox_Outcome_Item");

                    if (!Returned_privileges)
                        valuesToRemove.Add("Type_ComboBox_Returned_Item");

                    // تم إيقاف هذه العملية لحين دراستها
                    //if (Exchange_privileges)
                    //    Type_ComboBox_Exchange_Item.Visibility = Visibility.Visible;
                    //else
                    //    Type_ComboBox_Exchange_Item.Visibility = Visibility.Collapsed;
                    //Type_ComboBox_Exchange_Item.Visibility = Visibility.Collapsed;

                    valuesToRemove.Add("Type_ComboBox_Exchange_Item");

                    if (!Expand_privileges)
                        valuesToRemove.Add("Type_ComboBox_Expand_Item");


                    // تم إيقاف هذه العملية لحين دراستها
                    //if (Move_privileges)
                    //    Type_ComboBox_Move_Item.Visibility = Visibility.Visible;
                    //else
                    //    Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;

                    //Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;

                    valuesToRemove.Add("Type_ComboBox_Move_Item");
                }
                else
                {
                    if (!Income_privileges)
                        valuesToRemove.Add("Type_ComboBox_Income_Item");

                    valuesToRemove.Add("Type_ComboBox_Outcome_Item");
                    valuesToRemove.Add("Type_ComboBox_Returned_Item");

                    if (!Exchange_privileges)
                        valuesToRemove.Add("Type_ComboBox_Exchange_Item");

                    valuesToRemove.Add("Type_ComboBox_Expand_Item");


                    // تم إيقاف هذه العملية لحين دراستها
                    //if (Move_privileges)
                    //    Type_ComboBox_Move_Item.Visibility = Visibility.Visible;
                    //else
                    //    Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;

                    valuesToRemove.Add("Type_ComboBox_Move_Item");

                }
            }
            else if (Account_Type == "Exchange")
            {
                valuesToRemove.Add("Type_ComboBox_Income_Item");
                valuesToRemove.Add("Type_ComboBox_Outcome_Item");
                valuesToRemove.Add("Type_ComboBox_Returned_Item");

                if (!Exchange_privileges)
                    valuesToRemove.Add("Type_ComboBox_Exchange_Item");

                valuesToRemove.Add("Type_ComboBox_Expand_Item");
                valuesToRemove.Add("Type_ComboBox_Move_Item");

            }
            else if (Account_Type == "Personal")
            {
                if (!Income_privileges)
                    valuesToRemove.Add("Type_ComboBox_Income_Item");


                valuesToRemove.Add("Type_ComboBox_Outcome_Item");
                valuesToRemove.Add("Type_ComboBox_Returned_Item");
                valuesToRemove.Add("Type_ComboBox_Exchange_Item");

                if (!Expand_privileges)
                    valuesToRemove.Add("Type_ComboBox_Expand_Item");


                valuesToRemove.Add("Type_ComboBox_Move_Item");
            }
            else
            {
                valuesToRemove.Add("Type_ComboBox_Income_Item");
                valuesToRemove.Add("Type_ComboBox_Outcome_Item");
                valuesToRemove.Add("Type_ComboBox_Returned_Item");
                valuesToRemove.Add("Type_ComboBox_Exchange_Item");
                valuesToRemove.Add("Type_ComboBox_Expand_Item");
                valuesToRemove.Add("Type_ComboBox_Move_Item");
            }

            OperationType_dt.AsEnumerable()
                   .Where(row => valuesToRemove.Contains(row.Field<string>("ID")))
                   .ToList()
                   .ForEach(row => OperationType_dt.Rows.Remove(row));

            Type_comboBox.ItemsSource = OperationType_dt.DefaultView;
        }

        private void Type_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                Original_AvgRate_dt = AvgRate_dt = b_dt = t_dt = ExAcc_dt = Accounts_dt = Lines_dt = budgets_dt = all_cashAccounts_dt = Donors_dt = Actions_dt = null;
                inserted_value = "";

                if (Tilltype_comboBox.SelectedIndex != -1)
                {
                    if (Type_comboBox.SelectedIndex != -1)
                    {
                        if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                        {
                            inserted_value = "I";
                        }
                        else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                        {
                            inserted_value = "O";
                        }
                        else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                        {
                            inserted_value = "R";
                        }
                        else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                        {
                            inserted_value = "E";
                        }
                        else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                        {
                            inserted_value = "Ex";
                        }
                        else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                        {
                            inserted_value = "M";
                        }
                    }
                    else
                    {
                        inserted_value = "";
                    }
                    Create_Columns();

                    foreach (GridColumn c in myDataGrid.Columns)
                    {
                        ShownColumns.Add(c.HeaderText);
                    }
                    Fill_Datatables();

                    InitializeCopyPaste();

                    Export_btn.CommandTarget = myDataGrid;
                }
                else
                {
                    Create_Columns();
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

        private void Fill_Datatables()
        {
            if (Type_comboBox.SelectedIndex != -1)
            {
                if (!MultiAttachments)
                {
                    if (ShownColumns.Contains("Origin Avg Rate"))
                    {
                        if (Account_ID > 0)
                            Original_AvgRate_dt = r.Select_AllAvilableRates(Account_ID.ToString(), TillType, false);
                        else
                            Original_AvgRate_dt = r.Select_AllAvilableRates("", TillType, false);
                    }
                    else
                    {
                        Original_AvgRate_dt = null;
                    }
                    if (ShownColumns.Contains("Micro Project number"))
                    {
                        if (Account_ID > 0)
                            b_dt = b.Select("", Account_ID.ToString(), "");
                        else
                            b_dt = b.Select("", "", "");
                    }
                    else
                    {
                        b_dt = null;
                    }
                    if (ShownColumns.Contains("Corresponding till"))
                    {
                        if (Account_ID > 0)
                            t_dt = t.Select(Account_ID.ToString(), Till_ID, "");
                        else
                            t_dt = t.Select("", -1, "");
                    }
                    else
                    {
                        t_dt = null;
                    }
                    if (ShownColumns.Contains("Exchange account"))
                    {
                        ExAcc_dt = Acc.Select_ExAcc_Tills(TillType, "");
                    }
                    else
                    {
                        ExAcc_dt = null;
                    }
                    if (ShownColumns.Contains("Corresponding account"))
                    {
                        //Accounts_dt = Acc.Select_Accounts_With_Same_TillType(TillType, Account_ID);
                        DataTable temp_Accounts_dt = null;
                        if (Account_ID > 0)
                            temp_Accounts_dt = Acc.Select_Accounts_With_Same_TillType(TillType, Account_ID, -1);
                        else
                            temp_Accounts_dt = Acc.Select_Accounts_With_Same_TillType(TillType, -1, -1);

                        List<int> User_Subprojects_ID = new List<int>();
                        List<int> User_Accounts_ID = new List<int>();

                        if (Settings.Default.Role == "Financial Manager")
                        {
                            User_Subprojects_ID = Settings.Default.User_Subprojects.ToString().Split(',').Select(int.Parse).ToList();

                            string filterExpression = string.Join(" OR ", User_Subprojects_ID.Select(id => "subproject_ID = " + id));

                            System.Data.DataRow[] temprows = temp_Accounts_dt.Select(filterExpression);
                            DataTable selected_dt = new DataTable();
                            if (temprows.Length != 0)
                            {
                                selected_dt = temprows.CopyToDataTable();
                                Accounts_dt = selected_dt;
                            }
                        }
                        else if (Settings.Default.Role == "Accountant")
                        {
                            User_Accounts_ID = Settings.Default.User_Accounts.ToString().Split(',').Select(int.Parse).ToList();

                            string filterExpression = string.Join(" OR ", User_Accounts_ID.Select(id => "account_id = " + id));

                            System.Data.DataRow[] temprows = temp_Accounts_dt.Select(filterExpression);
                            DataTable selected_dt = new DataTable();
                            if (temprows.Length != 0)
                            {
                                selected_dt = temprows.CopyToDataTable();
                                Accounts_dt = selected_dt;
                            }
                        }
                        else
                        {
                            Accounts_dt = temp_Accounts_dt;
                        }
                    }
                    else
                    {
                        Accounts_dt = null;
                    }
                    if (ShownColumns.Contains("Line"))
                    {
                        if (Account_ID > 0)
                            Lines_dt = bl.Select_AllAvilableBudgetLines(Account_ID);
                        else
                            Lines_dt = bl.Select_AllAvilableBudgetLines(-1);

                        if (Lines_dt != null && Lines_dt.Rows.Count > 0)
                        {
                            DataView view = new DataView(Lines_dt);
                            budgets_dt = view.ToTable(true, "budget_ID", "budget_Name", "Account_ID");
                        }
                    }
                    else
                    {
                        Lines_dt = null;
                        budgets_dt = null;
                    }
                    if (ShownColumns.Contains("Cash till") || ShownColumns.Contains("Corresponding cash till") || ShownColumns.Contains("Exchange cash till"))
                    {
                        DataTable temp_dt = Acc.Select_Cash_Account("", "");

                        if (Settings.Default.Role == "Accountant" || Settings.Default.Role == "Financial Manager" || Settings.Default.Role == "Employee")
                        {
                            List<int> User_CashAccounts_ID = new List<int>();
                            if (Settings.Default.User_CashAccounts != null && !string.IsNullOrEmpty(Settings.Default.User_CashAccounts.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_CashAccounts.ToString()))
                            {
                                User_CashAccounts_ID = Settings.Default.User_CashAccounts.ToString().Split(',').Select(int.Parse).ToList();

                                string filterExpression = string.Join(" OR ", User_CashAccounts_ID.Select(id => "ID = " + id));

                                System.Data.DataRow[] temprows = temp_dt.Select(filterExpression);
                                DataTable selected_dt = new DataTable();
                                if (temprows.Length != 0)
                                {
                                    selected_dt = temprows.CopyToDataTable();
                                    all_cashAccounts_dt = selected_dt;

                                }
                            }
                            else
                            {
                                all_cashAccounts_dt = null;
                            }

                        }
                        else
                        {
                            all_cashAccounts_dt = temp_dt;
                        }
                    }
                    else
                    {
                        all_cashAccounts_dt = null;
                    }
                    if (ShownColumns.Contains("Donor"))
                    {
                        if (Account_ID > 0)
                            Donors_dt = Acc.Select_Account_Donors(Account_ID);
                        else
                            Donors_dt = Acc.Select_Account_Donors(-1);
                    }
                    else
                    {
                        Donors_dt = null;
                    }
                    if (inserted_value == "Ex" & Account_Type == "Project")
                    {
                        if (Account_ID > 0)
                            AvgRate_dt = r.Check_AvgRate_Availability(Account_ID.ToString(), "", TillType, "");
                        else
                            AvgRate_dt = r.Check_AvgRate_Availability("", "", TillType, "");
                    }
                    else
                    {
                        AvgRate_dt = null;
                    }
                    if (ShownColumns.Contains("Volunteer"))
                    {
                        if (SubProject_ID > 0)
                            Employee_dt = Emp.Select_Subproject_Monthly_Employee(SubProject_ID);
                        else
                            Employee_dt = Emp.Select_Subproject_Monthly_Employee(-1);
                    }
                    else
                    {
                        Employee_dt = null;
                    }
                }
                else
                {
                    if (Account_ID > 0)
                        Actions_dt = a.Select_All_Actions("", "", "", Account_ID.ToString(), TillType);
                    else
                    {

                        Actions_dt = a.Select_All_Actions(Account_Type, Category, inserted_value, "", TillType);
                    }
                }
            }

        }

        private void Create_Columns()
        {

            Login.MultiAction_AnyError = false;
            Login.MultiAction_List.Clear();
            Login.MultiAction_sc.Parameters.Clear();


            Login.MultiAttachment_AnyError = false;
            Login.MultiAttachment_Dictionary.Clear();


            dt = new DataTable();

            if (Account_Type == "Project")
            {
                dt.Columns.Add("Project");
            }
            dt.Columns.Add("Account");

            if (!MultiAttachments)
            {
                if (Type_comboBox.SelectedIndex != -1)
                {
                    dt.Columns.Add("Amount");
                    dt.Columns.Add("Operation date");
                    dt.Columns.Add("Description (AR)");
                    dt.Columns.Add("Description (Eng)");

                    if (Account_Type == "Project" && TillType != null)
                    {
                        if (Category == "Micro Projects")
                        {
                            if (TillType.ToLower() == "syr")
                            {
                                if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                                {
                                    //Not allowed.
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                                {
                                    dt.Columns.Add("Micro Project number");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                                {
                                    dt.Columns.Add("Micro Project number");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                                {
                                    dt.Columns.Add("Corresponding till");
                                    dt.Columns.Add("Cash till");
                                    dt.Columns.Add("Exchange account");
                                    dt.Columns.Add("Exchange cash till");
                                    dt.Columns.Add("Origin Avg Rate");
                                    dt.Columns.Add("Rate");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                                {
                                    dt.Columns.Add("Micro Project number");
                                    dt.Columns.Add("Cash till");
                                    dt.Columns.Add("Document type");
                                    dt.Columns.Add("Document number");
                                    dt.Columns.Add("Origin Avg Rate");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                                {
                                    dt.Columns.Add("Corresponding account");
                                    dt.Columns.Add("Cash till");
                                    dt.Columns.Add("Corresponding cash till");
                                    dt.Columns.Add("Origin Avg Rate");
                                }
                            }
                            else
                            {
                                if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                                {
                                    dt.Columns.Add("Donor");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                                {
                                    //Not allowed.
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                                {
                                    //Not allowed.
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                                {
                                    dt.Columns.Add("Corresponding till");
                                    dt.Columns.Add("Corresponding cash till");
                                    dt.Columns.Add("Donor");
                                    dt.Columns.Add("Exchange account");
                                    dt.Columns.Add("Exchange cash till");
                                    dt.Columns.Add("Origin Avg Rate");
                                    dt.Columns.Add("Rate");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                                {
                                    //Not allowed.
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                                {
                                    dt.Columns.Add("Donor");
                                    dt.Columns.Add("Corresponding account");
                                    dt.Columns.Add("Origin Avg Rate");
                                }
                            }
                        }
                        else if (Category == "Budget")
                        {
                            if (TillType.ToLower() == "syr")
                            {
                                if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                                {
                                    //Not allowed.
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                                {
                                    dt.Columns.Add("Budget");
                                    dt.Columns.Add("Line");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                                {
                                    dt.Columns.Add("Budget");
                                    dt.Columns.Add("Line");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                                {
                                    dt.Columns.Add("Corresponding till");
                                    dt.Columns.Add("Corresponding cash till");
                                    dt.Columns.Add("Exchange account");
                                    dt.Columns.Add("Exchange cash till");
                                    dt.Columns.Add("Budget");
                                    dt.Columns.Add("Line");
                                    dt.Columns.Add("Origin Avg Rate");
                                    dt.Columns.Add("Rate");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                                {
                                    dt.Columns.Add("Budget");
                                    dt.Columns.Add("Line");
                                    dt.Columns.Add("Cash till");
                                    dt.Columns.Add("Document type");
                                    dt.Columns.Add("Document number");
                                    dt.Columns.Add("Volunteer");
                                    dt.Columns.Add("Origin Avg Rate");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                                {
                                    dt.Columns.Add("Corresponding account");
                                    dt.Columns.Add("Budget");
                                    dt.Columns.Add("Line");
                                    dt.Columns.Add("Cash till");
                                    dt.Columns.Add("Corresponding cash till");
                                    dt.Columns.Add("Origin Avg Rate");
                                }
                            }
                            else
                            {
                                if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                                {
                                    dt.Columns.Add("Donor");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                                {
                                    //Not allowed.
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                                {
                                    //Not allowed.
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                                {
                                    dt.Columns.Add("Corresponding till");
                                    dt.Columns.Add("Corresponding cash till");
                                    dt.Columns.Add("Exchange account");
                                    dt.Columns.Add("Exchange cash till");
                                    dt.Columns.Add("Donor");
                                    dt.Columns.Add("Origin Avg Rate");
                                    dt.Columns.Add("Rate");
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                                {
                                    //Not allowed.
                                }
                                else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                                {
                                    dt.Columns.Add("Donor");
                                    dt.Columns.Add("Corresponding account");
                                    dt.Columns.Add("Origin Avg Rate");
                                }
                            }
                        }
                    }
                    else if (Account_Type == "Exchange" && TillType != null)
                    {
                        if (TillType.ToLower() == "syr")
                        {
                            if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                            {
                                dt.Columns.Add("Corresponding till");
                                dt.Columns.Add("Cash till");
                                dt.Columns.Add("Rate");
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                            {
                                //Not allowed.
                            }
                        }
                        else
                        {
                            if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                            {
                                dt.Columns.Add("Corresponding till");
                                dt.Columns.Add("Corresponding cash till");
                                dt.Columns.Add("Rate");
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                            {
                                //Not allowed.
                            }
                        }
                    }
                    else if (Account_Type == "Personal" && TillType != null)
                    {
                        if (TillType.ToLower() == "syr")
                        {
                            if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                            {
                                dt.Columns.Add("Cash till");
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                            {
                                dt.Columns.Add("Cash till");

                                dt.Columns.Add("Document type");
                                dt.Columns.Add("Document number");
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                            {
                                //Not allowed.
                            }
                        }
                        else
                        {
                            if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Income_Item")
                            {

                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Outcome_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Returned_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Exchange_Item")
                            {
                                //Not allowed.
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Expand_Item")
                            {
                                dt.Columns.Add("Document type");
                                dt.Columns.Add("Document number");
                            }
                            else if (Type_comboBox.SelectedValue.ToString() == "Type_ComboBox_Move_Item")
                            {
                                //Not allowed.
                            }
                        }
                    }

                    System.Data.DataRow newRow = dt.NewRow();
                    dt.Rows.Add(newRow);

                    myDataGrid.ItemsSource = null;
                    myDataGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }
            }
            else
            {
                if (Type_comboBox.SelectedIndex != -1)
                {
                    dt.Columns.Add("Action ID");
                    dt.Columns.Add("Attachments pathes");

                    System.Data.DataRow newRow = dt.NewRow();
                    dt.Rows.Add(newRow);

                    myDataGrid.ItemsSource = null;
                    myDataGrid.ItemsSource = dt.DefaultView;

                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }
            }



            All_textBlock.Text = "0";
            Error_textBlock.Text = "0";

        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Create_Columns();
                InitializeCopyPaste();
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

        private async void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (!MultiAttachments)
                {
                    if (Login.MultiAction_AnyError)
                    {
                        MessageBox.Show("Please make sure there are no errors in the entered data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                    {
                        if (Login.MultiAction_List.Count > 0)
                        {
                            if (Type_comboBox.SelectedIndex == -1)
                            {
                                MessageBox.Show("Please select the operation type.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                                return;
                            }


                            a.Insert_Multi_Actions();

                            MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                            Create_Columns();

                            InitializeCopyPaste();

                            if (TW != null)
                                TW.Load_Data();

                        }
                    }
                }
                else
                {
                    if (Login.MultiAttachment_AnyError)
                    {
                        MessageBox.Show("Please make sure there are no errors in the entered data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                    {
                        if (Type_comboBox.SelectedIndex == -1)
                        {
                            MessageBox.Show("Please select the operation type.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;
                        }

                        if (Login.MultiAttachment_Dictionary.Count > 0)
                        {

                            await Save_Attachments();


                            if (failurText != "")
                                MessageBox.Show(failurText, "", MessageBoxButton.OK, MessageBoxImage.Stop);

                            MessageBox.Show("Attachments:" + Environment.NewLine + "Succeeded - " + success.ToString() + Environment.NewLine + "Failed - " + (totalFiles - success).ToString(), "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        }

                        Create_Columns();

                        InitializeCopyPaste();

                        if (TW != null)
                            TW.Load_Data();

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

        private async Task Save_Attachments()
        {
            totalFiles = Login.MultiAttachment_Dictionary.Values.Sum(data => data.Paths.Count);
            circularProgressBar.Progress = 0;
            Percentage_label.Content = "0%";
            //circularProgressBar.SegmentCount = totalFiles;
            circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Visible;

            int currentFile = 0;


            foreach (var entry in Login.MultiAttachment_Dictionary)
            {
                int Action_ID = entry.Key;
                int Is_Approved = entry.Value.IsApproved;

                List<string> paths = entry.Value.Paths;

                string[] FullAttachNames = paths.ToArray();
                //totalFiles += FullAttachNames.Length;


                foreach (var FullAttachName in FullAttachNames)
                {
                    string t;
                    d.GetNameFromPath(FullAttachName, out t, out _);
                    string fileName = "";

                    if (Action_ID != -1)
                    {
                        fileName = "Action_id_" + Action_ID.ToString() + "_" + t;
                    }
                    /*
                    else if (Beneficiary_ID != -1)
                    {
                        fileName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + t;
                    }
                    */
                    while (d.file_Exist(fileName))
                    {
                        t = "(1)" + t;
                        if (Action_ID != -1)
                        {
                            fileName = "Action_id_" + Action_ID.ToString() + "_" + t;
                        }
                        /*
                        else if (Beneficiary_ID != -1)
                        {
                            fileName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + t;
                        }
                        */
                    }

                    // Calculate the progress percentage
                    double progress = ((double)currentFile / totalFiles) * 100;

                    // Update the circular progress bar value
                    circularProgressBar.Progress = progress;
                    Percentage_label.Content = progress.ToString("N0") + "%";

                    bool uploadSuccess = await Task.Run(() => d.SaveDocument(FullAttachName, fileName));

                    if (uploadSuccess)
                    {
                        bool Approved_Action = false;

                        if (Is_Approved == 1)
                            Approved_Action = true;

                        d.AddAttachmentToDatabase(Action_ID, -1, t, Approved_Action, "AddAction_Window");
                        success++;
                    }
                    else
                    {
                        failurText = "Alert!!!!" + Environment.NewLine + "Not all files have been uploaded.";
                    }

                    currentFile++;
                }
            }

            circularProgressBar.Progress = 100;
            Percentage_label.Content = "100%";
            await Task.Delay(1000);
            circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Collapsed;
        }

        private void AccountType_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                Tilltype_comboBox.ItemsSource = null;
                Type_comboBox.ItemsSource = null;

                if (AccountType_comboBox.IsEnabled == true)
                {
                    Account_Type = "";
                    Category = "";
                    TillType = "";

                    if (AccountType_comboBox.SelectedValue.ToString() == "ComboBox_MicroProject_Item")
                    {
                        Account_Type = "Project";
                        Category = "Micro Projects";
                    }
                    else if (AccountType_comboBox.SelectedValue.ToString() == "ComboBox_Budget_Item")
                    {
                        Account_Type = "Project";
                        Category = "Budget";
                    }
                    else if (AccountType_comboBox.SelectedValue.ToString() == "ComboBox_Exchange_Item")
                    {
                        Account_Type = "Exchange";
                    }
                    else if (AccountType_comboBox.SelectedValue.ToString() == "ComboBox_Personal_Item")
                    {
                        Account_Type = "Personal";
                    }
                }

                if (tills_dt != null && tills_dt.Rows.Count > 0)
                {
                    DataTable selected_dt = new DataTable();
                    System.Data.DataRow[] typesrows = null;

                    if (Account_Type == "Project")
                    {
                        typesrows = tills_dt.Select("Category_Name = '" + Category + "' AND Accounttype_Name = '" + Account_Type + "'");
                    }
                    else
                    {
                        typesrows = tills_dt.Select("Accounttype_Name = '" + Account_Type + "'");
                    }


                    if (typesrows.Length != 0)
                    {
                        selected_dt = typesrows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                        {
                            DataView view = new DataView(selected_dt);
                            DataTable selected_dt1 = new DataTable();
                            selected_dt1 = view.ToTable(true, "ID", "Name");

                            if (selected_dt1 != null && selected_dt1.Rows.Count > 0)
                            {
                                tilltypes_dt = selected_dt1;
                                Tilltype_comboBox.ItemsSource = tilltypes_dt.DefaultView;
                            }
                        }
                    }
                }

                Apply_Color_Theme();
                Apply_DataGrid_Color_Theme();

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

        private void Tilltype_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Tilltype_comboBox.IsEnabled)
            {
                int tilltype_id;
                TillType = "";

                tilltype_id = Convert.ToInt32(Tilltype_comboBox.SelectedValue);

                TillType = tilltypes_dt.AsEnumerable()
                   .FirstOrDefault(row => row.Field<int?>("ID")?.Equals(tilltype_id) ?? false)
                   ?.Field<string>("Name")
                   ?.ToString();


                Type_comboBox.SelectedIndex = -1;
                Set_privileges();
                Set_Available_Operations();
            }
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                Export_btn.CommandTarget = myDataGrid;
            }
        }

        private void clear_type_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Type_comboBox.SelectedIndex = -1;
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

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Error_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Error_textBlock.ActualWidth;
            else
                Error_textBlock.MinWidth = All_textBlock.ActualWidth;
        }

        private void CopyPasteHandler_PasteCompleted(object sender, EventArgs e)
        {
            if (!MultiAttachments)
            {
                // Now you can access the copyPasteHandler
                All_textBlock.Text = copyPasteHandler.TotalPastedRows.ToString("N0");
                Error_textBlock.Text = copyPasteHandler.TotalErrorRows.ToString("N0");
            }
            else
            {
                // Now you can access the copyPasteHandler
                All_textBlock.Text = Attachment_copyPasteHandler.TotalPastedRows.ToString("N0");
                Error_textBlock.Text = Attachment_copyPasteHandler.TotalErrorRows.ToString("N0");
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
    }
}
