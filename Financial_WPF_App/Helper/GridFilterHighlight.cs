using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Financial_WPF_App.Helper
{
    /// <summary>
    /// Makes a filtered column obvious. Syncfusion's grey funnel looks the same whether a column is
    /// filtered or not, so a user could read a grid without realising part of it is hidden. With
    /// this on, the funnel of every filtered column - in its usual place after the header text -
    /// turns into a light funnel on a disc of the grid's theme colour (the colour of the line
    /// under its headers), and the header's tooltip lists the values filtered on. The header
    /// itself is not restyled. Clearing the filter puts it back exactly as it was.
    ///
    /// Switched on for every grid by the shared SfDataGrid_body style, so no screen needs a
    /// change. Header texts are never touched - code elsewhere finds columns by them.
    /// </summary>
    public static class GridFilterHighlight
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(GridFilterHighlight), new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject o) { return (bool)o.GetValue(IsEnabledProperty); }
        public static void SetIsEnabled(DependencyObject o, bool value) { o.SetValue(IsEnabledProperty, value); }

        // the header style a column had before it was highlighted, to give it back afterwards
        private static readonly DependencyProperty OriginalHeaderStyleProperty = DependencyProperty.RegisterAttached(
            "OriginalHeaderStyle", typeof(Style), typeof(GridFilterHighlight), new PropertyMetadata(null));
        private static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.RegisterAttached(
            "IsHighlighted", typeof(bool), typeof(GridFilterHighlight), new PropertyMetadata(false));

        private static readonly Brush Fallback = new SolidColorBrush(Color.FromRgb(0xE0, 0x8A, 0x1E));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SfDataGrid grid = d as SfDataGrid;
            if (grid == null) return;

            grid.FilterChanged -= Grid_FilterChanged;
            grid.ItemsSourceChanged -= Grid_ItemsSourceChanged;
            if ((bool)e.NewValue)
            {
                grid.FilterChanged += Grid_FilterChanged;
                grid.ItemsSourceChanged += Grid_ItemsSourceChanged;
            }
        }

        private static void Grid_FilterChanged(object sender, GridFilterEventArgs e) { Refresh(sender as SfDataGrid); }

        // a new data source can arrive with its filters cleared
        private static void Grid_ItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e) { Refresh(sender as SfDataGrid); }

        /// <summary>Re-reads which columns are filtered and paints their headers.</summary>
        public static void Refresh(SfDataGrid grid)
        {
            if (grid == null) return;
            try
            {
                foreach (GridColumn column in grid.Columns)
                {
                    bool filtered = column.FilterPredicates != null && column.FilterPredicates.Count > 0;
                    bool highlighted = (bool)column.GetValue(IsHighlightedProperty);

                    if (filtered)
                    {
                        if (!highlighted)
                            column.SetValue(OriginalHeaderStyleProperty, column.HeaderStyle);
                        column.HeaderStyle = BuildStyle(grid, (Style)column.GetValue(OriginalHeaderStyleProperty), column);
                        column.SetValue(IsHighlightedProperty, true);
                    }
                    else if (highlighted)
                    {
                        // A column with no style of its own takes the grid's - but setting it back
                        // to null leaves the header cell already drawn without any, so give it the
                        // grid's header style explicitly in that case.
                        Style original = (Style)column.GetValue(OriginalHeaderStyleProperty);
                        column.HeaderStyle = original ?? grid.HeaderStyle;
                        column.ClearValue(OriginalHeaderStyleProperty);
                        column.SetValue(IsHighlightedProperty, false);
                    }
                }
            }
            catch (Exception)
            {
                // a cosmetic aid must never stop a screen from working
            }
        }

        /// <summary>
        /// The column's own header style, unchanged, plus a tooltip naming the filter and a style
        /// for the filter button inside this header only: the grey funnel, in its usual place
        /// after the header text, becomes a light funnel on a disc of the theme colour.
        /// </summary>
        private static Style BuildStyle(SfDataGrid grid, Style original, GridColumn column)
        {
            Style baseStyle = original ?? grid.HeaderStyle;
            Style style = baseStyle != null && baseStyle.TargetType == typeof(GridHeaderCellControl)
                ? new Style(typeof(GridHeaderCellControl), baseStyle)
                : new Style(typeof(GridHeaderCellControl));

            Brush accent = AccentOf(baseStyle) ?? Fallback;
            style.Resources.Add(typeof(FilterToggleButton), FilteredButtonStyle(accent));
            style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, Describe(column)));
            return style;
        }

        // Syncfusion's own funnel, so the filtered icon is the same shape as the one the user knows
        private const string Funnel = "F1M-2124.61,-1263.65L-2131.54,-1263.72 -2145.51,-1263.84 -2152.41,-1263.9C-2155.99,-1263.93,-2157.48,-1262.16,-2155.7,-1259.96L-2152.05,-1255.43C-2150.28,-1253.23,-2147.38,-1249.62,-2145.61,-1247.42L-2143.25,-1244.5 -2143.25,-1230.24C-2143.25,-1229.23,-2142.43,-1228.42,-2141.42,-1228.42L-2135.64,-1228.42C-2134.63,-1228.42,-2133.81,-1229.23,-2133.81,-1230.24L-2133.81,-1244.78 -2131.7,-1247.3C-2129.89,-1249.47,-2126.93,-1253.02,-2125.12,-1255.18L-2121.39,-1259.65C-2119.57,-1261.82,-2121.02,-1263.62,-2124.61,-1263.65z";

        /// <summary>The filter button of a filtered column: a light funnel on a theme-coloured disc.</summary>
        private static Style FilteredButtonStyle(Brush accent)
        {
            var template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                "xmlns:sf='http://schemas.syncfusion.com/wpf' TargetType='sf:FilterToggleButton'>" +
                "<Grid SnapsToDevicePixels='True' Background='Transparent'>" +
                "<Ellipse Width='18' Height='18' Fill='{TemplateBinding Background}'/>" +
                "<Path Name='PART_FilterToggleButtonIndicator' Data='" + Funnel + "' Stretch='Uniform' " +
                "Width='9' Height='9' Margin='0,1,0,0' Fill='{TemplateBinding Foreground}' SnapsToDevicePixels='True'/>" +
                "</Grid></ControlTemplate>");

            var style = new Style(typeof(FilterToggleButton));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            style.Setters.Add(new Setter(Control.BackgroundProperty, accent));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0xF1, 0xF2, 0xF2))));
            style.Setters.Add(new Setter(FrameworkElement.WidthProperty, 20.0));
            style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 20.0));
            return style;
        }

        /// <summary>The colour of the line under the headers, which is the screen's theme colour.</summary>
        private static Brush AccentOf(Style style)
        {
            for (Style s = style; s != null; s = s.BasedOn)
                foreach (Setter setter in s.Setters.OfType<Setter>())
                    if (setter.Property == Control.BorderBrushProperty && setter.Value is Brush)
                        return (Brush)setter.Value;
            return null;
        }

        private static string Describe(GridColumn column)
        {
            var values = new List<string>();
            foreach (FilterPredicate p in column.FilterPredicates)
            {
                string v = p.FilterValue == null ? "(blank)" : Convert.ToString(p.FilterValue);
                if (string.IsNullOrWhiteSpace(v)) v = "(blank)";
                if (p.FilterType != FilterType.Equals)
                    v = p.FilterType + " " + v;
                if (!values.Contains(v)) values.Add(v);
            }

            const int shown = 15;
            string list = string.Join(", ", values.Take(shown));
            if (values.Count > shown) list += " ... (+" + (values.Count - shown) + " more)";
            return "Filtered by: " + list;
        }
    }
}
