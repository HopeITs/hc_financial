using Financial_WPF_App.Models;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial_WPF_App
{
    public class WindowUtils : Window
    {
        // Close the window with a confirmation dialog
        internal static void CloseWindow(Window window)
        {
            if (window == null) return;
            window.Close();
        }

        // Minimize the window
        internal static void MinimizeWindow(Window window)
        {
            if (window == null) return;

            window.WindowState = WindowState.Minimized;
        }

        internal static void ResizeWindow(Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        public static void TitleBar_MouseDown(Window window, MouseButtonEventArgs e)
        {
            if (window != null && e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    window.DragMove();
                }
                catch (InvalidOperationException)
                {
                    // DragMove can throw an exception if called under certain conditions (e.g., mouse not down anymore).
                }
            }
        }

        public static void UIElementColoring(object content, Brush color)
        {
            if (!(content is DependencyObject parent) || color == null)
                return;

            // Define the target style key
            string targetStyleKey = "statistic_textBlockStyle";
            string buttonStyleKey = "ButtonColoredStyle";
            string labelStyleKey = "NoteStyle";

            // Update TextBlocks in the main content
            UpdateTextBlocksInContainer(parent, color, targetStyleKey);

            // Update Buttons in the main content
            UpdateButtonsInContainer(parent, color, color, buttonStyleKey);

            UpdateLabelsInContainer(parent, color, labelStyleKey);

            //UpdateDGInContainer(parent, color);
            // Find all TabControls inside the window
            foreach (var tabControl in FindVisualChildren<TabControl>(parent))
            {
                // Loop through all TabItems inside the TabControl
                foreach (var tabItem in tabControl.Items)
                {
                    if (tabItem is TabItem tab && tab.Content is DependencyObject tabContent)
                    {
                        UpdateTextBlocksInContainer(tabContent, color, targetStyleKey);
                        UpdateButtonsInContainer(tabContent, new SolidColorBrush(Colors.White), color, buttonStyleKey);
                        UpdateLabelsInContainer(tabContent, color, labelStyleKey);
                        //UpdateDGInContainer(tabContent, color);
                    }
                }
            }
        }

        // Helper method to find all TextBlocks with the target style and update them
        private static void UpdateTextBlocksInContainer(DependencyObject parent, Brush foreground, string styleKey)
        {
            foreach (var textBlock in FindVisualChildren<TextBlock>(parent))
            {
                if (textBlock.Style != null && textBlock.Style.Equals(Application.Current.TryFindResource(styleKey)))
                {
                    textBlock.Foreground = foreground;
                }
            }
        }
        private static void UpdateButtonsInContainer(DependencyObject parent, Brush foreground, Brush background, string styleKey)
        {
            foreach (var button in FindVisualChildren<Button>(parent))
            {
                if (button.Style != null && button.Style.Equals(Application.Current.TryFindResource(styleKey)))
                {
                    button.Background = background;
                }
            }

        }
        private static void UpdateLabelsInContainer(DependencyObject parent, Brush foreground, string styleKey)
        {
            foreach (var label in FindVisualChildren<Label>(parent))
            {
                if (label.Style != null && label.Style.Equals(Application.Current.TryFindResource(styleKey)))
                {
                    label.ClearValue(Label.ForegroundProperty); // Remove style-based Foreground
                    label.Foreground = foreground; // Apply new color
                }
            }
        }

        public static void UpdateDGInContainer(DependencyObject parent, Brush borderColor)
        {
            if (parent == null || borderColor == null)
                return;

            // Iterate through all the header cells in the parent container
            foreach (var headerCell in FindVisualChildren<GridHeaderCellControl>(parent))
            {
                // Set the BorderBrush and BorderThickness for the bottom border
                headerCell.BorderBrush = borderColor;

                // Set the BorderThickness to apply only to the bottom border
                headerCell.BorderThickness = new Thickness(0, 0, 0, 3); // Adjust the bottom thickness if needed
            }
            foreach (var tablesummary in FindVisualChildren<TableSummaryRowControl>(parent))
            {
                tablesummary.Foreground = borderColor;
            }
            foreach (var groupsummary in FindVisualChildren<GroupSummaryRowControl>(parent))
            {
                groupsummary.Foreground = borderColor;
            }
        }


        // Generic method to find all elements of a specific type in a given UI structure
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
