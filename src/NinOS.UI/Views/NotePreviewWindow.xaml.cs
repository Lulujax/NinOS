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
        private Brush PrimaryBrush = new SolidColorBrush(Color.FromRgb(0x1B, 0x3A, 0x2D));
        private static readonly Brush LabelGrayBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
        private Brush LightGrayBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xEC));
        private static readonly Brush BorderGrayBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
        private static readonly Brush DividerBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
        private static readonly Brush RedBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x00));
        private static readonly Brush GreenBrush = new SolidColorBrush(Color.FromRgb(0x22, 0x8B, 0x22));
        private static readonly Brush FooterGrayBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

        public bool Confirmed { get; private set; }
        public bool PdfRequested { get; private set; }

        public NotePreviewWindow(note_print_dto note)
        {
            InitializeComponent();
            PrimaryBrush = ParseBrush(note.accent_color, new SolidColorBrush(Color.FromRgb(0x1B, 0x3A, 0x2D)));
            LightGrayBrush = ParseBrush(note.accent_soft_color, new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xEC)));
            Title = $"Vista Previa - Nota {note.note_number}";
            BuildPreview(note);
        }

        private static Brush ParseBrush(string? hex, Brush fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return fallback;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            PdfRequested = false;
            DialogResult = false;
        }

        private void OnSaveOnly(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            PdfRequested = false;
            DialogResult = true;
        }

        private void OnSavePdf(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            PdfRequested = true;
            DialogResult = true;
        }

        private void BuildPreview(note_print_dto note)
        {
            var p = NotePanel;

            // CABECERA: empresa izquierda, NOTA DE ENTREGA derecha
            var header = new DockPanel { LastChildFill = true };
            var left = new StackPanel();
            left.Children.Add(MakeText(string.IsNullOrWhiteSpace(note.header_title) ? note.company_name : note.header_title, 22, true, PrimaryBrush));
            left.Children.Add(MakeText("Caracas - Venezuela", 10, false, LabelGrayBrush, new Thickness(0, 2, 0, 0)));
            DockPanel.SetDock(left, Dock.Left);
            var right = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            right.Children.Add(MakeText(note.document_label, 16, true, PrimaryBrush));
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

            var infoRow1 = MakeInfoRowWithCode(
                MakeInfoCell("Razon Social", note.customer_business_name),
                MakeInfoCell("Codigo", note.customer_code),
                MakeInfoCell("RIF", note.customer_rif));
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

            if (!string.IsNullOrWhiteSpace(note.customer_contact) || !string.IsNullOrWhiteSpace(note.credit_days_text))
            {
                var divider3 = MakeLine(1, DividerBrush, new Thickness(0, 4, 0, 0));
                Grid.SetRow(divider3, 5);
                infoGrid.Children.Add(divider3);

                var infoRow4 = MakeInfoRow(
                    MakeInfoCell("Contacto", note.customer_contact),
                    MakeInfoCell("Dias de Credito", note.credit_days_text));
                Grid.SetRow(infoRow4, 6);
                infoGrid.Children.Add(infoRow4);
            }

            infoBox.Child = infoGrid;
            p.Children.Add(infoBox);

            // DETALLE DE PRODUCTOS
            p.Children.Add(MakeText("DETALLE DE PRODUCTOS", 9, true, PrimaryBrush, new Thickness(0, 8, 0, 0)));
            p.Children.Add(MakeDetailTable(note));

            // ---- Fila 1: condicion de pago + primer Total General ----
            var totalRow1 = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            totalRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            totalRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });

            var condBox = MakeBox(new Thickness(6));
            var condCol = new StackPanel();
            condCol.Children.Add(MakeText(note.conditions_text, 9, false, Brushes.Black));
            condBox.Child = condCol;
            totalRow1.Children.Add(condBox);

            var tgBox = MakeBox(new Thickness(6));
            var tgCol = new StackPanel();
            tgCol.Children.Add(MakeText("Total General", 9, true, PrimaryBrush, null, HorizontalAlignment.Center));
            tgCol.Children.Add(MakeText($"{note.gross_total_usd:N2}", 14, true, PrimaryBrush, null, HorizontalAlignment.Center));
            tgBox.Child = tgCol;
            Grid.SetColumn(tgBox, 2);
            totalRow1.Children.Add(tgBox);
            p.Children.Add(totalRow1);

            // ---- Fila 2: DATOS PARA PAGO (3 columnas: desglose | firma | datos de pago) ----
            var payBox = MakeBox(new Thickness(0));
            payBox.Margin = new Thickness(0, 8, 0, 0);
            var payStack = new StackPanel();
            var bankHeaderBox = new Border { Background = LightGrayBrush, Padding = new Thickness(5), BorderBrush = BorderGrayBrush, BorderThickness = new Thickness(0, 0, 0, 1) };
            var bankHeaderCol = new StackPanel();
            bankHeaderCol.Children.Add(MakeText("DATOS PARA PAGOS NOTAS DE ENTREGA DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE", 10, true, Brushes.Black, null, HorizontalAlignment.Center));
            bankHeaderCol.Children.Add(MakeText("TRANSFERENCIA _ BANCO MERCANTIL  CUENTA CORRIENTE", 8, false, Brushes.Black, null, HorizontalAlignment.Center));
            bankHeaderCol.Children.Add(MakeText("NRO DE CUENTA _ 0105-0120-23-11200-92426  /  CEDULA - 13.046.042", 8, false, Brushes.Black, null, HorizontalAlignment.Center));
            bankHeaderCol.Children.Add(MakeText("PAGO MOVIL", 8, false, Brushes.Black, null, HorizontalAlignment.Center));
            bankHeaderCol.Children.Add(MakeText("BANCO MERCANTIL / NRO TELEFONO _ 0424.496.01.02  /  CEDULA - 13.046.042", 8, false, Brushes.Black, null, HorizontalAlignment.Center));
            bankHeaderBox.Child = bankHeaderCol;
            payStack.Children.Add(bankHeaderBox);

            var payRow = new Grid();
            payRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
            payRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            payRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            payRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            payRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });

            // columna izquierda: desglose
            var leftBox = MakeBox(new Thickness(6));
            var leftCol = new StackPanel();
            leftCol.Children.Add(MakeText(note.discount_conditions_text, 8, true, Brushes.Black, null, HorizontalAlignment.Center));
            leftCol.Children.Add(MakeTotalsRow("sub total", $"{note.gross_total_usd:N2}", true, PrimaryBrush, new Thickness(0, 4, 0, 0)));
            if (note.promo_discount_amount > 0)
                leftCol.Children.Add(MakeTotalsRow($"{note.promo_discount_percentage:0.##}% PROMO", $"-{note.promo_discount_amount:N2}", false, RedBrush, new Thickness(0, 2, 0, 0)));
            decimal disc_pdf = note.discount_amount > 0 ? note.discount_amount : 0;
            leftCol.Children.Add(MakeTotalsRow($"{note.discount_percentage:0.##}%", (note.discount_amount > 0 ? "-" : "") + $"{disc_pdf:N2}", false, note.discount_amount > 0 ? RedBrush : FooterGrayBrush, new Thickness(0, 2, 0, 0)));
            leftCol.Children.Add(MakeTotalsRow("Total General", $"{note.discounted_total_usd:N2}", true, PrimaryBrush, new Thickness(0, 3, 0, 0)));
            leftBox.Child = leftCol;
            payRow.Children.Add(leftBox);

            // columna media: firma
            var midBox = new Border { Padding = new Thickness(6) };
            var midCol = new StackPanel();
            midCol.Children.Add(MakeText("FECHA _ FIRMA Y SELLO DEL CLIENTE", 8, false, FooterGrayBrush, new Thickness(0, 14, 0, 0), HorizontalAlignment.Center));
            midCol.Children.Add(MakeLine(1, BorderGrayBrush, new Thickness(0, 36, 0, 0)));
            midBox.Child = midCol;
            Grid.SetColumn(midBox, 2);
            payRow.Children.Add(midBox);

            // columna derecha: datos para pago
            var manualBox = MakeBox(new Thickness(6));
            var manualCol = new StackPanel();
            manualCol.Children.Add(MakeText("DATOS PARA PAGO", 8, true, PrimaryBrush));
            manualCol.Children.Add(MakeManualRow("Fecha de pago:"));
            manualCol.Children.Add(MakeManualRow("Monto Bs:"));
            manualCol.Children.Add(MakeManualRow("Nro Referencia:"));
            manualCol.Children.Add(MakeManualRow("Banco:"));
            manualBox.Child = manualCol;
            Grid.SetColumn(manualBox, 4);
            payRow.Children.Add(manualBox);

            payStack.Children.Add(payRow);
            payBox.Child = payStack;
            p.Children.Add(payBox);

            // ---- Fila 3: Descuento por volumen + TOTAL A PAGAR ----
            decimal vol_pdf = note.volume_discount_amount > 0 ? note.volume_discount_amount : 0;
            var volBox = MakeBox(new Thickness(0));
            volBox.Width = 460;
            volBox.HorizontalAlignment = HorizontalAlignment.Left;
            volBox.Margin = new Thickness(0, 8, 0, 0);
            var volCol = new StackPanel();
            volCol.Children.Add(MakeTotalsRow($"Descuento por volumen {note.volume_discount_percentage:0.##}%", (note.volume_discount_amount > 0 ? "-" : "") + $"{vol_pdf:N2}", true, note.volume_discount_amount > 0 ? RedBrush : FooterGrayBrush));
            volCol.Children.Add(MakeLine(1, BorderGrayBrush, new Thickness(0, 4, 0, 0)));
            volCol.Children.Add(MakeTotalsRow("TOTAL A PAGAR", $"{note.total_amount_usd:N2}", true, PrimaryBrush, new Thickness(0, 4, 0, 0)));
            if (note.paid_amount_usd > 0)
                volCol.Children.Add(MakeTotalsRow("Abonado:", $"{note.paid_amount_usd:N2}", false, GreenBrush, new Thickness(0, 3, 0, 0)));
            volCol.Children.Add(MakeTotalsRow("Saldo:", $"{note.balance_due_usd:N2}", true, Brushes.Black, new Thickness(0, 3, 0, 0)));
            volBox.Child = volCol;
            p.Children.Add(volBox);

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

        private Grid MakeInfoRowWithCode(StackPanel cell1, StackPanel cell2, StackPanel cell3)
        {
            var r = new Grid();
            r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            r.Children.Add(cell1);
            Grid.SetColumn(cell2, 1);
            r.Children.Add(cell2);
            Grid.SetColumn(cell3, 2);
            r.Children.Add(cell3);
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

            dg.Columns.Add(MakeTextColumn("CANT.", "quantity", 45));
            dg.Columns.Add(MakeTextColumn("CODIGO", "code", 85));
            dg.Columns.Add(MakeTextColumn("DESCRIPCION", "name", 210));
            dg.Columns.Add(MakeTextColumn("PRECIO U.", "unit_price_usd", 80, "N2"));
            dg.Columns.Add(MakeTextColumn("PRECIO P.", "promo_price_usd", 80, "N2"));
            dg.Columns.Add(MakeTextColumn("SUBTOTAL", "subtotal_usd", 85, "N2"));

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