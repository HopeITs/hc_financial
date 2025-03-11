using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Cells;
using System.Drawing;
using System.Windows;

namespace Financial_WPF_App.Helper
{
    public class GridCustomStackedRenderer : GridStackedHeaderCellRenderer
    {
        private System.Windows.Media.Brush borderColor;

        // Constructor to accept border color
        public GridCustomStackedRenderer(System.Windows.Media.Brush borderColor)
        {
            this.borderColor = borderColor;
        }

        public override void OnInitializeEditElement(DataColumnBase dataColumn, GridStackedHeaderCellControl uiElement, object dataContext)
        {
            uiElement.Style = App.Current.Resources["SfDataGrid_StackedHeaderstyle"] as Style;

            // Apply BorderBrush and BorderThickness
            uiElement.BorderBrush = borderColor;
            uiElement.BorderThickness = new Thickness(0, 0, 0, 3);

            base.OnInitializeEditElement(dataColumn, uiElement, dataContext);
        }
    }

}
