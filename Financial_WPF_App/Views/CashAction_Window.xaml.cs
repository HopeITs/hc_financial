using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for CashAction_Window.xaml
    /// </summary>
    public partial class CashAction_Window : Window
    {
        public CashAction_Window(int Account_ID, string Account_Name, Main_Window_2 main_Window, CashTill_Window CashTill)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.CashTill = CashTill;
            this.cashaction_ID = -1;
            this.Account_ID = Account_ID;
            this.Account_Name = Account_Name;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public CashAction_Window(int Account_ID, string Account_Name, int cashaction_ID, Main_Window_2 main_Window, CashTill_Window CashTill)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.CashTill = CashTill;

            this.cashaction_ID = cashaction_ID;
            this.Account_ID = Account_ID;
            this.Account_Name = Account_Name;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        int cashaction_ID, Account_ID;
        string Account_Name;

        Models.Action a;
        private Account Acc;
        Users u;
        Denominations d;
        Main_Window_2 main_Window;
        CashTill_Window CashTill;
        DataTable Actions_dt = new DataTable();
        DataTable Original_Actions_dt = new DataTable();
        DataTable denominations_dt = new DataTable();

        int NewAddedRows = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"];
        string HeaderStyle_Name = "DarkgrayishViolet_SfDataGrid_header";
        string TableSummaryRowStyle_Name = "DarkgrayishViolet_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "DarkgrayishViolet_GroupSummaryRowStyle";

        private bool OnLoad()
        {
            try
            {
                a = new Models.Action();
                Acc = new Account();
                u = new Users();
                d = new Denominations();
                Amount_txtBox.PreviewTextInput += InputValidationHelper.Decimal_NumberValidationTextBox;
                //AccountName_lable.Content = "(" + Account_Name + ")";
                CashTill.Tag += " (" + Account_Name + ")";
                Load_Form();
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
            Actions_DataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            Actions_DataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            Actions_DataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
        }

        private void Load_Form()
        {
            ActionDate_datepicker.SelectedDate = DateTime.Today;

            Fill_ComboBoxes();

            //UPDATE MODE//
            if (cashaction_ID > 0)
            {
                DataTable dt = a.Select_CashAction(cashaction_ID);
                Actions_dt = dt.Copy(); Original_Actions_dt = dt.Copy();
                Fill_Action();
            }
        }

        private void Fill_ComboBoxes()
        {
            Denominations_comboBox.ItemsSource = null;
            denominations_dt = d.Select(true);
            if (denominations_dt != null && denominations_dt.Rows.Count > 0)
            {
                Denominations_comboBox.ItemsSource = denominations_dt.DefaultView;
            }
        }

        private void Denominations_comboBox_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Denominations_comboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            { if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) 
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host")) 
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else { MessageBox.Show(ex.Message, "Error"); } 
            }
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

        private void Fill_Action()
        {
            ActionDate_datepicker.SelectedDate = Convert.ToDateTime(Actions_dt.Rows[0].Field<DateTime>("ActionDate").ToString());

            Notes_txtBox.Text = Actions_dt.Rows[0].Field<string>("Note").ToString();
            E_Notes_txtBox.Text = Actions_dt.Rows[0].Field<string>("E_Note").ToString();

            Actions_DataGrid.ItemsSource = Actions_dt.DefaultView;

            Calc_TotalNet();
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

        private void InsertAction_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Denominations_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("الرجاء اختيار الفئة قبل الادخال", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("الرجاء اختيار الفئة قبل الادخال");
                }

                double Amount;

                if (Amount_txtBox.Text == "" || !double.TryParse(Amount_txtBox.Text, out Amount))
                {
                    MessageBox.Show("الرجاء التأكد من ادخال الملغ بشكل صحيح", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("الرجاء التأكد من ادخال الملغ بشكل صحيح");
                }

                if (E_RadioButton.IsChecked == false && I_RadioButton.IsChecked == false)
                {
                    MessageBox.Show("الرجاء التأكد من تحديد نوع العملية قبل الادخال", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("الرجاء التأكد من تحديد نوع العملية قبل الادخال");
                }

                if (Actions_DataGrid.ItemsSource == null)
                {
                    Actions_dt = new DataTable();
                    Actions_dt.Columns.Add("cashaction_denominations_id", typeof(int));
                    Actions_dt.Columns.Add("denominations_id", typeof(int));
                    Actions_dt.Columns.Add("denominations_name", typeof(string));
                    Actions_dt.Columns.Add("actiontype_shortname", typeof(string));
                    Actions_dt.Columns.Add("actiontype_name", typeof(string));
                    Actions_dt.Columns.Add("Income", typeof(double));
                    Actions_dt.Columns.Add("Expand", typeof(double));
                    Actions_dt.Columns.Add("Amount", typeof(double));

                    Actions_DataGrid.ItemsSource = Actions_dt;
                }

                System.Data.DataRow dr = Actions_dt.NewRow();

                NewAddedRows -= 1;
                dr["cashaction_denominations_id"] = NewAddedRows;
                dr["denominations_id"] = Convert.ToInt32(Denominations_comboBox.SelectedValue);
                dr["denominations_name"] = Denominations_comboBox.Text;
                if (I_RadioButton.IsChecked == true)
                {
                    dr["actiontype_shortname"] = "I";
                    dr["actiontype_name"] = "Income";
                    dr["Income"] = Amount;
                    dr["Expand"] = 0;
                }
                else if (E_RadioButton.IsChecked == true)
                {
                    dr["actiontype_shortname"] = "E";
                    dr["actiontype_name"] = "Outcome";
                    dr["Expand"] = Amount;
                    dr["Income"] = 0;
                }
                dr["Amount"] = Amount;

                Actions_dt.Rows.Add(dr);
                Actions_dt.AcceptChanges();

                Calc_TotalNet();

                Amount_txtBox.Text = "";
                Denominations_comboBox.SelectedIndex = -1;
                //E_RadioButton.IsChecked = true;
                //I_RadioButton.IsChecked = false;

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
            try
            {
                string ActionDate = "";

                if (ActionDate_datepicker.SelectedDate == null)
                {
                    MessageBox.Show("Please make sure to select the operation date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("Please make sure to select the operation date.");
                }
                else
                {
                    ActionDate = ((DateTime)ActionDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
                }

                if (Actions_dt.Rows.Count == 0)
                {
                    MessageBox.Show("الرجاء ادخال عملية واحدة على الأقل", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                    //throw new Exception("الرجاء ادخال عملية واحدة على الأقل");
                }

                DataTable Add_Actions = new DataTable();
                System.Data.DataRow[] rowsToAdd = Actions_dt.Select("cashaction_denominations_id < 0");
                if (rowsToAdd.Count() > 0)
                    Add_Actions = rowsToAdd.CopyToDataTable();

                if (cashaction_ID != -1)//Edit
                {
                    DataTable Delete_Actions = new DataTable();
                    var kept_Actions = Actions_dt.AsEnumerable()
                        .Where(row => row.Field<int>("cashaction_denominations_id") > 0)
                        .Select(row => row.Field<int>("cashaction_denominations_id"));

                    // Select rows from dt1 where id is not in dt2Ids
                    var result = Original_Actions_dt.AsEnumerable()
                                    .Where(row => !kept_Actions.Contains(row.Field<int>("cashaction_denominations_id")));

                    if (result.Count() > 0)
                    {
                        Delete_Actions = result.CopyToDataTable();
                    }
                    a.Update_CashAction(cashaction_ID, Account_Name, ActionDate, Notes_txtBox.Text, E_Notes_txtBox.Text, Add_Actions, Delete_Actions, "CashAction_Window");
                    this.Close();
                }
                else//Add new
                {
                    a.Insert_CashAction(Account_ID, Account_Name, ActionDate, Notes_txtBox.Text, E_Notes_txtBox.Text, Add_Actions, "CashAction_Window");
                }
                MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);

                Clear_Boxes();


                if (CashTill != null)
                    CashTill.Load_Data();

            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.CloseAndClearConnection();
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

        private void Clear_Boxes()
        {
            Amount_txtBox.Text = Notes_txtBox.Text = E_Notes_txtBox.Text = "";

            Denominations_comboBox.SelectedIndex = -1;

            ActionDate_datepicker.SelectedDate = DateTime.Today;


            Actions_DataGrid.ItemsSource = null;

            E_RadioButton.IsChecked = true;
            I_RadioButton.IsChecked = false;
        }

        private void Donor_DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)Actions_DataGrid.CurrentItem;
                int Current_cashaction_denominations_id = Convert.ToInt32(dataRow["cashaction_denominations_id"].ToString());

                System.Data.DataRow[] dtr = Actions_dt.Select("cashaction_denominations_id='" + Current_cashaction_denominations_id + "'");
                foreach (var drow in dtr)
                {
                    drow.Delete();
                }
                Actions_dt.AcceptChanges();

                Calc_TotalNet();
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

        private void Calc_TotalNet()
        {
            double Income = Actions_dt.AsEnumerable().Sum(row => row.Field<double>("Income"));
            double Expand = Actions_dt.AsEnumerable().Sum(row => row.Field<double>("Expand"));
            double Net = Income - Expand;

            if (Actions_DataGrid.TableSummaryRows.Count > 0)
            {
                Actions_DataGrid.TableSummaryRows[0].SummaryColumns.Clear();
                Actions_DataGrid.TableSummaryRows.RemoveAt(0);
            }

            Actions_DataGrid.TableSummaryRows.Add(new GridTableSummaryRow()
            {
                ShowSummaryInRow = true,
                Title = " Income:" + Income.ToString("N2") + " | Outcome:" + Expand.ToString("N2") + " | Total:" + Net.ToString("N2"),
                SummaryColumns = new ObservableCollection<ISummaryColumn>() { }
            });

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
