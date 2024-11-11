using System; 
using System.Windows;
using System.Windows.Controls; 
using System.Windows.Media.Imaging; 
using System.Windows.Controls.Ribbon;  
using System.Data;
using System.Windows.Controls.Ribbon.Primitives;
using Financial_WPF_App.Models;
using Financial_WPF_App.Views;

namespace Financial_WPF_App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
             
            c = new Category();
            sub = new SubProject();
            a = new Account();
            category_dt = c.Select("", "");
            sub_dt = sub.Select("", "", "", "");

            Fill_RibbonTabs();
        }
        DataTable category_dt,sub_dt,account_dt;
        SubProject sub; Account a;
        Category c;
         

        void Fill_RibbonTabs()
        { 
            RibbonTab tabItem = new RibbonTab();
            RibbonGroup ribbonGroup = new RibbonGroup(); 
            RibbonMenuButton ribbonMenuButton = new RibbonMenuButton();
            RibbonMenuItem ribbonMenuItem;

            // Tab Properties //
            tabItem.Header = "إنشاء جديد";
            tabItem.KeyTip = "N";

            // Group Properties //
            ribbonGroup.Name = "Accounts";
            //ribbonGroup.Header = "My group";

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(@"/Images/home32.png", UriKind.RelativeOrAbsolute);
            img.EndInit();
            ribbonMenuButton.LargeImageSource = img;

            ribbonMenuButton.Label = "إنشاء حساب جديد";
            ribbonMenuButton.KeyTip = "N";

            for (int i = 0; i < category_dt.Rows.Count; i++)
            {
                ribbonMenuItem = new RibbonMenuItem();
                ribbonMenuItem.ImageSource = new BitmapImage(new Uri(@"/Images/star-32.png", UriKind.RelativeOrAbsolute));
                ribbonMenuItem.Header = category_dt.Rows[i].Field<string>(1);
                ribbonMenuButton.Items.Add(ribbonMenuItem);
            }
           
            ribbonGroup.Items.Add(ribbonMenuButton);
            tabItem.Items.Add(ribbonGroup);
            MyRibbon.Items.Add(tabItem);

            ribbonGroup = new RibbonGroup();
            //RibbonMenuItemsPanel ribbonMenuItemsPanel = new RibbonMenuItemsPanel();
            //RibbonGallery ribbonGallery = new RibbonGallery();
            //RibbonGalleryCategory ribbonGalleryCategory = new RibbonGalleryCategory();
            RibbonMenuButton btn;
            for (int i = 0; i < sub_dt.Rows.Count; i++)
            {
                if (sub_dt.Rows[i].Field<int>("Project_ID") == 3) //MICRO
                {
                    btn = new RibbonMenuButton();
                    btn.SmallImageSource = new BitmapImage(new Uri(@"/Images/star-32.png", UriKind.RelativeOrAbsolute));
                    btn.Label = sub_dt.Rows[i].Field<string>("Name");
                    //ribbonGalleryCategory.Items.Add(btn);
                    
                    account_dt = a.Select(sub_dt.Rows[i].Field<int>("ID").ToString(),"",false);
                    for (int j = 0; j < account_dt.Rows.Count; j++)
                    {
                        ribbonMenuItem = new RibbonMenuItem();
                        ribbonMenuItem.ImageSource = new BitmapImage(new Uri(@"/Images/car32.png", UriKind.RelativeOrAbsolute));
                        ribbonMenuItem.Tag = account_dt.Rows[j].Field<int>(0);
                        ribbonMenuItem.Header = account_dt.Rows[j].Field<string>(1);
                        ribbonMenuItem.Click += new RoutedEventHandler(Account_Item_Click); 
                        btn.Items.Add(ribbonMenuItem);
                    }

                    ribbonGroup.Items.Add(btn);
                }  
            }
            tabItem.Items.Add(ribbonGroup);

            //ribbonGallery.Items.Add(ribbonGalleryCategory);
            //ribbonMenuItemsPanel.Children.Add(ribbonGallery);
            //ribbonGroup.Items.Add(ribbonMenuItemsPanel);
        }
        void Account_Item_Click(object sender, RoutedEventArgs e)
        {
            RibbonMenuItem item = (RibbonMenuItem)sender;
            string name = "";
            int id = -1;
            if (item.Header != null)
            {
                name = item.Header.ToString();
                id = Convert.ToInt32(item.Tag);

                // Create new constructor reads the account Id then Fill //

                Account_Window account_ = new Account_Window(id);
                //account_.Show();
                TabItem oTab = new TabItem();
                oTab.Content = account_.Content;
                oTab.Header = name;
                main_tabControl.Items.Add(oTab);
                main_tabControl.SelectedIndex = main_tabControl.Items.Count - 1;
            }
        }

        private void NewPayments_btn_OnClick(object sender, RoutedEventArgs e)
        {
            Payments_Window account_ = new Payments_Window();
                 
            TabItem oTab = new TabItem();
            oTab.Content = account_.Content;
            oTab.Header = "أثساط";
            main_tabControl.Items.Add(oTab);
            main_tabControl.SelectedIndex = main_tabControl.Items.Count - 1;
           
        }

        private void NewDonor_btn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Window newDonor = new NewDonor_Window();
                newDonor.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void NewCity_btn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Window cityWindow = new CityWindow();
                cityWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void NewProject_btn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Window projectWindow = new ProjectWindow();
                projectWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void NewCategory_btn_OnClick(object sender, RoutedEventArgs e)
        {
            Window categoryWindow = new CategoryWindow();
            categoryWindow.Show();
        }

        private void NewSubProject_btn_OnClick(object sender, RoutedEventArgs e)
        {
            Window subProjectWindow = new SubProjectWindow();
            subProjectWindow.Show();
        }

        private void NewLine_btn_OnClick(object sender, RoutedEventArgs e)
        {
            Window lineWindow = new Line_Window();
            lineWindow.Show();
        }

        #region new accounts
        private void NewProjectAccount_OnClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string buttonId = button.Uid;

            Account_Window Account_Window = new Account_Window("3","3",buttonId);
            TabItem oTab = new TabItem();
            oTab.Content = Account_Window.Content;
            oTab.Header = "حساب جديد";
            main_tabControl.Items.Add(oTab);
            main_tabControl.SelectedIndex = main_tabControl.Items.Count - 1;
        }
        #endregion

        string selected_tab_header = "";

 
        
        private void NewBeneficiary_btn_OnClick(object sender, RoutedEventArgs e)
        { 
            All_Beneficiaries_Window all_Beneficiaries_Window = new All_Beneficiaries_Window();
            TabItem oTab = new TabItem();
            oTab.Content = all_Beneficiaries_Window.Content;
            oTab.Header = "المستفيدين";
            main_tabControl.Items.Add(oTab);
            main_tabControl.SelectedIndex = main_tabControl.Items.Count - 1;
        }

        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
            RibbonButton ribbonButton = new RibbonButton(); 
            ribbonButton.Width = 30;
            ribbonButton.Label = "New Button";

            RibbonTab tabItem = new RibbonTab();
            tabItem.Header = "My New Tab";
            RibbonGroup ribbonGroup = new RibbonGroup();
            ribbonGroup.Header = "My group";
            ribbonGroup.Items.Add(ribbonButton);

            tabItem.Items.Add(ribbonGroup);
            MyRibbon.Items.Add(tabItem); 
        }

        private void ViewProjectAccount_OnClick(object sender, RoutedEventArgs e)
        {
            // SubProject_ID = 1 for MicroProjects //
            //All_Accounts_Window all_Accounts_Window = new All_Accounts_Window(this, "1");
            //TabItem oTab = new TabItem();
            //oTab.Content = all_Accounts_Window.Content;
            //oTab.Header = "حسابات المشاريع";
            //main_tabControl.Items.Add(oTab);
            //main_tabControl.SelectedIndex = main_tabControl.Items.Count - 1;
        }

        private void NewMicroAccount_Click(object sender, RoutedEventArgs e)
        {
            // SubProject_ID = 1 for MicroProjects //
            Account_Window account_Window = new Account_Window("3","3","1");
            TabItem oTab = new TabItem();
            oTab.Content = account_Window.Content;
            oTab.Header = "حساب جديد";
            main_tabControl.Items.Add(oTab);
            main_tabControl.SelectedIndex = main_tabControl.Items.Count - 1;
        }

        private void FillSubPojects_OnClick(object sender, RoutedEventArgs e)
        {
            SubProjectsPanel.Visibility = Visibility.Visible; 
        }
          

        private void MainWindow1_Closed(object sender, EventArgs e)
        {
            var dialogResult = MessageBox.Show("هل أنت متأكدأنك تريد الخروج من البرنامج؟", "Confirm before exit", MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
