using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using System;
using System.Collections.Generic;
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
        Double _ReturnedAmount = 0;
        string _Date = "";
        string _Note = "";
        string _Currency = "";
        int _MicroProject_ID = -1;
        int _CashAccount_ID = -1;
        string _Form_Name = "Add_MultiPayments_Window";
        bool _IsReturnedPayment = false;
        bool _ActionIdVariableInitialized = false;
        HashSet<int> _BeneficiaryIdsToRegenerate = new HashSet<int>();
        bool _RegenerationQueriesAppended = false;


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
                MessageBox.Show("Please make sure the number of columns are equal.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
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
            _ReturnedAmount = 0;
            _Date = "";
            _Note = "";
            _Currency = "";
            _MicroProject_ID = -1;
            _CashAccount_ID = -1;
            _IsReturnedPayment = false;
        }

        private object GetProposedOrCurrentValue(System.Data.DataRow row, DataColumnChangeEventArgs e, string columnName)
        {
            return e.Column.ColumnName == columnName ? e.ProposedValue : row[columnName];
        }

        private string GetText(System.Data.DataRow row, DataColumnChangeEventArgs e, string columnName)
        {
            return (GetProposedOrCurrentValue(row, e, columnName)?.ToString() ?? "").Trim();
        }

        private bool TryParsePositiveAmount(string text, out double amount)
        {
            amount = 0;
            if (text.IsNullOrWhiteSpace())
                return true;

            text = text.Replace(",", "");
            return double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out amount)
                || double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
        }

        private bool TryParseDateValue(object val, out string date)
        {
            date = "";
            DateTime Adate;
            string text = val?.ToString() ?? "";

            if (val is DateTime dt)
            {
                Adate = dt;
            }
            else if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double oaSerial))
            {
                Adate = DateTime.FromOADate(oaSerial);
            }
            else if (!DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out Adate)
                && !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out Adate))
            {
                return false;
            }

            date = Adate.ToString("yyyy/MM/dd");
            return true;
        }

        private string NormalizeBeneficiaryCurrency(string value)
        {
            value = (value ?? "").Trim().ToUpperInvariant();
            return value == "SYP" ? "SYR" : value;
        }

        /// <summary>
        /// The same rule as the single payment window: a cash till is a safe counted in one
        /// currency, so an instalment in a local currency must go to a cash till in that
        /// currency (old and new lira never share one), and a dollar instalment to a dollar cash
        /// till whenever the project has one. Returns the error to show, or null.
        /// </summary>
        private string CashTillCurrencyProblem(int cashAccountId, string currency)
        {
            if (cashAccountId == -1 || _CashAccounts_dt.Rows.Count == 0 || !_CashAccounts_dt.Columns.Contains("currency_tilltype_id"))
                return null;

            int currencyId = Helper.CurrencyService.GetCurrencyId(currency);
            if (currencyId <= 0)
                return null;

            System.Data.DataRow cashTill = _CashAccounts_dt.AsEnumerable().FirstOrDefault(r => r.Field<int>("ID") == cashAccountId);
            if (cashTill == null || cashTill["currency_tilltype_id"] == DBNull.Value)
                return null;

            int cashCurrencyId = Convert.ToInt32(cashTill["currency_tilltype_id"]);
            if (cashCurrencyId == currencyId)
                return null;

            bool projectHasOne = _CashAccounts_dt.AsEnumerable().Any(r =>
                r["currency_tilltype_id"] != DBNull.Value && Convert.ToInt32(r["currency_tilltype_id"]) == currencyId);
            if (!Helper.CurrencyService.IsLocalCurrency(currencyId, false) && !projectHasOne)
                return null;

            return "This cash till holds " + Helper.CurrencyService.GetCurrencyName(cashCurrencyId) + "; a " + currency
                 + " payment needs a " + currency + " cash till" + (projectHasOne ? "." : " (none is linked to this project yet).");
        }

        private int GetBeneficiaryId(int microProjectId)
        {
            if (_Beneficiaries_dt.Rows.Count <= 0)
                return -1;

            return _Beneficiaries_dt.AsEnumerable()
                     .Where(row => row.Field<int>("MicroProject_ID").ToString()?.Equals(microProjectId.ToString()) == true)
                     .Select(row => row.Field<int>("ID"))
                     .DefaultIfEmpty(-1)
                     .FirstOrDefault();
        }

        private string GetBeneficiaryCurrency(int microProjectId)
        {
            if (_Beneficiaries_dt.Rows.Count <= 0 || !_Beneficiaries_dt.Columns.Contains("Currency_Name"))
                return "";

            return _Beneficiaries_dt.AsEnumerable()
                     .Where(row => row.Field<int>("MicroProject_ID").ToString()?.Equals(microProjectId.ToString()) == true)
                     .Select(row => NormalizeBeneficiaryCurrency(row.Field<string>("Currency_Name")))
                     .DefaultIfEmpty("")
                     .FirstOrDefault();
        }

        private bool HasGeneratedPayments(int beneficiaryId)
        {
            return beneficiaryId != -1 && _Payments_dt.Rows.Count > 0
                && _Payments_dt.AsEnumerable().Any(row => row.Field<int>("Beneficiary_ID") == beneficiaryId);
        }

        private bool IsPayablePayment(System.Data.DataRow row)
        {
            string paymentState = row["payment_state"]?.ToString();
            return paymentState == "Unpaid" || paymentState == "Incomplete";
        }

        private void MarkRowError(System.Data.DataRow row, string columnName, string message)
        {
            Login.MultiPayments_AnyError = true;
            row.SetColumnError(columnName, message);
            row.RowError = "There are errors in this row.";
        }

        private void Column_Changing(object sender, DataColumnChangeEventArgs e)
        {
            string Last_Column_Name = e.Row.Table.Columns[e.Row.Table.Columns.Count - 1].ColumnName;

            if (e.Column.ColumnName == "Micro Project number")
            {
                e.Row.ClearErrors();
            }

            if (e.Column.ColumnName == "Description")
            {
                _Note = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "Date")
            {
                if (TryParseDateValue(e.ProposedValue, out string parsedDate))
                    _Date = parsedDate;
                else
                    MarkRowError(e.Row, e.Column.ColumnName, "This column only accepts Dates.");
            }
            else if (e.Column.ColumnName == "Cash till")
            {
                _CashAccount_ID = -1;
                if (_CashAccounts_dt.Rows.Count > 0)
                    _CashAccount_ID = _CashAccounts_dt.AsEnumerable()
                            .Where(row => row.Field<string>("Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
                            .Select(row => row.Field<int>("ID"))
                            .DefaultIfEmpty(-1)
                            .FirstOrDefault();

                if (_CashAccount_ID == -1)
                    MarkRowError(e.Row, e.Column.ColumnName, "This cash account does not exist.");
            }

            if (e.Column.ColumnName == Last_Column_Name)
            {
                string microProjectText = GetText(e.Row, e, "Micro Project number");
                string paymentAmountText = GetText(e.Row, e, "Payment amount");
                string returnedAmountText = GetText(e.Row, e, "Returned amount");
                string currencyText = GetText(e.Row, e, "Currency").ToUpperInvariant();

                bool hasPaymentAmount = !paymentAmountText.IsNullOrWhiteSpace();
                bool hasReturnedAmount = !returnedAmountText.IsNullOrWhiteSpace();

                if (!int.TryParse(microProjectText, out _MicroProject_ID) || GetBeneficiaryId(_MicroProject_ID) == -1)
                {
                    MarkRowError(e.Row, "Micro Project number", "This project number does not exist.");
                }

                if (!TryParsePositiveAmount(paymentAmountText, out _Amount) || (hasPaymentAmount && _Amount <= 0))
                {
                    MarkRowError(e.Row, "Payment amount", "This column only accepts positive numbers.");
                }

                if (!TryParsePositiveAmount(returnedAmountText, out _ReturnedAmount) || (hasReturnedAmount && _ReturnedAmount <= 0))
                {
                    MarkRowError(e.Row, "Returned amount", "This column only accepts positive numbers.");
                }

                if (hasPaymentAmount == hasReturnedAmount)
                {
                    MarkRowError(e.Row, "Payment amount", "Enter either Payment amount or Returned amount, not both or neither.");
                    MarkRowError(e.Row, "Returned amount", "Enter either Payment amount or Returned amount, not both or neither.");
                }

                if (!string.Equals(currencyText, "USD", StringComparison.OrdinalIgnoreCase) && !Helper.CurrencyService.IsLocalCurrency(currencyText, false))
                {
                    MarkRowError(e.Row, "Currency", "Currency must be USD or SYR.");
                }
                else
                {
                    _Currency = currencyText;
                    string beneficiaryCurrency = GetBeneficiaryCurrency(_MicroProject_ID);
                    if (!beneficiaryCurrency.IsNullOrWhiteSpace() && beneficiaryCurrency != _Currency)
                    {
                        MarkRowError(e.Row, "Currency", "Entered currency does not match beneficiary loan currency.");
                    }

                    string cashTillProblem = CashTillCurrencyProblem(_CashAccount_ID, _Currency);
                    if (cashTillProblem != null)
                        MarkRowError(e.Row, "Cash till", cashTillProblem);
                }

                int beneficiaryId = GetBeneficiaryId(_MicroProject_ID);
                if (hasPaymentAmount && beneficiaryId != -1 && !HasGeneratedPayments(beneficiaryId))
                {
                    MarkRowError(e.Row, "Micro Project number", "No payments have been generated for this beneficiary.");
                }

                _IsReturnedPayment = hasReturnedAmount && !hasPaymentAmount;

                TotalPastedRows++;

                if (!e.Row.RowError.IsNullOrWhiteSpace())
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
            Login.MultiPayments_sc.Parameters.AddWithValue("@ReturnedAmount" + k, _ReturnedAmount);

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

            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); // تم تعديل HH لتكون بنظام 24 ساعة لضمان دقة التوقيت المسائي
            Login.MultiPayments_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);
            Login.MultiPayments_sc.Parameters.AddWithValue("@Form_Name" + k, _Form_Name);

            // تصحيح الخطأ المطبعي من @Uesr_ID إلى @User_ID
            Login.MultiPayments_sc.Parameters.AddWithValue("@User_ID" + k, Settings.Default.User_ID);

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

            // حلقة جلب معرّف المعاملة الأحدث (Action_ID) الموحد لجميع الأسطر لمنع تكرار المفاتيح الأساسية
            if (!_ActionIdVariableInitialized)
            {
                temp_query += @"SET @Action_ID = (
                            SELECT IFNULL(MAX(max_id), 0) + 1 AS next_id
                            FROM (
                                SELECT MAX(ID) AS max_id FROM action
                                UNION ALL
                                SELECT MAX(ID) FROM action_not_approved
                                UNION ALL
                                SELECT MAX(ID) FROM action_deleted
                            ) AS combined_max_ids);";
                _ActionIdVariableInitialized = true;
            }

            if (_IsReturnedPayment)
            {
                temp_query += @"SET @num" + k + @" = (
                       SELECT p.Num
                       FROM payment p
                       WHERE p.Beneficiary_ID = @Beneficiary_ID" + k + @"
                       ORDER BY (
                           SELECT MAX(active_action.ID)
                           FROM (
                               SELECT ID, AType_ID FROM action
                               UNION ALL
                               SELECT ID, AType_ID FROM action_not_approved
                           ) AS active_action
                           JOIN action_payment ap3 ON ap3.action_id = active_action.ID
                           WHERE ap3.payment_id = p.ID
                           AND active_action.AType_ID = (SELECT ID FROM actiontype WHERE shortname = 'I' LIMIT 1)
                       ) DESC, p.ID ASC
                       LIMIT 1);";

                temp_query += " SET @Action_ID" + k + " = @Action_ID;";

                temp_query += " INSERT INTO `action_not_approved`(`id`,`Till_ID`, `AType_ID`, `currency_tilltype_id`, `Amount`, `Returned_Amount`, `rate`,"
                        + " `ActionDate`, `Note`, `IsReceipt`, `MicroProject_ID`, cashaccount_id, created_at, User_ID)"
                        + " VALUES (@Action_ID" + k + ", @Till_ID" + k + ", (Select id from actiontype where shortname = 'R'),"
                        + " (SELECT `tilltypes`.`id` FROM `tilltypes` LEFT JOIN `beneficiary` ON `beneficiary`.`currency_tilltype_id` = `tilltypes`.ID WHERE beneficiary.id = @Beneficiary_ID" + k + "),"
                        + " 0, @ReturnedAmount" + k + ", 0, @Date" + k + ", @Note" + k + ", -1,"
                        + " @MicroProject_ID" + k + ", @CashAccount_ID" + k + ", @Todaydate" + k + ", @User_ID" + k + ");"; // تعديل اسم البارامتر هنا

                temp_query += " INSERT INTO payment(Amount, DueDate, Beneficiary_ID, Num) VALUES(-@ReturnedAmount" + k + ", @Date" + k + ", @Beneficiary_ID" + k + ", IFNULL(@num" + k + ", 0) + 1);";
                temp_query += " INSERT INTO action_payment (action_id, payment_id) VALUES (@Action_ID" + k + ", LAST_INSERT_ID());";
                temp_query += " SET @Action_ID = @Action_ID" + k + " + 1;";
                temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @Action_ID" + k + ", 'action Type = payment - I (Return)', CONCAT('beneficiary ID = ',@Beneficiary_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                _BeneficiaryIdsToRegenerate.Add(Beneficiary_ID);
                Login.MultiPayments_List.Add(temp_query);
                temp_query = "";
                return;
            }

            DataTable selected_rows_dt = new DataTable();
            System.Data.DataRow[] My_rows = _Payments_dt.Select("(payment_state = 'Unpaid' OR payment_state = 'Incomplete') and Beneficiary_ID = " + Beneficiary_ID + "", "Num ASC, DueDate ASC, ID ASC");

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
                        Double Payment_Amount = Convert.ToDouble(selected_rows_dt.Rows[i]["Amount"]);
                        Double Paid_Amount = selected_rows_dt.Rows[i].Field<Double>("Paid_Amount");
                        Double Current_Payment;
                        Double Payment_Need = Payment_Amount - Paid_Amount;

                        if (Payment_Need <= 0)
                            continue;

                        if (_Amount > Payment_Need)
                        {
                            Current_Payment = Payment_Need;

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

                        temp_query += "SET @Action_ID" + i + "_" + k + " = @Action_ID;";

                        temp_query += "SELECT Amount, Num, DueDate INTO @PaymentAmount" + i + "_" + k + ", @PaymentNum" + i + "_" + k + ", @PaymentDueDate" + i + "_" + k +
                                      " FROM payment WHERE ID = @payment_id" + i + "_" + k + " FOR UPDATE;";

                        temp_query += @"SET @CurrentPaid" + i + "_" + k + @" = (
                            SELECT IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0)
                            FROM payment
                            LEFT JOIN action_payment ON payment.ID = action_payment.payment_id
                            LEFT JOIN (
                                SELECT ID, Amount, Returned_Amount FROM action
                                UNION ALL
                                SELECT ID, Amount, Returned_Amount FROM action_not_approved
                            ) AS combined_action ON combined_action.ID = action_payment.action_id
                            WHERE payment.ID = @payment_id" + i + "_" + k + @");";

                        temp_query += @"SET @EarlierPayableCount" + i + "_" + k + @" = (
                            SELECT COUNT(*)
                            FROM (
                                SELECT prior_payment.ID,
                                       prior_payment.Amount,
                                       IFNULL(SUM(combined_action.Amount),0) - IFNULL(SUM(combined_action.Returned_Amount),0) AS PaidAmount
                                FROM payment prior_payment
                                LEFT JOIN action_payment ON prior_payment.ID = action_payment.payment_id
                                LEFT JOIN (
                                    SELECT ID, Amount, Returned_Amount FROM action
                                    UNION ALL
                                    SELECT ID, Amount, Returned_Amount FROM action_not_approved
                                ) AS combined_action ON combined_action.ID = action_payment.action_id
                                WHERE prior_payment.Beneficiary_ID = @Beneficiary_ID" + k + @"
                                  AND prior_payment.Amount > 0
                                  AND (
                                      prior_payment.Num < @PaymentNum" + i + "_" + k + @"
                                      OR (
                                          prior_payment.Num = @PaymentNum" + i + "_" + k + @"
                                          AND COALESCE(prior_payment.DueDate, '9999-12-31') < COALESCE(@PaymentDueDate" + i + "_" + k + @", '9999-12-31')
                                      )
                                      OR (
                                          prior_payment.Num = @PaymentNum" + i + "_" + k + @"
                                          AND COALESCE(prior_payment.DueDate, '9999-12-31') = COALESCE(@PaymentDueDate" + i + "_" + k + @", '9999-12-31')
                                          AND prior_payment.ID < @payment_id" + i + "_" + k + @"
                                      )
                                  )
                                GROUP BY prior_payment.ID, prior_payment.Amount
                                HAVING PaidAmount < prior_payment.Amount
                            ) AS earlier_payable
                        );";

                        // إصلاح خطأ نقص حقل الـ currency_tilltype_id وتصحيح اسم بارامتر المستخدم @User_ID
                        temp_query += "INSERT INTO `action_not_approved`(`id`,`Till_ID`, `AType_ID`, `currency_tilltype_id`, `Amount`, `rate`,"
                                    + " `ActionDate`, `Note`, `IsReceipt`, `MicroProject_ID`, cashaccount_id, created_at, User_ID)"
                                    + " VALUES ((SELECT CASE WHEN @PaymentAmount" + i + "_" + k + " > 0"
                                    + " AND @CurrentPaid" + i + "_" + k + " < @PaymentAmount" + i + "_" + k
                                    + " AND @Amount" + i + "_" + k + " <= (@PaymentAmount" + i + "_" + k + " - @CurrentPaid" + i + "_" + k + ")"
                                    + " AND @EarlierPayableCount" + i + "_" + k + " = 0"
                                    + " THEN @Action_ID" + i + "_" + k + " ELSE NULL END), @Till_ID" + k + ", (Select id from actiontype where shortname = 'I'),"
                                    + " (SELECT `tilltypes`.`id` FROM `tilltypes` LEFT JOIN `beneficiary` ON `beneficiary`.`currency_tilltype_id` = `tilltypes`.ID WHERE beneficiary.id = @Beneficiary_ID" + k + "),"
                                    + " @Amount" + i + "_" + k + ", 0, @Date" + k + ", @Note" + k + ", -1,"
                                    + " @MicroProject_ID" + k + ", @CashAccount_ID" + k + ", @Todaydate" + k + ", @User_ID" + k + ");";

                        temp_query += "SET @Action_ID = @Action_ID" + i + "_" + k + " + 1;";
                        temp_query += " INSERT INTO action_payment (action_id, payment_id) VALUES (@Action_ID" + i + "_" + k + ", @payment_id" + i + "_" + k + ");";

                        _Amount = _Amount - Current_Payment;
                        numofinserts++;
                        if (_Amount <= 0)
                            break;
                    }
                }
            }

            if (numofinserts == 0)
            {
                return;
            }

            for (int i = 0; i < numofinserts; i++)
            {
                temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'action_not_approved', @Action_ID" + i + "_" + k + ", 'action Type = payment - I', CONCAT('beneficiary ID = ',@Beneficiary_ID" + k + ") , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
            }

            _BeneficiaryIdsToRegenerate.Add(Beneficiary_ID);
            Login.MultiPayments_List.Add(temp_query);
            temp_query = "";
        }

        public void AppendDeferredRegenerationQueries()
        {
            if (_RegenerationQueriesAppended || Login.MultiPayments_AnyError)
                return;

            foreach (int beneficiaryId in _BeneficiaryIdsToRegenerate)
            {
                Login.MultiPayments_List.Add("CALL ReGenerate_AllPayments(" + beneficiaryId + ");");
            }

            _RegenerationQueriesAppended = true;
        }

        protected virtual void OnPasteCompleted()
        {
            PasteCompleted?.Invoke(this, EventArgs.Empty);
        }

    }
}
