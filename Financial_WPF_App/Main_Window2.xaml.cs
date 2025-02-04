using Financial_WPF_App.Helper;
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using Financial_WPF_App.Views;
using Syncfusion.Data.Extensions;
using Syncfusion.Windows.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Login = Financial_WPF_App.Models.Login;

namespace Financial_WPF_App
{
    public partial class Main_Window_2 : Window
    {

        public Main_Window_2()
        {
            InitializeComponent();


            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(30);
            timer.Tick += Timer_Tick;
            timer.Start();
            //this.WindowState = WindowState.Minimized;
            myExpander.IsExpanded = false;
            On_Load();
            // Subscribe to the event
            ControlBar.ResizeExpanderRequested += OnResizeExpanderRequested;
        }
        
        private void On_Load()
        {
            try
            {
                Window window = new Login_Window2(this);
                sub = new SubProject();
                Acc = new Account();
                c = new City();
                t = new Till();
                fb = new FiscalBond();
                v = new Models.Version();

                Shutdown_Mode = new ShutdownMode();
                bool? result = window.ShowDialog();
                if (result ?? true)
                {
                    Username_label.Content = Settings.Default.username;
                    Update_Online_Button();
                    window.WindowState = WindowState.Maximized;
                    Login.Shutdown_Mode = ShutdownMode.OnMainWindowClose;
                    Fill_TreeView(false);

                    Show_Hide();

                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Show_Hide()
        {
            string role = Properties.Settings.Default.Role;
            if (role == "Head Office")
            {

            }
            else if (role == "Project Manager")
            {
                AddNew_MenuItem.Visibility = Visibility.Collapsed;
                //Employee_MenuItem.Visibility= Visibility.Collapsed;
                Reports_MenuItem.Visibility = Visibility.Collapsed;
                Log_MenuItem.Visibility = Visibility.Collapsed;
                Accounting_Entries_MenuItem.Visibility = Visibility.Collapsed;
            }
            else if (role == "Financial Manager")
            {
                AddNew_MenuItem.Visibility = Visibility.Collapsed;
                //Employee_MenuItem.Visibility = Visibility.Collapsed;
                Reports_MenuItem.Visibility = Visibility.Collapsed;
                Log_MenuItem.Visibility = Visibility.Collapsed;
            }
            else if (role == "Accountant")
            {
                AddNew_MenuItem.Visibility = Visibility.Collapsed;
                //Employee_MenuItem.Visibility = Visibility.Collapsed;
                Reports_MenuItem.Visibility = Visibility.Collapsed;
                Log_MenuItem.Visibility = Visibility.Collapsed;
            }
            else if (role == "Cashier")
            {
                AddNew_MenuItem.Visibility = Visibility.Collapsed;
                Employee_MenuItem.Visibility = Visibility.Collapsed;
                Reports_MenuItem.Visibility = Visibility.Collapsed;
                Log_MenuItem.Visibility = Visibility.Collapsed;
                Accounting_Entries_MenuItem.Visibility = Visibility.Collapsed;
                Approval_MenuItem.Visibility = Visibility.Collapsed;
            }
            else if (role == "Employee")
            {
                AddNew_MenuItem.Visibility = Visibility.Collapsed;
                Employee_MenuItem.Visibility = Visibility.Collapsed;
                Reports_MenuItem.Visibility = Visibility.Collapsed;
                Log_MenuItem.Visibility = Visibility.Collapsed;
                Accounting_Entries_MenuItem.Visibility = Visibility.Collapsed;
                Approval_MenuItem.Visibility = Visibility.Collapsed;
            }
            else if (role == "Donor")
            {
                AddNew_MenuItem.Visibility = Visibility.Collapsed;
                //Employee_MenuItem.Visibility = Visibility.Collapsed;
                Log_MenuItem.Visibility = Visibility.Collapsed;
                Accounting_Entries_MenuItem.Visibility = Visibility.Collapsed;
                Approval_MenuItem.Visibility = Visibility.Collapsed;
            }
        }

        private bool isDragging = false;
        private System.Windows.Point mouseDownPosition;
        private DispatcherTimer timer;
        private ToggleButton _expanderToggleButton;
        private ImageBrush defaultBackground = new ImageBrush();
        SubProject sub; Account Acc; Models.Version v;
        City c; Till t; FiscalBond fb;
        public static ShutdownMode Shutdown_Mode;
        int RClicked_ID = -1; string RClicked_Name = "";

        public DataSet AllAccounts_ds;

        DataTable Projects;
        DataTable ExchangeAccounts;
        DataTable Fiscalbonds;
        DataTable CashAccounts;
        DataTable PersonalAccounts;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea; // Gets the area excluding the taskbar
            this.MaxWidth = workArea.Width + 5;
            this.MaxHeight = workArea.Height + 5;
        }

        public void Create_tabItem(Window window, string header, System.Windows.Media.Brush foreground)
        {
            TabItemExt oTab = new TabItemExt();
            oTab.Content = window.Content;
            oTab.Header = header;
            oTab.Foreground = foreground;
            // Store the original Foreground color in the Tag property
            oTab.Tag = foreground;
            oTab.FontFamily = new System.Windows.Media.FontFamily("Rubik");
            oTab.FlowDirection = FlowDirection.LeftToRight;
            // Add the TabItemExt to the TabControlExt
            main_tabControl.Items.Add(oTab);
            main_tabControl.SelectedIndex = main_tabControl.Items.Count - 1;

            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WindowUtils.UpdateTextBlocksForeground(oTab.Content, foreground);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            defaultBackground.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/MainForm/Home_Background2_new.png"));
            defaultBackground.Stretch = Stretch.UniformToFill;
            TransformGroup transformGroup = new TransformGroup();
            ScaleTransform scaleTransform = new ScaleTransform
            {
                ScaleX = 1.05, // Scale horizontally by 0.5x
                ScaleY = 1.05,  // Scale vertically by 0.5x
                CenterX = 0.5,
                CenterY = 0.5
            };
            TranslateTransform translateTransform = new TranslateTransform
            {
                X = -0.55,
                Y = -0.55
            };
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTransform);
            defaultBackground.Transform = transformGroup;
            // Check if any tab is selected
            if (main_tabControl.SelectedIndex >= 0)
            {
                // Change the background to a solid color when a tab is selected
                main_tabControl.Background = (SolidColorBrush)Application.Current.Resources["LightGray1"]; // Custom background color
            }
            else
            {
                // Revert to the default background image when no tab is selected
                main_tabControl.Background = defaultBackground;
            }
            
            // Iterate through all tabs
            foreach (var item in main_tabControl.Items)
            {
                if (item is TabItemExt tab)
                {
                    // Check if the tab is selected
                    if (tab.IsSelected)
                    {
                        // Set the foreground of the selected tab to its original color (stored in Tag)
                        tab.Foreground = (System.Windows.Media.Brush)tab.Tag;
                        main_tabControl.TabItemSelectedForeground = tab.Foreground;
                    }
                    else
                    {
                        // Set the foreground of non-selected tabs to gray
                        tab.Foreground = (SolidColorBrush)Application.Current.Resources["MidGray"];
                    }
                }
            }
            

        }

        private void Online_Button_Click(object sender, RoutedEventArgs e)
        {
            //Online_Working_Loop_BeginStoryboard1.Storyboard.RepeatBehavior = RepeatBehavior.Forever;
        }

        private void Update_Online_Button()
        {
            // Update the button's background image
            if (Settings.Default.Online)
            {
                Online_Button.Background = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/Buttons/online.png", UriKind.Absolute)),
                    Stretch = Stretch.Uniform
                };
                Online_Button.ToolTip = "Online Database";
            }
            else
                {
                    Online_Button.Background = new ImageBrush
                    {
                        ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/Buttons/online_hover.png", UriKind.Absolute)),
                        Stretch = Stretch.Uniform
                    };
                    Online_Button.ToolTip = "Offline Database";
                }
            }

