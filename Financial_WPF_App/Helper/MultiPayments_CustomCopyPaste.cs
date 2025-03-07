using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Financial_WPF_App.Helper
{
    internal class MultiPayments_CustomCopyPaste : GridCutCopyPaste
    {
        public event EventHandler PasteCompleted;
        private DataTable _CashAccounts_dt, _Beneficiaries_dt, _Payments_dt;

        public int TotalPastedRows { get; private set; } = 0;
        public int TotalErrorRows { get; private set; } = 0;

        int _Subporject_ID;

        Double _Amount = 0;
        string _Date = "";
        string _Note = "";
        int _MicroProject_ID = -1;
        int _CashAccount_ID = -1;
        string _Form_Name = "Add_MultiPayments_Window";


        int k = -1;

        DataTable dt;

        public MultiPayments_CustomCopyPaste(SfDataGrid dataGrid, int Subporject_ID, DataTable CashAccounts_dt, DataTable Beneficiaries_dt, DataTable Payments_dt) : base(dataGrid)
        {
            _CashAccounts_dt = CashAccounts_dt ?? new DataTable();
            _Beneficiaries_dt = Beneficiaries_dt ?? new DataTable();
            _Payments_dt = Payments_dt ?? new DataTable();


            _Subporject_ID = Subporject_ID;
            if (dataGrid.ItemsSource != null)
            {
                dt = ((DataView)this.dataGrid.ItemsSource).Table;
                dt.ColumnChanging += new DataColumnChangeEventHandler(Column_Changing);
            }

        }

        protected override void PasteToRows(object clipBoardRows)
        {
            var copiedRecord = (string[])clipBoardRows;
            int copiedRecordsCount = copiedRecord.Count();


            if (dt.Rows.Count == 1)
            {
                bool onlyEmpty = true;

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (!String.IsNullOrEmpty(dt.Rows[0][i].ToString()) || !String.IsNullOrWhiteSpace(dt.Rows[0][i].ToString()))
                    {
                        onlyEmpty = false;
                    }
                }
                if (onlyEmpty)
                {
                    dt.Rows.Clear();
                }
            }

            var splitRecords = copiedRecord.Where(row => !string.IsNullOrWhiteSpace(row))
                                         .Select(row => Regex.Split(row, @"\t"))
                                         .ToList();

            if (!splitRecords.All(values => values.Length == dt.Columns.Count))
            {
                MessageBox.Show("Please make sure the number of columns are equal.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                return;
            }

            splitRecords.ForEach(values =>
            {
                var row = dt.NewRow();
                for (int i = 0; i < values.Length; i++)
                {
                    row[i] = values[i];
                }
                dt.Rows.Add(row);
            });

            this.dataGrid.ItemsSource = dt.DefaultView;
            OnPasteCompleted();
        }

        private void clear_vlaues()
        {
            _Amount = 0;
            _Date = "";
            _Note = "";
            _MicroProject_ID = -1;
            _CashAccount_ID = -1;
        }

        private void Column_Changing(object sender, DataColumnChangeEventArgs e)
        {
            string Last_Column_Name = e.Row.Table.Columns[e.Row.Table.Columns.Count - 1].ColumnName;

            if (e.Column.ColumnName == "Project number")
            {
                bool exists = false;
                if(_Beneficiaries_dt.Rows.Count > 0)
                    exists = _Beneficiaries_dt.AsEnumerable().Any(row => row.Field<int>("MicroProject_ID").ToString() == e.ProposedValue.ToString());

                if (exists)
                {
                    _MicroProject_ID = Int32.Parse(e.ProposedValue.ToString());


                    int Beneficiary_ID = _Beneficiaries_dt.AsEnumerable()
                             .Where(row => row.Field<int>("MicroProject_ID").ToString()?.Equals(_MicroProject_ID.ToString()) == true)
                             .Select(row => row.Field<int>("ID"))
                             .DefaultIfEmpty(-1)
                             .FirstOrDefault();

                    DataTable selected_rows_dt = new DataTable();

                    System.Data.DataRow[] My_rows = _Payments_dt.Select("Beneficiary_ID = " + Beneficiary_ID + "");

                    selected_rows_dt = _Payments_dt.Copy();
                    selected_rows_dt.Rows.Clear();
                    foreach (System.Data.DataRow row in My_rows)
                        selected_rows_dt.ImportRow(row);

                    if (selected_rows_dt.Rows.Count <= 0)
                    {
                        Login.MultiPayments_AnyError = true;
                        e.Row.SetColumnError(e.Column.ColumnName, "No payments have been generated for this beneficiary.");
                        e.Row.RowError = "There are errors in this row.";
                    }
                }
                else
                {
                    Login.MultiPayments_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This project number does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Payment amount")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _Amount);

                if (!isNumber)
                {
                    Login.MultiPayments_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Description")
            {
                _Note = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "Cash till")
            {
                bool exists = false;


                if (_CashAccounts_dt.Rows.Count > 0)
                    _CashAccount_ID = _CashAccounts_dt.AsEnumerable()
                            .Where(row => row.Field<string>("Name")?.Equals(e.ProposedValue.ToString()) == true)
                            .Select(row => row.Field<int>("ID"))
                            .DefaultIfEmpty(-1)
                            .FirstOrDefault();

                if (_CashAccount_ID != -1)
                    exists = true;

                if (exists)
                {

                }
                else
                {
                    Login.MultiPayments_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This cash account does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Date")
            {
                bool exists = false;

                DateTime Adate;
                bool isDate = DateTime.TryParseExact(
                         e.ProposedValue.ToString(),
                         "dd/MM/yyyy",
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.None,
                         out Adate);

                if (isDate)
                {
                    exists = true;
                    _Date = Adate.ToString("yyyy/MM/dd");
                }

                if (!exists)
                {
                    Login.MultiPayments_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts Dates.");
                    e.Row.RowError = "There are errors in this row.";
                }

            }

            if (e.Column.ColumnName == Last_Column_Name)
            {
                TotalPastedRows++;
                if (e.Row.RowError == "There are errors in this row.")
                    TotalErrorRows++;

                if (!Login.MultiPayments_AnyError)
                {
                    build_insert_query();
                }
                else
                {
                    Login.MultiPayments_List.Clear();
                    Login.MultiPayments_sc.Parameters.Clear();
                }

                clear_vlaues();
            }
        }

        public void build_insert_query()
        {
            k++;
            string temp_query = "";
            string log_query = "";

            Login.MultiPayments_sc.Parameters.AddWithValue("@Amount" + k, _Amount);

            if (_Date == "Null")
            {
                Login.MultiPayments_sc.Parameters.AddWithValue("@Date" + k, DBNull.Value);
            }
            else
            {
                Login.MultiPayments_sc.Parameters.AddWithValue("@Date" + k, _Date);
            }
            Login.MultiPayments_sc.Parameters.AddWithValue("@Note" + k, _Note);

            if (_MicroProject_ID == -1)
            {
                Login.MultiPayments_sc.Parameters.AddWithValue("@MicroProject_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiPayments_sc.Parameters.AddWithValue("@MicroProject_ID" + k, _MicroProject_ID);
            }
            if (_Subporject_ID == -1)
            {
                Login.MultiPayments_sc.Parameters.AddWithValue("@SubProject_ID" + k, DBNull.Value);
            }
            else
            {
                Login.MultiPayments_sc.Parameters.AddWithValue("@SubProject_ID" + k, _Subporject_ID);
            }
            Login.MultiPayments_sc.Parameters.AddWithValue("@CashAccount_ID" + k, _CashAccount_ID);

            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
            Login.MultiPayments_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

            Login.MultiPayments_sc.Parameters.AddWithValue("@Form_Name" + k, _Form_Name);

            Login.MultiPayments_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);

            temp_query += @"SET @Till_ID" + k + " = (SELECT till.ID" +
                            " FROM account LEFT JOIN till ON till.Account_ID = account.ID" +
                            " LEFT JOIN tilltypes ON till.Type_ID = tilltypes.ID" +
                            " LEFT JOIN account_donor on account_donor.Account_id = account.ID" +
                            " LEFT join(SELECT account_donor.Donor_ID AS 'DonorID', account.SubProject_ID AS 'SubProjectID' FROM beneficiary LEFT JOIN account ON beneficiary.Account_ID = account.ID  LEFT JOIN account_donor on account_donor.Account_id = account.ID WHERE beneficiary.MicroProject_ID = @MicroProject_ID" + k + " AND account.SubProject_ID = @SubProject_ID" + k + ") AS t1 ON account_donor.Donor_ID = t1.DonorID" +
                            " WHERE" +
                            " account.SubProject_ID = SubProjectID" +
                            " AND tilltypes.Name = 'pay'); ";



            int Beneficiary_ID = _Beneficiaries_dt.AsEnumerable()
                     .Where(row => row.Field<int>("MicroProject_ID").ToString()?.Equals(_MicroProject_ID.ToString()) == true)
                     .Select(row => row.Field<int>("ID"))
                     .DefaultIfEmpty(-1)
                     .FirstOrDefault();


            Login.MultiPayments_sc.Parameters.AddWithValue("@Beneficiary_ID" + k, Beneficiary_ID);

            DataTable selected_rows_dt = new DataTable();

            System.Data.DataRow[] My_rows = _Payments_dt.Select("(payment_state is null OR payment_state <> 'Done') and Beneficiary_ID = " + Beneficiary_ID + "");

            selected_rows_dt = _Payments_dt.Copy();
            selected_rows_dt.Rows.Clear();
            foreach (System.Data.DataRow row in My_rows)
                selected_rows_dt.ImportRow(row);

            int numofinserts = 0;
            if (selected_rows_dt.Rows.Count > 0)
            {
                for (int i = 0; i < selected_rows_dt.Rows.Count; i++)
                {
                    if (_Amount > 0)
                    {
                        int payment_id = selected_rows_dt.Rows[i].Field<Int32>("ID");

                        Double Payment_Amount = Double.Parse(selected_rows_dt.Rows[i].Field<Single>("Amount").ToString());
                        Double Paid_Amount = selected_rows_dt.Rows[i].Field<Double>("Paid_Amount");
                        Double Current_Payment;

                        if (i < selected_rows_dt.Rows.Count - 1 && _Amount > Payment_Amount - Paid_Amount)
                        {
                            Current_Payment = Payment_Amount - Paid_Amount;

                            System.Data.DataRow[] selectedRows = _Payments_dt.Select("ID = " + payment_id);
                            if (selectedRows.Length > 0)
                            {
                                selectedRows[0]["Paid_Amount"] = (Paid_Amount + Current_Payment).ToString();
                                selectedRows[0]["payment_state"] = "Done";
                                _Payments_dt.AcceptChanges();
                            }
                        }
                        else
                        {
                            Current_Payment = _Amount;
                            if (Current_Payment + Paid_Amount < Payment_Amount)
                            {
                                System.Data.DataRow[] selectedRows = _Payments_dt.Select("ID = " + payment_id);
                                if (selectedRows.Length > 0)
                                {
                                    selectedRows[0]["Paid_Amount"] = (Paid_Amount + Current_Payment).ToString();
                                    selectedRows[0]["payment_state"] = "Incomplete";
                                    _Payments_dt.AcceptChanges();
                                }
                            }
                            else
                            {
                                System.Data.DataRow[] selectedRows = _Payments_dt.Select("ID = " + payment_id);
                                if (selectedRows.Length > 0)
                                {
                                    selectedRows[0]["Paid_Amount"] = (Paid_Amount + Current_Payment).ToString();
                                    selectedRows[0]["payment_state"] = "Done";
                                    _Payments_dt.AcceptChanges();
                                }
                            }
                        }


                        Login.MultiPayments_sc.Parameters.AddWithValue("@payment_id" + i + "_" + k, payment_id);
                        Login.MultiPayments_sc.Parameters.AddWithValue("@Amount" + i + "_" + k, Current_Payment);


                        if (k == 0)
                        {
                            temp_query += @"SET @Action_ID = (
                                SELECT MAX(max_id) + 1 AS next_id
                                FROM (
                                    SELECT MAX(ID) AS max_id FROM action
                                    UNION ALL
                                    SELECT MAX(ID) FROM action_not_approved
                                    UNION ALL
                                    SELECT MAX(ID) FROM action_deleted
                                ) AS combined_max_ids);";
                        }

                        temp_query += "SET @Action_ID" + i + "_" + k + " = @Action_ID;";


                        temp_query += "INSERT INTO `action_not_approved`(`id`,`Till_ID`, `AType_ID`, `Amount`, `rate`,"
                                + " `ActionDate`, `Note`, `IsReceipt`, `MicroProject_ID`,cashaccount_id, created_at, User_ID)"
                                + " VALUES (@Action_ID" + i + "_" + k + ", @Till_ID" + k + ", (Select id from actiontype where shortname = 'I'), @Amount" + i + "_" + k + ", 0, @Date" + k + ", @Note" + k + ", -1,"
                                + " @MicroProject_ID" + k + ", @CashAccount_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ");";


                        temp_query += "SET @Action_ID = @Action_ID" + i + "_" + k + " +1;";


                        temp_query += " INSERT INTO action_payment (action_id, payment_id) VALUES (@Action_ID" + i + "_" + k + ",@payment_id" + i + "_" + k + ");";

                        _Amount = _Amount - Current_Payment;
                        numofinserts++;
                    }
                }

                for (int i = 0; i < numofinserts; i++)
                {
                    temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @Action_ID" + i + "_" + k + ", 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }
            }
            else
            {
                temp_query += "SET @Action_ID0" + k + "  = (SELECT CASE WHEN IFNULL(MAX(action.ID),0) > (SELECT IFNULL(MAX(action_not_approved.ID),0) FROM action_not_approved) THEN IFNULL(MAX(action.ID),0)+1 ELSE (SELECT IFNULL(MAX(action_not_approved.ID),0)+1 FROM action_not_approved) END FROM action);";


                temp_query += " INSERT INTO `action_not_approved`(`id`,`Till_ID`, `AType_ID`, `Amount`, `rate`,"
                    + " `ActionDate`, `Note`, `IsReceipt`, `MicroProject_ID`,cashaccount_id, created_at, User_ID)"
                    + " VALUES (@Action_ID0" + k + ", @Till_ID" + k + ", (Select id from actiontype where shortname = 'I'), @Amount" + k + ", 0, @Date" + k + ", @Note" + k + ", -1,"
                    + " @MicroProject_ID" + k + ", @CashAccount_ID" + k + ", @Todaydate" + k + ", @Uesr_ID" + k + ");";


                temp_query += "SET @Action_ID = @Action_ID0" + k + "  +1;";


                temp_query += " INSERT INTO action_payment (action_id, payment_id) VALUES (@Action_ID0" + k + ",(select ID from payment where payment.Beneficiary_ID = @Beneficiary_ID" + k + " and payment.Num = (select Max(num) from payment where payment.Beneficiary_ID = @Beneficiary_ID" + k + ")));";


                temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @Action_ID0" + k + ", 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

            }



            Login.MultiPayments_List.Add(temp_query);
            temp_query = "";

        }

        protected virtual void OnPasteCompleted()
        {
            PasteCompleted?.Invoke(this, EventArgs.Empty);
        }

    }
}
