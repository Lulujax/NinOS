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
    public static class ProVentaPdfGenerator
    {
        private static readonly string Accent = "#1565C0";
        private static readonly string SoftAccent = "#E3F2FD";
        private static readonly string Yellow = "#FFF2A8";
        private static readonly string BorderColor = "#B0BEC5";

        // Colores de la nota anulada. Se imprimen en rojo entero (fondo y texto) en las dos
        // paginas del reporte, igual que en la pantalla.
        private static readonly string AnnulledBackground = "#FFEBEE";
        private static readonly string AnnulledForeground = "#C62828";

        /// <summary>
        /// Texto de la leyenda de anuladas. Va en las dos paginas del reporte con el mismo texto
        /// que la pantalla, para que el PDF impreso no se preste a confusion: el TOTAL excluye las
        /// notas rojas, y eso hay que decirlo en el papel, no solo en la aplicacion.
        /// </summary>
        private static string AnulledLegendText(int count, decimal amount)
        {
            return "Las notas marcadas en rojo están ANULADAS y no se incluyen en los totales ni en la liquidación. "
                 + $"Anuladas: {count} nota(s) por {amount:N2} USD.";
        }

        public static void generate(
            pro_venta_weekly_dto dto,
            decimal nota_por_pagar,
            List<payment_dto>? payments = null,
            pro_venta_relation_row? relation_info = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar reporte de relacion",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Relacion_{dto.relation_number}_Semana_{dto.week_start:ddMMyy}-{dto.week_end:ddMMyy}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            decimal total_cobrado = dto.total_amount - dto.total_commission_luis;
            decimal diferencial = total_cobrado - nota_por_pagar;

            // Igual que en la ventana del historial: las notas anuladas no cuentan para el monto y
            // los asientos de anulacion (monto negativo) no cuentan como abono. Sumarlos correria el saldo.
            var vigente = dto.rows.Where(n => !n.esta_anulada).ToList();

            decimal total_notes = vigente.Sum(n => n.amount);
            decimal total_paid = payments != null && payments.Count > 0
                ? payments.Where(p => !p.es_anulacion).Sum(p => p.amount_usd)
                : (relation_info?.paid_amount_usd ?? 0m);

            decimal balance_due = relation_info != null
                ? relation_info.balance_due_usd
                : Math.Max(0m, total_notes - total_paid);

            bool is_paid = string.Equals(relation_info?.status, "Pagada", StringComparison.OrdinalIgnoreCase)
                || (total_paid > 0 && balance_due <= 0.005m);

            var document = Document.Create(container =>
            {
                // PÁGINA 1: REPORTE SEMANAL DE LA RELACIÓN (Hoja de cobranza / liquidación con columnas para llenado manual)
                container.Page(page =>
                {
                    BuildWeeklyReportPage(page, dto, total_cobrado, nota_por_pagar, diferencial);
                });

                // PÁGINA 2: DETALLE Y REGISTRO DE PAGOS (Solo se anexa cuando la relación ya está pagada)
                if (is_paid)
                {
                    container.Page(page =>
                    {
                        BuildDetailAndPaymentsPage(page, dto, payments, relation_info);
                    });
                }
            });

            document.GeneratePdf(save_dialog.FileName);
        }

        private static void BuildWeeklyReportPage(
            PageDescriptor page,
            pro_venta_weekly_dto dto,
            decimal total_cobrado,
            decimal nota_por_pagar,
            decimal diferencial)
        {
            page.Size(PageSizes.Letter);
            page.MarginLeft(0.8f, Unit.Centimetre);
            page.MarginTop(0.8f, Unit.Centimetre);
            page.MarginRight(0.8f, Unit.Centimetre);
            page.MarginBottom(0.8f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(7.5f));

            page.Content().Column(col =>
            {
                col.Item().Text($"RELACION NRO {dto.relation_number}").FontSize(15).Bold().FontColor(Accent).AlignCenter();
                col.Item().Text($"{dto.week_start:dd/MM} AL {dto.week_end:dd/MM} _ FACTURAS POR COBRAR {dto.city}").FontSize(9.5f).Bold().FontColor("#000000").AlignCenter();
                col.Item().PaddingTop(3).PaddingBottom(4).LineHorizontal(1.5f).LineColor(Accent);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.35f); // NRO NOTA CLIENTE
                        columns.RelativeColumn(2.65f); // CLIENTE
                        columns.RelativeColumn(1.05f); // MONTO NOTA
                        columns.RelativeColumn(1.20f); // COMISION LUIS 10%
                        columns.RelativeColumn(1.25f); // GASTOS OPERATIVOS 25%
                        columns.RelativeColumn(1.25f); // GASTOS ADMINISTRATIVOS 15%
                        columns.RelativeColumn(1.40f); // FECHA PAGO CLIENTE
                        columns.RelativeColumn(1.70f); // FORMA DE PAGO
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("NRO NOTA CLIENTE").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).Text("CLIENTE").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("MONTO NOTA").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("COMISION LUIS 10%").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("GASTOS OPER. 25%").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("GASTOS ADM. 15%").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("FECHA PAGO CLIENTE").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("FORMA DE PAGO").Bold().FontColor(Colors.White);
                    });

                    bool alternate = false;
                    foreach (var r in dto.rows)
                    {
                        // La nota anulada se imprime entera en rojo para que se note que esta
                        // pero no cuenta. El cebrao sigue avanzando igual, para que al volver a
                        // una nota vigente no queden dos filas rosadas seguidas.
                        bool anulada = r.esta_anulada;
                        string bg = anulada ? AnnulledBackground : (alternate ? SoftAccent : Colors.White);
                        string fg = anulada ? AnnulledForeground : Colors.Black;
                        alternate = !alternate;

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(BorderColor).MinHeight(22).PaddingHorizontal(2).PaddingVertical(3).AlignCenter().Text(r.note_number).FontColor(fg);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(BorderColor).MinHeight(22).PaddingHorizontal(2).PaddingVertical(3).Text(r.customer_name).FontColor(fg);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(BorderColor).MinHeight(22).PaddingHorizontal(2).PaddingVertical(3).AlignCenter().Text(r.amount.ToString("N2")).FontColor(fg);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(BorderColor).MinHeight(22).PaddingHorizontal(2).PaddingVertical(3).AlignCenter().Text(r.commission_luis.ToString("N2")).FontColor(fg);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(BorderColor).MinHeight(22).PaddingHorizontal(2).PaddingVertical(3).AlignCenter().Text(r.gastos_25.ToString("N2")).FontColor(fg);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(BorderColor).MinHeight(22).PaddingHorizontal(2).PaddingVertical(3).AlignCenter().Text(r.gastos_15.ToString("N2")).FontColor(fg);
                        // Espacio en blanco con altura suficiente para llenado a mano por el vendedor (lápiz / bolígrafo)
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(BorderColor).MinHeight(22).PaddingHorizontal(2).PaddingVertical(3).Text("");
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(BorderColor).MinHeight(22).PaddingHorizontal(2).PaddingVertical(3).Text("");
                    }

                    if (dto.rows.Count > 0)
                    {
                        table.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text("TOTAL").Bold();
                        table.Cell().Background(SoftAccent);
                        table.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(dto.total_amount.ToString("N2")).Bold();
                        table.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(dto.total_commission_luis.ToString("N2")).Bold();
                        table.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(dto.total_gastos_25.ToString("N2")).Bold();
                        table.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(dto.total_gastos_15.ToString("N2")).Bold();
                        table.Cell().Background(SoftAccent);
                        table.Cell().Background(SoftAccent);
                    }
                });

                if (dto.has_annulled)
                {
                    col.Item().PaddingTop(6).Table(legend =>
                    {
                        legend.ColumnsDefinition(columns => columns.RelativeColumn());
                        legend.Cell().Background(AnnulledBackground).Border(0.5f).BorderColor(AnnulledForeground)
                            .Padding(4)
                            .Text(AnulledLegendText(dto.annulled_count, dto.annulled_amount))
                            .FontSize(7).FontColor(AnnulledForeground);
                    });
                }

                col.Item().PaddingTop(8).Text("LIQUIDACION").FontSize(9).Bold().FontColor(Accent);
                col.Item().Table(liquidation =>
                {
                    liquidation.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(5f);
                        columns.RelativeColumn(2f);
                    });

                    liquidation.Cell().Background(SoftAccent).Padding(3).Text("TOTAL COBRADO (NOTAS MENOS COMISION LUIS G)").Bold();
                    liquidation.Cell().Background(SoftAccent).Padding(3).AlignCenter().Text(total_cobrado.ToString("N2")).Bold();

                    liquidation.Cell().Background(Yellow).Padding(3).Text("NOTA DE ENTREGA POR PAGAR A PRO VENTA").Bold();
                    liquidation.Cell().Background(Yellow).Padding(3).AlignCenter().Text(nota_por_pagar.ToString("N2")).Bold();

                    liquidation.Cell().BorderTop(0.5f).Background(SoftAccent).Padding(3).Text("DIFERENCIAL").Bold();
                    liquidation.Cell().BorderTop(0.5f).Background(SoftAccent).Padding(3).AlignCenter().Text(diferencial.ToString("N2")).Bold();
                });
            });
        }

        private static void BuildDetailAndPaymentsPage(
            PageDescriptor page,
            pro_venta_weekly_dto dto,
            List<payment_dto>? payments,
            pro_venta_relation_row? relation_info)
        {
            page.Size(PageSizes.Letter);
            page.MarginLeft(1, Unit.Centimetre);
            page.MarginTop(1, Unit.Centimetre);
            page.MarginRight(1, Unit.Centimetre);
            page.MarginBottom(1, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

            // Igual que en la ventana del historial: las notas anuladas no cuentan para el monto y los
            // asientos de anulacion (monto negativo) no cuentan como abono. Sumarlos correria el saldo.
            var vigente = dto.rows.Where(n => !n.esta_anulada).ToList();

            decimal total_notes = vigente.Sum(n => n.amount);
            decimal total_paid = payments != null && payments.Count > 0
                ? payments.Where(p => !p.es_anulacion).Sum(p => p.amount_usd)
                : (relation_info?.paid_amount_usd ?? 0m);

            decimal balance_due = total_notes - total_paid;
            if (balance_due < 0) balance_due = 0;

            string relation_label = $"NRO {dto.relation_number} ({dto.week_start:dd/MM} AL {dto.week_end:dd/MM})";
            string info_line = $"MONTO: {total_notes:N2}  |  ABONADO: {total_paid:N2}  |  SALDO PENDIENTE: {balance_due:N2}";

            page.Content().Column(col =>
            {
                // Título idéntico a la ventana
                col.Item().Text("DETALLE DE LA RELACION").FontSize(16).Bold().FontColor(Accent).AlignCenter();

                // Banner idéntico a la ventana (RelationInfoText en ProVentaHistoryWindow)
                col.Item().PaddingTop(6).PaddingBottom(8).Background(SoftAccent).Border(1).BorderColor("#BBDEFB").Padding(6).Column(b =>
                {
                    b.Item().Text(relation_label).FontSize(9.5f).Bold().FontColor(Accent);
                    b.Item().Text(info_line).FontSize(9.5f).Bold().FontColor(Accent);
                });

                // TABLA 1: NOTAS DE LA RELACION
                col.Item().PaddingBottom(4).Text("NOTAS DE LA RELACION").FontSize(10).Bold().FontColor(Accent);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(3f);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("NRO NOTA").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).Text("CLIENTE").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("MONTO").Bold().FontColor(Colors.White);
                    });

                    bool alternate = false;
                    foreach (var n in dto.rows)
                    {
                        // Mismo criterio que en la pagina 1: la anulada se ve en rojo pero no suma.
                        bool anulada = n.esta_anulada;
                        string bg = anulada ? AnnulledBackground : (alternate ? SoftAccent : Colors.White);
                        string fg = anulada ? AnnulledForeground : Colors.Black;
                        alternate = !alternate;

                        table.Cell().Background(bg).Padding(2).AlignCenter().Text(n.note_number).FontColor(fg);
                        table.Cell().Background(bg).Padding(2).Text(n.customer_name).FontColor(fg);
                        table.Cell().Background(bg).Padding(2).AlignCenter().Text(n.amount.ToString("N2")).FontColor(fg);
                    }

                    if (dto.rows.Count > 0)
                    {
                        table.Cell().Background(SoftAccent).Padding(2).Text("TOTAL").Bold();
                        table.Cell().Background(SoftAccent);
                        table.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(total_notes.ToString("N2")).Bold();
                    }
                });

                if (dto.has_annulled)
                {
                    col.Item().PaddingTop(6).Table(legend =>
                    {
                        legend.ColumnsDefinition(columns => columns.RelativeColumn());
                        legend.Cell().Background(AnnulledBackground).Border(0.5f).BorderColor(AnnulledForeground)
                            .Padding(4)
                            .Text(AnulledLegendText(dto.annulled_count, dto.annulled_amount))
                            .FontSize(7).FontColor(AnnulledForeground);
                    });
                }

                // TABLA 2: REGISTRO DE PAGOS
                col.Item().PaddingTop(12).PaddingBottom(4).Text("REGISTRO DE PAGOS").FontSize(10).Bold().FontColor(Accent);

                col.Item().Table(hist =>
                {
                    hist.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(3f);
                    });

                    hist.Header(header =>
                    {
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("FECHA DEL PAGO").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).AlignCenter().Text("MONTO USD").Bold().FontColor(Colors.White);
                        header.Cell().Background(Accent).Padding(3).Text("OBSERVACION").Bold().FontColor(Colors.White);
                    });

                    if (payments != null && payments.Count > 0)
                    {
                        bool alt = false;
                        foreach (var p in payments)
                        {
                            string bg = alt ? SoftAccent : Colors.White;
                            alt = !alt;
                            string color = p.es_negativo ? "#D32F2F" : "#2E7D32";
                            string monto = Math.Abs(p.amount_usd).ToString("N2");

                            hist.Cell().Background(bg).Padding(2).AlignCenter().Text(p.payment_date.ToString("dd/MM/yyyy"));
                            hist.Cell().Background(bg).Padding(2).AlignCenter().Text(monto).FontColor(color).Bold();
                            hist.Cell().Background(bg).Padding(2).Text(p.notes);
                        }

                        hist.Cell().Background(SoftAccent).Padding(2).Text("TOTAL").Bold();
                        hist.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(total_paid.ToString("N2")).Bold();
                        hist.Cell().Background(SoftAccent);
                    }
                    else
                    {
                        hist.Cell().ColumnSpan(3).Padding(8).AlignCenter().Text("No se registran pagos para esta relación.").FontColor("#777777");
                    }
                });
            });
        }
    }
}