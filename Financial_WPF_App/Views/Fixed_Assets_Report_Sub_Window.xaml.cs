using Financial_WPF_App.Models;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Fixed_Assets_Report_Sub_Window.xaml
    /// </summary>
    public partial class Fixed_Assets_Report_Sub_Window : Window
    {
        public Fixed_Assets_Report_Sub_Window(int TillType_ID, string TillType_Name, Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            this.TillType_ID = TillType_ID;
            this.TillType_Name = TillType_Name;

            PassException = true;
            OnLoad();
            PassException = false;
        }

        private Models.Line l;
        private DataTable dt;
        Main_Window_2 main_Window;

        StackedHeaderRow StackedHeaderRow1 = new StackedHeaderRow { };

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];



        int TillType_ID; string TillType_Name;
        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();
        bool PassException = false;

        bool UserMode;

        double HOffset = 0;

        private void OnLoad()
        {
            UserMode = false;
            l = new Models.Line();

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
            Fill_myDataGrid();
        }


        public void Fill_myDataGrid()
        {
            myDataGrid.ItemsSource = null;

            dt = l.Select_Fixed_Assets_Report(TillType_ID);



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
                if (e.Column is GridNumericColumn && !e.Column.MappingName.Contains("_ID") && e.Column.MappingName != "ID" && e.Column.MappingName != "Estimated lifetime - Months" &&
                    e.Column.MappingName != "Current lifetime - Months")
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

        private void myDataGrid_ItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e)
        {
            try
            {
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
                if (myDataGrid.Columns[i] is GridNumericColumn && myDataGrid.Columns[i].MappingName != "Estimated lifetime - Months" &&
                    myDataGrid.Columns[i].MappingName != "Current lifetime - Months")
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
    }
}
