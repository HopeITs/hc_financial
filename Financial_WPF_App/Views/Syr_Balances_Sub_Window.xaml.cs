using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Syncfusion.Data;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Syr_Balances_Sub_Window.xaml
    /// </summary>
    public partial class Syr_Balances_Sub_Window : Window
    {
        public Syr_Balances_Sub_Window(int TillType_ID, string TillType_Name, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.TillType_ID = TillType_ID;
            this.TillType_Name = TillType_Name;

            PassException = true;
            OnLoad();
            PassException = false;
        }


        private Rate r; private Donors d;
        private DataTable dt, Donor_dt;
        Main_Window_2 main_Window;

        StackedHeaderRow StackedHeaderRow1 = new StackedHeaderRow { };


        int TillType_ID; string TillType_Name;
        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();
        bool PassException = false;

        bool ShowTotals;
        bool UserMode;

        double HOffset = 0;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];



        private void OnLoad()
        {
            UserMode = false;
            r = new Rate();
            d = new Donors();

            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
            }
            Load_Data();
            Apply_Color_Theme();
            UserMode = true;
        }

        private void Apply_Color_Theme()
        {

            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Load_Data()
        {
            Fill_myDataGrid(true);
        }

        public void Fill_myDataGrid(bool Reload)
        {
            myDataGrid.ItemsSource = null;

            if (Reload)
            {
                Donor_dt = d.Select("","","");
                dt = r.Select_Syr_Balances(TillType_ID);
            }


            if (dt != null && dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = dt.DefaultView;

                if (HOffset != 0)
                    this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
            }
            else
            {
                myDataGrid.ItemsSource = null;
            }

        }

        private void myDataGrid_AutoGeneratingColumn(object sender, Syncfusion.UI.Xaml.Grid.AutoGeneratingColumnArgs e)
        {

            try
            {
                if (e.Column is GridNumericColumn && !e.Column.MappingName.Contains("_ID") && e.Column.MappingName != "ID")
                {
                    (e.Column as GridNumericColumn).NumberDecimalDigits = 2;
                    (e.Column as GridNumericColumn).NumberDecimalSeparator = ".";
                    (e.Column as GridNumericColumn).NumberGroupSizes = new Int32Collection { 3 };
                    (e.Column as GridNumericColumn).NumberGroupSeparator = ",";
                }

                e.Column.TextAlignment = TextAlignment.Left;
                e.Column.VerticalAlignment = VerticalAlignment.Center;

                //HIDE ID column.
                if (e.Column.MappingName.Contains("_ID") || e.Column.MappingName == "ID")
                    e.Cancel = true;


                if (ShowTotals)
                {
                    var Dononr = Donor_dt.AsEnumerable().Select(row => row.Field<string>("Name"));
                    if (Dononr.Any(name => e.Column.MappingName.Contains(name)))
                    {
                        e.Cancel = true;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_ItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e)
        {
            try
            {
                if (myDataGrid.ItemsSource != null)
                {
                    if (myDataGrid.StackedHeaderRows.Count != 0)
                        myDataGrid.StackedHeaderRows.RemoveAt(0);
                }

                if (myDataGrid.ItemsSource != null && !ShowTotals)
                {
                    add_stackedHeader(myDataGrid);
                    //rename_Columns();
                }

 
                Bind_Show_Hide_Menu();
                AddSums();
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
                {


                    if (myDataGrid.Columns[i].IsHidden == false)
                    {
                        bool found = false;

                        for (int j = 0; j < this.HideMenu.Items.Count; j++)
                        {
                            if (((MenuItem)this.HideMenu.Items[j]).Header.ToString() == CHeader)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            NewItem.Click += new RoutedEventHandler(HideColumn);
                            this.HideMenu.Items.Add(NewItem);
                        }

                    }
                    else
                    {
                        bool found = false;

                        for (int j = 0; j < this.ShowMenu.Items.Count; j++)
                        {
                            if (((MenuItem)this.ShowMenu.Items[j]).Header.ToString() == CHeader)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            NewItem.Click += new RoutedEventHandler(ShowColumn);
                            this.ShowMenu.Items.Add(NewItem);
                        }
                    }
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

                        //break;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
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

                        //break;
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;

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

        private void Total_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = true;
                if (UserMode)
                {
                    Fill_myDataGrid(false);
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

        private void PerDonor_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowTotals = false;
                if (UserMode)
                {
                    Fill_myDataGrid(false);
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

        private void add_stackedHeader(SfDataGrid MyDatagrid)
        {
            var BudgetDononr = Donor_dt.AsEnumerable().Select(row => row.Field<string>("Name"));

            StackedHeaderRow1 = new StackedHeaderRow { };

            for (int i = 0; i < MyDatagrid.Columns.Count; i++)
            {
                if (MyDatagrid.Columns[i].MappingName != null)
                {
                    int moved = 0;
                    if (!BudgetDononr.Any(name => MyDatagrid.Columns[i].MappingName.Contains(name)))
                    {
                        string StacHeader = MyDatagrid.Columns[i].MappingName;
                        var NewStackedColumn = new StackedColumn
                        {
                            HeaderText = StacHeader,
                            MappingName = StacHeader.Replace(" ", "")
                        };
                        if (MyDatagrid.Columns[i].MappingName.ToLower() != "city"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "project"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "subproject"
                            && MyDatagrid.Columns[i].MappingName.ToLower() != "account")
                        {
                            moved = stack_and_goup_columns(i, StacHeader, NewStackedColumn, MyDatagrid);
                        }
                    }
                    i += moved;
                }
            }
            MyDatagrid.StackedHeaderRows.Add(StackedHeaderRow1);

            //Default GridStackedCellRenderer is removed.
            MyDatagrid.CellRenderers.Remove("StackedHeader");

            //Customized GridStackedCellRenderer is added.
            MyDatagrid.CellRenderers.Add("StackedHeader", new GridCustomStackedRenderer());
        }

        private int stack_and_goup_columns(int i, string StacHeader, StackedColumn CurrentStack, SfDataGrid MyDatagrid)
        {
            int moved = 0;

            for (int j = i; j < MyDatagrid.Columns.Count; j++)
            {
                if (MyDatagrid.Columns[j].MappingName.Contains(StacHeader))
                {
                    if (StacHeader.Contains("In Foreign currency") || StacHeader.Contains("in Foreign currency"))
                    {
                        // Add column to StackedColumn
                        if (CurrentStack.ChildColumns == "")
                            CurrentStack.ChildColumns = MyDatagrid.Columns[j].HeaderText;
                        else
                            CurrentStack.ChildColumns += "," + MyDatagrid.Columns[j].HeaderText;

                        // Rename column header
                        if (MyDatagrid.Columns[j].MappingName != StacHeader)
                        {
                            MyDatagrid.Columns[j].HeaderText = MyDatagrid.Columns[j].MappingName.Replace(StacHeader, "");
                            MyDatagrid.Columns.MoveTo(j, i);
                            moved++;
                        }
                        else
                        {
                            MyDatagrid.Columns[j].HeaderText = "Total";
                        }
                    }
                    else
                    {
                        if (!MyDatagrid.Columns[j].MappingName.Contains("In Foreign currency") && !MyDatagrid.Columns[j].MappingName.Contains("in Foreign currency"))
                        {
                            // Add column to StackedColumn
                            if (CurrentStack.ChildColumns == "")
                                CurrentStack.ChildColumns = MyDatagrid.Columns[j].HeaderText;
                            else
                                CurrentStack.ChildColumns += "," + MyDatagrid.Columns[j].HeaderText;

                            // Rename column header
                            if (MyDatagrid.Columns[j].MappingName != StacHeader)
                            {
                                MyDatagrid.Columns[j].HeaderText = MyDatagrid.Columns[j].MappingName.Replace(StacHeader, "");
                                MyDatagrid.Columns.MoveTo(j, i);
                                moved++;
                            }
                            else
                            {
                                MyDatagrid.Columns[j].HeaderText = "Total";
                            }
                        }
                    }
                }
            }
            StackedHeaderRow1.StackedColumns.Add(CurrentStack);

            return moved;
        }

        private void AddSums()
        {
            if (myDataGrid.TableSummaryRows.Count > 0)
            {
                myDataGrid.TableSummaryRows[0].SummaryColumns.Clear();
                myDataGrid.TableSummaryRows.RemoveAt(0);
            }

            if (myDataGrid.GroupSummaryRows.Count > 0)
            {
                myDataGrid.GroupSummaryRows[0].SummaryColumns.Clear();
                myDataGrid.GroupSummaryRows.RemoveAt(0);
            }

            ObservableCollection<ISummaryColumn> SummaryColumns = new ObservableCollection<ISummaryColumn>();
            ObservableCollection<ISummaryColumn> GroupSummaryColumns = new ObservableCollection<ISummaryColumn>();


            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i] is GridNumericColumn && myDataGrid.Columns[i].MappingName != "Unit" &&
                    myDataGrid.Columns[i].MappingName != "Timing" && myDataGrid.Columns[i].MappingName != "Price")
                {
                    var summaryColumn = new GridSummaryColumn()
                    {
                        Name = myDataGrid.Columns[i].MappingName,
                        MappingName = myDataGrid.Columns[i].MappingName,
                        SummaryType = SummaryType.DoubleAggregate,
                        Format = "{Sum:N2}"
                    };
                    SummaryColumns.Add(summaryColumn);
                    GroupSummaryColumns.Add(summaryColumn);
                }
            }

            if (SummaryColumns.Count > 0)
            {
                myDataGrid.TableSummaryRows.Add(new GridTableSummaryRow()
                {
                    ShowSummaryInRow = false,
                    TitleColumnCount = 1,
                    Position = TableSummaryRowPosition.Bottom,
                    Title = "Total:",
                    SummaryColumns = SummaryColumns
                });
            }

            if (GroupSummaryColumns.Count > 0)
            {
                myDataGrid.GroupSummaryRows.Add(new GridSummaryRow()
                {
                    ShowSummaryInRow = false,
                    TitleColumnCount = 1,
                    Title = "Total:",
                    SummaryColumns = GroupSummaryColumns
                });
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

    }
}
