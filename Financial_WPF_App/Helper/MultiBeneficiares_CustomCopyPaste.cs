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
    internal class MultiBeneficiares_CustomCopyPaste : GridCutCopyPaste
    {
        public event EventHandler PasteCompleted;

        private DataTable _Microrojects_dt, _Accounts_dt;

        public int TotalPastedRows { get; private set; } = 0;
        public int TotalErrorRows { get; private set; } = 0;

        string _FirstName = "";
        string _LastName = "";
        string _FatherName = "";
        string _NationalNumber = "";
        string _Mobile = "";
        string _Partners = "";
        int _MicroProject_ID = -1;
        string _Project_Name = "";
        string _Currency = "";
        Double _Exchange_Rate = 0;
        Double _Amount = 0;
        string _Date = "";
        string _FirstPaymentDate = "";
        Double _ToReturn_Amount = 0;
        float _Payment = 0;
        int _Payment_Interval = 0;
        string _Notes = "";
        string _ReceiptNumber = "";
        string _OrderNumber = "";
        int _Account_ID = -1;
        string _Form_Name = "Add_MultiBeneficiary_Window";


        int k = -1;

        HashSet<int> pastedProjectNumbers = new HashSet<int>();

        DataTable dt;

        public MultiBeneficiares_CustomCopyPaste(SfDataGrid dataGrid, DataTable Microrojects_dt, DataTable Accounts_dt) : base(dataGrid)
        {
            pastedProjectNumbers.Clear();


            _Microrojects_dt = Microrojects_dt ?? new DataTable();
            _Accounts_dt = Accounts_dt ?? new DataTable();

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
            _FirstName = "";
            _LastName = "";
            _FatherName = "";
            _NationalNumber = "";
            _Mobile = "";
            _Partners = "";
            _MicroProject_ID = -1;
            _Project_Name = "";
            _Currency = "";
            _Exchange_Rate = 0;
            _Amount = 0;
            _Date = "";
            _FirstPaymentDate = "";
            _ToReturn_Amount = 0;
            _Payment = 0;
            _Payment_Interval = 0;
            _Notes = "";
            _ReceiptNumber = "";
            _OrderNumber = "";
            _Account_ID = -1;
        }

        private void Column_Changing(object sender, DataColumnChangeEventArgs e)
        {
            string Last_Column_Name = e.Row.Table.Columns[e.Row.Table.Columns.Count - 1].ColumnName;

            if (e.Column.ColumnName == "Micro Project Number")
            {
                bool isNumber = Int32.TryParse(e.ProposedValue.ToString(), out _MicroProject_ID);
                bool Alreadyexists = false; bool isDuplicated = false;


                if (isNumber)
                {
                    if (_Microrojects_dt.Rows.Count > 0)
                        Alreadyexists = _Microrojects_dt.AsEnumerable().Any(row => row.Field<int>("MicroProject_ID") == Convert.ToInt32(e.ProposedValue.ToString()));

                    isDuplicated = pastedProjectNumbers.Contains(_MicroProject_ID);

                    int projectNumber = _MicroProject_ID;
                    pastedProjectNumbers.Add(projectNumber);
                }

                if (!isNumber)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
                if (Alreadyexists)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "There is another micro project with the same number within the same center.");
                    e.Row.RowError = "There are errors in this row.";
                }
                if (isDuplicated)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "Duplicated project number.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Project Title")
            {
                if (string.IsNullOrWhiteSpace(e.ProposedValue.ToString()))
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cell can not be empty.");
                    e.Row.RowError = "There are errors in this row.";
                }
                else
                {
                    _Project_Name = e.ProposedValue.ToString();
                }
            }
            else if (e.Column.ColumnName == "First name")
            {
                if (string.IsNullOrWhiteSpace(e.ProposedValue.ToString()))
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cell can not be empty.");
                    e.Row.RowError = "There are errors in this row.";
                }
                else
                {
                    _FirstName = e.ProposedValue.ToString();
                }
            }
            else if (e.Column.ColumnName == "Father name")
            {
                if (string.IsNullOrWhiteSpace(e.ProposedValue.ToString()))
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cell can not be empty.");
                    e.Row.RowError = "There are errors in this row.";
                }
                else
                {
                    _FatherName = e.ProposedValue.ToString();
                }
            }
            else if (e.Column.ColumnName == "Last name")
            {
                if (string.IsNullOrWhiteSpace(e.ProposedValue.ToString()))
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cell can not be empty.");
                    e.Row.RowError = "There are errors in this row.";
                }
                else
                {
                    _LastName = e.ProposedValue.ToString();
                }
            }
            else if (e.Column.ColumnName == "Names of partners")
            {
                _Partners = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "ID number")
            {
                _NationalNumber = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "Mobile number")
            {
                _Mobile = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "Currency")
            {
                if (string.IsNullOrWhiteSpace(e.ProposedValue.ToString()))
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This cell can not be empty.");
                    e.Row.RowError = "There are errors in this row.";
                }
                else
                {
                    _Currency = e.ProposedValue.ToString();
                }
            }
            else if (e.Column.ColumnName == "Exchange Rate")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _Exchange_Rate);

                if (!isNumber)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Loan amount")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _Amount);

                if (!isNumber)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Amount owed")
            {
                bool isNumber = Double.TryParse(e.ProposedValue.ToString(), out _ToReturn_Amount);

                if (!isNumber)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Payment")
            {
                bool isNumber = float.TryParse(e.ProposedValue.ToString(), out _Payment);

                if (!isNumber)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Payments interval (M)")
            {
                bool isNumber = Int32.TryParse(e.ProposedValue.ToString(), out _Payment_Interval);

                if (!isNumber)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts numbers.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Financing date")
            {
                bool exists = false;

                if (String.IsNullOrWhiteSpace(e.ProposedValue.ToString()))
                {
                    exists = true;
                    _Date = "";
                }
                else
                {
                    DateTime Adate;
                    bool isDate = false;
                    object val = e.ProposedValue;
                    string text = val?.ToString() ?? "";

                    // 1) If it's already a DateTime, cast directly
                    if (val is DateTime dt)
                    {
                        Adate = dt;
                        isDate = true;
                    }
                    // 2) If it's a numeric OLE date (Excel’s internal serial), convert
                    else if (double.TryParse(
                                 text,
                                 NumberStyles.Any,
                                 CultureInfo.InvariantCulture,
                                 out double oaSerial))
                    {
                        Adate = DateTime.FromOADate(oaSerial);
                        isDate = true;
                    }
                    // 3) Otherwise let .NET try parsing with CurrentCulture OR InvariantCulture
                    else if (
                        DateTime.TryParse(
                            text,
                            CultureInfo.CurrentCulture,
                            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                            out Adate)
                        ||
                        DateTime.TryParse(
                            text,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                            out Adate)
                    )
                    {
                        isDate = true;
                    }

                    if (isDate)
                    {
                        exists = true;
                        _Date = Adate.ToString("yyyy/MM/dd");
                    }
                }
                if (!exists)
                {
                    Login.MultiBeneficiary_AnyError = true;
                    e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts Dates.");
                    e.Row.RowError = "There are errors in this row.";
                }

            }
            else if (e.Column.ColumnName == "Payments start date")
            {
                bool exists = false; bool isDate = false;

                if (String.IsNullOrWhiteSpace(e.ProposedValue.ToString()))
                {
                    if (String.IsNullOrWhiteSpace(e.Row["Financing date"].ToString()))
                    {
                        exists = true;
                        _FirstPaymentDate = "";
                    }
                    else
                    {
                        Login.MultiBeneficiary_AnyError = true;
                        e.Row.SetColumnError(e.Column.ColumnName, "You cannot enter only one of these two dates, the financing date and the payments start date.");
                        e.Row.RowError = "There are errors in this row.";
                    }
                }
                else
                {
                    if (String.IsNullOrWhiteSpace(e.Row["Financing date"].ToString()))
                    {
                        Login.MultiBeneficiary_AnyError = true;
                        e.Row.SetColumnError(e.Column.ColumnName, "You cannot enter only one of these two dates, the financing date and the payments start date.");
                        e.Row.RowError = "There are errors in this row.";
                    }
                    else
                    {
                        DateTime Adate;
                        object val = e.ProposedValue;
                        string text = val?.ToString() ?? "";

                        // 1) If it's already a DateTime, cast directly
                        if (val is DateTime dt)
                        {
                            Adate = dt;
                            isDate = true;
                        }
                        // 2) If it's a numeric OLE date (Excel’s internal serial), convert
                        else if (double.TryParse(
                                     text,
                                     NumberStyles.Any,
                                     CultureInfo.InvariantCulture,
                                     out double oaSerial))
                        {
                            Adate = DateTime.FromOADate(oaSerial);
                            isDate = true;
                        }
                        // 3) Otherwise let .NET try parsing with CurrentCulture OR InvariantCulture
                        else if (
                            DateTime.TryParse(
                                text,
                                CultureInfo.CurrentCulture,
                                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                                out Adate)
                            ||
                            DateTime.TryParse(
                                text,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                                out Adate)
                        )
                        {
                            isDate = true;
                        }

                        if (isDate)
                        {
                            exists = true;
                            _FirstPaymentDate = Adate.ToString("yyyy/MM/dd");
                        }
                        else
                        {
                            Login.MultiBeneficiary_AnyError = true;
                            e.Row.SetColumnError(e.Column.ColumnName, "This column only accepts Dates.");
                            e.Row.RowError = "There are errors in this row.";
                        }
                    }

                }
            }
            else if (e.Column.ColumnName == "Receipt number")
            {
                _ReceiptNumber = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "Order Number")
            {
                _OrderNumber = e.ProposedValue.ToString();
            }
            else if (e.Column.ColumnName == "Group account")
            {
                bool exists = false;


                if (_Accounts_dt.Rows.Count > 0)
                    _Account_ID = _Accounts_dt.AsEnumerable()
                                .Where(row => row.Field<string>("Name")?.ToLower().Trim().Equals(e.ProposedValue.ToString().ToLower().Trim()) == true)
                                .Select(row => row.Field<int>("ID"))
                                .DefaultIfEmpty(-1)
                                .FirstOrDefault();

                if (_Account_ID != -1)
                    exists = true;

                if (exists)
                {

                }
                else
                {
                    Login.MultiBeneficiary_AnyError = true;

                    e.Row.SetColumnError(e.Column.ColumnName, "This account does not exist.");
                    e.Row.RowError = "There are errors in this row.";
                }
            }
            else if (e.Column.ColumnName == "Notes")
            {
                _Notes = e.ProposedValue.ToString();
            }



            if (e.Column.ColumnName == Last_Column_Name)
            {
                TotalPastedRows++;

                if (!e.Row.RowError.IsNullOrWhiteSpace())
                    TotalErrorRows++;


                //if (e.Row.RowError == "There are errors in this row.")
                //    TotalErrorRows++;

                if (!Login.MultiBeneficiary_AnyError)
                {
                    build_insert_query();
                }
                else
                {
                    Login.MultiBeneficiary_List.Clear();
                    Login.MultiBeneficiary_sc.Parameters.Clear();
                }

                clear_vlaues();
            }
        }

        public void build_insert_query()
        {
            k++;
            string temp_query = "";
            string log_query = "";

            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@FirstName" + k, _FirstName);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@LastName" + k, _LastName);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@FatherName" + k, _FatherName);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@NationalNumber" + k, _NationalNumber);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Mobile" + k, _Mobile);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Partners" + k, _Partners);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@MicroProject_ID" + k, _MicroProject_ID);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Project_Name" + k, _Project_Name);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Currency" + k, _Currency);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Exchange_Rate" + k, _Exchange_Rate);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Amount" + k, _Amount);
            if (_Date == "")
            {
                Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Date" + k, DBNull.Value);
            }
            else
            {
                Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Date" + k, _Date);
            }
            if (_FirstPaymentDate == "")
            {
                Login.MultiBeneficiary_sc.Parameters.AddWithValue("@FirstPaymentDate" + k, DBNull.Value);
            }
            else
            {
                Login.MultiBeneficiary_sc.Parameters.AddWithValue("@FirstPaymentDate" + k, _FirstPaymentDate);
            }
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@ToReturn_Amount" + k, _ToReturn_Amount);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Payment" + k, _Payment);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Payment_Interval" + k, _Payment_Interval);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Notes" + k, _Notes);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@ReceiptNumber" + k, _ReceiptNumber);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@OrderNumber" + k, _OrderNumber);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Account_ID" + k, _Account_ID);

            string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Todaydate" + k, Todaydate);

            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Uesr_ID" + k, Settings.Default.User_ID);
            Login.MultiBeneficiary_sc.Parameters.AddWithValue("@Form_Name" + k, _Form_Name);



            temp_query += @"INSERT INTO `beneficiary`(`FirstName`, `LastName`, `FatherName`, `NationalNumber`, `Mobile`, `Partners`,
                            `MicroProject_ID`, `Project_Name`,`currency_tilltype_id`,`ExchangeRate`, `Amount`, `Date`,`FirstPaymentDate`, `ToReturn_Amount`, `payment`, `Payment_Interval`, `Notes`, `ReceiptNumber`, `OrderNumber`, `Account_ID`)
                            VALUES(@FirstName" + k + ", @LastName" + k + ", @FatherName" + k + ", @NationalNumber" + k + ", @Mobile" + k + ", @Partners" + k + ", @MicroProject_ID" + k + ", @Project_Name" + k + ",(SELECT id FROM tilltypes WHERE UPPER(Name)=UPPER(@Currency" + k + ") LIMIT 1) ,@Exchange_Rate" + k + " , " +
                            "@Amount" + k + ", @Date" + k + ", @FirstPaymentDate" + k + ", @ToReturn_Amount" + k + ", @payment" + k + ", @Payment_Interval" + k + "," +
                            "@Notes" + k + ", @ReceiptNumber" + k + ", @OrderNumber" + k + ", @Account_ID" + k + ");";

            temp_query += " SET @Beneficiary_ID" + k + " = Last_Insert_ID();";


            if (_Date == "" && _FirstPaymentDate == "")
            {

            }
            else
            {
                temp_query += "Call ReGenerate_allPayments(@Beneficiary_ID" + k + ");";
            }

            temp_query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name" + k + ", 'INSERT', 'Beneficiary', @Beneficiary_ID" + k + ", CONCAT('beneficiary Name = ', @FirstName" + k + ", ' ', @FatherName" + k + ",' ', @LastName" + k + "), CONCAT('Account ID = ',@Account_ID" + k + "), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

            Login.MultiBeneficiary_List.Add(temp_query);
            temp_query = "";

        }

        protected virtual void OnPasteCompleted()
        {
            PasteCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
