using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    public partial class BudgetLineType_Window : Window
    {
        public BudgetLineType_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;

            if (!OnLoad())
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private int Type_ID = -1;
        private LineCategoryType lineCategoryType;
        private DataTable dt;
        Main_Window_2 main_Window;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";

        private bool OnLoad()
        {
            try
            {
                lineCategoryType = new LineCategoryType();
                Load_Data();
                Apply_Color_Theme();
                Apply_DataGrid_Color_Theme();
                return true;
            }
            catch (Exception ex)
            {
                Show_Error(ex);
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
            LoadGrid();
        }

        private void LoadGrid()
        {
            dt = lineCategoryType.Select();
            myDataGrid.ItemsSource = dt != null && dt.Rows.Count > 0 ? dt.DefaultView : null;
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (Name_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                string Name = Login.ReplaceQuotation(Name_txtBox.Text);

                if (Type_ID != -1)
                {
                    lineCategoryType.Update(Type_ID, Name, "BudgetLineType_Window");
                }
                else
                {
                    lineCategoryType.Insert(Name, "BudgetLineType_Window");
                }

                Clear_Boxses();
                LoadGrid();
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.CloseAndClearConnection();
                }
                Show_Error(ex);
            }
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                Type_ID = Convert.ToInt32(dataRow["ID"].ToString());
                Name_txtBox.Text = dataRow["Name"].ToString();
            }
            catch (Exception ex)
            {
                Show_Error(ex);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;
                    Type_ID = Convert.ToInt32(dataRow["ID"].ToString());
                    string Name = dataRow["Name"].ToString();
                    lineCategoryType.Delete(Type_ID, Name, "BudgetLineType_Window");

                    Clear_Boxses();
                    LoadGrid();
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.CloseAndClearConnection();
                }
                Show_Error(ex);
            }
        }

        private void Clear_Boxses()
        {
            Type_ID = -1;
            Name_txtBox.Text = "";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clear_Boxses();
                LoadGrid();
            }
            catch (Exception ex)
            {
                Show_Error(ex);
            }
        }

        private void myDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
                            DataRowView rowview = grid.SelectedItem as DataRowView;
                            Type_ID = Convert.ToInt32(rowview["ID"].ToString());
                            Name_txtBox.Text = rowview["Name"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Show_Error(ex);
            }
        }

        private void BudgetLineType_Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Return)
                {
                    Save_btn_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                Show_Error(ex);
            }
        }

        private void Show_Error(Exception ex)
        {
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

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowUtils.MinimizeWindow(this);
        }

        private void Close_Click(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowUtils.CloseWindow(this);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowUtils.TitleBar_MouseDown(this, e);
        }
    }
}
