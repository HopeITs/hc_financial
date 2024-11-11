using System.Collections.Generic;

namespace Financial_WPF_App.Helper
{
    public class TreeViewItemState
    {
        public string Header { get; set; }
        public List<TreeViewItemState> Children { get; set; }
        public bool IsExpanded { get; set; }

    }
}
