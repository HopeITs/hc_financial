using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
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
    /// Interaction logic for Add_Payment_Window.xaml
    /// </summary>
    public partial class Add_Payment_Window : Window
    {
        public Add_Payment_Window(int SubProject_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.SubProject_ID = SubProject_ID;
            this.Action_ID = -1;
            this.Approved_Action = false;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Add_Payment_Window(int SubProject_ID, int Action_ID, bool Approved_Action, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.SubProject_ID = SubProject_ID;
            this.Action_ID = Action_ID;
            this.Approved_Action = Approved_Action;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }
        private int SubProject_ID, Beneficiary_ID;
        Main_Window_2 main_Window;
        private Beneficiary b; private Account Acc; private Payment p;

        int MicroProject_ID = -1, Action_ID =-1, Till_ID = -1;
        bool Approved_Action;

        private DataTable b_dt, p_dt, a_dt;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["HopeRed"];
        string HeaderStyle_Name = "HopeRed_SfDataGrid_header";
        string TableSummaryRowStyle_Name = "HopeRed_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "HopeRed_GroupSummaryRowStyle";

        private bool OnLoad()
        {
            try
            {
                b = new Beneficiary();
                //t = new Till();
                //a = new Models.Action();
                p = new Payment();
                Acc = new Account();

                MircoProject_ID_autoSuggestionUserControl.PreviewTextInput += InputValidationHelper.Integers_NumberValidationTextBox;
                Amount_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;

                for (int i = 0; i < payDataGrid.Columns.Count; i++)
                {
                    payDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                    payDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
                }

                Load_Data();
                Apply_Color_Theme();
                Apply_DataGrid_Color_Theme();
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
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

        private void Apply_DataGrid_Color_Theme()
        {
            payDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            payDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            payDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }
        private void Load_Data()
        {
            Date_datepicker.SelectedDate = DateTime.Today;
            Fill_Beneficiaries();
            Fill_ComboBoxses();

            if (Action_ID != -1)
            {
                Fill_Action();
            }
        }

        private void Fill_Beneficiaries()
        {
            if (Login.BuildConnection())
            {
                DataTable temp_dt = b.Select("", "", SubProject_ID.ToString());
                if (Settings.Default.Role == "Accountant")
                {
                    List<int> UserAccounts_ID = new List<int>();
                    if (Settings.Default.User_Accounts != null && !string.IsNullOrEmpty(Settings.Default.User_Accounts.ToString()) && !string.IsNullOrWhiteSpace(Settings.Default.User_Accounts.ToString()))
                    {
                        UserAccounts_ID = Settings.Default.User_Accounts.ToString().Split(',').Select(int.Parse).ToList();

                        string filterExpression = string.Join(" OR ", UserAccounts_ID.Select(id => "Account_ID = " + id));
                        System.Data.DataRow[] rows = temp_dt.Select(filterExpression);
                        DataTable selected_dt = new DataTable();
                        if (rows.Length != 0)
                        {
                            selected_dt = rows.CopyToDataTable();
                            b_dt = selected_dt;

                        }
                    }
                    else
                    {
                        b_dt = null;
                    }

                }
                else
                {
                    b_dt = temp_dt;
                }
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
                    BeneficiaryName_autoSuggestionUserControl.AutoSuggestionList = Nametems;
                }
            }
        }

        private void Fill_ComboBoxses()
        {
            if (Login.BuildConnection())
            {
                DataTable cashAccounts_dt = Acc.Select_Cash_Account("", SubProject_ID.ToString());

                if (cashAccounts_dt != null && cashAccounts_dt.Rows.Count > 0)
                {
                    CashAccount_comboBox.ItemsSource = cashAccounts_dt.DefaultView;
                }
            }
        }

        private void Fill_Action()
        {
            if (Approved_Action)
            {
                a_dt = p.Select_All_Payments(SubProject_ID.ToString(), Action_ID,"","",-1);
            }
            else
            {
                a_dt = p.Select_All_Not_Approved_Payments(SubProject_ID.ToString(), false, Action_ID,"","",-1);
            }

            if (a_dt.Rows[0]["MicroProject_ID"] != DBNull.Value)
            {

                MicroProject_ID = Convert.ToInt32(a_dt.Rows[0].Field<int>("MicroProject_ID").ToString());
                Till_ID = Convert.ToInt32(a_dt.Rows[0].Field<int>("Till_ID").ToString());


                MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = MicroProject_ID.ToString();
                MircoProject_ID_autoSuggestionUserControl.CloseAutoSuggestionBox();

                RoutedEventArgs eventArgs = new RoutedEventArgs();
                MircoProject_ID_autoSuggestionUserControl_LostFocus(MircoProject_ID_autoSuggestionUserControl, eventArgs);


                MircoProject_ID_autoSuggestionUserControl.IsEnabled = false;
                BeneficiaryName_autoSuggestionUserControl.IsEnabled = false;

                if (a_dt.Rows[0]["Income"] != DBNull.Value)
                {
                    Amount_txtBox.Text = string.Format("{0:N2}", a_dt.Rows[0].Field<Double>("Income"));
                }

                if (a_dt.Rows[0]["Note"] != DBNull.Value)
                {
                    Notes_txtBox.Text = a_dt.Rows[0].Field<string>("Note").ToString();
                }

                if (a_dt.Rows[0]["cashaccount_id"] != DBNull.Value)
                {
                    CashAccount_comboBox.SelectedValue = Convert.ToInt32(a_dt.Rows[0].Field<int>("cashaccount_id").ToString());
                }
                else CashAccount_comboBox.SelectedIndex = -1;


                if (a_dt.Rows[0]["ActionDate"] != DBNull.Value)
                    Date_datepicker.SelectedDate = Convert.ToDateTime(a_dt.Rows[0].Field<DateTime>("ActionDate").ToString());
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
        }

        private void payDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (payDataGrid.View != null)
            {
                Export_btn2.CommandTarget = payDataGrid;
            }
        }

        private void CashAccount_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            CashAccount_comboBox.SelectedIndex = -1;
        }

        private void Amount_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                string value = Amount_txtBox.Text;
                if (value != "")
                {
                    Amount_txtBox.Text = Login.ConvertToDouble(value);
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

        private void MircoProject_ID_autoSuggestionUserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                string MProject_ID = MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text;
                if (!string.IsNullOrWhiteSpace(MProject_ID))
                {
                    MicroProject_ID = int.Parse(MProject_ID);
                    System.Data.DataRow[] rows = b_dt.Select("MicroProject_ID=" + MicroProject_ID);

                    if (rows.Length != 0)
                    {
                        Fill_Details();
                    }
                    else
                    {
                        Clear_Details(true);
                    }
                }
                else
                {
                    Clear_Details(true);
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

        private void BeneficiaryName_autoSuggestionUserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                string Name = BeneficiaryName_autoSuggestionUserControl.autoTextBox.Text;
                System.Data.DataRow[] rows = b_dt.Select("Name like '%" + Name + "%'");
                if (string.IsNullOrWhiteSpace(Name) || rows.Length == 0)
                {
                    MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = "";
                    MircoProject_ID_autoSuggestionUserControl.CloseAutoSuggestionBox();
                }
                else
                {
                    MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = rows[0].Field<Int32>("MicroProject_ID").ToString();
                    MircoProject_ID_autoSuggestionUserControl.CloseAutoSuggestionBox();
                }

                RoutedEventArgs eventArgs = new RoutedEventArgs();
                MircoProject_ID_autoSuggestionUserControl_LostFocus(MircoProject_ID_autoSuggestionUserControl, eventArgs);
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

        private void Save2_btn_Click(object sender, RoutedEventArgs e)
        {
            Save2_btn.IsEnabled = false;
            try
            {
                if (Amount_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the amount.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Beneficiary_ID <= 0)
                {
                    MessageBox.Show("Please select the beneficiary.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }


                if (Date_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please specify the payment date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (CashAccount_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the cash till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Login.BuildConnection())
                {
                    string amount_str, Date;
                    double amount = 0;
                    int CashAccount_ID;

                    amount_str = Amount_txtBox.Text.Replace(",", "");
                    Double.TryParse(amount_str, out amount);

                    Date = ((DateTime)(Date_datepicker.SelectedDate)).ToString("yyyy/MM/dd");
                    CashAccount_ID = Convert.ToInt32(CashAccount_comboBox.SelectedValue);

                    if (Action_ID != -1)//Edit
                    {
                        if (Approved_Action)
                        {
                            p.Update_Payment(Action_ID, Till_ID, amount, Date, Notes_txtBox.Text, MicroProject_ID, Beneficiary_ID, CashAccount_ID, "Payment_Window");
                            MessageBox.Show("Payment updated", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            this.Close();
                        }
                        else
                        {
                            p.Update_Not_Approved_Payment(Action_ID, Till_ID, amount, Date, Notes_txtBox.Text, MicroProject_ID, Beneficiary_ID, CashAccount_ID, "Payment_Window");
                            MessageBox.Show("Payment updated", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                            this.Close();
                        }
                    }
                    else//Add new
                    {
                        if (p_dt.Rows.Count != 0)
                        {
                            DataTable selected_rows_dt = new DataTable();

                            System.Data.DataRow[] My_rows = p_dt.Select("payment_state is null OR payment_state <> 'Done'");
                            selected_rows_dt = p_dt.Copy();
                            selected_rows_dt.Rows.Clear();
                            foreach (System.Data.DataRow row in My_rows)
                                selected_rows_dt.ImportRow(row);

                            p.Insert_Payment(amount, Date, Notes_txtBox.Text, MicroProject_ID, SubProject_ID, Beneficiary_ID, selected_rows_dt, CashAccount_ID, "Payment_Window");
                            MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                            Clear_Details(false);
                            Load_Data();
                            Fill_Details();
                        }
                        else
                        {
                            MessageBox.Show("No payments have been generated for this beneficiary.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        }

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
                Save2_btn.IsEnabled = true;
            }
        }

        private void Clear_Details(bool All)
        {
            if (All)
            {
                Beneficiary_ID = -1;
                Action_ID = Till_ID = MicroProject_ID = -1;
                Approved_Action = false;
                MircoProject_ID_autoSuggestionUserControl.autoTextBox.Text = BeneficiaryName_autoSuggestionUserControl.autoTextBox.Text = "";
                payDataGrid.ItemsSource = null;
            }

            Amount_txtBox.Clear();
            Notes_txtBox.Clear();
            CashAccount_comboBox.SelectedIndex = -1;
            Date_datepicker.SelectedDate = DateTime.Now;

            LoanAmount_txtBox.Content = "";
            AmountAowed_txtBox.Content = "";
            AmountPaid_txtBox.Content = "";
            RemainingAmount_txtBox.Content = "";
            NotApprovedAmount_txtBox.Content = "";

            TotalPaymentCount_txtBox.Content = "";
            PaidPaymentCount_txtBox.Content = "";
            UnpaidPaymentCount_txtBox.Content = "";
            InComplete_txtBox.Content = "";
            DelayedPaymentCont_txtBox.Content = "";
            NotApprovedPaymentCont_txtBox.Content = "";
        }

        private void Fill_Details()
        {
            System.Data.DataRow[] rows = b_dt.Select("MicroProject_ID=" + MicroProject_ID);
            Beneficiary_ID = rows[0].Field<Int32>("ID");

            BeneficiaryName_autoSuggestionUserControl.autoTextBox.Text = rows[0].Field<string>("Name");
            BeneficiaryName_autoSuggestionUserControl.CloseAutoSuggestionBox();

            Fill_Payments();
        }

        private void Fill_Payments()
        {
            if (Login.BuildConnection())
            {
                p_dt = p.Select_Beneficiary_Payments(Beneficiary_ID.ToString(), "");
                if (p_dt != null && p_dt.Rows.Count > 0)
                {
                    payDataGrid.ItemsSource = p_dt.DefaultView;

                    payDataGrid.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));


                    Fill_Calculations();
                }
                else
                {
                    payDataGrid.ItemsSource = null;
                }
            }
        }

        private void Fill_Calculations()
        {
            int TotalPaymentCount, PaidPaymentCount, UnpaidPaymentCount, InCompletePaymentCont, DelayedPaymentCont, NotApprovedPaymentCont;

            TotalPaymentCount = p_dt.Rows.Count;
            PaidPaymentCount = Convert.ToInt32(p_dt.Select("payment_state='Done' and Approved = 'Yes'").Length.ToString());
            UnpaidPaymentCount = Convert.ToInt32(p_dt.Select("payment_state='Unpaid'").Length.ToString());
            InCompletePaymentCont = Convert.ToInt32(p_dt.Select("payment_state='Incomplete'").Length.ToString());
            DelayedPaymentCont = Convert.ToInt32(p_dt.Select("(Approved <> 'Yes' and DueDate < '" + DateTime.Now + "') or (payment_state <> 'Done' and DueDate < '" + DateTime.Now + "')").Length.ToString());
            NotApprovedPaymentCont = Convert.ToInt32(p_dt.Select("Approved = 'No'").Length.ToString());

            TotalPaymentCount_txtBox.Content = TotalPaymentCount.ToString();
            PaidPaymentCount_txtBox.Content = PaidPaymentCount.ToString();
            UnpaidPaymentCount_txtBox.Content = UnpaidPaymentCount.ToString();
            InComplete_txtBox.Content = InCompletePaymentCont.ToString();
            DelayedPaymentCont_txtBox.Content = DelayedPaymentCont.ToString();
            NotApprovedPaymentCont_txtBox.Content = NotApprovedPaymentCont.ToString();


            double LoanAmount, AmountAowed, AmountPaid, RemainingAmount, NotApprovedAmount;


            System.Data.DataRow[] rows = b_dt.Select("MicroProject_ID=" + MicroProject_ID);

            if (rows[0].Field<double?>("Amount") != null)
            {
                if (!double.TryParse(rows[0].Field<double>("Amount").ToString(), out LoanAmount))
                {
                    LoanAmount = 0;
                }
            }
            else
            {
                LoanAmount = 0;
            }

            if (rows[0].Field<double?>("ToReturn_Amount") != null)
            {
                if (!double.TryParse(rows[0].Field<double>("ToReturn_Amount").ToString(), out AmountAowed))
                {
                    AmountAowed = 0;
                }
            }
            else
            {
                AmountAowed = 0;
            }

            if (rows[0].Field<double?>("paidamount") != null)
            {
                if (!double.TryParse(rows[0].Field<double>("paidamount").ToString(), out AmountPaid))
                {
                    AmountPaid = 0;
                }
            }
            else
            {
                AmountPaid = 0;
            }


            if (rows[0].Field<double?>("NotApproved_paidamount") != null)
            {
                if (!double.TryParse(rows[0].Field<double>("NotApproved_paidamount").ToString(), out NotApprovedAmount))
                {
                    NotApprovedAmount = 0;
                }
            }
            else
            {
                NotApprovedAmount = 0;
            }

            RemainingAmount = AmountAowed - AmountPaid;

            LoanAmount_txtBox.Content = Login.ConvertToDouble(LoanAmount.ToString());
            AmountAowed_txtBox.Content = Login.ConvertToDouble(AmountAowed.ToString());
            AmountPaid_txtBox.Content = Login.ConvertToDouble(AmountPaid.ToString());
            NotApprovedAmount_txtBox.Content = Login.ConvertToDouble(NotApprovedAmount.ToString());
            RemainingAmount_txtBox.Content = Login.ConvertToDouble(RemainingAmount.ToString());


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
