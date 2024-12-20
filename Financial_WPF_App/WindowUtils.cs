using Financial_WPF_App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
    }
}
