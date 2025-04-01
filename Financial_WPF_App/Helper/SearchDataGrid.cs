using Syncfusion.UI.Xaml.Grid;
using System.Data;
using System.Linq;

public class SearchDataGrid
{
    private DataTable _dataTable;

    public SearchDataGrid(DataTable dataTable)
    {
        _dataTable = dataTable.Copy(); // Store a copy to prevent modifications to the original data
    }

    public void Search(string searchText, SfDataGrid dataGrid)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            dataGrid.ItemsSource = _dataTable.DefaultView;
            return;
        }

        DataTable filteredDataTable = _dataTable.Clone(); // Copy structure, no data

        foreach (System.Data.DataRow row in _dataTable.Rows)
        {
            if (RowContainsText(row, searchText))
            {
                filteredDataTable.Rows.Add(row.ItemArray);
            }
        }

        dataGrid.ItemsSource = filteredDataTable.DefaultView;
    }

    private bool RowContainsText(System.Data.DataRow row, string searchText)
    {
        string lowerSearchText = searchText.ToLower();

        return row.ItemArray.Any(item =>
            item.ToString().ToLower().Contains(lowerSearchText));
    }
}
