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
    public static class ProVentaDetailPdfGenerator
    {
        private static readonly string Accent = "#1565C0";
        private static readonly string SoftAccent = "#E3F2FD";

        public static void generate(pro_venta_relation_row row, List<pro_venta_weekly_row> notes, List<payment_dto> payments)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar detalle de relacion",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Detalle_Relacion_{row.relation_number}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            decimal total_notes = notes.Sum(n => n.amount);
            decimal total_paid = payments.Sum(p => p.amount_usd);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(1, Unit.Centimetre);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginRight(1, Unit.Centimetre);
                    page.MarginBottom(1, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

                    page.Content().Column(col =>
                    {
                        col.Item().Text("DETALLE DE LA RELACION").FontSize(16).Bold().FontColor(Accent).AlignCenter();
                        col.Item().Text($"RELACION NRO {row.relation_number} ({row.week_start:dd/MM} AL {row.week_end:dd/MM})").FontSize(10).Bold().FontColor("#666666").AlignCenter();
                        col.Item().PaddingTop(4).PaddingBottom(6).LineHorizontal(1.5f).LineColor(Accent);

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
                            foreach (var n in notes)
                            {
                                string bg = alternate ? SoftAccent : Colors.White;
                                alternate = !alternate;

                                table.Cell().Background(bg).Padding(2).AlignCenter().Text(n.note_number);
                                table.Cell().Background(bg).Padding(2).Text(n.customer_name);
                                table.Cell().Background(bg).Padding(2).AlignCenter().Text(n.amount.ToString("N2"));
                            }

                            if (notes.Count > 0)
                            {
                                table.Cell().Background(SoftAccent).Padding(2).Text("TOTAL").Bold();
                                table.Cell().Background(SoftAccent);
                                table.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(total_notes.ToString("N2")).Bold();
                            }
                        });

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

                            bool alt = false;
                            foreach (var p in payments)
                            {
                                string bg = alt ? SoftAccent : Colors.White;
                                alt = !alt;

                                hist.Cell().Background(bg).Padding(2).AlignCenter().Text(p.payment_date.ToString("dd/MM/yyyy"));
                                hist.Cell().Background(bg).Padding(2).AlignCenter().Text(p.amount_usd.ToString("N2"));
                                hist.Cell().Background(bg).Padding(2).Text(p.notes);
                            }

                            if (payments.Count > 0)
                            {
                                hist.Cell().Background(SoftAccent).Padding(2).Text("TOTAL").Bold();
                                hist.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(total_paid.ToString("N2")).Bold();
                                hist.Cell().Background(SoftAccent);
                            }
                        });
                    });
                });
            });

            document.GeneratePdf(save_dialog.FileName);
        }
    }
}