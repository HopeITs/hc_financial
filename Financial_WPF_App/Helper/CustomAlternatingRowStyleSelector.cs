using Syncfusion.UI.Xaml.Grid;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Financial_WPF_App.Helper
{
    internal class CustomAlternatingRowStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var row = (item as DataRowBase).RowData as DataRowView;

            int x = Convert.ToInt32(row.Row["Late payments"].ToString());
            if (x >= 3)
                return App.Current.Resources["rowStyle1"] as Style;

            return App.Current.Resources["rowStyle2"] as Style;
        }
    }
}
