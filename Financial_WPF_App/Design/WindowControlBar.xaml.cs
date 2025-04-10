using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using Window = System.Windows.Window;
namespace Financial_WPF_App
{
    /// <summary>
    /// Interaction logic for WindowControlBar.xaml
    /// </summary>
    /// 
    // Define the Event for Expander
    
    public partial class WindowControlBar : System.Windows.Controls.UserControl
    {
        public event Action<WindowState> WindowStateChanged; // Event to notify MainWindow
        private WindowState _customWindowState;
        private bool isMaximized = false;
        private Rect previousWindowRect;
        public WindowControlBar()
        {
            InitializeComponent();
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                _customWindowState = parentWindow.WindowState;
            }
            else
            {
                _customWindowState = WindowState.Maximized;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            window.WindowState = WindowState.Minimized;
            
        }


        private Rect previousBounds;
        private double restoreWidth = 1280;  // Set desired restore size
        private double restoreHeight = 700;
        private Rect GetWorkingArea(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            var screen = Screen.FromHandle(handle);

            // Get DPI scale
            var source = HwndSource.FromHwnd(handle);
            double dpiX = 1.0, dpiY = 1.0;
            if (source != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            // Convert WinForms pixels to WPF units
            var workingArea = screen.WorkingArea;
            return new Rect(
                workingArea.Left / dpiX,
                workingArea.Top / dpiY,
                workingArea.Width / dpiX,
                workingArea.Height / dpiY
            );
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            Rect workingArea = GetWorkingArea(window);

            if (!isMaximized)
            {
                // Maximize to current screen's working area
                window.Left = workingArea.Left;
                window.Top = workingArea.Top;
                window.Width = workingArea.Width;
                window.Height = workingArea.Height;

                isMaximized = true;
            }
            else
            {
                // Restore centered to current screen
                window.Width = restoreWidth;
                window.Height = restoreHeight;
                window.Left = workingArea.Left + (workingArea.Width - restoreWidth) / 2;
                window.Top = workingArea.Top + (workingArea.Height - restoreHeight) / 2;

                isMaximized = false;
            }
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

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            Rect workingArea = GetWorkingArea(window); // make sure you have this method

            if (!isMaximized)
            {
                window.Left = workingArea.Left;
                window.Top = workingArea.Top;
                window.Width = workingArea.Width;
                window.Height = workingArea.Height;

                isMaximized = true;
            }
            else
            {
                window.Width = restoreWidth;
                window.Height = restoreHeight;
                window.Left = workingArea.Left + (workingArea.Width - restoreWidth) / 2;
                window.Top = workingArea.Top + (workingArea.Height - restoreHeight) / 2;

                isMaximized = false;
            }
        }
    }
}
