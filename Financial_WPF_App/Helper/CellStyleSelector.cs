using Syncfusion.UI.Xaml.Grid;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Financial_WPF_App.Helper
{

    public class CellStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {

            var row = item as DataRowView;


            string ColumnName = (container as GridCell).ColumnBase.GridColumn.MappingName.ToString();

            if (!ColumnName.Contains("ID") && ColumnName != "Budget Line Type" && ColumnName != "Budget Line" && ColumnName != "Timing" &&
                !ColumnName.Contains("Expected Total") && !ColumnName.Contains("Remaining"))
            {
                double MonthlyTotal = 0;
                double MonthlyEx = 0;

                double Total = 0;
                double YearlyEx = 0;

                string DonorName = "";


                if(ColumnName.EndsWith("Expenditures Total"))
                {
                    if (ColumnName.Length > 19)
                        DonorName = ColumnName.Substring(0, ColumnName.Length - 19);

                    YearlyEx = Convert.ToDouble(row.Row[ColumnName].ToString());

                    if (DonorName.Length > 0)
                    {
                        Total = Convert.ToDouble((row.Row[DonorName + " Expected Total - Budget"].ToString()));
                    }
                    else
                    {
                        Total = Convert.ToDouble((row.Row["Expected Total - Budget"].ToString()));
                    }


                    if (YearlyEx > Total)
                        return App.Current.Resources["redCellStyle"] as Style;
                    else
                        return base.SelectStyle(item, container);
                }
                else if(!ColumnName.Contains("Expenditures Total"))
                {
                    if (!ColumnName.Contains("Out of budget date"))
                    {
                        if (ColumnName.Length > 9)
                            DonorName = ColumnName.Substring(0, ColumnName.Length - 9);
                    }
                    else
                    {
                        if (ColumnName.Length > 19)
                            DonorName = ColumnName.Substring(0, ColumnName.Length - 19);
                    }

                    MonthlyEx = Convert.ToDouble(row.Row[ColumnName].ToString());

                    if (DonorName.Length > 0)
                    {
                        MonthlyTotal = Convert.ToDouble((row.Row[DonorName + " Expected Total - Month"].ToString()));
                    }
                    else
                    {
                        MonthlyTotal = Convert.ToDouble((row.Row["Expected Total - Month"].ToString()));
                    }

                    if (MonthlyEx > MonthlyTotal)
                        return App.Current.Resources["redCellStyle"] as Style;
                    else
                        return base.SelectStyle(item, container);
                }

            }

            return base.SelectStyle(item, container);
        }
    }

}
