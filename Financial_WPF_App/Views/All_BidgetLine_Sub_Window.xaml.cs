using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Syncfusion.Data;
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
    /// Interaction logic for All_BidgetLine_Window.xaml
    /// </summary>
    public partial class All_BidgetLine_Sub_Window : Window
    {
        public All_BidgetLine_Sub_Window(int Budget_Type_ID, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.Budget_Type_ID = Budget_Type_ID;

            PassException = true;
            OnLoad();
            PassException = false;
        }


        private Budget b; private Budget_Lines bl;
        private DataTable BudgetLines_dt, Donor_dt;
        Main_Window_2 main_Window;

        int Budget_Type_ID;
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
            b = new Budget();
            bl = new Budget_Lines();

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

            if(Reload)
            {
                Donor_dt = b.Select_budget_donor(-1, -1);
                BudgetLines_dt = bl.Select_All(Budget_Type_ID);
            }


            if (BudgetLines_dt != null && BudgetLines_dt.Rows.Count > 0)
            {
                myDataGrid.ItemsSource = BudgetLines_dt.DefaultView;

                if (HOffset != 0)
                    this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
            }
            else
            {
                myDataGrid.ItemsSource = null;
            }

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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void myDataGrid_AutoGeneratingColumn(object sender, Syncfusion.UI.Xaml.Grid.AutoGeneratingColumnArgs e)
        {

            try
            {
                if (e.Column is GridNumericColumn && !e.Column.MappingName.Contains("_ID") && e.Column.MappingName != "ID" &&
                    e.Column.MappingName != "Unit" && e.Column.MappingName != "Timing")
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
                    var BudgetDononr = Donor_dt.AsEnumerable().Select(row => row.Field<string>("Donor"));
                    if (BudgetDononr.Any(name => e.Column.MappingName.Contains(name)))
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
                    add_stackedHeader();
                    rename_Columns();
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

                int count = myDataGrid.View.Records.Count();
                All_textBlock.Text = count.ToString("N0");

                //All_textBlock.Text = myDataGrid.View.Records.Count().ToString();
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

        private void add_stackedHeader()
        {
            var StackedHeaderRow1 = new StackedHeaderRow { };

            var ExpectedTotalColumn = new StackedColumn
            {
                HeaderText = "Expected Total",
                MappingName = "ExpectedTotal",
            };
            var ExpendituresTotalColumn = new StackedColumn
            {
                HeaderText = "Expenditures Total",
                MappingName = "ExpendituresTotal"
            };
            var RemainingColumn = new StackedColumn
            {
                HeaderText = "Remaining",
                MappingName = "Remaining"
            };

            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i].MappingName != null)
                {
                    if (myDataGrid.Columns[i].MappingName.Contains("Expected Total"))
                    {
                        if (ExpectedTotalColumn.ChildColumns == "")
                            ExpectedTotalColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            ExpectedTotalColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Expenditures Total"))
                    {
                        if (ExpendituresTotalColumn.ChildColumns == "")
                            ExpendituresTotalColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            ExpendituresTotalColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Remaining"))
                    {
                        if (RemainingColumn.ChildColumns == "")
                            RemainingColumn.ChildColumns = myDataGrid.Columns[i].HeaderText;
                        else
                            RemainingColumn.ChildColumns += "," + myDataGrid.Columns[i].HeaderText;
                    }
                }
            }

            StackedHeaderRow1.StackedColumns.Add(ExpectedTotalColumn);
            StackedHeaderRow1.StackedColumns.Add(ExpendituresTotalColumn);
            StackedHeaderRow1.StackedColumns.Add(RemainingColumn);

            myDataGrid.StackedHeaderRows.Add(StackedHeaderRow1);

            //Default GridStackedCellRenderer is removed.
            myDataGrid.CellRenderers.Remove("StackedHeader");

            //Customized GridStackedCellRenderer is added.
            myDataGrid.CellRenderers.Add("StackedHeader", new GridCustomStackedRenderer());
        }

        private void rename_Columns()
        {
            for (int i = 0; i < myDataGrid.Columns.Count; i++)
            {
                if (myDataGrid.Columns[i].MappingName != null)
                {
                    if (myDataGrid.Columns[i].MappingName.Contains("Expected Total"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Expected Total")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Expected Total", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Expenditures Total"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Expenditures Total")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Expenditures Total", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
                    }
                    else if (myDataGrid.Columns[i].MappingName.Contains("Remaining"))
                    {
                        if (myDataGrid.Columns[i].MappingName != "Remaining")
                        {
                            myDataGrid.Columns[i].HeaderText = myDataGrid.Columns[i].MappingName.Replace(" Remaining", "");
                        }
                        else
                        {
                            myDataGrid.Columns[i].HeaderText = "Total";
                        }
                    }
                }
            }
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

                if (BudgetLines_dt != null)
                {
                    SearchDataGrid searchHelper = new SearchDataGrid(BudgetLines_dt);

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
        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;
        }
    }
}
