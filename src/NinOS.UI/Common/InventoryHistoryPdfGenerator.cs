using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    // Una fila del historial de movimientos ya aplanada para el PDF. El generador no
    // depende de los DTO de dominio a proposito: producto y promocion comparten las
    // mismas columnas, asi que el mismo documento sirve para las dos ventanas.
    public class InventoryHistoryPdfRow
    {
        public string NoteNumber { get; set; } = string.Empty;

        // Fecha del movimiento en hora de Venezuela (la base la guarda en UTC).
        public DateTime FechaLocal { get; set; }

        public string Documento { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;

        // ENTRADA o SALIDA: decide el color de la fila y el signo de las unidades.
        public string Movimiento { get; set; } = string.Empty;

        public int Unidades { get; set; }
        public decimal PrecioUsd { get; set; }
        public decimal SubtotalUsd { get; set; }
        public string Estado { get; set; } = string.Empty;

        // Existencia que quedo despues de este movimiento. Lo calcula el generador
        // recorriendo el historial desde el stock actual hacia atras, para que el
        // cliente pueda seguir la cuenta (100 -> 150 -> 100) sin sumar a mano.
        public int SaldoUnidades { get; set; }

        public bool EsEntrada => string.Equals(Movimiento, "ENTRADA", StringComparison.OrdinalIgnoreCase);
        public int SignedUnits => EsEntrada ? Unidades : -Unidades;
    }

    public class InventoryHistoryPdfModel
    {
        // "PRODUCTO" o "PROMOCION": rotula la ficha de la cabecera.
        public string ItemLabel { get; set; } = "PRODUCTO";
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal UnitPriceUsd { get; set; }

        // Existencias que el articulo tiene hoy. Es el punto de partida para reconstruir
        // los saltos hacia atras.
        public int CurrentStock { get; set; }

        // Las promociones no tienen existencias propias (su disponibilidad se calcula con
        // los productos que las componen), asi que en su PDF se omite el saldo en vez de
        // imprimir un cero que seria mentira.
        public bool MostrarSaldos { get; set; } = true;

        // Movimientos en el mismo orden en que se ven en la pantalla: del mas reciente
        // al mas antiguo.
        public List<InventoryHistoryPdfRow> Rows { get; set; } = new();
    }

    // Respaldo imprimible del historial de movimientos del inventario (doble clic sobre un
    // producto o una promocion). Sigue el diseno de CustomerHistoryPdfGenerator para que
    // todos los PDF del sistema se vean iguales.
    public static class InventoryHistoryPdfGenerator
    {
        private static readonly string PrimaryColor = "#0D62B5";
        private static readonly string DarkText = "#1A1A1A";
        private static readonly string MutedText = "#555555";
        private static readonly string LightBorder = "#D0D7DE";
        private static readonly string TableHeaderBg = "#0D62B5";
        private static readonly string ZebraBg = "#F8FAFC";
        private static readonly string CardBg = "#F1F5F9";
        private static readonly string GreenColor = "#2E7D32";
        private static readonly string RedColor = "#C62828";
        private static readonly string OrangeColor = "#E65100";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static void generate(InventoryHistoryPdfModel model)
        {
            if (model == null) return;

            string safe_code = SanitizeFileName(string.IsNullOrWhiteSpace(model.ItemCode) ? "INVENTARIO" : model.ItemCode);

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar historial de movimientos en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"HISTORIAL_{safe_code}_{DateTime.Now:yyyyMMdd}.pdf".ToUpperInvariant()
            };

            if (save_dialog.ShowDialog() != true) return;

            generate_to_file(model, save_dialog.FileName);
        }

        public static void generate_to_file(InventoryHistoryPdfModel model, string output_path)
        {
            if (model == null || string.IsNullOrWhiteSpace(output_path)) return;

            build_document(model).GeneratePdf(output_path);
        }

        /// <summary>
        /// Arma el documento del historial. Se separa de generate_to_file para que el
        /// contenido se pueda guardar en archivo o previsualizar sin duplicar el armado.
        /// </summary>
        public static IDocument build_document(InventoryHistoryPdfModel model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            if (model.MostrarSaldos) calcular_saldos(model);

            var rows = model.Rows ?? new List<InventoryHistoryPdfRow>();
            int total_entradas = rows.Where(r => r.EsEntrada).Sum(r => r.Unidades);
            int total_salidas = rows.Where(r => !r.EsEntrada).Sum(r => r.Unidades);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(1, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(1, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(7.5f).FontColor(DarkText));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            // El titulo necesita mas ancho que la columna de emision/periodo:
                            // si no, "HISTORIAL DE MOVIMIENTOS DE INVENTARIO" se parte en dos.
                            row.RelativeItem(1.35f).Column(c =>
                            {
                                c.Item().Text("HISTORIAL DE MOVIMIENTOS DE INVENTARIO").FontSize(12).Bold().FontColor(PrimaryColor);
                                c.Item().PaddingTop(1).Text(model.ItemName).FontSize(11).Bold().FontColor(DarkText);
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text($"EMISION: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Bold().FontColor(MutedText);
                                c.Item().PaddingTop(1).Text($"PERIODO: {PeriodoLabel(rows).ToUpperInvariant()}").FontSize(8).Bold().FontColor(PrimaryColor);
                            });
                        });

                        col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("NinOS - Sistema de Gestion").FontSize(7).FontColor(MutedText);
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Pagina ").FontSize(7).FontColor(MutedText);
                            t.CurrentPageNumber().FontSize(7).FontColor(MutedText);
                            t.Span(" de ").FontSize(7).FontColor(MutedText);
                            t.TotalPages().FontSize(7).FontColor(MutedText);
                        });
                    });

                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        BuildInfoBox(col, model);
                        BuildKpiSummary(col, model, rows.Count, total_entradas, total_salidas);
                        BuildMovementsTable(col, model, rows, total_entradas, total_salidas);
                    });
                });
            });
        }

        /// <summary>
        /// Reconstruye la existencia que quedo despues de cada movimiento. La lista viene
        /// del mas reciente al mas antiguo y el producto ya tiene el stock de hoy, asi que
        /// se camina hacia atras restando el movimiento recien leido.
        /// </summary>
        private static void calcular_saldos(InventoryHistoryPdfModel model)
        {
            if (model.Rows == null) return;

            int saldo = model.CurrentStock;

            foreach (InventoryHistoryPdfRow row in model.Rows)
            {
                row.SaldoUnidades = saldo;
                saldo -= row.SignedUnits;
            }
        }

        private static string PeriodoLabel(List<InventoryHistoryPdfRow> rows)
        {
            var fechas = rows
                .Select(r => r.FechaLocal.Date)
                .Where(d => d != default)
                .ToList();

            if (fechas.Count == 0) return "Sin movimientos";

            DateTime desde = fechas.Min();
            DateTime hasta = fechas.Max();

            return desde == hasta
                ? $"Al {hasta:dd/MM/yyyy}"
                : $"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}";
        }

        private static void BuildInfoBox(ColumnDescriptor col, InventoryHistoryPdfModel model)
        {
            col.Item().PaddingBottom(6).Border(1).BorderColor(LightBorder).Background(CardBg).Padding(6).Column(box =>
            {
                box.Item().Row(row =>
                {
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span($"{model.ItemLabel}: ").Bold();
                            t.Span(model.ItemName).Bold().FontColor(PrimaryColor);
                        });
                        c.Item().PaddingTop(2).Text(t =>
                        {
                            t.Span("CODIGO: ").Bold();
                            t.Span(string.IsNullOrWhiteSpace(model.ItemCode) ? "-" : model.ItemCode);
                            t.Span("   |   MARCA / CATEGORIA: ").Bold();
                            t.Span(string.IsNullOrWhiteSpace(model.Category) ? "-" : model.Category);
                        });
                    });

                    row.RelativeItem(1).AlignRight().Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("PRECIO ($): ").Bold();
                            t.Span(model.UnitPriceUsd.ToString("N2", Ve)).Bold().FontColor(GreenColor);
                        });

                        if (model.MostrarSaldos)
                        {
                            c.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("EXISTENCIAS HOY: ").Bold();
                                t.Span(model.CurrentStock.ToString()).Bold().FontColor(OrangeColor);
                            });
                        }
                    });
                });
            });
        }

        private static void BuildKpiSummary(
            ColumnDescriptor col,
            InventoryHistoryPdfModel model,
            int movimientos,
            int total_entradas,
            int total_salidas)
        {
            col.Item().PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("MOVIMIENTOS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(movimientos.ToString()).FontSize(11).Bold().FontColor(PrimaryColor);
                    c.Item().AlignCenter().Text("registros").FontSize(6.5f).FontColor(MutedText);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("ENTRADAS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(signed_label(total_entradas, true)).FontSize(11).Bold().FontColor(GreenColor);
                    c.Item().AlignCenter().Text("unidades").FontSize(6.5f).FontColor(GreenColor);
                });

                row.Spacing(4);

                row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                {
                    c.Item().AlignCenter().Text("SALIDAS").FontSize(7).Bold().FontColor(MutedText);
                    c.Item().AlignCenter().Text(signed_label(total_salidas, false)).FontSize(11).Bold().FontColor(RedColor);
                    c.Item().AlignCenter().Text("unidades").FontSize(6.5f).FontColor(RedColor);
                });

                row.Spacing(4);

                if (model.MostrarSaldos)
                {
                    row.RelativeItem().Border(1).BorderColor(LightBorder).Background("#FFFFFF").Padding(4).Column(c =>
                    {
                        c.Item().AlignCenter().Text("EXISTENCIAS HOY").FontSize(7).Bold().FontColor(MutedText);
                        c.Item().AlignCenter().Text(model.CurrentStock.ToString()).FontSize(11).Bold().FontColor(OrangeColor);
                        c.Item().AlignCenter().Text("unidades").FontSize(6.5f).FontColor(MutedText);
                    });
                }
            });
        }

        private static void BuildMovementsTable(
            ColumnDescriptor col,
            InventoryHistoryPdfModel model,
            List<InventoryHistoryPdfRow> rows,
            int total_entradas,
            int total_salidas)
        {
            bool con_saldos = model.MostrarSaldos;
            int columnas = con_saldos ? 12 : 11;

            col.Item().PaddingBottom(2).Row(row =>
            {
                row.RelativeItem().Text("DETALLE DE MOVIMIENTOS").FontSize(9.5f).Bold().FontColor(PrimaryColor);
                row.RelativeItem().AlignRight().Text($"{rows.Count} movimiento(s)").FontSize(7.5f).FontColor(MutedText);
            });

            if (rows.Count == 0)
            {
                col.Item().PaddingBottom(8).Border(1).BorderColor(LightBorder).Padding(6)
                    .Text("Este articulo no tiene movimientos registrados.").Italic().FontColor(MutedText);
                return;
            }

            col.Item().PaddingBottom(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.95f); // Fecha (dd/MM/yyyy completo, sin partirse)
                    columns.RelativeColumn(1.15f); // Documento
                    columns.RelativeColumn(1.15f); // N documento
                    columns.RelativeColumn(1.20f); // Motivo
                    columns.RelativeColumn(2.10f); // Cliente
                    columns.RelativeColumn(1.25f); // Vendedor
                    columns.RelativeColumn(0.90f); // Movimiento
                    columns.RelativeColumn(0.75f); // Unidades

                    if (con_saldos)
                    {
                        columns.RelativeColumn(0.75f); // Saldo
                    }

                    columns.RelativeColumn(0.85f); // Precio
                    columns.RelativeColumn(0.95f); // Subtotal
                    columns.RelativeColumn(0.95f); // Estado
                });

                table.Header(header =>
                {
                    void h(string text, bool right = false, bool center = false)
                    {
                        var cell = header.Cell().Background(TableHeaderBg).PaddingVertical(2).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : (center ? cell.AlignCenter().Text(text) : cell.Text(text));
                        t.FontColor(Colors.White).Bold().FontSize(6.5f);
                    }

                    h("FECHA");
                    h("DOCUMENTO");
                    h("N DOC");
                    h("MOTIVO");
                    h("CLIENTE");
                    h("VENDEDOR");
                    h("MOV.", center: true);
                    h("UNID.", right: true);

                    if (con_saldos)
                    {
                        h("SALDO", right: true);
                    }

                    h("PRECIO ($)", right: true);
                    h("SUBTOTAL ($)", right: true);
                    h("ESTADO");
                });

                int idx = 0;
                foreach (InventoryHistoryPdfRow r in rows)
                {
                    string bg = (idx++ % 2 == 1) ? ZebraBg : "#FFFFFF";
                    string direction_color = r.EsEntrada ? GreenColor : RedColor;

                    void cellText(string text, bool right = false, bool center = false, string? customColor = null, bool bold = false)
                    {
                        var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LightBorder)
                            .PaddingVertical(1.8f).PaddingHorizontal(2);
                        var t = right ? cell.AlignRight().Text(text) : (center ? cell.AlignCenter().Text(text) : cell.Text(text));
                        t.FontSize(6.8f);
                        if (bold) t.Bold();
                        t.FontColor(customColor ?? DarkText);
                    }

                    cellText(r.FechaLocal.ToString("dd/MM/yyyy"));
                    cellText(string.IsNullOrWhiteSpace(r.Documento) ? "-" : r.Documento);
                    cellText(string.IsNullOrWhiteSpace(r.NoteNumber) ? "-" : r.NoteNumber);
                    cellText(string.IsNullOrWhiteSpace(r.Motivo) ? "-" : r.Motivo);
                    cellText(string.IsNullOrWhiteSpace(r.Cliente) ? "-" : r.Cliente);
                    cellText(string.IsNullOrWhiteSpace(r.Vendedor) ? "-" : r.Vendedor);
                    cellText(r.EsEntrada ? "ENTRADA" : "SALIDA", center: true, customColor: direction_color, bold: true);
                    cellText(r.SignedUnits > 0 ? "+" + r.SignedUnits : r.SignedUnits.ToString(), right: true, customColor: direction_color, bold: true);

                    if (con_saldos)
                    {
                        cellText(r.SaldoUnidades.ToString(), right: true, bold: true);
                    }

                    cellText(r.PrecioUsd.ToString("N2", Ve), right: true);
                    cellText(r.SubtotalUsd.ToString("N2", Ve), right: true);
                    cellText(string.IsNullOrWhiteSpace(r.Estado) ? "-" : r.Estado);
                }

                // Totales del periodo, pegados a la tabla para que no se separen en el salto
                // de pagina. Los tramos suman siempre el ancho completo de la tabla.
                uint tramo_1 = 6;
                uint tramo_2 = 2;
                uint tramo_3 = (uint)columnas - tramo_1 - tramo_2;

                void totalRow(string label, string value, string color)
                {
                    table.Cell().ColumnSpan(tramo_1).Background(CardBg).PaddingVertical(2).PaddingHorizontal(3)
                        .AlignRight().Text(label).Bold().FontSize(7).FontColor(DarkText);
                    table.Cell().ColumnSpan(tramo_2).Background(CardBg).PaddingVertical(2).PaddingHorizontal(2)
                        .AlignRight().Text(value).Bold().FontSize(7).FontColor(color);
                    table.Cell().ColumnSpan(tramo_3).Background(CardBg).PaddingVertical(2).PaddingHorizontal(2).Text("");
                }

                totalRow("TOTAL ENTRADAS:", signed_label(total_entradas, true), GreenColor);
                totalRow("TOTAL SALIDAS:", signed_label(total_salidas, false), RedColor);

                if (con_saldos)
                {
                    totalRow("EXISTENCIAS HOY:", model.CurrentStock.ToString(), OrangeColor);
                }
            });

            col.Item().PaddingTop(2).Text(con_saldos
                    ? "El SALDO es la existencia que quedo despues de cada movimiento. Los movimientos se listan del mas reciente al mas antiguo."
                    : "Los movimientos se listan del mas reciente al mas antiguo.")
                .FontSize(6.5f).Italic().FontColor(MutedText);
        }

        // "+50" para las entradas y "-50" para las salidas. El cero va sin signo: un "-0"
        // en un respaldo se lee como un error de calculo.
        private static string signed_label(int value, bool entrada)
        {
            if (value == 0) return "0";
            return entrada ? "+" + value : "-" + value;
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return cleaned.Replace(' ', '_');
        }
    }
}
