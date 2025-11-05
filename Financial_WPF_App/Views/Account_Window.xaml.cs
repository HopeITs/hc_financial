using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Financial_WPF_App.Views;
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
    public partial class Account_Window : Window
    {
        public Account_Window(string Category, string AccType, string Project_ID, string SubProject_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.Acc_ID = -1;

            this.Category = Category;
            this.AccType = AccType;

            if (Project_ID != "")
            {
                this.Project_ID = Convert.ToInt32(Project_ID);
            }
            else this.Project_ID = -1;


            if (SubProject_ID != "")
            {
                this.SubProject_ID = Convert.ToInt32(SubProject_ID);
            }
            else this.SubProject_ID = -1;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Account_Window(int Account_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();
            Main_Window_2.WindowStateChanged += OnMainWindowStateChanged; // Subscribe to event

            this.main_Window = main_Window;

            this.Acc_ID = Account_ID;

            this.Project_ID = -1;
            this.SubProject_ID = -1;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Account_Window(int Account_ID, string AccType, Main_Window_2 main_Window)
        {
            InitializeComponent();
            Main_Window_2.WindowStateChanged += OnMainWindowStateChanged; // Subscribe to event

            this.main_Window = main_Window;

            this.Acc_ID = Account_ID;
            this.AccType = AccType;
            this.Project_ID = -1;
            this.SubProject_ID = -1;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private int Acc_ID; private string Account_Name;
        private int Project_ID, SubProject_ID;
        private string Category, AccType;
        private Models.Project p;
        private SubProject sub;
        private Models.Account a;
        private Till t; SubProject_Donor sd;
        private City c;
        private TillType tt;
        private DataTable dt, t_dt, sub_dt, donor_dt, p_dt, Amount_dt, PersonalAcc_dt, ExchangeAcc_dt, Cities_dt,TillType_dt;
        int IsClosed = 0;
        bool UserMode;
        bool IsAdminAccount = false;
        Main_Window_2 main_Window;
        private WindowState _currentWindowState = WindowState.Maximized; // Track window state

        List<SubProject_Donor> objDonorList;
        List<SubProject_Donor> FilteredobjDonorList;
        List<SubProject_Donor> Original_objDonorList;


        List<Local_SubProject> objSubProjectList;
        List<Local_SubProject> FilteredSubProjectList;
        List<Local_SubProject> Original_SubProjectList;


        List<Local_PersonalAcc> objPersonalAccList;
        List<Local_PersonalAcc> Original_PersonalAccList;

        List<Local_ExchangeAcc> objExchangeAccList;
        List<Local_ExchangeAcc> Original_ExchangeAccList;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];


        #region privileges
        bool Edit_privileges, Syr_privileges, foreign_privileges, Rates_privileges, Administrative_Account_privileges,
            Budget_privileges, BudgetLins_privileges, ExpensesReport_privileges, AddTill_privileges, Beneficiaries_privileges, CashTill_privileges, Balances_privileges, ExchangeRates_privileges;

        #endregion

        private class Local_SubProject
        {
            public int SubID { set; get; }

            public string SubName { set; get; }

            public Boolean Sub_Check_Status { set; get; }
        }

        private class Local_PersonalAcc
        {
            public int PersonalAcc_ID { set; get; }

            public string PersonalAcc_Name { set; get; }

            public Boolean PersonalAcc_Check_Status { set; get; }
        }

        private class Local_ExchangeAcc
        {
            public int ExchangeAcc_ID { set; get; }

            public string ExchangeAcc_Name { set; get; }

            public Boolean ExchangeAcc_Check_Status { set; get; }
        }

        private bool OnLoad()
        {
            try
            {
                p = new Models.Project();
                sub = new SubProject();
                a = new Models.Account();
                t = new Till();
                sd = new SubProject_Donor();
                c = new City();
                tt = new TillType();
                Original_objDonorList = new List<SubProject_Donor>();
                Original_SubProjectList = new List<Local_SubProject>();
                Original_PersonalAccList = new List<Local_PersonalAcc>();
                Original_ExchangeAccList = new List<Local_ExchangeAcc>();

                TotalAmount_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;

                City_lable.IsEnabled = City_comboBox.IsEnabled = City_clear_btn.IsEnabled =
                        Project_lable.IsEnabled = Project_comboBox.IsEnabled = Project_clear_btn.IsEnabled =
                        SubProject_label.IsEnabled = SubProject_comboBox.IsEnabled = SubProject_clear_btn.IsEnabled =
                        PersonalAccount_label.IsEnabled = PersonalAccount_comboBox.IsEnabled = PersonalAccount_clear_btn.IsEnabled =
                        ExchangeAccount_label.IsEnabled = ExchangeAccount_comboBox.IsEnabled = ExchangeAccount_clear_btn.IsEnabled =
                        Donor_label.IsEnabled = Donor_comboBox.IsEnabled = Donor_clear_btn.IsEnabled = Currency_comboBox.IsEnabled = 
                        ProjectNumber_label.IsEnabled = ProjectNumber_txtBox.IsEnabled =
                        BeginDate_label.IsEnabled = BeginDate_datepicker.IsEnabled =
                        EndDate_label.IsEnabled = EndDate_datepicker.IsEnabled = EndDate_CheckBox.IsEnabled = EndDate_Border.IsEnabled =
                        TotalAmount_label.IsEnabled = TotalAmount_txtBox.IsEnabled = AmountType_comboBox.IsEnabled =
                        IsClosed_label.IsEnabled = IsClosed_btn.IsEnabled = AmountType_comboBox.IsEnabled = true;

                Load_Data();

                Show_Hide_Component();
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
                this.Close();
                return false;
                //main_Window.main_tabControl.Items.RemoveAt(main_Window.main_tabControl.SelectedIndex);
            }
        }

        private void OnMainWindowStateChanged(WindowState newState)
        {
            _currentWindowState = newState;
            //if (newState == WindowState.Normal && myExpander.IsExpanded)
            //{
            //    First_col.Width = new GridLength(220, GridUnitType.Star);
            //    Second_col.Width = new GridLength(160, GridUnitType.Star);
            //}
            //else if (newState == WindowState.Maximized && myExpander.IsExpanded)
            //{
            //    First_col.Width = new GridLength(85, GridUnitType.Star);
            //    Second_col.Width = new GridLength(45, GridUnitType.Star);
            //}
            //else if (newState == WindowState.Normal && !myExpander.IsExpanded)
            //{
            //    First_col.Width = new GridLength(220, GridUnitType.Star);
            //    Second_col.Width = new GridLength(160, GridUnitType.Star);
            //}
        }

        private void Apply_Color_Theme()
        {

            if (AccType == "Project")
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
            else if (AccType == "Exchange")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["Orange"];
            }
            else if (AccType == "Personal")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelGreen"];
            }
            else if (AccType == "Cash")
            {
                Theme_Color = (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"];
            }


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);



        }

        private void Set_privileges()
        {
            Edit_privileges = Syr_privileges = foreign_privileges = Rates_privileges = Administrative_Account_privileges =
                Budget_privileges = BudgetLins_privileges = ExpensesReport_privileges = AddTill_privileges = Beneficiaries_privileges = CashTill_privileges = Balances_privileges = ExchangeRates_privileges = false;

            if (Settings.Default.Role == "Head Office")
            {
                Edit_privileges = Syr_privileges = foreign_privileges = Rates_privileges = Administrative_Account_privileges =
                    Budget_privileges = BudgetLins_privileges = ExpensesReport_privileges = AddTill_privileges = Beneficiaries_privileges = CashTill_privileges = Balances_privileges = ExchangeRates_privileges = true;
            }
            else if (Settings.Default.Role == "Project Manager")
            {
                Syr_privileges = foreign_privileges = Rates_privileges =
                     Budget_privileges = BudgetLins_privileges = ExpensesReport_privileges = Beneficiaries_privileges = CashTill_privileges = Balances_privileges = ExchangeRates_privileges = true;
            }
            else if (Settings.Default.Role == "Financial Manager")
            {
                Syr_privileges = Rates_privileges =
                     Budget_privileges = BudgetLins_privileges = ExpensesReport_privileges = Beneficiaries_privileges = CashTill_privileges = Balances_privileges = ExchangeRates_privileges = true;
                if (AccType == "Project" || AccType == "Exchange")
                {
                    foreign_privileges = true;
                }
            }
            else if (Settings.Default.Role == "Accountant")
            {
                Syr_privileges = Rates_privileges =
                    Budget_privileges = BudgetLins_privileges = ExpensesReport_privileges = Beneficiaries_privileges = CashTill_privileges = Balances_privileges = true;
                if (AccType == "Project")
                {
                    foreign_privileges = true;
                }
            }
            else if (Settings.Default.Role == "Cashier")
            {
                CashTill_privileges = Balances_privileges = true;
            }
            else if (Settings.Default.Role == "Employee")
            {
                CashTill_privileges = Balances_privileges = true;
            }
            else if (Settings.Default.Role == "Donor")
            {
                Syr_privileges = foreign_privileges = Rates_privileges =
                    Budget_privileges = BudgetLins_privileges = ExpensesReport_privileges = Beneficiaries_privileges = CashTill_privileges = Balances_privileges = ExchangeRates_privileges = true;
            }
        }

        private void Load_Data()
        {
            AddAdminAcc_Label.Visibility = AddAdminAcc_btn.Visibility = Visibility.Collapsed;
            //AddAdminAcc_StackPanel.Visibility = Visibility.Collapsed;

            Load_Comboboxes_Data();
            if (Acc_ID != -1)
            {
                tabControl1.Visibility = Visibility.Visible; //Update


                myExpander_Collapsed(this, new RoutedEventArgs());
                myExpander.IsExpanded = false;

                Fill_Account(Acc_ID);

                Set_privileges();

                if (!Edit_privileges)
                {
                    myExpander.IsEnabled = false;
                    ToolTipService.SetToolTip(myExpander, "You do not have permission to edit.");
                    ToolTipService.SetShowOnDisabled(myExpander, true);
                    ToolTipService.SetInitialShowDelay(myExpander, 100);
                }

                if (Category == "Budget")
                {
                    if (Administrative_Account_privileges && !IsAdminAccount)
                    {
                        AddAdminAcc_Label.Visibility = AddAdminAcc_btn.Visibility = Visibility.Visible;
                        //AddAdminAcc_StackPanel.Visibility = Visibility.Visible;
                    }
                }

                if (AccType == "Project")
                {
                    Project_comboBox.IsEnabled = Project_clear_btn.IsEnabled = false;
                    SubProject_comboBox.IsEnabled = SubProject_clear_btn.IsEnabled = false;
                }


                Fill_Account_Tabs("", "");
                tabControl1.SelectedIndex = 0;
            }
            else
            {
                if (AccType == "Project")
                {
                    Project_comboBox.ItemsSource = null;
                    if (Category != null)
                    {
                        DataRow[] rows = p_dt.Select("Category='" + Category + "'");
                        DataTable selected_dt = new DataTable();
                        if (rows.Length != 0)
                        {
                            selected_dt = rows.CopyToDataTable();
                            if (selected_dt != null && selected_dt.Rows.Count > 0)
                                Project_comboBox.ItemsSource = selected_dt.DefaultView;
                        }
                    }

                    if (Project_ID != -1)
                    {
                        Project_comboBox.SelectedValue = Project_ID;
                        Project_comboBox_SelectionChanged(Project_comboBox, null);
                    }
                }

                if (SubProject_ID != -1)
                    SubProject_comboBox.SelectedValue = this.SubProject_ID;
                UserMode = false;
                BeginDate_datepicker.Text = BeginDate_datepicker.DisplayDate.ToString("yyyy/MM/dd");

                EndDate_CheckBox.IsChecked = true;
                EndDate_datepicker.IsEnabled = false;
                EndDate_datepicker.Text = "";
                UserMode = true;
                tabControl1.Visibility = Visibility.Hidden; //Create New//
                myExpander_Expanded(this, new RoutedEventArgs());
                myExpander.IsExpanded = true;
            }
        }

        private void Show_Hide_Component()
        {
            if (AccType == "Project")
            {
                City_lable.IsEnabled = City_comboBox.IsEnabled = City_clear_btn.IsEnabled = Currency_comboBox.IsEnabled = Currency_label.IsEnabled = Currency_clear_btn.IsEnabled = 
                    PersonalAccount_label.IsEnabled = PersonalAccount_comboBox.IsEnabled = PersonalAccount_clear_btn.IsEnabled =
                    ExchangeAccount_label.IsEnabled = ExchangeAccount_comboBox.IsEnabled = ExchangeAccount_clear_btn.IsEnabled = false;
            }
            if (AccType == "Exchange")
            {
                Project_lable.IsEnabled = Project_comboBox.IsEnabled = Project_clear_btn.IsEnabled = Currency_comboBox.IsEnabled = Currency_label.IsEnabled = Currency_clear_btn.IsEnabled =
                    SubProject_label.IsEnabled = SubProject_comboBox.IsEnabled = SubProject_clear_btn.IsEnabled =
                    PersonalAccount_label.IsEnabled = PersonalAccount_comboBox.IsEnabled = PersonalAccount_clear_btn.IsEnabled =
                    ExchangeAccount_label.IsEnabled = ExchangeAccount_comboBox.IsEnabled = ExchangeAccount_clear_btn.IsEnabled =
                    Donor_label.IsEnabled = Donor_comboBox.IsEnabled = Donor_clear_btn.IsEnabled =
                    ProjectNumber_label.IsEnabled = ProjectNumber_txtBox.IsEnabled =
                    BeginDate_label.IsEnabled = BeginDate_datepicker.IsEnabled =
                    EndDate_label.IsEnabled = EndDate_datepicker.IsEnabled = EndDate_CheckBox.IsEnabled = EndDate_Border.IsEnabled =
                    TotalAmount_label.IsEnabled = TotalAmount_txtBox.IsEnabled = AmountType_comboBox.IsEnabled =
                    IsClosed_label.IsEnabled = IsClosed_btn.IsEnabled = AmountType_comboBox.IsEnabled = false;
            }
            else if (AccType == "Cash")
            {
                Project_lable.IsEnabled = Project_comboBox.IsEnabled = Project_clear_btn.IsEnabled =

                Donor_label.IsEnabled = Donor_comboBox.IsEnabled = Donor_clear_btn.IsEnabled =
                ProjectNumber_label.IsEnabled = ProjectNumber_txtBox.IsEnabled =
                BeginDate_label.IsEnabled = BeginDate_datepicker.IsEnabled =
                EndDate_label.IsEnabled = EndDate_datepicker.IsEnabled = EndDate_CheckBox.IsEnabled = EndDate_Border.IsEnabled =
                TotalAmount_label.IsEnabled = TotalAmount_txtBox.IsEnabled = AmountType_comboBox.IsEnabled = 
                IsClosed_label.IsEnabled = IsClosed_btn.IsEnabled = AmountType_comboBox.IsEnabled = false;

                PersonalAccount_label.IsEnabled = PersonalAccount_comboBox.IsEnabled = PersonalAccount_clear_btn.IsEnabled = Currency_comboBox.IsEnabled;
                ExchangeAccount_label.IsEnabled = ExchangeAccount_comboBox.IsEnabled = ExchangeAccount_clear_btn.IsEnabled = true;

                SubProject_label.IsEnabled = SubProject_comboBox.IsEnabled = SubProject_clear_btn.IsEnabled= Currency_label.IsEnabled = Currency_clear_btn.IsEnabled = true;
            }
            else if (AccType == "Personal")
            {
                Project_lable.IsEnabled = Project_comboBox.IsEnabled = Project_clear_btn.IsEnabled =
                    SubProject_label.IsEnabled = SubProject_comboBox.IsEnabled = SubProject_clear_btn.IsEnabled =
                    PersonalAccount_label.IsEnabled = PersonalAccount_comboBox.IsEnabled = PersonalAccount_clear_btn.IsEnabled =
                    ExchangeAccount_label.IsEnabled = ExchangeAccount_comboBox.IsEnabled = ExchangeAccount_clear_btn.IsEnabled =
                    Donor_label.IsEnabled = Donor_comboBox.IsEnabled = Donor_clear_btn.IsEnabled =
                    ProjectNumber_label.IsEnabled = ProjectNumber_txtBox.IsEnabled =
                    TotalAmount_label.IsEnabled = TotalAmount_txtBox.IsEnabled = AmountType_comboBox.IsEnabled =
                    AmountType_comboBox.IsEnabled  = Currency_comboBox.IsEnabled = Currency_label.IsEnabled = false;
            }
        }

        private void Fill_Account(int Account_ID)
        {
            UserMode = false;

            dt = new DataTable();

            if (AccType == "Cash")
            {
                dt = a.Select_Cash_Account(Account_ID.ToString(), "");

                if (dt != null && dt.Rows.Count > 0)
                {
                    int i = dt.Rows.Count - 1;

                    if (dt.Rows[i]["city_id"] != DBNull.Value && dt.Rows[i]["city_id"] != null)
                    {
                        City_comboBox.SelectedValue = dt.Rows[i].Field<int>("city_id");
                    }
                    Currency_comboBox.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        if (dt.Rows[i]["currency_tilltype_id"] != DBNull.Value && dt.Rows[i]["currency_tilltype_id"] != null)
                        {
                            Currency_comboBox.SelectedValue = dt.Rows[i].Field<int>("currency_tilltype_id");
                            Currency_comboBox.IsEnabled = false;
                            Currency_clear_btn.IsEnabled = false;
                            Currency_label.IsEnabled = false;
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);


                    List<string> AllSubProject_ID = new List<string>();
                    List<string> AllPersonalAcc_ID = new List<string>();
                    List<string> AllExchangeAcc_ID = new List<string>();

                    Original_SubProjectList = new List<Local_SubProject>();
                    if (dt.Rows[i]["SubProject_ID"] != DBNull.Value && dt.Rows[i]["SubProject_ID"] != null)
                    {
                        AllSubProject_ID = dt.Rows[i]["SubProject_ID"].ToString().Split(',').ToList();
                        for (int j = 0; j < AllSubProject_ID.Count; j++)
                        {
                            if (objSubProjectList != null && objSubProjectList.Count > 0)
                            {
                                foreach (var subProject in objSubProjectList)
                                {
                                    if (subProject.SubID.ToString() == AllSubProject_ID[j])
                                    {
                                        subProject.Sub_Check_Status = true;

                                        Local_SubProject obj = new Local_SubProject();
                                        obj.SubID = subProject.SubID;
                                        obj.SubName = subProject.SubName;
                                        obj.Sub_Check_Status = true;
                                        Original_SubProjectList.Add(obj);
                                    }
                                }
                            }

                            SubProject_comboBox.ItemsSource = null;
                            SubProject_comboBox.Text = "0 Selected";
                            SubProject_comboBox.ItemsSource = objSubProjectList;
                            int count = objSubProjectList.Count(item => item.Sub_Check_Status == true);
                            SubProject_comboBox.Text = count.ToString() + " Selected";

                            SubProject_Checkbox_Item_Clicked(SubProject_comboBox, null);
                        }
                    }

                    Original_PersonalAccList = new List<Local_PersonalAcc>();
                    if (dt.Rows[i]["PersonalAccount_ID"] != DBNull.Value && dt.Rows[i]["PersonalAccount_ID"] != null)
                    {
                        AllPersonalAcc_ID = dt.Rows[i]["PersonalAccount_ID"].ToString().Split(',').ToList();
                        for (int j = 0; j < AllPersonalAcc_ID.Count; j++)
                        {
                            if (objPersonalAccList != null && objPersonalAccList.Count > 0)
                            {
                                foreach (var PersonalAcc in objPersonalAccList)
                                {
                                    if (PersonalAcc.PersonalAcc_ID.ToString() == AllPersonalAcc_ID[j])
                                    {
                                        PersonalAcc.PersonalAcc_Check_Status = true;

                                        Local_PersonalAcc obj = new Local_PersonalAcc();
                                        obj.PersonalAcc_ID = PersonalAcc.PersonalAcc_ID;
                                        obj.PersonalAcc_Name = PersonalAcc.PersonalAcc_Name;
                                        obj.PersonalAcc_Check_Status = true;
                                        Original_PersonalAccList.Add(obj);
                                    }
                                }
                            }
                            PersonalAccount_comboBox.ItemsSource = null;
                            PersonalAccount_comboBox.Text = "0 Selected";
                            PersonalAccount_comboBox.ItemsSource = objPersonalAccList;
                            int count = objPersonalAccList.Count(item => item.PersonalAcc_Check_Status == true);
                            PersonalAccount_comboBox.Text = count.ToString() + " Selected";
                        }
                    }

                    Original_ExchangeAccList = new List<Local_ExchangeAcc>();
                    if (dt.Rows[i]["ExchangeAccount_ID"] != DBNull.Value && dt.Rows[i]["ExchangeAccount_ID"] != null)
                    {
                        AllExchangeAcc_ID = dt.Rows[i]["ExchangeAccount_ID"].ToString().Split(',').ToList();
                        for (int j = 0; j < AllExchangeAcc_ID.Count; j++)
                        {
                            if (objExchangeAccList != null && objExchangeAccList.Count > 0)
                            {
                                foreach (var ExchangeAcc in objExchangeAccList)
                                {
                                    if (ExchangeAcc.ExchangeAcc_ID.ToString() == AllExchangeAcc_ID[j])
                                    {
                                        ExchangeAcc.ExchangeAcc_Check_Status = true;

                                        Local_ExchangeAcc obj = new Local_ExchangeAcc();
                                        obj.ExchangeAcc_ID = ExchangeAcc.ExchangeAcc_ID;
                                        obj.ExchangeAcc_Name = ExchangeAcc.ExchangeAcc_Name;
                                        obj.ExchangeAcc_Check_Status = true;
                                        Original_ExchangeAccList.Add(obj);
                                    }

                                }
                            }


                            ExchangeAccount_comboBox.ItemsSource = null;
                            ExchangeAccount_comboBox.Text = "0 Selected";
                            ExchangeAccount_comboBox.ItemsSource = objExchangeAccList;
                            int count = objExchangeAccList.Count(item => item.ExchangeAcc_Check_Status == true);
                            ExchangeAccount_comboBox.Text = count.ToString() + " Selected";
                        }
                    }

                    Account_Name = Name_txtBox.Text = dt.Rows[i].Field<string>("Name").ToString();
                }
            }
            else
            {
                dt = a.Select("", Account_ID.ToString(), "", false, "");

                if (dt != null && dt.Rows.Count > 0)
                {
                    int i = dt.Rows.Count - 1;

                    if (dt.Rows[i]["Project_ID"] != DBNull.Value)
                        Project_ID = dt.Rows[i].Field<int>("Project_ID");

                    if (dt.Rows[i]["SubProject_ID"] != DBNull.Value)
                        SubProject_ID = dt.Rows[i].Field<int>("SubProject_ID");

                    Category = dt.Rows[i].Field<string>("Category");

                    AccType = dt.Rows[i].Field<string>("Account_Type");


                    Project_comboBox.ItemsSource = null;
                    if (Category != null)
                    {
                        DataRow[] rows = p_dt.Select("Category='" + Category + "'");
                        DataTable selected_dt = new DataTable();
                        if (rows.Length != 0)
                        {
                            selected_dt = rows.CopyToDataTable();
                            if (selected_dt != null && selected_dt.Rows.Count > 0)
                                Project_comboBox.ItemsSource = selected_dt.DefaultView;
                        }
                    }

                    if (Project_ID != -1)
                    {
                        Project_comboBox.SelectedValue = Project_ID;
                        Project_comboBox_SelectionChanged(Project_comboBox, null);
                    }

                    if (dt.Rows[i]["City_ID"] != DBNull.Value) City_comboBox.SelectedValue = dt.Rows[i].Field<int>("City_ID").ToString();


                    List<string> AllSubProject_ID = new List<string>();

                    if (dt.Rows[i]["SubProject_ID"] != DBNull.Value && dt.Rows[i]["SubProject_ID"] != null)
                    {
                        AllSubProject_ID = dt.Rows[i]["SubProject_ID"].ToString().Split(',').ToList();
                        Original_SubProjectList = new List<Local_SubProject>();
                        for (int j = 0; j < AllSubProject_ID.Count; j++)
                        {
                            if (objSubProjectList != null && objSubProjectList.Count > 0)
                            {
                                foreach (var subProject in objSubProjectList)
                                {
                                    if (subProject.SubID.ToString() == AllSubProject_ID[j])
                                    {
                                        subProject.Sub_Check_Status = true;


                                        Local_SubProject obj = new Local_SubProject();
                                        obj.SubID = subProject.SubID;
                                        obj.SubName = subProject.SubName;
                                        obj.Sub_Check_Status = true;
                                        Original_SubProjectList.Add(obj);
                                    }
                                }
                            }
                            SubProject_comboBox.ItemsSource = null;
                            SubProject_comboBox.Text = "0 Selected";
                            SubProject_comboBox.ItemsSource = objSubProjectList;
                            int count = objSubProjectList.Count(item => item.Sub_Check_Status == true);
                            SubProject_comboBox.Text = count.ToString() + " Selected";
                            SubProject_Checkbox_Item_Clicked(SubProject_comboBox, null);
                        }
                    }


                    List<string> AllDonorsID = new List<string>();

                    if (dt.Rows[i]["Donors_ID"] != DBNull.Value && dt.Rows[i]["Donors_ID"] != null)
                    {
                        AllDonorsID = dt.Rows[i]["Donors_ID"].ToString().Split(',').ToList();
                        Original_objDonorList = new List<SubProject_Donor>();
                        for (int j = 0; j < AllDonorsID.Count; j++)
                        {
                            if (objDonorList != null && objDonorList.Count > 0)
                            {
                                foreach (var donor in objDonorList)
                                {
                                    if (donor.Donor_ID.ToString() == AllDonorsID[j])
                                    {
                                        donor.Check_Status = true;


                                        SubProject_Donor obj = new SubProject_Donor();
                                        obj.Donor_ID = donor.Donor_ID;
                                        obj.DonorName = donor.DonorName;
                                        obj.Check_Status = true;
                                        Original_objDonorList.Add(obj);
                                    }

                                }
                            }


                            Donor_comboBox.ItemsSource = null;
                            Donor_comboBox.Text = "0 Selected";
                            Donor_comboBox.ItemsSource = objDonorList;
                            int count = objDonorList.Count(item => item.Check_Status == true);
                            Donor_comboBox.Text = count.ToString() + " Selected";
                            Donor_Checkbox_Item_Clicked(Donor_comboBox, null);
                        }
                    }


                    Account_Name = Name_txtBox.Text = dt.Rows[i].Field<string>("Name").ToString();
                    ProjectNumber_txtBox.Text = dt.Rows[i].Field<string>("ProjectNumber").ToString();

                    if (dt.Rows[i]["BeginDate"] != DBNull.Value)
                        BeginDate_datepicker.SelectedDate = dt.Rows[i].Field<DateTime>("BeginDate");

                    if (dt.Rows[i]["EndDate"] != DBNull.Value)
                    {
                        if (dt.Rows[i].Field<object>("EndDate").ToString() != "0")
                        {
                            EndDate_datepicker.SelectedDate = DateTime.Parse(dt.Rows[i].Field<string>("EndDate"));
                            EndDate_CheckBox.IsChecked = false;
                        }
                        else EndDate_CheckBox.IsChecked = true;
                    }


                    if (dt.Rows[i].Field<object>("TotalAmount") != null)
                        TotalAmount_txtBox.Text = Login.ConvertToDouble(dt.Rows[i].Field<Double>("TotalAmount").ToString());

                    if (dt.Rows[i]["AmountType_ID"] != DBNull.Value) AmountType_comboBox.SelectedValue = dt.Rows[i].Field<int>("AmountType_ID").ToString();

                    if (dt.Rows[i].Field<string>("IsClosed").ToLower() == "Open".ToLower())
                    {
                        IsClosed_btn.Content = "Open";
                        //SolidColorBrush brush2 = (SolidColorBrush)App.Current.Resources["HopeBlue4"];
                        //IsClosed_btn.Foreground = brush2;
                    }
                    else if (dt.Rows[i].Field<string>("IsClosed").ToLower() == "Closed".ToLower())
                    {
                        IsClosed_btn.Content = "Closed";
                        //SolidColorBrush brush2 = (SolidColorBrush)App.Current.Resources["HopeRed2"];
                        //IsClosed_btn.Foreground = brush2;
                    }

                    if (dt.Rows[i].Field<int>("is_administrative") == 1)
                        IsAdminAccount = true;
                }
            }

            UserMode = true;

        }

        public void Fill_Account_Tabs(string till_ID, string type)
        {
            if (AccType != "Cash")
            {
                tabControl1.Items.Clear();
                TabItem newTabItem = new TabItem();

                if (till_ID == "" && type == "")
                {
                    t_dt = new DataTable();
                    t_dt = t.Select(Acc_ID.ToString(), -1, "");

                    if (t_dt != null && t_dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < t_dt.Rows.Count; i++)
                        {
                            int Till_ID = t_dt.Rows[i].Field<int>(0);
                            string TillType_Name = t_dt.Rows[i].Field<string>("Type");
                            //privileges
                            bool addtill = false;
                            if (TillType_Name.ToLower() == "syr" && Syr_privileges)
                            {
                                if (Syr_privileges)
                                    addtill = true;
                            }
                            else
                            {
                                if (foreign_privileges)
                                    addtill = true;
                            }
                            if (addtill)
                            {
                                Till_Window till_Window = new Till_Window(Acc_ID, IsAdminAccount, Account_Name, AccType, Category, SubProject_ID, Till_ID, TillType_Name, this, main_Window);
                                newTabItem = new TabItem
                                {
                                    Header = (t_dt.Rows[i].Field<string>(1)).ToUpper(),
                                    Name = t_dt.Rows[i].Field<string>(1) + "_tab",
                                    Style = this.FindResource("tabItemStyle") as Style,
                                    FlowDirection = FlowDirection.LeftToRight
                                };
                                if (AccType == "Project")
                                {
                                    switch (Category)
                                    {
                                        case "Budget":
                                            newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                                            newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                                            break;
                                        case "Micro Projects":
                                            newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                                            newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                            break;
                                        default:
                                            newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                                            newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                                            break;
                                    }
                                }
                                else if (AccType == "Exchange")
                                {
                                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["Orange"];
                                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["Orange"]);
                                }
                                else if (AccType == "Personal")
                                {
                                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelGreen"];
                                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                                }
                                else
                                {
                                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
                                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                                }
                                tabControl1.Items.Add(newTabItem);
                                newTabItem.Content = null;
                                newTabItem.Content = till_Window.Content;
                            }
                        }
                    }
                }
                if (AccType == "Project")
                {
                    if (Rates_privileges)
                    {
                        Rate_Window rate_Window = new Rate_Window(Acc_ID, IsAdminAccount, AccType, Category, main_Window);
                        newTabItem = new TabItem
                        {
                            Header = "Rates",
                            Name = "rate_tab",
                            Style = this.FindResource("tabItemStyle") as Style,
                            FlowDirection = FlowDirection.LeftToRight
                        };
                        switch (Category)
                        {
                            case "Budget":
                                newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                                newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                                break;
                            case "Micro Projects":
                                newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                                newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                break;
                            default:
                                newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                                newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                                break;
                        }
                        tabControl1.Items.Add(newTabItem);
                        newTabItem.Content = null;
                        newTabItem.Content = rate_Window.Content;
                    }

                    if (Category == "Micro Projects")
                    {
                        if (Beneficiaries_privileges)
                        {
                            Account_Beneficiaries_Window account_Beneficiaries_Window = new Account_Beneficiaries_Window(Acc_ID, SubProject_ID, main_Window);
                            newTabItem = new TabItem
                            {
                                Header = "Beneficiaries",
                                Name = "ben_tab",
                                Style = this.FindResource("tabItemStyle") as Style,
                                FlowDirection = FlowDirection.LeftToRight,
                            };
                            newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                            newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                            tabControl1.Items.Add(newTabItem);
                            newTabItem.Content = null;
                            newTabItem.Content = account_Beneficiaries_Window.Content;
                        }
                    }
                    else if (Category == "Budget")
                    {
                        if (Budget_privileges)
                        {
                            AddBudgetLines_Windows AddBudgetLines_Windows = new AddBudgetLines_Windows(Acc_ID.ToString(), IsAdminAccount, main_Window);
                            newTabItem = new TabItem
                            {
                                Header = "Budgets",
                                Name = "ben_tab",
                                Style = this.FindResource("tabItemStyle") as Style,
                                FlowDirection = FlowDirection.LeftToRight
                            };
                            newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                            newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                            tabControl1.Items.Add(newTabItem);
                            newTabItem.Content = null;
                            newTabItem.Content = AddBudgetLines_Windows.Content;
                        }
                        if (BudgetLins_privileges)
                        {
                            AllAccountBudgetLines_Windows AllAccountBudgetLines_Windows = new AllAccountBudgetLines_Windows(Acc_ID.ToString(), Project_ID, main_Window);
                            newTabItem = new TabItem
                            {
                                Header = "Budget Lines",
                                Name = "ben_tab",
                                Style = this.FindResource("tabItemStyle") as Style,
                                FlowDirection = FlowDirection.LeftToRight,
                            };
                            newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                            newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                            tabControl1.Items.Add(newTabItem);
                            newTabItem.Content = null;
                            newTabItem.Content = AllAccountBudgetLines_Windows.Content;
                        }

                        if (ExpensesReport_privileges)
                        {
                            ExpensesReport_Window ExpensesReport_Window = new ExpensesReport_Window(Acc_ID, main_Window);
                            newTabItem = new TabItem
                            {
                                Header = "Budget Report",
                                Name = "ben_tab",
                                Style = this.FindResource("tabItemStyle") as Style,
                                FlowDirection = FlowDirection.LeftToRight,
                            };
                            newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                            newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                            tabControl1.Items.Add(newTabItem);
                            newTabItem.Content = null;
                            newTabItem.Content = ExpensesReport_Window.Content;
                        }
                    }

                }
                else if (AccType == "Exchange")
                {
                    if (ExchangeRates_privileges)
                    {
                        ExchangAccount_Rate_Window ExchangAccount_Rate_Window = new ExchangAccount_Rate_Window(Acc_ID, Account_Name, main_Window);
                        newTabItem = new TabItem
                        {
                            Header = "Rates",
                            Name = "rates_tab",
                            Style = this.FindResource("tabItemStyle") as Style,
                            FlowDirection = FlowDirection.LeftToRight,
                        };
                        newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["Orange"];
                        newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["Orange"]);
                        tabControl1.Items.Add(newTabItem);
                        newTabItem.Content = null;
                        newTabItem.Content = ExchangAccount_Rate_Window.Content;
                    }
                }

                if (AddTill_privileges)
                {
                    Add_till add_till = new Add_till(Acc_ID, AccType, Category, this, main_Window);
                    newTabItem = new TabItem
                    {
                        Header = "+",
                        Name = "add_tab",
                        Style = this.FindResource("tabItemStyle") as Style,
                        FlowDirection = FlowDirection.LeftToRight
                    };
                    if (AccType == "Project")
                    {
                        switch (Category)
                        {
                            case "Budget":
                                newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                                newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                                break;
                            case "Micro Projects":
                                newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["HopeRed"];
                                newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                break;
                            default:
                                newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                                newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                                break;
                        }
                    }
                    else if (AccType == "Exchange")
                    {
                        newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["Orange"];
                        newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["Orange"]);
                    }
                    else if (AccType == "Personal")
                    {
                        newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelGreen"];
                        newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                    }
                    else
                    {
                        newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];
                        newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                    }
                    tabControl1.Items.Add(newTabItem);
                    newTabItem.Content = null;
                    newTabItem.Content = add_till.Content;
                }
            }
            else
            {
                TabItem newTabItem = new TabItem();

                if (CashTill_privileges)
                {
                    tabControl1.Items.Clear();

                    CashTill_Window cash_window = new CashTill_Window(Acc_ID, Account_Name, main_Window);
                    newTabItem = new TabItem
                    {
                        Header = "Cash Till",
                        Name = "cash_tab",
                        Style = this.FindResource("tabItemStyle") as Style,
                        FlowDirection = FlowDirection.LeftToRight
                    };
                    newTabItem.Foreground = (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"];
                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"]);
                    tabControl1.Items.Add(newTabItem);
                    newTabItem.Content = null;
                    newTabItem.Content = cash_window.Content;
                }

                if (Balances_privileges)
                {
                    CashBalances_Window CashBalances_Window = new CashBalances_Window(Acc_ID, main_Window);
                    newTabItem = new TabItem
                    {
                        Header = "Balances",
                        Name = "Balances_tab",
                        Style = this.FindResource("tabItemStyle") as Style,
                        Foreground = (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"],
                        FlowDirection = FlowDirection.LeftToRight
                    };
                    newTabItem.Loaded += (sender, e) => NewTabItem_Loaded(sender, e, (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"]);
                    tabControl1.Items.Add(newTabItem);
                    newTabItem.Content = null;
                    newTabItem.Content = CashBalances_Window.Content;
                }
            }

        }
        // Event handler for the Loaded event
        private void NewTabItem_Loaded(object sender, RoutedEventArgs e, SolidColorBrush color)
        {
            TabItem tabItem = sender as TabItem;
            if (tabItem != null)
            {
                // Find the BottomBorder inside the TabItem template
                Border bottomBorder = tabItem.Template.FindName("BottomBorder", tabItem) as Border;
                if (bottomBorder != null)
                {
                    // Set the new color for the bottom border
                    bottomBorder.Background = color;  // Replace with your desired color
                }
            }
        }

        private void PersonalAcc_Checkbox_Item_Clicked(object sender, RoutedEventArgs e)
        {
            int count = objPersonalAccList.Count(item => item.PersonalAcc_Check_Status == true);
            PersonalAccount_comboBox.Text = count.ToString() + " Selected";
        }

        private void Load_Comboboxes_Data()
        {

            Project_comboBox.ItemsSource = null;

            p_dt = p.Select("", "", "");


            if (AccType != "Project")
            {
                Cities_dt = c.Select("", "");
                City_comboBox.ItemsSource = Cities_dt.DefaultView;
            }

            SubProject_comboBox.ItemsSource = null;
            SubProject_comboBox.Text = "0 Selected";
            sub_dt = sub.Select("", "", "", "");
            //SubProject_comboBox.Text = "0 Selected";
            if (AccType == "Cash")
            {
                objSubProjectList = new List<Local_SubProject>();

                if (sub_dt != null && sub_dt.Rows.Count > 0)
                {
                    for (int i = 0; i < sub_dt.Rows.Count; i++)
                    {
                        Local_SubProject obj = new Local_SubProject();
                        obj.SubID = sub_dt.Rows[i].Field<int>(0);
                        obj.SubName = sub_dt.Rows[i].Field<string>(1);
                        objSubProjectList.Add(obj);
                    }
                    SubProject_comboBox.ItemsSource = objSubProjectList;
                    int count = objSubProjectList.Count(item => item.Sub_Check_Status == true);
                    SubProject_comboBox.Text = count.ToString() + " Selected";
                }
            }
            //Else Set on selection

            Donor_comboBox.ItemsSource = null;
            Donor_comboBox.Text = "0 Selected";
            donor_dt = sd.Select("", "");
            //Donor_comboBox.Text = "0 Selected";


            if (AccType == "Cash")
            {
                PersonalAccount_comboBox.ItemsSource = null;
                PersonalAccount_comboBox.Text = "0 Selected";
                PersonalAcc_dt = a.Select_Personal_Accounts();
                //PersonalAccount_comboBox.Text = "0 Selected";
                objPersonalAccList = new List<Local_PersonalAcc>();
                if (PersonalAcc_dt != null && PersonalAcc_dt.Rows.Count > 0)
                {
                    for (int i = 0; i < PersonalAcc_dt.Rows.Count; i++)
                    {
                        Local_PersonalAcc obj = new Local_PersonalAcc();
                        obj.PersonalAcc_ID = PersonalAcc_dt.Rows[i].Field<int>(0);
                        obj.PersonalAcc_Name = PersonalAcc_dt.Rows[i].Field<string>(1);
                        objPersonalAccList.Add(obj);
                    }
                    PersonalAccount_comboBox.ItemsSource = objPersonalAccList;
                    int count = objPersonalAccList.Count(item => item.PersonalAcc_Check_Status == true);
                    PersonalAccount_comboBox.Text = count.ToString() + " Selected";
                }

                ExchangeAccount_comboBox.ItemsSource = null;
                ExchangeAccount_comboBox.Text = "0 Selected";
                ExchangeAcc_dt = a.Select_Exchange_Accounts();
                //ExchangeAccount_comboBox.Text = "0 Selected";
                objExchangeAccList = new List<Local_ExchangeAcc>();
                if (ExchangeAcc_dt != null && ExchangeAcc_dt.Rows.Count > 0)
                {
                    for (int i = 0; i < ExchangeAcc_dt.Rows.Count; i++)
                    {
                        Local_ExchangeAcc obj = new Local_ExchangeAcc();
                        obj.ExchangeAcc_ID = ExchangeAcc_dt.Rows[i].Field<int>(0);
                        obj.ExchangeAcc_Name = ExchangeAcc_dt.Rows[i].Field<string>(1);
                        objExchangeAccList.Add(obj);
                    }
                    ExchangeAccount_comboBox.ItemsSource = objExchangeAccList;
                    int count = objExchangeAccList.Count(item => item.ExchangeAcc_Check_Status == true);
                    ExchangeAccount_comboBox.Text = count.ToString() + " Selected";
                }


                DataRow[] rows = p_dt.Select("Category='" + Category + "'");
                DataTable selected_dt = new DataTable();
                if (rows.Length != 0)
                {
                    selected_dt = rows.CopyToDataTable();
                    if (selected_dt != null && selected_dt.Rows.Count > 0)
                        Project_comboBox.ItemsSource = selected_dt.DefaultView;
                }

                Currency_comboBox.ItemsSource = null;
                TillType_dt = tt.SelectTillTypes();
                if(TillType_dt != null && TillType_dt.Rows.Count > 0)
                    {
                    Currency_comboBox.ItemsSource = TillType_dt.DefaultView;
                }
            }


            AmountType_comboBox.ItemsSource = null;
            Amount_dt = a.Select_AmountType();
            if (Amount_dt != null && Amount_dt.Rows.Count > 0)
            {
                AmountType_comboBox.ItemsSource = Amount_dt.DefaultView;
            }
        }

        private void Project_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AccType != "Cash")
                {
                    SubProject_comboBox.ItemsSource = null;
                    SubProject_comboBox.Text = "0 Selected";
                    if (Project_comboBox.SelectedValue != null && Project_comboBox.SelectedIndex != -1)
                    {
                        int project_id = Convert.ToInt32(Project_comboBox.SelectedValue);
                        DataRow[] rows = sub_dt.Select("Project_ID=" + project_id);
                        DataTable selected_dt = new DataTable();
                        if (rows.Length != 0)
                        {
                            selected_dt = rows.CopyToDataTable();
                            if (selected_dt != null && selected_dt.Rows.Count > 0)
                            {
                                objSubProjectList = new List<Local_SubProject>();
                                for (int i = 0; i < selected_dt.Rows.Count; i++)
                                {
                                    Local_SubProject obj = new Local_SubProject();
                                    obj.SubID = selected_dt.Rows[i].Field<int>(0);
                                    obj.SubName = selected_dt.Rows[i].Field<string>(1);
                                    objSubProjectList.Add(obj);
                                }
                                SubProject_comboBox.ItemsSource = objSubProjectList;
                                int count = objSubProjectList.Count(item => item.Sub_Check_Status == true);
                                SubProject_comboBox.Text = count.ToString() + " Selected";
                            }
                        }
                    }
                    Donor_comboBox.SelectedIndex = -1;
                    Donor_comboBox.ItemsSource = null;
                    Donor_comboBox.Text = "0 Selected";
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

        private void ExchangeAcc_Checkbox_Item_Clicked(object sender, RoutedEventArgs e)
        {
            int count = objExchangeAccList.Count(item => item.ExchangeAcc_Check_Status == true);
            ExchangeAccount_comboBox.Text = count.ToString() + " Selected";
        }

        private void City_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                City_comboBox.SelectedIndex = -1;
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

        private void AddAdminAcc_btn_Click(object sender, RoutedEventArgs e)
        {
            AddAdminAcc_btn.IsEnabled = false;
            Save_btn.IsEnabled = false;
            try
            {

                if (Acc_ID != -1)
                {
                    List<string> Auto_Tills = t_dt.AsEnumerable()
                                                   .Select(r => r.Field<string>("Type"))
                                                   .ToList();
                    List<string> Add_donors = new List<string>();

                    //bool Found;

                    if (Original_objDonorList != null && Original_objDonorList.Count > 0)
                        foreach (var donor in Original_objDonorList)
                        {
                            if (donor.Check_Status == true)
                            {

                                Add_donors.Add(donor.Donor_ID.ToString());
                            }
                        }


                    int Admin_Acc_ID = a.Insert_Administrative_Account(Acc_ID, Add_donors, Auto_Tills, "Account_Window");
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);



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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
            finally
            {
                AddAdminAcc_btn.IsEnabled = true;
                Save_btn.IsEnabled = true;
            }
        }

        private void Project_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Project_comboBox.SelectedIndex = -1;

                if (AccType != "Cash")
                {
                    SubProject_comboBox.SelectedIndex = -1;
                    SubProject_comboBox.ItemsSource = null;
                    SubProject_comboBox.Text = "0 Selected";
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

        private void SubProject_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objSubProjectList != null && objSubProjectList.Count > 0)
                {
                    foreach (var SubProjec in objSubProjectList)
                    {
                        SubProjec.Sub_Check_Status = false;
                    }
                }

                SubProject_comboBox.ItemsSource = null;
                SubProject_comboBox.Text = "0 Selected";
                SubProject_comboBox.ItemsSource = objSubProjectList;
                if (objSubProjectList != null)
                {
                    int count = objSubProjectList.Count(item => item.Sub_Check_Status == true);
                    SubProject_comboBox.Text = count.ToString() + " Selected";
                }

                Donor_comboBox.SelectedIndex = -1;
                Donor_comboBox.ItemsSource = null;
                Donor_comboBox.Text = "0 Selected";

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

        private void Donor_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objDonorList != null && objDonorList.Count > 0)
                {
                    foreach (var donor in objDonorList)
                    {
                        donor.Check_Status = false;
                    }
                }

                Donor_comboBox.ItemsSource = null;
                Donor_comboBox.Text = "0 Selected";
                Donor_comboBox.ItemsSource = objDonorList;
                if (objDonorList != null)
                {
                    int count = objDonorList.Count(item => item.Check_Status == true);
                    Donor_comboBox.Text = count.ToString() + " Selected";
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

        private void Currency_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Currency_comboBox.SelectedIndex = -1;
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

        private void TotalAmount_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TotalAmount_txtBox.Text != "")
                {
                    TotalAmount_txtBox.Text = Login.ConvertToDouble(TotalAmount_txtBox.Text);
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
            AddAdminAcc_btn.IsEnabled = false;

            try
            {
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the account name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Please enter the account name.");
                }

                if (AccType == "Project")
                {
                    if (Project_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select the project.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                        //throw new Exception("Please select the project.");
                    }

                    int SubFound = 0;
                    if (objSubProjectList != null && objSubProjectList.Count > 0)
                    {
                        foreach (var subProject in objSubProjectList)
                        {
                            if (subProject.Sub_Check_Status == true)
                            {
                                SubFound++;
                            }
                        }
                    }

                    if (SubFound == 0)
                    {
                        MessageBox.Show("Please select the project.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                        //throw new Exception("Please select the center.");

                    }

                    else if (SubFound > 1)
                    {
                        MessageBox.Show("You can not select more than one project for this type of programs.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                        //throw new Exception("You cannot select more than one center for this type of projects.");
                    }


                    int DononrFound = 0;
                    if (objDonorList != null && objDonorList.Count > 0)
                    {
                        foreach (var donor in objDonorList)
                        {
                            if (donor.Check_Status == true)
                            {
                                DononrFound++;
                            }
                        }
                    }

                    if (DononrFound == 0)
                    {
                        MessageBox.Show("Please select the donor.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                        //throw new Exception("Please select the donor.");
                    }

                    else if (DononrFound > 1 && Category == "Micro Projects")
                    {
                        MessageBox.Show("You can not select more than one donor for this type of programs.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                        //throw new Exception("You cannot select more than one donor for this type of projects.");

                    }



                    if (BeginDate_datepicker.SelectedDate == null)
                    {
                        MessageBox.Show("Please specify the start date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                        //throw new Exception("Please specify the start date.");
                    }
                }
                else if (AccType == "Cash")
                {
                    if (City_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select the city", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }

                    if (Currency_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select the currency", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }

                    int SubFound = 0;
                    if (objSubProjectList != null)
                        foreach (var subProject in objSubProjectList)
                        {
                            if (subProject.Sub_Check_Status == true)
                            {
                                SubFound++;
                            }
                        }
                    int PersonalFound = 0;
                    if (objPersonalAccList != null)
                        foreach (var PersonalAcc in objPersonalAccList)
                        {
                            if (PersonalAcc.PersonalAcc_Check_Status == true)
                            {
                                PersonalFound++;
                            }
                        }
                    int ExchangeFound = 0;
                    if (objExchangeAccList != null)
                        foreach (var ExchangeAcc in objExchangeAccList)
                        {
                            if (ExchangeAcc.ExchangeAcc_Check_Status == true)
                            {
                                ExchangeFound++;
                            }
                        }
                    if (SubFound == 0 && PersonalFound == 0 && ExchangeFound == 0)
                    {
                        MessageBox.Show("Please select a project, personal account, or exchange account at least", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                        //throw new Exception("Please select a center, personal account, or cash till at least");
                    }
                }
                else if (AccType == "Personal")
                {
                    if (City_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select the city", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }

                    if (BeginDate_datepicker.SelectedDate == null)
                    {
                        MessageBox.Show("Please specify the start date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                        //throw new Exception("Please specify the start date.");
                    }
                }
                else if (AccType == "Exchange")
                {
                    if (City_comboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select the city", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                }


                string Name = Login.ReplaceQuotation(Name_txtBox.Text);
                string ProjectNumber = "";
                string beginDate = "NULL";
                string endDate = "NULL";
                float totalAmount = 0;
                int amountType_ID = -1;
                int City_ID = -1;
                int Currency_ID = -1;
                List<string> Add_donors = new List<string>();
                List<string> Delete_donors = new List<string>();


                List<string> Add_SubProject = new List<string>();
                List<string> Delete_SubProject = new List<string>();


                List<string> Add_Acc = new List<string>();
                List<string> Delete_Acc = new List<string>();


                if (AccType == "Project")
                {
                    if (Category == "Micro Projects" || Category == "Budget")
                    {
                        Project_ID = Convert.ToInt32(Project_comboBox.SelectedValue);
                        SubProject_ID = Convert.ToInt32(SubProject_comboBox.SelectedValue);

                        if (objSubProjectList != null && objSubProjectList.Count > 0)
                            foreach (var subProject in objSubProjectList)
                            {
                                if (subProject.Sub_Check_Status == true)
                                {
                                    SubProject_ID = subProject.SubID;
                                    break;
                                }
                            }

                        bool Found;

                        if (objDonorList != null && objDonorList.Count > 0)
                            foreach (var donor in objDonorList)
                            {
                                if (donor.Check_Status == true)
                                {
                                    Found = false;
                                    if (Original_objDonorList != null && Original_objDonorList.Count > 0)
                                        foreach (var Originaldonor in Original_objDonorList)
                                        {
                                            if (Originaldonor.Donor_ID == donor.Donor_ID && Originaldonor.Check_Status == true)
                                            {
                                                Found = true;
                                                break;
                                            }
                                        }
                                    if (Found == false)
                                        Add_donors.Add(donor.Donor_ID.ToString());
                                }
                            }

                        if (Original_objDonorList != null && Original_objDonorList.Count > 0)
                            foreach (var Originaldonor in Original_objDonorList)
                            {
                                if (Originaldonor.Check_Status == true)
                                {
                                    Found = false;
                                    if (objDonorList != null && objDonorList.Count > 0)
                                        foreach (var donor in objDonorList)
                                        {
                                            if (Originaldonor.Donor_ID == donor.Donor_ID && donor.Check_Status == true)
                                            {
                                                Found = true;
                                                break;
                                            }
                                        }
                                    if (Found == false)
                                        Delete_donors.Add(Originaldonor.Donor_ID.ToString());
                                }
                            }


                        ProjectNumber = Login.ReplaceQuotation(ProjectNumber_txtBox.Text);
                        beginDate = ((DateTime)BeginDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                        if (EndDate_CheckBox.IsChecked == true) //&& EndDate_datepicker.IsEnabled == false
                            endDate = "NULL";
                        else endDate = ((DateTime)EndDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                        string text = TotalAmount_txtBox.Text.Replace(",", "");
                        totalAmount = (text == "" ? 0 : float.Parse(text));


                        if (AmountType_comboBox.SelectedIndex != -1)
                            amountType_ID = Convert.ToInt32(AmountType_comboBox.SelectedValue);
                        else
                            amountType_ID = -1;
                    }
                }
                else
                {
                    Project_ID = -1;
                    ProjectNumber = "";
                    beginDate = "NULL";
                    endDate = "NULL";
                    totalAmount = 0;
                    amountType_ID = -1;
                    City_ID = Convert.ToInt32(City_comboBox.SelectedValue);
                    Currency_ID = Convert.ToInt32(Currency_comboBox.SelectedValue);
                    if (AccType == "Cash")
                    {
                        bool Found;

                        if (objSubProjectList != null && objSubProjectList.Count > 0)
                            foreach (var subProject in objSubProjectList)
                            {
                                if (subProject.Sub_Check_Status == true)
                                {
                                    Found = false;
                                    if (Original_SubProjectList != null && Original_SubProjectList.Count > 0)
                                        foreach (var OriginalSubProject in Original_SubProjectList)
                                        {
                                            if (OriginalSubProject.SubID == subProject.SubID && OriginalSubProject.Sub_Check_Status == true)
                                            {
                                                Found = true;
                                                break;
                                            }
                                        }


                                    if (Found == false)
                                        Add_SubProject.Add(subProject.SubID.ToString());
                                }
                            }

                        if (Original_SubProjectList != null && Original_SubProjectList.Count > 0)
                            foreach (var OriginalSubProject in Original_SubProjectList)
                            {
                                if (OriginalSubProject.Sub_Check_Status == true)
                                {
                                    Found = false;
                                    if (objSubProjectList != null && objSubProjectList.Count > 0)
                                        foreach (var subProject in objSubProjectList)
                                        {
                                            if (OriginalSubProject.SubID == subProject.SubID && subProject.Sub_Check_Status == true)
                                            {
                                                Found = true;
                                                break;
                                            }
                                        }
                                    if (Found == false)
                                        Delete_SubProject.Add(OriginalSubProject.SubID.ToString());
                                }
                            }

                        if (objPersonalAccList != null && objPersonalAccList.Count > 0)
                            foreach (var PersonalAcc in objPersonalAccList)
                            {
                                if (PersonalAcc.PersonalAcc_Check_Status == true)
                                {
                                    Found = false;
                                    if (Original_PersonalAccList != null && Original_PersonalAccList.Count > 0)
                                        foreach (var OriginalPersonalAcc in Original_PersonalAccList)
                                        {
                                            if (OriginalPersonalAcc.PersonalAcc_ID == PersonalAcc.PersonalAcc_ID && OriginalPersonalAcc.PersonalAcc_Check_Status == true)
                                            {
                                                Found = true;
                                                break;
                                            }
                                        }
                                    if (Found == false)
                                        Add_Acc.Add(PersonalAcc.PersonalAcc_ID.ToString());
                                }
                            }

                        if (Original_PersonalAccList != null && Original_PersonalAccList.Count > 0)
                            foreach (var OriginalPersonalAcc in Original_PersonalAccList)
                            {
                                if (OriginalPersonalAcc.PersonalAcc_Check_Status == true)
                                {
                                    Found = false;
                                    if (objPersonalAccList != null && objPersonalAccList.Count > 0)
                                        foreach (var PersonalAcc in objPersonalAccList)
                                        {
                                            if (OriginalPersonalAcc.PersonalAcc_ID == PersonalAcc.PersonalAcc_ID && PersonalAcc.PersonalAcc_Check_Status == true)
                                            {
                                                Found = true;
                                                break;
                                            }
                                        }
                                    if (Found == false)
                                        Delete_Acc.Add(OriginalPersonalAcc.PersonalAcc_ID.ToString());
                                }
                            }

                        if (objExchangeAccList != null && objExchangeAccList.Count > 0)
                            foreach (var ExchangelAcc in objExchangeAccList)
                            {
                                if (ExchangelAcc.ExchangeAcc_Check_Status == true)
                                {
                                    Found = false;
                                    if (Original_ExchangeAccList != null && Original_ExchangeAccList.Count > 0)
                                        foreach (var OriginalExchangeAcc in Original_ExchangeAccList)
                                        {
                                            if (OriginalExchangeAcc.ExchangeAcc_ID == ExchangelAcc.ExchangeAcc_ID && OriginalExchangeAcc.ExchangeAcc_Check_Status == true)
                                            {
                                                Found = true;
                                                break;
                                            }
                                        }
                                    if (Found == false)
                                        Add_Acc.Add(ExchangelAcc.ExchangeAcc_ID.ToString());
                                }
                            }

                        if (Original_ExchangeAccList != null && Original_ExchangeAccList.Count > 0)
                            foreach (var OriginalExchangeAcc in Original_ExchangeAccList)
                            {
                                if (OriginalExchangeAcc.ExchangeAcc_Check_Status == true)
                                {
                                    Found = false;
                                    if (objExchangeAccList != null && objExchangeAccList.Count > 0)
                                        foreach (var ExchangelAcc in objExchangeAccList)
                                        {
                                            if (OriginalExchangeAcc.ExchangeAcc_ID == ExchangelAcc.ExchangeAcc_ID && ExchangelAcc.ExchangeAcc_Check_Status == true)
                                            {
                                                Found = true;
                                                break;
                                            }
                                        }
                                    if (Found == false)
                                        Delete_Acc.Add(OriginalExchangeAcc.ExchangeAcc_ID.ToString());
                                }
                            }
                    }
                    else if (AccType == "Personal")
                    {
                        SubProject_ID = -1;
                        beginDate = ((DateTime)BeginDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                        if (EndDate_CheckBox.IsChecked == true) //&& EndDate_datepicker.IsEnabled == false
                            endDate = "NULL";
                        else endDate = ((DateTime)EndDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                    }
                    else
                    {
                        SubProject_ID = -1;
                    }

                }
                if (Acc_ID != -1)//Edit
                {
                    if (AccType != "Cash")
                    {
                        a.Update(Acc_ID, Name, ProjectNumber, beginDate, endDate, totalAmount, amountType_ID, City_ID, IsClosed, SubProject_ID, Add_donors, Delete_donors, "Account_Window");
                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    }
                    else
                    {
                        a.Update_Cash_Account(Acc_ID, Name, Add_SubProject, Delete_SubProject, Add_Acc, Delete_Acc, City_ID ,"Account_Window");
                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    }

                }
                else//Add new
                {
                    if (AccType != "Cash")
                    {
                        List<string> Auto_Tills = new List<string>();

                        if (AccType == "Project" || AccType == "Exchange")
                        {
                            Auto_Tills.Add("syr");
                            Auto_Tills.Add("usd");
                        }
                        else if (AccType == "Personal")
                        {
                            Auto_Tills.Add("syr");
                        }

                        Acc_ID = a.Insert(Name, ProjectNumber, beginDate, endDate, totalAmount, amountType_ID, City_ID, IsClosed, SubProject_ID, Add_donors, Auto_Tills, "Account_Window", AccType);
                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                        tabControl1.Visibility = Visibility.Visible; //Update//
                    }
                    else
                    {
                        Acc_ID = a.Insert_Cash_Account(Name, Add_SubProject, Add_Acc, City_ID, Currency_ID ,"Account_Window");
                        MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                        tabControl1.Visibility = Visibility.Visible; //Update//
                    }
                }

                Clear_Boxes();
                Load_Data();

                if (Acc_ID != -1)
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        int i = dt.Rows.Count - 1;

                        if (dt.Rows[i]["Name"] != DBNull.Value)
                        {
                            TabItem oTab = main_Window.main_tabControl.Items[main_Window.main_tabControl.SelectedIndex] as TabItem;
                            oTab.Header = dt.Rows[i].Field<string>("Name");
                        }
                    }


                }

                main_Window.Fill_TreeView(true);

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
                AddAdminAcc_btn.IsEnabled = true;
            }
        }

        private void Donor_Checkbox_Item_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AccType == "Project" && Category == "Micro Projects")
                {
                    FilteredobjDonorList = new List<SubProject_Donor>();
                    if (objDonorList != null && objDonorList.Count > 0)
                    {
                        foreach (var donor in objDonorList)
                        {
                            if (donor.Check_Status == true)
                                FilteredobjDonorList.Add(donor);
                        }

                        if (FilteredobjDonorList.Count > 0)
                        {
                            Donor_comboBox.ItemsSource = null;
                            Donor_comboBox.Text = "0 Selected";
                            Donor_comboBox.ItemsSource = FilteredobjDonorList;
                            int count = objDonorList.Count(item => item.Check_Status == true);
                            Donor_comboBox.Text = count.ToString() + " Selected";
                        }
                        else
                        {
                            Donor_comboBox.ItemsSource = null;
                            Donor_comboBox.Text = "0 Selected";
                            Donor_comboBox.ItemsSource = objDonorList;
                            int count = objDonorList.Count(item => item.Check_Status == true);
                            Donor_comboBox.Text = count.ToString() + " Selected";
                        }
                    }
                    else
                    {
                        Donor_comboBox.ItemsSource = null;
                        Donor_comboBox.Text = "0 Selected";
                        Donor_comboBox.ItemsSource = objDonorList;
                        int count = objDonorList.Count(item => item.Check_Status == true);
                        Donor_comboBox.Text = count.ToString() + " Selected";
                    }
                }

                int DonorCount = objDonorList.Count(item => item.Check_Status == true);
                Donor_comboBox.Text = DonorCount.ToString() + " Selected";
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
        private void Currency_Checkbox_Item_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AccType == "Project" && Category == "Micro Projects")
                {
                    FilteredobjDonorList = new List<SubProject_Donor>();
                    if (objDonorList != null && objDonorList.Count > 0)
                    {
                        foreach (var donor in objDonorList)
                        {
                            if (donor.Check_Status == true)
                                FilteredobjDonorList.Add(donor);
                        }

                        if (FilteredobjDonorList.Count > 0)
                        {
                            Donor_comboBox.ItemsSource = null;
                            Donor_comboBox.Text = "0 Selected";
                            Donor_comboBox.ItemsSource = FilteredobjDonorList;
                            int count = objDonorList.Count(item => item.Check_Status == true);
                            Donor_comboBox.Text = count.ToString() + " Selected";
                        }
                        else
                        {
                            Donor_comboBox.ItemsSource = null;
                            Donor_comboBox.Text = "0 Selected";
                            Donor_comboBox.ItemsSource = objDonorList;
                            int count = objDonorList.Count(item => item.Check_Status == true);
                            Donor_comboBox.Text = count.ToString() + " Selected";
                        }
                    }
                    else
                    {
                        Donor_comboBox.ItemsSource = null;
                        Donor_comboBox.Text = "0 Selected";
                        Donor_comboBox.ItemsSource = objDonorList;
                        int count = objDonorList.Count(item => item.Check_Status == true);
                        Donor_comboBox.Text = count.ToString() + " Selected";
                    }
                }

                int DonorCount = objDonorList.Count(item => item.Check_Status == true);
                Donor_comboBox.Text = DonorCount.ToString() + " Selected";
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

        private void PersonalAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objPersonalAccList != null && objPersonalAccList.Count > 0)
                {
                    foreach (var PersonalAcc in objPersonalAccList)
                    {
                        PersonalAcc.PersonalAcc_Check_Status = false;
                    }
                }

                PersonalAccount_comboBox.ItemsSource = null;
                PersonalAccount_comboBox.Text = "0 Selected";
                PersonalAccount_comboBox.ItemsSource = objPersonalAccList;
                if (objPersonalAccList != null)
                {
                    int count = objPersonalAccList.Count(item => item.PersonalAcc_Check_Status == true);
                    PersonalAccount_comboBox.Text = count.ToString() + " Selected";
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

        private void ExchangeAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objExchangeAccList != null && objExchangeAccList.Count > 0)
                {
                    foreach (var ExAcc in objExchangeAccList)
                    {
                        ExAcc.ExchangeAcc_Check_Status = false;
                    }
                }

                ExchangeAccount_comboBox.ItemsSource = null;
                ExchangeAccount_comboBox.Text = "0 Selected";
                ExchangeAccount_comboBox.ItemsSource = objExchangeAccList;
                if (objExchangeAccList != null)
                {
                    int count = objExchangeAccList.Count(item => item.ExchangeAcc_Check_Status == true);
                    ExchangeAccount_comboBox.Text = count.ToString() + " Selected";
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

        private void SubProject_Checkbox_Item_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AccType != "Cash")
                {
                    FilteredSubProjectList = new List<Local_SubProject>();
                    if (objSubProjectList != null && objSubProjectList.Count > 0)
                    {
                        foreach (var subProject in objSubProjectList)
                        {
                            if (subProject.Sub_Check_Status == true)
                                FilteredSubProjectList.Add(subProject);
                        }
                    }

                    if (FilteredSubProjectList != null && FilteredSubProjectList.Count > 0)
                    {
                        SubProject_comboBox.ItemsSource = null;
                        SubProject_comboBox.Text = "0 Selected";
                        SubProject_comboBox.ItemsSource = FilteredSubProjectList;
                        int count = objSubProjectList.Count(item => item.Sub_Check_Status == true);
                        SubProject_comboBox.Text = count.ToString() + " Selected";
                    }
                    else
                    {
                        SubProject_comboBox.ItemsSource = null;
                        SubProject_comboBox.Text = "0 Selected";
                        SubProject_comboBox.ItemsSource = objSubProjectList;
                        int count = objSubProjectList.Count(item => item.Sub_Check_Status == true);
                        SubProject_comboBox.Text = count.ToString() + " Selected";
                    }

                    Update_Donor_ComboBox();
                }

                int SubProjectCount = objSubProjectList.Count(item => item.Sub_Check_Status == true);
                SubProject_comboBox.Text = SubProjectCount.ToString() + " Selected";
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

        private void Update_Donor_ComboBox()
        {
            Donor_comboBox.ItemsSource = null;
            Donor_comboBox.Text = "0 Selected";
            string IDs = "";
            if (objSubProjectList != null && objSubProjectList.Count > 0)
            {
                foreach (var subproject in objSubProjectList)
                {
                    if (subproject.Sub_Check_Status == true)
                    {
                        IDs += subproject.SubID.ToString() + ",";
                    }
                }
            }


            if (IDs != "")
            {
                IDs = IDs.Remove(IDs.Length - 1);

                DataRow[] rows = donor_dt.Select("SubProject_ID in (" + IDs + ")");
                DataTable selected_dt = new DataTable();
                if (rows.Length != 0)
                {
                    selected_dt = rows.CopyToDataTable();
                    if (selected_dt != null && selected_dt.Rows.Count > 0)
                    {
                        objDonorList = new List<SubProject_Donor>();
                        for (int i = 0; i < selected_dt.Rows.Count; i++)
                        {
                            SubProject_Donor obj = new SubProject_Donor();
                            obj.Donor_ID = selected_dt.Rows[i].Field<int>(0);
                            obj.DonorName = selected_dt.Rows[i].Field<string>(1);
                            obj.SubProject_ID = selected_dt.Rows[i].Field<int>(2);
                            objDonorList.Add(obj);
                        }
                        Donor_comboBox.ItemsSource = objDonorList;
                        int count = objDonorList.Count(item => item.Check_Status == true);
                        Donor_comboBox.Text = count.ToString() + " Selected";

                    }
                }
            }
        }

        private void EndDate_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                EndDate_datepicker.IsEnabled = false;
                if (UserMode)
                {
                    EndDate_datepicker.Text = "";
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

        private void EndDate_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                EndDate_datepicker.IsEnabled = true;
                if (UserMode)
                {
                    EndDate_datepicker.SelectedDate = DateTime.Now;
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

        private void myExpander_Expanded(object sender, RoutedEventArgs e)
        {
            myExpander.Width = double.NaN;
            //if (_currentWindowState == WindowState.Normal)
            //{
            //    First_col.Width = new GridLength(220, GridUnitType.Star);
            //    Second_col.Width = new GridLength(160, GridUnitType.Star);
            //}
            //else if (_currentWindowState == WindowState.Maximized)
            //{
            //    First_col.Width = new GridLength(85, GridUnitType.Star);
            //    Second_col.Width = new GridLength(45, GridUnitType.Star);
            //}

        }

        private void myExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            myExpander.Width = 30;
            //    First_col.Width = new GridLength(97, GridUnitType.Star);
            //    Second_col.Width = new GridLength(2.3, GridUnitType.Star);
        }

        private void IsClosed_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsClosed_btn.Content.ToString() == "Open")
                {
                    IsClosed_btn.Content = "Closed";
                    //SolidColorBrush brush2 = (SolidColorBrush)App.Current.Resources["HopeRed2"];
                    //IsClosed_btn.Foreground = brush2;
                    IsClosed = 1;
                }
                else
                {
                    IsClosed_btn.Content = "Open";
                    //SolidColorBrush brush2 = (SolidColorBrush)App.Current.Resources["HopeBlue4"];
                    //IsClosed_btn.Foreground = brush2;
                    IsClosed = 0;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Clear_Boxes()
        {
            Project_comboBox.SelectedIndex = -1;
            SubProject_comboBox.SelectedIndex = -1;
            Donor_comboBox.SelectedIndex = -1;
            Name_txtBox.Text = ProjectNumber_txtBox.Text = "";
            BeginDate_datepicker.SelectedDate = DateTime.Now;
            EndDate_datepicker.SelectedDate = null;
            EndDate_CheckBox.IsChecked = true;
            TotalAmount_txtBox.Text = "";
            AmountType_comboBox.SelectedIndex = -1;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EndDate_CheckBox.IsChecked = !EndDate_CheckBox.IsChecked;
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
