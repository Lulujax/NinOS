using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class MonthlyReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#000000";
        private static readonly string AccentBg = "#F0F4EC";
        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

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
            string mode = string.IsNullOrWhiteSpace(report.group_mode) ? "Por Vendedor" : report.group_mode;

            var document = Document.Create(container =>
            {
                if (rows.Count == 0)
                {
                    container.Page(page => BuildPage(page, report, month_cap, "GENERAL", null, null));
                    return;
                }

                if (mode == "Por Zona")
                {
                    var grouped = rows
                        .GroupBy(r => string.IsNullOrWhiteSpace(r.zone_name) ? "SIN ZONA" : r.zone_name.Trim())
                        .OrderBy(g => g.Key)
                        .ToList();

                    foreach (var g in grouped)
                    {
                        container.Page(page => BuildPage(page, report, month_cap, "ZONA", g.Key, g.ToList()));
                    }
                }
                else if (mode == "Consolidado")
                {
                    container.Page(page => BuildPage(page, report, month_cap, "GENERAL", "CONSOLIDADO GENERAL", rows));
                }
                else // Por Vendedor
                {
                    var grouped = rows
                        .GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "SIN VENDEDOR" : r.seller_name.Trim())
                        .OrderBy(g => g.Key)
                        .ToList();

                    foreach (var g in grouped)
                    {
                        container.Page(page => BuildPage(page, report, month_cap, "VENDEDOR", g.Key, g.ToList()));
                    }
                }
            });

            document.GeneratePdf(save_dialog.FileName);
        }

        private static void BuildPage(
            PageDescriptor page,
            monthly_report_dto report,
            string month_cap,
            string group_type,
            string? group_title,
            List<monthly_report_row_dto>? group_rows)
        {
            page.Size(PageSizes.Letter);
            page.MarginLeft(1, Unit.Centimetre);
            page.MarginTop(1, Unit.Centimetre);
            page.MarginRight(1, Unit.Centimetre);
            page.MarginBottom(1, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(report.title).FontSize(13).Bold().FontColor(PrimaryColor);
                    row.RelativeItem().AlignRight().Text(month_cap).FontSize(11).Bold().FontColor("#000000");
                });
                col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(PrimaryColor);
            });

            page.Content().PaddingVertical(4).Column(col =>
            {
                if (group_title == null)
                {
                    col.Item().Text(report.empty_text).FontSize(11).FontColor("#000000");
                    return;
                }

                var srows = (group_rows ?? new List<monthly_report_row_dto>())
                    .OrderByCorrelative(r => r.document_number)
                    .ToList();
                var detail_header = string.IsNullOrWhiteSpace(report.detail_column_header) ? "DETALLE" : report.detail_column_header;
                var status_header = string.IsNullOrWhiteSpace(report.status_column_header) ? "ESTADO" : report.status_column_header;

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        string badge = group_type == "ZONA" ? "ZONA" : (group_type == "VENDEDOR" ? "VENDEDOR" : "REPORTE");
                        c.Item().Text(badge).FontSize(6.5f).Bold().FontColor("#000000");
                        c.Item().PaddingTop(1).Text(group_title).FontSize(9).Bold();
                    });
                });

                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.05f); // FECHA
                        columns.RelativeColumn(1.05f); // DOCUMENTO
                        columns.RelativeColumn(2.6f);  // CLIENTE
                        columns.RelativeColumn(1.4f);  // ZONA o VENDEDOR
                        columns.RelativeColumn(1.15f); // MONTO $
                        if (report.show_paid_balance_summary)
                        {
                            columns.RelativeColumn(1.15f); // ABONADO
                        }
                        columns.RelativeColumn(1.1f);  // ESTADO
                    });

                    table.Header(header =>
                    {
                        void h(string text, bool left = false)
                        {
                            var c = header.Cell().Background(PrimaryColor).PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontColor(Colors.White).Bold().FontSize(6.5f);
                        }

                        h("FECHA");
                        h("DOCUMENTO");
                        h("CLIENTE", left: true);
                        string colTitle = group_type == "ZONA" ? "VENDEDOR" : (group_type == "VENDEDOR" ? "ZONA" : "VEND / ZONA");
                        h(colTitle, left: true);
                        h("MONTO $");
                        if (report.show_paid_balance_summary)
                        {
                            h(detail_header);
                        }
                        h(status_header);
                    });

                    bool alternate = false;
                    foreach (var r in srows)
                    {
                        string bg = alternate ? AccentBg : Colors.White;

                        void cell(string text, bool left = false, string color = "#333333", bool bold = false)
                        {
                            var c = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(LightBorder)
                                .PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontSize(7.5f).FontColor(color);
                            if (bold) t.Bold();
                        }

                        string secondary = group_type == "ZONA"
                            ? r.seller_name
                            : (group_type == "VENDEDOR" ? r.zone_name : $"{r.seller_name} ({r.zone_name})");

                        cell(r.fecha_display);
                        cell(r.document_number);
                        cell(r.customer_name, left: true);
                        cell(secondary, left: true);
                        cell(Money(r.amount_usd));
                        if (report.show_paid_balance_summary)
                        {
                            cell(r.detail_text);
                        }
                        cell(r.status, color: StatusColor(r.status), bold: true);

                        alternate = !alternate;
                    }
                });

                decimal group_total = srows.Sum(x => x.amount_usd);
                decimal group_paid = srows.Sum(x => x.paid_amount_usd);
                decimal group_balance = srows.Sum(x => x.balance_due_usd);

                col.Item().PaddingTop(10).Row(outerRow =>
                {
                    outerRow.RelativeItem();

                    outerRow.ConstantItem(320).Border(0.5f).BorderColor(LightBorder).Column(bottom =>
                    {
                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("SUB TOTAL $").FontSize(9).Bold().FontColor("#000000");
                            r.ConstantItem(120).AlignRight().Text(Money(group_total)).FontSize(10).Bold();
                        });

                        if (report.show_paid_balance_summary)
                        {
                            bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                            bottom.Item().Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text("ABONADO $").FontSize(9).Bold().FontColor("#2E7D32");
                                r.ConstantItem(120).AlignRight().Text(Money(group_paid)).FontSize(10).Bold().FontColor("#2E7D32");
                            });
                            bottom.Item().PaddingHorizontal(4).PaddingBottom(4).Row(r =>
                            {
                                r.RelativeItem().Text("SALDO $").FontSize(9).Bold().FontColor("#C62828");
                                r.ConstantItem(120).AlignRight().Text(Money(group_balance)).FontSize(10).Bold().FontColor("#C62828");
                            });
                        }

                        bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL $").FontSize(9).Bold().FontColor(PrimaryColor);
                            r.ConstantItem(120).AlignRight().Text(Money(group_total)).FontSize(12).Bold().FontColor(PrimaryColor);
                        });
                    });
                });

                if (report.show_goal_block && (report.sales_goal_usd.HasValue || report.month_total_usd != 0m))
                {
                    col.Item().PaddingTop(8).Border(0.5f).BorderColor(LightBorder).Column(b =>
                    {
                        b.Item().Background(AccentBg).Padding(4).Text("META DE VENTAS").FontSize(8.5f).Bold().FontColor(PrimaryColor);
                        b.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("META").FontSize(7.5f).Bold().FontColor("#000000");
                                c.Item().Text(report.sales_goal_usd.HasValue ? Money(report.sales_goal_usd.Value) : "Sin meta").FontSize(9).Bold();
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("VENTA DEL MES (Cumplimiento)").FontSize(7.5f).Bold().FontColor("#000000");
                                c.Item().Text(Money(report.month_total_usd)).FontSize(9).Bold().FontColor("#2E7D32");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PORCENTAJE").FontSize(7.5f).Bold().FontColor("#000000");
                                c.Item().Text(report.goal_progress_percent.ToString("0.0", Ve) + "%").FontSize(9).Bold();
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("FALTA PARA META").FontSize(7.5f).Bold().FontColor("#000000");
                                c.Item().Text(Money(Math.Max(0m, report.goal_remaining_usd))).FontSize(9).Bold().FontColor("#C62828");
                            });
                        });
                        if (!string.IsNullOrWhiteSpace(report.goal_status_text))
                        {
                            b.Item().PaddingHorizontal(4).PaddingBottom(4).Text(report.goal_status_text).FontSize(7.5f).Italic().FontColor("#000000");
                        }
                    });
                }
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text(report.title).FontSize(7).FontColor("#000000");
                    row.RelativeItem().AlignCenter().Text($"Impreso: {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#000000");
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Pagina ").FontSize(7).FontColor("#000000");
                        t.CurrentPageNumber().FontSize(7).FontColor("#000000");
                        t.Span(" de ").FontSize(7).FontColor("#000000");
                        t.TotalPages().FontSize(7).FontColor("#000000");
                    });
                });
            });
        }

        private static string Money(decimal value) => "$" + value.ToString("#,##0.00", Ve);

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private static string StatusColor(string status)
        {
            if (string.Equals(status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)) return "#C62828";
            if (string.Equals(status?.Trim(), "Devuelta", StringComparison.OrdinalIgnoreCase)) return "#7B1FA2";
            if (string.Equals(status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase)) return "#E65100";
            if (string.Equals(status?.Trim(), "Pagada", StringComparison.OrdinalIgnoreCase)) return "#2E7D32";
            return "#333333";
        }
    }
}
