using Financial_WPF_App.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Financial_WPF_App.Helper
{
    /// <summary>
    /// One description of a currency, as the program needs to reason about it.
    /// </summary>
    public class CurrencyInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }

        /// <summary>True for the local currencies and for the payments container.</summary>
        public bool IsLocal { get; set; }

        /// <summary>
        /// Divide an amount in this currency by this number to express it in the common
        /// unit. 1 for the current local currency and for anything foreign; 100 for the
        /// old lira, because a hundred old are worth one new.
        /// </summary>
        public decimal BaseDivisor { get; set; }

        /// <summary>
        /// True for a local currency that has been superseded - the old lira.
        ///
        /// Stored rather than derived from BaseDivisor on purpose. On the old schema
        /// every divisor is 1, so deriving it would answer "no legacy currency exists"
        /// and any report asking for old-currency-only rows would quietly return
        /// nothing. Keeping it separate lets the divisor stay honest (no conversion
        /// before the new currency exists) while this answer stays correct.
        /// </summary>
        public bool IsLegacy { get; set; }
    }

    /// <summary>
    /// The single place that answers currency questions.
    ///
    /// The program used to decide "is this local money?" by comparing a till type name
    /// against the text "SYR", in 134 separate places. That was correct while there was
    /// only one local currency. With two, the same comparison has to become three
    /// different questions, and they are impossible to tell apart by reading the code:
    ///
    ///   1. Is this a local currency?      -> IsLocalCurrency
    ///      Old and new must BOTH answer yes. Decides whether an amount is treated as
    ///      local or foreign - which column it lands in, which rules apply.
    ///
    ///   2. Which local currency exactly?  -> GetCurrencyId / GetCurrencyName
    ///      Old and new must be told apart. Banknote lists, labels, till selection.
    ///
    ///   3. Is this the OLD currency?      -> IsLegacyLocalCurrency
    ///      The new one must answer NO. Reports for closed accounts, which stay in old
    ///      lira by decision.
    ///
    /// Question 3 is the dangerous one. Treating one of those places as question 1 pulls
    /// new-currency rows into a report meant to show only old-currency figures. Nothing
    /// throws; the report is simply wrong. Naming the question in the call site is what
    /// makes that mistake visible in review.
    ///
    /// BEFORE AND AFTER THE SCHEMA CHANGE. This class works either way. If the two new
    /// columns on `tilltypes` are not there yet, it falls back to exactly today's
    /// name-based behaviour, so it is safe to merge and use before that change lands.
    /// Once the columns exist it picks them up automatically on the next refresh.
    /// </summary>
    public static class CurrencyService
    {
        private const string LegacyLocalCurrencyName = "SYR";
        private const string PaymentsContainerName = "pay";

        private static readonly object _gate = new object();
        private static Dictionary<int, CurrencyInfo> _byId;
        private static Dictionary<string, CurrencyInfo> _byName;

        private static bool _schemaSupportsMultipleLocalCurrencies;

        /// <summary>
        /// True when the database carries the currency properties, false while the
        /// program is still running against the old schema. Exposed so a screen can tell
        /// the user why dual-currency features are unavailable rather than failing oddly.
        ///
        /// Loads the cache if it has not been read yet. Without that it would answer
        /// false on a perfectly good database simply because nothing had asked a
        /// currency question yet - and a caller branching on it would quietly take the
        /// old-schema path.
        /// </summary>
        public static bool SchemaSupportsMultipleLocalCurrencies
        {
            get
            {
                EnsureLoaded();
                return _schemaSupportsMultipleLocalCurrencies;
            }
        }

        /// <summary>
        /// Drops the cache. Call after changing the currency table; otherwise the list is
        /// read once, on first use, and kept for the life of the process. The table holds
        /// a handful of rows, so reading it per call would mean thousands of queries on a
        /// single screen.
        /// </summary>
        public static void Refresh()
        {
            lock (_gate)
            {
                _byId = null;
                _byName = null;
            }
        }

        // ------------------------------------------------------------------
        //  Question 1 - is this a local currency?
        //  Old lira, new lira and the payments container all answer yes.
        // ------------------------------------------------------------------

        public static bool IsLocalCurrency(int tillTypeId)
        {
            CurrencyInfo c = Find(tillTypeId);
            return c != null && c.IsLocal;
        }

        public static bool IsLocalCurrency(string tillTypeName)
        {
            CurrencyInfo c = Find(tillTypeName);
            return c != null && c.IsLocal;
        }

        // ------------------------------------------------------------------
        //  Question 2 - which currency is this exactly?
        // ------------------------------------------------------------------

        /// <summary>Currency id for a till type name, or -1 when it does not resolve.</summary>
        public static int GetCurrencyId(string tillTypeName)
        {
            CurrencyInfo c = Find(tillTypeName);
            return c == null ? -1 : c.Id;
        }

        /// <summary>Currency name for an id, or an empty string when it does not resolve.</summary>
        public static string GetCurrencyName(int tillTypeId)
        {
            CurrencyInfo c = Find(tillTypeId);
            return c == null ? string.Empty : c.Name;
        }

        /// <summary>Every currency the database knows about.</summary>
        public static IEnumerable<CurrencyInfo> All()
        {
            EnsureLoaded();
            return _byId.Values;
        }

        // ------------------------------------------------------------------
        //  Question 3 - is this the OLD local currency specifically?
        //  The new lira answers no. The payments container answers no.
        //  Used by anything that must stay in old lira - closed-account reports above
        //  all. Getting this wrong pulls new-currency rows into an old-currency report
        //  without raising anything.
        // ------------------------------------------------------------------

        public static bool IsLegacyLocalCurrency(int tillTypeId)
        {
            CurrencyInfo c = Find(tillTypeId);
            return c != null && c.IsLegacy;
        }

        public static bool IsLegacyLocalCurrency(string tillTypeName)
        {
            CurrencyInfo c = Find(tillTypeName);
            return c != null && c.IsLegacy;
        }

        // ------------------------------------------------------------------
        //  Converting between local currencies for totals
        // ------------------------------------------------------------------

        /// <summary>
        /// The number to divide an amount by to express it in the common unit.
        /// Returns 1 for anything that does not resolve, so a total is never silently
        /// scaled by a value nobody chose.
        /// </summary>
        public static decimal GetBaseDivisor(int tillTypeId)
        {
            CurrencyInfo c = Find(tillTypeId);
            return c == null || c.BaseDivisor == 0m ? 1m : c.BaseDivisor;
        }

        public static decimal GetBaseDivisor(string tillTypeName)
        {
            CurrencyInfo c = Find(tillTypeName);
            return c == null || c.BaseDivisor == 0m ? 1m : c.BaseDivisor;
        }

        /// <summary>
        /// Expresses an amount in the common unit so amounts in different local
        /// currencies can be added together. DISPLAY ONLY - nothing stored is ever
        /// converted; the saved amount keeps matching its original paper document.
        /// </summary>
        public static decimal ToBaseUnit(decimal amount, int tillTypeId)
        {
            return amount / GetBaseDivisor(tillTypeId);
        }

        /// <summary>
        /// The reverse: expresses a common-unit amount in the given currency, for when
        /// the user asks to read figures in the old lira.
        /// </summary>
        public static decimal FromBaseUnit(decimal baseAmount, int tillTypeId)
        {
            return baseAmount * GetBaseDivisor(tillTypeId);
        }

        /// <summary>
        /// SQL fragment testing whether a tilltypes row is a local currency. Pass the
        /// alias the query already uses, for example LocalCurrencySql("tilltypes", true).
        ///
        /// includePaymentsContainer decides whether the 'pay' container counts. The
        /// existing queries are not consistent about this - the till screen counts pay
        /// for income but not for expenses - and that difference is preserved rather
        /// than quietly tidied away, because nobody now knows whether it was deliberate.
        /// Callers state which one they mean.
        ///
        /// On a database that has not had the currency columns added yet, this returns
        /// exactly the condition the query used before, so behaviour is unchanged.
        /// </summary>
        public static string LocalCurrencySql(string tilltypesAlias, bool includePaymentsContainer)
        {
            EnsureLoaded();

            if (!_schemaSupportsMultipleLocalCurrencies)
            {
                return includePaymentsContainer
                    ? "(" + tilltypesAlias + ".Name = 'SYR' OR " + tilltypesAlias + ".Name = 'PAY')"
                    : tilltypesAlias + ".Name = 'SYR'";
            }

            return includePaymentsContainer
                ? tilltypesAlias + ".is_local_currency = 1"
                : "(" + tilltypesAlias + ".is_local_currency = 1 AND LOWER(" + tilltypesAlias + ".Name) <> 'pay')";
        }

        /// <summary>
        /// SQL fragment for "the amount is in a local currency", as the action tables
        /// express it: the currency column says a local currency, or says nothing at all.
        /// A missing currency has always meant the local one.
        /// </summary>
        public static string LocalAmountSql(string currencyAlias)
        {
            EnsureLoaded();

            string isLocal = _schemaSupportsMultipleLocalCurrencies
                ? currencyAlias + ".is_local_currency = 1"
                : currencyAlias + ".Name = 'SYR'";

            return "(" + isLocal
                 + " OR " + currencyAlias + ".Name IS NULL"
                 + " OR " + currencyAlias + ".Name = '')";
        }

        /// <summary>
        /// SQL fragment that scales a column to the common unit, for totals that span
        /// both local currencies. Pass the aliases already in the query, for example
        /// ScaleToBaseUnitSql("action.Amount", "tilltypes").
        /// </summary>
        public static string ScaleToBaseUnitSql(string amountColumn, string tillTypesAlias)
        {
            EnsureLoaded();
            if (!SchemaSupportsMultipleLocalCurrencies)
                return amountColumn;

            return "(" + amountColumn + " / NULLIF(" + tillTypesAlias + ".base_divisor, 0))";
        }

        // ------------------------------------------------------------------
        //  Lookup
        // ------------------------------------------------------------------

        private static CurrencyInfo Find(int tillTypeId)
        {
            EnsureLoaded();
            CurrencyInfo c;
            return _byId.TryGetValue(tillTypeId, out c) ? c : null;
        }

        private static CurrencyInfo Find(string tillTypeName)
        {
            if (string.IsNullOrWhiteSpace(tillTypeName)) return null;

            EnsureLoaded();
            CurrencyInfo c;
            return _byName.TryGetValue(tillTypeName.Trim(), out c) ? c : null;
        }

        private static void EnsureLoaded()
        {
            if (_byId != null) return;

            lock (_gate)
            {
                if (_byId != null) return;

                Dictionary<int, CurrencyInfo> byId = new Dictionary<int, CurrencyInfo>();
                Dictionary<string, CurrencyInfo> byName =
                    new Dictionary<string, CurrencyInfo>(StringComparer.OrdinalIgnoreCase);

                bool hasColumns = DetectCurrencyColumns();

                string query = hasColumns
                    ? "SELECT ID, Name, is_local_currency, base_divisor FROM tilltypes;"
                    : "SELECT ID, Name FROM tilltypes;";

                DataTable dt = RunQuery(query);
                foreach (DataRow row in dt.Rows)
                {
                    CurrencyInfo c = new CurrencyInfo
                    {
                        Id = Convert.ToInt32(row["ID"]),
                        Name = Convert.ToString(row["Name"]).Trim()
                    };

                    bool isTheOldLira = string.Equals(
                        c.Name, LegacyLocalCurrencyName, StringComparison.OrdinalIgnoreCase);

                    if (hasColumns)
                    {
                        c.IsLocal = Convert.ToInt32(row["is_local_currency"]) != 0;
                        c.BaseDivisor = Convert.ToDecimal(row["base_divisor"]);
                        if (c.BaseDivisor == 0m) c.BaseDivisor = 1m;

                        // A local currency that is not the base unit has been superseded.
                        c.IsLegacy = c.IsLocal && c.BaseDivisor != 1m;
                    }
                    else
                    {
                        // Old schema: reproduce exactly what the program does today, so
                        // behaviour is unchanged until the columns are added.
                        c.IsLocal = isTheOldLira ||
                            string.Equals(c.Name, PaymentsContainerName, StringComparison.OrdinalIgnoreCase);

                        // No conversion is possible or wanted yet - there is nothing to
                        // convert to - so the divisor stays 1 and amounts pass through
                        // untouched. IsLegacy is answered by name instead, which keeps
                        // old-currency-only reports working on this schema.
                        c.BaseDivisor = 1m;
                        c.IsLegacy = isTheOldLira;
                    }

                    byId[c.Id] = c;
                    if (!string.IsNullOrEmpty(c.Name)) byName[c.Name] = c;
                }

                _schemaSupportsMultipleLocalCurrencies = hasColumns;
                _byId = byId;
                _byName = byName;
            }
        }

        /// <summary>
        /// Are the two currency properties present? Checked once per cache load rather
        /// than assumed, so the same build runs against a database before or after the
        /// schema change.
        /// </summary>
        private static bool DetectCurrencyColumns()
        {
            DataTable dt = RunQuery(
                @"SELECT COUNT(*) AS n FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'tilltypes'
                    AND COLUMN_NAME IN ('is_local_currency','base_divisor');");

            if (dt.Rows.Count == 0) return false;
            return Convert.ToInt32(dt.Rows[0]["n"]) == 2;
        }

        private static DataTable RunQuery(string query)
        {
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
    }
}
