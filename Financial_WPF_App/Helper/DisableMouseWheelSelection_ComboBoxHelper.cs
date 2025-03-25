using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Financial_WPF_App.helper
{
    public static class DisableMouseWheelSelection_ComboBoxHelper
    {
        public static bool GetDisableMouseWheelSelection(DependencyObject obj)
        {
            return (bool)obj.GetValue(DisableMouseWheelSelectionProperty);
        }

        public static void SetDisableMouseWheelSelection(DependencyObject obj, bool value)
        {
            obj.SetValue(DisableMouseWheelSelectionProperty, value);
        }

        public static readonly DependencyProperty DisableMouseWheelSelectionProperty =
            DependencyProperty.RegisterAttached(
                "DisableMouseWheelSelection",
                typeof(bool),
                typeof(DisableMouseWheelSelection_ComboBoxHelper),
                new UIPropertyMetadata(false, OnDisableMouseWheelSelectionChanged));

        private static void OnDisableMouseWheelSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox)
            {
                if ((bool)e.NewValue)
                {
                    comboBox.PreviewMouseWheel += ComboBox_PreviewMouseWheel;
                }
                else
                {
                    comboBox.PreviewMouseWheel -= ComboBox_PreviewMouseWheel;
                }
            }
        }

        private static void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }
    }
}
