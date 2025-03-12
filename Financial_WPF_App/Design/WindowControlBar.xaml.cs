using Syncfusion.Windows.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
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
