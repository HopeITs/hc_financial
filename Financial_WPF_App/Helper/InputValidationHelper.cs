using System.Text.RegularExpressions;
using System.Windows.Input;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;

namespace Financial_WPF_App.Helper
{
    public static class InputValidationHelper
    {
        private sealed class AmountFormatState
        {
            public bool IsFormatting;
            public string LastValidText = string.Empty;
            public int LastValidCaret;
        }

        private static readonly ConditionalWeakTable<TextBox, AmountFormatState> AmountFormatStates =
            new ConditionalWeakTable<TextBox, AmountFormatState>();

        public static void Decimal_NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text))
            {
                Regex regex = new Regex("[^0-9.]+");
                e.Handled = regex.IsMatch(e.Text);
            }
        }

        public static void Integers_NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text))
            {
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);
            }
        }

        public static void EnableThousandsSeparator(TextBox textBox)
        {
            AmountFormatState existingState;
            if (textBox == null || AmountFormatStates.TryGetValue(textBox, out existingState))
                return;

            var state = new AmountFormatState();
            AmountFormatStates.Add(textBox, state);
            textBox.TextChanged += AmountTextBox_TextChanged;
            FormatAmountTextBox(textBox, state);
        }

        public static bool TryParseFormattedNumber(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return double.TryParse(text.Replace(",", string.Empty),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out value) && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static bool TryParseFormattedDecimal(string text, out decimal value)
        {
            return decimal.TryParse((text ?? string.Empty).Replace(",", string.Empty),
                NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
        }

        public static decimal ParseFormattedDecimal(string text)
        {
            return decimal.Parse(text.Replace(",", string.Empty),
                NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        public static string FormatAmount(string text)
        {
            decimal value;
            return TryParseFormattedDecimal(text, out value)
                ? value.ToString("N2", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            AmountFormatState state;
            if (textBox == null || !AmountFormatStates.TryGetValue(textBox, out state) || state.IsFormatting)
                return;

            FormatAmountTextBox(textBox, state);
        }

        private static void FormatAmountTextBox(TextBox textBox, AmountFormatState state)
        {
            string original = textBox.Text ?? string.Empty;
            int logicalCaret = CountNonSeparators(original, textBox.CaretIndex);
            string formatted;

            if (!TryFormatAmount(original, out formatted))
            {
                state.IsFormatting = true;
                textBox.Text = state.LastValidText;
                textBox.CaretIndex = Math.Min(state.LastValidCaret, textBox.Text.Length);
                state.IsFormatting = false;
                return;
            }

            int formattedCaret = FindCaretIndex(formatted, logicalCaret);
            state.IsFormatting = true;
            if (!string.Equals(original, formatted, StringComparison.Ordinal))
                textBox.Text = formatted;
            textBox.CaretIndex = Math.Min(formattedCaret, textBox.Text.Length);
            state.IsFormatting = false;

            state.LastValidText = textBox.Text;
            state.LastValidCaret = textBox.CaretIndex;
        }

        private static bool TryFormatAmount(string text, out string formatted)
        {
            formatted = string.Empty;
            string normalized = (text ?? string.Empty).Replace(",", string.Empty);
            if (normalized.Length == 0)
                return true;

            int decimalPoint = normalized.IndexOf('.');
            if (decimalPoint != normalized.LastIndexOf('.'))
                return false;

            for (int i = 0; i < normalized.Length; i++)
            {
                if (!char.IsDigit(normalized[i]) && normalized[i] != '.')
                    return false;
            }

            string integerPart = decimalPoint >= 0 ? normalized.Substring(0, decimalPoint) : normalized;
            string decimalPart = decimalPoint >= 0 ? normalized.Substring(decimalPoint) : string.Empty;
            formatted = GroupDigits(integerPart) + decimalPart;
            return true;
        }

        private static string GroupDigits(string digits)
        {
            if (digits.Length <= 3)
                return digits;

            var result = new StringBuilder(digits.Length + (digits.Length - 1) / 3);
            int firstGroupLength = digits.Length % 3;
            if (firstGroupLength == 0)
                firstGroupLength = 3;

            result.Append(digits, 0, firstGroupLength);
            for (int i = firstGroupLength; i < digits.Length; i += 3)
            {
                result.Append(',');
                result.Append(digits, i, 3);
            }
            return result.ToString();
        }

        private static int CountNonSeparators(string text, int end)
        {
            int count = 0;
            for (int i = 0; i < Math.Min(end, text.Length); i++)
            {
                if (text[i] != ',')
                    count++;
            }
            return count;
        }

        private static int FindCaretIndex(string text, int logicalPosition)
        {
            if (logicalPosition <= 0)
                return 0;

            int logicalCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != ',')
                    logicalCount++;
                if (logicalCount == logicalPosition)
                    return i + 1;
            }
            return text.Length;
        }
    }

}
