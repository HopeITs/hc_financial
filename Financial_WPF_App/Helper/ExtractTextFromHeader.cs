using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Financial_WPF_App.Helper
{
    public static class HeaderTextExtractor
    {
        public static string ExtractText(object header)
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
                    if (child is TextBlock childTextBlock)
                    {
                        return childTextBlock.Text; // Extract text from the child TextBlock
                    }
                }
            }
            return string.Empty;
        }
    }

}
