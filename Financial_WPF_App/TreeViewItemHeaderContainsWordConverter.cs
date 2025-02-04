using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
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

                string header = value.ToString();
                string[] keywords = parameter.ToString().Split('|'); // Supports multiple keywords

                var keywordColors = new Dictionary<string, SolidColorBrush>
        {
            { "Micro", (SolidColorBrush)Application.Current.Resources["HopeRed"] },
            { "Exchange", (SolidColorBrush)Application.Current.Resources["Orange"] },
            { "Cash", (SolidColorBrush)Application.Current.Resources["DarkBlue"] }
        };

                // 🛑 Get the current TreeViewItem
                System.Windows.Controls.TreeViewItem currentItem = GetTreeViewItemFromHeader(value);
                if (currentItem == null)
                {
                    return (SolidColorBrush)Application.Current.Resources["DarkGray"];
                }

                // 🔍 Step 1: Check if the parent has a keyword
                System.Windows.Controls.TreeViewItem parentItem = GetParentTreeViewItem(currentItem);
                if (parentItem != null)
                {
                    string parentHeader = parentItem.Header?.ToString() ?? "";
                    foreach (var keyword in keywords)
                    {
                        if (parentHeader.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return keywordColors[keyword]; // Apply parent's color to all children
                        }
                    }
                }

                // 🔍 Step 2: If parent has no keyword, check if the current header contains a keyword
                foreach (var keyword in keywords)
                {
                    if (header.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return keywordColors[keyword];
                    }
                }

                return (SolidColorBrush)Application.Current.Resources["DarkGray"]; // Default color
            }

            // 🛠 Get TreeViewItem from Header
            private System.Windows.Controls.TreeViewItem GetTreeViewItemFromHeader(object header)
            {
                foreach (var item in FindVisualChildren<System.Windows.Controls.TreeViewItem>(Application.Current.MainWindow))
                {
                    if (item.Header?.ToString() == header?.ToString())
                    {
                        return item;
                    }
                }
                return null;
            }

            // 🔍 Get Parent of a TreeViewItem
            private System.Windows.Controls.TreeViewItem GetParentTreeViewItem(System.Windows.Controls.TreeViewItem item)
            {
                DependencyObject parent = VisualTreeHelper.GetParent(item);
                while (parent != null && !(parent is System.Windows.Controls.TreeViewItem))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                return parent as System.Windows.Controls.TreeViewItem;
            }

            // 🔍 Helper: Find all TreeViewItems
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
