using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class ProVentaRelationsSummaryPdfGenerator
    {
        private static readonly string Accent = "#1565C0";
        private static readonly string SoftAccent = "#E3F2FD";
        private static readonly string LightBorder = "#B0BEC5";
        private static readonly string GreenColor = "#2E7D32";
        private static readonly string RedColor = "#C62828";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        public static string get_default_file_name(string sectionLabel, string monthLabel)
        {
            string cleanSection = sectionLabel.Replace(" ", "_").ToUpperInvariant();
            string cleanMonth = (string.IsNullOrWhiteSpace(monthLabel) || string.Equals(monthLabel.Trim(), "Todas las relaciones", StringComparison.OrdinalIgnoreCase) || string.Equals(monthLabel.Trim(), "Todas", StringComparison.OrdinalIgnoreCase))
                ? "TODAS"
                : monthLabel.Replace(" ", "_").ToUpperInvariant();
            return $"RELACIONES_PRO_VENTA_{cleanSection}_{cleanMonth}_{DateTime.Now:yyyyMMdd}.pdf";
        }

        public static IDocument build_document(
            string reportTitle,
            string monthLabel,
            List<pro_venta_relation_row>? pendingRows,
            List<pro_venta_relation_row>? paidRows)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var pending = pendingRows ?? new List<pro_venta_relation_row>();
            var paid = paidRows ?? new List<pro_venta_relation_row>();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(1, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(1, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("REPORTE DE RELACIONES PRO VENTA - MARACAY")
                                    .FontSize(13).Bold().FontColor(Accent);
                                c.Item().PaddingTop(1).Text(reportTitle.ToUpperInvariant())
                                    .FontSize(10).Bold().FontColor("#333333");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text($"EMISION: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Bold().FontColor("#555555");
                                string displayPeriod = (string.IsNullOrWhiteSpace(monthLabel) || string.Equals(monthLabel.Trim(), "Todas las relaciones", StringComparison.OrdinalIgnoreCase) || string.Equals(monthLabel.Trim(), "Todas", StringComparison.OrdinalIgnoreCase))
                                    ? "TODAS"
                                    : monthLabel.ToUpperInvariant();
                                c.Item().PaddingTop(1).Text(displayPeriod).FontSize(8).Bold().FontColor(Accent);
                            });
                        });

                        col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Accent);
                    });

                    // Content
                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        if (pending.Count == 0 && paid.Count == 0)
                        {
                            col.Item().Text("No hay relaciones para mostrar en el período seleccionado.").FontSize(11).FontColor("#555555");
                            return;
                        }

                        // SECCIÓN 1: PENDIENTES
                        if (pending.Count > 0)
                        {
                            BuildRelationsTable(col, "RELACIONES POR COBRAR - PENDIENTES", pending, isPending: true);
                        }

                        // SECCIÓN 2: PAGADAS
                        if (paid.Count > 0)
                        {
                            if (pending.Count > 0)
                            {
                                col.Item().PaddingTop(14);
                            }
                            BuildRelationsTable(col, "RELACIONES POR COBRAR - PAGADAS", paid, isPending: false);
                        }

                        // RESUMEN / GRAN TOTAL
                        if (pending.Count > 0 || paid.Count > 0)
                        {
                            col.Item().PaddingTop(14).Row(outerRow =>
                            {
                                outerRow.RelativeItem();

                                outerRow.ConstantItem(360).Border(0.8f).BorderColor(Accent).Background(SoftAccent).Padding(6).Column(box =>
                                {
                                    string boxTitle = (pending.Count > 0 && paid.Count > 0)
                                        ? "CONSOLIDADO GENERAL DE RELACIONES"
                                        : (pending.Count > 0 ? "TOTAL RELACIONES PENDIENTES" : "TOTAL RELACIONES PAGADAS");
                                    box.Item().Text(boxTitle).FontSize(9.5f).Bold().FontColor(Accent).AlignCenter();
                                    box.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Accent);

                                    decimal totalMontoPagar = pending.Sum(r => r.amount) + paid.Sum(r => r.amount);
                                    decimal totalAbonado = pending.Sum(r => r.paid_amount_usd) + paid.Sum(r => r.paid_amount_usd);
                                    decimal totalSaldo = pending.Sum(r => r.balance_due_usd) + paid.Sum(r => r.balance_due_usd);

                                    box.Item().PaddingTop(4).Row(r =>
                                    {
                                        r.RelativeItem().Text("TOTAL MONTO POR PAGAR:").Bold().FontSize(8.5f);
                                        r.ConstantItem(120).AlignRight().Text($"${totalMontoPagar:N2}").Bold().FontSize(9f);
                                    });

                                    box.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.RelativeItem().Text("TOTAL COBRADO / ABONADO:").Bold().FontSize(8.5f).FontColor(GreenColor);
                                        r.ConstantItem(120).AlignRight().Text($"${totalAbonado:N2}").Bold().FontSize(9f).FontColor(GreenColor);
                                    });

                                    box.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.RelativeItem().Text("SALDO PENDIENTE TOTAL:").Bold().FontSize(8.5f).FontColor(RedColor);
                                        r.ConstantItem(120).AlignRight().Text($"${totalSaldo:N2}").Bold().FontSize(10f).FontColor(RedColor);
                                    });
                                });
                            });
                        }
                    });

                    // Footer
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        col.Item().PaddingTop(2).Row(row =>
                        {
                            row.RelativeItem().Text("NinOS - Módulo Pro Venta").FontSize(7).FontColor("#555555");
                            row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#555555");
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Página ").FontSize(7).FontColor("#555555");
                                t.CurrentPageNumber().FontSize(7).FontColor("#555555");
                                t.Span(" de ").FontSize(7).FontColor("#555555");
                                t.TotalPages().FontSize(7).FontColor("#555555");
                            });
                        });
                    });
                });
            });
        }

        private static void BuildRelationsTable(ColumnDescriptor col, string title, List<pro_venta_relation_row> rows, bool isPending)
        {
            col.Item().PaddingBottom(4).Row(r =>
            {
                r.RelativeItem().Text(title).FontSize(10).Bold().FontColor(Accent);
                r.RelativeItem().AlignRight().Text($"{rows.Count} relación(es)").FontSize(8).Bold().FontColor("#555555");
            });

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.6f); // RELACION NRO
                    columns.RelativeColumn(1.4f); // MONTO POR PAGAR
                    columns.RelativeColumn(1.4f); // ABONADO
                    columns.RelativeColumn(1.4f); // SALDO COMPLETO
                    columns.RelativeColumn(1.2f); // ESTADO
                });

                table.Header(header =>
                {
                    void h(string text, bool right = false)
                    {
                        var cell = header.Cell().Background(Accent).PaddingVertical(3).PaddingHorizontal(3);
                        var t = right ? cell.AlignRight().Text(text) : cell.AlignCenter().Text(text);
                        t.FontColor(Colors.White).Bold().FontSize(7.5f);
                    }

                    h("RELACION NRO");
                    h("MONTO POR PAGAR ($)", right: true);
                    h("ABONADO ($)", right: true);
                    h("SALDO PENDIENTE ($)", right: true);
                    h("ESTADO");
                });

                bool alternate = false;
                foreach (var r in rows)
                {
                    string bg = alternate ? SoftAccent : Colors.White;
                    alternate = !alternate;

                    void cell(string text, bool right = false, string? customColor = null, bool bold = false)
                    {
                        var c = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(LightBorder)
                            .PaddingVertical(2.5f).PaddingHorizontal(3);
                        var t = right ? c.AlignRight().Text(text) : c.AlignCenter().Text(text);
                        t.FontSize(7.5f);
                        if (bold) t.Bold();
                        if (customColor != null) t.FontColor(customColor);
                    }

                    cell(r.relation_label, bold: true);
                    cell($"${r.amount:N2}", right: true);
                    cell($"${r.paid_amount_usd:N2}", right: true, customColor: GreenColor, bold: true);
                    cell($"${r.balance_due_usd:N2}", right: true, customColor: r.balance_due_usd > 0 ? RedColor : GreenColor, bold: true);
                    cell(r.status, bold: true, customColor: isPending ? "#E65100" : GreenColor);
                }

                // Fila de total de la sección
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).AlignCenter().Text("TOTAL").Bold();
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).AlignRight().Text($"${rows.Sum(x => x.amount):N2}").Bold();
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).AlignRight().Text($"${rows.Sum(x => x.paid_amount_usd):N2}").Bold().FontColor(GreenColor);
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).AlignRight().Text($"${rows.Sum(x => x.balance_due_usd):N2}").Bold().FontColor(isPending ? RedColor : GreenColor);
                table.Cell().Background(SoftAccent).BorderTop(0.8f).BorderColor(Accent).Padding(3).Text("");
            });
        }

        public static void generate(
            string reportTitle,
            string monthLabel,
            List<pro_venta_relation_row>? pendingRows,
            List<pro_venta_relation_row>? paidRows)
        {
            string defaultName = get_default_file_name(reportTitle, monthLabel);

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar reporte de relaciones Pro Venta en PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = defaultName
            };

            if (save_dialog.ShowDialog() != true) return;

            build_document(reportTitle, monthLabel, pendingRows, paidRows).GeneratePdf(save_dialog.FileName);
        }
    }
}
