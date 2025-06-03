using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Financial_WPF_App.Helper
{
    public class LocExtension : MarkupExtension
    {
        private static readonly ResourceManager _resourceManager = Properties.Resources.ResourceManager;

        public string Key { get; set; }

        public LocExtension() { }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return "!EmptyKey!";

            var binding = new Binding($"[{Key}]")
            {
                Source = TranslationProvider.Instance,
                Mode = BindingMode.OneWay
            };

            return binding.ProvideValue(serviceProvider);
        }
    }

    public class TranslationProvider : INotifyPropertyChanged
    {
        private static TranslationProvider _instance;
        public static TranslationProvider Instance => _instance ?? (_instance = new TranslationProvider());

        private static readonly ResourceManager _resourceManager = Properties.Resources.ResourceManager;

        public string this[string key] => _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"!{key}!";

        public void RaiseLanguageChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty)); // Update all keys
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
