using Financial_WPF_App.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Windows;
using System.Windows.Input;
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

        // The currencies a limit can be set in, and whether their columns are on the grid yet.
        private DataTable currencies_dt;
        private bool currencyColumnsAdded;
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

                    AddCurrencyLimits();
                    myDataGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    myDataGrid.ItemsSource = null;
                }
            }

        }

        /// <summary>
        /// With per-currency limits in the database (P3), a column per currency: "Limit (SYR)",
        /// "Limit (SYPNEW)", "Limit (USD)"... An empty cell means the role may not approve
        /// anything in that currency.
        /// The columns come from the currency table, so a new currency gets one without a change here.
        /// </summary>
        private void AddCurrencyLimits()
        {
            if (!Helper.CurrencyService.HasRoleCurrencyLimits)
                return;

            currencies_dt = U.Get_LimitCurrencies();
            DataTable limits = U.Get_RoleCurrencyLimits();

            foreach (System.Data.DataRow c in currencies_dt.Rows)
            {
                string col = "L_" + c["ID"];
                if (!dt.Columns.Contains(col)) dt.Columns.Add(col, typeof(double));
            }

            foreach (System.Data.DataRow l in limits.Rows)
            {
                string col = "L_" + l["TillType_ID"];
                System.Data.DataRow[] roleRows = dt.Select("ID = " + Convert.ToInt32(l["Role_ID"]));
                if (roleRows.Length > 0 && dt.Columns.Contains(col))
                    roleRows[0][col] = Convert.ToDouble(l["Approval_Limit"]);
            }

            if (currencyColumnsAdded)
                return;

            foreach (System.Data.DataRow c in currencies_dt.Rows)
            {
                myDataGrid.Columns.Add(new Syncfusion.UI.Xaml.Grid.GridNumericColumn
                {
                    MappingName = "L_" + c["ID"],
                    HeaderText = "Limit (" + c["Name"] + ")",
                    AllowEditing = true,
                    AllowNullValue = true,
                    NumberDecimalDigits = 2,
                    NumberDecimalSeparator = ".",
                    NumberGroupSeparator = ",",
                    NumberGroupSizes = new Int32Collection { 3 }
                });
            }

            // The single limit per role is no longer what decides: each currency has its own, and
            // an empty one means "not allowed". It is hidden here and kept equal to the old
            // currency's limit on save, for copies of the program that have not been updated yet
            // and still read only that column.
            Limit_DataGridColumn.IsHidden = true;
            currencyColumnsAdded = true;
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
                            // With per-currency limits, the single role limit just mirrors the old
                            // currency's (0 when that is empty), for not-yet-updated copies of the program.
                            double roleLimit = float.Parse(row.Row["Approval_Limit"].ToString());
                            if (Helper.CurrencyService.HasRoleCurrencyLimits)
                            {
                                string legacyCol = "L_" + Helper.CurrencyService.LegacyCurrencyId;
                                roleLimit = row.Row.Table.Columns.Contains(legacyCol) && row.Row[legacyCol] != DBNull.Value
                                    ? Convert.ToDouble(row.Row[legacyCol]) : 0;
                            }
                            string roleLimitText = roleLimit.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            Query += "UPDATE `role` SET `Approval_Limit`= " + roleLimitText + " WHERE ID = " + (int)row.Row["ID"] + ";";
                            Log_Query += "UPDATE `role` SET `Approval_Limit`= " + roleLimitText + " WHERE ID = " + (int)row.Row["ID"] + ";";

                            // the per-currency limits: an empty cell removes the limit for that currency
                            if (Helper.CurrencyService.HasRoleCurrencyLimits && currencies_dt != null)
                            {
                                foreach (System.Data.DataRow c in currencies_dt.Rows)
                                {
                                    string col = "L_" + c["ID"];
                                    object value = row.Row.Table.Columns.Contains(col) ? row.Row[col] : DBNull.Value;
                                    string q = value == DBNull.Value
                                        ? "DELETE FROM `role_currency_limit` WHERE Role_ID = " + ID + " AND TillType_ID = " + Convert.ToInt32(c["ID"]) + ";"
                                        : "INSERT INTO `role_currency_limit` (Role_ID, TillType_ID, Approval_Limit) VALUES (" + ID + ", " + Convert.ToInt32(c["ID"]) + ", "
                                          + Convert.ToDouble(value).ToString(System.Globalization.CultureInfo.InvariantCulture)
                                          + ") ON DUPLICATE KEY UPDATE Approval_Limit = VALUES(Approval_Limit);";
                                    Query += q;
                                    Log_Query += q;
                                }
                            }
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
                    Login.CloseAndClearConnection();
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

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowUtils.MinimizeWindow(this); // Calls the WindowUtils method
        }


        private void Close_Click(object sender, System.ComponentModel.CancelEventArgs e)
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

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowUtils.TitleBar_MouseDown(this, e);
        }
    }
}
