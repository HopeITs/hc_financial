using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace Financial_WPF_App.helper
{
    public static class ComboBoxBehaviorManager
    {
        // Define the attached property for the behavior
        public static readonly DependencyProperty AttachMouseClickBehaviorProperty =
            DependencyProperty.RegisterAttached(
                "AttachMouseClickBehavior",
                typeof(bool),
                typeof(ComboBoxBehaviorManager),
                new PropertyMetadata(false, OnAttachMouseClickBehaviorChanged));

        // Getter for the attached property
        public static bool GetAttachMouseClickBehavior(DependencyObject obj)
        {
            return (bool)obj.GetValue(AttachMouseClickBehaviorProperty);
        }

        // Setter for the attached property
        public static void SetAttachMouseClickBehavior(DependencyObject obj, bool value)
        {
            obj.SetValue(AttachMouseClickBehaviorProperty, value);
        }

        // Called when the property changes
        private static void OnAttachMouseClickBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox)
            {
                bool shouldAttach = (bool)e.NewValue;
                if (shouldAttach)
                {
                    // Attach the MouseClickComboBoxBehavior to the ComboBox
                    var behavior = new MouseClickComboBoxBehavior();
                    Interaction.GetBehaviors(comboBox).Add(behavior);
                }
            }
        }
    }
}
