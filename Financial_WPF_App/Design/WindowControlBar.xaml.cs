using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Window = System.Windows.Window;

namespace Financial_WPF_App
{
    /// <summary>
    /// Interaction logic for WindowControlBar.xaml
    /// </summary>
    /// 
    // Define the Event for Expander
    
    public partial class WindowControlBar : UserControl
    {
        public event Action<WindowState> WindowStateChanged; // Event to notify MainWindow
        private WindowState _customWindowState;

        public WindowControlBar()
        {
            InitializeComponent();
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                _customWindowState = parentWindow.WindowState;
            }
        }
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            window.WindowState = WindowState.Minimized;
            
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            // Toggle window state
            _customWindowState = (_customWindowState == WindowState.Normal) ?
                                  WindowState.Maximized :
                                  WindowState.Normal;

            // Raise event for MainWindow
            WindowStateChanged?.Invoke(_customWindowState);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
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
