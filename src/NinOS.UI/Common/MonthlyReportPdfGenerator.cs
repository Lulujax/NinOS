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
                .Where(r => !string.IsNullOrWhiteSpace(r.seller_name))
                .GroupBy(r => r.seller_name.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            var grand_total = rows.Sum(r => r.amount_usd);
            var total_paid = rows.Sum(r => r.paid_amount_usd);
            var total_balance = rows.Sum(r => r.balance_due_usd);
            var doc_count = rows.Count;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginVertical(18);
                    page.MarginHorizontal(22);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(3).Column(left =>
                            {
                                left.Item().Text("NinOS").FontSize(20).Bold().FontColor(PrimaryColor);
                                left.Item().Text("Caracas - Venezuela").FontSize(10).FontColor("#555555");
                                left.Item().PaddingTop(6).Text("REPORTE DETALLADO").FontSize(9).Bold().FontColor("#888888");
                            });

                            row.RelativeItem(2).Column(right =>
                            {
                                right.Item().AlignRight().Text(report.title).FontSize(13).Bold().FontColor(PrimaryColor);
                                right.Item().PaddingTop(2).AlignRight().Text(month_cap).FontSize(11).Bold().FontColor("#666666");
                            });
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Item().Row(summary =>
                        {
                            SummaryBox(summary.RelativeItem(), "NOTAS", doc_count.ToString(), PrimaryColor);
                            SummaryBox(summary.RelativeItem(), "TOTAL FACTURADO", $"$ {grand_total:N2}", "#1565C0");
                            if (report.show_paid_balance_summary)
                            {
                                SummaryBox(summary.RelativeItem(), "ABONADO", $"$ {total_paid:N2}", "#2E7D32");
                                SummaryBox(summary.RelativeItem(), "SALDO", $"$ {total_balance:N2}", "#C62828");
                            }
                            else
                            {
                                SummaryBox(summary.RelativeItem(), "TOTAL RECIBIDO", $"$ {grand_total:N2}", "#2E7D32");
                            }
                        });

                        col.Item().PaddingTop(8).Text("DETALLE DE NOTAS").FontSize(10).Bold().FontColor(PrimaryColor);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.9f);   // FECHA
                                columns.RelativeColumn(1.2f);   // DOCUMENTO
                                columns.RelativeColumn(2.6f);   // CLIENTE
                                columns.RelativeColumn(1.3f);   // VENDEDOR
                                columns.RelativeColumn(1.3f);   // MONTO
                                columns.RelativeColumn(1.7f);   // DETALLE
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
                                l(header.Cell(), "VENDEDOR");
                                r(header.Cell(), "MONTO ($)");
                                r(header.Cell(), string.IsNullOrWhiteSpace(report.detail_column_header) ? "DETALLE" : report.detail_column_header);
                                c(header.Cell(), string.IsNullOrWhiteSpace(report.status_column_header) ? "ESTADO" : report.status_column_header);
                            });

                            bool alternate = false;
                            foreach (var r in rows)
                            {
                                string bg = alternate ? AccentBg : Colors.White;
                                table.Cell().Background(bg).Padding(3).Text(r.fecha_display).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(r.document_number).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(r.customer_name).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(r.seller_name).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight().Text(r.monto_display).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight().Text(r.detail_text).FontSize(8).FontColor("#555555");
                                table.Cell().Background(bg).Padding(3).AlignCenter().Text(r.status).FontSize(8).Bold().FontColor(StatusColor(r.status));
                                alternate = !alternate;
                            }
                        });

                        col.Item().PaddingTop(8).Text("TOTALES POR VENDEDOR").FontSize(9).Bold().FontColor(PrimaryColor);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1.4f);
                                columns.RelativeColumn(1.6f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(PrimaryColor).Padding(4).Text("VENDEDOR").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(PrimaryColor).Padding(4).AlignCenter().Text("NOTAS").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("TOTAL ($)").FontColor(Colors.White).Bold().FontSize(8);
                            });

                            bool alternate2 = false;
                            foreach (var g in grouped)
                            {
                                string bg = alternate2 ? AccentBg : Colors.White;
                                table.Cell().Background(bg).Padding(3).Text(g.Key).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignCenter().Text(g.Count().ToString()).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight().Text(g.Sum(r => r.amount_usd).ToString("N2")).FontSize(8);
                                alternate2 = !alternate2;
                            }
                        });

                        col.Item().PaddingTop(8).AlignRight().Row(row =>
                        {
                            row.ConstantItem(220).Border(0.5f).BorderColor(LightBorder).Padding(6).Column(totals =>
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("SUB-TOTAL:").FontSize(9);
                                    r.RelativeItem().AlignRight().Text($"{grand_total:N2}").FontSize(9);
                                });
                                if (report.show_paid_balance_summary)
                                {
                                    totals.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.RelativeItem().Text("Abonado:").FontSize(9).FontColor("#2E7D32");
                                        r.RelativeItem().AlignRight().Text($"{total_paid:N2}").FontSize(9).FontColor("#2E7D32");
                                    });
                                    totals.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.RelativeItem().Text("Saldo:").FontSize(9).FontColor("#C62828");
                                        r.RelativeItem().AlignRight().Text($"{total_balance:N2}").FontSize(9).FontColor("#C62828");
                                    });
                                }
                                totals.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(LightBorder);
                                totals.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("TOTAL GENERAL:").FontSize(10).Bold();
                                    r.RelativeItem().AlignRight().Text($"{grand_total:N2}").FontSize(10).Bold().FontColor(PrimaryColor);
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
                });
            });

            document.GeneratePdf(save_dialog.FileName);
        }

        private static void SummaryBox(IContainer container, string label, string value, string color)
        {
            container.Padding(3).Border(0.5f).BorderColor("#D0D0D0").Background("#FFFFFF").Padding(6).Column(c =>
            {
                c.Item().Text(label).FontSize(7).Bold().FontColor("#777777");
                c.Item().PaddingTop(2).Text(value).FontSize(12).Bold().FontColor(color);
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