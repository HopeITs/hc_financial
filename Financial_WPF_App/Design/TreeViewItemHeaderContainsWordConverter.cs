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
namespace Financial_WPF_App
{
    internal class TreeViewItemHeaderContainsWordConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return (SolidColorBrush)Application.Current.Resources["DarkGray"]; // Default color
            }

            string header = ExtractTextFromHeader(value); // 🔥 Extract text properly
            string[] keywords = parameter.ToString().Split('|'); // Supports multiple keywords

            var keywordColors = new Dictionary<string, SolidColorBrush>
            {
                { "Micro", (SolidColorBrush)Application.Current.Resources["HopeRed"] },
                { "Exchange", (SolidColorBrush)Application.Current.Resources["Orange"] },
                { "Cash", (SolidColorBrush)Application.Current.Resources["DarkBlue"] },
                { "Bond", (SolidColorBrush)Application.Current.Resources["GreenBlue"] }
            };

            // Get the current TreeViewItem
            TreeViewItem currentItem = GetTreeViewItemFromHeader(value);
            if (currentItem == null)
            {
                return (SolidColorBrush)Application.Current.Resources["DarkGray"];
            }

            // Step 1: Check all ancestors for keywords
            if (HasKeywordInAncestors(currentItem, keywords, keywordColors, out SolidColorBrush foundColor))
            {
                return foundColor;
            }

            // Step 2: If no parent has a keyword, check the current item
            foreach (var keyword in keywords)
            {
                if (header.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return keywordColors[keyword];
                }
            }
            return (SolidColorBrush)Application.Current.Resources["DarkGray"];
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
            else if (header is Panel panel) // 🔥 If Header is a DockPanel/StackPanel
            {
                foreach (var child in panel.Children)
                {
                    if (child is TextBlock textBlock)
                    {
                        return textBlock.Text; // Extract text from a TextBlock
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
