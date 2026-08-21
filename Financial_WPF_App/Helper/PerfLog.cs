using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Financial_WPF_App.Helper
{
    /// <summary>
    /// Temporary diagnostic scaffolding for the performance work.
    /// Measures, for every database operation, how long the connection took to open
    /// versus how long the query itself took, and appends one CSV line per operation.
    ///
    /// This is NOT part of the application logic. It never throws, and with
    /// <see cref="Enabled"/> set to false it does nothing at all.
    /// Delete this file and the two calls in Login.cs to remove it entirely.
    /// </summary>
    internal static class PerfLog
    {
        /// <summary>Flip to false to switch off all measurement.</summary>
        public static bool Enabled = true;

        private static readonly object Gate = new object();
        private static string _path;

        [ThreadStatic] private static Stopwatch _total;
        [ThreadStatic] private static long _openMs;
        [ThreadStatic] private static string _caller;

        private static string Path
        {
            get
            {
                if (_path == null)
                {
                    string dir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Hope_Center");
                    Directory.CreateDirectory(dir);
                    _path = System.IO.Path.Combine(dir, "perf.csv");

                    if (!File.Exists(_path))
                        File.AppendAllText(_path,
                            "time,caller,open_ms,query_ms,total_ms" + Environment.NewLine,
                            Encoding.UTF8);
                }
                return _path;
            }
        }

        /// <summary>Called right before MySqlConnection.Open().</summary>
        public static void BeginOpen()
        {
            if (!Enabled) return;
            try
            {
                _total = Stopwatch.StartNew();
                _openMs = -1;
                _caller = null;
            }
            catch { }
        }

        /// <summary>Called right after MySqlConnection.Open() returns.</summary>
        public static void EndOpen()
        {
            if (!Enabled) return;
            try
            {
                if (_total != null)
                    _openMs = _total.ElapsedMilliseconds;
                _caller = FindCaller();
            }
            catch { }
        }

        /// <summary>Called when the operation finishes and the connection is closed.</summary>
        public static void EndOperation()
        {
            if (!Enabled) return;
            try
            {
                if (_total == null) return;

                long total = _total.ElapsedMilliseconds;
                long open = _openMs < 0 ? 0 : _openMs;
                long query = total - open;
                _total = null;

                string line = string.Join(",",
                    DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    _caller ?? "?",
                    open.ToString(CultureInfo.InvariantCulture),
                    query.ToString(CultureInfo.InvariantCulture),
                    total.ToString(CultureInfo.InvariantCulture));

                lock (Gate)
                    File.AppendAllText(Path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>
        /// Walks the stack to the first frame outside Login/PerfLog so the log line
        /// says which model method actually ran the query.
        /// </summary>
        private static string FindCaller()
        {
            try
            {
                StackTrace st = new StackTrace(2, false);
                for (int i = 0; i < st.FrameCount; i++)
                {
                    var m = st.GetFrame(i).GetMethod();
                    if (m == null || m.DeclaringType == null) continue;

                    string type = m.DeclaringType.Name;
                    if (type == "PerfLog" || type == "Login") continue;

                    return type + "." + m.Name;
                }
            }
            catch { }
            return "?";
        }
    }
}
