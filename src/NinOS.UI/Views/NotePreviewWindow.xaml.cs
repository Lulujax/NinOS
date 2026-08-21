using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Views
{
    public partial class NotePreviewWindow : Window
    {
        private static readonly Brush PrimaryBrush = new SolidColorBrush(Color.FromRgb(0x1B, 0x3A, 0x2D));
        private static readonly Brush LightGrayBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xEC));
        private static readonly Brush BorderGrayBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
        private static readonly Brush RedBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x00));

        public NotePreviewWindow(note_print_dto note)
        {
            InitializeComponent();
            Title = $"Vista Previa - Nota {note.note_number}";
            BuildPreview(note);
        }

        private void BuildPreview(note_print_dto note)
        {
            var p = NotePanel;

            p.Children.Add(CreateTextBlock(note.company_name, 20, true, PrimaryBrush));
            p.Children.Add(CreateTextBlock("Caracas - Venezuela", 9, false, Brushes.Gray));
            p.Children.Add(CreateSeparator());

            var headerRow = new DockPanel { Margin = new Thickness(0, 5, 0, 0) };
            headerRow.Children.Add(CreateTextBlock($"VENDEDOR: {note.seller_name}", 10, true, PrimaryBrush));
            headerRow.Children.Add(CreateRightTextBlock($"NOTA DE ENTREGA Nro: {note.note_number}", 12, true, PrimaryBrush));
            p.Children.Add(headerRow);
            p.Children.Add(CreateSeparator());

            var gridBorder = new Border { BorderBrush = BorderGrayBrush, BorderThickness = new Thickness(1), Margin = new Thickness(0, 5, 0, 0) };
            var grid = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8)) };
            gridBorder.Child = grid;
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            AddLabelValueRow(grid, 0, "Razon Social", note.customer_business_name, "RIF", note.customer_rif);
            AddLabelValueRow(grid, 1, "Domicilio Fiscal", note.fiscal_address, "Direccion Entrega", note.customer_delivery_address);
            AddLabelValueRow(grid, 2, "Fecha Emision", note.creation_date.ToString("dd/MM/yyyy"), "Fecha Vencimiento", note.due_date.ToString("dd/MM/yyyy"));
            AddLabelValueRow(grid, 3, "Telefono", note.customer_phone, "Estado", note.status);
            p.Children.Add(gridBorder);

            p.Children.Add(CreateTextBlock("DETALLE DE PRODUCTOS", 10, true, PrimaryBrush, new Thickness(0, 10, 0, 4)));
            p.Children.Add(CreateDetailTable(note));

            var totals = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            totals.Children.Add(CreateRowLine("Subtotal:", $"{note.gross_total_usd:N2}", false));
            if (note.discount_amount > 0)
                totals.Children.Add(CreateRowLine($"Descuento ({note.discount_percentage:0}%):", $"-{note.discount_amount:N2}", false, RedBrush));
            totals.Children.Add(CreateSeparator());
            totals.Children.Add(CreateRowLine("TOTAL:", $"{note.total_amount_usd:N2}", true, PrimaryBrush));
            if (note.paid_amount_usd > 0)
                totals.Children.Add(CreateRowLine("Abonado:", $"{note.paid_amount_usd:N2}", false, new SolidColorBrush(Color.FromRgb(0x22, 0x8B, 0x22))));
            totals.Children.Add(CreateRowLine("SALDO:", $"{note.balance_due_usd:N2}", true, Brushes.Black));
            p.Children.Add(totals);

            p.Children.Add(new TextBlock
            {
                Text = "FECHA  /  FIRMA Y SELLO DEL CLIENTE",
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                Foreground = PrimaryBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 30)
            });
        }

        private TextBlock CreateTextBlock(string text, double size, bool bold, Brush? foreground = null, Thickness? margin = null)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                Foreground = foreground ?? Brushes.Black
            };
            if (margin.HasValue) tb.Margin = margin.Value;
            return tb;
        }

        private TextBlock CreateRightTextBlock(string text, double size, bool bold, Brush foreground)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Right
            };
        }

        private Border CreateSeparator()
        {
            return new Border
            {
                Height = 1,
                Background = BorderGrayBrush,
                Margin = new Thickness(0, 5, 0, 5)
            };
        }

        private void AddLabelValueRow(Grid grid, int row, string label1, string value1, string label2, string value2)
        {
            Grid.SetRow(grid, row);
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(CreateSmallLabel(label1));
            sp.Children.Add(CreateSmallValue(value1));
            sp.Children.Add(new TextBlock { Text = "    " });
            sp.Children.Add(CreateSmallLabel(label2));
            sp.Children.Add(CreateSmallValue(value2));
            grid.Children.Add(sp);
            Grid.SetRow(sp, row);
        }

        private TextBlock CreateSmallLabel(string text)
        {
            return new TextBlock { Text = text + ": ", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.Gray };
        }

        private TextBlock CreateSmallValue(string text)
        {
            return new TextBlock { Text = text ?? "", FontSize = 10 };
        }

        private DataGrid CreateDetailTable(note_print_dto note)
        {
            var dg = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HorizontalGridLinesBrush = BorderGrayBrush,
                VerticalGridLinesBrush = BorderGrayBrush,
                BorderBrush = BorderGrayBrush,
                BorderThickness = new Thickness(1),
                FontSize = 10,
                RowHeight = 26,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                AlternatingRowBackground = LightGrayBrush
            };

            var style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, PrimaryBrush));
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 9.0));
            dg.ColumnHeaderStyle = style;

            dg.Columns.Add(new DataGridTextColumn { Header = "CANT.", Binding = new System.Windows.Data.Binding("quantity"), Width = 50 });
            dg.Columns.Add(new DataGridTextColumn { Header = "DESCRIPCION", Binding = new System.Windows.Data.Binding("name"), Width = 280 });
            dg.Columns.Add(new DataGridTextColumn { Header = "PRECIO U.", Binding = new System.Windows.Data.Binding("unit_price_usd") { StringFormat = "{0:N2}" }, Width = 75 });
            dg.Columns.Add(new DataGridTextColumn { Header = "PRECIO P.", Binding = new System.Windows.Data.Binding("promo_price_usd") { StringFormat = "{0:N2}" }, Width = 75 });
            dg.Columns.Add(new DataGridTextColumn { Header = "SUBTOTAL", Binding = new System.Windows.Data.Binding("subtotal_usd") { StringFormat = "{0:N2}" }, Width = 85 });
            dg.Columns.Add(new DataGridTextColumn { Header = "CODIGO", Binding = new System.Windows.Data.Binding("code"), Width = 80 });

            foreach (var d in note.details)
                dg.Items.Add(d);

            return dg;
        }

        private DockPanel CreateRowLine(string left, string right, bool bold, Brush? foreground = null)
        {
            var dp = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            var leftTb = new TextBlock
            {
                Text = left,
                FontSize = 10,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = foreground ?? Brushes.Black
            };
            var rightTb = new TextBlock
            {
                Text = right,
                FontSize = 10,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = foreground ?? Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(rightTb, Dock.Right);
            dp.Children.Add(rightTb);
            dp.Children.Add(leftTb);
            return dp;
        }
    }
}
