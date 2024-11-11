using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Cells;
using System.Windows;

namespace Financial_WPF_App.Helper
{
    public class GridCustomStackedRenderer : GridStackedHeaderCellRenderer
    {

        public GridCustomStackedRenderer()
        {

        }

        public override void OnInitializeEditElement(DataColumnBase dataColumn, GridStackedHeaderCellControl uiElement, object dataContext)
        {
            uiElement.Style = App.Current.Resources["SfDataGrid_StackedHeaderstyle"] as Style;
            base.OnInitializeEditElement(dataColumn, uiElement, dataContext);
        }
    }
}
