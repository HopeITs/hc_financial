using Financial_WPF_App.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Windows;
using System.Windows.Media;

namespace Financial_WPF_App.Views
{
    /// <summary>
    /// Interaction logic for Approval_Limit_Window.xaml
    /// </summary>
    public partial class Approval_Limit_Window : Window
    {
        public Approval_Limit_Window(Main_Window_2 main_Window)
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
        private Users U;
        private DataTable dt, Limits_dt;
        Main_Window_2 main_Window;

        List<int> isDirty_list;
        string Query, Log_Query;

        System.Windows.Media.Brush Theme_Color = (SolidColorBrush)Application.Current.Resources["GreenBlue"];
        string HeaderStyle_Name = "SfDataGrid_header";
        string TableSummaryRowStyle_Name = "GreenBlue_TableSummaryRowStyle";
        string GroupSummaryRowStyle = "GreenBlue_GroupSummaryRowStyle";


        #region //IsDirty
        bool Limits_IsDirty = false;
        #endregion
        private bool OnLoad()
        {
            try
            {
                U = new Users();
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
            myDataGrid.HeaderStyle = (Style)Application.Current.Resources[HeaderStyle_Name];
            myDataGrid.TableSummaryRowStyle = (Style)Application.Current.Resources[TableSummaryRowStyle_Name];
            myDataGrid.GroupSummaryRowStyle = (Style)Application.Current.Resources[GroupSummaryRowStyle];
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
                Limits_IsDirty = false;
                DataTable Temp_dt = U.Get_UserRoles();

                System.Data.DataRow[] filteredRows = Temp_dt.Select("Name <> 'Head Office' AND Name <> 'Cashier' AND Name <> 'Employee' AND Name <> 'Donor'");

                if (filteredRows.Length != 0)
                {
                    dt = filteredRows.CopyToDataTable();
                }
                else
                {
                    dt = null;
                }
                Limits_dt = dt.Copy();

                if (dt != null && dt.Rows.Count > 0)
                {
                    using (DbDataReader dr = dt.CreateDataReader())
                    {
                        //Get Original Datatable structure
                        dt = dt.Clone();

                        // Add Auto Increment Column called ID
                        dt.Columns.Add(new System.Data.DataColumn("Local_Index")
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

        private void Approval_Limit_Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (Limits_IsDirty)
                {
                    if (MessageBox.Show("There are some unsaved changes, would you like to save them before closing the page?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None) == MessageBoxResult.Yes)
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

        private void myDataGrid_CurrentCellEndEdit(object sender, Syncfusion.UI.Xaml.Grid.CurrentCellEndEditEventArgs e)
        {
            try
            {
                Limits_IsDirty = true;

                DataRowView row = (DataRowView)this.myDataGrid.View.Records.GetItemAt(e.RowColumnIndex.RowIndex - 1);

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

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            Save_btn.IsEnabled = false;
            try
            {
                if (!Limits_IsDirty)
                {
                    MessageBox.Show("There are no changes to save. ", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    return;
                }
                else
                {
                    Query = "";
                    Log_Query = "";

                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                    {
                        DataRowView row = myDataGrid.View.Records.GetItemAt(i) as DataRowView;
                        int ID = (int)row.Row["ID"];
                        if (isDirty_list.Contains((int)row.Row["ID"]))
                        {
                            Query += "UPDATE `role` SET `Approval_Limit`= " + float.Parse(row.Row["Approval_Limit"].ToString()) + " WHERE ID = " + (int)row.Row["ID"] + ";";
                            Log_Query += "UPDATE `role` SET `Approval_Limit`= " + float.Parse(row.Row["Approval_Limit"].ToString()) + " WHERE ID = " + (int)row.Row["ID"] + ";";
                            Query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES ('Approval_Limit_Window', 'UPDATE', 'Role', '" + (int)row.Row["ID"] + "', CONCAT('Role Name = ', '" + Login.ReplaceQuotation(row.Row["Name"].ToString()) + "'), '' , '" + Log_Query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        }
                    }
                    Login.Execute_Query(Query);
                    MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    LoadGrid(false);
                    main_Window.Fill_TreeView(true);
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
            finally
            {
                Save_btn.IsEnabled = true;
            }
        }

    }
}
