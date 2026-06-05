using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Shapes;
using Financial_WPF_App.Controls;
using Financial_WPF_App.Helper;


namespace Financial_WPF_App.Views
{
    public partial class AddAction_Window2 : Window
    {
        // ================= CONTROLS =================

        private readonly UC_IncomeOperation _incomeControl = new UC_IncomeOperation();
        private readonly UC_ExpensesOperation _expensesControl = new UC_ExpensesOperation();
        private readonly UC_ExchangeOperation _exchangeControl = new UC_ExchangeOperation();
        private readonly UC_TransferOperation _transferControl = new UC_TransferOperation();

        // الحقول المطلوبة لحل الأخطاء المالية والبرمجية
        private DataTable tills_dt;                   // لبيانات الصناديق المفلترة
        private Models.Till t = new Models.Till();    // كائن جلب بيانات الصناديق
        private Main_Window_2 main_Window;             // مرجع الشاشة الرئيسية لجلب الحسابات

        public AddAction_Window2(Main_Window_2 main_Window)
        {
            // 1. إسناد المرجع أولاً وقبل كل شيء لضمان عدم حدوث NullReference
            this.main_Window = main_Window;

            // 2. بناء عناصر الواجهة الرسومية
            InitializeComponent();

            // 3. تهيئة التواريخ والإعدادات الافتراضية
            InitializeWindow();

            // 4. استدعاء تعبئة الحسابات والصناديق بعد أمان المراجع
            Fill_Main_Comboboxs();

        }

        // ================= INITIALIZE =================

        private void InitializeWindow()
        {
            ActionDate_datepicker.SelectedDate = System.DateTime.Today;

            ConfigureAvailableOperations();
        }

