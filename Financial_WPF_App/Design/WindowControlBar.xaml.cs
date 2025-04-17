using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
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

        public static readonly DependencyProperty IsMaximizedProperty =
        DependencyProperty.Register("IsMaximized", typeof(bool), typeof(WindowControlBar), new PropertyMetadata(true));

        public bool IsMaximized
        {
            get => (bool)GetValue(IsMaximizedProperty);
            set => SetValue(IsMaximizedProperty, value);
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private Rect previousWindowRect;
        private Window parentWindow = (Main_Window_2)System.Windows.Application.Current.MainWindow;
        public WindowControlBar()
        {
            InitializeComponent();
            Resize_btn.Background = (Brush)System.Windows.Application.Current.Resources["ButtonImageMaximize"];
            
            
            DataContext = this;
            
        }
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            window.WindowState = WindowState.Minimized;
            
        }


        private Rect previousBounds;
        public double restoreWidth = 1280;  // Set desired restore size
        public double restoreHeight = 700;
        private Rect GetWorkingArea()
        {
            var handle = new WindowInteropHelper(parentWindow).Handle;
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

        public void Resize_Click(object sender, RoutedEventArgs e)
        {
            resize_function();
        }

        public void resize_function()
        {
            Rect workingArea = GetWorkingArea();
            if (!IsMaximized)
            {
                // Maximize to current screen's working area
                parentWindow.Left = workingArea.Left;
                parentWindow.Top = workingArea.Top;
                parentWindow.Width = workingArea.Width;
                parentWindow.Height = workingArea.Height;

                IsMaximized = true;
                Resize_btn.Background = (Brush)System.Windows.Application.Current.Resources["ButtonImageMaximize"];
            }
            else
            {
                // Restore centered to current screen
                parentWindow.Width = restoreWidth;
                parentWindow.Height = restoreHeight;
                parentWindow.Left = workingArea.Left + (workingArea.Width - restoreWidth) / 2;
                parentWindow.Top = workingArea.Top + (workingArea.Height - restoreHeight) / 2;

                IsMaximized = false;
                Resize_btn.Background = (Brush)System.Windows.Application.Current.Resources["ButtonImageRestore"];
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                resize_function();
            }
            else
            {
                parentWindow.DragMove();
                Rect workingArea = GetWorkingArea();
                parentWindow.Width = restoreWidth;
                parentWindow.Height = restoreHeight;
                parentWindow.Left = workingArea.Left + (workingArea.Width - restoreWidth) / 2;
                parentWindow.Top = workingArea.Top + (workingArea.Height - restoreHeight) / 2;

                IsMaximized = false;
                Resize_btn.Background = (Brush)System.Windows.Application.Current.Resources["ButtonImageRestore"];
                
            }
            //WindowUtils.TitleBar_MouseDown(this, e);
        }
    }
}
