namespace Financial_WPF_App.Helper
{
    /// <summary>
    /// Adds LIMIT to a finished query.
    ///
    /// The queries in this application are assembled as strings and end with a semicolon,
    /// sometimes after an ORDER BY and sometimes not. LIMIT has to go after both and before
    /// the semicolon, and getting that wrong is a syntax error at run time rather than at
    /// compile time - so it is done in one place.
    /// </summary>
    public static class SqlLimit
    {
        /// <summary>
        /// Returns the query with "LIMIT n" appended. A limit of 0 or less returns the query
        /// untouched, which is what every caller outside the approval screens passes.
        /// </summary>
        public static string Apply(string query, int rows)
        {
            if (query == null || rows <= 0)
                return query;

            string trimmed = query.TrimEnd();

            // Already limited - leave it alone rather than produce "LIMIT 50 LIMIT 100".
            if (trimmed.TrimEnd(';').TrimEnd().ToUpperInvariant().Contains(" LIMIT "))
                return query;

            bool hadSemicolon = trimmed.EndsWith(";");
            if (hadSemicolon)
                trimmed = trimmed.Substring(0, trimmed.Length - 1).TrimEnd();

            return trimmed + " LIMIT " + rows + (hadSemicolon ? ";" : "");
        }
    }
}
