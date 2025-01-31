using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
namespace Financial_WPF_App
{
    internal class TreeViewItemHeaderContainsWordConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string header && parameter is string keyword)
            {
                return header.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0? (SolidColorBrush)Application.Current.Resources["HopeRed"] : (SolidColorBrush)Application.Current.Resources["DarkGray"];
            }
            return (SolidColorBrush)Application.Current.Resources["DarkGray"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
