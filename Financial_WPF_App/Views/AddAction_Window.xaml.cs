using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Financial_WPF_App.Views;
using Microsoft.Win32;
using Syncfusion.Data.Extensions;
using Syncfusion.Windows.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App
{
    /// <summary>
    /// Interaction logic for AddAction_Window.xaml
    /// </summary>
    public partial class AddAction_Window : Window
    {
        public AddAction_Window(int Account_ID, bool IsAdminAccount, string Account_Name, string Account_Type, string Category, int Till_ID, string TillType, int SubProject_ID, bool FromTillForm, Main_Window_2 main_Window, Till_Window TW)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.TW = TW;
            this.Action_ID = -1;
            this.Account_ID = Account_ID;
            this.Account_Name = Account_Name;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.Till_ID = Till_ID;
            this.TillType = TillType;
            this.FromTillForm = FromTillForm;
            this.SubProject_ID = SubProject_ID;
            this.IsAdminAccount = IsAdminAccount;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public AddAction_Window(int Account_ID, bool IsAdminAccount, string Account_Name, string Account_Type, string Category, int Till_ID, string TillType, int Action_ID, int SubProject_ID, bool FromTillForm, bool Approved_Action, Main_Window_2 main_Window, Till_Window TW)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.TW = TW;
            this.Action_ID = Action_ID;
            this.Account_ID = Account_ID;
            this.Account_Name = Account_Name;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.Till_ID = Till_ID;
            this.TillType = TillType;
            this.SubProject_ID = SubProject_ID;
            this.FromTillForm = FromTillForm;
            this.Approved_Action = Approved_Action;
            this.IsAdminAccount = IsAdminAccount;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        //ترحيل قيود
        public AddAction_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.TW = null;
            this.Action_ID = -1;
            this.FromTillForm = true;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }


        int Action_ID, Account_ID, Till_ID, SubProject_ID;
        string Account_Name; bool FromTillForm;
        string Account_Type; string Category;
        Till t;
        Models.Action a;
        private Account Acc;
        private Beneficiary b;
        private Rate r;
        private Budget_Lines bl;
        private Employee Emp;
        bool IsAdminAccount;

        Double expandature, USD_Value;
        string TillType;
        private DataTable tills_dt, rate_dt, b_dt, t_dt, ExAcc_dt, Accounts_dt, Lines_dt, cashAccounts_dt, Donors_dt, Employee_dt;
        bool UserMode;
        Main_Window_2 main_Window;
        Till_Window TW;
        string Selected_budgetID;

        private Document d;
        private OpenFileDialog open;
        private string[] FullAttachNames;
        private Thread myTh;

        DataTable dt = new DataTable();

        bool PassException = false;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];


        #region privileges
        bool Income_privileges, Outcome_privileges, Returned_privileges,
            Exchange_privileges, ExchangeToForeign_privileges,
            Expand_privileges, Move_privileges;


        bool Approved_Action = false;

        #endregion

        private bool OnLoad()
        {
            try
            {
                b = new Beneficiary();
                t = new Till();
                a = new Models.Action();
                Acc = new Account();
                r = new Rate();
                bl = new Budget_Lines();
                d = new Document();
                Emp = new Employee();

                Rate_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
                Amount_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
                Set_privileges();


                Fill_Main_Comboboxs();
                if (!Account_Type.IsNullOrWhiteSpace() && !Account_Type.GetIsDynamicBound())
                {
                    if (Account_Type == "Project")
                    {
                        if (SubProject_ID > 0)
                        {
                            Subproject_comboBox.IsEnabled = false;
                            Subproject_comboBox.SelectedValue = SubProject_ID;
                        }
                    }
                    else
                    {
                        Subproject_comboBox.IsEnabled = false;
      
                        Subproject_comboBox_SelectionChanged(Subproject_comboBox, null);
                    }
                }

                if (Account_ID > 0)
                {
                    Account_comboBox.IsEnabled = false;
                    Account_comboBox.SelectedValue = Account_ID;

                }
                if (Till_ID > 0)
                {
                    Till_comboBox.IsEnabled = false;
                    Till_comboBox.SelectedValue = Till_ID;
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

        private void Set_privileges()
        {
            Income_privileges = Outcome_privileges = Returned_privileges = Exchange_privileges = ExchangeToForeign_privileges
                = Expand_privileges = Move_privileges = false;

            if(TillType != null)
            {
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

        }

        private void Load_Form()
        {
            //AccountName_lable.Content = Account_Name;
            //TillName_lable.Content = TillType;

            Show_Hide_Components();

            Set_Available_Operations();

            ActionDate_datepicker.SelectedDate = DateTime.Today;

            //UPDATE MODE//
            if (Action_ID > 0)
            {
                PassException = true;

                if (Approved_Action)
                {
                    dt = a.Select(Action_ID.ToString(), "", SubProject_ID.ToString(), true);

                }
                else
                {
                    dt = a.Select_action_not_approved(Action_ID.ToString(), "", SubProject_ID.ToString(), true, false);
                }
                Fill_Action();
                Type_comboBox.IsEnabled = false;
                DestinationTill_comboBox.IsEnabled = false;
                ExchangeAcc_comboBox.IsEnabled = false;
                DestinationTill_clear_btn.Visibility = ExchangeAcc_clear_btn.Visibility = Visibility.Collapsed;

                if (Type_ComboBox_Exchange_Item.IsSelected)
                {
                    CashAccount_comboBox.IsEnabled = false;
                    ObserveCashAccount_comboBox.IsEnabled = false;
                    CashAccount_clear_btn.Visibility = ObserveCashAccount_clear_btn.Visibility = Visibility.Collapsed;
                }
                PassException = false;
            }
            else
                UserMode = true;

        }

        private void Set_Available_Operations()
        {
            Type_ComboBox_Income_Item.Visibility = Visibility.Visible;
            Type_ComboBox_Outcome_Item.Visibility = Visibility.Visible;
            Type_ComboBox_Returned_Item.Visibility = Visibility.Visible;
            Type_ComboBox_Exchange_Item.Visibility = Visibility.Visible;
            Type_ComboBox_Expand_Item.Visibility = Visibility.Visible;
            Type_ComboBox_Move_Item.Visibility = Visibility.Visible;

            if (Account_Type == "Project" && TillType != null)
            {
                if (TillType.ToLower() == "syr")
                {
                    Type_ComboBox_Income_Item.Visibility = Visibility.Collapsed;

                    if (!Outcome_privileges)    
                        Type_ComboBox_Outcome_Item.Visibility = Visibility.Collapsed;

                    if (!Returned_privileges)
                        Type_ComboBox_Returned_Item.Visibility = Visibility.Collapsed;

                    // تم إيقاف هذه العملية لحين دراستها
                    //if (Exchange_privileges)
                    //    Type_ComboBox_Exchange_Item.Visibility = Visibility.Visible;
                    //else
                    //    Type_ComboBox_Exchange_Item.Visibility = Visibility.Collapsed;
                    Type_ComboBox_Exchange_Item.Visibility = Visibility.Collapsed;

                    if (!Expand_privileges)
                        Type_ComboBox_Expand_Item.Visibility = Visibility.Collapsed;


                    // تم إيقاف هذه العملية لحين دراستها
                    //if (Move_privileges)
                    //    Type_ComboBox_Move_Item.Visibility = Visibility.Visible;
                    //else
                    //    Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;

                    Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (!Income_privileges)
                        Type_ComboBox_Income_Item.Visibility = Visibility.Collapsed;

                    Type_ComboBox_Outcome_Item.Visibility = Visibility.Collapsed;

                    Type_ComboBox_Returned_Item.Visibility = Visibility.Collapsed;

                    if (!Exchange_privileges)
                        Type_ComboBox_Exchange_Item.Visibility = Visibility.Collapsed;

                    Type_ComboBox_Expand_Item.Visibility = Visibility.Collapsed;

                    // تم إيقاف هذه العملية لحين دراستها
                    //if (Move_privileges)
                    //    Type_ComboBox_Move_Item.Visibility = Visibility.Visible;
                    //else
                    //

                    Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;
                }
            }
            else if (Account_Type == "Exchange")
            {
                Type_ComboBox_Income_Item.Visibility = Visibility.Collapsed;

                Type_ComboBox_Outcome_Item.Visibility = Visibility.Collapsed;

                Type_ComboBox_Returned_Item.Visibility = Visibility.Collapsed;

                if (!Exchange_privileges)
                    Type_ComboBox_Exchange_Item.Visibility = Visibility.Collapsed;

                Type_ComboBox_Expand_Item.Visibility = Visibility.Collapsed;

                Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;
            }
            else if (Account_Type == "Personal")
            {
                if (!Income_privileges)
                    Type_ComboBox_Income_Item.Visibility = Visibility.Collapsed;

                Type_ComboBox_Outcome_Item.Visibility = Visibility.Collapsed;

                Type_ComboBox_Returned_Item.Visibility = Visibility.Collapsed;

                Type_ComboBox_Exchange_Item.Visibility = Visibility.Collapsed;

                if (!Expand_privileges)
                    Type_ComboBox_Expand_Item.Visibility = Visibility.Collapsed;

                Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;
            }
            else
            {
                Type_ComboBox_Income_Item.Visibility = Visibility.Collapsed;
                Type_ComboBox_Outcome_Item.Visibility = Visibility.Collapsed;
                Type_ComboBox_Returned_Item.Visibility = Visibility.Collapsed;
                Type_ComboBox_Exchange_Item.Visibility = Visibility.Collapsed;
                Type_ComboBox_Expand_Item.Visibility = Visibility.Collapsed;
                Type_ComboBox_Move_Item.Visibility = Visibility.Collapsed;
            }
        }

        private void Fill_Main_Comboboxs()
        {
            tills_dt = null;
            Subproject_comboBox.ItemsSource = null;
            Account_comboBox.ItemsSource = null;
            Till_comboBox.ItemsSource = null;

            DataTable temp_tills_dt = t.Select_All_tills();

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
                        PersonalAccounts_filterExpression += " AND Tilltypes_Name = 'SYR'";

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
                        Accounts_filterExpression += " AND Tilltypes_Name = 'SYR'";


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
                        PersonalAccounts_filterExpression += " AND Tilltypes_Name = 'SYR'";

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
                        Fiscalbonds_filterExpression += " AND Tilltypes_Name = 'SYR'";


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

            if (tills_dt != null && tills_dt.Rows.Count > 0)
            {
                DataView view = new DataView(tills_dt);
                DataTable selected_dt = new DataTable();
                selected_dt = view.ToTable(true, "Subproject_ID", "Subproject_Name");

                if (selected_dt != null && selected_dt.Rows.Count > 0)
                    Subproject_comboBox.ItemsSource = selected_dt.DefaultView;
            }

        }


        // تمت كتابة كود الاظهار والاخفاء بطريقة مكرر وغبية عن قصد
        // وذلك لامكانية اضافة او تعديل شروط الاظهار والاخفاء لحلات استثنائية حسب تعديلا ت البرنامج 
        // لذلك لم يتم ربط اي حالتين متشابهتين بل تكرراهما
        //
        //
        // يعتمد الكود على العناصر الظاهرة والمخفية في تعبئة بيانات القوائم المنسدلة كما في التحقق من البيانات المدخلة وتخزينها قبل حفظها في قاعدة البيانات
        // لذلك يجب البدء باي تعديل يحصل من هذه النقطة
        private void Show_Hide_Components()
        {
            if (Action_ID == -1)
                Clear_Boxes();

            Amount_txtBox.Visibility =
                ActionDate_Label.Visibility = ActionDate_datepicker.Visibility =

                Notes_Label.Visibility = Notes_txtBox.Visibility = E_Notes_Label.Visibility = E_Notes_txtBox.Visibility =

                Add_Attachment_btn.Visibility = AddedAttachments_panel.Visibility =

                Exchange_Note_lable.Visibility =

                DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility = DestinationTill_help.Visibility =

                ExchangeAcc_lable.Visibility = ExchangeAcc_comboBox.Visibility = ExchangeAcc_clear_btn.Visibility = ExchangeAcc_help.Visibility =

                ExchangeCash_lable.Visibility = ExchangeCash_comboBox.Visibility = ExchangeCash_clear_btn.Visibility =

                MircoProject_ID_lable.Visibility = MircoProject_ID_autoSuggestionUserControl.Visibility =

                BeneficiaryName_lable.Visibility = BeneficiaryName_txtBox.Visibility =

                Donor_lable.Visibility = Donor_comboBox.Visibility = Donor_comboBox_clear_btn.Visibility =

                Budget_lable.Visibility = Budget_comboBox.Visibility = Budget_comboBox_clear.Visibility = Budget_help.Visibility =

                Category_lable.Visibility = Category_comboBox.Visibility = Category_comboBox_clear_btn.Visibility =

                Line_lable.Visibility = Line_comboBox.Visibility = Line_comboBox_clear_btn.Visibility =

                CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                ObserveCashAccount_lable.Visibility = ObserveCashAccount_comboBox.Visibility = ObserveCashAccount_clear_btn.Visibility =

                IsReceipt_comboBox.Visibility = ReceiptNum_txtBox.Visibility =

                Employee_lable.Visibility = Employee_comboBox.Visibility = Employee_comboBox_clear.Visibility = Employee_help.Visibility =

                Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility =

                Rate_lable.Visibility = Rate_txtBox.Visibility =

                Exchanged_lable.Visibility = Exchanged_txtBox.Visibility =

                Visibility.Collapsed;


            payslip_ComboBoxItem.Visibility = Visibility.Collapsed;  //IsReceipt_comboBox Item

            if (Type_comboBox.SelectedIndex != -1)
            {
                Amount_txtBox.Visibility = Visibility.Visible;
                Notes_Label.Visibility = Notes_txtBox.Visibility = E_Notes_Label.Visibility = E_Notes_txtBox.Visibility = Visibility.Visible;
                Add_Attachment_btn.Visibility = Visibility.Visible;
                if (FromTillForm)
                {
                    ActionDate_Label.Visibility = ActionDate_datepicker.Visibility = Visibility.Visible;
                    ActionDate_datepicker.IsEnabled = true;
                }
                else
                {
                    if (Action_ID != -1)
                    {
                        ActionDate_Label.Visibility = ActionDate_datepicker.Visibility = Visibility.Visible;
                        ActionDate_datepicker.IsEnabled = true;
                    }
                    else
                    {
                        ActionDate_Label.Visibility = ActionDate_datepicker.Visibility = Visibility.Visible;
                        ActionDate_datepicker.IsEnabled = false;
                    }
                }

                if (Account_Type == "Project")
                {
                    if (Category == "Micro Projects")
                    {
                        if (TillType.ToLower() == "syr")
                        {
                            if (Type_ComboBox_Income_Item.IsSelected)
                            {
                                //Not allowed.
                            }
                            else if (Type_ComboBox_Outcome_Item.IsSelected)
                            {
                                MircoProject_ID_lable.Visibility = MircoProject_ID_autoSuggestionUserControl.Visibility =

                                    BeneficiaryName_lable.Visibility = BeneficiaryName_txtBox.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Returned_Item.IsSelected)
                            {
                                MircoProject_ID_lable.Visibility = MircoProject_ID_autoSuggestionUserControl.Visibility =

                                    BeneficiaryName_lable.Visibility = BeneficiaryName_txtBox.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Exchange_Item.IsSelected)
                            {
                                Exchange_Note_lable.Visibility =

                                    DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility =

                                    ExchangeAcc_lable.Visibility = ExchangeAcc_comboBox.Visibility = ExchangeAcc_clear_btn.Visibility = ExchangeAcc_help.Visibility =

                                    ExchangeCash_lable.Visibility = ExchangeCash_comboBox.Visibility = ExchangeCash_clear_btn.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility =

                                    Rate_lable.Visibility = Rate_txtBox.Visibility =

                                    Exchanged_lable.Visibility = Exchanged_txtBox.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Expand_Item.IsSelected)
                            {
                                MircoProject_ID_lable.Visibility = MircoProject_ID_autoSuggestionUserControl.Visibility =

                                    BeneficiaryName_lable.Visibility = BeneficiaryName_txtBox.Visibility =

                                    IsReceipt_comboBox.Visibility = ReceiptNum_txtBox.Visibility =

                                    CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Move_Item.IsSelected)
                            {
                                DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility = DestinationTill_help.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                                    ObserveCashAccount_lable.Visibility = ObserveCashAccount_comboBox.Visibility = ObserveCashAccount_clear_btn.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            if (Type_ComboBox_Income_Item.IsSelected)
                            {
                                Donor_lable.Visibility = Donor_comboBox.Visibility = Donor_comboBox_clear_btn.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Outcome_Item.IsSelected)
                            {
                                //Not allowed.
                            }
                            else if (Type_ComboBox_Returned_Item.IsSelected)
                            {
                                //Not allowed.
                            }
                            else if (Type_ComboBox_Exchange_Item.IsSelected)
                            {
                                Exchange_Note_lable.Visibility =

                                    DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility =

                                    ExchangeAcc_lable.Visibility = ExchangeAcc_comboBox.Visibility = ExchangeAcc_clear_btn.Visibility = ExchangeAcc_help.Visibility =

                                    Donor_lable.Visibility = Donor_comboBox.Visibility = Donor_comboBox_clear_btn.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility =

                                    Rate_lable.Visibility = Rate_txtBox.Visibility =

                                    Exchanged_lable.Visibility = Exchanged_txtBox.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Expand_Item.IsSelected)
                            {
                                //Not allowed.
                            }
                            else if (Type_ComboBox_Move_Item.IsSelected)
                            {
                                DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility = DestinationTill_help.Visibility =
                                Donor_lable.Visibility = Donor_comboBox.Visibility = Donor_comboBox_clear_btn.Visibility =

                                Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =
                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Visible;
                            }
                        }
                    }
                    else if (Category == "Budget")
                    {
                        if (TillType.ToLower() == "syr")
                        {
                            if (Type_ComboBox_Income_Item.IsSelected)
                            {
                                //Not allowed.
                            }
                            else if (Type_ComboBox_Outcome_Item.IsSelected)
                            {
                                Budget_lable.Visibility = Budget_comboBox.Visibility = Budget_comboBox_clear.Visibility = Budget_help.Visibility =

                                    Category_lable.Visibility = Category_comboBox.Visibility = Category_comboBox_clear_btn.Visibility =

                                    Line_lable.Visibility = Line_comboBox.Visibility = Line_comboBox_clear_btn.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Returned_Item.IsSelected)
                            {
                                Budget_lable.Visibility = Budget_comboBox.Visibility = Budget_comboBox_clear.Visibility = Budget_help.Visibility =

                                    Category_lable.Visibility = Category_comboBox.Visibility = Category_comboBox_clear_btn.Visibility =

                                    Line_lable.Visibility = Line_comboBox.Visibility = Line_comboBox_clear_btn.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Exchange_Item.IsSelected)
                            {
                                Exchange_Note_lable.Visibility =

                                    DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility =

                                    ExchangeAcc_lable.Visibility = ExchangeAcc_comboBox.Visibility = ExchangeAcc_clear_btn.Visibility = ExchangeAcc_help.Visibility =

                                    ExchangeCash_lable.Visibility = ExchangeCash_comboBox.Visibility = ExchangeCash_clear_btn.Visibility =

                                    Budget_lable.Visibility = Budget_comboBox.Visibility = Budget_comboBox_clear.Visibility = Budget_help.Visibility =

                                    CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                                    Category_lable.Visibility = Category_comboBox.Visibility = Category_comboBox_clear_btn.Visibility =

                                    Line_lable.Visibility = Line_comboBox.Visibility = Line_comboBox_clear_btn.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility =

                                    Rate_lable.Visibility = Rate_txtBox.Visibility =

                                    Exchanged_lable.Visibility = Exchanged_txtBox.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Expand_Item.IsSelected)
                            {
                                Budget_lable.Visibility = Budget_comboBox.Visibility = Budget_comboBox_clear.Visibility = Budget_help.Visibility =

                                    Category_lable.Visibility = Category_comboBox.Visibility = Category_comboBox_clear_btn.Visibility =

                                    Line_lable.Visibility = Line_comboBox.Visibility = Line_comboBox_clear_btn.Visibility =

                                    CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                                    IsReceipt_comboBox.Visibility = ReceiptNum_txtBox.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Visible;


                                payslip_ComboBoxItem.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Move_Item.IsSelected)
                            {
                                DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility = DestinationTill_help.Visibility =

                                    Budget_lable.Visibility = Budget_comboBox.Visibility = Budget_comboBox_clear.Visibility = Budget_help.Visibility =

                                    Category_lable.Visibility = Category_comboBox.Visibility = Category_comboBox_clear_btn.Visibility =

                                    CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                                    ObserveCashAccount_lable.Visibility = ObserveCashAccount_comboBox.Visibility = ObserveCashAccount_clear_btn.Visibility =

                                    Line_lable.Visibility = Line_comboBox.Visibility = Line_comboBox_clear_btn.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            if (Type_ComboBox_Income_Item.IsSelected)
                            {
                                Donor_lable.Visibility = Donor_comboBox.Visibility = Donor_comboBox_clear_btn.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Outcome_Item.IsSelected)
                            {
                                //Not allowed.
                            }
                            else if (Type_ComboBox_Returned_Item.IsSelected)
                            {
                                //Not allowed.
                            }
                            else if (Type_ComboBox_Exchange_Item.IsSelected)
                            {
                                Exchange_Note_lable.Visibility =

                                    DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility =

                                    ExchangeAcc_lable.Visibility = ExchangeAcc_comboBox.Visibility = ExchangeAcc_clear_btn.Visibility = ExchangeAcc_help.Visibility =

                                    Donor_lable.Visibility = Donor_comboBox.Visibility = Donor_comboBox_clear_btn.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility =

                                    Rate_lable.Visibility = Rate_txtBox.Visibility =

                                    Exchanged_lable.Visibility = Exchanged_txtBox.Visibility = Visibility.Visible;
                            }
                            else if (Type_ComboBox_Expand_Item.IsSelected)
                            {
                                //Not allowed.
                            }
                            else if (Type_ComboBox_Move_Item.IsSelected)
                            {
                                DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility = DestinationTill_help.Visibility =
                                Donor_lable.Visibility = Donor_comboBox.Visibility = Donor_comboBox_clear_btn.Visibility =

                                    Original_AvgRate_lable.Visibility = Original_AvgRate_comboBox.Visibility = Original_AvgRate_clear_btn.Visibility = Original_AvgRate_help.Visibility =

                                    USD_Value_lable.Visibility = USD_Value_TxtBox.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
                else if (Account_Type == "Exchange")
                {
                    if (TillType.ToLower() == "syr")
                    {
                        if (Type_ComboBox_Income_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Outcome_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Returned_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Exchange_Item.IsSelected)
                        {
                            DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility =

                                CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                                Rate_lable.Visibility = Rate_txtBox.Visibility =

                                Exchanged_lable.Visibility = Exchanged_txtBox.Visibility = Visibility.Visible;
                        }
                        else if (Type_ComboBox_Expand_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Move_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                    }
                    else
                    {
                        if (Type_ComboBox_Income_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Outcome_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Returned_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Exchange_Item.IsSelected)
                        {
                            DestinationTill_lable.Visibility = DestinationTill_comboBox.Visibility = DestinationTill_clear_btn.Visibility =

                                Rate_lable.Visibility = Rate_txtBox.Visibility =

                                Exchanged_lable.Visibility = Exchanged_txtBox.Visibility = Visibility.Visible;
                        }
                        else if (Type_ComboBox_Expand_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Move_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                    }
                }
                else if (Account_Type == "Personal")
                {
                    if (TillType.ToLower() == "syr")
                    {
                        if (Type_ComboBox_Income_Item.IsSelected)
                        {
                            CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility = Visibility.Visible;
                        }
                        else if (Type_ComboBox_Outcome_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Returned_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Exchange_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Expand_Item.IsSelected)
                        {
                            CashAccount_lable.Visibility = CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility =

                                    IsReceipt_comboBox.Visibility = ReceiptNum_txtBox.Visibility = Visibility.Visible;
                        }
                        else if (Type_ComboBox_Move_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                    }
                    else
                    {
                        if (Type_ComboBox_Income_Item.IsSelected)
                        {

                        }
                        else if (Type_ComboBox_Outcome_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Returned_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Exchange_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                        else if (Type_ComboBox_Expand_Item.IsSelected)
                        {
                            IsReceipt_comboBox.Visibility = ReceiptNum_txtBox.Visibility = Visibility.Visible;

                        }
                        else if (Type_ComboBox_Move_Item.IsSelected)
                        {
                            //Not allowed.
                        }
                    }
                }
            }
        }

        private void Clear_Boxes()
        {
            Amount_txtBox.Text = Notes_txtBox.Text = E_Notes_txtBox.Text =
                BeneficiaryName_txtBox.autoTextBox.Text = ReceiptNum_txtBox.Text =
                USD_Value_TxtBox.Text = Rate_txtBox.Text = Exchanged_txtBox.Text =
                MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = "";
            BeneficiaryName_txtBox.CloseAutoSuggestionBox();


            DestinationTill_comboBox.SelectedIndex = ExchangeAcc_comboBox.SelectedIndex = Donor_comboBox.SelectedIndex = Budget_comboBox.SelectedIndex =
                Category_comboBox.SelectedIndex = Line_comboBox.SelectedIndex = CashAccount_comboBox.SelectedIndex = ObserveCashAccount_comboBox.SelectedIndex =
                Original_AvgRate_comboBox.SelectedIndex = IsReceipt_comboBox.SelectedIndex = Employee_comboBox.SelectedIndex = -1;


            ActionDate_datepicker.SelectedDate = DateTime.Today;

            FullAttachNames = new string[] { };
            AddedAttachments_panel.Visibility = Visibility.Collapsed;

        }

        private void Avgrate_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Original_AvgRate_comboBox.SelectedIndex = -1;
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

        private void Line_comboBox_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Line_comboBox.SelectedIndex = -1;
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

        private void ExchangeTill_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExchangeAcc_comboBox.SelectedIndex = -1;
                ExchangeCash_comboBox.SelectedIndex = -1;
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

        private void DestinationTill_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DestinationTill_comboBox.SelectedIndex = -1;
                ExchangeAcc_comboBox.ItemsSource = null;
                CashAccount_comboBox.SelectedIndex = -1;
                ObserveCashAccount_comboBox.ItemsSource = null;

                if (Category == "Budget" && TillType.ToLower() != "syr")
                {
                    if (Type_ComboBox_Exchange_Item.IsSelected)

                    {
                        CashAccount_lable.Visibility = Visibility.Collapsed;
                        CashAccount_comboBox.Visibility = CashAccount_clear_btn.Visibility = Visibility.Collapsed;

                        ObserveCashAccount_lable.Visibility = Visibility.Collapsed;
                        ObserveCashAccount_comboBox.Visibility = ObserveCashAccount_clear_btn.Visibility = Visibility.Collapsed;
                    }
                }
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

        private void Budget_comboBox_clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Budget_comboBox.SelectedIndex = -1;
                Category_comboBox.ItemsSource = null;
                Line_comboBox.ItemsSource = null;
                Original_AvgRate_comboBox.ItemsSource = null;
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

        private void Category_comboBox_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Category_comboBox.SelectedIndex = -1;
                Line_comboBox.ItemsSource = null;
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

        private void ActionDate_datepicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (UserMode)
                {
                    Original_AvgRate_comboBox.ItemsSource = null;
                    Employee_comboBox.ItemsSource = null;

                    if (Original_AvgRate_comboBox.Visibility == Visibility.Visible)
                    {
                        if (rate_dt != null && rate_dt.Rows.Count > 0)
                        {
                            string Date = ((DateTime)ActionDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                            DataRow[] rows = null;

                            if (Budget_comboBox.Visibility == Visibility.Visible)
                            {
                                if (Budget_comboBox.SelectedIndex != -1)
                                {
                                    DataRow[] budgetrow = Lines_dt.Select("budget_ID = '" + Selected_budgetID + "'");

                                    int BudgetType_ID = Convert.ToInt32(budgetrow[0]["BudgetType_ID"].ToString());

                                    if (Action_ID > 0 && dt.Rows[0]["Rate_ID"] != DBNull.Value)
                                    {
                                        int Current_RateID = dt.Rows[0].Field<Int32>("Rate_ID");
                                        rows = rate_dt.Select("((BeginDate <= '" + Date + "' and EndDate >= '" + Date + "') or Expenditures < Exchanged_Value or ID = '" + Current_RateID + "') and fromtp_id = '" + BudgetType_ID + "'");
                                    }
                                    else
                                    {
                                        rows = rate_dt.Select("((BeginDate <= '" + Date + "' and EndDate >= '" + Date + "') or Expenditures < Exchanged_Value) and fromtp_id = '" + BudgetType_ID + "'");
                                    }
                                }
                            }
                            else
                            {
                                if (Action_ID > 0 && dt.Rows[0]["Rate_ID"] != DBNull.Value)
                                {
                                    int Current_RateID = dt.Rows[0].Field<Int32>("Rate_ID");
                                    if (rate_dt != null)
                                        rows = rate_dt.Select("(BeginDate <= '" + Date + "' and EndDate >= '" + Date + "') or Expenditures < Exchanged_Value or ID = '" + Current_RateID + "'");
                                }
                                else
                                {
                                    if (rate_dt != null)
                                        rows = rate_dt.Select("(BeginDate <= '" + Date + "' and EndDate >= '" + Date + "') or Expenditures < Exchanged_Value");
                                }
                            }

                            DataTable selected_dt = new DataTable();
                            if (rows != null && rows.Length != 0)
                            {
                                selected_dt = rows.CopyToDataTable();
                                if (selected_dt != null && selected_dt.Rows.Count > 0)
                                    Original_AvgRate_comboBox.ItemsSource = selected_dt.DefaultView;
                            }
                            else
                                USD_Value_TxtBox.Text = "";
                        }
                    }
                    if (Employee_comboBox.Visibility == Visibility.Visible)
                    {
                        if (Employee_dt != null && Employee_dt.Rows.Count > 0)
                        {
                            string Date = ((DateTime)ActionDate_datepicker.SelectedDate).ToString("yyyy/MM");

                            var filteredRows = Employee_dt.AsEnumerable()
                                   .Where(row => ((DateTime)row["Month"]).ToString("yyyy/MM") == Date);

                            DataTable filteredTable = filteredRows.Any() ? filteredRows.CopyToDataTable() : Employee_dt.Clone();

                            Employee_comboBox.ItemsSource = filteredTable.DefaultView;

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
                if (PassException)
                    throw;
            }
        }

        private void Fill_ComboBoxes()
        {
            if (Original_AvgRate_comboBox.Visibility == Visibility.Visible)
            {
                Original_AvgRate_comboBox.ItemsSource = null;
                rate_dt = r.Select_AllAvilableRates(Account_ID.ToString(), TillType, true);
                if (rate_dt != null && rate_dt.Rows.Count > 0)
                {
                    string Date = ((DateTime)ActionDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                    DataRow[] rows;
                    if (Action_ID > 0 && dt.Rows[0]["Rate_ID"] != DBNull.Value)
                    {
                        int Current_RateID = dt.Rows[0].Field<Int32>("Rate_ID");
                        rows = rate_dt.Select("(BeginDate <= '" + Date + "' and EndDate >= '" + Date + "') or Expenditures < Exchanged_Value or ID = '" + Current_RateID + "'");
                    }
                    else
                    {
                        rows = rate_dt.Select("(BeginDate <= '" + Date + "' and EndDate >= '" + Date + "') or Expenditures < Exchanged_Value");
                    }
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                            Original_AvgRate_comboBox.ItemsSource = selected_dt.DefaultView;
                    }
                    else
                        USD_Value_TxtBox.Text = "";
                }

            }

            if (Budget_comboBox.Visibility == Visibility.Visible)
            {
                Budget_comboBox.ItemsSource = null;
                Category_comboBox.ItemsSource = null;
                Line_comboBox.ItemsSource = null;
                Original_AvgRate_comboBox.ItemsSource = null;

                Lines_dt = bl.Select_AllAvilableBudgetLines(Account_ID);
                if (Lines_dt != null && Lines_dt.Rows.Count > 0)
                {
                    DataView view = new DataView(Lines_dt);
                    DataTable selected_dt = new DataTable();
                    selected_dt = view.ToTable(true, "budget_ID", "budget_Name");

                    if (selected_dt != null && selected_dt.Rows.Count > 0)
                        Budget_comboBox.ItemsSource = selected_dt.DefaultView;
                }
            }

            CashAccount_comboBox.ItemsSource = null;
            ObserveCashAccount_comboBox.ItemsSource = null;
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
                        cashAccounts_dt = selected_dt;
                    }
                }
                else
                {
                    cashAccounts_dt = null;
                }
            }
            else
            {
                cashAccounts_dt = temp_dt;
            }

            if (CashAccount_comboBox.Visibility == Visibility.Visible)
            {
                DataRow[] rows = null;
                if ((Category == "Budget" || Category == "Micro Projects") && cashAccounts_dt != null)
                {
                    rows = cashAccounts_dt.Select("SubProject_ID like '" + SubProject_ID + ",%' or SubProject_ID like '%," + SubProject_ID + ",%' or SubProject_ID like '%," + SubProject_ID + "' or SubProject_ID like '" + SubProject_ID + "'");
                }
                else if (Account_Type == "Exchange" && cashAccounts_dt != null)
                {
                    rows = cashAccounts_dt.Select("ExchangeAccount_ID like '" + Account_ID + ",%' or ExchangeAccount_ID like '%," + Account_ID + ",%' or ExchangeAccount_ID like '%," + Account_ID + "' or ExchangeAccount_ID like '" + Account_ID + "'");
                }
                else if (Account_Type == "Personal" && cashAccounts_dt != null)
                {
                    rows = cashAccounts_dt.Select("PersonalAccount_ID like '" + Account_ID + ",%' or PersonalAccount_ID like '%," + Account_ID + ",%' or PersonalAccount_ID like '%," + Account_ID + "' or PersonalAccount_ID like '" + Account_ID + "'");
                }
                DataTable selected_dt = new DataTable();
                if (rows != null)
                {
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                            CashAccount_comboBox.ItemsSource = selected_dt.DefaultView;
                    }
                }
            }


            if (MircoProject_ID_autoSuggestionUserControl.Visibility == Visibility.Visible)
            {
                if (Login.BuildConnection())
                {
                    b_dt = b.Select("", Account_ID.ToString(), "");
                    List<string> IDItems = new List<string>();
                    List<string> Nametems = new List<string>();
                    if (b_dt != null && b_dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < b_dt.Rows.Count; i++)
                        {
                            IDItems.Add(b_dt.Rows[i].Field<Int32>("MicroProject_ID").ToString());
                            Nametems.Add(b_dt.Rows[i].Field<string>("Name").ToString());
                        }
                        MircoProject_ID_autoSuggestionUserControl.AutoSuggestionList = IDItems;
                        BeneficiaryName_txtBox.AutoSuggestionList = Nametems;
                    }
                }
            }

            if (DestinationTill_comboBox.Visibility == Visibility.Visible)
            {
                if (Type_ComboBox_Move_Item.IsSelected)
                {
                    DestinationTill_lable.Content = "Corresponding account:";
                    DestinationTill_help.Visibility = Visibility.Visible;
                    DestinationTill_comboBox.ItemsSource = null;


                    if (TillType.ToLower() == "syr")
                    {
                        DataTable temp_Accounts_dt = Acc.Select_Accounts_With_Same_TillType(TillType, Account_ID, -1);


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
                        if (Accounts_dt != null && Accounts_dt.Rows.Count > 0)
                        {
                            DestinationTill_comboBox.ItemsSource = Accounts_dt.DefaultView;
                        }
                    }
                    else
                    {
                        // تتم العملية بعد تحديد الممول
                    }
                }
                else
                {
                    DestinationTill_lable.Content = "Corresponding till:";
                    DestinationTill_help.Visibility = Visibility.Collapsed;
                    DestinationTill_comboBox.ItemsSource = null;
                    DataTable temp_dt1 = t.Select(Account_ID.ToString(), Till_ID, "");
                    if (TillType.ToLower() == "syr")
                    {
                        t_dt = temp_dt1;
                    }
                    else

                    {
                        if (ExchangeToForeign_privileges)
                        {
                            t_dt = temp_dt1;
                        }
                        else
                        {
                            System.Data.DataRow[] rows = temp_dt1.Select("Type = 'Syr'");
                            DataTable selected_dt = new DataTable();
                            if (rows.Length != 0)
                            {
                                selected_dt = rows.CopyToDataTable();
                                t_dt = selected_dt;
                            }
                        }
                    }

                    if (t_dt != null && t_dt.Rows.Count > 0)
                    {
                        DestinationTill_comboBox.ItemsSource = t_dt.DefaultView;
                    }
                }
            }

            ExchangeAcc_comboBox.ItemsSource = null;
            ExAcc_dt = Acc.Select_ExAcc_Tills(TillType, "");


            if (Donor_comboBox.Visibility == Visibility.Visible)
            {
                Donor_comboBox.ItemsSource = null;
                Donors_dt = Acc.Select_Account_Donors(Account_ID);
                if (Donors_dt != null && Donors_dt.Rows.Count > 0)
                {
                    Donor_comboBox.ItemsSource = Donors_dt.DefaultView;
                    if (Donors_dt.Rows.Count == 1)
                        Donor_comboBox.SelectedIndex = 1;
                }
            }


            Employee_comboBox.ItemsSource = null;
            Employee_dt = Emp.Select_Subproject_Monthly_Employee(SubProject_ID);

        }

        private void Fill_Action()
        {

            UserMode = false;


            if (dt.Rows[0]["Income"] != DBNull.Value)
                if (dt.Rows[0].Field<Double>("Income") != 0)
                {
                    Type_comboBox.SelectedIndex = 0; //وارد
                    //Amount_txtBox.Text = string.Format("{0:N2}", dt.Rows[0].Field<Double>("Income"));
                    Amount_txtBox.Text = dt.Rows[0].Field<Double>("Income").ToString();
                }

            if (dt.Rows[0]["Outcome"] != DBNull.Value)
                if (dt.Rows[0].Field<Double>("Outcome") != 0)
                {
                    Type_comboBox.SelectedIndex = 1; //سلفة
                    //Amount_txtBox.Text = string.Format("{0:N2}", dt.Rows[0].Field<Double>("Outcome"));
                    Amount_txtBox.Text = dt.Rows[0].Field<Double>("Outcome").ToString();

                }

            if (dt.Rows[0]["Returned"] != DBNull.Value)
                if (dt.Rows[0].Field<Double>("Returned") != 0)
                {
                    Type_comboBox.SelectedIndex = 2; //مرتجع
                    //Amount_txtBox.Text = string.Format("{0:N2}", dt.Rows[0].Field<Double>("Returned"));
                    Amount_txtBox.Text = dt.Rows[0].Field<Double>("Returned").ToString();


                }
            if (dt.Rows[0]["Exchange"] != DBNull.Value)
                if (dt.Rows[0].Field<Double>("Exchange") != 0)
                {
                    Type_comboBox.SelectedIndex = 3; //تصريف
                    //Amount_txtBox.Text = string.Format("{0:N2}", dt.Rows[0].Field<Double>("Exchange"));
                    Amount_txtBox.Text = dt.Rows[0].Field<Double>("Exchange").ToString();


                }
            if (dt.Rows[0]["Expand"] != DBNull.Value)
                if (dt.Rows[0].Field<Double>("Expand") != 0)
                {
                    if (dt.Rows[0]["ObserveAcion_ID"] != DBNull.Value)
                    {
                        Type_comboBox.SelectedIndex = 5; //نقل
                        //Amount_txtBox.Text = string.Format("{0:N2}", dt.Rows[0].Field<Double>("Expand"));
                        Amount_txtBox.Text = dt.Rows[0].Field<Double>("Expand").ToString();

                    }
                    else
                    {
                        Type_comboBox.SelectedIndex = 4; //فاتورة
                        //Amount_txtBox.Text = string.Format("{0:N2}", dt.Rows[0].Field<Double>("Expand"));
                        Amount_txtBox.Text = dt.Rows[0].Field<Double>("Expand").ToString();

                    }

                }
            UserMode = true;
            ActionDate_datepicker.SelectedDate = Convert.ToDateTime(dt.Rows[0].Field<DateTime>("ActionDate").ToString());
            UserMode = false;

            Notes_txtBox.Text = dt.Rows[0].Field<string>("Note").ToString();
            E_Notes_txtBox.Text = dt.Rows[0].Field<string>("E_Note").ToString();
            // ObserveTill_ID //
            if (dt.Rows[0]["ObserveTill_ID"] != DBNull.Value)
            {
                int a = Convert.ToInt32(dt.Rows[0].Field<int>("ObserveTill_ID"));
                DestinationTill_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("ObserveTill_ID"));
                DestinationTill_comboBox_SelectionChanged(DestinationTill_comboBox, null);
            }
            else DestinationTill_comboBox.SelectedIndex = -1;


            if (dt.Rows[0]["Donor_ID"] != DBNull.Value)
            {
                Donor_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("Donor_ID"));
            }
            else Donor_comboBox.SelectedIndex = -1;


            if (dt.Rows[0]["ExchangeAcc_ID"] != DBNull.Value)
            {
                ExchangeAcc_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("ExchangeAcc_ID"));
            }
            else ExchangeAcc_comboBox.SelectedIndex = -1;

            if (dt.Rows[0]["Exchange_Cash_ID"] != DBNull.Value)
            {
                ExchangeCash_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("Exchange_Cash_ID"));
            }
            else ExchangeCash_comboBox.SelectedIndex = -1;

            if (dt.Rows[0]["MicroProject_ID"] != DBNull.Value)
            {
                MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = dt.Rows[0].Field<int>("MicroProject_ID").ToString();
                //MircoProject_ID_autoSuggestionUserControl.IsEnabled = false;
            }
            else MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = "";

            //Don't show the popup list//
            MircoProject_ID_autoSuggestionUserControl.CloseAutoSuggestionBox();
            //fill beneficiary name manually//
            if (dt.Rows[0]["Name"] != DBNull.Value)
            {
                BeneficiaryName_txtBox.autoTextBox.Text = dt.Rows[0].Field<string>("Name").ToString();

                BeneficiaryName_txtBox.CloseAutoSuggestionBox();

            }

            if (dt.Rows[0]["Budget_ID"] != DBNull.Value)
            {
                Budget_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("Budget_ID"));
                Budget_comboBox_SelectionChanged(Budget_comboBox, null);
            }
            else Budget_comboBox.SelectedIndex = -1;

            if (dt.Rows[0]["LineCategory_ID"] != DBNull.Value)
            {
                Category_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("LineCategory_ID"));
                Category_comboBox_SelectionChanged(Category_comboBox, null);
            }
            else Category_comboBox.SelectedIndex = -1;

            if (dt.Rows[0]["BudgetLine_ID"] != DBNull.Value)
            {
                Line_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("BudgetLine_ID"));
            }
            else Line_comboBox.SelectedIndex = -1;


            if (dt.Rows[0]["CashAccount_ID"] != DBNull.Value)
            {
                CashAccount_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("CashAccount_ID"));
            }
            else CashAccount_comboBox.SelectedIndex = -1;

            if (dt.Rows[0]["ObserveCashAccount_ID"] != DBNull.Value)
            {
                ObserveCashAccount_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("ObserveCashAccount_ID"));
            }
            else ObserveCashAccount_comboBox.SelectedIndex = -1;


            if (dt.Rows[0]["IsReceipt"] != DBNull.Value)
            {
                if (dt.Rows[0].Field<string>("IsReceipt") == "invoice") IsReceipt_comboBox.SelectedIndex = 0;
                else if (dt.Rows[0].Field<string>("IsReceipt") == "receipt") IsReceipt_comboBox.SelectedIndex = 1;
                else if (dt.Rows[0].Field<string>("IsReceipt") == "payslip") IsReceipt_comboBox.SelectedIndex = 2;

                else IsReceipt_comboBox.SelectedIndex = -1;
            }

            if (dt.Rows[0]["Employee_Salary_ID"] != DBNull.Value)
            {
                Employee_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0].Field<int>("Employee_Salary_ID"));
            }
            else Employee_comboBox.SelectedIndex = -1;


            ReceiptNum_txtBox.Text = dt.Rows[0].Field<string>("ReceiptNote");


            if (dt.Rows[0]["AvgRateID"] != DBNull.Value)
            {
                Original_AvgRate_comboBox.SelectedValue = dt.Rows[0].Field<int>("AvgRateID");
            }


            if (dt.Rows[0]["Rate"] != DBNull.Value)
            {
                Rate_txtBox.Text = string.Format("{0:N9}", dt.Rows[0].Field<Double>("Rate"));
            }
            Calc_ExchangedValue();
            Calc_USDValue();
            UserMode = true;

        }

        private bool Check_AvgRate()
        {

            string Date = ((DateTime)ActionDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

            DataTable AvgRate_dt = r.Check_AvgRate_Availability(Account_ID.ToString(), Date, TillType, DestinationTill_comboBox.Text);
            if (AvgRate_dt.Rows.Count == 0)
            {
                return false;
            }
            else
                return true;
        }

        private void Original_AvgRate_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (UserMode)
                    Calc_USDValue();
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
                if (PassException)
                    throw;
            }
        }

        private void Amount_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                Calc_ExchangedValue();

                Calc_USDValue();


                string value = Amount_txtBox.Text;
                if (!String.IsNullOrEmpty(value) && !String.IsNullOrWhiteSpace(value))
                {
                    Amount_txtBox.Text = Login.ConvertToDouble(value);
                    /*
                    Calc_ExchangedValue();

                    Calc_USDValue();*/
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
                if (PassException)
                    throw;
            }

        }

        private void Budget_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Category_comboBox.ItemsSource = null;
                Line_comboBox.ItemsSource = null;
                Original_AvgRate_comboBox.ItemsSource = null;


                if (Budget_comboBox.SelectedValue != null && Budget_comboBox.SelectedIndex != -1)
                {
                    Selected_budgetID = Budget_comboBox.SelectedValue.ToString();

                    DataTable selected_dt = new DataTable();


                    DataRow[] Linesrows = Lines_dt.Select("budget_ID = '" + Selected_budgetID + "'");
                    if (Linesrows.Length != 0)
                    {
                        //DataTable selected_dt = new DataTable();
                        selected_dt = Linesrows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                        {
                            DataView view = new DataView(selected_dt);
                            DataTable selected_dt1 = new DataTable();
                            selected_dt1 = view.ToTable(true, "linecategory_ID", "linecategory_Name");

                            if (selected_dt1 != null && selected_dt1.Rows.Count > 0)
                                Category_comboBox.ItemsSource = selected_dt1.DefaultView;
                        }
                    }

                    if (Original_AvgRate_comboBox.Visibility == Visibility.Visible)
                    {
                        if (rate_dt != null && rate_dt.Rows.Count > 0)
                        {
                            string Date = ((DateTime)ActionDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                            DataRow[] budgetrow = Lines_dt.Select("budget_ID = '" + Selected_budgetID + "'");

                            int BudgetType_ID = Convert.ToInt32(budgetrow[0]["BudgetType_ID"].ToString());

                            DataRow[] rows;
                            if (Action_ID > 0 && dt.Rows[0]["Rate_ID"] != DBNull.Value)
                            {
                                int Current_RateID = dt.Rows[0].Field<Int32>("Rate_ID");
                                rows = rate_dt.Select("((BeginDate <= '" + Date + "' and EndDate >= '" + Date + "') or Expenditures < Exchanged_Value or ID = '" + Current_RateID + "') and fromtp_id = '" + BudgetType_ID + "'");
                            }
                            else
                            {
                                rows = rate_dt.Select("((BeginDate <= '" + Date + "' and EndDate >= '" + Date + "') or Expenditures < Exchanged_Value) and fromtp_id = '" + BudgetType_ID + "'");
                            }
                            if (rows.Length != 0)
                            {
                                selected_dt = rows.CopyToDataTable();
                                if (selected_dt != null && selected_dt.Rows.Count > 0)
                                    Original_AvgRate_comboBox.ItemsSource = selected_dt.DefaultView;
                            }
                            else
                                USD_Value_TxtBox.Text = "";
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
                if (PassException)
                    throw;
            }
        }

        private void AddedAttachments_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FullAttachNames = new string[] { };
                AddedAttachments_panel.Visibility = Visibility.Collapsed;
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

        private void ExchangeCash_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExchangeCash_comboBox.SelectedIndex = -1;
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

        private void ExchangeAcc_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ExchangeCash_comboBox.ItemsSource = null;
                if (ExchangeAcc_comboBox.SelectedValue != null && ExchangeAcc_comboBox.SelectedIndex != -1)
                {
                    int ExAcc_ID = Convert.ToInt32(ExchangeAcc_comboBox.SelectedValue);
                    DataRow[] rows = null;
                    if(cashAccounts_dt != null)
                    {
                        rows = cashAccounts_dt.Select("ExchangeAccount_ID like '" + ExAcc_ID + ",%' or ExchangeAccount_ID like '%," + ExAcc_ID + ",%' or ExchangeAccount_ID like '%," + ExAcc_ID + "' or ExchangeAccount_ID like '" + ExAcc_ID + "'");
                    }
                    DataTable selected_dt = new DataTable();
                    if (rows != null)
                    {
                        if (rows.Length != 0)
                        {
                            selected_dt = rows.CopyToDataTable();
                            if (selected_dt != null && selected_dt.Rows.Count > 0)
                                ExchangeCash_comboBox.ItemsSource = selected_dt.DefaultView;
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
                if (PassException)
                    throw;
            }
        }

        private void BeneficiaryName_autoSuggestionUserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BeneficiaryName_txtBox.autoTextBox.Text == "")
                    MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = "";
                else
                {
                    string Name = BeneficiaryName_txtBox.autoTextBox.Text;
                    DataRow[] rows = b_dt.Select("Name like '%" + Name + "%'");
                    if (rows.Length != 0)
                    {
                        MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = rows[0].Field<Int32>("MicroProject_ID").ToString();
                        MircoProject_ID_autoSuggestionUserControl.CloseAutoSuggestionBox();
                    }
                    else MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = "";
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
                if (PassException)
                    throw;
            }
        }

        private void CashAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CashAccount_comboBox.SelectedIndex = -1;
                ExchangeAcc_comboBox.ItemsSource = null;
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

        private void Donor_comboBox_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Donor_comboBox.SelectedIndex = -1;
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

        private void Employee_comboBox_clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Employee_comboBox.SelectedIndex = -1;
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

        private void Subproject_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Account_comboBox.ItemsSource = null;
                Till_comboBox.ItemsSource = null;
                Type_comboBox.SelectedIndex = -1;

                if (Subproject_comboBox.IsEnabled)
                {
                    SubProject_ID = -1;
                    Category = "";
                    Account_ID = -1;
                    Account_Name = "";
                    Account_Type = "";
                    IsAdminAccount = false;
                    Till_ID = -1;
                    TillType = "";

                    Clear_Boxes();
                    Show_Hide_Components();

                    SubProject_ID = Convert.ToInt32(Subproject_comboBox.SelectedValue);

                    Account_Type = tills_dt.AsEnumerable()
                                  .FirstOrDefault(row => row.Field<int?>("Subproject_ID")?.Equals(SubProject_ID) ?? false)
                                  ?.Field<string>("Accounttype_Name")
                                  ?.ToString();
                }


                DataTable selected_dt = new DataTable();

                if (SubProject_ID > 0)
                {

                    if (Subproject_comboBox.SelectedValue != null && Subproject_comboBox.SelectedIndex != -1)
                    {
                        string Selected_Subproject = Subproject_comboBox.SelectedValue.ToString();

                        DataRow[] Accountsrows = tills_dt.Select("Subproject_ID = '" + Selected_Subproject + "'");
                        if (Accountsrows.Length != 0)
                        {
                            selected_dt = Accountsrows.CopyToDataTable();
                            if (selected_dt != null && selected_dt.Rows.Count > 0)
                            {
                                DataView view = new DataView(selected_dt);
                                DataTable selected_dt1 = new DataTable();
                                selected_dt1 = view.ToTable(true, "Account_ID", "Account_Name");

                                if (selected_dt1 != null && selected_dt1.Rows.Count > 0)
                                    Account_comboBox.ItemsSource = selected_dt1.DefaultView;
                            }
                        }
                    }
                }
                else
                {
                    DataRow[] Accountsrows = tills_dt.Select("Accounttype_Name = 'Exchange' OR Accounttype_Name = 'Personal'");
                    if (Accountsrows.Length != 0)
                    {
                        selected_dt = Accountsrows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                            Account_comboBox.ItemsSource = selected_dt.DefaultView;
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
                if (PassException)
                    throw;
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

        private void Account_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Till_comboBox.ItemsSource = null;
                Type_comboBox.SelectedIndex = -1;

                if (Account_comboBox.IsEnabled)
                {
                    Account_ID = -1;
                    Account_Name = "";
                    Account_Type = "";
                    IsAdminAccount = false;
                    Till_ID = -1;
                    TillType = "";

                    Clear_Boxes();
                    Show_Hide_Components();

                    Account_ID = Convert.ToInt32(Account_comboBox.SelectedValue);


                    Account_Name = tills_dt.AsEnumerable()
                                         .FirstOrDefault(row => row.Field<int?>("Account_ID")?.Equals(Account_ID) ?? false)
                                         ?.Field<string>("Account_Name")
                                         ?.ToString();

                    Account_Type = tills_dt.AsEnumerable()
                                          .FirstOrDefault(row => row.Field<int?>("Account_ID")?.Equals(Account_ID) ?? false)
                                          ?.Field<string>("Accounttype_Name")
                                          ?.ToString();

                    Category = tills_dt.AsEnumerable()
                                          .FirstOrDefault(row => row.Field<int?>("Subproject_ID")?.Equals(SubProject_ID) ?? false)
                                          ?.Field<string>("Category_Name")
                                          ?.ToString();

                    IsAdminAccount = tills_dt.AsEnumerable()
                                        .FirstOrDefault(row => row.Field<int>("Account_ID") == Account_ID)?
                                        .Field<int>("is_administrative") == 1;

                }

                if (Account_comboBox.SelectedValue != null && Account_comboBox.SelectedIndex != -1)
                {
                    string Selected_Account = Account_comboBox.SelectedValue.ToString();

                    DataTable selected_dt = new DataTable();


                    DataRow[] Tillsrows = tills_dt.Select("Account_ID = '" + Selected_Account + "'");
                    if (Tillsrows.Length != 0)
                    {
                        selected_dt = Tillsrows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                            Till_comboBox.ItemsSource = selected_dt.DefaultView;
                    }
                }

                Apply_Color_Theme();
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
                if (PassException)
                    throw;
            }
        }

        private void Till_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Account_comboBox.IsEnabled)
                {
                    Till_ID = -1;
                    TillType = "";

                    Till_ID = Convert.ToInt32(Till_comboBox.SelectedValue);

                    TillType = tills_dt.AsEnumerable()
                       .FirstOrDefault(row => row.Field<int?>("Till_ID")?.Equals(Till_ID) ?? false)
                       ?.Field<string>("Tilltypes_Name")
                       ?.ToString();


                    Type_comboBox.SelectedIndex = -1;
                    Set_privileges();
                    Set_Available_Operations();

                    Clear_Boxes();
                    Show_Hide_Components();
                }


                /*
                if (Till_comboBox.SelectedIndex != -1 && Type_comboBox.SelectedIndex != -1)
                {
                    Show_Hide_Components();

                    Fill_ComboBoxes();
                }
                */
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
                if (PassException)
                    throw;
            }
        }

        private void Donor_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DestinationTill_comboBox.Visibility == Visibility.Visible)
                {
                    if (Type_ComboBox_Move_Item.IsSelected)
                    {
                        if (TillType.ToLower() != "syr")
                        {
                            int Donor_ID = Convert.ToInt32(Donor_comboBox.SelectedValue);
                            DataTable temp_Accounts_dt = Acc.Select_Accounts_With_Same_TillType(TillType, Account_ID, Donor_ID);


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
                            if (Accounts_dt != null && Accounts_dt.Rows.Count > 0)
                            {
                                DestinationTill_comboBox.ItemsSource = Accounts_dt.DefaultView;
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
                if (PassException)
                    throw;
            }
        }

        private void IsReceipt_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Employee_lable.Visibility = Employee_comboBox.Visibility = Employee_comboBox_clear.Visibility = Employee_help.Visibility = Visibility.Collapsed;

                if (IsReceipt_comboBox.SelectedIndex != -1)
                {
                    if (IsReceipt_comboBox.SelectedItem.ToString().Contains("payslip"))
                    {
                        if (ActionDate_datepicker.SelectedDate != null)
                        {
                            string Date = ((DateTime)ActionDate_datepicker.SelectedDate).ToString("yyyy/MM");


                            var filteredRows = Employee_dt.AsEnumerable()
                                   .Where(row => ((DateTime)row["Month"]).ToString("yyyy/MM") == Date);


                            DataTable filteredTable = filteredRows.Any() ? filteredRows.CopyToDataTable() : Employee_dt.Clone();

                            Employee_comboBox.ItemsSource = filteredTable.DefaultView;

                        }
                        else
                        {
                            Employee_comboBox.ItemsSource = null;
                        }
                        Employee_lable.Visibility = Employee_comboBox.Visibility = Employee_comboBox_clear.Visibility = Employee_help.Visibility = Visibility.Visible;
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
                if (PassException)
                    throw;
            }
        }

        private void ObserveCashAccount_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {


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
                if (PassException)
                    throw;
            }
        }

        private void CashAccount_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {


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
                if (PassException)
                    throw;
            }
        }

        private void ObserveCashAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ObserveCashAccount_comboBox.SelectedIndex = -1;
                ExchangeAcc_comboBox.ItemsSource = null;
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

        private void Category_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Line_comboBox.ItemsSource = null;
                if (Category_comboBox.SelectedValue != null && Category_comboBox.SelectedIndex != -1)
                {
                    string Selected_CatID = Category_comboBox.SelectedValue.ToString();
                    DataRow[] rows = Lines_dt.Select("budget_ID = '" + Selected_budgetID + "' and linecategory_ID = '" + Selected_CatID + "'");
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                            Line_comboBox.ItemsSource = selected_dt.DefaultView;
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
                if (PassException)
                    throw;
            }
        }

        private void DestinationTill_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ExchangeAcc_comboBox.ItemsSource = null;
                if (DestinationTill_comboBox.SelectedValue != null && DestinationTill_comboBox.SelectedIndex != -1)
                {
                    if (Type_comboBox.SelectedItem.ToString().Contains("Move"))
                    {
                        DataRow[] row = Accounts_dt.Select("ID=" + Convert.ToInt32(DestinationTill_comboBox.SelectedValue));

                        if (ObserveCashAccount_comboBox.Visibility == Visibility.Visible)
                        {
                            ObserveCashAccount_comboBox.ItemsSource = null;
                            int SubID = Convert.ToInt32(row[0]["subproject_ID"].ToString());
                            DataRow[] rows = null;
                            if (cashAccounts_dt != null)
                            {
                                rows = cashAccounts_dt.Select("SubProject_ID like '" + SubID + ",%' or SubProject_ID like '%," + SubID + ",%' or SubProject_ID like '%," + SubID + "' or SubProject_ID like '" + SubID + "'");
                            }
                            DataTable selected_dt = new DataTable();
                            if (rows != null && rows.Length != 0)
                            {
                                selected_dt = rows.CopyToDataTable();
                                if (selected_dt != null && selected_dt.Rows.Count > 0)
                                    ObserveCashAccount_comboBox.ItemsSource = selected_dt.DefaultView;
                            }
                        }
                    }
                    else
                    {
                        DataRow[] row = t_dt.Select("ID=" + Convert.ToInt32(DestinationTill_comboBox.SelectedValue));

                        if (ExchangeAcc_comboBox.Visibility == Visibility.Visible)
                        {
                            ExchangeAcc_comboBox.ItemsSource = null;

                            string ToTill = row[0]["Type"].ToString();
                            DataRow[] rows = ExAcc_dt.Select("ToTill = '" + ToTill + "'");

                            DataTable selected_dt = new DataTable();
                            if (rows.Length != 0)
                            {
                                selected_dt = rows.CopyToDataTable();
                                if (selected_dt != null && selected_dt.Rows.Count > 0)
                                    ExchangeAcc_comboBox.ItemsSource = selected_dt.DefaultView;
                            }
                        }

                        if (Category == "Budget" || Category == "Micro Projects" || Account_Type == "Exchange")
                        {
                            if (Type_ComboBox_Exchange_Item.IsSelected)
                            {
                                if (row[0]["Type"].ToString().ToLower() == "syr")
                                {
                                    ObserveCashAccount_comboBox.ItemsSource = null;
                                    ObserveCashAccount_lable.Visibility = Visibility.Visible;
                                    ObserveCashAccount_comboBox.Visibility = ObserveCashAccount_clear_btn.Visibility = Visibility.Visible;
                                    DataRow[] rows = null;
                                    if ((Category == "Budget" || Category == "Micro Projects") && cashAccounts_dt != null)
                                    {
                                        rows = cashAccounts_dt.Select("SubProject_ID like '" + SubProject_ID + ",%' or SubProject_ID like '%," + SubProject_ID + ",%' or SubProject_ID like '%," + SubProject_ID + "' or SubProject_ID like '" + SubProject_ID + "'");
                                    }
                                    else if (Account_Type == "Exchange" && cashAccounts_dt != null)
                                    {
                                        rows = cashAccounts_dt.Select("ExchangeAccount_ID like '" + Account_ID + ",%' or ExchangeAccount_ID like '%," + Account_ID + ",%' or ExchangeAccount_ID like '%," + Account_ID + "' or ExchangeAccount_ID like '" + Account_ID + "'");
                                    }
                                    else if (Account_Type == "Personal" && cashAccounts_dt != null)
                                    {
                                        rows = cashAccounts_dt.Select("PersonalAccount_ID like '" + Account_ID + ",%' or PersonalAccount_ID like '%," + Account_ID + ",%' or PersonalAccount_ID like '%," + Account_ID + "' or PersonalAccount_ID like '" + Account_ID + "'");
                                    }
                                    DataTable selected_dt = new DataTable();
                                    if (rows != null)
                                    {
                                        if (rows.Length != 0)
                                        {
                                            selected_dt = rows.CopyToDataTable();
                                            if (selected_dt != null && selected_dt.Rows.Count > 0)
                                                ObserveCashAccount_comboBox.ItemsSource = selected_dt.DefaultView;
                                        }
                                    }
                                }
                                else
                                {
                                    ObserveCashAccount_lable.Visibility = Visibility.Collapsed;
                                    ObserveCashAccount_comboBox.Visibility = ObserveCashAccount_clear_btn.Visibility = Visibility.Collapsed;
                                }


                                if ((row[0]["Type"].ToString().ToLower() == "syr" || TillType.ToLower() == "syr") && Account_Type != "Exchange")
                                {
                                    ExchangeCash_comboBox.ItemsSource = null;

                                    ExchangeCash_lable.Visibility = ExchangeCash_comboBox.Visibility = ExchangeCash_clear_btn.Visibility = Visibility.Visible;
                                    if (ExchangeAcc_comboBox.SelectedValue != null && ExchangeAcc_comboBox.SelectedIndex != -1)
                                    {
                                        int ExAcc_ID = Convert.ToInt32(ExchangeAcc_comboBox.SelectedValue);
                                        DataRow[] ExCashrows = null;
                                        if(cashAccounts_dt != null)
                                        {
                                            ExCashrows = cashAccounts_dt.Select("ExchangeAccount_ID like '" + ExAcc_ID + ",%' or ExchangeAccount_ID like '%," + ExAcc_ID + ",%' or ExchangeAccount_ID like '%," + ExAcc_ID + "' or ExchangeAccount_ID like '" + ExAcc_ID + "'");
                                        }
                                        DataTable ExCashselected_dt = new DataTable();
                                        if (ExCashrows != null)
                                        {
                                            if (ExCashrows.Length != 0)
                                            {
                                                ExCashselected_dt = ExCashrows.CopyToDataTable();
                                                if (ExCashselected_dt != null && ExCashselected_dt.Rows.Count > 0)
                                                    ExchangeCash_comboBox.ItemsSource = ExCashselected_dt.DefaultView;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    ExchangeCash_lable.Visibility = ExchangeCash_comboBox.Visibility = ExchangeCash_clear_btn.Visibility = Visibility.Collapsed;
                                }
                            }
                            else
                            {
                                ObserveCashAccount_lable.Visibility = Visibility.Collapsed;
                                ObserveCashAccount_comboBox.Visibility = ObserveCashAccount_clear_btn.Visibility = Visibility.Collapsed;
                                ExchangeCash_lable.Visibility = ExchangeCash_comboBox.Visibility = ExchangeCash_clear_btn.Visibility = Visibility.Collapsed;

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
                if (PassException)
                    throw;
            }
        }

        private void Rate_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                string value = Rate_txtBox.Text;
                if (value != "")
                {
                    double d;
                    if (double.TryParse(value, out d) && !Double.IsNaN(d) && !Double.IsInfinity(d))


                        //Rate_txtBox.Text = Login.ConvertToDouble(value);
                        Rate_txtBox.Text = d.ToString();
                    Calc_ExchangedValue();
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
                if (PassException)
                    throw;
            }
        }

        private void MircoProject_ID_txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //if (MircoProject_ID_txtBox.Text == "") MircoProject_ID_txtBox.Text = BeneficiaryName_txtBox.Text = "";
                //else
                //{
                //    string MircoProject_ID = MircoProject_ID_txtBox.Text;
                //    DataRow[] rows = b_dt.Select("MicroProject_ID=" + MircoProject_ID);
                //    if (rows.Length != 0)
                //    {
                //        BeneficiaryName_txtBox.Text = rows[0].Field<string>("Name");

                //        // view the rows of this project in the grid //
                //        DataRow[] My_rows = dt.Select("MicroProject_ID=" + MircoProject_ID_txtBox.Text);
                //        if (My_rows.Length != 0)
                //        {
                //            DataTable selected_rows_dt = new DataTable();

                //            selected_rows_dt = dt.Copy();
                //            selected_rows_dt.Rows.Clear();
                //            foreach (DataRow row in My_rows)
                //                selected_rows_dt.ImportRow(row);

                //            //myDataGrid.ItemsSource = selected_rows_dt.DefaultView;
                //        }
                //    }
                //    else BeneficiaryName_txtBox.Text = "";
                //}

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

        private void MircoProject_ID_autoSuggestionUserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text == "")
                {
                    BeneficiaryName_txtBox.autoTextBox.Text = "";
                }
                else
                {
                    string MircoProject_ID = MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text;
                    DataRow[] rows = b_dt.Select("MicroProject_ID=" + MircoProject_ID);
                    if (rows.Length != 0)
                    {
                        BeneficiaryName_txtBox.autoTextBox.Text = rows[0].Field<string>("Name");
                        BeneficiaryName_txtBox.CloseAutoSuggestionBox();
                    }
                    else BeneficiaryName_txtBox.autoTextBox.Text = "";
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
                if (PassException)
                    throw;
            }
        }

        private void Type_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Till_comboBox.SelectedIndex != -1 && Type_comboBox.SelectedIndex != -1)
                {
                    Show_Hide_Components();

                    Fill_ComboBoxes();
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
                if (PassException)
                    throw;
            }
        }

        private void Calc_USDValue()
        {
            //Calculate USD Value//
            if (Amount_txtBox.Text != "" && Original_AvgRate_comboBox.SelectedIndex != -1)
            {
                int Rate_ID = Convert.ToInt32(Original_AvgRate_comboBox.SelectedValue.ToString());
                DataRow[] rows = rate_dt.Select("ID=" + Rate_ID);
                if (rows.Length > 0)
                {
                    double avgRate = rows[0].Field<double>("Value");

                    //string expand = Amount_txtBox.Text.Replace(",", "");
                    Double.TryParse(Amount_txtBox.Text, out expandature);
                    USD_Value = expandature / avgRate;
                    USD_Value_TxtBox.Text = USD_Value.ToString();
                }

            }
            else
            {
                USD_Value = 0;
                USD_Value_TxtBox.Text = "";
            }
        }

        private void Calc_ExchangedValue()
        {
            if (Amount_txtBox.Text != "" && Rate_txtBox.Text != "")
            {
                //double exchanged = Double.Parse(Amount_txtBox.Text.Replace(",", "")) * Double.Parse(Rate_txtBox.Text.Replace(",", ""));
                double exchanged = Double.Parse(Amount_txtBox.Text) * Double.Parse(Rate_txtBox.Text);
                Exchanged_txtBox.Text = exchanged.ToString();
            }
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                string inserted_value = "";
                double amount = 0;
                string ActionDate = "";
                string Notes = "";
                string E_Notes = "";
                string ObserveTill_Type = "";
                int DestinationTill_ID = -1;
                int ExchangeAcc_ID = -1;
                int Donor_ID = -1;
                int MicroProject_ID = -1;
                int budget_line_ID = -1;
                int CashAccount_ID = -1;
                int ObserveCashAccount_ID = -1;
                int ExchangeCash_ID = -1;
                int IsReceipt = -1;
                int OriginalRate_ID = -1;
                string ReceiptNum = "";
                int Employee_Salary_ID = -1;
                bool WithExAcc = false;
                bool WithExchangeRate = false;

                double rate = 0;

                #region Check and get data

                if (Type_comboBox.Text == "")
                {
                    MessageBox.Show("Please select the operation type.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                else
                {
                    if (Type_comboBox.SelectedItem.ToString().Contains("Income"))
                    {
                        inserted_value = "I";
                    }
                    else if (Type_comboBox.SelectedItem.ToString().Contains("Adv. Payment"))
                    {
                        inserted_value = "O";
                    }
                    else if (Type_comboBox.SelectedItem.ToString().Contains("Returned"))
                    {
                        inserted_value = "R";
                    }
                    else if (Type_comboBox.SelectedItem.ToString().Contains("Expenses"))
                    {
                        inserted_value = "E";
                    }
                    else if (Type_comboBox.SelectedItem.ToString().Contains("Exchange"))
                    {
                        inserted_value = "Ex";
                        if (Account_Type == "Project")
                        {
                            if (!Check_AvgRate())
                            {
                                MessageBox.Show("Please ensure that there is at least one AvgRate that matches the conversion currency and extends for a period that includes the transaction date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                                return;
                            }
                        }
                    }
                    else if (Type_comboBox.SelectedItem.ToString().Contains("Move"))
                    {
                        inserted_value = "M";
                    }
                }

                if (Amount_txtBox.Text == "")
                {
                    MessageBox.Show("Please make sure to enter the amount.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }
                else
                {

                    Double.TryParse(Amount_txtBox.Text, out amount);
                }

                if (ActionDate_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please make sure to select the operation date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;

                }
                else
                {
                    ActionDate = ((DateTime)ActionDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                }

                if (DestinationTill_comboBox.Visibility == Visibility.Visible)
                {

                    if (Type_comboBox.SelectedItem.ToString().Contains("Exchange"))
                    {
                        if (DestinationTill_comboBox.SelectedIndex == -1)
                        {
                            MessageBox.Show("Please make sure to select the Corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;


                        }
                        else
                        {
                            DestinationTill_ID = Convert.ToInt32(DestinationTill_comboBox.SelectedValue);

                            DataRow[] row = t_dt.Select("ID=" + Convert.ToInt32(DestinationTill_ID));

                            ObserveTill_Type = row[0]["Type"].ToString();

                            if (Account_Type == "Project" && ExchangeAcc_comboBox.SelectedIndex != -1)
                                WithExAcc = true;

                            if (Account_Type == "Exchange")
                                WithExchangeRate = true;
                        }
                    }
                    else if (Type_comboBox.SelectedItem.ToString().Contains("Move"))
                    {
                        if (DestinationTill_comboBox.SelectedIndex == -1)
                        {
                            MessageBox.Show("Please make sure to select the Corresponding account.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;

                        }
                        else
                        {
                            if (DestinationTill_comboBox.SelectedIndex != -1)
                                DestinationTill_ID = Convert.ToInt32(DestinationTill_comboBox.SelectedValue);

                            ObserveTill_Type = TillType;
                        }
                    }

                }

                if (ExchangeAcc_comboBox.Visibility == Visibility.Visible)
                {
                    if (ExchangeAcc_comboBox.SelectedIndex == -1)
                    {

                    }
                    else
                    {
                        ExchangeAcc_ID = Convert.ToInt32(ExchangeAcc_comboBox.SelectedValue);

                        if (ExchangeCash_comboBox.Visibility == Visibility.Visible)
                        {
                            if (ExchangeCash_comboBox.SelectedIndex == -1)
                            {
                                MessageBox.Show("Please make sure to select the Exchange cash till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                                return;

                            }
                            else
                            {
                                ExchangeCash_ID = Convert.ToInt32(ExchangeCash_comboBox.SelectedValue);
                            }
                        }
                    }
                }

                if (MircoProject_ID_autoSuggestionUserControl.Visibility == Visibility.Visible)
                {
                    if (MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text != "" && BeneficiaryName_txtBox.autoTextBox.Text != "")
                        int.TryParse(MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text, out MicroProject_ID);
                    else MicroProject_ID = -1;

                    if (MicroProject_ID == -1)
                    {
                        MessageBox.Show("Please make sure to select the micro project.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;

                    }
                }

                if (Donor_comboBox.Visibility == Visibility.Visible)
                {
                    if (Donor_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please make sure to select the donor", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                        Donor_ID = Convert.ToInt32(Donor_comboBox.SelectedValue);
                }

                if (CashAccount_comboBox.Visibility == Visibility.Visible)
                {
                    if (CashAccount_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please make sure to select the cash till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                        CashAccount_ID = Convert.ToInt32(CashAccount_comboBox.SelectedValue);
                }

                if (ObserveCashAccount_comboBox.Visibility == Visibility.Visible)
                {
                    if (ObserveCashAccount_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please make sure to select the corresponding cash till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                        ObserveCashAccount_ID = Convert.ToInt32(ObserveCashAccount_comboBox.SelectedValue);
                }

                if (Budget_comboBox.Visibility == Visibility.Visible)
                {
                    if (Line_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please make sure to select the budget, category and budget line.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                        budget_line_ID = Convert.ToInt32(Line_comboBox.SelectedValue);
                }



                if (IsReceipt_comboBox.Visibility == Visibility.Visible)
                {
                    if (IsReceipt_comboBox.SelectedIndex == -1 && Account_Type != "Personal")
                    {
                        MessageBox.Show("Please make sure to select the documet type and to specify its date", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                    {
                        if (IsReceipt_comboBox.SelectedIndex != -1)
                        {
                            if (IsReceipt_comboBox.SelectedItem.ToString().Contains("invoice"))
                            {
                                IsReceipt = 1;
                            }
                            else if (IsReceipt_comboBox.SelectedItem.ToString().Contains("receipt"))
                            {
                                IsReceipt = 0;
                            }
                            else if (IsReceipt_comboBox.SelectedItem.ToString().Contains("payslip"))
                            {
                                IsReceipt = 2;
                            }
                        }
                    }
                }

                if (ReceiptNum_txtBox.Visibility == Visibility.Visible)
                {
                    if (ReceiptNum_txtBox.Text == "" && Account_Type != "Personal")
                    {
                        MessageBox.Show("Please make sure to enter the document number", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;

                    }
                    else
                        ReceiptNum = ReceiptNum_txtBox.Text;
                }

                if (Original_AvgRate_comboBox.Visibility == Visibility.Visible)
                {
                    if (Original_AvgRate_comboBox.SelectedIndex == -1)
                    {
                        if (Account_Type == "Project" && TillType.ToLower() == "syr")
                        {
                            MessageBox.Show("Please make sure to select the origin Avg rate", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            return;

                        }
                    }
                    else
                        OriginalRate_ID = Convert.ToInt32(Original_AvgRate_comboBox.SelectedValue);
                }

                if (Rate_txtBox.Visibility == Visibility.Visible)
                {
                    if (Rate_txtBox.Text == "")
                    {
                        MessageBox.Show("Please make sure to enter the rate", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                        Double.TryParse(Rate_txtBox.Text, out rate);
                }

                if (Employee_comboBox.Visibility == Visibility.Visible)
                {
                    if (Employee_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please make sure to select the Volunteer.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                    else
                    {
                        Employee_Salary_ID = Convert.ToInt32(Employee_comboBox.SelectedValue);
                    }
                }

                Notes = Notes_txtBox.Text;
                E_Notes = E_Notes_txtBox.Text;
                #endregion

                if (Login.BuildConnection())
                {
                    if (Action_ID != -1)//Edit
                    {
                        if (Approved_Action)
                        {
                            if (inserted_value == "Ex" || inserted_value == "M")
                            {
                                a.Update(Action_ID, inserted_value, Till_ID, TillType, DestinationTill_ID, ObserveTill_Type, Donor_ID, ExchangeAcc_ID, true, WithExAcc, WithExchangeRate, amount, rate
                                    , ActionDate, Notes, E_Notes, IsReceipt, ReceiptNum, Employee_Salary_ID
                                    , OriginalRate_ID, MicroProject_ID, budget_line_ID, -1, CashAccount_ID, ObserveCashAccount_ID, ExchangeCash_ID, Account_ID, "AddAction_Window");
                            }
                            else
                            {
                                a.Update(Action_ID, inserted_value, Till_ID, TillType, DestinationTill_ID, ObserveTill_Type, Donor_ID, ExchangeAcc_ID, false, false, false, amount, rate
                                , ActionDate, Notes, E_Notes, IsReceipt, ReceiptNum, Employee_Salary_ID
                                    , OriginalRate_ID, MicroProject_ID, budget_line_ID, -1, CashAccount_ID, ObserveCashAccount_ID, ExchangeCash_ID, -1, "AddAction_Window"); ;
                            }
                        }
                        else
                        {
                            if (inserted_value == "Ex" || inserted_value == "M")
                            {
                                a.Update_Not_Approved(Action_ID, inserted_value, Till_ID, TillType, DestinationTill_ID, ObserveTill_Type, Donor_ID, ExchangeAcc_ID, true, WithExAcc, WithExchangeRate, amount, rate
                                    , ActionDate, Notes, E_Notes, IsReceipt, ReceiptNum, Employee_Salary_ID
                                    , OriginalRate_ID, MicroProject_ID, budget_line_ID, -1, CashAccount_ID, ObserveCashAccount_ID, ExchangeCash_ID, Account_ID, "AddAction_Window");
                            }
                            else
                            {
                                a.Update_Not_Approved(Action_ID, inserted_value, Till_ID, TillType, DestinationTill_ID, ObserveTill_Type, Donor_ID, ExchangeAcc_ID, false, false, false, amount, rate
                                , ActionDate, Notes, E_Notes, IsReceipt, ReceiptNum, Employee_Salary_ID
                                    , OriginalRate_ID, MicroProject_ID, budget_line_ID, -1, CashAccount_ID, ObserveCashAccount_ID, ExchangeCash_ID, -1, "AddAction_Window"); ;
                            }
                        }
                        int success = 0;
                        if (FullAttachNames != null && FullAttachNames.Length > 0)
                        {
                            foreach (var FullAttachName in FullAttachNames)
                            {
                                if (Save_Attachments(FullAttachName, Action_ID)) success++;
                            }
                        }

                        if (FullAttachNames != null && FullAttachNames.Length > 0)
                            MessageBox.Show("Saved successfully." + Environment.NewLine + "Attachments: Succeeded" + FullAttachNames.Length.ToString() + "/" + success.ToString(), "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        else
                            MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                        this.Close();
                    }
                    else//Add new
                    {
                        int A_id = -1;
                        if (inserted_value == "Ex" || inserted_value == "M")
                        {
                            A_id = a.Insert(Till_ID, TillType, DestinationTill_ID, ObserveTill_Type, Donor_ID, ExchangeAcc_ID, true, WithExAcc, WithExchangeRate, inserted_value, amount, rate, ActionDate
                            , Notes, E_Notes, IsReceipt, ReceiptNum, Employee_Salary_ID
                           , OriginalRate_ID, MicroProject_ID, budget_line_ID, -1, CashAccount_ID, ObserveCashAccount_ID, ExchangeCash_ID, Account_ID, "AddAction_Window");
                        }
                        else
                        {
                            A_id = a.Insert(Till_ID, TillType, DestinationTill_ID, ObserveTill_Type, Donor_ID, -1, false, false, false, inserted_value, amount, rate, ActionDate
                            , Notes, E_Notes, IsReceipt, ReceiptNum, Employee_Salary_ID
                            , OriginalRate_ID, MicroProject_ID, budget_line_ID, -1, CashAccount_ID, ObserveCashAccount_ID, ExchangeCash_ID, -1, "AddAction_Window");
                        }
                        int success = 0;
                        if (FullAttachNames != null && FullAttachNames.Length > 0)
                        {
                            foreach (var FullAttachName in FullAttachNames)
                            {
                                if (Save_Attachments(FullAttachName, A_id)) success++;
                            }
                        }

                        if (FullAttachNames != null && FullAttachNames.Length > 0)
                            MessageBox.Show("Saved successfully." + Environment.NewLine + "Attachments: Succeeded" + FullAttachNames.Length.ToString() + "/" + success.ToString(), "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        else
                            MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        UserMode = false;
                        Clear_Boxes();
                        UserMode = true;
                    }

                    if (TW != null)
                        TW.Load_Data();
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clear_Boxes();
                Load_Form();
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

        private void Add_Attachment_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                myTh = new Thread(CallDialog);
                myTh.SetApartmentState(ApartmentState.STA);
                myTh.Start();
                myTh.Join();


                if (FullAttachNames.Length > 0)
                {
                    AddedAttachments_lable.Content = "Attachments:" + open.FileNames.Length.ToString();
                    AddedAttachments_panel.Visibility = Visibility.Visible;
                }
                else
                {
                    AddedAttachments_lable.Content = "Attachments:";
                    AddedAttachments_panel.Visibility = Visibility.Collapsed;
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

        private void CallDialog()
        {
            open = new OpenFileDialog();
            //open.Filter = "Image Files|*.jpg;*.jpeg;*.png;";
            open.Multiselect = true;
            var res = open.ShowDialog();
            if (res == true)
            {
                FullAttachNames = open.FileNames;
            }
            else
            {
                FullAttachNames = new string[] { };
            }
        }

        private bool Save_Attachments(string FullAttachName, int Action_id)
        {
            bool sucess = false;

            //Added By Maher 
            string t;
            d.GetNameFromPath(FullAttachName, out t, out _);
            //FilePicName = MicroProject_ID + "_img" + DateTime.Now.ToString("yyyyMMddhhmmss") + "." + t;

            while (d.file_Exist("Action_id_" + Action_ID.ToString() + "_" + t))
            {
                t = "(1)" + t;
            }

            ////////////////// Upload Image to server ////////////////////
            if (d.SaveDocument(FullAttachName, "Action_id_" + Action_id.ToString() + "_" + t))
            {
                d.AddAttachmentToDatabase(Action_id, -1, t, false, "AddAction_Window");
                sucess = true;
            }

            return sucess;

        }
    }
}
