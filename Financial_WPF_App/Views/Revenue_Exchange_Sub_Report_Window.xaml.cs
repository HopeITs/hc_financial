using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ubiety.Dns.Core.Records;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Revenue_Exchange_Sub_Report_Window.xaml
    /// </summary>
    public partial class Revenue_Exchange_Sub_Report_Window : Window
    {
        public Revenue_Exchange_Sub_Report_Window(int TillType_ID, string TillType_Name, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.TillType_ID = TillType_ID;
            this.TillType_Name = TillType_Name;
            Onload();
        }

        Till t;
        Models.Action a;

        private DataTable dt; //Account_Budgets_dt;
        Main_Window_2 main_Window;
        int TillType_ID;
        string TillType_Name;

        bool GridLoaded = false;
        double HOffset = 0;

        List<string> HiddenColumns = new List<string>();
        List<string> Originally_HiddenColumns = new List<string>();
        private bool Onload()
        {
            try
            {
                t = new Till();
                a = new Models.Action();
                for (int i = 0; i < myDataGrid.Columns.Count; i++)
                {
                    myDataGrid.Columns[i].TextAlignment = TextAlignment.Left;
                    myDataGrid.Columns[i].VerticalAlignment = VerticalAlignment.Center;
                }

                Load_Data();
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

        public void Load_Data()
        {
            Show_Hide();

            LoadGrid();

            Calculate_Sums();
        }

        private void Show_Hide()
        {
            if (TillType_Name.ToLower() == "syr")
            {
                Exchange_Label.Visibility = Exchange_Border.Visibility = Exchpand_Label.Visibility = Exchpand_Border.Visibility = Net_lable.Visibility = Net_Border.Visibility =
                Exchange_Label1.Visibility = Exchange_Border1.Visibility = Exchpand_Label1.Visibility = Exchpand_Border1.Visibility = Net_lable1.Visibility = Net_Border1.Visibility = Visibility.Collapsed;


                foreach (var C in myDataGrid.Columns)
                    if (C.HeaderText.ToString() == "Exchange" || C.HeaderText.ToString() == "Expenses"
                        || C.HeaderText.ToString() == "Balance" || C.HeaderText.ToString() == "Original Avg Rate group" || C.HeaderText.ToString() == "Original Avg Rate amount"
                        || C.HeaderText.ToString() == "Value" || C.HeaderText.ToString() == "Exchange account" || C.HeaderText.ToString() == "Exchanged") 
                    {
                        C.IsHidden = true;
                        Originally_HiddenColumns.Add(C.HeaderText.ToString());
                    }

            }
        }

        private void LoadGrid()
        {
            if (Login.BuildConnection())
            {
                dt = a.Select_Revenue_Exchange_Report(TillType_ID, TillType_Name);

                Set_myDataGrid_ItemSource();

                GridLoaded = true;
                Bind_Show_Hide_Menu();
            }
        }

        private void Set_myDataGrid_ItemSource()
        {
            try
            {

                if (dt != null && dt.Rows.Count > 0)
                {
                    All_textBlock.Text = dt.Rows.Count.ToString();
                    myDataGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }


                if (myDataGrid.ItemsSource != null)
                {
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
                    if (HOffset != 0)
                        this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);
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

        private void Calculate_Sums()
        {
            int count = 0;
            double incomes = 0;
            double Exchange = 0;
            double expaned = 0;
            double Balance = 0;

            DataTable Filtered_dt = null;

            // Convert Filtered Datagridview to Datatable
            if (myDataGrid.View != null)
            {
                var dataRowViews = this.myDataGrid.View.Records.Select(recordentry => recordentry.Data).Cast<DataRowView>();
                var dataRows = dataRowViews.Select(drv => drv.Row);
                Filtered_dt = dataRows.CopyToDataTable();
            }
            else
            {
                Filtered_dt = dt.Copy();
            }


            if (Filtered_dt != null && Filtered_dt.Rows.Count > 0)
            {
                count = Filtered_dt.Rows.Count;

                incomes = Convert.ToDouble(Filtered_dt.Compute("Sum(Income)", string.Empty).ToString());
                Exchange = Convert.ToDouble(Filtered_dt.Compute("Sum(Exchange)", string.Empty).ToString());
                expaned = Convert.ToDouble(Filtered_dt.Compute("Sum(Expand)", string.Empty).ToString());
                Balance = Convert.ToDouble(Filtered_dt.Compute("Sum(Balance)", string.Empty).ToString());
            }

            All_textBlock.Text = count.ToString("N0");
            Income_textBlock.Text = Login.ConvertToDouble(incomes.ToString());
            Exchange_textBlock.Text = Login.ConvertToDouble(Exchange.ToString());
            Exchpand_textBlock.Text = Login.ConvertToDouble(expaned.ToString());
            Net_textBlock.Text = Login.ConvertToDouble(Balance.ToString());

        }

        private void myDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
        {
            try
            {
                int count = myDataGrid.SelectedItems.Count;
                Selected_textBlock.Text = count.ToString("N0");

                double selected_incomes = 0;
                double selected_Exchange = 0;
                double selected_expaned = 0;
                double selected_Balance = 0;

                for (var i = 0; i < count; i++)
                {
                    DataRowView SelectedDataRow = (DataRowView)myDataGrid.SelectedItems[i];
                    if (double.Parse(SelectedDataRow["Income"].ToString()) != 0)
                    {
                        selected_incomes += double.Parse(SelectedDataRow["Income"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Exchange"].ToString()) != 0)
                    {
                        selected_Exchange += double.Parse(SelectedDataRow["Exchange"].ToString());
                    }
                    else if (double.Parse(SelectedDataRow["Expand"].ToString()) != 0)
                    {
                        selected_expaned += double.Parse(SelectedDataRow["Expand"].ToString());
                    }

                    if (double.Parse(SelectedDataRow["Balance"].ToString()) != 0)
                    {
                        selected_Balance += double.Parse(SelectedDataRow["Balance"].ToString());
                    }
                }
                Income_textBlock1.Text = selected_incomes.ToString("N2");
                Exchange_textBlock1.Text = selected_Exchange.ToString("N2");
                Exchpand_textBlock1.Text = selected_expaned.ToString("N2");
                Net_textBlock1.Text = selected_Balance.ToString("N2");
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

        private void myDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.View != null)
            {
                this.myDataGrid.GetVisualContainer().ScrollOwner.ScrollChanged += ScrollOwner_ScrollChanged;

                All_textBlock.Text = myDataGrid.View.Records.Count().ToString();
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

        #region Sizing TextBlock 

        private void All_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Selected_textBlock.ActualWidth > All_textBlock.ActualWidth)
                All_textBlock.MinWidth = Selected_textBlock.ActualWidth;
            else
                Selected_textBlock.MinWidth = All_textBlock.ActualWidth;

        }

        private void Income_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Income_textBlock1.ActualWidth > Income_textBlock.ActualWidth)
                Income_textBlock.MinWidth = Income_textBlock1.ActualWidth;

            else
                Income_textBlock1.MinWidth = Income_textBlock.ActualWidth;

        }

        private void Exchange_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Exchange_textBlock1.ActualWidth > Exchange_textBlock.ActualWidth)
                Exchange_textBlock.MinWidth = Exchange_textBlock1.ActualWidth;
            else
                Exchange_textBlock1.MinWidth = Exchange_textBlock.ActualWidth;

        }

        private void Exchpand_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Exchpand_textBlock1.ActualWidth > Exchpand_textBlock.ActualWidth)
                Exchpand_textBlock.MinWidth = Exchpand_textBlock1.ActualWidth;

            else
                Exchpand_textBlock1.MinWidth = Exchpand_textBlock.ActualWidth;

        }

        private void Net_textBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Net_textBlock1.ActualWidth > Net_textBlock.ActualWidth)
                Net_textBlock.MinWidth = Net_textBlock1.ActualWidth;

            else
                Net_textBlock1.MinWidth = Net_textBlock.ActualWidth;
        }
        #endregion

        private void myDataGrid_FilterChanged(object sender, GridFilterEventArgs e)
        {
            if (HOffset != 0)
                this.myDataGrid.GetVisualContainer().SetHorizontalOffset(HOffset);

            Calculate_Sums();
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
                if (!CHeader.Contains("ID") && !CHeader.Contains("Delete") && !CHeader.Contains("Edit") && !CHeader.Contains("Attachment") && !CHeader.Contains("#") && !Originally_HiddenColumns.Contains(CHeader))
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
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
    }
}
