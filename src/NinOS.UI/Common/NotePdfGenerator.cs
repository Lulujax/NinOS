using Microsoft.Win32;
using NinOS.Domain.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

namespace NinOS.UI.Common
{
    public static class NotePdfGenerator
    {
        private static string PrimaryColor = "#1B3A2D";
        private static readonly string LightBorder = "#B0B0B0";
        private static string AccentBg = "#F0F4EC";

        public static void generate(note_print_dto note)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            PrimaryColor = string.IsNullOrWhiteSpace(note.accent_color) ? "#1B3A2D" : note.accent_color;
            AccentBg = string.IsNullOrWhiteSpace(note.accent_soft_color) ? "#F0F4EC" : note.accent_soft_color;

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar nota de entrega",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"NotaEntrega_{note.note_number}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginVertical(20);
                    page.MarginHorizontal(25);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(3).Column(left =>
                            {
                                left.Item().Text(string.IsNullOrWhiteSpace(note.header_title) ? note.company_name : note.header_title).FontSize(22).Bold().FontColor(PrimaryColor);
                                left.Item().Text("Caracas - Venezuela").FontSize(10).FontColor("#555555");
                            });

                            row.RelativeItem(2).Column(right =>
                            {
                                right.Item().AlignRight().Text(note.document_label).FontSize(16).Bold().FontColor(PrimaryColor);
                                right.Item().PaddingTop(2).AlignRight().Text($"Nro: {note.note_number}").FontSize(11).Bold();
                            });
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(PrimaryColor);
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("VENDEDOR:").FontSize(8).Bold().FontColor("#555555");
                                c.Item().PaddingTop(1).Text(note.seller_name).FontSize(10).Bold();
                            });
                        });

                        col.Item().PaddingTop(6).Border(0.5f).BorderColor(LightBorder).Padding(6).Column(grid =>
                        {
                            grid.Item().Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Razon Social").FontSize(7).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_business_name).FontSize(10);
                                });
                                r.ConstantItem(80).Column(c =>
                                {
                                    c.Item().Text("Codigo").FontSize(7).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_code).FontSize(10);
                                });
                                r.ConstantItem(120).Column(c =>
                                {
                                    c.Item().Text("RIF").FontSize(7).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_rif).FontSize(10);
                                });
                            });

                            grid.Item().PaddingTop(4).LineHorizontal(0.25f).LineColor("#DDDDDD");

                            grid.Item().PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Domicilio Fiscal").FontSize(7).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.fiscal_address).FontSize(10);
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Direccion de Entrega").FontSize(7).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_delivery_address).FontSize(10);
                                });
                            });

                            grid.Item().PaddingTop(4).LineHorizontal(0.25f).LineColor("#DDDDDD");

                            grid.Item().PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Fecha Emision").FontSize(7).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.creation_date.ToString("dd/MM/yyyy")).FontSize(10);
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Fecha Vencimiento").FontSize(7).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.due_date.ToString("dd/MM/yyyy")).FontSize(10);
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Telefono").FontSize(7).Bold().FontColor("#555555");
                                    c.Item().PaddingTop(1).Text(note.customer_phone).FontSize(10);
                                });
                            });

                            if (!string.IsNullOrWhiteSpace(note.customer_contact) || !string.IsNullOrWhiteSpace(note.credit_days_text))
                            {
                                grid.Item().PaddingTop(4).LineHorizontal(0.25f).LineColor("#DDDDDD");

                                grid.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Contacto").FontSize(7).Bold().FontColor("#555555");
                                        c.Item().PaddingTop(1).Text(note.customer_contact).FontSize(10);
                                    });
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Dias de Credito").FontSize(7).Bold().FontColor("#555555");
                                        c.Item().PaddingTop(1).Text(note.credit_days_text).FontSize(10);
                                    });
                                });
                            }
                        });

                        var detail_chunks = note.details.Chunk(20).ToList();
                        for (int ci = 0; ci < detail_chunks.Count; ci++)
                        {
                            if (ci > 0) col.Item().PageBreak();

                            col.Item().PaddingTop(8).Text(ci == 0 ? "DETALLE DE PRODUCTOS" : "DETALLE DE PRODUCTOS (CONTINUACION)").FontSize(9).Bold().FontColor(PrimaryColor);

                            col.Item().PaddingTop(3).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(PrimaryColor).Padding(4).Text("CANT.").FontColor(Colors.White).Bold().FontSize(8);
                                    header.Cell().Background(PrimaryColor).Padding(4).Text("CODIGO").FontColor(Colors.White).Bold().FontSize(8);
                                    header.Cell().Background(PrimaryColor).Padding(4).Text("DESCRIPCION").FontColor(Colors.White).Bold().FontSize(8);
                                    header.Cell().Background(PrimaryColor).Padding(4).Text("PRECIO U.").FontColor(Colors.White).Bold().FontSize(8);
                                    header.Cell().Background(PrimaryColor).Padding(4).Text("PRECIO P.").FontColor(Colors.White).Bold().FontSize(8);
                                    header.Cell().Background(PrimaryColor).Padding(4).Text("SUBTOTAL").FontColor(Colors.White).Bold().FontSize(8);
                                });

                                bool alternate = false;
                                foreach (var d in detail_chunks[ci])
                                {
                                    string bg = alternate ? AccentBg : Colors.White;
                                    table.Cell().Background(bg).Padding(3).Text(d.quantity.ToString()).FontSize(9);
                                    table.Cell().Background(bg).Padding(3).Text(d.code).FontSize(9);
                                    table.Cell().Background(bg).Padding(3).Text(d.name).FontSize(9);
                                    table.Cell().Background(bg).Padding(3).Text(d.unit_price_usd.ToString("N2")).FontSize(9);
                                    table.Cell().Background(bg).Padding(3).Text(d.promo_price_usd.ToString("N2")).FontSize(9);
                                    table.Cell().Background(bg).Padding(3).Text(d.subtotal_usd.ToString("N2")).FontSize(9);
                                    alternate = !alternate;
                                }
                            });
                        }

                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem();

                            row.ConstantItem(220).Border(0.5f).BorderColor(LightBorder).Padding(6).Column(totals =>
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Subtotal:").FontSize(9);
                                    r.RelativeItem().AlignRight().Text($"{note.gross_total_usd:N2}").FontSize(9);
                                });

                                if (note.promo_discount_amount > 0)
                                {
                                    totals.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.RelativeItem().Text($"Promo ({note.promo_discount_percentage:0.##}%):").FontSize(9).FontColor("#CC0000");
                                        r.RelativeItem().AlignRight().Text($"-{note.promo_discount_amount:N2}").FontSize(9).FontColor("#CC0000");
                                    });
                                }

                                if (note.discount_amount > 0)
                                {
                                    totals.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.RelativeItem().Text($"Descuento ({note.discount_percentage:0.##}%):").FontSize(9).FontColor("#CC0000");
                                        r.RelativeItem().AlignRight().Text($"-{note.discount_amount:N2}").FontSize(9).FontColor("#CC0000");
                                    });
                                }

                                totals.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("Total General:").FontSize(10).Bold();
                                    r.RelativeItem().AlignRight().Text($"{note.discounted_total_usd:N2}").FontSize(10).Bold();
                                });

                                if (note.paid_amount_usd > 0)
                                {
                                    totals.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.RelativeItem().Text("Abonado:").FontSize(9);
                                        r.RelativeItem().AlignRight().Text($"{note.paid_amount_usd:N2}").FontSize(9).FontColor("#228B22");
                                    });
                                }

                                totals.Item().PaddingTop(2).Row(r =>
                                {
                                    r.RelativeItem().Text("Saldo:").FontSize(9).Bold();
                                    r.RelativeItem().AlignRight().Text($"{note.balance_due_usd:N2}").FontSize(9).Bold();
                                });
                            });
                        });

                        bool has_conditions = !string.IsNullOrWhiteSpace(note.conditions_text);
                        bool has_discount_conditions = !string.IsNullOrWhiteSpace(note.discount_conditions_text);

                        if (has_conditions || has_discount_conditions)
                        {
                            col.Item().PaddingTop(8).Row(row =>
                            {
                                if (has_conditions)
                                {
                                    row.RelativeItem().Border(0.5f).BorderColor(LightBorder).Padding(6).Column(cond =>
                                    {
                                        cond.Item().Text("CONDICIONES DE PAGO").FontSize(9).Bold().FontColor(PrimaryColor);
                                        cond.Item().PaddingTop(3).Text(note.conditions_text).FontSize(9);
                                    });
                                }

                                if (has_discount_conditions)
                                {
                                    if (has_conditions) row.ConstantItem(10);
                                    row.RelativeItem().Border(0.5f).BorderColor(LightBorder).Padding(6).Column(disc =>
                                    {
                                        disc.Item().Text("DESCUENTO").FontSize(9).Bold().FontColor(PrimaryColor);
                                        disc.Item().PaddingTop(3).Text(note.discount_conditions_text).FontSize(8).FontColor("#CC6600");
                                    });
                                }
                            });
                        }

                        col.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Border(0.5f).BorderColor(LightBorder).Padding(6).Column(bank =>
                            {
                                bank.Item().Text("DATOS BANCARIOS").FontSize(8).Bold().FontColor(PrimaryColor);
                                bank.Item().PaddingTop(3).Text("Transferencia:").FontSize(8).Bold();
                                bank.Item().Text("Banco: BANCO MERCANTIL").FontSize(8);
                                bank.Item().Text("Cuenta Corriente: 0105-0120-23-11200-92426").FontSize(8);
                                bank.Item().Text("Cedula: 13.046.042").FontSize(8);
                                bank.Item().PaddingTop(4).Text("Pago Móvil:").FontSize(8).Bold();
                                bank.Item().Text("Banco: BANCO MERCANTIL").FontSize(8);
                                bank.Item().Text("Telefono: 0424.496.01.02").FontSize(8);
                                bank.Item().Text("Cedula: 13.046.042").FontSize(8);
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(0.5f).BorderColor(LightBorder).Padding(6).Column(manual =>
                            {
                                manual.Item().Text("DATOS PARA PAGO").FontSize(8).Bold().FontColor(PrimaryColor);
                                manual.Item().PaddingTop(8).Row(r =>
                                {
                                    r.RelativeItem().Text("Fecha de pago:").FontSize(8);
                                    r.RelativeItem().Text("____________________").FontSize(8);
                                });
                                manual.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text("Monto Bs:").FontSize(8);
                                    r.RelativeItem().Text("____________________").FontSize(8);
                                });
                                manual.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text("Nro Referencia:").FontSize(8);
                                    r.RelativeItem().Text("____________________").FontSize(8);
                                });
                                manual.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text("Banco:").FontSize(8);
                                    r.RelativeItem().Text("____________________").FontSize(8);
                                });
                            });
                        });

                        col.Item().PaddingTop(10).Border(0.5f).BorderColor(LightBorder).Padding(10).Column(signature =>
                        {
                            signature.Item().Text("FECHA  /  FIRMA Y SELLO DEL CLIENTE").FontSize(9).Bold().FontColor(PrimaryColor).AlignCenter();
                            signature.Item().PaddingTop(40).LineHorizontal(0.5f).LineColor(LightBorder);
                        });

                        if (note.volume_discount_amount > 0)
                        {
                            col.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem();

                                row.ConstantItem(240).Border(0.5f).BorderColor(LightBorder).Padding(6).Column(volume =>
                                {
                                    volume.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text($"Descuento por volumen ({note.volume_discount_percentage:0.##}%):").FontSize(9).FontColor("#CC0000");
                                        r.RelativeItem().AlignRight().Text($"-{note.volume_discount_amount:N2}").FontSize(9).FontColor("#CC0000");
                                    });

                                    volume.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(LightBorder);

                                    volume.Item().PaddingTop(3).Row(r =>
                                    {
                                        r.RelativeItem().Text("TOTAL A PAGAR:").FontSize(11).Bold();
                                        r.RelativeItem().AlignRight().Text($"{note.total_amount_usd:N2}").FontSize(11).Bold().FontColor(PrimaryColor);
                                    });
                                });
                            });
                        }
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(LightBorder);
                        col.Item().PaddingTop(3).Row(row =>
                        {
                            row.RelativeItem().Text($"Nota: {note.note_number}").FontSize(7).FontColor("#888888");
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
