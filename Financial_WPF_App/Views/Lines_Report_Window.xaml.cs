using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Lines_Report_Window.xaml
    /// </summary>
    public partial class Lines_Report_Window : Window
    {
        public Lines_Report_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();
            NameCurrencyColumns();
            this.main_Window = main_Window;

            Onload();
        }

        private DataTable dt;
        Main_Window_2 main_Window;
        Models.Action a;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();
        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";

        /// <summary>
        /// Titles the per-currency expense columns with the currencies' real names. The old
        /// currency's column now holds only the old currency: both used to share one column
        /// and the totals under it added old lira to new.
        /// </summary>
        private void NameCurrencyColumns()
        {
            string oldName = Helper.CurrencyService.GetLegacyLocalCurrencyName();
            string newName = Helper.CurrencyService.GetCurrentLocalCurrencyName();
            if (string.IsNullOrWhiteSpace(newName)) newName = "New";

            Expensesyr_DataGridColumn.HeaderText = "Expense(" + oldName + ")";
            ExpenseCurrent_DataGridColumn.HeaderText = "Expense(" + newName + ")";
        }

        private void Onload()
        {
            a = new Models.Action();
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
            }
            // Open on the current year rather than the whole table. The report used to read
            // every action ever recorded - 2021 to today - on every open. "All" is one click
            // away and clears both dates.
            FillYears();
            SetCurrentYear();

            Load_Data();
            Apply_Color_Theme();
            Apply_DataGrid_Color_Theme();
        }

        // Set while code is moving the pickers, so their SelectedDateChanged does not fight
        // the year box that just set them.
        private bool _settingDates;

        private const string AllYears = "All";

        private void FillYears()
        {
            _settingDates = true;

            Year_comboBox.Items.Clear();
            Year_comboBox.Items.Add(AllYears);

            // The oldest action in the database is from 2021; going a little wider costs
            // nothing and keeps the list right if older data is imported later.
            for (int y = DateTime.Today.Year; y >= 2020; y--)
                Year_comboBox.Items.Add(y.ToString());

            _settingDates = false;
        }

        private void SetCurrentYear()
        {
            _settingDates = true;

            int year = DateTime.Today.Year;
            FromDate_datepicker.SelectedDate = new DateTime(year, 1, 1);
            ToDate_datepicker.SelectedDate = new DateTime(year, 12, 31);
            Year_comboBox.SelectedItem = year.ToString();

            _settingDates = false;
        }

        // Picking a year just fills the two dates - one filter underneath, two ways in.
        private void Year_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settingDates || Year_comboBox.SelectedItem == null)
                return;

            string choice = Year_comboBox.SelectedItem.ToString();
            _settingDates = true;

            if (choice == AllYears)
            {
                FromDate_datepicker.SelectedDate = null;
                ToDate_datepicker.SelectedDate = null;
            }
            else
            {
                int year = int.Parse(choice);
                FromDate_datepicker.SelectedDate = new DateTime(year, 1, 1);
                ToDate_datepicker.SelectedDate = new DateTime(year, 12, 31);
            }

            _settingDates = false;
            Load_Data();
        }

        // Typing a date by hand means the range is no longer a whole year.
        private void Date_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_settingDates)
                return;

            _settingDates = true;
            Year_comboBox.SelectedItem = null;
            _settingDates = false;
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

        public void Load_Data()
        {
            LoadGrid();
        }

        // Empty means no bound on that side; both empty reads everything, as before.
        private string SelectedFrom()
        {
            return FromDate_datepicker.SelectedDate == null
                ? ""
                : ((DateTime)FromDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
        }

        private string SelectedTo()
        {
            return ToDate_datepicker.SelectedDate == null
                ? ""
                : ((DateTime)ToDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
        }


        private void Go_btn_Click(object sender, RoutedEventArgs e)
        {
            Load_Data();
        }


        private void LoadGrid()
        {
            string from = SelectedFrom();
            string to = SelectedTo();

            dt = new DataTable();
            dt = a.SelectLines_Report(from, to);

            // The counter used to be written only in myDataGrid_Loaded, which runs once, so it
            // kept showing whatever the first load found no matter how the filter changed.
            All_textBlock.Text = (dt == null ? 0 : dt.Rows.Count).ToString("N0");

            if (dt != null && dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = dt.DefaultView;
                if (HOffset != 0)
                    this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
                if (HiddenColumns.Count != 0)
                {
                    foreach (string CName in HiddenColumns)
                    {
                        for (int i = 0; i < myDataGrid.Columns.Count; i++)
                        {
                            if (myDataGrid.Columns[i].HeaderText.ToString() == CName)
                            {
                                myDataGrid.Columns[i].IsHidden = true; break;
                            }
                        }
                    }
                }
            }
            else
            {
                myDataGrid.ItemsSource = null;
            }

            Bind_Show_Hide_Menu();
        }


        private void Attachment_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                DataRowView dataRow = (DataRowView)myDataGrid.CurrentItem;
                int Action_ID = Convert.ToInt32(dataRow["ID"]);
                Window win = new Attachments(Action_ID, -1, true, null, null, null, null, null, this);
                win.ShowDialog();

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
        

        private void Bind_Show_Hide_Menu()
        {

            SolidColorBrush MyWhite = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF");
            this.ShowMenu.Items.Clear();
            this.HideMenu.Items.Clear();
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                string CHeader = myDataGrid.Columns[i].HeaderText.ToString();
                MenuItem NewItem = new MenuItem();
                NewItem.Header = CHeader;
                NewItem.Background = MyWhite;
                NewItem.StaysOpenOnClick = true;
                if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") &&
                    !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") &&
                    !CHeader.Contains("Select") && !CHeader.Contains("#") &&
                    !Originally_HiddenColumns.Contains(CHeader))
                    if (myDataGrid.Columns[i].IsHidden == false)
                    {
                        NewItem.Click += new RoutedEventHandler(HideColumn);
                        this.HideMenu.Items.Add(NewItem);
                    }
                    else
                    {
                        NewItem.Click += new RoutedEventHandler(ShowColumn);
                        this.ShowMenu.Items.Add(NewItem);
                    }
            }
        }

        private void ShowColumn(object sender, RoutedEventArgs e)
        {
            try
            {
                string CHeader = ((MenuItem)sender).Header.ToString();
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    if (myDataGrid.Columns[i].HeaderText.ToString() == CHeader)
                    {
                        myDataGrid.Columns[i].IsHidden = false;

                        HiddenColumns.RemoveAt(HiddenColumns.IndexOf(CHeader));

                        break;
                    }
                }
                Bind_Show_Hide_Menu();
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

        private void HideColumn(object sender, RoutedEventArgs e)
        {
            try
            {
                string CHeader = ((MenuItem)sender).Header.ToString();
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    if (myDataGrid.Columns[i].HeaderText.ToString() == CHeader)
                    {
                        myDataGrid.Columns[i].IsHidden = true;
                        HiddenColumns.Add(CHeader);

                        break;
                    }
                }
                Bind_Show_Hide_Menu();
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

        private void ShowAll_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    string CHeader = myDataGrid.Columns[i].HeaderText.ToString();
                    if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") && !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") && !CHeader.Contains("#") && !Originally_HiddenColumns.Contains(CHeader))
                    {
                        myDataGrid.Columns[i].IsHidden = false;
                    }
                }
                HiddenColumns.Clear();
                Bind_Show_Hide_Menu();
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

        private void Search_txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Get PlaceholderTextBlock inside the TextBox template
                var placeholder = (TextBlock)Search_txtBox.Template.FindName("PlaceholderText", Search_txtBox);

                if (placeholder != null)
                {
                    // Hide placeholder when user types, show when empty
                    placeholder.Visibility = string.IsNullOrWhiteSpace(Search_txtBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                }

                if (dt != null)
                {
                    SearchDataGrid searchHelper = new SearchDataGrid(dt);

                    // Call the search method when text changes
                    searchHelper.Search(Search_txtBox.Text, myDataGrid);
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
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;
                int count = myDataGrid.View.Records.Count();
                All_textBlock.Text = count.ToString("N0");

                // All_textBlock.Text = myDataGrid.View.Records.Count().ToString();
                Export_btn.CommandTarget = myDataGrid;
            }
        }

        void ScrollOwner_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange > 0 || e.HorizontalChange < 0)
            {
                HOffset = e.HorizontalOffset;
            }
        }

        private void myDataGrid_FilterChanged(object sender, Syncfusion.UI.Xaml.Grid.GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
        }

        private void myDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
        {
            try
            {
                int count = myDataGrid.SelectedItems.Count;
                Selected_textBlock.Text = count.ToString("N0");
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

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;
        }
    }
}
