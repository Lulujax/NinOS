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

                // Si TODAS las notas del reporte son anuladas o devueltas, no hay nada que
                // consolidar: se avisa en vez de imprimir paginas con total 0 que parecen
                // una venta de cero.
                bool hay_vigentes = rows.Any(r =>
                    !string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(r.status?.Trim(), "Devuelta", StringComparison.OrdinalIgnoreCase));

                if (!hay_vigentes)
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
                    float title_size = Math.Max(8.5f, Math.Min(12f, 380f / (report.title.Length * 0.7f)));
                    row.RelativeItem(3f).Text(report.title).FontSize(title_size).Bold().FontColor(PrimaryColor);
                    row.RelativeItem().AlignRight().Text(month_cap).FontSize(9.5f).Bold().FontColor("#000000");
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

                // Orden de impresion: primero las cuentas zombie del grupo y luego la cartera organica,
                // todo seguido. Dentro de cada bloque va por correlativo, como siempre.
                // Solo aplica cuando el reporte pide ese orden (anual de CxC); el mensual
                // y el de ventas no cambian de comportamiento.
                var srows = (group_rows ?? new List<monthly_report_row_dto>())
                    .OrderBy(r => report.ordenar_zombies_primero && r.is_customer_ghost ? 0 : 1)
                    .ThenBy(r => SeriesCalculator.ParseCorrelative(r.document_number))
                    .ThenBy(r => r.document_number ?? string.Empty)
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
                        columns.RelativeColumn(1.15f); // CORRELATIVO
                        columns.RelativeColumn(1.05f); // FECHA
                        if (report.show_dispatch_date_column)
                        {
                            columns.RelativeColumn(1.05f); // DESPACHO
                        }
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

                        h("CORRELATIVO");
                        h("FECHA");
                        if (report.show_dispatch_date_column) h("DESPACHO");
                        h("CLIENTE", left: true);
                        string colTitle = group_type == "ZONA" ? "VENDEDOR" : (group_type == "VENDEDOR" ? "ZONA" : "VEND / ZONA");
                        h(colTitle, left: true);
                        h("MONTO");
                        if (report.show_paid_balance_summary)
                        {
                            h(detail_header);
                        }
                        h(status_header);
                    });

                    bool alternate = false;
                    foreach (var r in srows)
                    {
                        bool is_annulled = string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);
                        string bg = is_annulled ? "#FFEBEE" : (alternate ? AccentBg : Colors.White);
                        string defaultColor = is_annulled ? "#C62828" : "#333333";

                        void cell(string text, bool left = false, string? color = null, bool bold = false)
                        {
                            var c = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(LightBorder)
                                .PaddingVertical(2).PaddingHorizontal(1.5f);
                            var t = left ? c.Text(text) : c.AlignCenter().Text(text);
                            t.FontSize(7.5f).FontColor(color ?? defaultColor);
                            if (bold) t.Bold();
                        }

                        string secondary = group_type == "ZONA"
                            ? r.seller_name
                            : (group_type == "VENDEDOR" ? r.zone_name : $"{r.seller_name} ({r.zone_name})");

                        cell(r.document_number);
                        cell(r.fecha_display);
                        if (report.show_dispatch_date_column) cell(r.despacho_display);
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

                // Ni las anuladas ni las devueltas suman. Antes solo se excluian las anuladas y las notas
                // Devuelta se colaban en el subtotal y en el total, con lo que el reporte de
                // ventas daba un monto que no era el vendido. Se muestran aparte para que se
                // vean sin que falseen la cifra.
                var valid_srows = srows.Where(x => !es_anulada(x) && !es_devuelta(x)).ToList();
                decimal group_total = valid_srows.Sum(x => x.amount_usd);
                decimal group_paid = valid_srows.Sum(x => x.paid_amount_usd);
                decimal group_balance = valid_srows.Sum(x => x.balance_due_usd);

                var annulled_rows = srows.Where(x => es_anulada(x)).ToList();
                int annulled_count = annulled_rows.Count;
                decimal annulled_amount = annulled_rows.Sum(x => x.amount_usd);

                var returned_rows = srows.Where(x => es_devuelta(x)).ToList();
                int returned_count = returned_rows.Count;
                decimal returned_amount = returned_rows.Sum(x => x.amount_usd);

                col.Item().PaddingTop(10).Row(outerRow =>
                {
                    outerRow.RelativeItem();

                    outerRow.ConstantItem(380).Border(0.5f).BorderColor(LightBorder).Column(bottom =>
                    {
                        bottom.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("SUBTOTAL POR COBRAR").FontSize(9).Bold().FontColor("#000000");
                            r.ConstantItem(120).AlignRight().Text(Money(group_total)).FontSize(10).Bold();
                        });

                        if (report.show_paid_balance_summary)
                        {
                            bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                            bottom.Item().Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text("SALDO YA COBRADO").FontSize(9).Bold().FontColor("#2E7D32");
                                r.ConstantItem(120).AlignRight().Text(Money(group_paid)).FontSize(10).Bold().FontColor("#2E7D32");
                            });
                            bottom.Item().PaddingHorizontal(4).PaddingBottom(4).Row(r =>
                            {
                                r.RelativeItem().Text("SALDO POR COBRAR").FontSize(9).Bold().FontColor("#C62828");
                                r.ConstantItem(120).AlignRight().Text(Money(group_balance)).FontSize(10).Bold().FontColor("#C62828");
                            });
                        }

                        if (annulled_count > 0)
                        {
                            bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                            bottom.Item().Background("#FFEBEE").Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text($"ANULADAS ({annulled_count}):").FontSize(8).Bold().FontColor("#C62828");
                                r.ConstantItem(120).AlignRight().Text(Money(annulled_amount)).FontSize(8.5f).Bold().FontColor("#C62828");
                            });
                            bottom.Item().Background("#FFEBEE").PaddingHorizontal(4).PaddingBottom(2).Text("(Excluidas del subtotal y del saldo por cobrar)").FontSize(6.5f).Italic().FontColor("#C62828");
                        }

                        if (returned_count > 0)
                        {
                            bottom.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                            bottom.Item().Background("#F3E5F5").Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text($"DEVUELTAS ({returned_count}):").FontSize(8).Bold().FontColor("#7B1FA2");
                                r.ConstantItem(120).AlignRight().Text(Money(returned_amount)).FontSize(8.5f).Bold().FontColor("#7B1FA2");
                            });
                            bottom.Item().Background("#F3E5F5").PaddingHorizontal(4).PaddingBottom(2).Text("(Excluidas del subtotal y del saldo por cobrar)").FontSize(6.5f).Italic().FontColor("#7B1FA2");
                        }
                    });
                });

                // Bloque de meta: la meta es por vendedor, nunca por zona. En "Por Vendedor"
                // cada pagina lleva la meta de SU vendedor contra la venta de ese vendedor. Si el
                // alcance del reporte no coincide con lo que cubre la meta, SalesViewModel deja
                // show_goal_block en false para no imprimir un cumplimiento que seria mentira.
                bool show_goal = report.show_goal_block;
                decimal? goal_value = report.sales_goal_usd;
                decimal goal_sale = report.month_total_usd;
                string goal_label = string.IsNullOrWhiteSpace(report.goal_scope_label) ? "META DEL MES" : report.goal_scope_label;
                string sale_label = "VENTA DEL MES (Cumplimiento)";

                if (show_goal && group_type == "VENDEDOR")
                {
                    if (group_title != null && report.goal_by_group.TryGetValue(group_title, out var seller_goal))
                    {
                        goal_value = seller_goal;
                        goal_sale = group_total;
                        goal_label = "META DEL VENDEDOR";
                        sale_label = "VENTA DEL VENDEDOR (Cumplimiento)";
                    }
                    else
                    {
                        show_goal = false; // este vendedor no tiene meta que comparar
                    }
                }

                if (show_goal && (goal_value.HasValue || goal_sale != 0m))
                {
                    decimal goal_amount = goal_value.GetValueOrDefault();
                    bool has_goal = goal_value.HasValue && goal_amount > 0m;
                    decimal goal_pct = has_goal ? Math.Min(100m, goal_sale / goal_amount * 100m) : 0m;
                    decimal goal_missing = has_goal ? Math.Max(0m, goal_amount - goal_sale) : 0m;
                    string goal_status = !has_goal
                        ? string.Empty
                        : (goal_missing > 0 ? $"Falta {goal_missing:N2} para la meta" : "Meta alcanzada!");

                    col.Item().PaddingTop(8).Border(0.5f).BorderColor(LightBorder).Column(b =>
                    {
                        b.Item().Background(AccentBg).Padding(4).Text(goal_label).FontSize(8.5f).Bold().FontColor(PrimaryColor);
                        b.Item().Padding(4).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("META").FontSize(7.5f).Bold().FontColor("#000000");
                                c.Item().Text(has_goal ? Money(goal_amount) : "Sin meta").FontSize(9).Bold();
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(sale_label).FontSize(7.5f).Bold().FontColor("#000000");
                                c.Item().Text(Money(goal_sale)).FontSize(9).Bold().FontColor("#2E7D32");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PORCENTAJE").FontSize(7.5f).Bold().FontColor("#000000");
                                c.Item().Text(has_goal ? goal_pct.ToString("0.0", Ve) + "%" : "-").FontSize(9).Bold();
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("FALTA PARA META").FontSize(7.5f).Bold().FontColor("#000000");
                                c.Item().Text(has_goal ? Money(goal_missing) : "-").FontSize(9).Bold().FontColor("#C62828");
                            });
                        });
                        if (!string.IsNullOrWhiteSpace(goal_status))
                        {
                            b.Item().PaddingHorizontal(4).PaddingBottom(4).Text(goal_status).FontSize(7.5f).Italic().FontColor("#000000");
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

        private static string Money(decimal value) => value.ToString("#,##0.00", Ve);

        private static string Capitalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        /// <summary>
        /// Una nota Anulada o Devuelta no suma en ningun total del reporte. Se muestran en su
        /// propio bloque al pie para que se vean sin falsear la cifra.
        /// </summary>
        private static bool es_anulada(monthly_report_row_dto r)
            => string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

        private static bool es_devuelta(monthly_report_row_dto r)
            => string.Equals(r.status?.Trim(), "Devuelta", StringComparison.OrdinalIgnoreCase);

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
