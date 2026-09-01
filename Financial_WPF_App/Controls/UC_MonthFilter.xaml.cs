using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Financial_WPF_App.Controls
{
    /// <summary>
    /// The month filter shared by the accounts screen and the five approval screens.
    ///
    /// It was written three times before this - the till screen first, then both approval
    /// windows - and that is how the row-counter bug reached six screens: by being copied.
    /// The picker logic lives in Helper.PeriodPicker; this is the markup and the wiring.
    ///
    /// The host supplies <see cref="LoadMonths"/> and handles <see cref="Apply"/>.
    /// </summary>
    public partial class UC_MonthFilter : UserControl
    {
        private Helper.PeriodPicker _picker;
        private bool _settingDates;

        public UC_MonthFilter()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Where the month list comes from. Called the first time the drop-down is opened,
        /// never when the screen loads - the approval screens open with no queries at all.
        /// Must return columns Period ("yyyy-MM") and Movements (int).
        /// </summary>
        public Func<DataTable> LoadMonths { get; set; }

        /// <summary>
        /// Raised when the user has asked for a period: when the month drop-down closes on a
        /// changed selection, or when Apply is pressed for the From/To range. Months need no
        /// Apply on any screen - the button is there for the dates.
        /// </summary>
        public event EventHandler Apply;

        /// <summary>
        /// Raised when the X is pressed. Nothing is selected any more, and the screen should
        /// show nothing rather than everything - an empty filter here means "not asked yet",
        /// not "asked for all of it".
        /// </summary>
        public event EventHandler Cleared;

        /// <summary>
        /// False on the accounts screen, which filters by month alone. True on the approval
        /// screens, which also offer From/To and load nothing until Apply is pressed.
        /// </summary>
        public bool ShowDateRange
        {
            get { return Range_Panel.Visibility == Visibility.Visible; }
            set { Range_Panel.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// File name under Images/Buttons for the Apply button. Each approval tab carries its
        /// own tab colour: Budget dark blue, Micro red, Exchange orange, Personal green.
        /// </summary>
        public string ApplyImage
        {
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                Go_btn.Background = new ImageBrush(
                    new BitmapImage(new Uri("pack://application:,,,/Images/Buttons/" + value)))
                {
                    Stretch = Stretch.Uniform
                };
            }
        }

        /// <summary>Must be called once, after LoadMonths is set.</summary>
        public void Initialise()
        {
            _picker = new Helper.PeriodPicker(
                Period_comboBox,
                () => LoadMonths == null ? new DataTable() : LoadMonths(),
                DropDownFinished);
        }

        /// <summary>The ticked months. Empty means the dates decide, or nothing does.</summary>
        public List<string> SelectedMonths()
        {
            return _picker == null ? new List<string>() : _picker.Selected();
        }

        public bool AnyMonthSelected()
        {
            return _picker != null && _picker.AnySelected();
        }

        /// <summary>Empty means no bound on that side.</summary>
        public string From()
        {
            return FromDate_datepicker.SelectedDate == null
                ? ""
                : ((DateTime)FromDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
        }

        public string To()
        {
            return ToDate_datepicker.SelectedDate == null
                ? ""
                : ((DateTime)ToDate_datepicker.SelectedDate).ToString("yyyy/MM/dd");
        }

        /// <summary>Tick the newest month. The accounts screen opens on it.</summary>
        public void SelectNewest()
        {
            if (_picker != null)
                _picker.SelectNewest();
        }

        /// <summary>
        /// Forget the month list so the next look at the drop-down fetches it again. The
        /// approval screens switch between the waiting rows and the deleted ones, which are
        /// different tables with different counts.
        /// </summary>
        public void InvalidateMonths()
        {
            if (_picker != null)
                _picker.Invalidate();
        }

        /// <summary>Replaces the month list, for a host that already has it to hand.</summary>
        public void Fill(DataTable months)
        {
            if (_picker != null)
                _picker.Fill(months);
        }

        // ------------------------------------------------------------------ internals

        private void DropDownFinished()
        {
            // Picking months is the request, on every screen. The dates are the other way in,
            // so choosing months clears them.
            if (_picker.AnySelected())
                ClearDates();

            Raise();
        }

        private void Period_Checkbox_Item_Clicked(object sender, RoutedEventArgs e)
        {
            CheckBox box = sender as CheckBox;
            if (box == null)
                return;

            Helper.PeriodPicker.Row row = box.DataContext as Helper.PeriodPicker.Row;
            if (row == null)
                return;

            row.Check_Status = box.IsChecked == true;
            _picker.Ticked(row);
        }

        /// <summary>
        /// The X drops the whole period - months and dates both - and the screen empties.
        /// Reloading everything instead would be the one thing nobody pressing a clear button
        /// wants on a table this size.
        /// </summary>
        private void Period_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_picker == null)
                return;

            _picker.Clear();
            ClearDates();

            EventHandler h = Cleared;
            if (h != null)
                h(this, EventArgs.Empty);
        }

        private void ClearDates()
        {
            _settingDates = true;
            FromDate_datepicker.SelectedDate = null;
            ToDate_datepicker.SelectedDate = null;
            _settingDates = false;
        }

        // Months and the range are one filter with two ways in: a date typed by hand means
        // the months no longer describe what is shown.
        private void Date_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_settingDates || _picker == null)
                return;

            _picker.Clear();
        }

        private void Go_btn_Click(object sender, RoutedEventArgs e)
        {
            Raise();
        }

        private void Raise()
        {
            EventHandler h = Apply;
            if (h != null)
                h(this, EventArgs.Empty);
        }
    }
}
