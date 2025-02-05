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
        public event Action<double> ResizeExpanderRequested;
        public WindowControlBar()
        {
            InitializeComponent();
        }
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            window.WindowState = WindowState.Minimized;
            
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);

            if (window != null)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
                if (window.WindowState == WindowState.Normal)
                {
                    // Raise the event and pass the new width (e.g., 300)
                    ResizeExpanderRequested?.Invoke(50);
                }
                // Check if the window is minimized
                //if (window.WindowState == WindowState.Minimized)
                //{
                //    // Resize the window when minimized
                //    this.Width = 1330;  // Set desired width
                //    this.Height = 700; // Set desired height
                //}
                //else if (window.WindowState == WindowState.Normal)
                //{
                //    // Restore the original size when window is restored
                //    this.Width = 1920;  // Original width
                //    this.Height = 1021; // Original height
                //}
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
        
    }
}
