using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Financial_WPF_App.Helper
{
    internal class CultureManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static CultureManager _instance = new CultureManager();
        public static CultureManager Instance => _instance;
        public static event Action LanguageChanged;
        public void ChangeCulture(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;

            TranslationProvider.Instance.RaiseLanguageChanged();
            LanguageChanged?.Invoke();

        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public static void UpdateAllWindowsFlowDirection()
        {
            var direction = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            foreach (Window window in Application.Current.Windows)
            {
                window.FlowDirection = direction;
            }
        }

        public static FlowDirection GetCurrentFlowDirection()
        {
            return CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }
    }
}
