using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Views
{
    public partial class NotePreviewWindow : Window
    {
        private static readonly Brush PrimaryBrush = new SolidColorBrush(Color.FromRgb(0x1B, 0x3A, 0x2D));
        private static readonly Brush LabelGrayBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
        private static readonly Brush LightGrayBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xEC));
        private static readonly Brush BorderGrayBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
        private static readonly Brush DividerBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
        private static readonly Brush RedBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x00));
        private static readonly Brush GreenBrush = new SolidColorBrush(Color.FromRgb(0x22, 0x8B, 0x22));
        private static readonly Brush FooterGrayBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

        public NotePreviewWindow(note_print_dto note)
        {
            InitializeComponent();
            Title = $"Vista Previa - Nota {note.note_number}";
            BuildPreview(note);
        }

        private void BuildPreview(note_print_dto note)
        {
            var p = NotePanel;

            // CABECERA: empresa izquierda, NOTA DE ENTREGA derecha
            var header = new DockPanel { LastChildFill = true };
            var left = new StackPanel();
            left.Children.Add(MakeText(note.company_name, 22, true, PrimaryBrush));
            left.Children.Add(MakeText("Caracas - Venezuela", 10, false, LabelGrayBrush, new Thickness(0, 2, 0, 0)));
            DockPanel.SetDock(left, Dock.Left);
            var right = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            right.Children.Add(MakeText("NOTA DE ENTREGA", 16, true, PrimaryBrush));
            right.Children.Add(MakeText($"Nro: {note.note_number}", 11, true, Brushes.Black, new Thickness(0, 2, 0, 0)));
            DockPanel.SetDock(right, Dock.Right);
            header.Children.Add(right);
            header.Children.Add(left);
            p.Children.Add(header);
            p.Children.Add(MakeLine(2, PrimaryBrush, new Thickness(0, 8, 0, 0)));

            // VENDEDOR
            var sellerSp = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            sellerSp.Children.Add(MakeText("VENDEDOR:", 8, true, LabelGrayBrush));
            sellerSp.Children.Add(MakeText(note.seller_name, 10, true, Brushes.Black, new Thickness(0, 1, 0, 0)));
            p.Children.Add(sellerSp);

            // CAJA DE DATOS DEL CLIENTE
            var infoBox = MakeBox(padding: new Thickness(6));
            infoBox.Margin = new Thickness(0, 6, 0, 0);
            var infoGrid = new Grid();
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());

            var infoRow1 = MakeInfoRow(
                MakeInfoCell("Razon Social", note.customer_business_name),
                MakeInfoCell("RIF", note.customer_rif, 120));
            Grid.SetRow(infoRow1, 0);
            infoGrid.Children.Add(infoRow1);

            var divider1 = MakeLine(1, DividerBrush, new Thickness(0, 4, 0, 0));
            Grid.SetRow(divider1, 1);
            infoGrid.Children.Add(divider1);

            var infoRow2 = MakeInfoRow(
                MakeInfoCell("Domicilio Fiscal", note.fiscal_address),
                MakeInfoCell("Direccion de Entrega", note.customer_delivery_address));
            Grid.SetRow(infoRow2, 2);
            infoGrid.Children.Add(infoRow2);

            var divider2 = MakeLine(1, DividerBrush, new Thickness(0, 4, 0, 0));
            Grid.SetRow(divider2, 3);
            infoGrid.Children.Add(divider2);

            var infoRow3 = MakeTripleRow(
                MakeInfoCell("Fecha Emision", note.creation_date.ToString("dd/MM/yyyy")),
                MakeInfoCell("Fecha Vencimiento", note.due_date.ToString("dd/MM/yyyy")),
                MakeInfoCell("Telefono", note.customer_phone));
            Grid.SetRow(infoRow3, 4);
            infoGrid.Children.Add(infoRow3);

            infoBox.Child = infoGrid;
            p.Children.Add(infoBox);

            // DETALLE DE PRODUCTOS
            p.Children.Add(MakeText("DETALLE DE PRODUCTOS", 9, true, PrimaryBrush, new Thickness(0, 8, 0, 0)));
            p.Children.Add(MakeDetailTable(note));

            // TOTALES (caja derecha 220)
            var totalsWrap = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var totalsBox = MakeBox(padding: new Thickness(6));
            totalsBox.Width = 220;
            var totalsCol = new StackPanel();
            totalsCol.Children.Add(MakeTotalsRow("Subtotal:", $"{note.gross_total_usd:N2}", false, Brushes.Black));
            if (note.discount_amount > 0)
                totalsCol.Children.Add(MakeTotalsRow($"Descuento ({note.discount_percentage:0}%):", $"-{note.discount_amount:N2}", false, RedBrush, new Thickness(0, 2, 0, 0)));
            totalsCol.Children.Add(MakeLine(1, BorderGrayBrush, new Thickness(0, 3, 0, 0)));
            totalsCol.Children.Add(MakeTotalsRow("TOTAL GENERAL:", $"{note.total_amount_usd:N2}", true, PrimaryBrush, new Thickness(0, 3, 0, 0)));
            if (note.paid_amount_usd > 0)
                totalsCol.Children.Add(MakeTotalsRow("Abonado:", $"{note.paid_amount_usd:N2}", false, GreenBrush, new Thickness(0, 2, 0, 0)));
            totalsCol.Children.Add(MakeTotalsRow("Saldo:", $"{note.balance_due_usd:N2}", true, Brushes.Black, new Thickness(0, 2, 0, 0)));
            totalsBox.Child = totalsCol;
            DockPanel.SetDock(totalsBox, Dock.Right);
            totalsWrap.Children.Add(totalsBox);
            totalsWrap.Children.Add(new TextBlock());
            p.Children.Add(totalsWrap);

            // CONDICIONES DE PAGO + DESCUENTO
            var condRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            condRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (!string.IsNullOrWhiteSpace(note.discount_conditions_text))
                condRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            condRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            condRow.Children.Add(MakeSection("CONDICIONES DE PAGO", note.conditions_text));
            if (!string.IsNullOrWhiteSpace(note.discount_conditions_text))
            {
                var discBox = MakeBox(new Thickness(6));
                var discCol = new StackPanel();
                discCol.Children.Add(MakeText("DESCUENTO", 9, true, PrimaryBrush));
                discCol.Children.Add(MakeText(note.discount_conditions_text, 8, false, new SolidColorBrush(Color.FromRgb(0xCC, 0x66, 0x00)), new Thickness(0, 3, 0, 0)));
                discBox.Child = discCol;
                Grid.SetColumn(discBox, 2);
                condRow.Children.Add(discBox);
            }
            p.Children.Add(condRow);

            // DATOS BANCARIOS + DATOS PARA PAGO
            var bankRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            bankRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bankRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            bankRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            bankRow.Children.Add(MakeSection("DATOS BANCARIOS",
                "Transferencia:\nBanco: BANCO DE VENEZUELA\nCuenta Corriente: 0134-0134-13-0134123456\nRIF: J-12345678-9\n\nPago Móvil:\nBanco: BANCO DE VENEZUELA\nTelefono: 0414-1234567\nCedula: V-12.345.678"));

            var manualBox = MakeBox(new Thickness(0));
            var manualCol = new StackPanel();
            manualCol.Children.Add(MakeText("DATOS PARA PAGO", 8, true, PrimaryBrush));
            manualCol.Children.Add(MakeManualRow("Fecha de pago:"));
            manualCol.Children.Add(MakeManualRow("Monto Bs:"));
            manualCol.Children.Add(MakeManualRow("Nro Referencia:"));
            manualCol.Children.Add(MakeManualRow("Banco:"));
            manualBox.Child = manualCol;
            Grid.SetColumn(manualBox, 2);
            bankRow.Children.Add(manualBox);
            p.Children.Add(bankRow);

            // FIRMA
            var signatureBox = MakeBox(padding: new Thickness(10));
            signatureBox.Margin = new Thickness(0, 10, 0, 0);
            var sigCol = new StackPanel();
            sigCol.Children.Add(MakeText("FECHA  /  FIRMA Y SELLO DEL CLIENTE", 9, true, PrimaryBrush, null, HorizontalAlignment.Center));
            sigCol.Children.Add(MakeLine(1, BorderGrayBrush, new Thickness(0, 40, 0, 0)));
            signatureBox.Child = sigCol;
            p.Children.Add(signatureBox);

            // PIE
            p.Children.Add(MakeLine(1, BorderGrayBrush, new Thickness(0, 10, 0, 0)));
            var footerRow = new Grid { Margin = new Thickness(0, 3, 0, 0) };
            footerRow.ColumnDefinitions.Add(new ColumnDefinition());
            footerRow.ColumnDefinitions.Add(new ColumnDefinition());
            footerRow.ColumnDefinitions.Add(new ColumnDefinition());
            footerRow.Children.Add(MakeText($"Nota: {note.note_number}", 7, false, FooterGrayBrush));
            footerRow.Children.Add(MakeText($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}", 7, false, FooterGrayBrush, null, HorizontalAlignment.Center));
            Grid.SetColumn(footerRow.Children[footerRow.Children.Count - 1], 1);
            footerRow.Children.Add(MakeText("Pagina 1", 7, false, FooterGrayBrush, null, HorizontalAlignment.Right));
            Grid.SetColumn(footerRow.Children[footerRow.Children.Count - 1], 2);
            p.Children.Add(footerRow);
        }

        private Border MakeSection(string title, string body)
        {
            var box = MakeBox(new Thickness(6));
            var col = new StackPanel();
            col.Children.Add(MakeText(title, 9, true, PrimaryBrush));
            if (!string.IsNullOrWhiteSpace(body))
                col.Children.Add(MakeText(body, 9, false, Brushes.Black, new Thickness(0, 3, 0, 0)));
            box.Child = col;
            return box;
        }

        private DockPanel MakeManualRow(string label)
        {
            var dp = new DockPanel { Margin = new Thickness(0, 4, 0, 0), LastChildFill = true };
            var lbl = MakeText(label, 8, false, Brushes.Black);
            DockPanel.SetDock(lbl, Dock.Left);
            var line = MakeText("____________________", 8, false, Brushes.Black, null, HorizontalAlignment.Right);
            dp.Children.Add(line);
            dp.Children.Add(lbl);
            return dp;
        }

        private DockPanel MakeTotalsRow(string left, string right, bool bold, Brush? foreground, Thickness? margin = null)
        {
            var dp = new DockPanel { LastChildFill = true };
            if (margin.HasValue) dp.Margin = margin.Value;
            var rightTb = MakeText(right, bold ? 11 : 9, bold, foreground ?? Brushes.Black, null, HorizontalAlignment.Right);
            DockPanel.SetDock(rightTb, Dock.Right);
            var leftTb = MakeText(left, bold ? 11 : 9, bold, foreground ?? Brushes.Black);
            dp.Children.Add(rightTb);
            dp.Children.Add(leftTb);
            return dp;
        }

        private Border MakeBox(Thickness padding)
        {
            return new Border
            {
                BorderBrush = BorderGrayBrush,
                BorderThickness = new Thickness(1),
                Padding = padding
            };
        }

        private Grid MakeInfoRow(StackPanel cell1, StackPanel cell2)
        {
            var r = new Grid();
            r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r.Children.Add(cell1);
            Grid.SetColumn(cell2, 1);
            r.Children.Add(cell2);
            return r;
        }

        private Grid MakeTripleRow(StackPanel cell1, StackPanel cell2, StackPanel cell3)
        {
            var r = new Grid();
            r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r.Children.Add(cell1);
            Grid.SetColumn(cell2, 1);
            r.Children.Add(cell2);
            Grid.SetColumn(cell3, 2);
            r.Children.Add(cell3);
            return r;
        }

        private StackPanel MakeInfoCell(string label, string? value, double fixedWidth = 0)
        {
            var col = new StackPanel { Margin = new Thickness(0, 3, 0, 0) };
            col.Children.Add(MakeText(label, 7, true, LabelGrayBrush));
            col.Children.Add(MakeText(value ?? "", 10, false, Brushes.Black, new Thickness(0, 1, 0, 0)));
            return col;
        }

        private Border MakeLine(double height, Brush brush, Thickness margin)
        {
            return new Border
            {
                Height = height,
                Background = brush,
                Margin = margin
            };
        }

        private TextBlock MakeText(string text, double size, bool bold, Brush foreground, Thickness? margin = null, HorizontalAlignment alignment = HorizontalAlignment.Left)
        {
            var tb = new TextBlock
            {
                Text = text ?? "",
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                Foreground = foreground,
                HorizontalAlignment = alignment
            };
            if (margin.HasValue) tb.Margin = margin.Value;
            return tb;
        }

        private DataGrid MakeDetailTable(note_print_dto note)
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
                FontSize = 9,
                RowHeight = 26,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                AlternatingRowBackground = LightGrayBrush,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Margin = new Thickness(0, 3, 0, 0),
                ItemsSource = new ObservableCollection<object>(note.details)
            };

            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, PrimaryBrush));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty, Brushes.White));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 8.0));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            dg.ColumnHeaderStyle = headerStyle;

            dg.Columns.Add(MakeTextColumn("CANT.", "quantity", 60));
            dg.Columns.Add(MakeTextColumn("DESCRIPCION", "name", 250));
            dg.Columns.Add(MakeTextColumn("PRECIO U.", "unit_price_usd", 85, "N2"));
            dg.Columns.Add(MakeTextColumn("PRECIO P.", "promo_price_usd", 85, "N2"));
            dg.Columns.Add(MakeTextColumn("SUBTOTAL", "subtotal_usd", 90, "N2"));
            dg.Columns.Add(MakeTextColumn("CODIGO", "code", 80));

            return dg;
        }

        private DataGridTextColumn MakeTextColumn(string header, string binding, double width, string? format = null)
        {
            var col = new DataGridTextColumn { Header = header, Width = width };
            var b = new Binding(binding);
            if (format != null) b.StringFormat = format;
            col.Binding = b;
            return col;
        }
    }
}