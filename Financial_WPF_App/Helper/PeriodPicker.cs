using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Controls;

namespace Financial_WPF_App.Helper
{
    /// <summary>
    /// The month picker used by the approval screens: a combo box of tick boxes listing the
    /// months that actually have rows, with a Select All at the top.
    ///
    /// Till_Window grew this first and kept it to itself. Approval needs it on two different
    /// windows, so it lives here instead of being written a third and fourth time.
    ///
    /// Two things it deliberately does NOT do:
    ///
    ///   - It does not load the month list when the screen opens. The list costs a query,
    ///     and the approval screens open with none. The list is fetched the first time the
    ///     drop-down is opened, and only then.
    ///   - It does not reload on every tick. A user choosing four months would fire four
    ///     queries while still deciding. The caller is told once, when the drop-down closes.
    /// </summary>
    public class PeriodPicker
    {
        /// <summary>The Select All row. Ticking it ticks every month, unticking clears them.</summary>
        public const string SelectAllCaption = "Select All";

        public class Row
        {
            public string Period { get; set; }      // "yyyy-MM", or SelectAllCaption
            public string Display { get; set; }     // what the list shows
            public bool Check_Status { get; set; }
            public bool IsSelectAll { get { return Period == SelectAllCaption; } }
        }

        private readonly ComboBox _combo;
        private readonly Func<DataTable> _load;     // Period, Movements
        private readonly Action _onClosed;

        private List<Row> _rows = new List<Row>();
        private bool _loaded;
        private bool _dirty;
        private bool _syncing;                      // set while Select All is driving the others

        public PeriodPicker(ComboBox combo, Func<DataTable> load, Action onClosed)
        {
            _combo = combo;
            _load = load;
            _onClosed = onClosed;

            _combo.DropDownOpened += DropDownOpened;
            _combo.DropDownClosed += DropDownClosed;
        }

        /// <summary>The ticked months, newest first. Empty means no month restriction.</summary>
        public List<string> Selected()
        {
            return _rows.Where(r => !r.IsSelectAll && r.Check_Status)
                        .Select(r => r.Period)
                        .ToList();
        }

        public bool AnySelected()
        {
            return _rows.Any(r => !r.IsSelectAll && r.Check_Status);
        }

        /// <summary>
        /// Tick the newest month, which is the first row - the list arrives newest first.
        /// The till screen opens on it.
        /// </summary>
        public void SelectNewest()
        {
            Row first = _rows.FirstOrDefault(r => !r.IsSelectAll);
            if (first == null)
                return;

            foreach (Row r in _rows)
                r.Check_Status = false;
            first.Check_Status = true;
            Rebind();
        }

        /// <summary>Untick everything. Used when the user types a date by hand instead.</summary>
        public void Clear()
        {
            foreach (Row r in _rows)
                r.Check_Status = false;
            Rebind();
        }

        /// <summary>
        /// Called by the window when a tick box is clicked. Select All ticks or clears the
        /// whole list; any other row unticks Select All if it no longer holds.
        /// </summary>
        public void Ticked(Row row)
        {
            if (_syncing || row == null)
                return;

            _syncing = true;
            try
            {
                if (row.IsSelectAll)
                {
                    foreach (Row r in _rows)
                        if (!r.IsSelectAll)
                            r.Check_Status = row.Check_Status;
                }
                else
                {
                    Row all = _rows.FirstOrDefault(r => r.IsSelectAll);
                    if (all != null)
                        all.Check_Status = _rows.Where(r => !r.IsSelectAll).All(r => r.Check_Status)
                                        && _rows.Any(r => !r.IsSelectAll);
                }
            }
            finally { _syncing = false; }

            Rebind();
            _dirty = true;
        }

        private void DropDownOpened(object sender, EventArgs e)
        {
            if (_loaded)
                return;

            // First look at the list is the first time it is worth a query.
            Fill(_load());
            _loaded = true;
        }

        private void DropDownClosed(object sender, EventArgs e)
        {
            if (!_dirty)
                return;

            _dirty = false;
            if (_onClosed != null)
                _onClosed();
        }

        /// <summary>
        /// Columns: Period ("yyyy-MM"), Movements (int). A host that already has the list -
        /// the till screen gets it inside the batch that fills the grid - calls this instead,
        /// and the drop-down will not go and fetch it again.
        /// </summary>
        public void Fill(DataTable periods)
        {
            _loaded = true;

            HashSet<string> ticked = new HashSet<string>(Selected());

            _rows = new List<Row> {
                new Row { Period = SelectAllCaption, Display = SelectAllCaption, Check_Status = false }
            };

            if (periods != null)
            {
                foreach (DataRow dr in periods.Rows)
                {
                    string period = Convert.ToString(dr["Period"]);
                    if (string.IsNullOrWhiteSpace(period))
                        continue;

                    _rows.Add(new Row
                    {
                        Period = period,
                        Display = period + "   (" + Convert.ToString(dr["Movements"]) + ")",
                        Check_Status = ticked.Contains(period)
                    });
                }
            }

            Row all = _rows[0];
            all.Check_Status = _rows.Count > 1 && _rows.Skip(1).All(r => r.Check_Status);

            Rebind();
        }

        private void Rebind()
        {
            _combo.ItemsSource = null;
            _combo.ItemsSource = _rows;
            ShowCaption();
        }

        /// <summary>What the closed combo box reads.</summary>
        private void ShowCaption()
        {
            int months = _rows.Count(r => !r.IsSelectAll);
            int picked = _rows.Count(r => !r.IsSelectAll && r.Check_Status);

            if (picked == 0)
                _combo.Text = "";
            else if (picked == 1)
                _combo.Text = _rows.First(r => !r.IsSelectAll && r.Check_Status).Period;
            else if (picked == months)
                _combo.Text = SelectAllCaption;
            else
                _combo.Text = picked + " months";
        }
    }
}
