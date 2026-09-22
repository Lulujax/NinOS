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

        public static void generate(pro_venta_weekly_dto dto, decimal nota_por_pagar)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var save_dialog = new SaveFileDialog
            {
                Title = "Guardar relacion semanal",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Relacion_{dto.relation_number}_Semana_{dto.week_start:ddMMyy}-{dto.week_end:ddMMyy}.pdf"
            };

            if (save_dialog.ShowDialog() != true) return;

            decimal total_cobrado = dto.total_amount - dto.total_commission_luis;
            decimal diferencial = total_cobrado - nota_por_pagar;

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
                        col.Item().Text($"RELACION NRO {dto.relation_number}").FontSize(16).Bold().FontColor(Accent).AlignCenter();
                        col.Item().Text($"{dto.week_start:dd/MM} AL {dto.week_end:dd/MM} _ FACTURAS POR COBRAR {dto.city}").FontSize(10).Bold().FontColor("#666666").AlignCenter();
                        col.Item().PaddingTop(4).PaddingBottom(4).LineHorizontal(1.5f).LineColor(Accent);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.4f);
                                columns.RelativeColumn(3f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.6f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Accent).Padding(3).Text("NRO NOTA CLIENTE").Bold().FontColor(Colors.White);
                                header.Cell().Background(Accent).Padding(3).Text("CLIENTE").Bold().FontColor(Colors.White);
                                header.Cell().Background(Accent).Padding(3).AlignCenter().Text("MONTO NOTA").Bold().FontColor(Colors.White);
                                header.Cell().Background(Accent).Padding(3).AlignCenter().Text("COMISION LUIS 10%").Bold().FontColor(Colors.White);
                                header.Cell().Background(Accent).Padding(3).AlignCenter().Text("GASTOS OPERATIVOS 25%").Bold().FontColor(Colors.White);
                                header.Cell().Background(Accent).Padding(3).AlignCenter().Text("GASTOS ADMINISTRATIVOS 15%").Bold().FontColor(Colors.White);
                            });

                            bool alternate = false;
                            foreach (var r in dto.rows)
                            {
                                string bg = alternate ? SoftAccent : Colors.White;
                                alternate = !alternate;

                                table.Cell().Background(bg).Padding(2).Text(r.note_number);
                                table.Cell().Background(bg).Padding(2).Text(r.customer_name);
                                table.Cell().Background(bg).Padding(2).AlignCenter().Text(r.amount.ToString("N2"));
                                table.Cell().Background(bg).Padding(2).AlignCenter().Text(r.commission_luis.ToString("N2"));
                                table.Cell().Background(bg).Padding(2).AlignCenter().Text(r.gastos_25.ToString("N2"));
                                table.Cell().Background(bg).Padding(2).AlignCenter().Text(r.gastos_15.ToString("N2"));
                            }
                        });

                        if (dto.rows.Count > 0)
                        {
                            col.Item().PaddingTop(4).Table(totals =>
                            {
                                totals.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.4f);
                                    columns.RelativeColumn(3f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.6f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                });

                                totals.Cell().Background(SoftAccent).Padding(2).Text("TOTAL").Bold();
                                totals.Cell().Background(SoftAccent);
                                totals.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(dto.total_amount.ToString("N2")).Bold();
                                totals.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(dto.total_commission_luis.ToString("N2")).Bold();
                                totals.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(dto.total_gastos_25.ToString("N2")).Bold();
                                totals.Cell().Background(SoftAccent).Padding(2).AlignCenter().Text(dto.total_gastos_15.ToString("N2")).Bold();
                            });
                        }

                        col.Item().PaddingTop(10).Text("LIQUIDACION").FontSize(9).Bold().FontColor(Accent);

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
                });
            });

            document.GeneratePdf(save_dialog.FileName);
        }
    }
}