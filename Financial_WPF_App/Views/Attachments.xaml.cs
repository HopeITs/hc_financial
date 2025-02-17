using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Attachments.xaml
    /// </summary>
    public partial class Attachments : Window
    {
        public Attachments(int Action_ID, int Beneficiary_ID, bool Approved_Action, string Account_Type, string Category, Till_Window TW, Account_Beneficiaries_Window AB, Approval_Sub_Window App)
        {
            InitializeComponent();
            this.TW = TW;
            this.AB = AB;
            this.App = App;
            this.Action_ID = Action_ID;
            this.Beneficiary_ID = Beneficiary_ID;
            this.Account_Type = Account_Type;
            this.Category = Category;
            this.Approved_Action = Approved_Action;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        Document d;
        DataTable attachments_dt = new DataTable();
        int Action_ID; int Beneficiary_ID;
        private string Account_Type; private string Category;
        private OpenFileDialog open;
        private string[] FullAttachNames;
        private Thread myTh;

        private System.Windows.Forms.FolderBrowserDialog FolderBrowserDialog1;

        string DownloadFileName;
        string SaveFileName;
        int attachments_id;

        bool Approved_Action;
        Till_Window TW;
        Account_Beneficiaries_Window AB;
        Approval_Sub_Window App;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];

        private bool OnLoad()
        {
            try
            {
                circularProgressBar.Visibility = Visibility.Collapsed;
                d = new Document();
                Load_Data();
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                WindowUtils.UpdateTextBlocksForeground(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Load_Data()
        {
            if (Login.BuildConnection())
            {
                attachments_dt = d.GetAttachmentsNames(Action_ID, Beneficiary_ID);
                if (attachments_dt != null && attachments_dt.Rows.Count > 0)
                {
                    myDataGrid.ItemsSource = attachments_dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
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
                    fileName = "Action_id_" + Action_ID.ToString() + "_" + dataRow["attach_name"].ToString();

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
                    MessageBox.Show("The file is missing - The name has been deleted from the list.", "Error",MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                    DownloadFileName = "Action_id_" + Action_ID.ToString() + "_" + dataRow["attach_name"].ToString();
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
                    MessageBox.Show("The file is missing - The name has been deleted from the list.","Error" ,MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
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
                MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
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
                    MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                }
                else
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

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
                circularProgressBar.SegmentCount = totalFiles;
                circularProgressBar.Visibility = Visibility.Visible;
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


                    bool uploadSuccess = await Task.Run(() => d.SaveDocument(FullAttachName, fileName));

                    if (uploadSuccess)
                    {
                        d.AddAttachmentToDatabase(Action_ID, Beneficiary_ID, t,Approved_Action, "AddAction_Window");
                        success++;
                    }
                    else
                    {
                        failurText = "Alert!!!!" + Environment.NewLine + "Not all files have been uploaded.";
                    }

                    currentFile++;
                }
                circularProgressBar.Progress = 100;
                await Task.Delay(1000);
                circularProgressBar.Visibility = Visibility.Collapsed;


                if (failurText != "")
                    MessageBox.Show(failurText, "", MessageBoxButton.OK, MessageBoxImage.Stop);

                MessageBox.Show("Attachments:" + Environment.NewLine + "Succeeded - " + success.ToString() + Environment.NewLine + "Failed - " + (totalFiles - success).ToString(), "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                Load_Data();


                if (TW != null)
                    TW.Load_Data();


                if (AB != null)
                    AB.Load_Data();

                if(App != null)
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }
    }
}
