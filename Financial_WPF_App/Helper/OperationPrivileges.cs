using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial_WPF_App.Helper
{
    public class OperationPrivileges
    {
        public bool Income { get; set; }
        public bool Outcome { get; set; }
        public bool Returned { get; set; }
        public bool Exchange { get; set; }
        public bool ExchangeToForeign { get; set; }
        public bool Expand { get; set; }
        public bool Move { get; set; }
    }

    public static class FinancialAuthorizationService
    {
        public static DataTable FilterTillsByRole(DataTable temp_tills_dt, DataSet allAccounts_ds)
        {
            if (temp_tills_dt == null || temp_tills_dt.Rows.Count == 0) return null;

            string role = Properties.Settings.Default.Role;
            if (role == "Head Office") return temp_tills_dt;

            List<string> orConditions = new List<string>();
            DataTable projects_dt = allAccounts_ds.Tables[0];
            DataTable fiscalbonds_dt = allAccounts_ds.Tables[2];
            DataTable personalAccounts_dt = allAccounts_ds.Tables[4];

            // 1. المدير المالي
            if (role == "Financial Manager")
            {
                if (projects_dt != null && projects_dt.Rows.Count > 0)
                {
                    var userSubprojects = projects_dt.AsEnumerable().Select(row => row.Field<int>("SubprojectID")).Distinct();
                    orConditions.Add($"Subproject_ID IN ({string.Join(",", userSubprojects)})");
                }
                if (personalAccounts_dt != null && personalAccounts_dt.Rows.Count > 0)
                {
                    var userPersonal = personalAccounts_dt.AsEnumerable().Select(row => row.Field<int>("ID")).Distinct();
                    orConditions.Add($"(Account_ID IN ({string.Join(",", userPersonal)}) AND {CurrencyService.LocalCurrencyNamesFilter("Tilltypes_Name", false)})");
                }
                if (orConditions.Count == 0) return null;
                string filter = $"({string.Join(" OR ", orConditions)}) AND is_administrative = 0";
                DataRow[] rows = temp_tills_dt.Select(filter);
                return rows.Length > 0 ? rows.CopyToDataTable() : null;
            }

            // 2. المحاسب
            if (role == "Accountant")
            {
                if (projects_dt != null && projects_dt.Rows.Count > 0)
                {
                    var userAccounts = projects_dt.AsEnumerable().Select(row => row.Field<int>("AccountID")).Distinct();
                    orConditions.Add($"(Account_ID IN ({string.Join(",", userAccounts)}) AND {CurrencyService.LocalCurrencyNamesFilter("Tilltypes_Name", false)})");
                }
                if (personalAccounts_dt != null && personalAccounts_dt.Rows.Count > 0)
                {
                    var userPersonal = personalAccounts_dt.AsEnumerable().Select(row => row.Field<int>("ID")).Distinct();
                    orConditions.Add($"(Account_ID IN ({string.Join(",", userPersonal)}) AND {CurrencyService.LocalCurrencyNamesFilter("Tilltypes_Name", false)})");
                }
                if (orConditions.Count == 0) return null;
                string filter = $"({string.Join(" OR ", orConditions)}) AND is_administrative = 0 AND IsClosed = 0";
                DataRow[] rows = temp_tills_dt.Select(filter);
                return rows.Length > 0 ? rows.CopyToDataTable() : null;
            }

            // 3. الموظف
            if (role == "Employee")
            {
                if (fiscalbonds_dt != null && fiscalbonds_dt.Rows.Count > 0)
                {
                    var userFiscal = fiscalbonds_dt.AsEnumerable().Select(row => row.Field<int>("FiscalbondID")).Distinct();
                    orConditions.Add($"(Fiscalbond_ID IN ({string.Join(",", userFiscal)}) AND {CurrencyService.LocalCurrencyNamesFilter("Tilltypes_Name", false)})");
                }
                if (orConditions.Count == 0) return null;
                string filter = $"({string.Join(" OR ", orConditions)}) AND is_administrative = 0 AND IsClosed = 0";
                DataRow[] rows = temp_tills_dt.Select(filter);
                return rows.Length > 0 ? rows.CopyToDataTable() : null;
            }

            return null;
        }

        public static OperationPrivileges GetOperationPrivileges(string role, string accountType, string tillType)
        {
            var priv = new OperationPrivileges();
            if (string.IsNullOrEmpty(tillType)) return priv;

            string currency = tillType.ToLower();

            // الإدارة العامة تمتلك كامل الصلاحيات لجميع الحسابات والعملات
            if (role == "Head Office")
            {
                priv.Income = priv.Outcome = priv.Returned = priv.Exchange = priv.ExchangeToForeign = priv.Expand = priv.Move = true;
                return priv;
            }

            // صلاحيات المدير المالي والمحاسب متطابقة في القيود التشغيلية بحسب الصندوق والحساب
            if (role == "Financial Manager" || role == "Accountant")
            {
                if (accountType == "Personal")
                {
                    if (CurrencyService.IsLocalCurrency(currency, false) || currency == "usd" || currency == "eur")
                    {
                        priv.Income = priv.Expand = true;
                    }
                }
                else if (accountType == "Project")
                {
                    if (CurrencyService.IsLocalCurrency(currency, false))
                    {
                        priv.Income = priv.Outcome = priv.Returned = priv.Exchange = priv.Expand = priv.Move = true;
                    }
                    else
                    {
                        priv.Exchange = true;
                    }
                }
            }
            else if (role == "Employee")
            {
                if (CurrencyService.IsLocalCurrency(currency, false))
                {
                    priv.Income = priv.Outcome = priv.Returned = priv.Expand = true;
                }
            }

            return priv;
        }
    }
}