        private void Notification_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Profile_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window window = new Login_Window2(this);
                bool? result = window.ShowDialog();
                if (result ?? true)
                {
                    //Get_User_Profile();
                    //Check_Notifications();
                    Username_label.Content = Settings.Default.username;
                    Update_Online_Button();
                    //Check_User_Role();
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void Main_Window2_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (Login.Shutdown_Mode == ShutdownMode.OnMainWindowClose)
                {
                    if (MessageBox.Show("Are you sure you want to close the program?", "", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Main_Window2_Closed(object sender, EventArgs e)
        {
            Thread.Sleep(500);
            Application.Current.Shutdown();
        }
        private void myExpander_Loaded(object sender, RoutedEventArgs e)
        {
            _expanderToggleButton = FindChild<ToggleButton>(myExpander);

            if (_expanderToggleButton != null)
            {
                // Set initial appearance
                // Replace the ToggleButton template with a new one
                _expanderToggleButton.Template = CreateCustomToggleButtonTemplate();
                // Set the cursor to "Hand"
                _expanderToggleButton.Cursor = Cursors.Hand;
            }
        }
        private ControlTemplate CreateCustomToggleButtonTemplate()
        {
            // Create a new ControlTemplate for the ToggleButton
            var template = new ControlTemplate(typeof(ToggleButton));
            var grid = new FrameworkElementFactory(typeof(Grid));

            var image = new FrameworkElementFactory(typeof(System.Windows.Controls.Image));
            image.SetBinding(System.Windows.Controls.Image.SourceProperty, new Binding("IsChecked")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                Converter = new BooleanToImageConverter(), // Custom converter for the arrow
            });

            image.SetValue(System.Windows.Controls.Image.WidthProperty, 12.0);
            image.SetValue(System.Windows.Controls.Image.HeightProperty, 12.0);
            image.SetValue(System.Windows.Controls.Image.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            image.SetValue(System.Windows.Controls.Image.VerticalAlignmentProperty, VerticalAlignment.Top);
            image.SetValue(System.Windows.Controls.Image.MarginProperty, new Thickness(5, 0, -10, 0));

            grid.AppendChild(image);
            template.VisualTree = grid;

            return template;
        }
        public class BooleanToImageConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool isChecked = (bool)value;
                string imagePath = isChecked
                    ? "pack://application:,,,/Images/Icons/arrow_collapsed.png"
                    : "pack://application:,,,/Images/Icons/arrow_collapsed.png";

                return new BitmapImage(new Uri(imagePath));
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        private void myExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid_col.Width = new GridLength(125, GridUnitType.Star);
                Expander_col.Width = new GridLength(2.5, GridUnitType.Star);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }
        private void myExpander_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                Grid_col.Width = new GridLength(150, GridUnitType.Star);
                Expander_col.Width = new GridLength(44, GridUnitType.Star);
                if (myExpander.IsExpanded && window.WindowState == WindowState.Normal)
                  Expander_col.Width = new GridLength(50, GridUnitType.Star);
                else if (!myExpander.IsExpanded && window.WindowState == WindowState.Normal)
                    Expander_col.Width = new GridLength(44, GridUnitType.Star);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void OnResizeExpanderRequested(double newWidth)
        {
            if(myExpander.IsExpanded)
                Expander_col.Width = new GridLength(newWidth, GridUnitType.Star);
        }

        private T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T foundChild)
                {
                    return foundChild;
                }
                var result = FindChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }


        private void main_tabControl_TabClosing(object sender, CancelingRoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to close this page?", "", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        #region TreeView

        public void Fill_TreeView(bool keepExpanded)
        {
            AllAccounts_ds = c.Select_Tree_View();
            if (AllAccounts_ds != null)
            {
                List<TreeViewItemState> expandedItems = MyTreeView.GetExpandedItems();

                PopulateTreeView();
                MyTreeView.RestoreExpandedState(expandedItems);
                
            }
            RClicked_ID = -1; RClicked_Name = "";
        }

        private void PopulateTreeView()
        {
            string role = Properties.Settings.Default.Role;
            MyTreeView.Items.Clear();
            Projects = AllAccounts_ds.Tables[0];
            ExchangeAccounts = AllAccounts_ds.Tables[1];
            Fiscalbonds = AllAccounts_ds.Tables[2];
            CashAccounts = AllAccounts_ds.Tables[3];
            PersonalAccounts = AllAccounts_ds.Tables[4];

            var projects = CreateParentTreeViewItem("Projects Accounts");
            projects.IsExpanded = false;

            var CaAccounts = CreateParentTreeViewItem("Cash Tills");
            CaAccounts.Tag = -1;

            var ExcAccounts = CreateParentTreeViewItem("Exchange Accounts");
            ExcAccounts.Tag = -1;

            var PerAccounts = CreateParentTreeViewItem("Personal Accounts");
            PerAccounts.Tag = -1;

            var fiscalbonds = CreateParentTreeViewItem("Bonds");
            fiscalbonds.Tag = -1;


            if (role == "Head Office")
            {
                string[] NewAccount = { "Create New Micro Projects Account", "Create New Budget Account" };


                foreach (var Account in NewAccount)
                {
                    System.Windows.Controls.TreeViewItem AddNewAccount;
                    if (Account == "Create New Micro Projects Account")
                    {
                         AddNewAccount = CreateTreeViewItem("Create New Micro Projects Account", "pack://application:,,,/Images/Icons/micro_projects.png");
                                             
                    }
                    else
                    {
                         AddNewAccount = CreateTreeViewItem("Create New Budget Account", "pack://application:,,,/Images/Icons/budget_account.png");
                                            
                    }
                    AddNewAccount.Tag = -1;
                    AddNewAccount.PreviewMouseLeftButtonDown += AddNewitem_PreviewMouseLeftButtonDown;

                    projects.Items.Add(AddNewAccount);
                }
            }


            // Group data by city, project, and subproject
            var Projects_cityGroups = Projects.AsEnumerable().GroupBy(row => row.Field<string>("CityName"));
            foreach (var city in Projects_cityGroups)
            {
                // Create a treeview item for the city
                var city_Item =  CreateTreeViewItem(city.Key);
                city_Item.Tag = city.First().Field<int>("CityID");

                // Group projects by project_ID
                var project_Groups = city.GroupBy(row => row.Field<string>("ProjectName"));
                foreach (var project in project_Groups)
                {
                    // Create a treeview item for the project
                    var project_Item = new System.Windows.Controls.TreeViewItem();
                    project_Item.Header = project.First().Field<string>("ProjectName");
                    project_Item.Tag = project.First().Field<int?>("ProjectID");
                    // Group subprojects by subproject_ID
                    var subproject_Groups = project.GroupBy(row => row.Field<string>("SubprojectName"));


                    foreach (var subproject in subproject_Groups)
                    {
                        // Create a treeview item for the subproject
                        var subproject_Item = new System.Windows.Controls.TreeViewItem();
                        subproject_Item.Header = subproject.First().Field<string>("SubprojectName");
                        subproject_Item.Tag = subproject.First().Field<int?>("SubprojectID");

                        if (role == "Head Office")
                        {
                            var subproject_contextMenu = new ContextMenu();
                            var subproject_OptionsMenuItem = new System.Windows.Controls.MenuItem();
                            subproject_OptionsMenuItem.Header = "Delete center";
                            subproject_OptionsMenuItem.Click += subproject_DeleteMenuItem_Click;
                            subproject_contextMenu.Items.Add(subproject_OptionsMenuItem);

                            subproject_Item.ContextMenu = subproject_contextMenu;
                            subproject_Item.AddHandler(System.Windows.Controls.TreeViewItem.ContextMenuOpeningEvent, new ContextMenuEventHandler(Right_ContextMenuOpening));

                        }

                        if (subproject.First().Field<string>("CategoryName") == "Micro Projects")
                        {
                            if (role == "Head Office" || role == "Project Manager" || role == "Financial Manager" || role == "Donor")
                            {
                                var AllAccounts = CreateTreeViewItem("All Accounts");
                                AllAccounts.Tag = -1;
                                AllAccounts.PreviewMouseLeftButtonDown += Options_PreviewMouseLeftButtonDown;
                                subproject_Item.Items.Add(AllAccounts);
                            }

                            var Payments = CreateTreeViewItem("Payments Account");
                            Payments.Tag = -1;
                            Payments.PreviewMouseLeftButtonDown += Options_PreviewMouseLeftButtonDown;

                            var Beneficiary = CreateTreeViewItem("Beneficiaries Accounts");
                            Beneficiary.Tag = -1;
                            Beneficiary.PreviewMouseLeftButtonDown += Options_PreviewMouseLeftButtonDown;


                            subproject_Item.Items.Add(Payments);
                            subproject_Item.Items.Add(Beneficiary);
                        }
                        else if (subproject.First().Field<string>("CategoryName") == "Budget")
                        {
                            if (role == "Head Office" || role == "Project Manager" || role == "Financial Manager" || role == "Donor")
                            {
                                var AllAccounts = CreateTreeViewItem("All Accounts");
                                AllAccounts.Tag = -1;
                                AllAccounts.PreviewMouseLeftButtonDown += Options_PreviewMouseLeftButtonDown;

                                subproject_Item.Items.Add(AllAccounts);
                            }

                        }

                        // Add accounts as child items
                        foreach (var Row in subproject)
                        {
                            string AccountName = Row.Field<string>("AccountName");
                            var account_Item = CreateTreeViewItem(AccountName);
                            account_Item.Tag = Row.Field<int?>("AccountID");
                            if (AccountName != null && Row.Field<Int64>("IsPayments") == 0 && Row.Field<Int32>("is_administrative") != 1 && Row.Field<Int32>("IsClosed") != 1)
                            {
                                account_Item.PreviewMouseLeftButtonDown += AccountItem_PreviewMouseLeftButtonDown;

                                if (role == "Head Office")
                                {
                                    var contextMenu = new ContextMenu();
                                    var OptionsMenuItem = new System.Windows.Controls.MenuItem();
                                    OptionsMenuItem.Header = "Delete account";
                                    OptionsMenuItem.Click += Acc_DeleteMenuItem_Click;
                                    contextMenu.Items.Add(OptionsMenuItem);

                                    account_Item.ContextMenu = contextMenu;
                                    account_Item.AddHandler(System.Windows.Controls.TreeViewItem.ContextMenuOpeningEvent, new ContextMenuEventHandler(Right_ContextMenuOpening));
                                }


                                if (role == "Head Office")
                                {
                                    var AdminAccounts_Groups = subproject.Where(row => row.Field<Int32?>("ObserveAccount_ID") != null && row.Field<Int32?>("ObserveAccount_ID") == Row.Field<int?>("AccountID") && row.Field<Int32>("IsClosed") != 1)
                                                                         .GroupBy(row => row.Field<Int32>("AccountID"));

                                    foreach (var AdminAccount in AdminAccounts_Groups)
                                    {
                                        var AdminAccount_Item = CreateTreeViewItem(AdminAccount.First().Field<string>("AccountName"));
                                        AdminAccount_Item.Tag = AdminAccount.First().Field<int?>("AccountID");
                                        AdminAccount_Item.PreviewMouseLeftButtonDown += AccountItem_PreviewMouseLeftButtonDown;


                                        var contextMenu = new ContextMenu();
                                        var OptionsMenuItem = new System.Windows.Controls.MenuItem();
                                        OptionsMenuItem.Header = "Delete account";
                                        OptionsMenuItem.Click += Acc_DeleteMenuItem_Click;
                                        contextMenu.Items.Add(OptionsMenuItem);

                                        AdminAccount_Item.ContextMenu = contextMenu;
                                        AdminAccount_Item.AddHandler(System.Windows.Controls.TreeViewItem.ContextMenuOpeningEvent, new ContextMenuEventHandler(Right_ContextMenuOpening));


                                        account_Item.Items.Add(AdminAccount_Item);

                                    }
                                }
                                subproject_Item.Items.Add(account_Item);
                            }

                        }
                        project_Item.Items.Add(subproject_Item);
                    }
                    city_Item.Items.Add(project_Item);
                }
                projects.Items.Add(city_Item);
            }


            if (role == "Head Office")
            {
                var AddNewCashAccount = CreateTreeViewItem("Create New Cash Till", "pack://application:,,,/Images/Icons/cash_till.png");
                AddNewCashAccount.Tag = -1;
                AddNewCashAccount.PreviewMouseLeftButtonDown += AddNewitem_PreviewMouseLeftButtonDown;

                CaAccounts.Items.Add(AddNewCashAccount);
            }


            var Cash_cityGroups = CashAccounts.AsEnumerable().GroupBy(row => row.Field<string>("CityName"));
            foreach (var city in Cash_cityGroups)
            {
                // Create a treeview item for the city
                var city_Item = CreateTreeViewItem(city.Key);
                city_Item.Tag = city.First().Field<int>("CityID");

                var CashAccount_Groups = city.GroupBy(row => row.Field<string>("Name"));

                // Add accounts as child items
                foreach (var CashAccount in CashAccount_Groups)
                {
                    var Cash_Item = CreateTreeViewItem(CashAccount.First().Field<string>("Name"));
                    Cash_Item.Tag = CashAccount.First().Field<int>("ID");

                    Cash_Item.PreviewMouseLeftButtonDown += Cash_AccountItem_PreviewMouseLeftButtonDown;

                    if (role == "Head Office")
                    {
                        var contextMenu = new ContextMenu();
                        var OptionsMenuItem = new System.Windows.Controls.MenuItem();
                        OptionsMenuItem.Header = "Delete account";
                        OptionsMenuItem.Click += Cash_Acc_DeleteMenuItem_Click;
                        contextMenu.Items.Add(OptionsMenuItem);

                        Cash_Item.ContextMenu = contextMenu;
                        Cash_Item.AddHandler(System.Windows.Controls.TreeViewItem.ContextMenuOpeningEvent, new ContextMenuEventHandler(Right_ContextMenuOpening));

                    }
                    city_Item.Items.Add(Cash_Item);
                }
                CaAccounts.Items.Add(city_Item);
            }


            if (role == "Head Office")
            {
                var AddNewExAccount = CreateTreeViewItem("Create New Exchange Account", "pack://application:,,,/Images/Icons/exchange_account.png");
                AddNewExAccount.Tag = -1;
                AddNewExAccount.PreviewMouseLeftButtonDown += AddNewitem_PreviewMouseLeftButtonDown;

                ExcAccounts.Items.Add(AddNewExAccount);
            }


            var Exchange_cityGroups = ExchangeAccounts.AsEnumerable().GroupBy(row => row.Field<string>("CityName"));
            foreach (var City in Exchange_cityGroups)
            {
                // Create a treeview item for the city
                var city_Item = CreateTreeViewItem(City.Key);
                city_Item.Tag = City.First().Field<int>("CityID");

                var ExchangeAccount_Groups = City.GroupBy(row => row.Field<string>("Name"));

                foreach (var ExchangeAccount in ExchangeAccount_Groups)
                {
                    var Exchange_Item = CreateTreeViewItem(ExchangeAccount.First().Field<string>("Name"));
                    Exchange_Item.Tag = ExchangeAccount.First().Field<int>("ID");

                    Exchange_Item.PreviewMouseLeftButtonDown += AccountItem_PreviewMouseLeftButtonDown;


                    if (role == "Head Office")
                    {
                        var contextMenu = new ContextMenu();
                        var OptionsMenuItem = new System.Windows.Controls.MenuItem();
                        OptionsMenuItem.Header = "Delete account";
                        OptionsMenuItem.Click += Acc_DeleteMenuItem_Click;
                        contextMenu.Items.Add(OptionsMenuItem);

                        Exchange_Item.ContextMenu = contextMenu;
                    }

                    Exchange_Item.AddHandler(System.Windows.Controls.TreeViewItem.ContextMenuOpeningEvent, new ContextMenuEventHandler(Right_ContextMenuOpening));

                    city_Item.Items.Add(Exchange_Item);
                }
                ExcAccounts.Items.Add(city_Item);
            }

            if (role == "Head Office")
            {
                var AddNewPersonalAccount = CreateTreeViewItem("Create New Personal Account" , "pack://application:,,,/Images/Icons/personal_account.png");
                AddNewPersonalAccount.Tag = -1;
                AddNewPersonalAccount.PreviewMouseLeftButtonDown += AddNewitem_PreviewMouseLeftButtonDown;

                PerAccounts.Items.Add(AddNewPersonalAccount);
            }


            var Personal_cityGroups = PersonalAccounts.AsEnumerable().GroupBy(row => row.Field<string>("CityName"));
            foreach (var City in Personal_cityGroups)
            {
                // Create a treeview item for the city
                var city_Item = CreateTreeViewItem(City.Key);
                city_Item.Tag = City.First().Field<int>("CityID");

                var PersonalAccounts_Groups = City.GroupBy(row => row.Field<string>("Name"));

                if (role == "Head Office" || role == "Project Manager" || role == "Financial Manager" || role == "Donor")
                {
                    var AllPersonalAccounts = CreateTreeViewItem("All Personal Accounts");
                    AllPersonalAccounts.Tag = -1;
                    AllPersonalAccounts.PreviewMouseLeftButtonDown += Options_PreviewMouseLeftButtonDown;

                    city_Item.Items.Add(AllPersonalAccounts);
                }

                foreach (var PersonalAccount in PersonalAccounts_Groups)
                {
                    var Personal_Item = CreateTreeViewItem(PersonalAccount.First().Field<string>("Name"));
                    Personal_Item.Tag = PersonalAccount.First().Field<int>("ID");
                    if (Personal_Item != null && PersonalAccount.First().Field<Int32>("IsClosed") != 1)
                    {
                        Personal_Item.PreviewMouseLeftButtonDown += AccountItem_PreviewMouseLeftButtonDown;


                        if (role == "Head Office")
                        {
                            var contextMenu = new ContextMenu();
                            var OptionsMenuItem = new System.Windows.Controls.MenuItem();
                            OptionsMenuItem.Header = "Delete account";
                            OptionsMenuItem.Click += Acc_DeleteMenuItem_Click;
                            contextMenu.Items.Add(OptionsMenuItem);

                            Personal_Item.ContextMenu = contextMenu;
                        }

                        Personal_Item.AddHandler(System.Windows.Controls.TreeViewItem.ContextMenuOpeningEvent, new ContextMenuEventHandler(Right_ContextMenuOpening));


                        city_Item.Items.Add(Personal_Item);
                    }


                }
                PerAccounts.Items.Add(city_Item);
            }


            if (role == "Head Office")
            {
                var AddNewFiscalbond = CreateTreeViewItem("Create New Bond");
                AddNewFiscalbond.Tag = -1;
                AddNewFiscalbond.PreviewMouseLeftButtonDown += AddNewitem_PreviewMouseLeftButtonDown;
                fiscalbonds.Items.Add(AddNewFiscalbond);
            }

            // Group data by city, project, and subproject
            var cityGroups1 = Fiscalbonds.AsEnumerable().GroupBy(row => row.Field<string>("CityName"));
            foreach (var cityGroup in cityGroups1)
            {
                // Create a treeview item for the city
                var cityItem = CreateTreeViewItem(cityGroup.Key);
                cityItem.Tag = cityGroup.First().Field<int>("CityID");

                // Group projects by project_ID
                var projectGroups = cityGroup.GroupBy(row => row.Field<string>("ProjectName"));
                foreach (var projectGroup in projectGroups)
                {
                    // Create a treeview item for the project
                    var projectItem = CreateTreeViewItem(projectGroup.First().Field<string>("ProjectName"));
                    projectItem.Tag = projectGroup.First().Field<int?>("ProjectID");

                    // Group subprojects by subproject_ID
                    var subprojectGroups = projectGroup.GroupBy(row => row.Field<string>("SubprojectName"));
                    foreach (var subprojectGroup in subprojectGroups)
                    {
                        // Create a treeview item for the subproject
                        var subprojectItem = CreateTreeViewItem(subprojectGroup.First().Field<string>("SubprojectName"));
                        subprojectItem.Tag = subprojectGroup.First().Field<int?>("SubprojectID");


                        // Add accounts as child items
                        foreach (var FiscalbondRow in subprojectGroup)
                        {
                            
                            string FiscalbondName = FiscalbondRow.Field<string>("FiscalbondName");
                            var FiscalbondItem = CreateTreeViewItem(FiscalbondName);
                            FiscalbondItem.Tag = FiscalbondRow.Field<int?>("FiscalbondID");
                            if (FiscalbondName != null && FiscalbondRow.Field<Int32>("IsClosed") != 1)
                            {
                                FiscalbondItem.PreviewMouseLeftButtonDown += fiscalbondItem_PreviewMouseLeftButtonDown;

                                if (role == "Head Office")
                                {
                                    var Till_contextMenu = new ContextMenu();
                                    var Till_OptionsMenuItem = new System.Windows.Controls.MenuItem();
                                    Till_OptionsMenuItem.Header = "Delete Bond";
                                    Till_OptionsMenuItem.Click += fiscalbond_DeleteMenuItem_Click;
                                    Till_contextMenu.Items.Add(Till_OptionsMenuItem);

                                    FiscalbondItem.ContextMenu = Till_contextMenu;
                                    FiscalbondItem.AddHandler(System.Windows.Controls.TreeViewItem.ContextMenuOpeningEvent, new ContextMenuEventHandler(Right_ContextMenuOpening));

                                }

                                subprojectItem.Items.Add(FiscalbondItem);
                            }

                        }

                        projectItem.Items.Add(subprojectItem);
                    }

                    cityItem.Items.Add(projectItem);
                }

                fiscalbonds.Items.Add(cityItem);
            }

            if (projects.Items.Count > 0)
                MyTreeView.Items.Add(projects);
            if (CaAccounts.Items.Count > 0)
                MyTreeView.Items.Add(CaAccounts);
            if (ExcAccounts.Items.Count > 0)
                MyTreeView.Items.Add(ExcAccounts);
            if (PerAccounts.Items.Count > 0)
                MyTreeView.Items.Add(PerAccounts);
            if (fiscalbonds.Items.Count > 0)
                MyTreeView.Items.Add(fiscalbonds);

            Fill_Approval_Cities();
        }

        private System.Windows.Controls.TreeViewItem CreateTreeViewItem(string text, string imagePath = null)
        {
            // Create a DockPanel for the header
            var panel = new DockPanel();

            // Add an image if the imagePath is provided
            if (!string.IsNullOrEmpty(imagePath))
            {
                var image = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri(imagePath)),
                    Width = 13,
                    Height = 10,
                    Margin = new Thickness(-20, 0, 10, 0)
                };
                panel.Children.Add(image);
            }

            // Add text
            var textBlock = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(textBlock);

            // Return the TreeViewItem

            return new System.Windows.Controls.TreeViewItem
            {
                Header = panel,
            };
        }

