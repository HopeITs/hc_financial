using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Financial_WPF_App.Controls
{
    /// <summary>
    /// "Show: 50" - how many rows the approval screens fetch.
    ///
    /// It replaces the month picker and the From/To range on those screens. An approval
    /// queue is worked through rather than read by date, so the useful question is how
    /// much of it to bring back, not which part of the calendar it came from. The grids
    /// were already ordered newest first, so a limit takes the newest N.
    ///
    /// Choosing a number is the request. There is no Apply button, and therefore no way
    /// to leave a choice half-made.
    /// </summary>
    public partial class UC_RowLimitFilter : UserControl
    {
        /// <summary>What "Select All" passes to the model: no limit.</summary>
        public const int NoLimit = 0;

        private const int DefaultLimit = 50;

        public class Choice
        {
            public string Caption { get; set; }
            public int Rows { get; set; }        // 0 = every row
        }

        private bool _loading;

        public UC_RowLimitFilter()
        {
            InitializeComponent();

            List<Choice> choices = new List<Choice>
            {
                new Choice { Caption = "50",         Rows = 50 },
                new Choice { Caption = "100",        Rows = 100 },
                new Choice { Caption = "200",        Rows = 200 },
                new Choice { Caption = "500",        Rows = 500 },
                new Choice { Caption = "1000",       Rows = 1000 },
                new Choice { Caption = "Select All", Rows = NoLimit },
            };

            _loading = true;
            Limit_comboBox.ItemsSource = choices;
            Limit_comboBox.SelectedIndex = 0;      // 50, the screen's starting state
            _loading = false;
        }

        /// <summary>Raised when a different number is chosen.</summary>
        public event EventHandler Changed;

        /// <summary>The chosen row count. 0 means every row.</summary>
        public int Limit
        {
            get
            {
                Choice c = Limit_comboBox.SelectedItem as Choice;
                return c == null ? DefaultLimit : c.Rows;
            }
        }

        private void Limit_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The list is built in the constructor; that first selection is not a request.
            if (_loading)
                return;

            EventHandler h = Changed;
            if (h != null)
                h(this, EventArgs.Empty);
        }
    }
}
