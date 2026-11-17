using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common;
using QuestPDF.Fluent;
using Xunit;

namespace NinOS.Tests.Documentos
{
    public class VesuprontaNotaPdfTests
    {
        private const double ANCHO_CARTA_PUNTOS = 612;
        private const double ALTO_CARTA_PUNTOS = 792;
        private const double TOLERANCIA_PUNTOS = 0.5;

        private static note_print_dto crear_nota(bool es_vesupronta, int cantidad_renglones)
        {
            List<note_detail_print_dto> renglones = Enumerable.Range(1, cantidad_renglones)
                .Select(i => new note_detail_print_dto
                {
                    code = $"P{i:000}",
                    name = $"Producto de prueba {i}",
                    quantity = 2,
                    unit_price_usd = 1.50m,
                    promo_price_usd = 1.50m,
                    subtotal_usd = 3.00m
                })
                .ToList();

            decimal total = renglones.Sum(r => r.subtotal_usd);

            return new note_print_dto
            {
                note_number = "000_010",
                company_name = NinOS.Domain.BrandHeader.Title,
                header_title = NinOS.Domain.BrandHeader.Title,
                document_label = "NOTA DE ENTREGA",
                is_vesupronta = es_vesupronta,
                creation_date = new DateTime(2026, 10, 5, 10, 0, 0),
                due_date = new DateTime(2026, 11, 4),
                seller_name = "Sandra",
                customer_code = "00303",
                customer_business_name = "PRUEBA VESUPRONTA",
                customer_rif = "J-00000000-0",
                gross_total_usd = total,
                discounted_total_usd = total,
                total_amount_usd = total,
                credit_days_text = "30 DIAS",
                details = renglones
            };
        }

        private static List<(double ancho, double alto)> medir_paginas(byte[] pdf)
        {
            string contenido = Encoding.Latin1.GetString(pdf);
            MatchCollection cajas = Regex.Matches(contenido, @"/MediaBox\s*\[\s*0\s+0\s+([\d.]+)\s+([\d.]+)\s*\]");

            return cajas
                .Select(caja => (
                    double.Parse(caja.Groups[1].Value, CultureInfo.InvariantCulture),
                    double.Parse(caja.Groups[2].Value, CultureInfo.InvariantCulture)))
                .ToList();
        }

        private static void verificar_paginas_carta(byte[] pdf, int paginas_esperadas, string caso)
        {
            List<(double ancho, double alto)> paginas = medir_paginas(pdf);

            Assert.True(paginas.Count == paginas_esperadas,
                $"[{caso}] se esperaban {paginas_esperadas} paginas y el PDF trae {paginas.Count}.");

            for (int i = 0; i < paginas.Count; i++)
            {
                (double ancho, double alto) = paginas[i];
                bool es_carta = Math.Abs(ancho - ANCHO_CARTA_PUNTOS) < TOLERANCIA_PUNTOS
                             && Math.Abs(alto - ALTO_CARTA_PUNTOS) < TOLERANCIA_PUNTOS;
                Assert.True(es_carta,
                    $"[{caso}] la pagina {i + 1} mide {ancho} x {alto} puntos; carta es {ANCHO_CARTA_PUNTOS} x {ALTO_CARTA_PUNTOS}.");
            }
        }

        [Fact]
        public void feliz_nota_vesupronta_imprime_original_copia_y_administracion_en_carta()
        {
            byte[] pdf = NotePdfGenerator.build_document(crear_nota(true, 5)).GeneratePdf();

            verificar_paginas_carta(pdf, 3, "vesupronta con 5 productos");
        }

        [Fact]
        public void limite_nota_vesupronta_con_20_productos_cabe_en_una_hoja_por_copia()
        {
            byte[] pdf = NotePdfGenerator.build_document(crear_nota(true, 20)).GeneratePdf();

            verificar_paginas_carta(pdf, 3, "vesupronta con 20 productos");
        }

        [Fact]
        public void feliz_nota_general_no_lleva_hoja_de_administracion()
        {
            byte[] pdf = NotePdfGenerator.build_document(crear_nota(false, 20)).GeneratePdf();

            verificar_paginas_carta(pdf, 2, "general con 20 productos");
        }
    }
}
