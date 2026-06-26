using System;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Financial_WPF_App.Properties;
using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;


namespace Financial_WPF_App.Models
{
    public class Action
    {
        public Action()
        {

        }

        public DataTable Select(string Action_ID, string Till_IDs, string SubProject_ID, bool showClosed)
        {

            string query = @"SELECT category.ID AS `Category_ID`, category.Name as `Category_Name`,
                project.ID AS `Project_ID`, project.Name AS `Project_Name`,
                subproject.ID AS `Subproject_ID`, subproject.Name AS `Subproject_Name`,
                account.ID AS `Account_ID`, account.Name AS `Account_Name`,
                accounttype.id AS `Accounttype_UD`, accounttype.name AS `Accounttype_name`,
                action.currency_tilltype_id AS 'Currency_TillType_ID', currency_type.Name AS 'Currency',
                till.id AS `Till_ID`, tilltypes.id AS `Tilltypes_ID`, tilltypes.Name as `Tilltypes_Name`,

                action.`ID` as 'ID' , action.`ObserveAcion_ID`,
                actiontype.`Name` as 'AType', action.`Amount`, action.`IsApproved`, approved_byuser.`Username` as 'approved_by', action.`Approved_at`, created_byuser.`Username` as 'created_by', created_byuser.`ID` as 'created_byuser_ID', edited_byuser.username as 'edited_by',
                (CASE   WHEN (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '')
                AND actiontype.shortname = 'I' THEN action.Amount  ELSE 0 END) AS 'Income',
                (CASE WHEN (tilltypes.Name = 'SYR' OR tilltypes.Name = 'PAY') AND currency_type.Name = 'USD' AND actiontype.shortname = 'I' THEN action.Amount
                ELSE 0 END) AS 'Incomeusd',
                (CASE WHEN actiontype.shortname = 'O' THEN action.Amount ELSE 0 END) AS 'Outcome',
                (CASE WHEN actiontype.shortname = 'R' THEN action.Amount ELSE 0 END) AS 'Returned',
                (CASE WHEN actiontype.shortname = 'Ex' THEN action.Amount ELSE 0 END) AS 'Exchange',
                (CASE  WHEN (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '')
                AND actiontype.shortname = 'E' THEN action.Amount ELSE 0 END) AS 'Expand',
               (CASE WHEN tilltypes.Name = 'SYR' AND currency_type.Name = 'USD'   AND actiontype.shortname = 'E'
               THEN action.Amount  ELSE 0 END) AS 'Expandusd',


                (SELECT account.ID FROM action oaction LEFT JOIN till ON oaction.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID LEFT JOIN accounttype oaccounttype on oaccounttype.id = account.Accounttype_id WHERE oaccounttype.name = 'Exchange' and accounttype.name <> 'Exchange' AND oaction.ObserveAcion_ID = action.`ID` GROUP BY account.ID) AS 'ExchangeAcc_ID',
                (SELECT account.Name FROM action oaction LEFT JOIN till ON oaction.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID LEFT JOIN accounttype oaccounttype on oaccounttype.id = account.Accounttype_id WHERE oaccounttype.name = 'Exchange' and accounttype.name <> 'Exchange'  AND oaction.ObserveAcion_ID = action.`ID` GROUP BY account.ID) AS 'ExchangeAcc_Name',

                donor.id as 'Donor_ID', donor.name as 'Donor',
                action.`MicroProject_ID` ,

                (select CONCAT(`FirstName`,' ',`FatherName`, ' ',`LastName`) from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                where beneficiary.MicroProject_ID = action.MicroProject_ID AND account.SubProject_ID = subproject.ID) as 'Name',

                 action.`ActionDate`,DATE_FORMAT(action.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action.ActionDate, ""%Y"") as 'ActionYear', action.`Note` ,action.`E_Note`,
                 action.`rate` as 'Rate', (Case when actiontype.shortname = 'Ex' then (action.Amount * action.rate) else 0 END) as `Exchanged`,
                 (case action.`IsReceipt` when 0 then 'receipt' when 1 then 'invoice' when 2 then 'payslip' else '' end) as 'IsReceipt' ,
                 action.`ReceiptNote` , action.Employee_Salary_ID,
                 (SELECT employee.id FROM employee_salary LEFT JOIN employee_subproject ON employee_subproject.id = employee_salary.employee_subproject_id LEFT JOIN employee ON employee.id = employee_subproject.employee_id WHERE employee_salary.id = action.Employee_Salary_ID) AS 'Employee_ID',
                 (SELECT CASE WHEN employee.FatherName = '' THEN CONCAT(employee.FirstName,' ',employee.LastName) ELSE
                 CONCAT(employee.FirstName,' ',employee.FatherName,' ',employee.LastName) END FROM employee_salary LEFT JOIN employee_subproject ON employee_subproject.id = employee_salary.employee_subproject_id LEFT JOIN employee ON employee.id = employee_subproject.employee_id WHERE employee_salary.id = action.Employee_Salary_ID) AS 'Employee_Name',
                 all_avgrates.id as 'AvgRateID',all_avgrates.value as 'AvgRateValue', all_avgrates.Name as 'AvgRateName',
                 (CASE WHEN currency_type.Name = 'USD' THEN action.Amount ELSE  action.Amount / all_avgrates.value END) AS `USD_Value`,
                 budget_line.budget_id as 'Budget_ID', linecategory.id as 'LineCategory_ID', action.`BudgetLine_ID`, line.Name as 'Line', budget.Name as 'Budget',
                 action.`IncomeType_ID`, action.`Rate_ID`,
                 action.`ObserveTill_ID`,
                 (select account.name from account left join till on till.account_id = account.id where till.id = action.`ObserveTill_ID`) as 'ObserveAccount',
                 action.`CashAccount_ID`, cashaccount.Name as 'CashAccount', (select tilltypes.Name from till left join tilltypes on tilltypes.ID = till.Type_ID where till.id = action.`ObserveTill_ID`) as 'ObserveTill',
                 action.`ObserveCashAccount_ID` as 'ObserveCashAccount_ID',
                 obs_cashaccount.Name as 'ObserveCashAccount',
                 action.Exchange_Cash_ID as 'Exchange_Cash_ID', exchange_cashaccount.name as 'Exchange_Cash_Name',
                 fiscalbond.ID as 'FiscalBond_ID' ,fiscalbond.Name as 'fiscalbond' , action.created_at, action.updated_at, action.`SourceAction`,
                 (select count(*) from action_attachments where action_attachments.action_id = action.id) as 'Attachments_count'

                 FROM `action` LEFT JOIN actiontype on actiontype.id = action.atype_id
                 LEFT JOIN budget_line on action.BudgetLine_ID = budget_line.id
                 LEFT JOIN line on line.id = budget_line.line_id
                 LEFT JOIN budget on budget.id = budget_line.budget_id
                 LEFT JOIN linecategory on linecategory.id = line.Category_ID
                 LEFT JOIN cashaccount on cashaccount.id = action.`CashAccount_ID`
                 LEFT JOIN cashaccount obs_cashaccount on obs_cashaccount.id = action.`ObserveCashAccount_ID`
                 LEFT JOIN cashaccount exchange_cashaccount on exchange_cashaccount.id = action.`Exchange_Cash_ID`
                 LEFT JOIN donor on donor.id = action.`Donor_ID`
                 LEFT JOIN all_avgrates on action.Rate_ID = all_avgrates.id
                 LEFT join `till` on action.Till_ID = till.ID
                 LEFT join `tilltypes` on tilltypes.ID = till.Type_ID
                 LEFT join `account` on account.ID = till.Account_ID
                 LEFT JOIN accounttype on account.Accounttype_id = accounttype.id
                 LEFT JOIN `account_donor` ON account_donor.account_ID = account.ID
                 LEFT JOIN subproject on subproject.ID = account.SubProject_ID
                 LEFT JOIN project ON subproject.Project_ID = project.ID
                 LEFT JOIN category ON category.ID = project.Category_ID
                 Left join `fiscalbond` on fiscalbond.Till_ID = till.ID
                 LEFT JOIN `user` approved_byuser on approved_byuser.id = action.Approved_by
                 LEFT JOIN `user` created_byuser on created_byuser.id = action.User_ID
                 LEFT JOIN `user` edited_byuser on edited_byuser.id = edited_by
                 LEFT JOIN tilltypes AS currency_type ON currency_type.ID = action.currency_tilltype_id";



            string condition = " where 1 ";

            if (Action_ID != "")
            {
                condition += " and action.ID in (" + Action_ID + ")";
            }
            if (Till_IDs != "")
            {
                condition += " and till.ID in (" + Till_IDs + ")";
            }
            if (SubProject_ID != "-1" && SubProject_ID != "")
            {
                condition += " and account.SubProject_ID = @SubProject_ID";
            }

            if (!showClosed)
            {
                condition += " and account.IsClosed = 0 ";
            }

            query += condition + " Group by action.id " + " ORDER BY action.ActionDate DESC, action.ID DESC;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataSet Select_Account_Balances(int Account_ID)
        {

            string query = "SELECT cash_balances.CashAccount_Name AS 'Account', cash_balances.Account_I AS 'Income_SYR' , cash_balances.Account_E AS 'Expenses_SYR' , cash_balances.Account_Net AS 'Difference_SYR'" +
                ", cash_balances.Account_I_USD AS 'Income_USD',cash_balances.Account_E_USD AS 'Expenses_USD',cash_balances.Account_Net_USD AS 'Difference_USD' " +
                " FROM cash_balances WHERE cash_balances.Account_id = @Account_ID;";

            DataSet ds = new DataSet();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.CommandTimeout = 21600;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(ds);
                Login.CloseAndClearConnection();
            }
            return ds;
        }

