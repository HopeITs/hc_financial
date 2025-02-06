
using Financial_WPF_App.Models;
using Financial_WPF_App.Properties;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Financial_WPF_App
{
    public partial class Login_Window2 : Window
    {
        public Login_Window2(Main_Window_2 main_Window)
        {
            InitializeComponent();

            this.main_Window = main_Window;
            On_Load();
        }

        private void On_Load()
        {
            try
            {
                // add events to the storyboards
                Storyboard sb1 = this.FindResource("Login_Click_Animation") as Storyboard;
                sb1.Completed += new EventHandler(sb1_Completed);

                Storyboard sb2 = this.FindResource("Shutdown_Pressed") as Storyboard;
                sb2.Completed += new EventHandler(Shutdown_Pressed_onCompleted);

                if (Settings.Default.update_version)
                {
                    Settings.Default.Upgrade();
                    Settings.Default.update_version = false;
                    Settings.Default.Save();
                }

                Settings.Default.Online = true;
                Settings.Default.Save();
                if (Settings.Default.RememberMe)
                {
                    Remember_checkBox_Copy.IsChecked = true;
                    Remember_Me_Pressed_BeginStoryboard1.Storyboard.Begin();

                    if (Settings.Default.username != "" && Settings.Default.password != "")
                    {
                        username_txt.Text = Settings.Default.username;
                        password_txt1.Password = Settings.Default.password;
                    }
                }
                else
                {
                    Remember_checkBox_Copy.IsChecked = false;
                    Remembe_me_unpressed_BeginStoryboard1.Storyboard.Begin();

                    if (Settings.Default.username != "")
                        username_txt.Text = Settings.Default.username;
                    else username_txt.Text = "username";
                    password_txt1.Password = "";
                }
                username_txt.Select(username_txt.Text.Length, username_txt.Text.Length);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private Users u;
        private DataTable user_dt;
        Main_Window_2 main_Window;
        bool Sb1Completed = false;

        private void WEB_Butten_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://hcsyria.org");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Login_Butten_Click_1(object sender, RoutedEventArgs e)
        {
            Storyboard sb1 = this.FindResource("Login_Click_Animation") as Storyboard;
            sb1.Begin();
        }

        private void sb1_Completed(object sender, EventArgs e)
        {
            try
            {
                if (Settings.Default.Online)
                    Login.Set_ConnectionString(true, 'h'); //online
                else
                    Login.Set_ConnectionString(false, 'h'); //offline

                if (Login.Login_BuildConnection())
                {
                    if(!Sb1Completed)
                    {
                        if(main_Window.Check_App_Version())
                        {
                            u = new Users();
                            user_dt = u.Get_User(username_txt.Text, password_txt1.Password);
                            if (user_dt.Rows.Count == 1) // user exsist
                            {
                                var User_ID = user_dt.Rows[0].Field<int>(0);
                                var role = user_dt.Rows[0].Field<string>(3);
                                var CashAccount_Cahier = user_dt.Rows[0].Field<string>(4);
                                var user_accounts = user_dt.Rows[0].Field<string>(5);
                                var User_CashAccounts = user_dt.Rows[0].Field<string>(6);
                                var User_Subprojects = user_dt.Rows[0].Field<string>(7);


                                if (Remember_checkBox_Copy.IsChecked == true)
                                {
                                    //save properties settings
                                    Settings.Default.RememberMe = true;
                                    Settings.Default.username = username_txt.Text;
                                    Settings.Default.password = password_txt1.Password;

                                    //if (Settings.Default.Online)
                                    //    Settings.Default.Online = true;
                                    //else
                                    //    Settings.Default.Online = false;
                                }
                                else
                                {
                                    //clear properties settings
                                    Settings.Default.RememberMe = false;
                                    Settings.Default.username = username_txt.Text;
                                    Settings.Default.password = "";
                                    //Settings.Default.Online = false;
                                }

                                Settings.Default.Role = role;
                                Settings.Default.User_ID = User_ID;
                                Settings.Default.CashAccount_Cashier = CashAccount_Cahier;
                                Settings.Default.User_Accounts = user_accounts;
                                Settings.Default.User_CashAccounts = User_CashAccounts;
                                Settings.Default.User_Subprojects = User_Subprojects;

                                Settings.Default.Save();
                                string Query = " INSERT INTO log (form_name, title, affected_table, affected_id, details, additionaldetails, query, date, user) VALUES ('Login_Window2', 'LOGIN', '', '', '', '' , '', DATE(NOW()), '" + Properties.Settings.Default.username + "');";
                                Login.Execute_Query(Query);
                                Sb1Completed = true;
                                this.DialogResult = true;
                            }
                            else
                            {
                                Storyboard sb = this.FindResource("User_False") as Storyboard;
                                sb.Begin();
                            }
                        }         
                    }
                }
                else
                {
                    Storyboard sb = this.FindResource("Server_False") as Storyboard;
                    sb.Begin();
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL"))
                {
                    Storyboard sb = this.FindResource("Network_False") as Storyboard;
                    sb.Begin();
                }
                else if (ex.Message.Contains("host"))
                {
                    Storyboard sb = this.FindResource("Server_False") as Storyboard;
                    sb.Begin();
                }
                else
                {
                    Storyboard sb = this.FindResource("Other_False") as Storyboard;
                    sb.Begin();

                    MessageBox.Show(ex.Message, "Error");
                }
   
            }
        }

        private void Remember_Me_Butten_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Remember_checkBox_Copy.IsChecked = false;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Remember_Me_Butten_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Remember_checkBox_Copy.IsChecked = true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void Online_Butten_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings.Default.Online = !Settings.Default.Online;
                Settings.Default.Save();
                Online_Working_Loop_BeginStoryboard1.Storyboard.RepeatBehavior = RepeatBehavior.Forever;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }
        }

        private void OnPasswordChange(object sender, RoutedEventArgs e)
        {
            try
            {
                if (password_txt1.Password == "")
                { Password.Visibility = Visibility.Visible; }
                else { Password.Visibility = Visibility.Hidden; }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void Shutdown_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to close the program?", "", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                {
                    e.Handled = true;
                }
                else
                {
                    Login.Shutdown_Mode = new ShutdownMode();
                    //Shutdown_Pressed_BeginStoryboard.Storyboard.BeginTime = TimeSpan.FromSeconds(2);
                    //Shutdown_Pressed_BeginStoryboard.Storyboard.Begin();
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Return)
                {
                    Login_Butten_Click_1(sender, e);
                }

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
            }

        }

        private void Shutdown_Pressed_onCompleted(object sender, EventArgs e)
        {
            try
            {
                Thread.Sleep(500);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("MySql") || ex.Message.Contains("MySQL")) { MessageBox.Show("please check your network and try again", "Error"); } else if (ex.Message.Contains("host")) { MessageBox.Show("please check the server or your network connection and try again", "Error"); } else if (ex.Message.Contains("Duplicate entry")) { MessageBox.Show("Error … There are duplicate values.", "Error"); } else { MessageBox.Show(ex.Message, "Error"); }
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

    }
}
