using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
        private static readonly string[] Palette = { "#1B3A2D", "#2E7D32", "#1565C0", "#6A1B9A", "#E65100", "#C62828", "#00897B", "#F9A825" };

        public static void generate(monthly_report_dto report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar reporte de mes",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Reporte_{report.month.Replace(" ", "_")}.pdf"
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

            string bar_svg = BuildBarChart(grouped);
            string donut_svg = report.show_paid_balance_summary ? BuildDonutChart(total_paid, total_balance) : string.Empty;

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
                            SummaryBox(summary.RelativeItem(), "DOCUMENTOS", doc_count.ToString(), PrimaryColor);
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

                        col.Item().PaddingTop(8).Row(chartRow =>
                        {
                            chartRow.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("VENTAS POR VENDEDOR (USD)").FontSize(10).Bold().FontColor(PrimaryColor);
                                if (grouped.Count > 0)
                                    c.Item().PaddingTop(4).Svg(bar_svg).FitWidth();
                                else
                                    c.Item().PaddingTop(4).Text("Sin datos para el mes seleccionado.").FontSize(9).FontColor("#888888");
                            });

                            if (report.show_paid_balance_summary && grand_total > 0)
                            {
                                chartRow.ConstantItem(16);
                                chartRow.RelativeItem(1).Column(c =>
                                {
                                    c.Item().Text("DISTRIBUCION DE COBRO").FontSize(10).Bold().FontColor(PrimaryColor);
                                    c.Item().PaddingTop(4).Svg(donut_svg).FitWidth();
                                    c.Item().PaddingTop(4).Row(legend =>
                                    {
                                        legend.RelativeItem().Column(l =>
                                        {
                                            l.Item().Text(t => { t.Span("● ").FontColor("#2E7D32"); t.Span($"Abonado  $ {total_paid:N2}").FontSize(8); });
                                            l.Item().PaddingTop(2).Text(t => { t.Span("● ").FontColor("#C62828"); t.Span($"Saldo    $ {total_balance:N2}").FontSize(8); });
                                        });
                                    });
                                });
                            }
                        });

                        col.Item().PaddingTop(8).Text("DETALLE DE DOCUMENTOS").FontSize(10).Bold().FontColor(PrimaryColor);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.9f);   // FECHA
                                columns.RelativeColumn(1.3f);   // DOCUMENTO
                                columns.RelativeColumn(3);      // CLIENTE
                                columns.RelativeColumn(1.4f);   // VENDEDOR
                                columns.RelativeColumn(1.4f);   // MONTO
                                columns.RelativeColumn(2.1f);   // DETALLE
                            });

                            table.Header(header =>
                            {
                                Action<string> hcell = t => header.Cell().Background(PrimaryColor).Padding(4).Text(t).FontColor(Colors.White).Bold().FontSize(8);
                                hcell("FECHA");
                                hcell("DOCUMENTO");
                                hcell("CLIENTE");
                                hcell("VENDEDOR");
                                hcell("MONTO ($)");
                                hcell(string.IsNullOrWhiteSpace(report.detail_column_header) ? "DETALLE" : report.detail_column_header);
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
                                table.Cell().Background(bg).Padding(3).Text(r.detail_text).FontSize(8).FontColor("#555555");
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
                                header.Cell().Background(PrimaryColor).Padding(4).Text("DOCS.").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(PrimaryColor).Padding(4).Text("TOTAL ($)").FontColor(Colors.White).Bold().FontSize(8);
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

        private static string BuildBarChart(List<IGrouping<string, monthly_report_row_dto>> grouped)
        {
            int width = 620, height = 230;
            int chart_bottom = height - 28;
            int chart_top = 22;
            int label_w = 120;
            int chart_left = label_w;
            int plot_w = width - label_w - 30;
            int chart_right = chart_left + plot_w;

            decimal max = grouped.Count > 0 ? Math.Max(1m, grouped.Max(g => g.Sum(r => r.amount_usd))) : 1m;

            var sb = new StringBuilder();
            AppendSvgHeader(sb, width, height);

            double step = (double)plot_w / Math.Max(1, grouped.Count);
            int i = 0;
            foreach (var g in grouped)
            {
                decimal total = g.Sum(r => r.amount_usd);
                double bar_h = (double)(total / max) * (chart_bottom - chart_top);
                double x = chart_left + step * i + step * 0.18;
                double w = step * 0.64;
                double y = chart_bottom - bar_h;
                string color = Palette[i % Palette.Length];

                sb.Append($"<rect x='{ToStr(x)}' y='{ToStr(y)}' width='{ToStr(w)}' height='{ToStr(bar_h)}' rx='4' fill='{color}'/>");
                sb.Append($"<text x='{ToStr(x + w / 2)}' y='{ToStr(y - 8)}' text-anchor='middle' font-size='13' font-weight='bold' fill='{color}' font-family='Arial'>{total:N0}</text>");
                sb.Append($"<text x='{ToStr((x + w / 2) - 9)}' y='{chart_bottom + 16}' text-anchor='end' font-size='11' fill='#444444' font-family='Arial'>{EscapeXml(g.Key)}</text>");
                i++;
            }

            sb.Append($"<line x1='{chart_left - 6}' y1='{chart_bottom}' x2='{chart_right}' y2='{chart_bottom}' stroke='#BBBBBB' stroke-width='1.5'/>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string BuildDonutChart(decimal paid, decimal balance)
        {
            int size = 260;
            decimal total = paid + balance;
            if (total <= 0) total = 1;

            double cx = size / 2.0, cy = size / 2.0;
            double r = size * 0.36;

            double a_paid = (double)(paid / total) * 360.0;
            double a_balance = 360.0 - a_paid;

            var sb = new StringBuilder();
            AppendSvgHeader(sb, size, size);

            if (a_paid > 0.5)
                sb.Append(ArcPath(cx, cy, r, 0, a_paid, "#2E7D32"));
            if (a_balance > 0.5)
                sb.Append(ArcPath(cx, cy, r, a_paid, a_balance, "#C62828"));

            sb.Append($"<circle cx='{ToStr(cx)}' cy='{ToStr(cy)}' r='{ToStr(r * 0.6)}' fill='#FFFFFF'/>");
            sb.Append($"<text x='{ToStr(cx)}' y='{ToStr(cy - 4)}' text-anchor='middle' font-size='15' font-weight='bold' fill='#1B3A2D' font-family='Arial'>${total:N0}</text>");
            sb.Append($"<text x='{ToStr(cx)}' y='{ToStr(cy + 16)}' text-anchor='middle' font-size='10' fill='#777777' font-family='Arial'>TOTAL</text>");

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string ArcPath(double cx, double cy, double r, double start_deg, double sweep_deg, string color)
        {
            double start_rad = (start_deg - 90) * Math.PI / 180.0;
            double end_deg = start_deg + sweep_deg;
            double end_rad = (end_deg - 90) * Math.PI / 180.0;

            double x1 = cx + r * Math.Cos(start_rad);
            double y1 = cy + r * Math.Sin(start_rad);
            double x2 = cx + r * Math.Cos(end_rad);
            double y2 = cy + r * Math.Sin(end_rad);

            int large = sweep_deg > 180 ? 1 : 0;
            return $"<path d='M {ToStr(cx)} {ToStr(cy)} L {ToStr(x1)} {ToStr(y1)} A {ToStr(r)} {ToStr(r)} 0 {large} 1 {ToStr(x2)} {ToStr(y2)} Z' fill='{color}'/>";
        }

        private static void AppendSvgHeader(StringBuilder sb, int width, int height)
        {
            sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>");
        }

        private static string ToStr(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static string ToStr(decimal v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static string EscapeXml(string v) => (v ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}