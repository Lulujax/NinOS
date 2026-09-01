using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class MonthlyReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#B0B0B0";
        private static readonly string AccentBg = "#F0F4EC";

        public static void generate(monthly_report_dto report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string file_name = string.IsNullOrWhiteSpace(report.report_name)
                ? $"reporte {report.month}.pdf"
                : $"reporte {report.report_name} {report.month}.pdf";
            file_name = file_name.ToUpperInvariant();

            var save_dialog = new SaveFileDialog
            {
                Title = file_name,
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = file_name
            };

            if (save_dialog.ShowDialog() != true) return;

            var month_cap = Capitalize(report.month);
            var rows = report.rows ?? new List<monthly_report_row_dto>();

            var grouped = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "SIN VENDEDOR" : r.seller_name.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            var detail_header = string.IsNullOrWhiteSpace(report.detail_column_header) ? "DETALLE" : report.detail_column_header;
            var status_header = string.IsNullOrWhiteSpace(report.status_column_header) ? "ESTADO" : report.status_column_header;

            var document = Document.Create(container =>
            {
                if (grouped.Count == 0)
                {
                    container.Page(page => BuildSellerPage(page, report, month_cap, null, null, detail_header, status_header));
                    return;
                }

                foreach (var g in grouped)
                {
                    container.Page(page => BuildSellerPage(page, report, month_cap, g.Key, g.ToList(), detail_header, status_header));
                }
            });

            document.GeneratePdf(save_dialog.FileName);
        }

        private static void BuildSellerPage(
            PageDescriptor page,
            monthly_report_dto report,
            string month_cap,
            string? seller_name,
            List<monthly_report_row_dto>? seller_rows,
            string detail_header,
            string status_header)
        {
            page.Size(PageSizes.A4);
            page.MarginVertical(18);
            page.MarginHorizontal(22);
            page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(report.title).FontSize(13).Bold().FontColor(PrimaryColor);
                    row.RelativeItem().AlignRight().Text(month_cap).FontSize(11).Bold().FontColor("#666666");
                });
                col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Content().PaddingVertical(8).Column(col =>
            {
                if (seller_name == null)
                {
                    col.Item().Text("Sin ventas para el mes seleccionado.").FontSize(11).FontColor("#888888");
                    return;
                }

                var srows = seller_rows ?? new List<monthly_report_row_dto>();

                col.Item().Text($"VENDEDOR: {seller_name}").FontSize(12).Bold().FontColor(PrimaryColor);
                col.Item().PaddingTop(2).Text("DETALLE DE NOTAS").FontSize(10).Bold().FontColor("#666666");

                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.9f);   // FECHA
                        columns.RelativeColumn(1.3f);   // DOCUMENTO
                        columns.RelativeColumn(3);      // CLIENTE
                        columns.RelativeColumn(1.4f);   // MONTO
                        columns.RelativeColumn(1.8f);   // DETALLE
                        columns.RelativeColumn(1.1f);   // ESTADO
                    });

                    table.Header(header =>
                    {
                        Action<IContainer, string> l = (cell, text) => cell.Background(PrimaryColor).Padding(4).Text(text).FontColor(Colors.White).Bold().FontSize(8);
                        Action<IContainer, string> r = (cell, text) => { cell.Background(PrimaryColor).Padding(4).AlignRight().Text(text).FontColor(Colors.White).Bold().FontSize(8); };
                        Action<IContainer, string> c = (cell, text) => { cell.Background(PrimaryColor).Padding(4).AlignCenter().Text(text).FontColor(Colors.White).Bold().FontSize(8); };
                        l(header.Cell(), "FECHA");
                        l(header.Cell(), "DOCUMENTO");
                        l(header.Cell(), "CLIENTE");
                        r(header.Cell(), "MONTO ($)");
                        r(header.Cell(), detail_header);
                        c(header.Cell(), status_header);
                    });

                    bool alternate = false;
                    foreach (var r in srows)
                    {
                        string bg = alternate ? AccentBg : Colors.White;
                        table.Cell().Background(bg).Padding(3).Text(r.fecha_display).FontSize(8);
                        table.Cell().Background(bg).Padding(3).Text(r.document_number).FontSize(8);
                        table.Cell().Background(bg).Padding(3).Text(r.customer_name).FontSize(8);
                        table.Cell().Background(bg).Padding(3).AlignRight().Text(r.monto_display).FontSize(8);
                        table.Cell().Background(bg).Padding(3).AlignRight().Text(r.detail_text).FontSize(8).FontColor("#555555");
                        table.Cell().Background(bg).Padding(3).AlignCenter().Text(r.status).FontSize(8).Bold().FontColor(StatusColor(r.status));
                        alternate = !alternate;
                    }
                });

                decimal seller_total = srows.Sum(x => x.amount_usd);
                decimal seller_paid = srows.Sum(x => x.paid_amount_usd);
                decimal seller_balance = srows.Sum(x => x.balance_due_usd);
                int seller_count = srows.Count;

                col.Item().PaddingTop(6).AlignRight().Row(row =>
                {
                    row.ConstantItem(230).Border(0.5f).BorderColor(LightBorder).Padding(6).Column(totals =>
                    {
                        totals.Item().Row(t =>
                        {
                            t.RelativeItem().Text("NOTAS:").FontSize(9);
                            t.RelativeItem().AlignRight().Text(seller_count.ToString()).FontSize(9);
                        });
                        totals.Item().PaddingTop(2).Row(t =>
                        {
                            t.RelativeItem().Text("SUB-TOTAL:").FontSize(9);
                            t.RelativeItem().AlignRight().Text($"{seller_total:N2}").FontSize(9);
                        });
                        if (report.show_paid_balance_summary)
                        {
                            totals.Item().PaddingTop(2).Row(t =>
                            {
                                t.RelativeItem().Text("Abonado:").FontSize(9).FontColor("#2E7D32");
                                t.RelativeItem().AlignRight().Text($"{seller_paid:N2}").FontSize(9).FontColor("#2E7D32");
                            });
                            totals.Item().PaddingTop(2).Row(t =>
                            {
                                t.RelativeItem().Text("Saldo:").FontSize(9).FontColor("#C62828");
                                t.RelativeItem().AlignRight().Text($"{seller_balance:N2}").FontSize(9).FontColor("#C62828");
                            });
                        }
                        totals.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(LightBorder);
                        totals.Item().PaddingTop(3).Row(t =>
                        {
                            t.RelativeItem().Text("TOTAL:").FontSize(10).Bold();
                            t.RelativeItem().AlignRight().Text($"{seller_total:N2}").FontSize(10).Bold().FontColor(PrimaryColor);
                        });
                    });
                });
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text(report.title).FontSize(7).FontColor("#888888");
                    row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#888888");
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Pagina ").FontSize(7).FontColor("#888888");
                        t.CurrentPageNumber().FontSize(7).FontColor("#888888");
                    });
                });
            });
        }

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private static string StatusColor(string status)
        {
            if (string.Equals(status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)) return "#C62828";
            if (string.Equals(status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase)) return "#E65100";
            if (string.Equals(status?.Trim(), "Pagada", StringComparison.OrdinalIgnoreCase)) return "#2E7D32";
            return "#333333";
        }
    }
}