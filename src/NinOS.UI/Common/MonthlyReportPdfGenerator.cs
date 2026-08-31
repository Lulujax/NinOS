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

            var save_dialog = new SaveFileDialog
            {
                Title = $"Guardar reporte de mes",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Reporte_{report.title}_{report.month.Replace(" ", "_")}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            var grouped = report.rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.seller_name) ? "Sin vendedor" : r.seller_name)
                .OrderBy(g => g.Key)
                .ToList();

            var grand_total = report.rows.Sum(r => r.amount_usd);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginVertical(20);
                    page.MarginHorizontal(25);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(3).Column(left =>
                            {
                                left.Item().Text("NinOS").FontSize(20).Bold().FontColor(PrimaryColor);
                                left.Item().Text("Caracas - Venezuela").FontSize(10).FontColor("#555555");
                            });

                            row.RelativeItem(2).Column(right =>
                            {
                                right.Item().AlignRight().Text(report.title).FontSize(16).Bold().FontColor(PrimaryColor);
                                right.Item().PaddingTop(2).AlignRight().Text(report.month).FontSize(11).Bold();
                            });
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.1f);   // FECHA
                                columns.RelativeColumn(1.4f);   // DOCUMENTO
                                columns.RelativeColumn(3);      // CLIENTE
                                columns.RelativeColumn(1.5f);   // VENDEDOR
                                columns.RelativeColumn(1.4f);   // MONTO
                                columns.RelativeColumn(2);      // DETALLE
                                columns.RelativeColumn(1.2f);   // ESTADO
                            });

                            table.Header(header =>
                            {
                                Action<string> hcell = t => header.Cell().Background(PrimaryColor).Padding(4).Text(t).FontColor(Colors.White).Bold().FontSize(8);
                                hcell("FECHA");
                                hcell("DOCUMENTO");
                                hcell("CLIENTE");
                                hcell("VENDEDOR");
                                hcell("MONTO ($)");
                                hcell(string.IsNullOrWhiteSpace(report.detail_column_header) ? "DETALLE" : report.detail_column_header.ToUpperInvariant());
                                hcell("ESTADO");
                            });

                            bool alternate = false;
                            foreach (var r in report.rows)
                            {
                                string bg = alternate ? AccentBg : Colors.White;
                                table.Cell().Background(bg).Padding(3).Text(r.fecha_display).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(r.document_number).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(r.customer_name).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(r.seller_name).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight().Text(r.monto_display).FontSize(8);
                                table.Cell().Background(bg).Padding(3).Text(r.detail_text).FontSize(8).FontColor("#555555");
                                table.Cell().Background(bg).Padding(3).Text(r.status).FontSize(8);
                                alternate = !alternate;
                            }
                        });

                        col.Item().PaddingTop(8).Text("TOTALES POR VENDEDOR").FontSize(9).Bold().FontColor(PrimaryColor);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(PrimaryColor).Padding(4).Text("VENDEDOR").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(PrimaryColor).Padding(4).Text("TOTAL ($)").FontColor(Colors.White).Bold().FontSize(8);
                            });

                            bool alternate2 = false;
                            foreach (var g in grouped)
                            {
                                string bg = alternate2 ? AccentBg : Colors.White;
                                table.Cell().Background(bg).Padding(3).Text(g.Key).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight().Text(g.Sum(r => r.amount_usd).ToString("N2")).FontSize(8);
                                alternate2 = !alternate2;
                            }
                        });

                        col.Item().PaddingTop(8).AlignRight().Row(row =>
                        {
                            row.ConstantItem(200).Border(0.5f).BorderColor(LightBorder).Padding(6).Column(totals =>
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("TOTAL GENERAL:").FontSize(11).Bold();
                                    r.RelativeItem().AlignRight().Text($"{grand_total:N2}").FontSize(11).Bold().FontColor(PrimaryColor);
                                });
                            });
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        col.Item().PaddingTop(3).Row(row =>
                        {
                            row.RelativeItem().Text($"Reporte {report.title}").FontSize(7).FontColor("#888888");
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
    }
}
