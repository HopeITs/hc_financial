using Financial_WPF_App.Models;
using Syncfusion.Windows.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Syncfusion.UI.Xaml.Grid;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;


namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for User_Window.xaml
    /// </summary>
    public partial class User_Window : Window
    {
        public User_Window(Main_Window_2 main_Window)
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
        private Users u;
        private Models.City city;
        DataTable role_dt, users_dt, subprojects_dt, accounts_dt, cashaccounts_dt, cashier_dt, personalaccounts_dt, exchangeaccounts_dt;

        List<int> Original_Subprojects;
        List<int> Original_Accounts;
        List<int> Original_CashAccounts;
        List<int> Original_Cashier;
        List<int> Original_PersonalAccounts;
        List<int> Original_ExchangeAccounts;


        List<int> Subprojects;
        List<int> Accounts;
        List<int> CashAccounts;
        List<int> Cashier;
        List<int> PersonalAccounts;
        List<int> ExchangeAccounts;

        int User_ID = -1;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";
        private bool OnLoad()
        {
            try
            {
                u = new Users();
                city = new Models.City();
                Original_Subprojects = new List<int>();
                Original_Accounts = new List<int>();
                Original_CashAccounts = new List<int>();
                Original_Cashier = new List<int>();
                Original_PersonalAccounts = new List<int>();
                Original_ExchangeAccounts = new List<int>();


                Subprojects = new List<int>();
                Accounts = new List<int>();
                CashAccounts = new List<int>();
                Cashier = new List<int>();
                PersonalAccounts = new List<int>();
                ExchangeAccounts = new List<int>();

                Load_Data();
                Apply_Color_Theme();
                Apply_DataGrid_Color_Theme();
                Show_Hide_Component();

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
            Load_Comboboxes_Data();
            LoadGrid();
        }

        private void Load_Comboboxes_Data()
        {
            Role_comboBox.ItemsSource = null;
            role_dt = u.Get_UserRoles();
            if (role_dt != null && role_dt.Rows.Count > 0)
            {
                Role_comboBox.ItemsSource = role_dt.DefaultView;
            }

            DataSet ds = u.Get_All_Accounts();

            subprojects_dt = ds.Tables[0];

            accounts_dt = ds.Tables[1];

            exchangeaccounts_dt = ds.Tables[2];

            cashaccounts_dt = ds.Tables[3];

            cashier_dt = ds.Tables[4];

            personalaccounts_dt = ds.Tables[5];
        }

        private void Fill_Comboboxes_Data()
        {
            Subproject_CheckListBox.ItemsSource = null;
            Subproject_CheckListBox.ItemsSource = subprojects_dt.DefaultView;

            Account_CheckListBox.ItemsSource = null;

            ExchangeAccount_CheckListBox.ItemsSource = null;
            ExchangeAccount_CheckListBox.ItemsSource = exchangeaccounts_dt.DefaultView;

            CashAccount_CheckListBox.ItemsSource = null;
            CashAccount_CheckListBox.ItemsSource = cashaccounts_dt.DefaultView;

            IsCashier_CashAccount_CheckListBox.ItemsSource = null;
            IsCashier_CashAccount_CheckListBox.ItemsSource = cashier_dt.DefaultView;

            PersonalAccount_CheckListBox.ItemsSource = null;
            PersonalAccount_CheckListBox.ItemsSource = personalaccounts_dt.DefaultView;
        }

        private void Clear_Lists()
        {
            Subprojects.Clear();
            Accounts.Clear();
            CashAccounts.Clear();
            Cashier.Clear();
            PersonalAccounts.Clear();
            ExchangeAccounts.Clear();
        }

        private void LoadGrid()
        {
            if (Login.BuildConnection())
            {
                users_dt = u.Get_AlUsers();
                if (users_dt != null && users_dt.Rows.Count > 0)
                {
                    myDataGrid.ItemsSource = users_dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }
            }

        }

        private void Subproject_CheckListBox_ItemChecked(object sender, Syncfusion.Windows.Tools.Controls.ItemCheckedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)e.Item;
                int Subproject_ID = Convert.ToInt32(dataRow["ID"].ToString());
                if (e.Checked)
                {
                    Subprojects.Add(Subproject_ID);
                }
                else if (!e.Checked)
                {
                    Subprojects.Remove(Subproject_ID);
                }

                List<int> tempAccounts = new List<int>();

                if (Accounts.Count > 0)
                {
                    tempAccounts = Accounts.ToList();
                }
                Accounts.Clear();

                Account_CheckListBox.ItemsSource = null;

                if (Subprojects.Count > 0)
                {
                    string filterExpression = string.Join(" OR ", Subprojects.Select(id => "subproject_id = " + id));
                    System.Data.DataRow[] rows = accounts_dt.Select(filterExpression);
                    DataTable selected_dt = new DataTable();
                    if (rows.Length != 0)
                    {
                        selected_dt = rows.CopyToDataTable();
                        if (selected_dt != null && selected_dt.Rows.Count > 0)
                        {
                            Account_CheckListBox.ItemsSource = selected_dt.DefaultView;
                        }
                    }
                }

                if (tempAccounts.Count > 0)
                {
                    for (int i = 0; i < Account_CheckListBox.Items.Count; i++)
                    {
                        DataRowView row = (DataRowView)Account_CheckListBox.Items[i];
                        if (tempAccounts.Contains(Convert.ToInt32(row["ID"])))
                        {
                            Account_CheckListBox.SelectedItems.Add(Account_CheckListBox.Items[i]);
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
            }
        }

        private void Account_CheckListBox_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            DataRowView dataRow = (DataRowView)e.Item;
            int Account_ID = Convert.ToInt32(dataRow["ID"].ToString());
            if (e.Checked)
            {
                Accounts.Add(Account_ID);
            }
            else if (!e.Checked)
            {
                Accounts.Remove(Account_ID);
            }
        }

        private void CashAccount_CheckListBox_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            DataRowView dataRow = (DataRowView)e.Item;
            int CashAccount_ID = Convert.ToInt32(dataRow["ID"].ToString());
            if (e.Checked)
            {
                CashAccounts.Add(CashAccount_ID);
            }
            else if (!e.Checked)
            {
                CashAccounts.Remove(CashAccount_ID);
            }
        }

        private void IsCashier_CashAccount_CheckListBox_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            DataRowView dataRow = (DataRowView)e.Item;
            int CashAccount_ID = Convert.ToInt32(dataRow["ID"].ToString());
            if (e.Checked)
            {
                Cashier.Add(CashAccount_ID);
            }
            else if (!e.Checked)
            {
                Cashier.Remove(CashAccount_ID);
            }
        }

        private void PersonalAccount_CheckListBox_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            DataRowView dataRow = (DataRowView)e.Item;
            int PersonalAccount_ID = Convert.ToInt32(dataRow["ID"].ToString());
            if (e.Checked)
            {
                PersonalAccounts.Add(PersonalAccount_ID);
            }
            else if (!e.Checked)
            {
                PersonalAccounts.Remove(PersonalAccount_ID);
            }
        }

        private void ExchangeAccount_CheckListBox_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            DataRowView dataRow = (DataRowView)e.Item;
            int ExchangeAccount_ID = Convert.ToInt32(dataRow["ID"].ToString());
            if (e.Checked)
            {
                ExchangeAccounts.Add(ExchangeAccount_ID);
            }
            else if (!e.Checked)
            {
                ExchangeAccounts.Remove(ExchangeAccount_ID);
            }
        }


        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            User_ID = -1;
            Username_txtBox.Text = string.Empty;
            Password_txtBox.Text = string.Empty;
            Role_comboBox.SelectedIndex = -1;
            Load_Data();
        }

        private void myDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
      

                            User_ID = -1;
                            Username_txtBox.Text = string.Empty;
                            Password_txtBox.Text = string.Empty;
                            Role_comboBox.SelectedIndex = -1;
                            Clear_Lists();


                            DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                            User_ID = Convert.ToInt32(dataRow["user_id"].ToString());
                            Username_txtBox.Text = dataRow["username"].ToString();
                            Password_txtBox.Text = dataRow["Password"].ToString();
                            Role_comboBox.SelectedValue = Convert.ToInt32(dataRow["role_id"].ToString());


                            Original_Subprojects.Clear();
                            if (dataRow["user_subprojects"] != DBNull.Value && dataRow["user_subprojects"] != null)
                            {
                                Original_Subprojects = dataRow["user_subprojects"].ToString().Split(',').Select(int.Parse).ToList();

                                for (int i = 0; i < Subproject_CheckListBox.Items.Count; i++)
                                {
                                    DataRowView row = (DataRowView)Subproject_CheckListBox.Items[i];
                                    if (Original_Subprojects.Contains(Convert.ToInt32(row["ID"])))
                                    {
                                        Subproject_CheckListBox.SelectedItems.Add(Subproject_CheckListBox.Items[i]);
                                    }
                                }
                            }

                            Original_Accounts.Clear();
                            if (dataRow["user_accounts"] != DBNull.Value && dataRow["user_accounts"] != null)
                            {
                                Original_Accounts = dataRow["user_accounts"].ToString().Split(',').Select(int.Parse).ToList();

                                for (int i = 0; i < Account_CheckListBox.Items.Count; i++)
                                {
                                    DataRowView row = (DataRowView)Account_CheckListBox.Items[i];
                                    if (Original_Accounts.Contains(Convert.ToInt32(row["ID"])))
                                    {
                                        Account_CheckListBox.SelectedItems.Add(Account_CheckListBox.Items[i]);
                                    }
                                }
                            }

                            Original_PersonalAccounts.Clear();
                            if (dataRow["user_Personalaccounts"] != DBNull.Value && dataRow["user_Personalaccounts"] != null)
                            {
                                Original_PersonalAccounts = dataRow["user_Personalaccounts"].ToString().Split(',').Select(int.Parse).ToList();

                                for (int i = 0; i < PersonalAccount_CheckListBox.Items.Count; i++)
                                {
                                    DataRowView row = (DataRowView)PersonalAccount_CheckListBox.Items[i];
                                    if (Original_PersonalAccounts.Contains(Convert.ToInt32(row["ID"])))
                                    {
                                        PersonalAccount_CheckListBox.SelectedItems.Add(PersonalAccount_CheckListBox.Items[i]);
                                    }
                                }
                            }

                            Original_ExchangeAccounts.Clear();
                            if (dataRow["user_Exchangeaccounts"] != DBNull.Value && dataRow["user_Exchangeaccounts"] != null)
                            {
                                Original_ExchangeAccounts = dataRow["user_Exchangeaccounts"].ToString().Split(',').Select(int.Parse).ToList();

                                for (int i = 0; i < ExchangeAccount_CheckListBox.Items.Count; i++)
                                {
                                    DataRowView row = (DataRowView)ExchangeAccount_CheckListBox.Items[i];
                                    if (Original_ExchangeAccounts.Contains(Convert.ToInt32(row["ID"])))
                                    {
                                        ExchangeAccount_CheckListBox.SelectedItems.Add(ExchangeAccount_CheckListBox.Items[i]);
                                    }
                                }
                            }

                            Original_CashAccounts.Clear();
                            if (dataRow["user_Cashaccounts"] != DBNull.Value && dataRow["user_Cashaccounts"] != null)
                            {
                                Original_CashAccounts = dataRow["user_Cashaccounts"].ToString().Split(',').Select(int.Parse).ToList();

                                for (int i = 0; i < CashAccount_CheckListBox.Items.Count; i++)
                                {
                                    DataRowView row = (DataRowView)CashAccount_CheckListBox.Items[i];
                                    if (Original_CashAccounts.Contains(Convert.ToInt32(row["ID"])))
                                    {
                                        CashAccount_CheckListBox.SelectedItems.Add(CashAccount_CheckListBox.Items[i]);
                                    }
                                }
                            }

                            Original_Cashier.Clear();
                            if (dataRow["user_IsCashier"] != DBNull.Value && dataRow["user_IsCashier"] != null)
                            {
                                Original_Cashier = dataRow["user_IsCashier"].ToString().Split(',').Select(int.Parse).ToList();

                                for (int i = 0; i < IsCashier_CashAccount_CheckListBox.Items.Count; i++)
                                {
                                    DataRowView row = (DataRowView)IsCashier_CashAccount_CheckListBox.Items[i];
                                    if (Original_Cashier.Contains(Convert.ToInt32(row["ID"])))
                                    {
                                        IsCashier_CashAccount_CheckListBox.SelectedItems.Add(IsCashier_CashAccount_CheckListBox.Items[i]);
                                    }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Show_Hide_Component()
        {
            string Selected_Role = "";
            if (Role_comboBox.SelectedIndex != -1)
            {
                System.Data.DataRow[] row = role_dt.Select("ID=" + Convert.ToInt32(Role_comboBox.SelectedValue));
                Selected_Role = row[0]["Name"].ToString();
            }

            Subproject_Label.Visibility = Subproject_CheckListBox.Visibility =
                Account_Label.Visibility = Account_CheckListBox.Visibility =
                CashAccount_Label.Visibility = CashAccount_CheckListBox.Visibility = IsCashier_CashAccount_CheckListBox.Visibility =
                PersonalAccount_Label.Visibility = PersonalAccount_CheckListBox.Visibility =
                ExchangeAccountAccount_Label.Visibility = ExchangeAccount_CheckListBox.Visibility = Visibility.Collapsed;

            if (Selected_Role == "Head Office")
            {

            }
            else if (Selected_Role == "Project Manager")
            {
                Subproject_Label.Visibility = Subproject_CheckListBox.Visibility = Visibility.Visible;
            }
            else if (Selected_Role == "Financial Manager")
            {
                Subproject_Label.Visibility = Subproject_CheckListBox.Visibility =
                    CashAccount_Label.Visibility = CashAccount_CheckListBox.Visibility = IsCashier_CashAccount_CheckListBox.Visibility =
                    PersonalAccount_Label.Visibility = PersonalAccount_CheckListBox.Visibility =
                    ExchangeAccountAccount_Label.Visibility = ExchangeAccount_CheckListBox.Visibility = Visibility.Visible;
            }
            else if (Selected_Role == "Accountant")
            {
                Subproject_Label.Visibility = Subproject_CheckListBox.Visibility =
                    Account_Label.Visibility = Account_CheckListBox.Visibility =
                    CashAccount_Label.Visibility = CashAccount_CheckListBox.Visibility = IsCashier_CashAccount_CheckListBox.Visibility =
                    PersonalAccount_Label.Visibility = PersonalAccount_CheckListBox.Visibility = Visibility.Visible;
            }
            else if (Selected_Role == "Cashier")
            {
                CashAccount_Label.Visibility = CashAccount_CheckListBox.Visibility = IsCashier_CashAccount_CheckListBox.Visibility = Visibility.Visible;
            }
            else if (Selected_Role == "Employee")
            {
                Subproject_Label.Visibility = Subproject_CheckListBox.Visibility =
                    Account_Label.Visibility = Account_CheckListBox.Visibility =
                    CashAccount_Label.Visibility = CashAccount_CheckListBox.Visibility = IsCashier_CashAccount_CheckListBox.Visibility = Visibility.Visible;
            }
            else if (Selected_Role == "Donor")
            {

            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                User_ID = -1;
                Username_txtBox.Text = string.Empty;
                Password_txtBox.Text = string.Empty;
                Role_comboBox.SelectedIndex = -1;
                Clear_Lists();


                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                User_ID = Convert.ToInt32(dataRow["user_id"].ToString());
                Username_txtBox.Text = dataRow["username"].ToString();
                Password_txtBox.Text = dataRow["Password"].ToString();
                Role_comboBox.SelectedValue = Convert.ToInt32(dataRow["role_id"].ToString());


                Original_Subprojects.Clear();
                if (dataRow["user_subprojects"] != DBNull.Value && dataRow["user_subprojects"] != null)
                {
                    Original_Subprojects = dataRow["user_subprojects"].ToString().Split(',').Select(int.Parse).ToList();

                    for (int i = 0; i < Subproject_CheckListBox.Items.Count; i++)
                    {
                        DataRowView row = (DataRowView)Subproject_CheckListBox.Items[i];
                        if (Original_Subprojects.Contains(Convert.ToInt32(row["ID"])))
                        {
                            Subproject_CheckListBox.SelectedItems.Add(Subproject_CheckListBox.Items[i]);
                        }
                    }
                }

                Original_Accounts.Clear();
                if (dataRow["user_accounts"] != DBNull.Value && dataRow["user_accounts"] != null)
                {
                    Original_Accounts = dataRow["user_accounts"].ToString().Split(',').Select(int.Parse).ToList();

                    for (int i = 0; i < Account_CheckListBox.Items.Count; i++)
                    {
                        DataRowView row = (DataRowView)Account_CheckListBox.Items[i];
                        if (Original_Accounts.Contains(Convert.ToInt32(row["ID"])))
                        {
                            Account_CheckListBox.SelectedItems.Add(Account_CheckListBox.Items[i]);
                        }
                    }
                }

                Original_PersonalAccounts.Clear();
                if (dataRow["user_Personalaccounts"] != DBNull.Value && dataRow["user_Personalaccounts"] != null)
                {
                    Original_PersonalAccounts = dataRow["user_Personalaccounts"].ToString().Split(',').Select(int.Parse).ToList();

                    for (int i = 0; i < PersonalAccount_CheckListBox.Items.Count; i++)
                    {
                        DataRowView row = (DataRowView)PersonalAccount_CheckListBox.Items[i];
                        if (Original_PersonalAccounts.Contains(Convert.ToInt32(row["ID"])))
                        {
                            PersonalAccount_CheckListBox.SelectedItems.Add(PersonalAccount_CheckListBox.Items[i]);
                        }
                    }
                }

                Original_ExchangeAccounts.Clear();
                if (dataRow["user_Exchangeaccounts"] != DBNull.Value && dataRow["user_Exchangeaccounts"] != null)
                {
                    Original_ExchangeAccounts = dataRow["user_Exchangeaccounts"].ToString().Split(',').Select(int.Parse).ToList();

                    for (int i = 0; i < ExchangeAccount_CheckListBox.Items.Count; i++)
                    {
                        DataRowView row = (DataRowView)ExchangeAccount_CheckListBox.Items[i];
                        if (Original_ExchangeAccounts.Contains(Convert.ToInt32(row["ID"])))
                        {
                            ExchangeAccount_CheckListBox.SelectedItems.Add(ExchangeAccount_CheckListBox.Items[i]);
                        }
                    }
                }

                Original_CashAccounts.Clear();
                if (dataRow["user_Cashaccounts"] != DBNull.Value && dataRow["user_Cashaccounts"] != null)
                {
                    Original_CashAccounts = dataRow["user_Cashaccounts"].ToString().Split(',').Select(int.Parse).ToList();

                    for (int i = 0; i < CashAccount_CheckListBox.Items.Count; i++)
                    {
                        DataRowView row = (DataRowView)CashAccount_CheckListBox.Items[i];
                        if (Original_CashAccounts.Contains(Convert.ToInt32(row["ID"])))
                        {
                            CashAccount_CheckListBox.SelectedItems.Add(CashAccount_CheckListBox.Items[i]);
                        }
                    }
                }

                Original_Cashier.Clear();
                if (dataRow["user_IsCashier"] != DBNull.Value && dataRow["user_IsCashier"] != null)
                {
                    Original_Cashier = dataRow["user_IsCashier"].ToString().Split(',').Select(int.Parse).ToList();

                    for (int i = 0; i < IsCashier_CashAccount_CheckListBox.Items.Count; i++)
                    {
                        DataRowView row = (DataRowView)IsCashier_CashAccount_CheckListBox.Items[i];
                        if (Original_Cashier.Contains(Convert.ToInt32(row["ID"])))
                        {
                            IsCashier_CashAccount_CheckListBox.SelectedItems.Add(IsCashier_CashAccount_CheckListBox.Items[i]);
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
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                    int ID = Convert.ToInt32(dataRow["user_id"].ToString());
                    string name = dataRow["username"].ToString();
                    u.Delete_User(ID, name, "User_Window");

                    User_ID = -1;
                    Username_txtBox.Text = string.Empty;
                    Password_txtBox.Text = string.Empty;
                    Role_comboBox.SelectedIndex = -1;
                    LoadGrid();
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                { MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Role_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Role_comboBox.SelectedIndex = -1;
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
                string Username;
                string Password;
                string role;
                int role_id;

                if (String.IsNullOrWhiteSpace(Username_txtBox.Text) || String.IsNullOrEmpty(Username_txtBox.Text))
                {
                    MessageBox.Show("Please enter the username.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                else
                {
                    Username = Username_txtBox.Text;

                    bool usernameExists = users_dt.AsEnumerable()
                                .Any(row => row.Field<string>("username") == Username);

                    if (usernameExists)
                    {
                        MessageBox.Show("Username is already used.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
        
                }

                if (String.IsNullOrWhiteSpace(Password_txtBox.Text) || String.IsNullOrEmpty(Password_txtBox.Text))
                {
                    MessageBox.Show("Please enter the password.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                else
                {
                    Password = Password_txtBox.Text;
                }

                if (Role_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select the role", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                else
                {
                    role_id = Convert.ToInt32(Role_comboBox.SelectedValue);
                    System.Data.DataRow[] row = role_dt.Select("ID=" + Convert.ToInt32(Role_comboBox.SelectedValue));
                    role = row[0]["Name"].ToString();
                }

                if (Cashier.Count > 0)
                {
                    if (Check_Cahier())
                    {
                        MessageBox.Show("Cannot select more than one cashier for the same account.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        return;
                    }
                }

                if (Account_CheckListBox.Visibility == Visibility.Visible) { Subprojects.Clear(); Original_Subprojects.Clear(); }

                if (User_ID == -1)
                {
                    u.Insert_User(Username, Password, role, role_id, Subprojects, Accounts, CashAccounts, Cashier, PersonalAccounts, ExchangeAccounts, "User_Window");
                }
                else
                {
                    List<int> add_Subprojects = Subprojects.Except(Original_Subprojects).ToList();
                    List<int> add_Accounts = Accounts.Except(Original_Accounts).ToList();
                    List<int> add_CashAccounts = CashAccounts.Except(Original_CashAccounts).ToList();
                    List<int> add_PersonalAccounts = PersonalAccounts.Except(Original_PersonalAccounts).ToList();
                    List<int> add_ExchangeAccounts = ExchangeAccounts.Except(Original_ExchangeAccounts).ToList();


                    List<int> add_Cashier = Cashier.Except(Original_Cashier).Intersect(Original_CashAccounts).ToList();
                    List<int> remove_Cashier = Original_Cashier.Except(Cashier).ToList();

                    List<int> delete_Subprojects = Original_Subprojects.Except(Subprojects).ToList();
                    List<int> delete_Accounts = Original_Accounts.Except(Accounts).ToList();
                    List<int> delete_CashAccounts = Original_CashAccounts.Except(CashAccounts).ToList();
                    List<int> delete_PersonalAccounts = Original_PersonalAccounts.Except(PersonalAccounts).ToList();
                    List<int> delete_ExchangeAccounts = Original_ExchangeAccounts.Except(ExchangeAccounts).ToList();

                    u.Update_User(User_ID, Username, Password, role, role_id, add_Subprojects, add_Accounts, add_CashAccounts, Cashier, add_Cashier, remove_Cashier, add_PersonalAccounts, add_ExchangeAccounts, delete_Subprojects, delete_Accounts, delete_CashAccounts, delete_PersonalAccounts, delete_ExchangeAccounts, "User_Window");
                }

                MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                User_ID = -1;
                Username_txtBox.Text = string.Empty;
                Password_txtBox.Text = string.Empty;
                Role_comboBox.SelectedIndex = -1;
                LoadGrid();
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

        private void Role_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                Show_Hide_Component();
                Clear_Lists();
                Fill_Comboboxes_Data();
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

        private bool Check_Cahier()
        {
            DataTable dt = u.Check_Cashiers(Cashier, User_ID);
            if (dt.Rows.Count == 0)
            {
                return false;
            }
            else
                return true;
        }
        

    }
}
