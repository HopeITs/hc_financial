using Syncfusion.UI.Xaml.Grid;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Financial_WPF_App.Helper
{
    internal class Approval_CustomAlternatingRowStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var row = (item as DataRowBase).RowData as DataRowView;

            string x = row.Row["edited_by"].ToString();
            if (!String.IsNullOrEmpty(x) && !String.IsNullOrWhiteSpace(x))
                return App.Current.Resources["rowStyle1"] as Style;

            return App.Current.Resources["rowStyle2"] as Style;
        }
    }
}
