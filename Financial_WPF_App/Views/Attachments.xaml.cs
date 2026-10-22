using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Microsoft.Win32;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.RowFilter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public Attachments(int Action_ID, int Beneficiary_ID, bool Approved_Action, string Account_Type, string Category, Till_Window TW, Account_Beneficiaries_Window AB, Approval_Sub_Window App, Lines_Report_Window LR)
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
            GroupBy_Panel.Visibility = Visibility.Visible;
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
        int Action_ID; int Beneficiary_ID;
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
        List<int> SelectedAccountIDs;

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
                if (Theme_Color is SolidColorBrush accent)
                    Resources["AccentBrush"] = accent;
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
            filters = new[] { cityFilter, programFilter, projectFilter, accountFilter };
            cityFilter.Box = City_comboBox;
            cityFilter.Required = true;
            programFilter.Box = Program_comboBox;
            projectFilter.Box = Subproject_comboBox;
            accountFilter.Box = Account_comboBox;

            LoadUserCities();
        }

        private class FilterItem : INotifyPropertyChanged
        {
            private bool isChecked;
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsAll { get; set; }
            public bool IsChecked
            {
                get { return isChecked; }
                set
                {
                    if (isChecked == value) return;
                    isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        private class MultiFilter
        {
            public ComboBox Box;
            public List<FilterItem> Items = new List<FilterItem>();
            public bool Required;

            public List<int> SelectedIds
            {
                get { return Items.Where(i => !i.IsAll && i.IsChecked).Select(i => i.Id).ToList(); }
            }

            // Nothing ticked means "no restriction at this level": every item in the list counts,
            // except on the required (City) level where nothing ticked means nothing chosen yet.
            public List<int> EffectiveIds
            {
                get
                {
                    var ids = SelectedIds;
                    if (ids.Count > 0 || Required) return ids;
                    return Items.Where(i => !i.IsAll).Select(i => i.Id).ToList();
                }
            }

            public void Load(DataTable dt)
            {
                var previous = new HashSet<int>(SelectedIds);
                Items = new List<FilterItem>();
                if (dt != null && dt.Rows.Count > 0)
                {
                    Items.Add(new FilterItem { Id = -1, Name = "Select All", IsAll = true });
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        int id = Convert.ToInt32(row[0]);
                        Items.Add(new FilterItem { Id = id, Name = Convert.ToString(row[1]), IsChecked = previous.Contains(id) });
                    }
                    SyncAll();
                }
                Box.ItemsSource = Items;
                Box.IsEnabled = Items.Count > 0;
                UpdateText();
            }

            public void SyncAll()
            {
                var all = Items.FirstOrDefault(i => i.IsAll);
                if (all == null) return;
                var real = Items.Where(i => !i.IsAll).ToList();
                all.IsChecked = real.Count > 0 && real.All(i => i.IsChecked);
            }

            public void UpdateText()
            {
                int count = SelectedIds.Count;
                int total = Items.Count(i => !i.IsAll);
                if (total == 0) Box.Text = "";
                else if (count == 0) Box.Text = Required ? "Select city..." : "All (" + total + ")";
                else Box.Text = count == total ? "All (" + total + ")" : count + " Selected";
            }
        }

        private readonly MultiFilter cityFilter = new MultiFilter();
        private readonly MultiFilter programFilter = new MultiFilter();
        private readonly MultiFilter projectFilter = new MultiFilter();
        private readonly MultiFilter accountFilter = new MultiFilter();
        private MultiFilter[] filters;

        public void LoadUserCities()
        {
            cityFilter.Load(a.get_user_cities(userId, userRole));
            ReloadPrograms();
        }

        private void ReloadPrograms()
        {
            programFilter.Load(a.get_user_programs(userId, cityFilter.EffectiveIds, userRole));
            ReloadProjects();
        }

        private void ReloadProjects()
        {
            projectFilter.Load(a.get_user_projects(userId, cityFilter.EffectiveIds, programFilter.EffectiveIds, userRole));
            ReloadAccounts();
        }

        private void ReloadAccounts()
        {
            accountFilter.Load(a.get_user_accounts(userId, projectFilter.EffectiveIds, userRole));
            SelectedAccountIDs = accountFilter.EffectiveIds;
        }

        private void Filter_Item_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (sender as CheckBox)?.DataContext as FilterItem;
                var filter = filters.FirstOrDefault(f => f.Items.Contains(item));
                if (filter == null) return;

                if (item.IsAll)
                {
                    foreach (var other in filter.Items)
                        other.IsChecked = item.IsChecked;
                }
                else
                {
                    filter.SyncAll();
                }
                filter.UpdateText();

                if (filter == cityFilter) ReloadPrograms();
                else if (filter == programFilter) ReloadProjects();
                else if (filter == projectFilter) ReloadAccounts();
                else SelectedAccountIDs = accountFilter.EffectiveIds;

                filter.Box.IsDropDownOpen = true;
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

        private void Load_Data()
        {
            string FromDate = null; string ToDate = null;
            if (FromDate_datepicker.SelectedDate != null && FromDate_datepicker.SelectedDate.ToString() != "")
                FromDate = ((DateTime)FromDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");


            if (ToDate_datepicker.SelectedDate != null && ToDate_datepicker.SelectedDate.ToString() != "")
                ToDate = ((DateTime)ToDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");

            if (UseDate_check.IsChecked != true)
                FromDate = ToDate = null;

            bool bulkMode = GroupBy_Panel.Visibility == Visibility.Visible;
            bool skipQuery = bulkMode && SelectedAccountIDs != null && SelectedAccountIDs.Count == 0;

            if (SelectedAccountIDs != null)
            {
                attachments_dt = skipQuery ? new DataTable() : a.get_attachments(SelectedAccountIDs, Action_ID, Beneficiary_ID, FromDate, ToDate);

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

                if (dataRow == null)
                    return;

                SaveFileName = dataRow["attach_name"].ToString();
                attachments_id = Convert.ToInt32(dataRow["id"]);

                if (Action_ID != -1)
                {
                    DownloadFileName = "Action_id_" + dataRow["action_id"] + "_" + SaveFileName;
                }
                else if (Beneficiary_ID != -1)
                {
                    DownloadFileName = "Beneficiary_id_" + Beneficiary_ID + "_" + SaveFileName;
                }

                if (!d.file_Exist(DownloadFileName))
                {
                    d.DeleteAttachmentTfromDatabase(
                        attachments_id,
                        SaveFileName,
                        Action_ID,
                        Beneficiary_ID,
                        Approved_Action,
                        "Attachments"
                    );

                    MessageBox.Show("The file is missing - The name has been deleted from the list.", "Error");
                    Load_Data();
                    return;
                }

                var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                SaveDialog(folderDialog.SelectedPath);

                MessageBox.Show("File saved successfully.", "Confirmation");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private sealed class DownloadItem
        {
            public int Id;
            public string Name;
            public string OnlineName;
            public string Folder;
            public string City, Program, Project, Account, Month;
        }

        private string FolderPart(DataRowView row, string column)
        {
            if (!row.Row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return "Unknown";
            string v = CleanFolderName(row[column].ToString().Trim());
            return v.Length == 0 ? "Unknown" : v;
        }

        private async void DownloadSelected_Button_Click(object sender, RoutedEventArgs e)
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

                var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK ||
                    string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                    return;

                string rootName = AskFolderName("Attachments_" + DateTime.Today.ToString("yyyy-MM-dd"));
                if (rootName == null)
                    return;

                string selectedPath = Path.Combine(folderDialog.SelectedPath, CleanFolderName(rootName));
                bool isBulkMode = GroupBy_Panel.Visibility == Visibility.Visible;
                bool groupByAccount = GroupByAccount_Radio.IsChecked == true;
                bool hasAccountCols = attachments_dt.Columns.Contains("account_name") && attachments_dt.Columns.Contains("action_date");

                var items = new List<DownloadItem>();
                foreach (var dataRow in selectedRows)
                {
                    string name = dataRow["attach_name"].ToString();
                    string onlineName = null;
                    if (Action_ID != -1)
                        onlineName = "Action_id_" + dataRow["action_id"].ToString() + "_" + name;
                    else if (Beneficiary_ID != -1)
                        onlineName = "Beneficiary_id_" + Beneficiary_ID.ToString() + "_" + name;

                    var item = new DownloadItem { Id = Convert.ToInt32(dataRow["id"]), Name = name, OnlineName = onlineName, Folder = selectedPath };
                    if (isBulkMode && hasAccountCols)
                    {
                        item.City = FolderPart(dataRow, "city_name");
                        item.Program = FolderPart(dataRow, "program_name");
                        item.Project = FolderPart(dataRow, "project_name");
                        item.Account = FolderPart(dataRow, "account_name");
                        item.Month = dataRow["action_date"] != DBNull.Value
                            ? Convert.ToDateTime(dataRow["action_date"]).ToString("yyyy-MM")
                            : "Unknown";
                    }
                    items.Add(item);
                }

                if (isBulkMode && hasAccountCols)
                {
                    // A level is only added as a folder when the chosen files span more than one of it,
                    // so a single-city download has no pointless City folder; two accounts sharing a name
                    // in different projects/cities never land in the same folder.
                    bool useCity = items.Select(i => i.City).Distinct().Count() > 1;
                    bool useProgram = items.Select(i => i.Program).Distinct().Count() > 1;
                    bool useProject = items.Select(i => i.Project).Distinct().Count() > 1;
                    foreach (var item in items)
                    {
                        var parts = new List<string>();
                        if (useCity) parts.Add(item.City);
                        if (useProgram) parts.Add(item.Program);
                        if (useProject) parts.Add(item.Project);
                        parts.Add(item.Account);
                        if (groupByAccount) parts.Add(item.Month); else parts.Insert(0, item.Month);
                        item.Folder = Path.Combine(new[] { selectedPath }.Concat(parts).ToArray());
                    }
                }

                DownloadAttachments_btn.IsEnabled = false;
                GroupBy_Panel.IsEnabled = false;
                circularProgressBar.Progress = 0;
                Percentage_label.Content = "0 / " + items.Count;
                circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Visible;

                var missing = new List<DownloadItem>();
                int done = 0;
                var progress = new Progress<int>(n =>
                {
                    circularProgressBar.Progress = n * 100.0 / items.Count;
                    Percentage_label.Content = n + " / " + items.Count;
                });

                await Task.Run(() =>
                {
                    foreach (var item in items)
                    {
                        if (!d.file_Exist(item.OnlineName))
                        {
                            missing.Add(item);
                        }
                        else
                        {
                            Directory.CreateDirectory(item.Folder);
                            SaveFileTo(Login.OnlineRoot + "/" + item.OnlineName, item.Folder, item.Name);
                        }
                        ((IProgress<int>)progress).Report(++done);
                    }
                });

                foreach (var item in missing)
                    d.DeleteAttachmentTfromDatabase(item.Id, item.Name, Action_ID, Beneficiary_ID, Approved_Action, "Attachments");

                circularProgressBar.Progress = 100;
                await Task.Delay(500);
                circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Collapsed;

                MessageBox.Show("Saved " + (items.Count - missing.Count) + " file(s) to:" + Environment.NewLine + selectedPath +
                    (missing.Count > 0 ? Environment.NewLine + missing.Count + " missing file(s) were removed from the list." : ""),
                    "Confirmation");
                Load_Data();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                DownloadAttachments_btn.IsEnabled = true;
                GroupBy_Panel.IsEnabled = true;
                circularProgressBar.Visibility = Percentage_label.Visibility = Visibility.Collapsed;
            }
        }

        private string AskFolderName(string defaultName)
        {
            string result = null;
            var box = new TextBox { Text = defaultName, Margin = new Thickness(0, 6, 0, 12), Padding = new Thickness(6, 4, 6, 4), FontSize = 13 };
            var ok = new Button { Content = "Create & Download", IsDefault = true, Padding = new Thickness(16, 5, 16, 5), Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(16, 5, 16, 5) };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = "Folder that will hold all downloaded files:", FontSize = 12 });
            panel.Children.Add(box);
            panel.Children.Add(buttons);
            var win = new Window
            {
                Title = "Download folder name",
                Content = panel,
                Width = 380,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ShowInTaskbar = false
            };
            ok.Click += (s, a) =>
            {
                if (string.IsNullOrWhiteSpace(box.Text)) return;
                result = box.Text.Trim();
                win.DialogResult = true;
            };
            win.Loaded += (s, a) => { box.Focus(); box.SelectAll(); };
            win.ShowDialog();
            return result;
        }

        private void SaveFileTo(string onlineFilePath, string folder, string fileName)
        {
            string fullPath = Path.Combine(folder, fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            int counter = 1;
            while (File.Exists(fullPath))
            {
                fullPath = Path.Combine(folder, $"{name}({counter}){ext}");
                counter++;
            }
            d.CopyToLocal(onlineFilePath, fullPath);
        }

        private string CleanFolderName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private void SaveDialog(string selectedPath)
        {
            string OnlineFilePath = Login.OnlineRoot + "/" + DownloadFileName;

            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            string fullPath = System.IO.Path.Combine(selectedPath, SaveFileName);

            string name = System.IO.Path.GetFileNameWithoutExtension(SaveFileName);
            string ext = System.IO.Path.GetExtension(SaveFileName);

            int counter = 1;

            while (System.IO.File.Exists(fullPath))
            {
                string newFileName = $"{name}({counter}){ext}";
                fullPath = System.IO.Path.Combine(selectedPath, newFileName);
                counter++;
            }

            d.CopyToLocal(OnlineFilePath, fullPath);
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

        private void comboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space || e.Key == Key.Back || e.Key == Key.Delete;
        }

        private void comboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        private void ValidateDates(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (FromDate_datepicker.SelectedDate != null && ToDate_datepicker.SelectedDate != null)
            {
                if (ToDate_datepicker.SelectedDate < FromDate_datepicker.SelectedDate)
                {
                    MessageBox.Show(
                        "To Date cannot be earlier than From Date",
                        "Invalid Date",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    ToDate_datepicker.SelectedDate = FromDate_datepicker.SelectedDate;
                }
            }
        }
    }
}
