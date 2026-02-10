using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Microsoft.Win32;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.RowFilter;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Attachments.xaml
    /// </summary>
    public partial class Attachments : Window
    {
        public Attachments(int Action_ID, int Beneficiary_ID, bool Approved_Action, string Account_Type, string Category, Till_Window TW, Account_Beneficiaries_Window AB, Approval_Sub_Window App , Lines_Report_Window LR)
        {
            InitializeComponent();
            this.TW = TW;
            this.AB = AB;
            this.App = App;
            this.LR = LR;
            this.Action_ID = Action_ID;
            this.Beneficiary_ID = Beneficiary_ID;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.Approved_Action = Approved_Action;
            SelectedAccountIDs = new List<int>();
            FilterRow2.Height = new GridLength(0);
            FilterPanel2.Visibility = Visibility.Hidden;
            FilterRow1.Height = new GridLength(0);
            FilterPane11.Visibility = Visibility.Hidden;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        public Attachments(int? Account_ID)
        {
            InitializeComponent();
            SelectedAccountIDs = new List<int>();
            SelectedAccountIDs.Add(Account_ID.Value);
            AddAttachments_btn.Visibility = Visibility.Hidden;
            Delete_DataGridColumn.IsHidden = true;
            FilterRow1.Height = new GridLength(0);
            FilterPane11.Visibility = Visibility.Hidden;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }
        public Attachments()
        {
            InitializeComponent();
            AddAttachments_btn.Visibility = Visibility.Hidden;
            FilterPane11.Visibility = Visibility.Visible;
            Delete_DataGridColumn.IsHidden = true;
            SelectedAccountIDs = new List<int>();
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }
        Document d;
        Account a;
        DataTable attachments_dt = new DataTable();
        int Action_ID; int Beneficiary_ID ;
        private string Account_Type; private string Category;
        private OpenFileDialog open;
        private string[] FullAttachNames;
        private Thread myTh;
        int userId = Properties.Settings.Default.User_ID;
        string userRole = Properties.Settings.Default.Role;

        private System.Windows.Forms.FolderBrowserDialog FolderBrowserDialog1;

        string DownloadFileName;
        string SaveFileName;
        int attachments_id;

        bool Approved_Action;
        Till_Window TW;
        Account_Beneficiaries_Window AB;
        Approval_Sub_Window App;
        Lines_Report_Window LR;
        DateTime to_Date = new DateTime();
        DateTime from_Date = new DateTime(); 
        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";
        List<Local_Account> objAccountList;
        List<Local_Account> FilteredAccountList;
        List<Local_Account> Original_AccountList;
        List<int> SelectedAccountIDs;

        private class Local_Account
        {
            public int AccID { set; get; }

            public string AccName { set; get; }

            public Boolean Acc_Check_Status { set; get; }
        }
        private bool OnLoad()
        {
            try
            {
                circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Collapsed;
                d = new Document();
                a = new Account();
                Load_Data();
                Fill_Comboboxes();
                Apply_Color_Theme();
                Apply_DataGrid_Color_Theme();
                to_Date = DateTime.Today;
                from_Date = to_Date.AddMonths(-1);

                string ToDate = to_Date.ToString("yyyy-MM-dd");
                string FromDate = from_Date.ToString("yyyy-MM-dd");
                FromDate_datepicker.Text = FromDate;
                ToDate_datepicker.Text = ToDate;
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

        private void Fill_Comboboxes()
        {
            LoadUserCities();
        }

        public void LoadUserCities()
        {
            DataTable dt = a.get_user_cities(userId,userRole);

            City_comboBox.ItemsSource = dt.DefaultView;
            City_comboBox.DisplayMemberPath = "name";
            City_comboBox.SelectedValuePath = "id";

            City_comboBox.SelectedIndex = -1;
        }

        public void LoadUserProjects(int city_id)
        {
            DataTable dt = a.get_user_projects(userId, city_id, userRole);

            Subproject_comboBox.ItemsSource = dt.DefaultView;
            Subproject_comboBox.DisplayMemberPath = "name";
            Subproject_comboBox.SelectedValuePath = "id";

            Subproject_comboBox.SelectedIndex = -1;
        }

        public void LoadUserAccounts(int subproject_id)
        {
            DataTable dt = a.get_user_accounts(userId, subproject_id, userRole);
            List<string> AllAccount_ID = new List<string>();
            Original_AccountList = new List<Local_Account>();
            objAccountList = new List<Local_Account>();

            if (dt != null && dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Local_Account obj = new Local_Account();
                    obj.AccID = dt.Rows[i].Field<int>(0);
                    obj.AccName = dt.Rows[i].Field<string>(1);
                    objAccountList.Add(obj);
                }
                Account_comboBox.ItemsSource = objAccountList;
                int count = objAccountList.Count(item => item.Acc_Check_Status == true);
                Account_comboBox.Text = count.ToString() + " Selected";
            }
        }

        private void Account_Checkbox_Item_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                //FilteredAccountList = new List<Local_Account>();
                //    if (objAccountList != null && objAccountList.Count > 0)
                //    {
                //        foreach (var account in objAccountList)
                //        {
                //            if (account.Acc_Check_Status == true)
                //            FilteredAccountList.Add(account);
                //        }
                //    }

                //    if (FilteredAccountList != null && FilteredAccountList.Count > 0)
                //    {
                //        Account_comboBox.ItemsSource = null;
                //    Account_comboBox.Text = "0 Selected";
                //    Account_comboBox.ItemsSource = FilteredAccountList;
                //        int count = objAccountList.Count(item => item.Acc_Check_Status == true);
                //    Account_comboBox.Text = count.ToString() + " Selected";
                //    }
                //    else
                //    {
                //    Account_comboBox.ItemsSource = null;
                //    Account_comboBox.Text = "0 Selected";
                //    Account_comboBox.ItemsSource = objAccountList;
                //        int count = objAccountList.Count(item => item.Acc_Check_Status == true);
                //    Account_comboBox.Text = count.ToString() + " Selected";
                //    }



                //int AccoutCount = objAccountList.Count(item => item.Acc_Check_Status == true);
                //Account_comboBox.Text = AccoutCount.ToString() + " Selected";
                if (objAccountList == null || objAccountList.Count == 0)
                {
                    Account_comboBox.Text = "0 Selected";
                    return;
                }

                int selectedCount = objAccountList.Count(x => x.Acc_Check_Status);
                SelectedAccountIDs = new List<int>();
                Account_comboBox.Text = $"{selectedCount} Selected";

                Account_comboBox.IsDropDownOpen = true;
                SelectedAccountIDs = objAccountList.Where(x => x.Acc_Check_Status).Select(x => x.AccID).ToList();
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

        private void Load_Data()
        {
            string FromDate = null; string ToDate = null;
            if (FromDate_datepicker.SelectedDate != null && FromDate_datepicker.SelectedDate.ToString() != "")
                FromDate = ((DateTime)FromDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");


            if (ToDate_datepicker.SelectedDate != null && ToDate_datepicker.SelectedDate.ToString() != "")
                ToDate = ((DateTime)ToDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

            if (SelectedAccountIDs != null)
            {
                attachments_dt = a.get_attachments(SelectedAccountIDs, Action_ID,Beneficiary_ID, FromDate, ToDate);
                
                if (attachments_dt != null && attachments_dt.Rows.Count > 0)
                {
                    System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(bool));
                    attachments_dt.Columns.Add(newColumn);
                    attachments_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", false));
                    myDataGrid.ItemsSource = attachments_dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }

                if (myDataGrid.View != null)
                {
                    int count = myDataGrid.View.Records.Count();
                    All_Label.Content = count.ToString("N0");
                    DownloadAttachments_btn.Visibility = Visibility.Visible;
                    DownloadAttachments_btn.Width = 24;
                }
                else
                {
                    All_Label.Content = "0";
                    DownloadAttachments_btn.Visibility = Visibility.Hidden;
                    All_Label.Content = "0";
                }
            }
            else
            {
                attachments_dt = a.get_attachments(SelectedAccountIDs, Action_ID, Beneficiary_ID, FromDate, ToDate);
                //attachments_dt = d.GetAttachmentsNames(Action_ID, Beneficiary_ID);
                if (attachments_dt != null && attachments_dt.Rows.Count > 0)
                {
                    System.Data.DataColumn newColumn = new System.Data.DataColumn("IsSelected", typeof(bool));
                    attachments_dt.Columns.Add(newColumn);
                    attachments_dt.AsEnumerable().ToList().ForEach(row => row.SetField("IsSelected", false));
                    myDataGrid.ItemsSource = attachments_dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }

                if (myDataGrid.View != null)
                {
                    int count = myDataGrid.View.Records.Count();
                    All_Label.Content = count.ToString("N0");
                    DownloadAttachments_btn.Visibility = Visibility.Visible;
                    DownloadAttachments_btn.Width = 24;
                }
                else
                {
                    All_Label.Content = "0";
                    DownloadAttachments_btn.Visibility = Visibility.Hidden;
                    All_Label.Content = "0";
                }
            }
        }

        private void View_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                string fileName = "";
                if (Action_ID != -1)
                {
                    fileName = "Action_id_" + dataRow["action_id"].ToString() + "_" + dataRow["attach_name"].ToString();

                }
                else if (Beneficiary_ID != -1)
                {
                    fileName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + dataRow["attach_name"].ToString();
                }
                int attachments_id = Convert.ToInt32(dataRow["id"].ToString());
                string OnlineFilePath = Login.OnlineRoot + @"/" + fileName;
                if (d.file_Exist(fileName))
                {
                    string Type;
                    d.GetNameFromPath(OnlineFilePath, out _, out Type);
                    string tempDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Financial\attachments";

                    System.IO.Directory.CreateDirectory(tempDirectory);

                    //Clean Temp Directory
                    System.IO.DirectoryInfo di = new DirectoryInfo(tempDirectory);
                    foreach (FileInfo allfiles in di.GetFiles())
                    {
                        try
                        {
                            allfiles.Delete();
                        }
                        catch { }

                    }

                    FileInfo file = new FileInfo(tempDirectory + "\\" + fileName);

                    d.CopyToLocal(OnlineFilePath, tempDirectory + "\\" + fileName);

                    System.Diagnostics.Process.Start(tempDirectory + "\\" + fileName);
                }
                else
                {
                    d.DeleteAttachmentTfromDatabase(attachments_id, dataRow["attach_name"].ToString(), Action_ID, Beneficiary_ID, Approved_Action, "Attachments");
                    MessageBox.Show("The file is missing - The name has been deleted from the list.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    Load_Data();
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
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                string fileName = "";

                if (Action_ID != -1)
                {
                    fileName = "Action_id_" + Action_ID.ToString() + "_" + dataRow["attach_name"].ToString();
                }
                else if (Beneficiary_ID != -1)
                {
                    fileName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + dataRow["attach_name"].ToString();
                }
                int attachments_id = Convert.ToInt32(dataRow["id"].ToString());
                string OnlineFilePath = Login.OnlineRoot + @"/" + fileName;

                d.DeleteOnline(OnlineFilePath);
                d.DeleteAttachmentTfromDatabase(attachments_id, dataRow["attach_name"].ToString(), Action_ID, Beneficiary_ID, Approved_Action, "Attachments");
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

        private void Download_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                if (Action_ID != -1)
                {
                    DownloadFileName = "Action_id_" + dataRow["action_id"].ToString() + "_" + dataRow["attach_name"].ToString();
                }
                else if (Beneficiary_ID != -1)
                {
                    DownloadFileName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + dataRow["attach_name"].ToString();
                }
                SaveFileName = dataRow["attach_name"].ToString();
                attachments_id = Convert.ToInt32(dataRow["id"].ToString());
                if (d.file_Exist(DownloadFileName))
                {
                    myTh = new Thread(SaveDialog);
                    myTh.SetApartmentState(ApartmentState.STA);
                    myTh.Start();
                    myTh.Join();
                }
                else
                {
                    d.DeleteAttachmentTfromDatabase(attachments_id, SaveFileName, Action_ID, Beneficiary_ID, Approved_Action, "Attachments");
                    MessageBox.Show("The file is missing - The name has been deleted from the list.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    Load_Data();
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

        private void DownloadSelected_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedRows = myDataGrid.View.Records
                    .Select(r => r.Data as DataRowView)
                    .Where(r => r != null && Convert.ToInt32(r["IsSelected"]) == 1)
                    .ToList();

                if (selectedRows.Count == 0)
                {
                    MessageBox.Show("Please select at least one file.", "Warning");
                    return;
                }

                foreach (var dataRow in selectedRows)
                {
                    string saveFileName = dataRow["attach_name"].ToString();
                    int attachmentId = Convert.ToInt32(dataRow["id"]);


                    if (Action_ID != -1)
                    {
                        DownloadFileName = "Action_id_" + dataRow["action_id"].ToString() + "_" + saveFileName;
                    }
                    else if (Beneficiary_ID != -1)
                    {
                        DownloadFileName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + saveFileName;
                    }

                    if (d.file_Exist(DownloadFileName))
                    {
                        myTh = new Thread(SaveDialog);
                        myTh.SetApartmentState(ApartmentState.STA);
                        myTh.Start();
                        myTh.Join();
                    }
                    else
                    {
                        d.DeleteAttachmentTfromDatabase(
                            attachmentId,
                            saveFileName,
                            Action_ID,
                            Beneficiary_ID,
                            Approved_Action,
                            "Attachments"
                        );
                        MessageBox.Show("The file is missing - The name has been deleted from the list.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    }
                }

                Load_Data();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void SaveDialog()
        {
            string OnlineFilePath = Login.OnlineRoot + @"/" + DownloadFileName;

            FolderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            var res = FolderBrowserDialog1.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK && FolderBrowserDialog1.SelectedPath != "")
            {
                FileInfo file = new FileInfo(FolderBrowserDialog1.SelectedPath + "\\" + SaveFileName);

                while (file.Exists.Equals(true))
                {
                    SaveFileName = "(1)" + SaveFileName;
                    file = new FileInfo(FolderBrowserDialog1.SelectedPath + "\\" + SaveFileName);
                }

                d.CopyToLocal(OnlineFilePath, FolderBrowserDialog1.SelectedPath + "\\" + SaveFileName);
                MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
            }


        }

        private void AddAttachments_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                myTh = new Thread(CallDialog);
                myTh.SetApartmentState(ApartmentState.STA);
                myTh.Start();
                myTh.Join();

                // Call the async method and handle any exceptions
                HandleAddAttachmentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                {
                    MessageBox.Show("please check your network and try again", "Error");
                }
                else if (ex.Message.Contains("host"))
                {
                    MessageBox.Show("please check the server or your network connection and try again", "Error");
                }
                else if (ex.Message.Contains("Duplicate entry"))
                {
                    MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                }
                else
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        private void chkHeader_Loaded(object sender, RoutedEventArgs e)
        {
            headerCheckbox = sender as CheckBox;
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
                    myDataGrid.View.Refresh();
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
        private CheckBox headerCheckbox;
        private async Task HandleAddAttachmentsAsync()
        {
            await Save_Attachments();
        }


        private void CallDialog()
        {
            open = new OpenFileDialog();
            //open.Filter = "Image Files|*.jpg;*.jpeg;*.png;";
            open.Multiselect = true;
            var res = open.ShowDialog();
            if (res == true)
                FullAttachNames = open.FileNames;
            else
                FullAttachNames = new string[] { };
        }


        private async Task Save_Attachments()
        {
            if (FullAttachNames.Length > 0)
            {
                string failurText = "";
                int totalFiles = FullAttachNames.Length;
                int currentFile = 0;

                circularProgressBar.Progress = 0;
                Percentage_label.Content = "0%";

                //if (totalFiles < 8)
                //    circularProgressBar.SegmentCount = totalFiles * 4;
                //else
                //    circularProgressBar.SegmentCount = totalFiles;

                circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Visible;
                int success = 0;
                foreach (var FullAttachName in FullAttachNames)
                {
                    string t;
                    d.GetNameFromPath(FullAttachName, out t, out _);
                    string fileName = "";

                    if (Action_ID != -1)
                    {
                        fileName = "Action_id_" + Action_ID.ToString() + "_" + t;
                    }
                    else if (Beneficiary_ID != -1)
                    {
                        fileName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + t;
                    }

                    while (d.file_Exist(fileName))
                    {
                        t = "(1)" + t;
                        if (Action_ID != -1)
                        {
                            fileName = "Action_id_" + Action_ID.ToString() + "_" + t;
                        }
                        else if (Beneficiary_ID != -1)
                        {
                            fileName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + t;
                        }
                    }

                    // Calculate the progress percentage
                    double progress = ((double)currentFile / totalFiles) * 100;

                    // Update the circular progress bar value
                    circularProgressBar.Progress = progress;
                    Percentage_label.Content = progress.ToString("N0") + "%";


                    bool uploadSuccess = await Task.Run(() => d.SaveDocument(FullAttachName, fileName));

                    if (uploadSuccess)
                    {
                        d.AddAttachmentToDatabase(Action_ID, Beneficiary_ID, t, Approved_Action, "AddAction_Window");
                        success++;
                    }
                    else
                    {
                        failurText = "Alert!!!!" + Environment.NewLine + "Not all files have been uploaded.";
                    }

                    currentFile++;
                }
                circularProgressBar.Progress = 100;
                Percentage_label.Content = "100%";

                await Task.Delay(1000);
                circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Collapsed;


                if (failurText != "")
                    MessageBox.Show(failurText, "", MessageBoxButton.OK, MessageBoxImage.Stop);

                MessageBox.Show("Attachments:" + Environment.NewLine + "Succeeded - " + success.ToString() + Environment.NewLine + "Failed - " + (totalFiles - success).ToString(), "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                Load_Data();


                if (TW != null)
                    TW.Load_Data();


                if (AB != null)
                    AB.Load_Data();

                if (App != null)
                    App.Load_Data();
            }
        }

        private void myDataGrid_Drop(object sender, DragEventArgs e)
        {
            try
            {
                FullAttachNames = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                HandleAddAttachmentsAsync().ConfigureAwait(false);
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

        private void myDataGrid_CurrentCellValueChanged(object sender, CurrentCellValueChangedEventArgs e)
        {
            var columnIndex = this.myDataGrid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex);
            var column = myDataGrid.Columns[columnIndex];
            if (column.MappingName == "IsSelected" && headerCheckbox != null)
            {
                var rows = attachments_dt.AsEnumerable();

                bool allChecked = rows.All(r => r.Field<bool>("IsSelected"));
                bool anyChecked = rows.Any(r => r.Field<bool>("IsSelected"));

                headerCheckbox.IsChecked =
                    allChecked ? true :
                    anyChecked ? (bool?)null :
                    false;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowUtils.MinimizeWindow(this); // Calls the WindowUtils method
        }


        private void Close_Click(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowUtils.CloseWindow(this); // Calls the WindowUtils method

        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowUtils.TitleBar_MouseDown(this, e);
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {

                int count = myDataGrid.View.Records.Count();
                All_Label.Content = count.ToString("N0");
            }
           
        }

        private void Apply_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //GridFilled = true;
                
                Load_Data();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void City_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (City_comboBox.SelectedValue == null)
                return;

            int cityId = Convert.ToInt32(City_comboBox.SelectedValue);

            LoadUserProjects(cityId); // your next method
            Subproject_comboBox.SelectedIndex = -1;
            Subproject_comboBox.IsEnabled = true;
            Account_comboBox.ItemsSource = null;
        }

        private void Subproject_comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Subproject_comboBox.SelectedValue == null)
                return;

            int subprojectId = Convert.ToInt32(Subproject_comboBox.SelectedValue);

            LoadUserAccounts(subprojectId); // your next method
            Account_comboBox.SelectedIndex = -1;
            Account_comboBox.IsEnabled = true;
        }

        

        private void comboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space || e.Key == Key.Back || e.Key == Key.Delete;
        }

        private void comboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }
    }
}