        private void Fill_Main_Comboboxs()
        {
            try
            {
                tills_dt = null;
                Subproject_comboBox.ItemsSource = null;

                // جلب وتصفية الصناديق بناءً على الصلاحيات والأمان المالي
                DataTable temp_tills_dt = t.Select_All_tills();
                tills_dt = Helper.FinancialAuthorizationService.FilterTillsByRole(temp_tills_dt, main_Window.AllAccounts_ds);

                if (tills_dt != null && tills_dt.Rows.Count > 0)
                {
                    DataView view = new DataView(tills_dt);
                    DataTable selected_dt = view.ToTable(true, "Subproject_ID", "Subproject_Name");

                    if (selected_dt != null && selected_dt.Rows.Count > 0)
                    {
                        // 1. تعبئة الكومبوبوكس الرئيسي المتواجد في الشاشة الأساسية
                        Subproject_comboBox.ItemsSource = selected_dt.DefaultView;
                        Subproject_comboBox.DisplayMemberPath = "Subproject_Name";
                        Subproject_comboBox.SelectedValuePath = "Subproject_ID";

                        // 2. تجهيز بيانات الحسابات والوجهات المحسنة محاسبياً للتحويلات
                        DataTable dest_dt = selected_dt.Copy();
                        dest_dt.Columns.Add("Display_Name", typeof(string));

                        foreach (DataRow r in dest_dt.Rows)
                        {
                            var idObj = r["Subproject_ID"];
                            var name = (r["Subproject_Name"]?.ToString() ?? "").Trim();

                            bool isPersonal = (idObj == DBNull.Value || Convert.ToInt32(idObj) == 0) ||
                                              name.IndexOf("Personal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0;

                            r["Display_Name"] = isPersonal ? "Personal" : name;
                        }

                        // 3. التمرير الآمن للـ UserControl المسؤول عن عمليات التحويل
                        _transferControl.FillDestinationProjects(dest_dt.DefaultView);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في مزامنة وتعبئة الحسابات البينية: {ex.Message}", "خطأ نظام", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ميثود تطبيق الصلاحيات على الواجهة الجديدة لتفعيل أو تعطيل خيارات العمليات
        private void ApplyOperationPrivileges(string accountType, string tillType)
        {
            try
            {
                // 1. الحل الجذري لمنع استثناء (Items collection must be empty):
                // نقوم بمسح التعبئة الديناميكية والثابتة معاً قبل التعبئة الجديدة
                Type_comboBox.ItemsSource = null;
                Type_comboBox.Items.Clear();

                string currentRole = Financial_WPF_App.Properties.Settings.Default.Role;

                // 2. استدعاء الـ Helper المالي الخاص بك بالأسماء الصافية
                var privileges = Helper.FinancialAuthorizationService.GetOperationPrivileges(currentRole, accountType, tillType);

                var availableOperations = new List<object>();

                // 3. الالتزام التام بالمسميات المختصرة للواجهة القديمة والـ Switch Case القائم لديك
                if (privileges.Income)
                {
                    availableOperations.Add(new { Text = "Income", Value = "Income" });
                }

                if (privileges.Outcome)
                {
                    availableOperations.Add(new { Text = "Expenses", Value = "Expenses" }); // متوافق مع شاشتك المتوقعة Expenses
                }

                if (privileges.Exchange)
                {
                    availableOperations.Add(new { Text = "Exchange", Value = "Exchange" });
                }

                if (privileges.Move)
                {
                    availableOperations.Add(new { Text = "Transfer", Value = "Transfer" }); // متوافق مع شاشتك المتوقعة Transfer
                }

                // 4. ربط القائمة بالـ ComboBox
                Type_comboBox.ItemsSource = availableOperations;
                Type_comboBox.DisplayMemberPath = "Text";
                Type_comboBox.SelectedValuePath = "Value";

                // 5. اختيار أول عملية متاحة تلقائياً لتنشيط الواجهة
                if (Type_comboBox.Items.Count > 0)
                {
                    Type_comboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ApplyOperationPrivileges: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= OPERATION SELECTION =================

        private void Type_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // تعديل الفحص ليناسب التعبئة الديناميكية الجديدة (سواء كائن مجهول أو ComboBoxItem)
            if (Type_comboBox.SelectedValue == null)
            {
                DynamicOperationContainer.Content = null;
                return;
            }

            // جلب القيمة البرمجية الصافية للعملية (Income, Expenses, Exchange, Transfer)
            string operationType = Type_comboBox.SelectedValue.ToString();

            switch (operationType)
            {
                case "Income":
                    ShowIncomeOperation();
                    break;

                case "Expenses":
                    ShowExpensesOperation();
                    break;

                case "Exchange":
                    ShowExchangeOperation();
                    break;

                case "Transfer":
                    ShowTransferOperation();
                    break;

                default:
                    DynamicOperationContainer.Content = null;
                    break;
            }
        }

        // ================= SHOW OPERATIONS =================

        private void ShowOperation(UserControl control)
        {
            DynamicOperationContainer.Content = control;
        }

        private void ShowIncomeOperation()
        {
            ShowOperation(_incomeControl);
        }

        private void ShowExpensesOperation()
        {
            ShowOperation(_expensesControl);
        }

        private void ShowExchangeOperation()
        {
            ShowOperation(_exchangeControl);
        }

        private void ShowTransferOperation()
        {
            ShowOperation(_transferControl);
        }

        // ================= CONFIGURATION =================

        private void ConfigureAvailableOperations()
        {
            // لاحقاً تضيف منطق العملات هون
        }

        // ================= EVENTS =================

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Subproject_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // الحل الجذري: تصفير الـ ItemsSource أولاً للتخلص من أي ربط قديم
                Account_comboBox.ItemsSource = null;
                // تصفير وعزل الـ Items للتأكد من خلو الكومبوبوكس تماماً قبل إعادة التعبئة
                Account_comboBox.Items.Clear();

                Till_comboBox.ItemsSource = null;
                Till_comboBox.Items.Clear();

                if (Subproject_comboBox.SelectedValue == null || tills_dt == null) return;

                int selectedSubprojectId = Convert.ToInt32(Subproject_comboBox.SelectedValue);

                DataView accountsView = new DataView(tills_dt);
                accountsView.RowFilter = $"Subproject_ID = {selectedSubprojectId}";

                DataTable distinctAccounts = accountsView.ToTable(true, "Account_ID", "Account_Name");

                if (distinctAccounts != null && distinctAccounts.Rows.Count > 0)
                {
                    // الآن الإسناد سيتم بأمان كامل وبدون استثناءات
                    Account_comboBox.ItemsSource = distinctAccounts.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصفية الحسابات: {ex.Message}", "خطأ نظام", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Account_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 1. تفريغ وتصفير القائمة تماماً لمنع أي استثناءات تعارض أو تكرار
                Till_comboBox.ItemsSource = null;
                Till_comboBox.Items.Clear();

                if (Account_comboBox.SelectedValue == null || tills_dt == null) return;

                int selectedAccountId = Convert.ToInt32(Account_comboBox.SelectedValue);

                // 2. تصفية الصناديق داخل الذاكرة بناءً على الحساب المختار
                DataView tillView = new DataView(tills_dt);
                tillView.RowFilter = $"Account_ID = {selectedAccountId}";

                // 3. استخراج الصناديق الفريدة باستخدام أسماء الأعمدة المطابقة للـ XAML تماماً
                // نختار "Till_ID" ليتطابق مع SelectedValuePath و "Tilltypes_Name" ليتطابق مع DisplayMemberPath
                DataTable distinctTills = tillView.ToTable(true, "Till_ID", "Tilltypes_Name");

                if (distinctTills != null && distinctTills.Rows.Count > 0)
                {
                    // 4. ربط البيانات المفلترة بالكومبوبوكس بأمان
                    Till_comboBox.ItemsSource = distinctTills.DefaultView;

                    // ملاحظة: لا داعي لإعادة كتابة DisplayMemberPath أو SelectedValuePath هنا في الـ C# 
                    // لأن الـ WPF سيقرأها تلقائياً وبنجاح من ملف الـ XAML الخاص بك.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تصفية وتعبئة الصناديق (Till): {ex.Message}", "خطأ نظام", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Till_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Till_comboBox.SelectedValue == null) return;

                // 1. جلب السطر المختار من الحساب لقراءة نوع الحساب (Project أو Personal) المتوقع بالـ Helper
                DataRowView selectedAccountRow = Account_comboBox.SelectedItem as DataRowView;
                string accountType = selectedAccountRow?["Account_Type"]?.ToString() ?? "Project"; // الافتراضي مشروع إذا لم يجد

                // 2. جلب السطر المختار من الصندوق لقراءة العملة بدقة (SYR, USD, EUR)
                DataRowView selectedTillRow = Till_comboBox.SelectedItem as DataRowView;
                if (selectedTillRow == null) return;

                string tillType = selectedTillRow["Tilltypes_Name"]?.ToString()?.ToUpper()?.Trim() ?? "";

                // 3. استدعاء الدالة لتطبيق الصلاحيات وربطها بالـ Type_comboBox فوراً
                ApplyOperationPrivileges(accountType, tillType);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تطبيق قيود العمليات للعملة: {ex.Message}", "خطأ صلاحيات ماليّة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Amount_txtBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void ActionDate_datepicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddedAttachments_clear_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Add_Attachment_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Save_btn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