        private System.Windows.Controls.TreeViewItem CreateParentTreeViewItem(string text)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)Application.Current.Resources["DarkGray"]

            };

            return new System.Windows.Controls.TreeViewItem
            {
                Header = textBlock,
                IsExpanded = false,
                Margin = new Thickness(20, 0, 5, 0)
            };
        }
        
        
        private void Options_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Windows.Controls.TreeViewItem clickedItem = sender as System.Windows.Controls.TreeViewItem;
                if (clickedItem != null && clickedItem.Header is DockPanel panel)
                {
                    ItemsControl parent = GetSelectedTreeViewItemParent(clickedItem);

                    System.Windows.Controls.TreeViewItem treeitem = parent as System.Windows.Controls.TreeViewItem;
                    string Sub_ID = treeitem.Tag.ToString();
                    var Sub_Name = treeitem.Header.ToString();
                    if (treeitem.Header is DockPanel panel1)
                    {
                        // Extract text from the TextBlock inside the DockPanel
                        Sub_Name = panel1.Children.OfType<TextBlock>().FirstOrDefault()?.Text ?? string.Empty;
                        foreach (var child in panel.Children)
                            {
                                if (child is TextBlock textBlock)
                                {
                                    switch (textBlock.Text)
                                    {
                                        case "All Accounts":
                                            All_Accounts_Window All_Accounts = new All_Accounts_Window(this, Sub_ID);
                                            Create_tabItem(All_Accounts, textBlock.Text + "-" + Sub_Name, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                                            break;
                                        case "Payments Account":
                                            Window Payments = new Payments_Window(this, Sub_ID);
                                            Create_tabItem(Payments, textBlock.Text + "-" + Sub_Name, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                                            break;
                                        case "Beneficiaries Accounts":
                                            Account_Beneficiaries_Window account_Beneficiaries = new Account_Beneficiaries_Window(-1, Convert.ToInt32(Sub_ID), this);
                                            Create_tabItem(account_Beneficiaries, textBlock.Text + Sub_Name, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                                            break;
                                        case "All Personal Accounts":
                                            All_Peersonal_Accounts_Window All_Peersonal_Accounts = new All_Peersonal_Accounts_Window(this, Sub_ID);
                                            Create_tabItem(All_Peersonal_Accounts, textBlock.Text + "-" + Sub_Name, (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                                            break;
                                    }
                                }
                            }
                    }
                    else if (Sub_Name.Contains("Micro"))
                    {
                        foreach (var child in panel.Children)
                            {
                                if (child is TextBlock textBlock)
                                {
                                    if (Sub_Name.Contains("Micro"))
                                    {
                                        switch (textBlock.Text)
                                        {
                                            case "All Accounts":
                                                All_Accounts_Window All_Accounts = new All_Accounts_Window(this, Sub_ID);
                                                Create_tabItem(All_Accounts, textBlock.Text + "-" + Sub_Name, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                                break;
                                            case "Payments Account":
                                                Window Payments = new Payments_Window(this, Sub_ID);
                                                Create_tabItem(Payments, textBlock.Text + "-" + Sub_Name, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                                break;
                                            case "Beneficiaries Accounts":
                                                Account_Beneficiaries_Window account_Beneficiaries = new Account_Beneficiaries_Window(-1, Convert.ToInt32(Sub_ID), this);
                                                Create_tabItem(account_Beneficiaries, textBlock.Text + Sub_Name, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                                break;
                                            case "All Personal Accounts":
                                                All_Peersonal_Accounts_Window All_Peersonal_Accounts = new All_Peersonal_Accounts_Window(this, Sub_ID);
                                                Create_tabItem(All_Peersonal_Accounts, textBlock.Text + "-" + clickedItem.Header, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                                break;
                                        }
                                    }
                                }
                            }
                    }
                    else
                    {
                        foreach (var child in panel.Children)
                            {
                                if (child is TextBlock textBlock)
                                {
                                    switch (textBlock.Text)
                                    {
                                        case "All Accounts":
                                            All_Accounts_Window All_Accounts = new All_Accounts_Window(this, Sub_ID);
                                            Create_tabItem(All_Accounts, textBlock.Text + "-" + Sub_Name, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                                            break;
                                        case "Payments Account":
                                            Window Payments = new Payments_Window(this, Sub_ID);
                                            Create_tabItem(Payments, textBlock.Text + "-" + Sub_Name, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                                            break;
                                        case "Beneficiaries Accounts":
                                            Account_Beneficiaries_Window account_Beneficiaries = new Account_Beneficiaries_Window(-1, Convert.ToInt32(Sub_ID), this);
                                            Create_tabItem(account_Beneficiaries, textBlock.Text + Sub_Name, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                                            break;
                                        case "All Personal Accounts":
                                            All_Peersonal_Accounts_Window All_Peersonal_Accounts = new All_Peersonal_Accounts_Window(this, Sub_ID);
                                            Create_tabItem(All_Peersonal_Accounts, textBlock.Text + "-" + clickedItem.Header, (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                                            break;
                                    }
                                }
                            }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        public ItemsControl GetSelectedTreeViewItemParent(System.Windows.Controls.TreeViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (!(parent is System.Windows.Controls.TreeViewItem || parent is System.Windows.Controls.TreeView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as ItemsControl;
        }

        //Exchange Account
        private void AccountItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {   
                var source = e.OriginalSource as DependencyObject;
                while (source != null && !(source is ToggleButton))
                {
                    source = VisualTreeHelper.GetParent(source);
                }

                // If source is not null, it means we've found a ToggleButton in the visual tree
                if (source != null)
                {
                    // Ignore the event if the source is a ToggleButton (expander arrow)
                    return;
                }
                
                
                    var treeViewItem = sender as System.Windows.Controls.TreeViewItem;
                if (treeViewItem.Items.OfType<System.Windows.Controls.TreeViewItem>().All(item => !item.IsMouseOver))
                {
                    System.Windows.Controls.TreeViewItem accountItem = sender as System.Windows.Controls.TreeViewItem;
                    if (accountItem != null && accountItem.Header is DockPanel panel)
                    {
                        int Acc_ID = Convert.ToInt32(accountItem.Tag.ToString());
                        string Acc_Name = "";
                        foreach (var child in panel.Children)
                        {
                            if (child is TextBlock textBlock)
                            {
                                Acc_Name = textBlock.Text;
                            }
                        }
                        ItemsControl parent = GetSelectedTreeViewItemParent(accountItem);

                        System.Windows.Controls.TreeViewItem treeitem = parent as System.Windows.Controls.TreeViewItem;
                        string Sub_ID = treeitem.Tag.ToString();
                        string Sub_Name = treeitem.Header.ToString();
                        Account_Window account_Window = new Account_Window(Acc_ID, this);
                        if (Sub_Name.Contains("Micro")) { 
                            Create_tabItem(account_Window, Acc_Name, (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                        }
                        if (Acc_Name.Contains("Exchange"))
                        {
                            Create_tabItem(account_Window, Acc_Name, (SolidColorBrush)Application.Current.Resources["Orange"]);
                        }
                        else if(Acc_Name.Contains("Advance Payments"))
                            Create_tabItem(account_Window, Acc_Name, (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                        else if( !Acc_Name.Contains("Exchange") && !Acc_Name.Contains("Advance Payments") && !Sub_Name.Contains("Micro"))
                            Create_tabItem(account_Window, Acc_Name, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Cash_AccountItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Windows.Controls.TreeViewItem accountItem = sender as System.Windows.Controls.TreeViewItem;
                if (accountItem != null && accountItem.Header is DockPanel panel)
                {
                    int Acc_ID = Convert.ToInt32(accountItem.Tag.ToString());
                    string Acc_Name = "";
                    foreach (var child in panel.Children)
                    {
                        if (child is TextBlock textBlock)
                        {
                            Acc_Name = textBlock.Text;
                        }
                    }

                    Account_Window account_Window = new Account_Window(Acc_ID, "Cash", this);
                    Create_tabItem(account_Window, Acc_Name, (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void fiscalbondItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Windows.Controls.TreeViewItem fiscalbondItem = sender as System.Windows.Controls.TreeViewItem;
                if (fiscalbondItem != null && fiscalbondItem.Header is DockPanel panel)
                {
                    int fiscalbond_ID = Convert.ToInt32(fiscalbondItem.Tag.ToString());
                    string fiscalbond_Name = "";
                    foreach (var child in panel.Children)
                    {
                        if (child is TextBlock textBlock)
                        {
                            fiscalbond_Name = textBlock.Text;
                        }
                    }

                    Till_Window till_Window = new Till_Window(fiscalbond_ID, fiscalbond_Name, this);
                    Create_tabItem(till_Window, fiscalbond_Name, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                    /*
                    All_ActionFiscalBond_Window window = new All_ActionFiscalBond_Window(fiscalbond_ID, fiscalbond_Name, this);
                    Create_dragablz_tabItem(window, fiscalbond_Name);*/
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void AddNewitem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Windows.Controls.TreeViewItem NewItem = sender as System.Windows.Controls.TreeViewItem;
                if (NewItem != null && NewItem.Header is DockPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is TextBlock textBlock)
                        {
                            switch (textBlock.Text)
                            {
                                case "Create New Bond":
                                    FiscalBond_Window _FBWindow = new FiscalBond_Window(this);
                                    _FBWindow.Show();
                                    break;
                                case "Create New Micro Projects Account":
                                    Account_Window Maccount_ = new Account_Window("Micro Projects", "Project", "", "", this);
                                    Create_tabItem(Maccount_, "New Micro Projects Account", (SolidColorBrush)Application.Current.Resources["HopeRed"]);
                                    break;
                                case "Create New Exchange Account":
                                    Account_Window Eaccount_ = new Account_Window("", "Exchange", "", "", this);
                                    Create_tabItem(Eaccount_, "New Exchange Account", (SolidColorBrush)Application.Current.Resources["Orange"]);
                                    break;
                                case "Create New Budget Account":
                                    Account_Window Yaccount_ = new Account_Window("Budget", "Project", "", "", this);
                                    Create_tabItem(Yaccount_, "New Budget Account", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                                    break;
                                case "Create New Cash Till":
                                    Account_Window Cashaccount_ = new Account_Window("", "Cash", "", "", this);
                                    Create_tabItem(Cashaccount_, "New Cash Till", (SolidColorBrush)Application.Current.Resources["PastelDarkBlue"]);
                                    break;
                                case "Create New Personal Account":
                                    Account_Window Personalaccount_ = new Account_Window("", "Personal", "", "", this);
                                    Create_tabItem(Personalaccount_, "New Personal Account", (SolidColorBrush)Application.Current.Resources["PastelGreen"]);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void MyTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DependencyObject source = e.OriginalSource as DependencyObject;
                System.Windows.Controls.TreeViewItem treeViewItem = FindParentTreeViewItem(source);
                if (treeViewItem != null)
                {
                    treeViewItem.Focus();
                    treeViewItem.IsSelected = true;
                    if (treeViewItem.ContextMenu != null)
                    {
                        RClicked_ID = Convert.ToInt32(treeViewItem.Tag.ToString());
                        RClicked_Name = treeViewItem.Header.ToString();

                        treeViewItem.ContextMenu.PlacementTarget = treeViewItem;
                        treeViewItem.ContextMenu.IsOpen = true;
                        treeViewItem.IsExpanded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }


        }

        private void Acc_DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RClicked_ID != -1)
                {
                    //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                    var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        if (main_tabControl.Items != null && main_tabControl.Items.Count != 0)
                        {
                            foreach (TabItemExt tab in main_tabControl.Items)
                            {
                                if (tab.Header.ToString() == RClicked_Name)
                                {
                                    MessageBox.Show("Please close the account before deleting it.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                    return;
                                }
                            }
                        }

                        Acc.Delete(RClicked_ID, RClicked_Name, "Main Window");

                        Fill_TreeView(true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                    MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error");
                else
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Cash_Acc_DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RClicked_ID != -1)
                {
                    //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                    var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        if (main_tabControl.Items != null && main_tabControl.Items.Count != 0)
                        {
                            foreach (TabItemExt tab in main_tabControl.Items)
                            {
                                if (tab.Header.ToString() == RClicked_Name)
                                {
                                    MessageBox.Show("Please close the account before deleting it.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                    return;
                                }
                            }
                        }

                        Acc.Delete_Cash_Account(RClicked_ID, RClicked_Name, "Main Window");

                        Fill_TreeView(true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                    MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error");
                else
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void subproject_DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RClicked_ID != -1)
                {
                    //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                    var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                    if (dialogResult == MessageBoxResult.Yes)
                    {

                        if (main_tabControl.Items != null && main_tabControl.Items.Count != 0)
                        {
                            DataRow[] rows = AllAccounts_ds.Tables[0].Select("SubprojectID = '" + RClicked_ID + "'");
                            DataTable accounts_dt = new DataTable();
                            if (rows.Length != 0)
                            {
                                accounts_dt = rows.CopyToDataTable();
                                if (accounts_dt != null && accounts_dt.Rows.Count > 0)
                                {
                                    for (int i = 0; i < accounts_dt.Rows.Count; i++)
                                    {
                                        foreach (TabItemExt tab in main_tabControl.Items)
                                        {
                                            if (tab.Header.ToString() == accounts_dt.Rows[i]["AccountName"].ToString())
                                            {
                                                MessageBox.Show("Please close all this center accounts before deleting it.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        sub.Delete(RClicked_ID, RClicked_Name, "Main Window");

                        Fill_TreeView(true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                    MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error");
                else
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void fiscalbond_DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RClicked_ID != -1)
                {
                    //var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirm before save", MessageBoxButton.YesNo);
                    var dialogResult = MessageBox.Show("Are you sure you want to delete this data?", "Confirmation Request", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        if (main_tabControl.Items != null && main_tabControl.Items.Count != 0)
                        {
                            foreach (TabItemExt tab in main_tabControl.Items)
                            {
                                if (tab.Header.ToString() == RClicked_Name)
                                {
                                    MessageBox.Show("Please close the account before deleting it.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                                    return;
                                }
                            }
                        }

                        fb.Delete(RClicked_ID, RClicked_Name, "Main Window");

                        Fill_TreeView(true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Login.MyConn != null && Login.MyConn.State == ConnectionState.Open)
                {
                    Login.MyConn.Close();
                }
                if (ex.Message.Contains("Cannot delete or update a parent row"))
                    MessageBox.Show("This item cannot be deleted due to its association with other data.", "Error");
                else
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Right_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                System.Windows.Controls.TreeViewItem treeViewItem = sender as System.Windows.Controls.TreeViewItem;
                if (treeViewItem != null)
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private System.Windows.Controls.TreeViewItem FindParentTreeViewItem(DependencyObject child)
        {
            try
            {
                while (child != null && !(child is System.Windows.Controls.TreeViewItem))
                {
                    child = VisualTreeHelper.GetParent(child);
                }

                return child as System.Windows.Controls.TreeViewItem;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
                return null;
            }

        }

        #endregion


        private void Donor_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window _DonorWindow = new NewDonor_Window(this);
                _DonorWindow.Show();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void City_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window _CityWindow = new CityWindow(this);
                _CityWindow.Show();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Subproject_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window _SubPWindow = new SubProjectWindow(this);
                _SubPWindow.Show(); ;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Budget_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window _BudgetWindow = new Budget_Window(this);
                _BudgetWindow.Show();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Denominations_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window _DenominationsWindow = new Denominations_Window(this);
                _DenominationsWindow.Show();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Log_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log_Window log = new Log_Window(this);
                Create_tabItem(log, "LOG", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Lines_Report_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Lines_Report_Window Lines_Report_Window = new Lines_Report_Window(this);
                Create_tabItem(Lines_Report_Window, "Expenses Report", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Cash_Balances_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CashBalances_Window CashBalances_Window = new CashBalances_Window(-1, this);
                Create_tabItem(CashBalances_Window, "Balances", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void User_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                User_Window User_Window = new User_Window(this);
                Create_tabItem(User_Window, "Users", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Fill_TreeView(true);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Project_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Project_Window _Project_Window = new Project_Window(this);
                _Project_Window.Show();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Employee_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Employee_Window Employee_Window = new Employee_Window(this);
                Create_tabItem(Employee_Window, "Volunteers", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Monthly_Salaries_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Monthly_Salaries_Window Monthly_Salaries = new Monthly_Salaries_Window(this);
                Create_tabItem(Monthly_Salaries, "Salaries", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Approval_Limits_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window _Approval_Limits = new Approval_Limit_Window(this);
                _Approval_Limits.Show();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Approval_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                if (menuItem != null)
                {
                    int cityId = Convert.ToInt32(menuItem.Tag.ToString());
                    string cityName = menuItem.Header.ToString();

                    Approval_Window Approval = new Approval_Window(cityId, this);
                    Create_tabItem(Approval, "Approval - " + cityName, (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
                }


            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Check_App_Version();
        }

        public bool Check_App_Version()
        {
            bool VesrionMatches = false;
            DataTable Version_dt = v.Get_Version();

            var version1 = new System.Version(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            var version2 = new System.Version(Version_dt.Rows[0].Field<string>("l_version"));

            var result = version1.CompareTo(version2);
            if (result > 0)
            {
                v.Update_Version(Assembly.GetExecutingAssembly().GetName().Version.ToString());
                VesrionMatches = true;
            }
            else if (result < 0)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("THE APPLICATION WILL BE CLOSED" + Environment.NewLine + "You are using an old version of this application." + Environment.NewLine + "Please update to the latest one." + Environment.NewLine + version2.ToString(), "Error", MessageBoxButton.OK);
                if (messageBoxResult == MessageBoxResult.OK)
                {
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
                VesrionMatches = false;
            }
            else
            {
                VesrionMatches = true;
            }

            return VesrionMatches;
        }

        private void Fill_Approval_Cities()
        {
            var cityNames = new Dictionary<string, int>(); // Change HashSet to Dictionary

            foreach (DataTable dt in new[] { Projects, ExchangeAccounts, PersonalAccounts })
            {
                foreach (DataRow row in dt.Rows)
                {
                    string cityName = row["CityName"].ToString();
                    int cityId = Convert.ToInt32(row["CityID"].ToString());
                    if (!cityNames.ContainsKey(cityName))
                    {
                        cityNames.Add(cityName, cityId);
                    }
                }
            }

            foreach (var city in cityNames)
            {
                //Changes From Syncfusion:MenuItemAdv to MenuItem
                var menuItem = new MenuItem
                {
                    Header = city.Key,
                    Tag = city.Value
                };
                // Apply Style Dynamically 
                Approval_MenuItem.Style = (Style)this.Resources["MenuItemStyle"];
                menuItem.Click += Approval_MenuItemAdv_Click;
                Approval_MenuItem.Items.Add(menuItem);
            }
        }

        private void All_Budget_Lines_Report_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                All_Budgetlins_Window All_Budgetlins_Window = new All_Budgetlins_Window(this);
                Create_tabItem(All_Budgetlins_Window, "All Bidget Lines Report", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Revenue_Exchange_Report_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Revenue_Exchange_Report_Window Revenue_Exchange_Report_Window = new Revenue_Exchange_Report_Window(this);
                Create_tabItem(Revenue_Exchange_Report_Window, "Revenue/Exchange Report", (SolidColorBrush)Application.Current.Resources["Orange"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Lines_Lifetime_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Lines_lifetime_Window Lines_lifetime_Window = new Lines_lifetime_Window(this);
                Create_tabItem(Lines_lifetime_Window, "Lines lifetime", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Fixed_Assets_Report_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Fixed_Assets_Report_Window Fixed_Assets_Report_Window = new Fixed_Assets_Report_Window(this);
                Create_tabItem(Fixed_Assets_Report_Window, "Fixed Assets Report", (SolidColorBrush)Application.Current.Resources["GreenBlue"]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Single_Entry_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddAction_Window _Window = new AddAction_Window(this);
                _Window.ShowDialog();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Multi_Entry_MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiActions_Window _Window = new Add_MultiActions_Window(this, false);
                _Window.ShowDialog();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void MenuItemAdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Add_MultiActions_Window _Window = new Add_MultiActions_Window(this, true);
                _Window.ShowDialog();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                { MessageBox.Show("please check your network and try again", "Error"); }
                else if (ex.Message.Contains("host"))
                { MessageBox.Show("please check the server or your network connection and try again", "Error"); }
                else if (ex.Message.Contains("Duplicate entry"))
                { MessageBox.Show("Error … There are duplicate values.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
                else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowUtils.MinimizeWindow(this); // Calls the WindowUtils method
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            WindowUtils.ResizeWindow(this); // Calls the WindowUtils method
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            WindowUtils.CloseWindow(this); // Calls the WindowUtils method
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowUtils.TitleBar_MouseDown(this, e);
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Check if the window is minimized
            if (this.WindowState == WindowState.Minimized)
            {
                // Resize the window when minimized
                this.Width = 1330;  // Set desired width
                this.Height = 700; // Set desired height
            }
            else if (this.WindowState == WindowState.Normal)
            {
                // Restore the original size when window is restored
                this.Width = 1920;  // Original width
                this.Height = 1021; // Original height
            }
        }


    }
}





