using Microsoft.Xaml.Behaviors;
using System;
using System.Data;
using System.Windows.Controls;
using System.Windows.Input;

namespace Financial_WPF_App.helper
{
    /// <summary>
    /// Filters an editable ComboBox bound to a <see cref="DataView"/> using
    /// <see cref="DataView.RowFilter"/> (contains match on DisplayMemberPath).
    /// This is the supported filter API for DataView sources; ICollectionView.Filter is not.
    /// </summary>
    public class DataViewContainsFilterComboBoxBehavior : Behavior<ComboBox>
    {
        private bool _suppressFilter;

        protected override void OnAttached()
        {
            base.OnAttached();

            var comboBox = AssociatedObject;
            comboBox.IsEditable = true;
            comboBox.IsTextSearchEnabled = false; // contains filtering replaces prefix TextSearch
            comboBox.StaysOpenOnEdit = true;

            comboBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(OnTextChanged), true);
            comboBox.DropDownOpened += OnDropDownOpened;
            comboBox.SelectionChanged += OnSelectionChanged;
            comboBox.PreviewKeyDown += OnPreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            var comboBox = AssociatedObject;
            if (comboBox != null)
            {
                comboBox.RemoveHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));
                comboBox.DropDownOpened -= OnDropDownOpened;
                comboBox.SelectionChanged -= OnSelectionChanged;
                comboBox.PreviewKeyDown -= OnPreviewKeyDown;
                ClearRowFilter(comboBox);
            }

            base.OnDetaching();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var comboBox = AssociatedObject;
            if (comboBox == null || !comboBox.IsEditable || comboBox.IsDropDownOpen)
                return;

            if (e.Key == Key.Tab || e.Key == Key.Escape || e.Key == Key.Enter)
                return;

            if (e.Key == Key.Down || e.Key == Key.Space || IsTextInputKey(e.Key))
                comboBox.IsDropDownOpen = true;
        }

        private static bool IsTextInputKey(Key key)
        {
            return key == Key.Back
                || key == Key.Delete
                || (key >= Key.A && key <= Key.Z)
                || (key >= Key.D0 && key <= Key.D9)
                || (key >= Key.NumPad0 && key <= Key.NumPad9)
                || key == Key.OemMinus
                || key == Key.OemPeriod
                || key == Key.Space;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressFilter)
                return;

            // After a selection, restore the full list for the next search.
            _suppressFilter = true;
            try
            {
                ClearRowFilter(AssociatedObject);
            }
            finally
            {
                _suppressFilter = false;
            }
        }

        private void OnDropDownOpened(object sender, EventArgs e)
        {
            var comboBox = AssociatedObject;
            if (comboBox?.SelectedItem == null)
                return;

            string selectedText = GetDisplayText(comboBox.SelectedItem, comboBox.DisplayMemberPath);
            if (string.Equals(comboBox.Text, selectedText, StringComparison.CurrentCultureIgnoreCase))
                ClearRowFilter(comboBox);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressFilter)
                return;

            var comboBox = AssociatedObject;
            if (comboBox == null || !comboBox.IsEditable || !comboBox.IsKeyboardFocusWithin)
                return;

            string text = comboBox.Text ?? string.Empty;

            if (comboBox.SelectedItem != null)
            {
                string selectedText = GetDisplayText(comboBox.SelectedItem, comboBox.DisplayMemberPath);
                if (string.Equals(text, selectedText, StringComparison.CurrentCultureIgnoreCase))
                {
                    ClearRowFilter(comboBox);
                    return;
                }
            }

            ApplyContainsFilter(comboBox, text);

            if (!comboBox.IsDropDownOpen && !string.IsNullOrWhiteSpace(text))
                comboBox.IsDropDownOpen = true;
        }

        private static void ApplyContainsFilter(ComboBox comboBox, string searchText)
        {
            var dataView = comboBox.ItemsSource as DataView;
            if (dataView == null)
                return;

            string column = comboBox.DisplayMemberPath;
            if (string.IsNullOrWhiteSpace(column))
                return;

            string filter = (searchText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(filter))
            {
                dataView.RowFilter = string.Empty;
                return;
            }

            // DataView.RowFilter LIKE is the supported contains-filter for this ItemsSource type.
            dataView.RowFilter = string.Format(
                "Convert([{0}], 'System.String') LIKE '%{1}%'",
                column,
                EscapeRowFilterLikeValue(filter));
        }

        private static void ClearRowFilter(ComboBox comboBox)
        {
            var dataView = comboBox?.ItemsSource as DataView;
            if (dataView != null)
                dataView.RowFilter = string.Empty;
        }

        private static string EscapeRowFilterLikeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");
        }

        private static string GetDisplayText(object item, string displayMemberPath)
        {
            if (item == null)
                return string.Empty;

            var dataRowView = item as DataRowView;
            if (dataRowView != null)
            {
                if (!string.IsNullOrEmpty(displayMemberPath)
                    && dataRowView.Row.Table.Columns.Contains(displayMemberPath))
                {
                    return Convert.ToString(dataRowView[displayMemberPath]) ?? string.Empty;
                }

                return Convert.ToString(dataRowView.Row[0]) ?? string.Empty;
            }

            return Convert.ToString(item) ?? string.Empty;
        }
    }
}
