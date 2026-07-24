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

        /// <summary>
        /// Determines whether Origin Avg Rate should be offered (enabled/populated) for a
        /// Budget/Micro Project exchange. EUR to USD is not offered; other cross-currency
        /// pairs including USD to SYR remain available for optional or required selection.
        /// </summary>
        public static bool IsProjectExchangeOriginAvgRateOffered(string sourceTill, string correspondingTill)
        {
            string source = (sourceTill ?? string.Empty).Trim().ToUpperInvariant();
            string destination = (correspondingTill ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination) || source == destination)
                return false;

            // EUR to USD is the only foreign-currency pair that does not expose Origin Avg Rate.
            return source != "EUR" || destination == "SYR";
        }

        /// <summary>
        /// Determines whether a Budget/Micro Project exchange requires an Origin Avg Rate on save.
        /// EUR to USD is not required (and not offered). USD to SYR is offered but optional.
        /// </summary>
        public static bool IsProjectExchangeOriginAvgRateRequired(string sourceTill, string correspondingTill)
        {
            string source = (sourceTill ?? string.Empty).Trim().ToUpperInvariant();
            string destination = (correspondingTill ?? string.Empty).Trim().ToUpperInvariant();

            if (source == "USD" && destination == "SYR")
                return false;

            return IsProjectExchangeOriginAvgRateOffered(source, destination);
        }

        /// <summary>
        /// Resolves the Avg Rate currency pair used for a Project exchange. USD to SYR consumes
        /// the project's EUR to USD Origin Avg Rate; all other supported pairs use their own pair.
        /// </summary>
        public static void GetProjectExchangeOriginAvgRatePair(
            string sourceTill,
            string correspondingTill,
            out string rateFromTill,
            out string rateToTill)
        {
            string source = (sourceTill ?? string.Empty).Trim().ToUpperInvariant();
            string destination = (correspondingTill ?? string.Empty).Trim().ToUpperInvariant();

            if (source == "USD" && destination == "SYR")
            {
                rateFromTill = "EUR";
                rateToTill = "USD";
                return;
            }

            rateFromTill = source;
            rateToTill = destination;
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
