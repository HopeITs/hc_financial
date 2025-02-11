using Financial_WPF_App.Models;
using Financial_WPF_App.Views;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App
{
    public partial class AddTillType : Window
    {
        public AddTillType(Add_till AddTIll, Main_Window_2 main_Window)
        {
            InitializeComponent();
            this.main_Window = main_Window;
            this.AddTIll = AddTIll;
            if (OnLoad())
            {

            }
            else
            {
                throw new ArgumentException("This page will not open due to data fetching error.");
            }
        }

        private Till t;
        private DataTable dt, tillType_dt;
        Main_Window_2 main_Window;
        Add_till AddTIll;
        List<int> isDirty_list;
        List<int> Delete_list;
        string Query, Log_Query;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];


        #region //IsDirty
        bool TillType_IsDirty = false;
        #endregion

        private bool OnLoad()
        {
            try
            {
                t = new Till();
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


            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UpdateTextBlocksForeground(this.Content, Theme_Color);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        private void Load_Data()
        {
            LoadGrid(false);
        }

        private void LoadGrid(bool Update)
        {
            if (Update)
            {
                myDataGrid.ItemsSource = dt.DefaultView;
            }
            else
            {
                isDirty_list = new List<int>();
                Delete_list = new List<int>();
                TillType_IsDirty = false;
                dt = t.Select_All_TillTypes();
                tillType_dt = dt.Copy();

                if (dt != null && dt.Rows.Count > 0)
                {
                    using (DbDataReader dr = dt.CreateDataReader())
                    {
                        //Get Original Datatable structure
                        dt = dt.Clone();

                        // Add Auto Increment Column called ID
                        dt.Columns.Add(new DataColumn("Local_Index")
                        {
                            AutoIncrement = true,
                            AllowDBNull = false,
                            AutoIncrementSeed = 1,
                            AutoIncrementStep = 1,
                            DataType = typeof(System.Int32),
                            Unique = true
                        });

                        // Change Auto Increment Column Ordinal Position to 0 (ie First Column)
                        dt.Columns["Local_Index"].SetOrdinal(0);

                        // Re-load original Data
                        dt.Load(dr);
                    }

                    myDataGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }
            }
        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (!TillType_IsDirty)
                {
                    MessageBox.Show("There are no changes to save. ", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }
                else
                {
                    Query = "";
                    Log_Query = "";
                    for (int i = 0; i < Delete_list.Count; i++)
                    {
                        Query += " DELETE FROM `tilltypes` WHERE ID = " + Delete_list[i] + ";";
                        Log_Query = " DELETE FROM `tilltypes` WHERE ID = " + Delete_list[i] + ";";
                        DataRow[] rows = tillType_dt.Select("ID =" + Delete_list[i]);
                        string Name = rows[0]["Name"].ToString();
                        Query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES ('AddTillType_Window', 'DELETE', 'Tilltypes', '" + Delete_list[i] + "', CONCAT('Tilltype Name = ', '" + Name + "'), '' , '" + Log_Query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                    for (int i = 0; i < myDataGrid.Items.Count - 1; i++)
                    {
                        DataRowView row = myDataGrid.Items[i] as DataRowView;
                        if (!string.IsNullOrWhiteSpace(row.Row["Name"].ToString()))
                        {
                            if (row.Row["ID"] == null || row.Row["ID"].ToString() == "") //ID 
                            {
                                Query += "INSERT INTO `tilltypes`(`Name`) VALUES (N'" + Login.ReplaceQuotation(row.Row["Name"].ToString()) + "');";
                                Query += "Set @tilltype_ID = Last_Insert_ID();";
                                Log_Query = "INSERT INTO `tilltypes`(`Name`) VALUES (N''" + Login.ReplaceQuotation(row.Row["Name"].ToString()) + "'');";
                                Query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES ('AddTillType_Window', 'INSERT', 'Tilltypes', @tilltype_ID , CONCAT('Tilltype Name = ', '" + Login.ReplaceQuotation(row.Row["Name"].ToString()) + "'), '' , '" + Log_Query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                            }
                            else if (isDirty_list.Contains((int)row.Row["ID"]))
                            {
                                Query += "UPDATE `tilltypes` SET `Name`=N'" + Login.ReplaceQuotation(row.Row["Name"].ToString()) + "' WHERE ID = " + (int)row.Row["ID"] + ";";
                                Log_Query = "UPDATE `tilltypes` SET `Name`=N''" + Login.ReplaceQuotation(row.Row["Name"].ToString()) + "'' WHERE ID = " + (int)row.Row["ID"] + ";";
                                Query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES ('AddTillType_Window', 'UPDATE', 'Tilltypes', '" + (int)row.Row["ID"] + "', CONCAT('Tilltype Name = ', '" + Login.ReplaceQuotation(row.Row["Name"].ToString()) + "'), '' , '" + Log_Query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                            }
                        }
                    }
                    Login.Execute_Query(Query);
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    LoadGrid(false);
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                { MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (myDataGrid.SelectedIndex < myDataGrid.Items.Count - 1)
                {
                    //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                    var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        DataRowView dataRow = (DataRowView)myDataGrid.SelectedItem;


                        if (dataRow.Row["ID"] == null || dataRow.Row["ID"].ToString() == "")
                        {

                        }
                        else
                        {
                            int TillTypeID = Int32.Parse(dataRow.Row["ID"].ToString());

                            Delete_list.Add(TillTypeID);
                            isDirty_list.Remove(TillTypeID);

                            LoadGrid(true);
                        }

                        int Local_Index = Int32.Parse(dataRow.Row["Local_Index"].ToString());

                        DataRow[] dtr = dt.Select("Local_Index='" + Local_Index + "'");
                        foreach (var drow in dtr)
                        {
                            drow.Delete();
                        }
                        dt.AcceptChanges();

                        TillType_IsDirty = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                { MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

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

        private void myDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                int row_index = e.Row.GetIndex();
                TillType_IsDirty = true;

                DataRowView row = myDataGrid.Items[row_index] as DataRowView;

                if (row.Row["ID"] == null || row.Row["ID"].ToString() == "")
                {

                }
                else
                {
                    if (!isDirty_list.Contains(Int32.Parse(row.Row["ID"].ToString())))
                        isDirty_list.Add(Int32.Parse(row.Row["ID"].ToString()));
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

        private void Export_btn_Click(object sender, RoutedEventArgs e)
        {

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

        private void AddTillType_Window_KeyDown(object sender, KeyEventArgs e)
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
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void AddTillType_Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {

                if (TillType_IsDirty)
                {
                    if (MessageBox.Show("There are some unsaved changes, would you like to save them before closing the page?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == MessageBoxResult.Yes)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else
                    {
                        // continue closing;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
    }
}
