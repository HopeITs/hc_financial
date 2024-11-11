using System.Collections.Generic;
using System.Windows.Controls;

namespace Financial_WPF_App.Helper
{
    public static class TreeViewExtensions
    {
        public static List<TreeViewItemState> GetExpandedItems(this System.Windows.Controls.TreeView treeView)
        {
            var expandedItems = new List<TreeViewItemState>();
            foreach (var item in treeView.Items)
            {
                if (item is TreeViewItem treeViewItem)
                {
                    var treeViewItemState = new TreeViewItemState
                    {
                        Header = treeViewItem.Header.ToString(),
                        IsExpanded = treeViewItem.IsExpanded,
                        Children = GetExpandedItems(treeViewItem)
                    };
                    expandedItems.Add(treeViewItemState);
                }
            }
            return expandedItems;
        }

        private static List<TreeViewItemState> GetExpandedItems(TreeViewItem parentItem)
        {
            var expandedItems = new List<TreeViewItemState>();
            foreach (var item in parentItem.Items)
            {
                if (item is TreeViewItem treeViewItem)
                {
                    var treeViewItemState = new TreeViewItemState
                    {
                        Header = treeViewItem.Header.ToString(),
                        IsExpanded = treeViewItem.IsExpanded,
                        Children = GetExpandedItems(treeViewItem)
                    };
                    expandedItems.Add(treeViewItemState);
                }
            }
            return expandedItems;
        }

        public static void RestoreExpandedState(this System.Windows.Controls.TreeView treeView, List<TreeViewItemState> expandedItems)
        {
            foreach (var expandedItem in expandedItems)
            {
                RestoreExpandedState(treeView.Items, expandedItem);
            }
        }

        private static void RestoreExpandedState(ItemCollection items, TreeViewItemState expandedItem)
        {
            foreach (var item in items)
            {
                if (item is TreeViewItem treeViewItem && treeViewItem.Header.ToString() == expandedItem.Header)
                {
                    treeViewItem.IsExpanded = expandedItem.IsExpanded;
                    RestoreExpandedState(treeViewItem.Items, expandedItem.Children);
                    break;
                }
            }
        }

        private static void RestoreExpandedState(ItemCollection items, List<TreeViewItemState> expandedItems)
        {
            foreach (var expandedItem in expandedItems)
            {
                RestoreExpandedState(items, expandedItem);
            }
        }
    }
}
