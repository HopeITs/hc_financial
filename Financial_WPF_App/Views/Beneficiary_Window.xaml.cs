using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App
{
    public partial class Beneficiary_Window : Window
    {
        public Beneficiary_Window(int Beneficiary_ID, int SubProject_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Beneficiary_ID = Beneficiary_ID;
            this.SubProject_ID = SubProject_ID;
            this.Acc_ID = -1;
            GeneratePayments_btn.Visibility = Visibility.Visible;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Beneficiary_Window(string SubProject_ID, int Acc_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Beneficiary_ID = -1;
            this.SubProject_ID = Convert.ToInt32(SubProject_ID);
            this.Acc_ID = Acc_ID;
            GeneratePayments_btn.Visibility = Visibility.Collapsed;
            if (SubProject_ID != "") this.SubProject_ID = Convert.ToInt32(SubProject_ID);
            else this.SubProject_ID = -1;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private int Beneficiary_ID, SubProject_ID, Acc_ID;
        private Account a;
        private Beneficiary b;
        Payment p;
        private DataTable dt, a_dt, p_dt;
        string Original_ToReturn_Amount, Original_Payment, Original_Amount, Original_Payment_interval;
        DateTime? Original_FirstPaymentDate;
        int Original_MicroProject_ID;
        bool RegeneratePayments = false;
        bool DeleteSchedualedPayments = false;
        Main_Window_2 main_Window;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["HopeRed"];
        string HeaderStyle_Name = "HopeRed_SfDataGrid_header";
        string TableSummaryRowStyle_Name = "HopeRed_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "HopeRed_GroupSummaryRowStyle";

        private bool OnLoad()
        {
            try
            {
                a = new Account();
                b = new Beneficiary(); p = new Payment();
                a_dt = new DataTable();

                Amount_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
                ToReturn_Amount_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
                Payment_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
                Payment_interval_TextBox.PreviewTextInput += InputValidationHelper.Integers_NumberValidationTextBox;
                MP_ID_txtBox.PreviewTextInput += InputValidationHelper.Integers_NumberValidationTextBox;

                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                    myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
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

        private void Apply_DataGrid_Color_Theme()
        {
            myDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            myDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            myDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }

        private void Load_Data()
        {
            if (SubProject_ID != -1)
            {
                LoadAccountComboBox(SubProject_ID.ToString());
                if (Acc_ID != -1)
                    Account_comboBox.SelectedValue = Acc_ID;
            }
            else
            {
                LoadAccountComboBox("");
            }

            if (Beneficiary_ID != -1)
                Fill_Beneficiary_ByID();
        }

        private void Fill_Beneficiary_ByID()
        {
            dt = b.Select(Beneficiary_ID.ToString(), "", "");
            if (dt.Rows.Count != 0)
            {
                int MicroProject_ID = Original_MicroProject_ID = Convert.ToInt32(dt.Rows[0]["MicroProject_ID"].ToString());
                MP_ID_txtBox.Text = dt.Rows[0]["MicroProject_ID"].ToString();
                MP_Name_txtBox.Text = dt.Rows[0]["Project_Name"].ToString();
                FName_txtBox.Text = dt.Rows[0]["FirstName"].ToString();
                LName_txtBox.Text = dt.Rows[0]["LastName"].ToString();
                FaName_txtBox.Text = dt.Rows[0]["FatherName"].ToString();
                NationalNumber_txtBox.Text = dt.Rows[0]["NationalNumber"].ToString();
                Mobile_txtBox.Text = dt.Rows[0]["Mobile"].ToString();
                Partners_txtBox.Text = dt.Rows[0]["Partners"].ToString();
                Amount_txtBox.Text = Original_Amount = Login.ConvertToDouble(dt.Rows[0]["Amount"].ToString());
                if (dt.Rows[0]["Date"] == DBNull.Value)
                    Date_datepicker.SelectedDate = null;
                else
                    Date_datepicker.SelectedDate = Convert.ToDateTime(dt.Rows[0]["Date"].ToString());

                if (dt.Rows[0]["FirstPaymentDate"] == DBNull.Value)
                    FirstPaymentDate_datepicker.SelectedDate = Original_FirstPaymentDate = null;
                else
                    FirstPaymentDate_datepicker.SelectedDate = Original_FirstPaymentDate = Convert.ToDateTime(dt.Rows[0]["FirstPaymentDate"].ToString());
                Payment_interval_TextBox.Text = Original_Payment_interval = dt.Rows[0]["Payment_interval"].ToString();
                ToReturn_Amount_txtBox.Text = Original_ToReturn_Amount = Login.ConvertToDouble(dt.Rows[0]["ToReturn_Amount"].ToString());
                Payment_txtBox.Text = Original_Payment = Login.ConvertToDouble(dt.Rows[0]["Payment"].ToString());
                //PaymentCount_txtBox.Text = Login.ConvertToDouble(dt.Rows[0]["PaymentsCount"].ToString());
                Notes_txtBox.Text = dt.Rows[0]["Notes"].ToString();
                ReceiptNumber_txtBox.Text = dt.Rows[0]["ReceiptNumber"].ToString();
                OrderNumber_txtBox.Text = dt.Rows[0]["OrderNumber"].ToString();

                if (dt.Rows[0]["Account_ID"] != DBNull.Value)
                    Account_comboBox.SelectedValue = Convert.ToInt32(dt.Rows[0]["Account_ID"]);


                double Rate, AmountAowed, AmountPaid, RemainingAmount, NotApprovedAmount;


                if (dt.Rows[0].Field<double?>("Rate") != null)
                {
                    if (!double.TryParse(dt.Rows[0].Field<double>("Rate").ToString(), out Rate))
                    {
                        Rate = 0;
                    }
                }
                else
                {
                    Rate = 0;
                }

                if (dt.Rows[0].Field<double?>("ToReturn_Amount") != null)
                {
                    if (!double.TryParse(dt.Rows[0].Field<double>("ToReturn_Amount").ToString(), out AmountAowed))
                    {
                        AmountAowed = 0;
                    }
                }
                else
                {
                    AmountAowed = 0;
                }

                if (dt.Rows[0].Field<double?>("paidamount") != null)
                {
                    if (!double.TryParse(dt.Rows[0].Field<double>("paidamount").ToString(), out AmountPaid))
                    {
                        AmountPaid = 0;
                    }
                }
                else
                {
                    AmountPaid = 0;
                }

                if (dt.Rows[0].Field<double?>("NotApproved_paidamount") != null)
                {
                    if (!double.TryParse(dt.Rows[0].Field<double>("NotApproved_paidamount").ToString(), out NotApprovedAmount))
                    {
                        NotApprovedAmount = 0;
                    }
                }
                else
                {
                    NotApprovedAmount = 0;
                }

                RemainingAmount = AmountAowed - AmountPaid;

                Rate_txtBox.Content = Login.ConvertToDouble(Rate.ToString());
                Returned_txtBox.Content = Login.ConvertToDouble(AmountPaid.ToString());
                NotApprovedAmount_txtBox.Content = Login.ConvertToDouble(NotApprovedAmount.ToString());
                RemainedِAmount_txtBox.Content = Login.ConvertToDouble(RemainingAmount.ToString());

                Calculate_values();
                Fill_Payments();
            }
        }

        private void LoadAccountComboBox(string SubProject_ID)
        {
            Account_comboBox.ItemsSource = null;
            a_dt = a.Select(SubProject_ID, "", "", false, "");
            if (a_dt != null && a_dt.Rows.Count > 0)
                Account_comboBox.ItemsSource = a_dt.DefaultView;
        }


        Double amount, ToReturn_Amountamount;

        string amount_str, payment, ToReturn_Amount_str;

        private void ToReturn_Amount_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ToReturn_Amount_txtBox.Text != "")
                {
                    ToReturn_Amount_txtBox.Text = Login.ConvertToDouble(ToReturn_Amount_txtBox.Text);
                }
                Calculate_values();
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

        private void Amount_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Amount_txtBox.Text != "")
                {
                    Amount_txtBox.Text = Login.ConvertToDouble(Amount_txtBox.Text);
                }
                Calculate_values();
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

        private void Payment_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Payment_txtBox.Text != "")
                {
                    Payment_txtBox.Text = Login.ConvertToDouble(Payment_txtBox.Text);
                }
                Calculate_values();
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
            GeneratePayments_btn.IsEnabled = false;
            try
            {
                Save_Beneficiry();
                Load_Data();
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
                GeneratePayments_btn.IsEnabled = true;
            }
        }

        private void GeneratePayments_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            GeneratePayments_btn.IsEnabled = false;
            try
            {
                RegeneratePayments = true;
                Save_Beneficiry();
                //DialogResult = true;
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
                GeneratePayments_btn.IsEnabled = true;
            }
        }
        private void Save_Beneficiry()
        {
            if (MP_ID_txtBox.Text == "" || MP_Name_txtBox.Text == "")
            {
                MessageBox.Show("Please enter the project number and project title", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                return;
            }

            if (String.IsNullOrWhiteSpace(FName_txtBox.Text) || String.IsNullOrWhiteSpace(LName_txtBox.Text) || String.IsNullOrWhiteSpace(FaName_txtBox.Text) || String.IsNullOrWhiteSpace(NationalNumber_txtBox.Text))
            {
                MessageBox.Show("Please enter the first name, father name, last name and the ID number.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                return;

            }

            if (String.IsNullOrWhiteSpace(Amount_txtBox.Text) || String.IsNullOrWhiteSpace(Payment_txtBox.Text) || String.IsNullOrWhiteSpace(ToReturn_Amount_txtBox.Text))
            {
                MessageBox.Show("Please enter the loan amount, amount owed and payment.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                return;
            }

            if ((Date_datepicker.SelectedDate == null && FirstPaymentDate_datepicker.SelectedDate != null) || (Date_datepicker.SelectedDate != null && FirstPaymentDate_datepicker.SelectedDate == null))
            {
                MessageBox.Show("You cannot enter only one of these two dates, the financing date and the payments start date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                return;
            }

            if (String.IsNullOrWhiteSpace(Payment_interval_TextBox.Text))
            {
                MessageBox.Show("Please enter the payments interval.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                return;
            }

            if (Account_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Please select the group account.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                return;
            }

            int MicroProject_ID = Convert.ToInt32(MP_ID_txtBox.Text);

            DataTable dt = b.CHeck_MicroProjectNum(Beneficiary_ID, SubProject_ID, MicroProject_ID);

            if (dt != null && dt.Rows.Count > 0)
            {
                MessageBox.Show("There is another micro project with the same number within the same center.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                return;
            }

            string Date, FirstPaymentDate;

            int Account_ID = Convert.ToInt32(Account_comboBox.SelectedValue);

            if (Date_datepicker.SelectedDate == null && FirstPaymentDate_datepicker.SelectedDate == null)
            {
                Date = null;
                FirstPaymentDate = null;
                RegeneratePayments = false;
                DeleteSchedualedPayments = true;
            }
            else
            {
                Date = ((DateTime)Date_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                FirstPaymentDate = ((DateTime)FirstPaymentDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

                DeleteSchedualedPayments = false;
                if (Beneficiary_ID != -1)
                {
                    if (Amount_txtBox.Text != Original_Amount ||
                        ToReturn_Amount_txtBox.Text != Original_ToReturn_Amount ||
                        Payment_txtBox.Text != Original_Payment ||
                        Payment_interval_TextBox.Text != Original_Payment_interval ||
                        FirstPaymentDate_datepicker.SelectedDate != Original_FirstPaymentDate)
                        RegeneratePayments = true;
                    else
                        RegeneratePayments = false;
                }
                else
                {
                    RegeneratePayments = true;
                }
            }
            if (Beneficiary_ID != -1)//Edit
            {
                bool ToSave = true;
                if (RegeneratePayments)
                {
                    var dialogResult1 = MessageBox.Show("A rescheduling will be carried out for the unpaid amount due to a change in one of the following:" +
                        Environment.NewLine + "Loan amount" +
                        Environment.NewLine + "Payment" +
                        Environment.NewLine + "Amount owed" +
                        Environment.NewLine + "Payments interval" +
                        Environment.NewLine + "Payments start date", "Confirm before save", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);
                    if (dialogResult1 == MessageBoxResult.No)
                    {
                        ToSave = false;
                    }
                }

                if (DeleteSchedualedPayments)
                {
                    var dialogResult1 = MessageBox.Show("All unpaid payments will be erased due to the deletion of the financing date and the payments start date.", "Confirm before save", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);
                    if (dialogResult1 == MessageBoxResult.No)
                    {
                        ToSave = false;
                    }

                }

                if (ToSave)
                {
                    b.Update(Beneficiary_ID, FName_txtBox.Text, LName_txtBox.Text, FaName_txtBox.Text, NationalNumber_txtBox.Text, Mobile_txtBox.Text
                        , Partners_txtBox.Text, MicroProject_ID, MP_Name_txtBox.Text
                        , amount, Date, FirstPaymentDate, ToReturn_Amountamount, float.Parse(payment), Convert.ToInt32(Payment_interval_TextBox.Text)
                        , Notes_txtBox.Text, ReceiptNumber_txtBox.Text, OrderNumber_txtBox.Text, Account_ID, Original_MicroProject_ID, SubProject_ID, RegeneratePayments, DeleteSchedualedPayments, "Beneficiary_Window");

                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                }

            }
            else//Add new
            {
                Beneficiary_ID = b.Insert(FName_txtBox.Text, LName_txtBox.Text, FaName_txtBox.Text, NationalNumber_txtBox.Text, Mobile_txtBox.Text
                    , Partners_txtBox.Text, MicroProject_ID, MP_Name_txtBox.Text
                    , amount, Date, FirstPaymentDate, ToReturn_Amountamount, float.Parse(payment), Convert.ToInt32(Payment_interval_TextBox.Text)
                    , Notes_txtBox.Text, ReceiptNumber_txtBox.Text, OrderNumber_txtBox.Text, Account_ID, RegeneratePayments, "Beneficiary_Window");

                MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

            }


        }

        private void Account_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Account_comboBox.SelectedIndex = -1;
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

        private void Calculate_values()
        {

            amount = ToReturn_Amountamount = 0;

            if (!String.IsNullOrWhiteSpace(Amount_txtBox.Text))
                amount_str = Amount_txtBox.Text.Replace(",", "");
            else
                amount_str = "";
            if (!String.IsNullOrWhiteSpace(Payment_txtBox.Text))
                payment = Payment_txtBox.Text.Replace(",", "");
            else
                payment = "";
            if (!String.IsNullOrWhiteSpace(ToReturn_Amount_txtBox.Text))
                ToReturn_Amount_str = ToReturn_Amount_txtBox.Text.Replace(",", "");
            else
                ToReturn_Amount_str = "";

            Double.TryParse(amount_str, out amount);
            Double.TryParse(ToReturn_Amount_str, out ToReturn_Amountamount);


            if (Payment_txtBox.Text == "" || Payment_txtBox.Text == "0") payment = "0";

            if (ToReturn_Amount_txtBox.Text == "" || ToReturn_Amount_txtBox.Text == "0") ToReturn_Amountamount = 0;

        }

        private void Clear_Boxes()
        {
            MP_ID_txtBox.Text = MP_Name_txtBox.Text = FName_txtBox.Text = LName_txtBox.Text = FaName_txtBox.Text =
                Partners_txtBox.Text = NationalNumber_txtBox.Text = Mobile_txtBox.Text = Amount_txtBox.Text =
                ToReturn_Amount_txtBox.Text = Payment_txtBox.Text = ReceiptNumber_txtBox.Text = OrderNumber_txtBox.Text = Notes_txtBox.Text = "";


            Date_datepicker.SelectedDate = FirstPaymentDate_datepicker.SelectedDate = null;
            Account_comboBox.SelectedIndex = -1;
        }

        private void FName_txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Get PlaceholderTextBlock inside the TextBox template
                var placeholder_first = (TextBlock)FName_txtBox.Template.FindName("PlaceholderText", FName_txtBox);
                if (placeholder_first != null)
                {
                    // Hide placeholder when user types, show when empty
                    placeholder_first.Visibility = string.IsNullOrWhiteSpace(placeholder_first.Text) ? Visibility.Visible : Visibility.Collapsed;
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

        private void FaName_txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {

                var placeholder_fa = (TextBlock)FaName_txtBox.Template.FindName("PlaceholderText", FaName_txtBox);
                if (placeholder_fa != null)
                {
                    // Hide placeholder when user types, show when empty
                    placeholder_fa.Visibility = string.IsNullOrWhiteSpace(placeholder_fa.Text) ? Visibility.Visible : Visibility.Collapsed;
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

        private void Date_datepicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Date_datepicker.SelectedDate != null)
                {
                    FirstPaymentDate_datepicker.SelectedDate = Date_datepicker.SelectedDate.Value.AddMonths(2);
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

        private void Fill_Payments()
        {
            p_dt = p.Select_Beneficiary_Payments(Beneficiary_ID.ToString(), "");
            if (p_dt != null && p_dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = p_dt.DefaultView;


                int TotalPaymentCount, PaidPaymentCount, UnpaidPaymentCount, InCompletePaymentCont, DelayedPaymentCont, NotApprovedPaymentCont;

                TotalPaymentCount = p_dt.Rows.Count;
                PaidPaymentCount = Convert.ToInt32(p_dt.Select("payment_state='Done' and Approved = 'Yes'").Length.ToString());
                UnpaidPaymentCount = Convert.ToInt32(p_dt.Select("payment_state='Unpaid'").Length.ToString());
                InCompletePaymentCont = Convert.ToInt32(p_dt.Select("payment_state='Incomplete'").Length.ToString());
                DelayedPaymentCont = Convert.ToInt32(p_dt.Select("(Approved <> 'Yes' and DueDate < '" + DateTime.Now + "') or (payment_state <> 'Done' and DueDate < '" + DateTime.Now + "')").Length.ToString());
                NotApprovedPaymentCont = Convert.ToInt32(p_dt.Select("Approved = 'No'").Length.ToString());

                PaymentCount_txtBox.Content = TotalPaymentCount.ToString();
                PaidPaymentCount_txtBox.Content = PaidPaymentCount.ToString();
                RemainedPaymentCount_txtBox.Content = UnpaidPaymentCount.ToString();
                NotCompleted_txtBox.Content = InCompletePaymentCont.ToString();
                DelayedPaymentCont_txtBox.Content = DelayedPaymentCont.ToString();
                NotApprovedPaymentCont_txtBox.Content = NotApprovedPaymentCont.ToString();
            }
            else
            {
                myDataGrid.ItemsSource = null;
                Returned_txtBox.Content = RemainedِAmount_txtBox.Content = PaymentCount_txtBox.Content
                    = NotCompleted_txtBox.Content = RemainedPaymentCount_txtBox.Content = "0.00";
            }
        }

        private void myDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
        {
            try
            {
                int count = myDataGrid.SelectedItems.Count;
                //Selected_textBlock.Text = count.ToString("N0");
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Beneficiary_ID == -1)
                {
                    Clear_Boxes();
                    Calculate_values();
                }

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

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                int count = myDataGrid.View.Records.Count();
                //All_textBlock.Text = count.ToString("N0");

                // All_textBlock.Text = myDataGrid.View.Records.Count().ToString();
                Export_btn.CommandTarget = myDataGrid;
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
        private void LName_txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {

                var placeholder_last = (TextBlock)LName_txtBox.Template.FindName("PlaceholderText", LName_txtBox);
                if (placeholder_last != null)
                {
                    // Hide placeholder when user types, show when empty
                    placeholder_last.Visibility = string.IsNullOrWhiteSpace(placeholder_last.Text) ? Visibility.Visible : Visibility.Collapsed;
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
    }
}
