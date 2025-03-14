using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{

    public partial class Line_Window : Window
    {
        public Line_Window(Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;

            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }


        private int Cat_ID;
        private LineCategory cat;
        private DataTable cat_dt;
        Main_Window_2 main_Window;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";

        private bool OnLoad()
        {
            try
            {
                cat = new LineCategory();

                Load_Data();
                Apply_Color_Theme();
                Apply_DataGrid_Color_Theme();
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

            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UIElementColoring(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Apply_DataGrid_Color_Theme()
        {
            categoryDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            categoryDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            categoryDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];

         
        }
        private void Load_Data()
        {
            cat_dt = new DataTable();

            LoadGrid_Category();
        }

        private void LoadGrid_Category()
        {

            if (Login.BuildConnection())
            {
                cat_dt = cat.Select("");
                if (cat_dt != null && cat_dt.Rows.Count > 0)
                {
                    categoryDataGrid.ItemsSource = cat_dt.DefaultView;
                }
                else
                {
                    categoryDataGrid.ItemsSource = null;
                }
            }

        }

        private void ProjectWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Return)
                {
                    CategorySave_btn_Click(sender, e);
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

        #region line category
        private void CategorySave_btn_Click(object sender, RoutedEventArgs e)
        {
            categorySave_btn.IsEnabled = false;
            try
            {
                if (CategoryName_txtBox.Text == "")
                {
                    MessageBox.Show("Please enter the name.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Fixed_checkBox.IsChecked == false && InFlow_checkBox.IsChecked == false)
                {
                    MessageBox.Show("Please select the type (Equipping the place / Running cost)", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Profit_checkBox.IsChecked == false && Budget_checkBox.IsChecked == false)
                {
                    MessageBox.Show("Please select the category (budget / profit)", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }

                if (Login.BuildConnection())
                {
                    string CName = Login.ReplaceQuotation(CategoryName_txtBox.Text);
                    int Type, Kind;

                    if (Fixed_checkBox.IsChecked == true) Type = 0;
                    else Type = 1;

                    if (Profit_checkBox.IsChecked == true) Kind = 0;
                    else Kind = 1;

                    if (Cat_ID != -1)//Edit
                    {
                        cat.Update(Cat_ID, CName, Type, Kind, "Line_Window");
                    }
                    else//Add new
                    {
                        cat.Insert(CName, Type, Kind, "Line_Window");
                    }
                    Clear_Boxses();
                    LoadGrid_Category();
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
            finally
            {
                categorySave_btn.IsEnabled = true;
            }
        }

        private void Fixed_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Fixed_checkBox.IsChecked == true) InFlow_checkBox.IsChecked = false;
                else InFlow_checkBox.IsChecked = true;
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

        private void Profit_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Profit_checkBox.IsChecked == true) Budget_checkBox.IsChecked = false;
                else Budget_checkBox.IsChecked = true;
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

        private void InFlow_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (InFlow_checkBox.IsChecked == true) Fixed_checkBox.IsChecked = false;
                else Fixed_checkBox.IsChecked = true;
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

        private void Budget_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Budget_checkBox.IsChecked == true) Profit_checkBox.IsChecked = false;
                else Profit_checkBox.IsChecked = true;
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView dataRow = (DataRowView)categoryDataGrid.SelectedItem;
                Cat_ID = Convert.ToInt32(dataRow[0].ToString());

                CategoryName_txtBox.Text = dataRow[1].ToString();

                if (dataRow[2] != DBNull.Value || dataRow[2] != null)
                {
                    if (dataRow[2].ToString() == "Equipping the place") Fixed_checkBox.IsChecked = true;                
                    else InFlow_checkBox.IsChecked = true;           
                }

                if (dataRow[3] != DBNull.Value || dataRow[3] != null)
                {
                    if (dataRow[3].ToString() == "Profit") Profit_checkBox.IsChecked = true;                
                    else Budget_checkBox.IsChecked = true;             
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
                //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    DataRowView dataRow = (DataRowView)categoryDataGrid.SelectedItem;
                    Cat_ID = Convert.ToInt32(dataRow[0].ToString());
                    string Cat_Name = dataRow["Name"].ToString();
                    cat.Delete(Cat_ID, Cat_Name, "Line_Window");

                    Cat_ID = -1;
                    LoadGrid_Category();
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
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

        }
        #endregion

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    scrollViewer.LineLeft();
                }
                else
                {
                    scrollViewer.LineRight();
                }
            }
            else
            {
                if (e.Delta < 0)
                {
                    scrollViewer.LineDown();
                }
                else
                {
                    scrollViewer.LineUp();
                }
            }
            e.Handled = true;
        }

        private void Clear_Boxses()
        {
            Cat_ID = -1;
            CategoryName_txtBox.Text = "";
            Fixed_checkBox.IsChecked = false;
            InFlow_checkBox.IsChecked = false;
            Budget_checkBox.IsChecked = false;
            Profit_checkBox.IsChecked = false;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clear_Boxses();
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
                            Cat_ID = Convert.ToInt32(rowview[0].ToString());

                            CategoryName_txtBox.Text = rowview[1].ToString();

                            if (rowview[2] != DBNull.Value || rowview[2] != null)
                            {
                                if (rowview[2].ToString() == "Equipping the place") Fixed_checkBox.IsChecked = true;
                                else InFlow_checkBox.IsChecked = true;
                            }

                            if (rowview[3] != DBNull.Value || rowview[3] != null)
                            {
                                if (rowview[3].ToString() == "Profit") Profit_checkBox.IsChecked = true;
                                else Budget_checkBox.IsChecked = true;
                            }


                        }
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
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
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
