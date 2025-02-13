using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Controls;
using Financial_WPF_App.Design;


namespace Financial_WPF_App
{
    internal class TreeViewItemHeaderContainsWordConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return (SolidColorBrush)Application.Current.Resources["DarkGray"];
            }

            // Try to get the TreeViewItem from the value
            TreeViewItem currentItem = FindParentTreeViewItem(value as DependencyObject);

            if (currentItem == null)
            {
                return (SolidColorBrush)Application.Current.Resources["DarkGray"];
            }

            if (currentItem.Tag is tagItemInfo itemInfo)
            {
                string category = itemInfo.Category;
                string header = ExtractTextFromHeader(currentItem.Header);

                // Example: Map the category to a color
                var categoryColors = new Dictionary<string, SolidColorBrush>
                    {
                        { "Micro", (SolidColorBrush)Application.Current.Resources["HopeRed"] },
                        { "Budget", (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"] },
                        { "Exchange", (SolidColorBrush)Application.Current.Resources["Orange"] },
                        { "Cash", (SolidColorBrush)Application.Current.Resources["DarkgrayishViolet"] },
                        { "Personal", (SolidColorBrush)Application.Current.Resources["PastelGreen"] },
                        { "Default", (SolidColorBrush)Application.Current.Resources["DarkGray"] }
                    };

                if (categoryColors.TryGetValue(category, out SolidColorBrush color))
                {
                    return color;
                }
            }


            return (SolidColorBrush)Application.Current.Resources["DarkGray"];
        }

        private TreeViewItem FindParentTreeViewItem(DependencyObject obj)
        {
            while (obj != null && !(obj is TreeViewItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            return obj as TreeViewItem;
        }


        private string ExtractTextFromHeader(object header)
        {
            if (header is string text)
            {
                return text; // Already a string
            }
            else if (header is ContentControl contentControl && contentControl.Content is string contentText)
            {
                return contentText; // Extract content from a Label, Button, etc.
            }
            else if (header is TextBlock textBlock)
            {
                return textBlock.Text; // Extract text from a TextBlock
            }
            else if (header is Panel panel) // If Header is a DockPanel/StackPanel
            {
                foreach (var child in panel.Children)
                {
                    if (child is TextBlock childTextBlock) // Rename the variable to avoid conflicts
                    {
                        return childTextBlock.Text; // Extract text from the child TextBlock
                    }
                }
            }
            return string.Empty;
        }



        private bool HasKeywordInAncestors(System.Windows.Controls.TreeViewItem item, string[] keywords, Dictionary<string, SolidColorBrush> keywordColors, out SolidColorBrush foundColor)
        {
            System.Windows.Controls.TreeViewItem parentItem = GetParentTreeViewItem(item);

            while (parentItem != null) // Keep moving up the hierarchy
            {
                string parentHeader = parentItem.Header?.ToString() ?? "";

                foreach (var keyword in keywords)
                {
                    if (parentHeader.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foundColor = keywordColors.ContainsKey(keyword) ? keywordColors[keyword] : (SolidColorBrush)Application.Current.Resources["DarkGray"];
                        return true; // Found a match
                    }
                }

                parentItem = GetParentTreeViewItem(parentItem); // Move up
            }

            foundColor = null;
            return false;
        }

        // Get TreeViewItem from Header
        private System.Windows.Controls.TreeViewItem GetTreeViewItemFromHeader(object header)
        {
            foreach (var item in FindVisualChildren<System.Windows.Controls.TreeViewItem>(Application.Current.MainWindow))
            {
                string itemHeader = item.Header?.ToString();

                if (itemHeader == header?.ToString())
                {
                    return item;
                }
            }
            return null;
        }

        // Get Parent of a TreeViewItem
        private System.Windows.Controls.TreeViewItem GetParentTreeViewItem(System.Windows.Controls.TreeViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (parent != null && !(parent is System.Windows.Controls.TreeViewItem))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as System.Windows.Controls.TreeViewItem;
        }

        // Helper: Find all TreeViewItems
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T tChild)
                {
                    yield return tChild;
                }
                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
