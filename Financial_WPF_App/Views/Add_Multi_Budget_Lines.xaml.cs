using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Add_Multi_Budget_Lines.xaml
    /// </summary>
    public partial class Add_Multi_Budget_Lines : Window
    {
        public Add_Multi_Budget_Lines(int Budget_ID, string Budget_Name, int Project_ID, Main_Window_2 main_Window, AllAccountBudgetLines_Windows Bl_W)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.Bl_W = Bl_W;
            this.Budget_ID = Budget_ID;
            this.Budget_Name = Budget_Name;
            this.Project_ID = Project_ID;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        Main_Window_2 main_Window;
        AllAccountBudgetLines_Windows Bl_W;

        int Budget_ID;
        string Budget_Name;
        int Project_ID;
        private Budget_Lines bl;

        private DataTable dt, MissingLiens_dt;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"];

        private MultiBudgetLines_CustomCopyPaste copyPasteHandler;

        private bool OnLoad()
        {
            try
            {
                bl = new Budget_Lines();

                Create_Columns();
                Fill_Datatables();
                InitializeCopyPaste();
                //this.myDataGrid.GridCopyPaste = new MultiBudgetLines_CustomCopyPaste(myDataGrid, Budget_ID, Budget_Name, MissingLiens_dt);
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

        void InitializeCopyPaste()
        {
            copyPasteHandler = new MultiBudgetLines_CustomCopyPaste(myDataGrid, Budget_ID, Budget_Name, MissingLiens_dt);
            this.myDataGrid.GridCopyPaste = copyPasteHandler;
            copyPasteHandler.PasteCompleted += CopyPasteHandler_PasteCompleted;
        }

        private void Apply_Color_Theme()
        {


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Create_Columns()
        {

            Login.MultiBudgetLines_AnyError = false;
            Login.MultiBudgetLines_List.Clear();
            Login.MultiBudgetLines_sc.Parameters.Clear();
            //Login.ErrorRows.Clear();

            dt = new DataTable();

            dt.Columns.Add("Line");
            dt.Columns.Add("Unit");
            dt.Columns.Add("Timing");
            dt.Columns.Add("Price");

            System.Data.DataRow newRow = dt.NewRow();
            dt.Rows.Add(newRow);

            myDataGrid.ItemsSource = null;
            myDataGrid.ItemsSource = dt.DefaultView;



            All_textBlock.Text = "0";
            Error_textBlock.Text = "0";
        }

        private void Fill_Datatables()
        {
            MissingLiens_dt = bl.Select_Missing_Lines(Budget_ID, Project_ID);
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Create_Columns();

                InitializeCopyPaste();
                //this.myDataGrid.GridCopyPaste = new MultiBudgetLines_CustomCopyPaste(myDataGrid,  Budget_ID,  Budget_Name,  MissingLiens_dt);
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

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Login.MultiBudgetLines_AnyError)
                {
                    MessageBox.Show("Please make sure there are no errors in the entered data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }
                else
                {
                    if (Login.MultiBudgetLines_List.Count > 0)
                    {
                        if (Login.BuildConnection())
                        {
                            bl.Insert_Multi_Lines();

                            MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                            Create_Columns();
                            Fill_Datatables();
                            InitializeCopyPaste();
                            //this.myDataGrid.GridCopyPaste = new MultiBudgetLines_CustomCopyPaste(myDataGrid, Budget_ID, Budget_Name, MissingLiens_dt);

                            if (Bl_W != null)
                                Bl_W.Fill_myDataGrid(false);
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
            finally
            {
                Save_btn.IsEnabled = true;
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
            // Now you can access the copyPasteHandler
            All_textBlock.Text = copyPasteHandler.TotalPastedRows.ToString("N0");
            Error_textBlock.Text = copyPasteHandler.TotalErrorRows.ToString("N0");
        }
    }
}
