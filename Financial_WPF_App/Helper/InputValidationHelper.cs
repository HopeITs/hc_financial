using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Financial_WPF_App.Helper
{
    public static class InputValidationHelper
    {
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
    }

}