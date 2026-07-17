using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Linq;

namespace Financial_WPF_App.Helper
{
    /// <summary>
    /// Validation and calculation helpers extracted from AddAction_Window.
    /// </summary>
    public static class AddActionSaveHelpers
    {
        public static bool CheckAvgRate(Rate rateModel, string accountId, DateTime actionDate, string tillType, string destinationTillText)
        {
            string date = actionDate.ToString("yyyy/MM/dd");
            DataTable avgRateDt = rateModel.Check_AvgRate_Availability(accountId, date, tillType, destinationTillText ?? string.Empty);
            if (avgRateDt.Rows.Count == 0)
                return false;
            return true;
        }

        public static int GetUsdTillIdForCurrentAccount(DataTable tills, int accountId)
        {
            if (tills == null || tills.Rows.Count == 0)
                return -1;

            if (accountId <= 0)
                return -1;

            var row = tills.AsEnumerable()
                .FirstOrDefault(r =>
                    !r.IsNull("Tilltypes_Name") &&
                    r["Tilltypes_Name"].ToString().Equals("usd", StringComparison.OrdinalIgnoreCase) &&
                    !r.IsNull("Account_ID") &&
                    Convert.ToInt32(r["Account_ID"]) == accountId);

            if (row == null)
                return -1;

            return Convert.ToInt32(row["Till_ID"]);
        }

        public static int GetCurrencyTilltypeIdForAddActionSave(string actionType, string tillType, int selectedCurrencyId)
        {
            if (IsExpenseFromSyrTill(actionType, tillType))
            {
                if (selectedCurrencyId <= 0)
                    return AddActionSaveContext.SyrCurrencyTilltypeId;
            }

            return selectedCurrencyId;
        }

        public static bool IsExpenseFromSyrTill(string actionType, string tillType)
        {
            return actionType == "E" &&
                   !string.IsNullOrEmpty(tillType) &&
                   tillType.Equals("syr", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetObserveTillType(DataTable correspondingTills, int destinationTillId)
        {
            if (correspondingTills == null || destinationTillId <= 0)
                return string.Empty;

            DataRow[] rows = correspondingTills.Select("ID=" + destinationTillId);
            return rows.Length == 0 ? string.Empty : Convert.ToString(rows[0]["Type"]);
        }

        public static string ResolveInsertedValue(string operationTypeText)
        {
            if (string.IsNullOrEmpty(operationTypeText))
                return string.Empty;

            if (operationTypeText.Contains("Income"))
                return "I";
            if (operationTypeText.Contains("Adv. Payment"))
                return "O";
            if (operationTypeText.Contains("Returned"))
                return "R";
            if (operationTypeText.Contains("Expenses"))
                return "E";
            if (operationTypeText.Contains("Exchange"))
                return "Ex";
            if (operationTypeText.Contains("Move"))
                return "M";

            return string.Empty;
        }
    }
}
