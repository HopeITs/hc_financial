using Microsoft.Win32;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.XlsIO;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Financial_WPF_App.Helper
{
    internal class Commands
    {
        static Commands()
        {
            CommandManager.RegisterClassCommandBinding(typeof(SfDataGrid), new CommandBinding(ExportToExcel, OnExecuteExportToExcel, OnCanExecuteExportToExcel));
        }

        #region ExportToExcel Command

        public static RoutedCommand ExportToExcel = new RoutedCommand("ExportToExcel", typeof(SfDataGrid));


        private static void OnExecuteExportToExcel(object sender, ExecutedRoutedEventArgs args)
        {
            var dataGrid = args.Source as SfDataGrid;
            if (dataGrid == null) return;
            dataGrid.ApplyTemplate();

            var dataGrid2 = args.Parameter as SfDataGrid;
            try
            {
                ExcelExportingOptions options = new ExcelExportingOptions();

                options.AllowOutlining = true;
                options.ExportStackedHeaders = true;

                if (dataGrid.View == null || options == null) return;

                foreach (var column in dataGrid.Columns)
                {
                    if (column.IsHidden || column.HeaderText == "Attachment"
                        || column.HeaderText == "Edit"
                        || column.HeaderText == "Delete"
                        || column.HeaderText == "Check"
                        || column.HeaderText == "Monthly Salary")
                        options.ExcludeColumns.Add(column.MappingName);
                }

                ExcelEngine excelEngine = new ExcelEngine();
                IWorkbook workBook = excelEngine.Excel.Workbooks.Create();


                dataGrid.ExportToExcel(dataGrid.View, options, workBook.Worksheets[0]);

 

                IConditionalFormats condition = workBook.Worksheets[0].UsedRange.ConditionalFormats;
                IConditionalFormat condition1 = condition.AddCondition();
                condition1.FormatType = ExcelCFType.Formula;
                condition1.FirstFormula = "MOD(ROW(),2)=0";
                condition1.BackColorRGB = System.Drawing.Color.LightBlue;
                IConditionalFormat condition2 = condition.AddCondition();
                condition2.FormatType = ExcelCFType.Formula;
                condition2.FirstFormula = "MOD(ROW(),2)=1";
                condition2.BackColorRGB = System.Drawing.Color.White;
                var workSheet = workBook.Worksheets[0];


                workSheet.UsedRange.AutofitColumns();
                workBook.Worksheets[0].UsedRange.BorderInside(ExcelLineStyle.Thin, ExcelKnownColors.Black);
                workBook.Worksheets[0].UsedRange.BorderAround(ExcelLineStyle.Thin, ExcelKnownColors.Black);
                workBook.Worksheets[0].AutoFilters.FilterRange = workBook.Worksheets[0].UsedRange;


                if (dataGrid2 != null)
                {
                    dataGrid2.ApplyTemplate();
                    if (dataGrid2.View != null)
                    {
                        foreach (var column in dataGrid2.Columns)
                        {
                            if (column.IsHidden || column.HeaderText == "Attachment"
                                || column.HeaderText == "Edit"
                                || column.HeaderText == "Delete"
                                || column.HeaderText == "Check")
                                options.ExcludeColumns.Add(column.MappingName);
                        }

                        dataGrid2.ExportToExcel(dataGrid2.View, options, workBook.Worksheets[1]);


                        condition = workBook.Worksheets[1].UsedRange.ConditionalFormats;
                        condition1 = condition.AddCondition();
                        condition1.FormatType = ExcelCFType.Formula;
                        condition1.FirstFormula = "MOD(ROW(),2)=0";
                        condition1.BackColorRGB = System.Drawing.Color.LightBlue;
                        condition2 = condition.AddCondition();
                        condition2.FormatType = ExcelCFType.Formula;
                        condition2.FirstFormula = "MOD(ROW(),2)=1";
                        condition2.BackColorRGB = System.Drawing.Color.White;
                        workSheet = workBook.Worksheets[1];
                        workSheet.UsedRange.AutofitColumns();
                        workBook.Worksheets[1].UsedRange.BorderInside(ExcelLineStyle.Thin, ExcelKnownColors.Black);
                        workBook.Worksheets[1].UsedRange.BorderAround(ExcelLineStyle.Thin, ExcelKnownColors.Black);
                        workBook.Worksheets[1].AutoFilters.FilterRange = workBook.Worksheets[1].UsedRange;
                    }
                }


                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "(*.xlsx)|*.xlsx"
                };

                if (sfd.ShowDialog() == true)
                {
                    using (Stream stream = sfd.OpenFile())
                    {
                        if (sfd.FilterIndex == 1)
                            workBook.Version = ExcelVersion.Excel97to2003;
                        else
                            workBook.Version = ExcelVersion.Excel2010;
                        workBook.SaveAs(stream);
                    }

                    //Message box confirmation to view the created spreadsheet.
                    if (MessageBox.Show("Do you want to view the workbook?", "Workbook has been created",
                                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        //Launching the Excel file using the default Application.[MS Excel Or Free ExcelViewer]
                        System.Diagnostics.Process.Start(sfd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static void OnCanExecuteExportToExcel(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }
        #endregion
    }
}
