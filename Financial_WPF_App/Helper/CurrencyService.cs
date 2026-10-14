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

        private static bool _cashBalancesHasCurrentCurrency;
        private static bool _hasRoleCurrencyLimits;

        /// <summary>
        /// True once the role_currency_limit table exists (P3 script): approval limits are then
        /// set per role AND per currency. Asked of the database, so the program runs before P3.
        /// </summary>
        public static bool HasRoleCurrencyLimits
        {
            get { EnsureLoaded(); return _hasRoleCurrencyLimits; }
        }


        /// <summary>
        /// True once the cash_balances view carries the current local currency's columns
        /// (Account_I_New and friends, added by the P2 script). Asked of the database rather
        /// than assumed, so a screen can offer those columns only when they exist and the
        /// program still runs against a database where the view has not been updated yet.
        /// </summary>
        public static bool CashBalancesHasCurrentCurrency
        {
            get { EnsureLoaded(); return _cashBalancesHasCurrentCurrency; }
        }

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

        /// <summary>Same, with the caller deciding whether the payments container counts.</summary>
        public static bool IsLocalCurrency(int tillTypeId, bool includePaymentsContainer)
        {
            CurrencyInfo c = Find(tillTypeId);
            if (c == null || !c.IsLocal) return false;

            if (includePaymentsContainer) return true;

            return !string.Equals(c.Name, PaymentsContainerName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLocalCurrency(string tillTypeName)
        {
            CurrencyInfo c = Find(tillTypeName);
            return c != null && c.IsLocal;
        }

        /// <summary>
        /// Same question, but the caller says whether the payments container counts.
        /// Rules about what a user may do with money should normally pass false: 'pay'
        /// holds local money, so it is a local currency, but it is a container rather
        /// than something anyone picks or spends from.
        /// </summary>
        public static bool IsLocalCurrency(string tillTypeName, bool includePaymentsContainer)
        {
            CurrencyInfo c = Find(tillTypeName);
            if (c == null || !c.IsLocal) return false;

            if (includePaymentsContainer) return true;

            return !string.Equals(c.Name, PaymentsContainerName, StringComparison.OrdinalIgnoreCase);
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

        /// <summary>
        /// The local currency in use now - the one that is not superseded and is not the
        /// payments container. Returns an empty string if there is no such currency.
        ///
        /// Used where something new has to be created in "the" local currency without
        /// naming it in code: a new account's amount type, for instance.
        /// </summary>
        public static string GetCurrentLocalCurrencyName()
        {
            EnsureLoaded();

            foreach (CurrencyInfo c in _byId.Values)
            {
                if (!c.IsLocal || c.IsLegacy) continue;
                if (string.Equals(c.Name, PaymentsContainerName, StringComparison.OrdinalIgnoreCase)) continue;
                return c.Name;
            }
            return string.Empty;
        }

        /// <summary>The old local currency's name, or "SYR" when none is flagged.</summary>
        public static string GetLegacyLocalCurrencyName()
        {
            EnsureLoaded();
            foreach (CurrencyInfo c in _byId.Values)
                if (c.IsLocal && c.IsLegacy) return c.Name;
            return LegacyLocalCurrencyName;
        }

        /// <summary>
        /// "SYR, SYPNEW or USD" - the local currencies (not the pay container) and USD, read
        /// from the currency table, for messages naming what a Currency field accepts. Those
        /// messages said "USD or SYR" while also accepting the new lira.
        /// </summary>
        public static string LocalOrUsdNames()
        {
            EnsureLoaded();
            var names = new List<string>();
            foreach (CurrencyInfo c in _byId.Values)
                if (IsLocalCurrency(c.Name, false)) names.Add(c.Name);
            names.Add("USD");
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.GetRange(0, names.Count - 1)) + " or " + names[names.Count - 1];
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
        /// A DataTable filter expression matching the local currencies by name, for the
        /// places that filter an in-memory table rather than query the database - for
        /// example LocalCurrencyNamesFilter("Tilltypes_Name", false) produces
        /// Tilltypes_Name IN ('SYR','SYP(NEW)').
        ///
        /// Names are taken from the currency table rather than written into the code, so
        /// a currency added later is included without anyone editing this.
        /// </summary>
        public static string LocalCurrencyNamesFilter(string columnName, bool includePaymentsContainer)
        {
            EnsureLoaded();

            var names = new List<string>();
            foreach (CurrencyInfo c in _byId.Values)
            {
                if (!c.IsLocal) continue;
                if (!includePaymentsContainer &&
                    string.Equals(c.Name, PaymentsContainerName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // A single quote inside a DataTable filter literal is escaped by doubling it.
                names.Add("'" + c.Name.Replace("'", "''") + "'");
            }

            // Nothing local at all would make the filter match everything if left empty,
            // so fall back to a condition that matches nothing instead.
            if (names.Count == 0) return "1 = 0";

            return columnName + " IN (" + string.Join(",", names) + ")";
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

        /// <summary>
        /// SQL condition: this action is in the OLD local currency. A row with no currency
        /// recorded predates the new currency, so it counts as old.
        /// </summary>
        public static string LegacyAmountSql(string currencyAlias)
        {
            EnsureLoaded();
            string old = _schemaSupportsMultipleLocalCurrencies
                ? "(" + currencyAlias + ".is_local_currency = 1 AND " + currencyAlias + ".base_divisor <> 1)"
                : currencyAlias + ".Name = 'SYR'";
            return "(" + old + " OR " + currencyAlias + ".Name IS NULL OR " + currencyAlias + ".Name = '')";
        }

        /// <summary>
        /// SQL condition: this action is in the CURRENT local currency (the one whose
        /// base_divisor is 1). On the old schema there is none, so it matches nothing.
        /// </summary>
        public static string CurrentLocalAmountSql(string currencyAlias)
        {
            EnsureLoaded();
            if (!_schemaSupportsMultipleLocalCurrencies) return "1 = 0";
            return "(" + currencyAlias + ".is_local_currency = 1 AND " + currencyAlias + ".base_divisor = 1 AND LOWER(" + currencyAlias + ".Name) <> 'pay')";
        }

        // ------------------------------------------------------------------
        //  Which local currency an ACTION's amount is in
        //
        //  The action's currency column cannot be read on its own:
        //   - on a local till it is right when filled (the till's currency, or USD for a
        //     dollar expense paid from that till), and empty means the till's own currency
        //     - exchanges and some entry screens leave it empty;
        //   - on a USD / EUR till the amount is always in that till's currency, whatever the
        //     column says (an exchange stamps its target currency there, often 'SYR');
        //   - on the pay till it is always filled; an empty one predates the new currency.
        //  So these take the till's currency alias as well. The one-argument forms above
        //  remain for rows that have no till (a beneficiary's loan currency).
        // ------------------------------------------------------------------

        /// <summary>
        /// SQL condition: this action's amount is in the OLD local currency. Pass the alias of
        /// the action's currency and of its till's type, e.g. LegacyAmountSql("currency_type", "tilltypes").
        /// </summary>
        public static string LegacyAmountSql(string currencyAlias, string tillTypeAlias)
        {
            EnsureLoaded();
            string noCurrency = "(" + currencyAlias + ".Name IS NULL OR " + currencyAlias + ".Name = '')";
            string tillIsLocal = LocalCurrencySql(tillTypeAlias, true);

            if (!_schemaSupportsMultipleLocalCurrencies)
                return NullSafe("(" + tillIsLocal + " AND (" + currencyAlias + ".Name = 'SYR' OR " + noCurrency + "))");

            return NullSafe("(" + tillIsLocal + " AND ("
                 + "(" + currencyAlias + ".is_local_currency = 1 AND " + currencyAlias + ".base_divisor <> 1)"
                 + " OR (" + noCurrency + " AND (" + tillTypeAlias + ".base_divisor <> 1 OR LOWER(" + tillTypeAlias + ".Name) = 'pay'))))");
        }

        /// <summary>
        /// A row with no currency makes the comparisons on the currency's columns NULL, and
        /// NULL is not "false": NOT (condition) would drop the row from both sides. Collapsing
        /// it to 0 keeps the condition a plain yes / no, so it can be negated safely.
        /// </summary>
        private static string NullSafe(string condition)
        {
            return "(COALESCE(" + condition + ", 0) = 1)";
        }

        /// <summary>
        /// SQL condition: this action's amount is in the CURRENT local currency - recorded so,
        /// or recorded with no currency on a till in that currency.
        /// </summary>
        public static string CurrentLocalAmountSql(string currencyAlias, string tillTypeAlias)
        {
            EnsureLoaded();
            if (!_schemaSupportsMultipleLocalCurrencies) return "1 = 0";

            string noCurrency = "(" + currencyAlias + ".Name IS NULL OR " + currencyAlias + ".Name = '')";
            return NullSafe("(" + LocalCurrencySql(tillTypeAlias, true) + " AND ("
                 + CurrentLocalAmountSql(currencyAlias)
                 + " OR (" + noCurrency + " AND " + tillTypeAlias + ".base_divisor = 1 AND LOWER(" + tillTypeAlias + ".Name) <> 'pay')))");
        }

        /// <summary>
        /// The divisor of the old local currency, for the rows that do not say which
        /// currency they are in. Falls back to 1, so an unresolved legacy currency
        /// leaves amounts alone rather than scaling them by a guess.
        /// </summary>
        public static decimal LegacyBaseDivisor
        {
            get
            {
                EnsureLoaded();
                foreach (CurrencyInfo c in _byId.Values)
                    if (c.IsLegacy) return c.BaseDivisor == 0m ? 1m : c.BaseDivisor;
                return 1m;
            }
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
                _cashBalancesHasCurrentCurrency = hasColumns && DetectCashBalancesColumns();
                _hasRoleCurrencyLimits = DetectRoleCurrencyLimits();
                _byId = byId;
                _byName = byName;
            }
        }

        /// <summary>
        /// SQL expression: the id of the currency an action's amount is really in - the rule
        /// described above LegacyAmountSql. On a USD / EUR till that is the till's currency,
        /// whatever the currency column says (an exchange records its TARGET there: 8,000 USD
        /// sold for lira is saved as 'SYR'). On a local till it is the recorded currency (USD for
        /// a dollar expense paid from it), or the till's own when none is recorded. On the pay
        /// till an empty one is the old currency.
        /// </summary>
        public static string AmountCurrencyIdSql(string currencyAlias, string tillTypeAlias)
        {
            EnsureLoaded();
            int legacyId = LegacyCurrencyId;
            string payFallback = legacyId > 0 ? legacyId.ToString() : tillTypeAlias + ".ID";
            return "(CASE WHEN LOWER(" + tillTypeAlias + ".Name) = 'pay' THEN COALESCE(" + currencyAlias + ".ID, " + payFallback + ")"
                 + " WHEN " + LocalCurrencySql(tillTypeAlias, true) + " THEN COALESCE(" + currencyAlias + ".ID, " + tillTypeAlias + ".ID)"
                 + " ELSE " + tillTypeAlias + ".ID END)";
        }

        /// <summary>
        /// SQL expression: an amount as it compares against the single role limit used before
        /// per-currency limits (P3). Those limits were set in the old lira, so a local amount is
        /// expressed in that unit first - 10,000 new lira is 1,000,000 old and must not pass under
        /// a 1,000,000 limit as if it were small. A foreign amount is compared as it always was.
        /// The currency is the one the amount is really in (AmountCurrencyIdSql), so an empty
        /// currency on a new-currency till is scaled as new lira, not taken as old.
        /// </summary>
        public static string ApprovalAmountSql(string amountColumn, string currencyAlias, string tillTypeAlias)
        {
            EnsureLoaded();
            if (!_schemaSupportsMultipleLocalCurrencies)
                return amountColumn;

            string legacy = LegacyBaseDivisor.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string divisor = "(SELECT d.base_divisor FROM tilltypes d WHERE d.ID = " + AmountCurrencyIdSql(currencyAlias, tillTypeAlias)
                           + " AND d.is_local_currency = 1 AND LOWER(d.Name) <> 'pay')";
            return "(CASE WHEN " + divisor + " IS NULL THEN " + amountColumn
                 + " ELSE " + amountColumn + " * " + legacy + " / NULLIF(" + divisor + ", 0) END)";
        }

        /// <summary>The old local currency's id, or 0 when none is flagged.</summary>
        public static int LegacyCurrencyId
        {
            get
            {
                EnsureLoaded();
                foreach (CurrencyInfo c in _byId.Values)
                    if (c.IsLocal && c.IsLegacy) return c.Id;
                return 0;
            }
        }

        /// <summary>
        /// SQL condition: may a user in this role approve this amount? Once per-currency limits
        /// exist (role_currency_limit, P3) the amount is compared with the role's limit IN THE
        /// ACTION'S CURRENCY, and a currency with no limit set is NOT approvable by that role -
        /// an empty limit means "not allowed", never "no limit". Before P3 the old rule applies:
        /// the single role limit, with a local amount expressed in old lira first.
        /// The amount's currency is read with its till's (AmountCurrencyIdSql): an exchange of
        /// 8,000 USD is held to the USD limit, not to the lira limit its currency column names.
        /// </summary>
        public static string ApprovalLimitConditionSql(string amountColumn, string currencyAlias, string tillTypeAlias, string roleName)
        {
            EnsureLoaded();
            string role = (roleName ?? "").Replace("'", "''");
            string singleLimit = "(select role.Approval_Limit from role where role.Name = '" + role + "')";
            string oldRule = ApprovalAmountSql(amountColumn, currencyAlias, tillTypeAlias) + " <= " + singleLimit;

            if (!_hasRoleCurrencyLimits)
                return oldRule;

            string perCurrency = "(SELECT l.Approval_Limit FROM role_currency_limit l JOIN role r ON r.ID = l.Role_ID"
                               + " WHERE r.Name = '" + role + "' AND l.TillType_ID = " + AmountCurrencyIdSql(currencyAlias, tillTypeAlias) + ")";

            return "(" + perCurrency + " IS NOT NULL AND " + amountColumn + " <= " + perCurrency + ")";
        }

        /// <summary>Which local currencies actually hold data in some part of the books.</summary>
        public struct LocalDataScope
        {
            public bool HasLegacy;
            public bool HasCurrent;

            /// <summary>Show the old currency's columns: only where it has data.</summary>
            public bool ShowLegacy { get { return HasLegacy; } }

            /// <summary>
            /// Show the current currency's columns: where it has data, and also where
            /// nothing has any yet (a new, empty scope should offer the currency being used).
            /// </summary>
            public bool ShowCurrent { get { return HasCurrent || !HasLegacy; } }

            /// <summary>An all-local total only means something when both currencies are present.</summary>
            public bool ShowsBoth { get { return ShowLegacy && ShowCurrent; } }
        }

        private static bool IsIdList(string ids)
        {
            return !string.IsNullOrWhiteSpace(ids)
                && System.Text.RegularExpressions.Regex.IsMatch(ids.Trim(), @"^\d+(\s*,\s*\d+)*$");
        }

        /// <summary>
        /// Does this part of the books - the given accounts and/or sub-projects, or all of it
        /// when both are empty - hold anything in the old local currency, and in the current
        /// one? Looks at actions (approved and not), and at the beneficiaries' loan currency.
        /// A screen asks once when it opens and shows only the columns that can have figures,
        /// so a project opened after the old currency was retired shows one column, not two
        /// with one of them always empty. Asked of the whole scope rather than of what is
        /// loaded, so the columns do not appear and disappear as the user filters.
        /// Where the database cannot tell (old schema, an unusable id list) both are shown.
        /// </summary>
        public static LocalDataScope GetLocalDataScope(string accountIds, string subprojectIds)
        {
            EnsureLoaded();

            LocalDataScope both = new LocalDataScope { HasLegacy = true, HasCurrent = true };
            if (!_schemaSupportsMultipleLocalCurrencies)
                return new LocalDataScope { HasLegacy = true, HasCurrent = false };

            bool hasAccounts = !string.IsNullOrWhiteSpace(accountIds);
            bool hasSubs = !string.IsNullOrWhiteSpace(subprojectIds);
            if ((hasAccounts && !IsIdList(accountIds)) || (hasSubs && !IsIdList(subprojectIds)))
                return both;

            string actionScope = " WHERE 1 = 1";
            string benefScope = " WHERE 1 = 1";
            if (hasAccounts)
            {
                actionScope += " AND t.Account_ID IN (" + accountIds.Trim() + ")";
                benefScope += " AND b.Account_ID IN (" + accountIds.Trim() + ")";
            }
            if (hasSubs)
            {
                actionScope += " AND ac.SubProject_ID IN (" + subprojectIds.Trim() + ")";
                benefScope += " AND ac.SubProject_ID IN (" + subprojectIds.Trim() + ")";
            }

            // An action's currency is read together with its till's (see LegacyAmountSql);
            // a beneficiary has no till, so its loan currency is taken as recorded.
            Func<string, string, string> exists = (actionPredicate, benefPredicate) =>
                "(EXISTS (SELECT 1 FROM `action` a JOIN till t ON t.ID = a.Till_ID JOIN account ac ON ac.ID = t.Account_ID"
                + " JOIN tilltypes tt ON tt.ID = t.Type_ID"
                + " LEFT JOIN tilltypes ct ON ct.ID = a.currency_tilltype_id" + actionScope + " AND " + actionPredicate + ")"
                + " OR EXISTS (SELECT 1 FROM action_not_approved a JOIN till t ON t.ID = a.Till_ID JOIN account ac ON ac.ID = t.Account_ID"
                + " JOIN tilltypes tt ON tt.ID = t.Type_ID"
                + " LEFT JOIN tilltypes ct ON ct.ID = a.currency_tilltype_id" + actionScope + " AND " + actionPredicate + ")"
                + " OR EXISTS (SELECT 1 FROM beneficiary b JOIN account ac ON ac.ID = b.Account_ID"
                + " LEFT JOIN tilltypes ct ON ct.ID = b.currency_tilltype_id" + benefScope + " AND " + benefPredicate + "))";

            DataTable dt = RunQuery("SELECT " + exists(LegacyAmountSql("ct", "tt"), LegacyAmountSql("ct")) + " AS has_legacy, "
                                    + exists(CurrentLocalAmountSql("ct", "tt"), CurrentLocalAmountSql("ct")) + " AS has_current;");
            if (dt.Rows.Count == 0)
                return both;

            return new LocalDataScope
            {
                HasLegacy = Convert.ToInt32(dt.Rows[0]["has_legacy"]) != 0,
                HasCurrent = Convert.ToInt32(dt.Rows[0]["has_current"]) != 0
            };
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

        private static bool DetectRoleCurrencyLimits()
        {
            DataTable dt = RunQuery(
                @"SELECT COUNT(*) AS n FROM information_schema.TABLES
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'role_currency_limit';");
            return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["n"]) == 1;
        }

        private static bool DetectCashBalancesColumns()
        {
            DataTable dt = RunQuery(
                @"SELECT COUNT(*) AS n FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'cash_balances'
                    AND COLUMN_NAME IN ('Account_I_New','Account_E_New','Account_Net_New');");

            if (dt.Rows.Count == 0) return false;
            return Convert.ToInt32(dt.Rows[0]["n"]) == 3;
        }

        /// <summary>
        /// Runs on a connection of its own. It used Login.BuildConnection, which REPLACES the
        /// shared Login.MyConn and then closes it - so a currency question asked while a caller
        /// held Login.MyConn open (the first one inside a save, or the per-screen scope check)
        /// would close that caller's connection under it. Pooling keeps this as cheap as before.
        /// </summary>
        private static DataTable RunQuery(string query)
        {
            DataTable dt = new DataTable();
            using (MySqlConnection conn = new MySqlConnection(Login.ConnectionString))
            using (MySqlCommand sc = new MySqlCommand("SET SESSION wait_timeout = @t;" + query, conn))
            {
                sc.CommandTimeout = 120;
                sc.Parameters.AddWithValue("@t", 600);
                conn.Open();
                using (MySqlDataAdapter da = new MySqlDataAdapter(sc))
                    da.Fill(dt);
            }
            return dt;
        }
    }
}