        public DataTable Select_action_not_approved(string Action_ID, string Till_IDs, string SubProject_ID, bool showClosed, bool ForApproval)
        {

            string query = @"SELECT category.ID AS `Category_ID`, category.Name as `Category_Name`,
                project.ID AS `Project_ID`, project.Name AS `Project_Name`,
                subproject.ID AS `Subproject_ID`, subproject.Name AS `Subproject_Name`,
                account.ID AS `Account_ID`, account.Name AS `Account_Name`,
                accounttype.id AS `Accounttype_UD`, accounttype.name AS `Accounttype_name`,
               action_not_approved.currency_tilltype_id AS 'Currency_TillType_ID', currency_type.Name AS 'Currency',
                till.id AS `Till_ID`, tilltypes.id AS `Tilltypes_ID`, tilltypes.Name as `Tilltypes_Name`,

                action_not_approved.`ID` as 'ID' , action_not_approved.`ObserveAcion_ID`,
                actiontype.`Name` as 'AType', action_not_approved.`Amount`, action_not_approved.`IsApproved`, approved_byuser.`Username` as 'approved_by', action_not_approved.`Approved_at`, created_byuser.`Username` as 'created_by', created_byuser.`ID` as 'created_byuser_ID', edited_byuser.username as 'edited_by',
                (CASE   WHEN (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '')
                AND actiontype.shortname = 'I' THEN action_not_approved.Amount  ELSE 0 END) AS 'Income',
                (CASE WHEN tilltypes.Name = 'SYR' AND currency_type.Name = 'USD' AND actiontype.shortname = 'I' THEN action_not_approved.Amount
                ELSE 0 END) AS 'Incomeusd',
                (CASE WHEN actiontype.shortname = 'O' THEN action_not_approved.Amount ELSE 0 END) AS 'Outcome',
                (CASE WHEN actiontype.shortname = 'R' THEN action_not_approved.Amount ELSE 0 END) AS 'Returned',
                (CASE WHEN actiontype.shortname = 'Ex' THEN action_not_approved.Amount ELSE 0 END) AS 'Exchange',
                (CASE  WHEN (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '')
                AND actiontype.shortname = 'E' THEN action_not_approved.Amount ELSE 0 END) AS 'Expand',
               (CASE WHEN tilltypes.Name = 'SYR' AND currency_type.Name = 'USD'   AND actiontype.shortname = 'E'
               THEN action_not_approved.Amount  ELSE 0 END) AS 'Expandusd',


                (SELECT account.ID FROM action_not_approved oaction LEFT JOIN till ON oaction.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID LEFT JOIN accounttype oaccounttype on oaccounttype.id = account.Accounttype_id WHERE oaccounttype.name = 'Exchange' and accounttype.name <> 'Exchange' AND oaction.ObserveAcion_ID = action_not_approved.`ID` GROUP BY account.ID) AS 'ExchangeAcc_ID',
                (SELECT account.Name FROM action_not_approved oaction LEFT JOIN till ON oaction.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID LEFT JOIN accounttype oaccounttype on oaccounttype.id = account.Accounttype_id WHERE oaccounttype.name = 'Exchange' and accounttype.name <> 'Exchange'  AND oaction.ObserveAcion_ID = action_not_approved.`ID` GROUP BY account.ID) AS 'ExchangeAcc_Name',

                donor.id as 'Donor_ID', donor.name as 'Donor',
                action_not_approved.`MicroProject_ID` ,

                (select CONCAT(`FirstName`,' ',`FatherName`, ' ',`LastName`) from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                where beneficiary.MicroProject_ID = action_not_approved.MicroProject_ID AND account.SubProject_ID = subproject.ID) as 'Name',

                action_not_approved.`ActionDate`,DATE_FORMAT(action_not_approved.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action_not_approved.ActionDate, ""%Y"") as 'ActionYear', action_not_approved.`Note` ,action_not_approved.`E_Note`,
                action_not_approved.`rate` as 'Rate', (Case when actiontype.shortname = 'Ex' then (action_not_approved.Amount * action_not_approved.rate) else 0 END) as `Exchanged`,
                (case action_not_approved.`IsReceipt` when 0 then 'receipt' when 1 then 'invoice' when 2 then 'payslip' else '' end) as 'IsReceipt' ,
                action_not_approved.`ReceiptNote` ,action_not_approved.Employee_Salary_ID,
                (SELECT employee.id FROM employee_salary LEFT JOIN employee_subproject ON employee_subproject.id = employee_salary.employee_subproject_id LEFT JOIN employee ON employee.id = employee_subproject.employee_id WHERE employee_salary.id = action_not_approved.Employee_Salary_ID) AS 'Employee_ID',
                (SELECT CASE WHEN employee.FatherName = '' THEN CONCAT(employee.FirstName,' ',employee.LastName) ELSE
                CONCAT(employee.FirstName,' ',employee.FatherName,' ',employee.LastName) END FROM employee_salary LEFT JOIN employee_subproject ON employee_subproject.id = employee_salary.employee_subproject_id LEFT JOIN employee ON employee.id = employee_subproject.employee_id WHERE employee_salary.id = action_not_approved.Employee_Salary_ID) AS 'Employee_Name',
                all_avgrates.id as 'AvgRateID',all_avgrates.value as 'AvgRateValue', all_avgrates.Name as 'AvgRateName',
                (CASE WHEN currency_type.Name = 'USD' THEN action_not_approved.Amount ELSE action_not_approved.Amount / all_avgrates.value END) AS `USD_Value`,
                budget_line.budget_id as 'Budget_ID', linecategory.id as 'LineCategory_ID', action_not_approved.`BudgetLine_ID`, line.Name as 'Line', budget.Name as 'Budget',
                action_not_approved.`IncomeType_ID`, action_not_approved.`Rate_ID`,
                action_not_approved.`ObserveTill_ID`,
                (select account.name from account left join till on till.account_id = account.id where till.id = action_not_approved.`ObserveTill_ID`) as 'ObserveAccount',
                action_not_approved.`CashAccount_ID`, cashaccount.Name as 'CashAccount', (select tilltypes.Name from till left join tilltypes on tilltypes.ID = till.Type_ID where till.id = action_not_approved.`ObserveTill_ID`) as 'ObserveTill',
                action_not_approved.`ObserveCashAccount_ID` as 'ObserveCashAccount_ID',
                obs_cashaccount.Name as 'ObserveCashAccount',
                action_not_approved.Exchange_Cash_ID as 'Exchange_Cash_ID', exchange_cashaccount.name as 'Exchange_Cash_Name',
                fiscalbond.ID as 'FiscalBond_ID' ,fiscalbond.Name as 'fiscalbond' , action_not_approved.created_at, action_not_approved.updated_at, action_not_approved.`SourceAction`,
                (select count(*) from action_attachments where action_attachments.action_id = action_not_approved.id) as 'Attachments_count'


                FROM `action_not_approved` LEFT JOIN actiontype on actiontype.id = action_not_approved.atype_id
                LEFT JOIN budget_line on action_not_approved.BudgetLine_ID = budget_line.id
                LEFT JOIN line on line.id = budget_line.line_id
                LEFT JOIN budget on budget.id = budget_line.budget_id
                LEFT JOIN linecategory on linecategory.id = line.Category_ID
                LEFT JOIN cashaccount on cashaccount.id = action_not_approved.`CashAccount_ID`
                LEFT JOIN cashaccount obs_cashaccount on obs_cashaccount.id = action_not_approved.`ObserveCashAccount_ID`
                LEFT JOIN cashaccount exchange_cashaccount on exchange_cashaccount.id = action_not_approved.`Exchange_Cash_ID`
                LEFT JOIN donor on donor.id = action_not_approved.`Donor_ID`
                LEFT JOIN all_avgrates on action_not_approved.Rate_ID = all_avgrates.id
                LEFT join `till` on action_not_approved.Till_ID = till.ID
                LEFT join `tilltypes` on tilltypes.ID = till.Type_ID
                LEFT join `account` on account.ID = till.Account_ID
                LEFT JOIN accounttype on account.Accounttype_id = accounttype.id
                LEFT JOIN `account_donor` ON account_donor.account_ID = account.ID
                LEFT JOIN subproject on subproject.ID = account.SubProject_ID
                LEFT JOIN project ON subproject.Project_ID = project.ID
                LEFT JOIN category ON category.ID = project.Category_ID
                Left join `fiscalbond` on fiscalbond.Till_ID = till.ID
                LEFT JOIN `user` approved_byuser on approved_byuser.id = action_not_approved.Approved_by
                LEFT JOIN `user` created_byuser on created_byuser.id = action_not_approved.User_ID
                LEFT JOIN `user` edited_byuser on edited_byuser.id = edited_by
                LEFT JOIN tilltypes AS currency_type ON currency_type.ID = action_not_approved.currency_tilltype_id";


            string condition = " where 1 ";

            if (Action_ID != "")
            {
                condition += " and action_not_approved.ID in (" + Action_ID + ")";
            }
            if (Till_IDs != "")
            {
                condition += " and till.ID in (" + Till_IDs + ")";
            }
            if (SubProject_ID != "-1" && SubProject_ID != "")
            {
                condition += " and account.SubProject_ID = @SubProject_ID";
            }

            if (!showClosed)
            {
                condition += " and account.IsClosed = 0 ";
            }


            if (ForApproval)
            {
                condition += " and action_not_approved.SourceAction = 1";
                condition += " and (edited_byuser.ID <> " + Properties.Settings.Default.User_ID + " or edited_byuser.ID is null) and created_byuser.`ID` <> " + Properties.Settings.Default.User_ID;

                if (Properties.Settings.Default.Role != "Head Office")
                    condition += " and action_not_approved.`Amount` <= (select role.Approval_Limit from role where role.Name = '" + Properties.Settings.Default.Role + "')";
            }

            query += condition + " GROUP BY action_not_approved.id " +" ORDER BY action_not_approved.ActionDate DESC, action_not_approved.ID DESC;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@SubProject_ID", SubProject_ID);

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_Deleted_action(string Till_IDs, bool ForApproval)
        {

            string query = @"SELECT category.ID AS `Category_ID`, category.Name as `Category_Name`,
                project.ID AS `Project_ID`, project.Name AS `Project_Name`,
                subproject.ID AS `Subproject_ID`, subproject.Name AS `Subproject_Name`,
                account.ID AS `Account_ID`, account.Name AS `Account_Name`,
                action_deleted.currency_tilltype_id AS 'Currency_TillType_ID', currency_type.Name AS 'Currency',
                accounttype.id AS `Accounttype_UD`, accounttype.name AS `Accounttype_name`,
                till.id AS `Till_ID`, tilltypes.id AS `Tilltypes_ID`, tilltypes.Name as `Tilltypes_Name`,

                action_deleted.`ID` as 'ID' , action_deleted.`ObserveAcion_ID`,
                actiontype.`Name` as 'AType', action_deleted.`Amount`,
                action_deleted.`IsApproved`, approved_byuser.`Username` as 'approved_by', action_deleted.`Approved_at`,
                created_byuser.`Username` as 'created_by', created_byuser.`ID` as 'created_byuser_ID',
                edited_byuser.username as 'edited_by',
                deleted_byuser.username as 'deleted_by', action_deleted.`deleted_at` as 'deleted_at',
                (CASE   WHEN (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '')
                AND actiontype.shortname = 'I' THEN action_deleted.Amount  ELSE 0 END) AS 'Income',
                (CASE WHEN tilltypes.Name = 'SYR' AND currency_type.Name = 'USD' AND actiontype.shortname = 'I' THEN action_deleted.Amount
                ELSE 0 END) AS 'Incomeusd',
                (CASE WHEN actiontype.shortname = 'O' THEN action_deleted.Amount ELSE 0 END) AS 'Outcome',
                (CASE WHEN actiontype.shortname = 'R' THEN action_deleted.Amount ELSE 0 END) AS 'Returned',
                (CASE WHEN actiontype.shortname = 'Ex' THEN action_deleted.Amount ELSE 0 END) AS 'Exchange',
                (CASE  WHEN (currency_type.Name = 'SYR' OR currency_type.Name IS NULL OR currency_type.Name = '')
                AND actiontype.shortname = 'E' THEN action_deleted.Amount ELSE 0 END) AS 'Expand',
                (CASE WHEN tilltypes.Name = 'SYR' AND currency_type.Name = 'USD'   AND actiontype.shortname = 'E'
                THEN action_deleted.Amount  ELSE 0 END) AS 'Expandusd',

                (SELECT account.ID FROM action_deleted oaction LEFT JOIN till ON oaction.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID LEFT JOIN accounttype oaccounttype on oaccounttype.id = account.Accounttype_id WHERE oaccounttype.name = 'Exchange' and accounttype.name <> 'Exchange' AND oaction.ObserveAcion_ID = action_deleted.`ID` GROUP BY account.ID) AS 'ExchangeAcc_ID',
                (SELECT account.Name FROM action_deleted oaction LEFT JOIN till ON oaction.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID LEFT JOIN accounttype oaccounttype on oaccounttype.id = account.Accounttype_id WHERE oaccounttype.name = 'Exchange' and accounttype.name <> 'Exchange'  AND oaction.ObserveAcion_ID = action_deleted.`ID` GROUP BY account.ID) AS 'ExchangeAcc_Name',

                donor.id as 'Donor_ID', donor.name as 'Donor',
                action_deleted.`MicroProject_ID` ,

                (select CONCAT(`FirstName`,' ',`FatherName`, ' ',`LastName`) from beneficiary JOIN account on beneficiary.Account_ID = account.ID
                where beneficiary.MicroProject_ID = action_deleted.MicroProject_ID AND account.SubProject_ID = subproject.ID) as 'Name',

                action_deleted.`ActionDate`,DATE_FORMAT(action_deleted.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action_deleted.ActionDate, ""%Y"") as 'ActionYear', action_deleted.`Note` ,action_deleted.`E_Note`,
                action_deleted.`rate` as 'Rate', (Case when actiontype.shortname = 'Ex' then (action_deleted.Amount * action_deleted.rate) else 0 END) as `Exchanged`,
                (case action_deleted.`IsReceipt` when 0 then 'receipt' when 1 then 'invoice' when 2 then 'payslip' else '' end) as 'IsReceipt' ,
                action_deleted.`ReceiptNote` ,action_deleted.Employee_Salary_ID,
                (SELECT employee.id FROM employee_salary LEFT JOIN employee_subproject ON employee_subproject.id = employee_salary.employee_subproject_id LEFT JOIN employee ON employee.id = employee_subproject.employee_id WHERE employee_salary.id = action_deleted.Employee_Salary_ID) AS 'Employee_ID',
                (SELECT CASE WHEN employee.FatherName = '' THEN CONCAT(employee.FirstName,' ',employee.LastName) ELSE
                CONCAT(employee.FirstName,' ',employee.FatherName,' ',employee.LastName) END FROM employee_salary LEFT JOIN employee_subproject ON employee_subproject.id = employee_salary.employee_subproject_id LEFT JOIN employee ON employee.id = employee_subproject.employee_id WHERE employee_salary.id = action_deleted.Employee_Salary_ID) AS 'Employee_Name',
                all_avgrates.id as 'AvgRateID',all_avgrates.value as 'AvgRateValue', all_avgrates.Name as 'AvgRateName',
                (action_deleted.`Amount`/ all_avgrates.value) as 'USD_Value',
                budget_line.budget_id as 'Budget_ID', linecategory.id as 'LineCategory_ID', action_deleted.`BudgetLine_ID`, line.Name as 'Line', budget.Name as 'Budget',
                action_deleted.`IncomeType_ID`, action_deleted.`Rate_ID`,
                action_deleted.`ObserveTill_ID`,
                (select account.name from account left join till on till.account_id = account.id where till.id = action_deleted.`ObserveTill_ID`) as 'ObserveAccount',
                action_deleted.`CashAccount_ID`, cashaccount.Name as 'CashAccount', (select tilltypes.Name from till left join tilltypes on tilltypes.ID = till.Type_ID where till.id = action_deleted.`ObserveTill_ID`) as 'ObserveTill',
                action_deleted.`ObserveCashAccount_ID` as 'ObserveCashAccount_ID',
                obs_cashaccount.Name as 'ObserveCashAccount',
                action_deleted.Exchange_Cash_ID as 'Exchange_Cash_ID', exchange_cashaccount.name as 'Exchange_Cash_Name',
                fiscalbond.ID as 'FiscalBond_ID' ,fiscalbond.Name as 'fiscalbond' , action_deleted.created_at, action_deleted.updated_at, action_deleted.`SourceAction`,
                (select count(*) from action_attachments where action_attachments.action_id = action_deleted.id) as 'Attachments_count'


                FROM `action_deleted` LEFT JOIN actiontype on actiontype.id = action_deleted.atype_id
                LEFT JOIN budget_line on action_deleted.BudgetLine_ID = budget_line.id
                LEFT JOIN line on line.id = budget_line.line_id
                LEFT JOIN budget on budget.id = budget_line.budget_id
                LEFT JOIN linecategory on linecategory.id = line.Category_ID
                LEFT JOIN cashaccount on cashaccount.id = action_deleted.`CashAccount_ID`
                LEFT JOIN cashaccount obs_cashaccount on obs_cashaccount.id = action_deleted.`ObserveCashAccount_ID`
                LEFT JOIN cashaccount exchange_cashaccount on exchange_cashaccount.id = action_deleted.`Exchange_Cash_ID`
                LEFT JOIN donor on donor.id = action_deleted.`Donor_ID`
                LEFT JOIN all_avgrates on action_deleted.Rate_ID = all_avgrates.id
                LEFT join `till` on action_deleted.Till_ID = till.ID
                LEFT join `tilltypes` on tilltypes.ID = till.Type_ID
                LEFT join `account` on account.ID = till.Account_ID
                LEFT JOIN accounttype on account.Accounttype_id = accounttype.id
                LEFT JOIN `account_donor` ON account_donor.account_ID = account.ID
                LEFT JOIN subproject on subproject.ID = account.SubProject_ID
                LEFT JOIN project ON subproject.Project_ID = project.ID
                LEFT JOIN category ON category.ID = project.Category_ID
                Left join `fiscalbond` on fiscalbond.Till_ID = till.ID
                LEFT JOIN `user` approved_byuser on approved_byuser.id = action_deleted.Approved_by
                LEFT JOIN `user` created_byuser on created_byuser.id = action_deleted.User_ID
                LEFT JOIN `user` edited_byuser on edited_byuser.id = edited_by
                LEFT JOIN `user` deleted_byuser on deleted_byuser.id = deleted_by
                LEFT JOIN tilltypes AS currency_type ON currency_type.ID = action_deleted.currency_tilltype_id";


            string condition = " where 1 ";

            if (Till_IDs != "")
            {
                condition += " and till.ID in (" + Till_IDs + ")";
            }

            if (ForApproval)
            {
                condition += " and action_deleted.SourceAction = 1";
                condition += " and (edited_byuser.ID <> " + Properties.Settings.Default.User_ID + " or edited_byuser.ID is null) and created_byuser.`ID` <> " + Properties.Settings.Default.User_ID + " and deleted_by <> " + Properties.Settings.Default.User_ID + "";

                if (Properties.Settings.Default.Role != "Head Office")
                    condition += " and action_deleted.`Amount` <= (select role.Approval_Limit from role where role.Name = '" + Properties.Settings.Default.Role + "')";
            }

            query += condition + " Group by action_deleted.id " + " ORDER BY action_deleted.ActionDate DESC, action_deleted.ID DESC;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_All_Actions(string Account_Type, string Category, string inserted_value, string Account_ID, string TillType)
        {
            string query = @"SELECT allactions.ID as 'Action_ID', allactions.IsApproved as 'IsApproved',
                                allactions.SourceAction as 'SourceAction', account.ID as 'Account_ID',
                                accounttype.name as 'Accounttype_Name',
                                category.name as 'Category.Name',
                                actiontype.shortname as 'Actiontype',
                                account.Name as 'Account.Name',
                                tilltypes.name as 'Tilltypes' 
                                FROM
                                (
                                SELECT * FROM action
                                UNION ALL
                                SELECT * FROM action_not_approved
                                ) AS `allactions`
                                LEFT JOIN actiontype ON actiontype.id = allactions.AType_ID
                                LEFT JOIN till ON till.ID = allactions.Till_ID
                                LEFT JOIN tilltypes ON tilltypes.ID = till.Type_ID
                                LEFT JOIN account ON till.Account_ID = account.ID
                                LEFT JOIN accounttype ON accounttype.id = account.Accounttype_id
                                LEFT JOIN subproject ON subproject.id = account.subproject_id
                                LEFT JOIN project ON project.id = subproject.project_id
                                LEFT JOIN category ON category.id = project.category_id";


            string condition = " WHERE 1 and Lower(tilltypes.Name) <> 'pay' ";

            if (Account_Type != "")
            {
                condition += " and LOWER(accounttype.name) = @Account_Type";
            }
            if (Category != "")
            {
                condition += " and LOWER(category.name) = @Category";
            }
            if (inserted_value != "")
            {
                condition += " and LOWER(actiontype.shortname) = @inserted_value";
            }
            if (Account_ID != "")
            {
                condition += " and account.ID = @Account_ID";
            }
            if (TillType != "")
            {
                condition += " and LOWER(tilltypes.name) = @TillType";
            }

            query += condition + " Group by allactions.id;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Account_Type", Account_Type.ToLower());
                sc.Parameters.AddWithValue("@Category", Category.ToLower());
                sc.Parameters.AddWithValue("@inserted_value", inserted_value.ToLower());
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@TillType", TillType.ToLower());

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_CashAction(int CashAction_ID)
        {
            string query = @"SELECT cashaction.id AS 'cashaction_id', cashaction.Note AS 'Note',cashaction.E_Note AS 'E_Note',cashaction.ActionDate AS 'ActionDate',
                                cashaction_denominations.id AS 'cashaction_denominations_id', denominations.id AS 'denominations_id',
                                case when denominations.name = 0 then 'Uncounted' else denominations.name END AS 'denominations_name',actiontype.shortname AS 'actiontype_shortname',
                                Case when actiontype.shortname = 'I' then 'Income' when actiontype.shortname = 'E' then 'Outcome' END AS 'actiontype_name',
                                Case when actiontype.shortname = 'I' then cashaction_denominations.amount else 0 END as 'Income',
                                Case when actiontype.shortname = 'E' then cashaction_denominations.amount else 0 END as 'Expand',
                                cashaction_denominations.amount AS 'Amount'


                                FROM cashaction
                                LEFT JOIN cashaction_denominations ON cashaction.id = cashaction_denominations.cashaction_id
                                LEFT JOIN denominations ON cashaction_denominations.denominations_id = denominations.id
                                LEFT JOIN actiontype ON actiontype.id = cashaction_denominations.AType_id";

            string condition = " where 1 ";


            if (CashAction_ID != -1)
            {
                condition += " and cashaction.ID = @CashAction_ID;";
            }
            query += condition;

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@CashAction_ID", CashAction_ID);
                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataSet Select_Sums(string Till_IDs, int Account_ID, string ActionDate)
        {
            //check user from database
            string sub_condition = " WHERE action.Till_ID in (" + Till_IDs + ") ";
            if (ActionDate != "")
            {
                sub_condition += " and action.ActionDate = @ActionDate ";
            }

            string query = @"SELECT  
                               (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id LEFT JOIN till ON action.Till_ID = till.ID
                                LEFT JOIN tilltypes tilltypes ON tilltypes.ID = till.Type_ID LEFT JOIN tilltypes currency_type ON currency_type.ID = action.currency_tilltype_id"
                                + sub_condition + " and actiontype.shortname like 'I' AND NOT (tilltypes.Name = 'SYR' AND IFNULL(currency_type.Name, '') = 'USD')) as 'SumI' " +
                            ", (SELECT IFNULL(sum(action.Amount), 0) FROM action LEFT JOIN actiontype ON action.atype_id = actiontype.id LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN tilltypes tilltypes ON tilltypes.ID = till.Type_ID LEFT JOIN tilltypes currency_type ON currency_type.ID = action.currency_tilltype_id" + sub_condition + @" AND actiontype.shortname = 'I'AND tilltypes.Name = 'SYR'AND currency_type.Name = 'USD') AS 'SumIU'"+
                            ", (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'O') as 'SumO' " +
                            ", (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'R') as 'SumR' " +
                            ", (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'Ex') as 'SumEx' " +
                            ", (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id LEFT JOIN till ON action.Till_ID = till.ID  LEFT JOIN tilltypes tilltypes ON tilltypes.ID = till.Type_ID LEFT JOIN tilltypes currency_type ON currency_type.ID = action.currency_tilltype_id" + sub_condition + " and actiontype.shortname like 'E'  AND NOT (tilltypes.Name = 'SYR' AND IFNULL(currency_type.Name, '') = 'USD')) as 'SumE' " +
                            ", (SELECT IFNULL(sum(action.Amount), 0) FROM action LEFT JOIN actiontype ON action.atype_id = actiontype.id LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN tilltypes tilltypes ON tilltypes.ID = till.Type_ID LEFT JOIN tilltypes currency_type ON currency_type.ID = action.currency_tilltype_id " + sub_condition + @" AND actiontype.shortname = 'E' AND tilltypes.Name = 'SYR' AND currency_type.Name = 'USD' ) AS 'SumEU'"+

                            ",( (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'I')" +
                            " - (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'O')" +
                            " - (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'Ex')" +
                            " - (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'E')" +
                            " + (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'R') ) as 'CashNet'" +

                            ",( (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'I')" +
                            " - (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'E')" +
                            " - (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'Ex')" +
                            " - (SELECT IFNULL(sum(`Amount`),0) FROM action left join actiontype on action.atype_id = actiontype.id" + sub_condition + " and actiontype.shortname like 'O')) as 'Net'" +

                            ",( (SELECT IFNULL(sum(action.Amount),0) FROM action LEFT JOIN actiontype ON action.atype_id = actiontype.id LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN tilltypes tilltypes ON tilltypes.ID = till.Type_ID LEFT JOIN tilltypes currency_type ON currency_type.ID = action.currency_tilltype_id " + sub_condition + @" AND actiontype.shortname = 'I' AND tilltypes.Name = 'SYR' AND currency_type.Name = 'USD') - (SELECT IFNULL(sum(action.Amount),0) FROM action LEFT JOIN actiontype ON action.atype_id = actiontype.id LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN tilltypes tilltypes ON tilltypes.ID = till.Type_ID LEFT JOIN tilltypes currency_type ON currency_type.ID = action.currency_tilltype_id " + sub_condition + " AND actiontype.shortname = 'E' AND tilltypes.Name = 'SYR' AND currency_type.Name = 'USD') ) AS 'CashNetUSD'" +

                            ",( (SELECT IFNULL(sum(action.Amount),0) FROM action LEFT JOIN actiontype ON action.atype_id = actiontype.id LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN tilltypes tilltypes ON tilltypes.ID = till.Type_ID LEFT JOIN tilltypes currency_type ON currency_type.ID = action.currency_tilltype_id " + sub_condition + @" AND actiontype.shortname = 'I' AND tilltypes.Name = 'SYR' AND currency_type.Name = 'USD') - (SELECT IFNULL(sum(action.Amount),0) FROM action LEFT JOIN actiontype ON action.atype_id = actiontype.id LEFT JOIN till ON action.Till_ID = till.ID LEFT JOIN tilltypes tilltypes ON tilltypes.ID = till.Type_ID LEFT JOIN tilltypes currency_type ON currency_type.ID = action.currency_tilltype_id " + sub_condition + " AND actiontype.shortname = 'E' AND tilltypes.Name = 'SYR' AND currency_type.Name = 'USD') ) AS 'NetUSD'" +


                            " FROM till ";

            string condition = " where 1 ";
            if (Till_IDs != "" && Till_IDs != "-1")
            {
                condition += " and till.ID in (" + Till_IDs + ")";
            }
            query += condition;
            query += ";";

            query += "SELECT cash_balances.CashAccount_id as 'CashAccount_id', cash_balances.CashAccount_Name AS 'Account', cash_balances.Account_I AS 'Income'," +
                " cash_balances.Account_E AS 'Expenses', cash_balances.Account_Net AS 'Deference'" +
                " FROM cash_balances WHERE cash_balances.Account_id = @Account_ID;";

            DataSet ds = new DataSet();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                sc.Parameters.AddWithValue("@ActionDate", ActionDate);

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.CommandTimeout = 21600;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(ds);
                Login.CloseAndClearConnection();
            }
            return ds;
        }

        public DataTable SelectLines_Report()
        {
            //هنا جلب الأسماء مجمعة من المسارين دون تكرار
            //Group_Concat(donor.Name) as `Donor`,

            string query = @"SELECT  action.id AS `ID`,
                        city.Name AS `City_Name`,
                        project.Name AS `Project_Name`,
                        subproject.Name AS `Subproject_Name`,
                        account.Name as `Account_Name`,
                        action.MicroProject_ID,
                        CONCAT(b.FirstName,' ',b.FatherName,' ',b.LastName) as 'Beneficiary', 
                        budget.Name as `Budget`,

                       GROUP_CONCAT(DISTINCT COALESCE(`d_budget`.`Name`, `d_account`.`Name`) SEPARATOR ', ') AS `Donor`,

                        linecategory.Name as `Line Type`, 
                        line.Name as `Line`,
                        action.ActionDate,
                        DATE_FORMAT(action.ActionDate, ""%c/%Y"") as 'ActionMonth',
                        DATE_FORMAT(action.ActionDate, ""%Y"") as 'ActionYear',
                        Currency_Tilltype.Name as 'Currency',
                        (CASE WHEN actiontype.shortname = 'Ex' THEN action.Amount ELSE 0 END) AS 'Exchange',
                        (CASE WHEN actiontype.shortname = 'E' and (LOWER(Currency_Tilltype.Name) = LOWER('Syr') OR Currency_Tilltype.Name IS NULL) THEN action.Amount ELSE 0 END) AS 'ExpenseSYR',
                        (CASE WHEN actiontype.shortname = 'E' and LOWER(Currency_Tilltype.Name) = LOWER('Usd') THEN action.Amount ELSE 0 END) AS 'ExpenseUSD',
                        action.Note,
                        action.E_Note,
                        (SELECT CASE WHEN employee.FatherName = '' THEN CONCAT(employee.FirstName,' ',employee.LastName) ELSE
                        CONCAT(employee.FirstName,' ',employee.FatherName,' ',employee.LastName) END FROM employee_salary LEFT JOIN employee_subproject ON employee_subproject.id = employee_salary.employee_subproject_id LEFT JOIN employee ON employee.id = employee_subproject.employee_id WHERE employee_salary.id = action.Employee_Salary_ID) AS 'Employee_Name',
                        (case action.IsReceipt when 0 then 'receipt' when 1 then 'invoice' when 2 then 'payslip' else '' end) as `IsReceipt`,
                        action.ReceiptNote ,
                        action.`rate` as 'Rate',
                        (Case when actiontype.shortname = 'Ex' then (action.Amount * action.rate) else 0 END) as `Exchanged`,
                        all_avgrates.Name as `AvgRateName`,
                        all_avgrates.value as `AvgRateValue`, 
                        CASE 
                       WHEN actiontype.shortname = 'E' 
                       AND (LOWER(Currency_Tilltype.Name) = LOWER('Syr') OR Currency_Tilltype.Name IS NULL)
                       THEN action.Amount / NULLIF(all_avgrates.value,0)

                       WHEN actiontype.shortname = 'E' 
                       AND LOWER(Currency_Tilltype.Name) = LOWER('Usd') 
                       THEN action.Amount

    ELSE NULL
END AS `USD_Value`,
                        cashaccount.Name as `CashAccount`,
                        (select tilltypes.Name from till left join tilltypes on tilltypes.ID = till.Type_ID where till.id = action.ObserveTill_ID) as `ObserveTill`,
                        (select account.name from account left join till on till.account_id = account.id where till.id = action.ObserveTill_ID) as `ObserveAccount`,
                        obs_cashaccount.Name as `ObserveCashAccount`,
                        action.created_at, action.updated_at, COUNT(DISTINCT att.id) AS Attachments_count


                        FROM action 
                        LEFT JOIN actiontype on actiontype.id = action.atype_id
                        LEFT JOIN budget_line on action.BudgetLine_ID = budget_line.id
                        LEFT JOIN line on line.id = budget_line.line_id
                        LEFT JOIN budget on budget.id = budget_line.budget_id
                        LEFT JOIN budget_donor on budget_donor.budget_id = budget.id

                        LEFT JOIN donor `d_budget` ON budget_donor.donor_id = `d_budget`.id

                        LEFT JOIN linecategory on linecategory.id = line.Category_ID
                        LEFT JOIN cashaccount on cashaccount.id = action.CashAccount_ID
                        LEFT JOIN cashaccount obs_cashaccount on obs_cashaccount.id = action.ObserveCashAccount_ID
                        LEFT JOIN all_avgrates on action.Rate_ID = all_avgrates.id
                        LEFT join till on action.Till_ID = till.ID 
                        LEFT join tilltypes on tilltypes.ID = till.Type_ID
                        LEFT join tilltypes Currency_Tilltype on action.currency_tilltype_id = Currency_Tilltype.ID
                        LEFT join account on account.ID = till.Account_ID
                        
                        LEFT JOIN account_donor ON account.id = account_donor.Account_ID
                        LEFT JOIN donor `d_account` ON account_donor.Donor_ID = `d_account`.id

                        LEFT JOIN subproject ON account.SubProject_ID = subproject.ID
                        LEFT JOIN project ON subproject.Project_ID = project.ID
                        LEFT JOIN category ON project.Category_ID = category.ID
                        LEFT JOIN city ON subproject.City_ID = city.ID
                        LEFT JOIN action_attachments att ON att.action_id = action.id
                        LEFT JOIN beneficiary b ON b.MicroProject_ID = action.MicroProject_ID


                        where actiontype.shortname IN ('E','Ex') AND LOWER(tilltypes.Name) = LOWER('Syr') AND category.Name in('Budget','Micro Projects')
                        Group by action.id;";
            // LEFT JOIN donor on donor.id = budget_donor.donor_id

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public DataTable Select_Revenue_Exchange_Report(int TillType_ID, string TillType_Name)
        {
            string query = @"SELECT 
                        city.Name AS `City_Name`,
                        project.Name AS `Project_Name`,
                        subproject.Name AS `Subproject_Name`,
                        account.Name as `Account_Name`,
                        action.ActionDate, DATE_FORMAT(action.ActionDate, ""%c/%Y"") as 'ActionMonth', DATE_FORMAT(action.ActionDate, ""%Y"") as 'ActionYear',
                        (CASE WHEN (Currency_Tilltype.Name = 'SYR' OR Currency_Tilltype.Name IS NULL OR Currency_Tilltype.Name = '')
                        AND actiontype.shortname = 'I' THEN action.Amount ELSE 0 END) AS 'Income', 
                        (CASE WHEN tilltypes.Name = 'SYR' AND Currency_Tilltype.Name = 'USD' AND actiontype.shortname = 'I' THEN action.Amount
                        ELSE 0 END) AS 'Incomeusd',
                        (CASE WHEN actiontype.shortname = 'O'  THEN action.Amount ELSE 0 END) AS 'Outcome',
                        (CASE WHEN actiontype.shortname = 'Ex' THEN action.Amount ELSE 0 END) AS 'Exchange',
                        (CASE WHEN actiontype.shortname = 'E' THEN action.Amount ELSE 0 END) AS 'Expand',
                        ((CASE WHEN actiontype.shortname = 'I' THEN action.Amount ELSE 0 END) - 
                         (CASE WHEN actiontype.shortname = 'Ex' THEN action.Amount ELSE 0 END) - 
                         (CASE WHEN actiontype.shortname = 'E' THEN action.Amount ELSE 0 END) - 
                         (CASE WHEN actiontype.shortname = 'O' THEN action.Amount ELSE 0 END)) AS 'Balance',
                        action.Note,
                        action.E_Note,
                        action.`rate` as 'Rate',
                        (Case when actiontype.shortname = 'Ex' then (action.Amount * action.rate) else 0 END) as `Exchanged`,
                        all_avgrates.Name as `AvgRateName`,
                        all_avgrates.value as `AvgRateValue`,
                        (action.Amount/ all_avgrates.value) as `USD_Value`,
                        (select tilltypes.Name from till left join tilltypes on tilltypes.ID = till.Type_ID where till.id = action.ObserveTill_ID) as `ObserveTill`,
                        (select account.name from account left join till on till.account_id = account.id where till.id = action.ObserveTill_ID) as `ObserveAccount`,
                        action.ID, action.`ObserveAcion_ID`, 
                        (SELECT account.Name FROM action oaction LEFT JOIN till ON oaction.Till_ID = till.ID LEFT JOIN account ON till.Account_ID = account.ID LEFT JOIN accounttype oaccounttype on oaccounttype.id = account.Accounttype_id WHERE oaccounttype.name = 'Exchange' and accounttype.name <> 'Exchange'  AND oaction.ObserveAcion_ID = action.`ID` GROUP BY account.ID) AS 'ExchangeAcc_Name',
                        GROUP_CONCAT(DISTINCT COALESCE(`d_action`.`Name`, `d_budget`.`Name`, `d_account`.`Name`) SEPARATOR ', ') AS `Donor`,
                        action.created_at, action.updated_at, action.approved_at

                        FROM action LEFT JOIN actiontype on action.AType_ID = actiontype.id
                        LEFT JOIN all_avgrates on action.Rate_ID = all_avgrates.id
                        LEFT join till on action.Till_ID = till.ID 
                        LEFT join tilltypes on tilltypes.ID = till.Type_ID
                        LEFT join tilltypes Currency_Tilltype on Currency_Tilltype.ID = action.currency_tilltype_id
                        LEFT JOIN donor `d_action` ON action.Donor_ID = `d_action`.id
                        LEFT JOIN budget_line on action.BudgetLine_ID = budget_line.id
                        LEFT JOIN budget on budget.id = budget_line.budget_id
                        LEFT JOIN budget_donor on budget_donor.budget_id = budget.id
                        LEFT JOIN donor `d_budget` ON budget_donor.donor_id = `d_budget`.id
                        LEFT join account on account.ID = till.Account_ID
                        LEFT JOIN accounttype on account.Accounttype_id = accounttype.id
                        LEFT JOIN `account_donor` ON account_donor.account_id = account.id
                        LEFT JOIN donor `d_account` ON account_donor.donor_id = `d_account`.id
                        LEFT JOIN subproject ON account.SubProject_ID = subproject.ID
                        LEFT JOIN project ON subproject.Project_ID = project.ID
                        LEFT JOIN category ON project.Category_ID = category.ID
                        LEFT JOIN city ON subproject.City_ID = city.ID";

            if (TillType_Name.ToLower() == "syr")
            {
                query += " where actiontype.shortname IN ('I') AND LOWER(accounttype.name) = 'project' AND tilltypes.ID = @TillType_ID";
            }
            else
            {
                query += " where actiontype.shortname IN ('I','Ex','E','O') AND LOWER(accounttype.name) = 'project' AND tilltypes.ID = @TillType_ID";
            }

            query += " Group by action.id ORDER BY city.id, project.id, subproject.id, account.id, action.id;";

            DataTable dt = new DataTable();
            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@TillType_ID", TillType_ID);

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;

                MySqlDataAdapter da = new MySqlDataAdapter(sc);
                da.Fill(dt);
                Login.CloseAndClearConnection();
            }
            return dt;
        }

        public int Insert(int Till_ID, string Till_Type, int ObserveTill_ID, string ObserveTill_Type, int Donor_ID, int ExchangeAcc_ID, bool WithObserveAcion_ID, bool WithExAcc, bool WithExchangeRate, string AType, Double Amount, Double rate, string ActionDate
            , string Note, string E_Note, int IsReceipt, string ReceiptNote, int Employee_Salary_ID, int Rate_ID
            , int MicroProject_ID, int BudgetLine_ID, int IncomeType_ID, int CashAccount_ID, int ObserveCashAccount_ID, int Exchange_Cash_ID, int Account_ID, string Form_Name , int Currency_tilltype_id)
        {
            string query = "";
            string log_query = "";

            int Action_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Till_ID", Till_ID);
                sc.Parameters.AddWithValue("@Till_Type", Till_Type);
                if (ObserveTill_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ObserveTill_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ObserveTill_ID", ObserveTill_ID);
                }
                sc.Parameters.AddWithValue("@ObserveTill_Type", ObserveTill_Type);

                if (Donor_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Donor_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Donor_ID", Donor_ID);
                }

                if (ExchangeAcc_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ExchangeAcc_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ExchangeAcc_ID", ExchangeAcc_ID);
                }

                if (AType == "M")
                    sc.Parameters.AddWithValue("@AType", "E");
                else
                    sc.Parameters.AddWithValue("@AType", AType);

                sc.Parameters.AddWithValue("@Amount", Amount);
                sc.Parameters.AddWithValue("@rate", rate);

                if (AType == "M")
                    sc.Parameters.AddWithValue("@ConvertedAmount", Amount);
                else
                    sc.Parameters.AddWithValue("@ConvertedAmount", Amount * rate);

                sc.Parameters.AddWithValue("@Note", Note);
                sc.Parameters.AddWithValue("@E_Note", E_Note);

                sc.Parameters.AddWithValue("@IsReceipt", IsReceipt);
                sc.Parameters.AddWithValue("@ReceiptNote", ReceiptNote);
                if (Employee_Salary_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Employee_Salary_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Employee_Salary_ID", Employee_Salary_ID);
                }
                if (Rate_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Rate_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Rate_ID", Rate_ID);
                }
                if (ActionDate == "")
                {
                    sc.Parameters.AddWithValue("@ActionDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ActionDate", ActionDate);
                }
                if (MicroProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", MicroProject_ID);
                }
                if (BudgetLine_ID == -1)
                {
                    sc.Parameters.AddWithValue("@BudgetLine_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@BudgetLine_ID", BudgetLine_ID);
                }
                if (IncomeType_ID == -1)
                {
                    sc.Parameters.AddWithValue("@IncomeType_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@IncomeType_ID", IncomeType_ID);
                }
                if (CashAccount_ID == -1)
                {
                    sc.Parameters.AddWithValue("@CashAccount_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);
                }
                if (ObserveCashAccount_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ObserveCashAccount_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ObserveCashAccount_ID", ObserveCashAccount_ID);
                }
                if (Exchange_Cash_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Exchange_Cash_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Exchange_Cash_ID", Exchange_Cash_ID);
                }
                if (Account_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Account_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Account_ID", Account_ID);

                }

                if (Currency_tilltype_id == -1)
                {
                    sc.Parameters.AddWithValue("@Currency_tilltype_id", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Currency_tilltype_id", Currency_tilltype_id);

                }

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);

                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"SET @Action_ID = (
                                SELECT MAX(max_id) + 1 AS next_id
                                FROM (
                                    SELECT MAX(ID) AS max_id FROM action
                                    UNION ALL
                                    SELECT MAX(ID) FROM action_not_approved
                                    UNION ALL
                                    SELECT MAX(ID) FROM action_deleted
                                ) AS combined_max_ids);";

                query += @" INSERT INTO `action_not_approved`(`id`,`Till_ID`,`ObserveTill_ID`, `AType_ID`, `currency_tilltype_id` ,`Amount`, `rate`, `ActionDate`
                            , `Note`, `E_Note`, `IsReceipt`, `Employee_Salary_ID`, `ReceiptNote`, `Rate_ID`
                            , `MicroProject_ID`, `BudgetLine_ID`, `IncomeType_ID`, `CashAccount_ID`, `ObserveCashAccount_ID`, `Exchange_Cash_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES (@Action_ID, @Till_ID ,@ObserveTill_ID, (select id from actiontype where shortname = @AType), @Currency_tilltype_id, @Amount, @rate, @ActionDate, @Note,@E_Note, @IsReceipt, @Employee_Salary_ID, @ReceiptNote, @Rate_ID, 
                            @MicroProject_ID, @BudgetLine_ID, @IncomeType_ID, @CashAccount_ID, @ObserveCashAccount_ID, @Exchange_Cash_ID, @Donor_ID, @Todaydate, @Uesr_ID, '1');";

                bool isUsdExpenseFromSyr = AType == "E" && Till_Type.ToLower() == "syr" && Currency_tilltype_id == 2 && !WithExAcc;

                if (WithObserveAcion_ID && !isUsdExpenseFromSyr)
                {
                    query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            , `Note`,`E_Note`, `IsReceipt`,`Employee_Salary_ID`, `ReceiptNote`, `CashAccount_ID`,`ObserveCashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES(@ObserveTill_ID, @Till_ID, @Action_ID, (select id from actiontype where shortname = 'I'), @ConvertedAmount,@rate, @ActionDate, @Note,@E_Note, @IsReceipt, @Employee_Salary_ID, @ReceiptNote,@ObserveCashAccount_ID, @CashAccount_ID, @Donor_ID, @Todaydate, @Uesr_ID, '0'); ";

                    query += " SET @ObserveAcion_ID = Last_Insert_ID();";

                    query += " UPDATE action_not_approved SET ObserveAcion_ID = @ObserveAcion_ID WHERE ID = @Action_ID;";

                }  

                    if (WithObserveAcion_ID && WithExAcc)
                    {
                        query += " Set @From_id = (select till.id from till left join account on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where account.id = @ExchangeAcc_ID and tilltypes.name = @Till_Type);";
                        query += " Set @To_id = (select till.id from till left join account on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where account.id = @ExchangeAcc_ID and tilltypes.name = @ObserveTill_Type);";

                        query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            , `Note`,`E_Note`, `IsReceipt`,`Employee_Salary_ID`, `ReceiptNote`, `CashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES(@From_id, @Till_ID, @Action_ID, (select id from actiontype where shortname = 'I'), @Amount ,@rate, @ActionDate, @Note,@E_Note, @IsReceipt, @Employee_Salary_ID, @ReceiptNote, (case when LOWER(@Till_Type) = 'syr' then @Exchange_Cash_ID else null end), @Donor_ID, @Todaydate, @Uesr_ID, '0'); ";

                        query += " SET @FromExTill_Acc_ID = Last_Insert_ID();";

                        query += @" INSERT INTO `action_not_approved`(`Till_ID`,`ObserveTill_ID`, `ObserveAcion_ID`, `AType_ID`, `Amount`, `rate`, `ActionDate`
                            , `Note`,`E_Note`, `IsReceipt`,`Employee_Salary_ID`, `ReceiptNote`, `CashAccount_ID`, `Donor_ID`, `created_at`, `User_ID`, `SourceAction`)  
                            VALUES(@To_id, @Till_ID, @Action_ID, (select id from actiontype where shortname = 'E'), @ConvertedAmount ,@rate, @ActionDate, @Note, @E_Note, @IsReceipt, @Employee_Salary_ID, @ReceiptNote, (case when LOWER(@ObserveTill_Type) = 'syr' then @Exchange_Cash_ID else null end), @Donor_ID, @Todaydate, @Uesr_ID, '0'); ";

                        query += " SET @ToExTill_Acc_ID = Last_Insert_ID();";
                    }

                    else if (WithObserveAcion_ID && isUsdExpenseFromSyr)
                    {
                    //  Outcome من USD till
                    query += @" INSERT INTO action_not_approved (Till_ID, ObserveTill_ID, ObserveAcion_ID, AType_ID, currency_tilltype_id, Amount, rate, ActionDate, Note, E_Note, IsReceipt, Employee_Salary_ID, ReceiptNote, CashAccount_ID, Donor_ID, BudgetLine_ID, created_at, User_ID, SourceAction)
                               VALUES (@ObserveTill_ID, @Till_ID, @Action_ID, (select id from actiontype where shortname = 'O'), @Currency_tilltype_id, @Amount, @rate, @ActionDate, @Note, @E_Note, @IsReceipt, @Employee_Salary_ID, @ReceiptNote, @CashAccount_ID, @Donor_ID, @BudgetLine_ID, @Todaydate, @Uesr_ID, '0'); ";

                    query += " SET @UsdOutcome_ID = Last_Insert_ID(); ";

                    // Income على SYR till بعملة USD 
                    query += @" INSERT INTO action_not_approved (Till_ID, ObserveTill_ID, ObserveAcion_ID, AType_ID, currency_tilltype_id, Amount, rate, ActionDate, Note, E_Note, IsReceipt, Employee_Salary_ID, ReceiptNote, CashAccount_ID, Donor_ID, BudgetLine_ID, created_at, User_ID, SourceAction) 
                             VALUES (@Till_ID, @ObserveTill_ID, @Action_ID, (select id from actiontype where shortname = 'I'), @Currency_tilltype_id, @Amount, @rate, @ActionDate, @Note, @E_Note, @IsReceipt, @Employee_Salary_ID, @ReceiptNote, @CashAccount_ID, @Donor_ID, @BudgetLine_ID, @Todaydate, @Uesr_ID, '0');";

                    query += " SET @SyrIncome_ID = Last_Insert_ID(); ";
                }
              

                

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'action_not_approved', @Action_ID, CONCAT('action Type = ', @AType), CONCAT('till ID = ',@Till_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                if (WithObserveAcion_ID && !isUsdExpenseFromSyr)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'action_not_approved', @ObserveAcion_ID, 'action Type = I', CONCAT('till ID = ', @ObserveTill_ID ,' - Auto Created With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    if (WithExAcc)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'action_not_approved', @FromExTill_Acc_ID, 'action Type = I', CONCAT('till ID = ', @From_id ,' - Auto Created With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'action_not_approved', @ToExTill_Acc_ID, 'action Type = E', CONCAT('till ID = ', @To_id ,' - Auto Created With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }
                if (WithObserveAcion_ID && isUsdExpenseFromSyr)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                             " VALUES (@Form_Name, 'INSERT', 'action_not_approved', @UsdOutcome_ID, 'action Type = O', CONCAT('till ID = ', @ObserveTill_ID ,' - Linked to action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                             " VALUES (@Form_Name, 'INSERT', 'action_not_approved', @SyrIncome_ID, 'action Type = I', CONCAT('till ID = ', @Till_ID ,' - Linked to action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                }

                query += " SELECT @Action_ID;";
                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Action_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }
            return Action_ID;
        }

        public void Insert_Multi_Actions()
        {
            string query = "";


            query += "START TRANSACTION;";

            //query += Login.MultiAction_Query;

            query += string.Join(Environment.NewLine, Login.MultiAction_List.ToArray());
            query += " COMMIT;";


            if (Login.BuildConnection())
            {
                Login.MultiAction_sc.CommandTimeout = 120;
                Login.MultiAction_sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                Login.MultiAction_sc.Parameters.AddWithValue("@t", 600);
                Login.MultiAction_sc.Connection = Login.MyConn;

                Login.MultiAction_sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public int Insert_CashAction(int Acc_ID, string Acc_Name, string ActionDate, string Note, string E_Note, DataTable Add_Actions, string Form_Name)
        {
            string query = "";
            string log_query = "";

            int Action_ID = -1;

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Acc_ID", Acc_ID);
                sc.Parameters.AddWithValue("@Acc_Name", Acc_Name);
                sc.Parameters.AddWithValue("@Note", Note);
                sc.Parameters.AddWithValue("@E_Note", E_Note);
                if (ActionDate == "")
                {
                    sc.Parameters.AddWithValue("@ActionDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ActionDate", ActionDate);
                }

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);

                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += @"INSERT INTO cashaction (cashaccount_id, ActionDate, note,E_Note, created_at, User_ID)
                            VALUES (@Acc_ID,@ActionDate, @Note,@E_Note, @Todaydate, @Uesr_ID);";

                query += " SET @CashAction_ID = Last_Insert_ID();";

                if (Add_Actions.Rows.Count > 0)
                    for (int i = 0; i < Add_Actions.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@Amount" + i, Add_Actions.Rows[i]["Amount"]);
                        sc.Parameters.AddWithValue("@Denominations_id" + i, Add_Actions.Rows[i]["denominations_id"]);
                        sc.Parameters.AddWithValue("@Denominations_name" + i, Add_Actions.Rows[i]["Denominations_name"]);
                        sc.Parameters.AddWithValue("@Actiontype_shortname" + i, Add_Actions.Rows[i]["Actiontype_shortname"]);


                        query += @" INSERT INTO `cashaction_denominations`(`cashaction_id`, `denominations_id`, `AType_id`, `amount`) VALUES (@CashAction_ID, @Denominations_id" + i + ", (SELECT actiontype.id FROM actiontype WHERE actiontype.shortname = @Actiontype_shortname" + i + "), @Amount" + i + ");";
                        query += " SET @cashaction_denominations_id" + i + " = Last_Insert_ID();";

                    }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'Cash action', @CashAction_ID, CONCAT('account Name = ', @Acc_Name), '' , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";



                if (Add_Actions.Rows.Count > 0)
                    for (int i = 0; i < Add_Actions.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'cashaction_denominations', @cashaction_denominations_id" + i + " ,  CONCAT('Cash action ID = ',  @CashAction_ID), CONCAT('action Type = ',@Actiontype_shortname" + i + " ), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                    }

                query += " SELECT @CashAction_ID;";
                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                Action_ID = Convert.ToInt32(sc.ExecuteScalar());

                Login.CloseAndClearConnection();
            }
            return Action_ID;
        }

        public void Update(int Action_ID, string AType, int Till_ID, string Till_Type, int ObserveTill_ID, string ObserveTill_Type, int Donor_ID, int ExchangeAcc_ID, bool WithObserveAcion_ID, bool WithExAcc, bool WithExchangeRate, Double Amount, Double rate
            , string ActionDate, string Note, string E_Note, int IsReceipt, string ReceiptNote, int Employee_Salary_ID, int Rate_ID
            , int MicroProject_ID, int BudgetLine_ID, int IncomeType_ID, int CashAccount_ID, int ObserveCashAccount_ID, int Exchange_Cash_ID, int Account_ID, string Form_Name, int Currency_tilltype_id)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);

                if (AType == "M")
                    sc.Parameters.AddWithValue("@AType", "E");
                else
                    sc.Parameters.AddWithValue("@AType", AType);

                if (Till_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Till_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Till_ID", Till_ID);
                }
                sc.Parameters.AddWithValue("@Amount", Amount);
                sc.Parameters.AddWithValue("@rate", rate);

                if (AType == "M")
                    sc.Parameters.AddWithValue("@ConvertedAmount", Amount);
                else
                    sc.Parameters.AddWithValue("@ConvertedAmount", Amount * rate);

                sc.Parameters.AddWithValue("@Till_Type", Till_Type);

                if (ObserveTill_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ObserveTill_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ObserveTill_ID", ObserveTill_ID);
                }

                sc.Parameters.AddWithValue("@ObserveTill_Type", ObserveTill_Type);

                if (Donor_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Donor_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Donor_ID", Donor_ID);
                }

                if (ExchangeAcc_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ExchangeAcc_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ExchangeAcc_ID", ExchangeAcc_ID);
                }

                sc.Parameters.AddWithValue("@Note", Note);
                sc.Parameters.AddWithValue("@E_Note", E_Note);
                sc.Parameters.AddWithValue("@IsReceipt", IsReceipt);
                sc.Parameters.AddWithValue("@ReceiptNote", ReceiptNote);
                if (Employee_Salary_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Employee_Salary_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Employee_Salary_ID", Employee_Salary_ID);
                }
                if (Rate_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Rate_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Rate_ID", Rate_ID);
                }
                if (ActionDate == "")
                {
                    sc.Parameters.AddWithValue("@ActionDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ActionDate", ActionDate);
                }
                if (MicroProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", MicroProject_ID);
                }
                if (BudgetLine_ID == -1)
                {
                    sc.Parameters.AddWithValue("@BudgetLine_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@BudgetLine_ID", BudgetLine_ID);
                }
                if (IncomeType_ID == -1)
                {
                    sc.Parameters.AddWithValue("@IncomeType_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@IncomeType_ID", IncomeType_ID);
                }
                if (CashAccount_ID == -1)
                {
                    sc.Parameters.AddWithValue("@CashAccount_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);
                }
                if (ObserveCashAccount_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ObserveCashAccount_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ObserveCashAccount_ID", ObserveCashAccount_ID);
                }
                if (Exchange_Cash_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Exchange_Cash_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Exchange_Cash_ID", Exchange_Cash_ID);
                }
                if (Account_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Account_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                }

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);

                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "UPDATE `action` SET " +
                    " `Amount`= @Amount" +
                    " ,`rate`= @rate" +
                    " ,`ActionDate`= @ActionDate" +
                    " ,`Note`= @Note" +
                    " ,`E_Note`= @E_Note" +
                    " ,`IsReceipt` = @IsReceipt" +
                    " ,`ReceiptNote`= @ReceiptNote" +
                    " ,`Employee_Salary_ID` = @Employee_Salary_ID" +
                    " ,`Rate_ID`= @Rate_ID" +
                    " ,`MicroProject_ID`= @MicroProject_ID" +
                    " ,`BudgetLine_ID`= @BudgetLine_ID" +
                    " ,`IncomeType_ID`= @IncomeType_ID" +
                    " ,`CashAccount_ID`= @CashAccount_ID" +
                    " ,`ObserveCashAccount_ID`= @ObserveCashAccount_ID" +
                    " ,`Exchange_Cash_ID`= @Exchange_Cash_ID" +
                    " ,`Donor_ID`= @Donor_ID" +
                    " ,`updated_at` = @Todaydate, `edited_by` = @Uesr_ID  WHERE ID = @Action_ID;";

                bool isUsdExpenseFromSyr = AType == "E" && Till_Type.ToLower() == "syr" && Currency_tilltype_id == 2 && !WithExAcc;

                if (WithObserveAcion_ID && !isUsdExpenseFromSyr)
                {
                    query += @" SET @ObserveAcion_ID = (SELECT ObserveAcion_ID FROM action WHERE action.ID = @Action_ID);";

                    if (WithExAcc)
                    {
                        query += " Set @FromExtill_id = (select till.id from till left join account on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where account.id = @ExchangeAcc_ID and tilltypes.name = @Till_Type);";
                        query += " SET @FromExTill_Acc_ID = (select action.id from action where action.ObserveAcion_ID = @Action_id and action.till_id = @FromExtill_id);";
                        query += " Set @ToExTill_id = (select till.id from till left join account on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where account.id = @ExchangeAcc_ID and tilltypes.name = @ObserveTill_Type);";
                        query += " SET @ToExTill_Acc_ID = (select action.id from action where action.ObserveAcion_ID = @Action_id and action.till_id = @ToExTill_id);";
                    }

                    query += @" UPDATE action SET 
                                Amount= @ConvertedAmount, rate = @rate, ActionDate= @ActionDate, Note= @Note, E_Note= @E_Note, CashAccount_ID = @ObserveCashAccount_ID, ObserveCashAccount_ID = @CashAccount_ID,Donor_ID = @Donor_ID,
                                updated_at= @Todaydate, `edited_by` = @Uesr_ID  WHERE action.ID = @ObserveAcion_ID;";


                    if (WithExAcc)
                    {
                        query += @" UPDATE action SET 
                                Amount= @Amount,rate = @rate, ActionDate= @ActionDate, Note= @Note, E_Note= @E_Note, Donor_ID = @Donor_ID, `CashAccount_ID`= (case when LOWER(@Till_Type) = 'syr' then @Exchange_Cash_ID else null end),
                                updated_at= @Todaydate, `edited_by` = @Uesr_ID  WHERE action.ID = @FromExTill_Acc_ID;";

                        query += @" UPDATE action SET 
                                Amount= @ConvertedAmount,rate = @rate, ActionDate= @ActionDate, Note= @Note, E_Note= @E_Note, Donor_ID = @Donor_ID, `CashAccount_ID`= (case when LOWER(@ObserveTill_Type) = 'syr' then @Exchange_Cash_ID else null end),
                                updated_at= @Todaydate, `edited_by` = @Uesr_ID  WHERE action.ID = @ToExTill_Acc_ID;";
                    }
                }

                if (WithObserveAcion_ID && isUsdExpenseFromSyr)
                {
                    query += @" UPDATE action SET Amount = @Amount, rate = @rate, ActionDate = @ActionDate, Note = @Note ,  E_Note = @E_Note,`IsReceipt` = @IsReceipt ,`ReceiptNote`= @ReceiptNote , CashAccount_ID = @ObserveCashAccount_ID, ObserveCashAccount_ID = @CashAccount_ID
                             , Donor_ID = @Donor_ID, updated_at = @Todaydate,`BudgetLine_ID`= @BudgetLine_ID , edited_by = @Uesr_ID  WHERE action.ObserveAcion_ID = @Action_ID;";
                }


                query += "UPDATE `action` SET " +
                        " `IsApproved`= 0" +
                        " ,`Approved_at`= null" +
                        " ,`Approved_by`= null" +
                        " WHERE ID = @Action_ID;";

                query += "UPDATE `action` SET " +
                        " `IsApproved`= 0" +
                        " ,`Approved_at`= null" +
                        " ,`Approved_by`= null" +
                       " WHERE ObserveAcion_ID = @Action_ID;";


                query += "INSERT INTO action_not_approved SELECT * FROM action WHERE action.id = @Action_ID OR action.ObserveAcion_ID = @Action_ID;";

                query += "DELETE FROM action WHERE action.ObserveAcion_ID = @Action_ID;";

                query += "DELETE FROM action WHERE action.id  = @Action_ID;";

                /*
                if (WithExchangeRate)
                {
                    query += " insert IGNORE into exchangeaccount_rate (Account_ID, ratedate, rate, FromTill_ID, ToTill_ID) values (@Account_ID, @ActionDate, @rate,(select tilltypes.id from tilltypes where tilltypes.name = @Till_Type),(select tilltypes.id from tilltypes where tilltypes.name = @ObserveTill_Type));";
                }
                */
                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action', @Action_ID, CONCAT('action Type = ', @AType), CONCAT('till ID = ',@Till_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (WithObserveAcion_ID && !isUsdExpenseFromSyr)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action', @ObserveAcion_ID, 'action Type = I', CONCAT('till ID = ', @ObserveTill_ID ,' - Auto updated With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    if (WithExAcc)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action', @FromExTill_Acc_ID, 'action Type = I', CONCAT('till ID = ', @ObserveTill_ID ,' - Auto updated With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action', @ToExTill_Acc_ID, 'action Type = E', CONCAT('till ID = ', @ObserveTill_ID ,' - Auto updated With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }
                if (WithObserveAcion_ID && isUsdExpenseFromSyr)
                {
                    query += "INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                             "SELECT @Form_Name, 'UPDATE', 'action', ID, 'action Type = O', CONCAT('till ID = ', IFNULL(Till_ID,0),' - linked to Action ID = ', ObserveAcion_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "' " +
                             "FROM action_not_approved WHERE ObserveAcion_ID = @Action_ID AND AType_ID = 2;";

                    query += "INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                             "SELECT @Form_Name, 'UPDATE', 'action', ID, 'action Type = I', CONCAT('till ID = ', IFNULL(Till_ID,0),' - linked to Action ID = ', ObserveAcion_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "' " +
                             "FROM action_not_approved WHERE ObserveAcion_ID = @Action_ID AND AType_ID = 1;";
                }
            

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }


        public void Update_Not_Approved(int Action_ID, string AType, int Till_ID, string Till_Type, int ObserveTill_ID, string ObserveTill_Type, int Donor_ID, int ExchangeAcc_ID, bool WithObserveAcion_ID, bool WithExAcc, bool WithExchangeRate, Double Amount, Double rate
    , string ActionDate, string Note, string E_Note, int IsReceipt, string ReceiptNote, int Employee_Salary_ID, int Rate_ID
    , int MicroProject_ID, int BudgetLine_ID, int IncomeType_ID, int CashAccount_ID, int ObserveCashAccount_ID, int Exchange_Cash_ID, int Account_ID, string Form_Name, int Currency_tilltype_id)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);

                if (AType == "M")
                    sc.Parameters.AddWithValue("@AType", "E");
                else
                    sc.Parameters.AddWithValue("@AType", AType);

                if (Till_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Till_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Till_ID", Till_ID);
                }
                sc.Parameters.AddWithValue("@Amount", Amount);
                sc.Parameters.AddWithValue("@rate", rate);

                if (AType == "M")
                    sc.Parameters.AddWithValue("@ConvertedAmount", Amount);
                else
                    sc.Parameters.AddWithValue("@ConvertedAmount", Amount * rate);

                sc.Parameters.AddWithValue("@Till_Type", Till_Type);

                if (ObserveTill_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ObserveTill_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ObserveTill_ID", ObserveTill_ID);
                }

                sc.Parameters.AddWithValue("@ObserveTill_Type", ObserveTill_Type);

                if (Donor_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Donor_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Donor_ID", Donor_ID);
                }

                if (ExchangeAcc_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ExchangeAcc_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ExchangeAcc_ID", ExchangeAcc_ID);
                }

                sc.Parameters.AddWithValue("@Note", Note);
                sc.Parameters.AddWithValue("@E_Note", E_Note);
                sc.Parameters.AddWithValue("@IsReceipt", IsReceipt);
                sc.Parameters.AddWithValue("@ReceiptNote", ReceiptNote);
                if (Employee_Salary_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Employee_Salary_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Employee_Salary_ID", Employee_Salary_ID);
                }
                if (Rate_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Rate_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Rate_ID", Rate_ID);
                }
                if (ActionDate == "")
                {
                    sc.Parameters.AddWithValue("@ActionDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ActionDate", ActionDate);
                }
                if (MicroProject_ID == -1)
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@MicroProject_ID", MicroProject_ID);
                }
                if (BudgetLine_ID == -1)
                {
                    sc.Parameters.AddWithValue("@BudgetLine_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@BudgetLine_ID", BudgetLine_ID);
                }
                if (IncomeType_ID == -1)
                {
                    sc.Parameters.AddWithValue("@IncomeType_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@IncomeType_ID", IncomeType_ID);
                }
                if (CashAccount_ID == -1)
                {
                    sc.Parameters.AddWithValue("@CashAccount_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@CashAccount_ID", CashAccount_ID);
                }
                if (ObserveCashAccount_ID == -1)
                {
                    sc.Parameters.AddWithValue("@ObserveCashAccount_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ObserveCashAccount_ID", ObserveCashAccount_ID);
                }
                if (Exchange_Cash_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Exchange_Cash_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Exchange_Cash_ID", Exchange_Cash_ID);
                }
                if (Account_ID == -1)
                {
                    sc.Parameters.AddWithValue("@Account_ID", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@Account_ID", Account_ID);
                }

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);

                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "UPDATE `action_not_approved` SET " +
                    " `Amount`= @Amount" +
                    " ,`rate`= @rate" +
                    " ,`ActionDate`= @ActionDate" +
                    " ,`Note`= @Note" +
                    " ,`E_Note`= @E_Note" +
                    " ,`IsReceipt` = @IsReceipt" +
                    " ,`ReceiptNote`= @ReceiptNote" +
                    " ,`Employee_Salary_ID` = @Employee_Salary_ID" +
                    " ,`Rate_ID`= @Rate_ID" +
                    " ,`MicroProject_ID`= @MicroProject_ID" +
                    " ,`BudgetLine_ID`= @BudgetLine_ID" +
                    " ,`IncomeType_ID`= @IncomeType_ID" +
                    " ,`CashAccount_ID`= @CashAccount_ID" +
                    " ,`ObserveCashAccount_ID`= @ObserveCashAccount_ID" +
                    " ,`Exchange_Cash_ID`= @Exchange_Cash_ID" +
                    " ,`Donor_ID`= @Donor_ID" +
                    " ,`updated_at` = @Todaydate, `edited_by` = @Uesr_ID  WHERE ID = @Action_ID;";

                bool isUsdExpenseFromSyr = AType == "E" && Till_Type.ToLower() == "syr" && Currency_tilltype_id == 2 && !WithExAcc;

                if (WithObserveAcion_ID && !isUsdExpenseFromSyr)
                {
                    query += @" SET @ObserveAcion_ID = (SELECT ObserveAcion_ID FROM action_not_approved WHERE action_not_approved.ID = @Action_ID);";

                    if (WithExAcc)
                    {
                        query += " Set @FromExtill_id = (select till.id from till left join account on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where account.id = @ExchangeAcc_ID and tilltypes.name = @Till_Type);";
                        query += " SET @FromExTill_Acc_ID = (select action_not_approved.id from action_not_approved where action_not_approved.ObserveAcion_ID = @Action_id and action_not_approved.till_id = @FromExtill_id);";
                        query += " Set @ToExTill_id = (select till.id from till left join account on till.account_id = account.id left join tilltypes on tilltypes.id = till.type_id where account.id = @ExchangeAcc_ID and tilltypes.name = @ObserveTill_Type);";
                        query += " SET @ToExTill_Acc_ID = (select action_not_approved.id from action_not_approved where action_not_approved.ObserveAcion_ID = @Action_id and action_not_approved.till_id = @ToExTill_id);";
                    }

                    query += @" UPDATE action_not_approved SET 
                                Amount= @ConvertedAmount, rate = @rate, ActionDate= @ActionDate, Note= @Note, E_Note= @E_Note, CashAccount_ID = @ObserveCashAccount_ID, ObserveCashAccount_ID = @CashAccount_ID,Donor_ID = @Donor_ID,
                                updated_at= @Todaydate, `edited_by` = @Uesr_ID  WHERE action_not_approved.ID = @ObserveAcion_ID;";


                    if (WithExAcc)
                    {
                        query += @" UPDATE action_not_approved SET 
                                Amount= @Amount,rate = @rate, ActionDate= @ActionDate, Note= @Note, E_Note= @E_Note, Donor_ID = @Donor_ID, `CashAccount_ID`= (case when LOWER(@Till_Type) = 'syr' then @Exchange_Cash_ID else null end),
                                updated_at= @Todaydate, `edited_by` = @Uesr_ID  WHERE action_not_approved.ID = @FromExTill_Acc_ID;";

                        query += @" UPDATE action_not_approved SET 
                                Amount= @ConvertedAmount,rate = @rate, ActionDate= @ActionDate, Note= @Note, E_Note= @E_Note, Donor_ID = @Donor_ID, `CashAccount_ID`= (case when LOWER(@ObserveTill_Type) = 'syr' then @Exchange_Cash_ID else null end),
                                updated_at= @Todaydate, `edited_by` = @Uesr_ID  WHERE action_not_approved.ID = @ToExTill_Acc_ID;";
                    }
                }

                if (WithObserveAcion_ID && isUsdExpenseFromSyr)
                {
                    query += @" UPDATE action_not_approved SET Amount = @Amount, rate = @rate, ActionDate = @ActionDate, Note = @Note ,  E_Note = @E_Note,`IsReceipt` = @IsReceipt ,`ReceiptNote`= @ReceiptNote, CashAccount_ID = @ObserveCashAccount_ID, ObserveCashAccount_ID = @CashAccount_ID
                               , Donor_ID = @Donor_ID, updated_at = @Todaydate, `BudgetLine_ID`= @BudgetLine_ID , edited_by = @Uesr_ID  WHERE action_not_approved.ObserveAcion_ID = @Action_ID;";
                }

                query += "UPDATE `action_not_approved` SET " +
                        " `IsApproved`= 0" +
                        " ,`Approved_at`= null" +
                        " ,`Approved_by`= null" +
                        " WHERE ID = @Action_ID;";

                query += "UPDATE `action_not_approved` SET " +
                        " `IsApproved`= 0" +
                        " ,`Approved_at`= null" +
                        " ,`Approved_by`= null" +
                       " WHERE ObserveAcion_ID = @Action_ID;";


                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action_not_approved', @Action_ID, CONCAT('action Type = ', @AType), CONCAT('till ID = ',@Till_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (WithObserveAcion_ID && !isUsdExpenseFromSyr)
                {
                    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action_not_approved', @ObserveAcion_ID, 'action Type = I', CONCAT('till ID = ', @ObserveTill_ID ,' - Auto updated With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    if (WithExAcc)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action_not_approved', @FromExTill_Acc_ID, 'action Type = I', CONCAT('till ID = ', @ObserveTill_ID ,' - Auto updated With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action_not_approved', @ToExTill_Acc_ID, 'action Type = E', CONCAT('till ID = ', @ObserveTill_ID ,' - Auto updated With action ID = ',@Action_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }
                }

                if (WithObserveAcion_ID && isUsdExpenseFromSyr)
                {
                    query += "INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                             "SELECT @Form_Name, 'UPDATE', 'action_not_approved', ID, 'action Type = O',  CONCAT('till ID = ', IFNULL(Till_ID,0),' - linked to Action ID = ', ObserveAcion_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "' " +
                             "FROM action_not_approved WHERE ObserveAcion_ID = @Action_ID AND AType_ID = 2;";

                    query += "INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) " +
                             "SELECT @Form_Name, 'UPDATE', 'action_not_approved', ID, 'action Type = I',  CONCAT('till ID = ', IFNULL(Till_ID,0),' - linked to Action ID = ', ObserveAcion_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "' " +
                             "FROM action_not_approved WHERE ObserveAcion_ID = @Action_ID AND AType_ID = 1;";
                }

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }


        public void Update_CashAction(int Action_ID, string Acc_Name, string ActionDate, string Note, string E_Note, DataTable Add_Actions, DataTable Delete_Actions, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Acc_Name", Acc_Name);
                sc.Parameters.AddWithValue("@Note", Note);
                sc.Parameters.AddWithValue("@E_Note", E_Note);

                if (ActionDate == "")
                {
                    sc.Parameters.AddWithValue("@ActionDate", DBNull.Value);
                }
                else
                {
                    sc.Parameters.AddWithValue("@ActionDate", ActionDate);
                }


                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                query = "START TRANSACTION;";

                query += "UPDATE `cashaction` SET " +
                    " `Note`= @Note" +
                    " ,`E_Note`= @E_Note" +
                    " ,`ActionDate` = @ActionDate" +
                    " ,`updated_at` = @Todaydate, edited_by = @Uesr_ID WHERE ID = @Action_ID;";


                if (Delete_Actions.Rows.Count > 0)
                    for (int i = 0; i < Delete_Actions.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@deletecashaction_denominations_id" + i, Delete_Actions.Rows[i]["cashaction_denominations_id"]);
                        query += @" DELETE FROM cashaction_denominations where id = @deletecashaction_denominations_id" + i + ";";
                    }


                if (Add_Actions.Rows.Count > 0)
                    for (int i = 0; i < Add_Actions.Rows.Count; i++)
                    {
                        sc.Parameters.AddWithValue("@Amount" + i, Add_Actions.Rows[i]["Amount"]);
                        sc.Parameters.AddWithValue("@Denominations_id" + i, Add_Actions.Rows[i]["denominations_id"]);
                        sc.Parameters.AddWithValue("@Denominations_name" + i, Add_Actions.Rows[i]["denominations_name"]);
                        sc.Parameters.AddWithValue("@Actiontype_shortname" + i, Add_Actions.Rows[i]["actiontype_shortname"]);

                        query += @" INSERT INTO `cashaction_denominations`(`cashaction_id`, `denominations_id`, `AType_id`, `amount`) VALUES (@Action_ID, @Denominations_id" + i + ", (SELECT actiontype.id FROM actiontype WHERE actiontype.shortname = @Actiontype_shortname" + i + "), @Amount" + i + ");";
                        query += " SET @cashaction_denominations_id" + i + " = Last_Insert_ID();";

                    }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'Cash action', @Action_ID, CONCAT('account Name = ' @Acc_Name), '', '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                if (Delete_Actions.Rows.Count > 0)
                    for (int i = 0; i < Delete_Actions.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'cashaction_denominations', @deletecashaction_denominations_id" + i + " ,CONCAT('account Name = ',@Acc_Name),  CONCAT('Cash action ID = ',  @Action_ID), '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                    }


                if (Add_Actions.Rows.Count > 0)
                    for (int i = 0; i < Add_Actions.Rows.Count; i++)
                    {
                        query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'INSERT', 'cashaction_denominations', @cashaction_denominations_id" + i + " ,CONCAT('account Name = ', @Acc_Name), CONCAT('Cash action ID = ',  @Action_ID),'" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                    }

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }


        public void Update_Approval(int Action_ID, int IsApproved, string Form_Name)
        {
            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();
                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@IsApproved", IsApproved);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                if (IsApproved == 0)
                {
                    sc.Parameters.AddWithValue("@Approved_at", DBNull.Value);
                    sc.Parameters.AddWithValue("@Uesr_ID", DBNull.Value);
                }
                else
                {
                    string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                    sc.Parameters.AddWithValue("@Approved_at", Todaydate);
                    sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);
                }

                query = "START TRANSACTION;";

                query += "UPDATE `action` SET " +
                    " `IsApproved`= @IsApproved" +
                    " ,`Approved_at`= @Approved_at" +
                    " ,`Approved_by`= @Uesr_ID" +
                    " WHERE ID = @Action_ID;";

                query += "UPDATE `action` SET " +
                   " `IsApproved`= @IsApproved" +
                   " ,`Approved_at`= @Approved_at" +
                   " ,`Approved_by`= @Uesr_ID" +
                   " WHERE ObserveAcion_ID = @Action_ID;";


                query += " COMMIT;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'UPDATE', 'action', @Action_ID, CONCAT('Approved = ', @IsApproved), '', '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";


                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public void Update_Multi_Approval()
        {
            string query = "";


            query += "START TRANSACTION;";

            query += string.Join(Environment.NewLine, Login.ActionApproval_List.ToArray());
            query += " COMMIT;";
            if (Login.BuildConnection())
            {
                Login.ActionApproval_sc.CommandTimeout = 120;
                Login.ActionApproval_sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                Login.ActionApproval_sc.Parameters.AddWithValue("@t", 600);
                Login.ActionApproval_sc.Connection = Login.MyConn;

                Login.ActionApproval_sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public void Delete(int ID, int Till_ID, string AType, bool Approved_Action, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@ID", ID);
                sc.Parameters.AddWithValue("@Till_ID", Till_ID);
                sc.Parameters.AddWithValue("@AType", AType);
                sc.Parameters.AddWithValue("@Form_Name", Form_Name);

                string Todaydate = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
                sc.Parameters.AddWithValue("@Todaydate", Todaydate);
                sc.Parameters.AddWithValue("@Uesr_ID", Settings.Default.User_ID);


                query += "START TRANSACTION;";


                if (Approved_Action)
                {
                    query += "INSERT INTO action_deleted SELECT * FROM action WHERE action.id = @ID OR action.ObserveAcion_ID = @ID;";

                    query += "Update action_deleted set `deleted_at` = @Todaydate, `deleted_by` = @Uesr_ID WHERE action_deleted.id = @ID OR action_deleted.ObserveAcion_ID = @ID;";

                    query += "DELETE FROM action WHERE action.ObserveAcion_ID = @ID;";

                    query += "DELETE FROM action WHERE action.id  = @ID;";

                    //query += "DELETE FROM `action` WHERE action.ObserveAcion_ID = @ID;";

                    //query += "DELETE FROM `action` WHERE action.ID = @ID;";

                    //query += "DELETE FROM action_attachments WHERE action_id = @ID;";
                }
                else
                {
                    query += "INSERT INTO action_deleted SELECT * FROM action_not_approved WHERE action_not_approved.id = @ID OR action_not_approved.ObserveAcion_ID = @ID;";

                    query += "Update action_deleted set `deleted_at` = @Todaydate, `deleted_by` = @Uesr_ID WHERE action_deleted.id = @ID OR action_deleted.ObserveAcion_ID = @ID;";

                    query += "DELETE FROM action_not_approved WHERE action_not_approved.ObserveAcion_ID = @ID;";

                    query += "DELETE FROM action_not_approved WHERE action_not_approved.id  = @ID;";

                    //query += "DELETE FROM `action_not_approved` WHERE action_not_approved.ObserveAcion_ID = @ID;";

                    //query += "DELETE FROM `action_not_approved` WHERE action_not_approved.ID = @ID;";

                    //query += "DELETE FROM action_attachments WHERE action_id = @ID;";

                }

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                //if (Approved_Action)
                //{
                //    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'action', @ID, CONCAT('action Type = ', @AType), CONCAT('till ID = ',@Till_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                //}
                //else
                //{
                //    query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'action_not_approved', @ID, CONCAT('action Type = ', @AType), CONCAT('till ID = ',@Till_ID) , '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                //}
                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }

        public void Delete_CashAction(int Action_ID, string Acc_Name, string Form_Name)
        {

            string query = "";
            string log_query = "";

            if (Login.BuildConnection())
            {
                MySqlCommand sc = new MySqlCommand();

                sc.Parameters.AddWithValue("@Action_ID", Action_ID);
                sc.Parameters.AddWithValue("@Acc_Name", Acc_Name);

                sc.Parameters.AddWithValue("@Form_Name", Form_Name);


                query += "START TRANSACTION;";

                query += "DELETE FROM `cashaction` WHERE ID = @Action_ID;";

                log_query = Regex.Replace(query, "'", "''");

                foreach (MySqlParameter p in sc.Parameters)
                {
                    log_query = Regex.Replace(log_query, p.ParameterName, p.Value == null ? "" : Regex.Replace(Regex.Replace(p.Value.ToString(), "'", "''"), "'", "''"));
                }

                query += " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES (@Form_Name, 'DELETE', 'Cash action', @Action_ID, CONCAT('account Name = ',@Acc_Name) ,'', '" + log_query + "', DATE(NOW()), '" + Properties.Settings.Default.username + "');";

                query += " COMMIT;";

                sc.CommandTimeout = 120;
                sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                sc.Parameters.AddWithValue("@t", 600);
                sc.Connection = Login.MyConn;
                sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }

        }


        public void Delete_Multi_Actions()
        {
            string query = "";


            query += "START TRANSACTION;";

            query += string.Join(Environment.NewLine, Login.DeleteMultiActions_List.ToArray());
            query += " COMMIT;";
            if (Login.BuildConnection())
            {
                Login.DeleteMultiActions_sc.CommandTimeout = 120; 
                Login.DeleteMultiActions_sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                Login.DeleteMultiActions_sc.Parameters.AddWithValue("@t", 600);
                Login.DeleteMultiActions_sc.Connection = Login.MyConn;

                Login.DeleteMultiActions_sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

        public void Delete_Multi_CashAction()
        {
            string query = "";


            query += "START TRANSACTION;";

            query += string.Join(Environment.NewLine, Login.DeleteMultiCashActions_List.ToArray());
            query += " COMMIT;";
            if (Login.BuildConnection())
            {
                Login.DeleteMultiCashActions_sc.CommandTimeout = 120;
                Login.DeleteMultiCashActions_sc.CommandText = "SET SESSION wait_timeout = @t;" + query;
                Login.DeleteMultiCashActions_sc.Parameters.AddWithValue("@t", 600);
                Login.DeleteMultiCashActions_sc.Connection = Login.MyConn;

                Login.DeleteMultiCashActions_sc.ExecuteScalar();

                Login.CloseAndClearConnection();
            }
        }

    }
}
