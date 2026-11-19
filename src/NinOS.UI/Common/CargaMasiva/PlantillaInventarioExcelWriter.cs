using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using NinOS.Domain.CargaMasiva;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common.CargaMasiva
{
    public static class PlantillaInventarioExcelWriter
    {
        public static void generar(CabeceraPlantilla cabecera, IReadOnlyList<ProductoPlantilla> productos, string ruta)
        {
            if (cabecera == null) throw new ArgumentNullException(nameof(cabecera));
            if (productos == null) throw new ArgumentNullException(nameof(productos));
            if (string.IsNullOrWhiteSpace(ruta)) throw new ArgumentException("La ruta de la plantilla está vacía.", nameof(ruta));

            AppLog.Info("Generando plantilla de inventario. tipo=" + cabecera.tipo + ", productos=" + productos.Count);

            using var libro = new XLWorkbook();
            IXLWorksheet hoja = libro.Worksheets.Add(FormatoPlantilla.NombreHoja);

            IReadOnlyList<string> encabezados = FormatoPlantilla.encabezados(cabecera);
            escribir_titulos(hoja, cabecera, encabezados.Count);
            escribir_encabezados(hoja, encabezados);
            escribir_productos(hoja, cabecera, productos, encabezados);
            proteger(hoja, cabecera, productos.Count, encabezados.Count);
            escribir_hoja_oculta(libro, cabecera);

            libro.SaveAs(ruta);
            AppLog.Info("Plantilla de inventario guardada. filas=" + productos.Count);
        }

        private static void escribir_titulos(IXLWorksheet hoja, CabeceraPlantilla cabecera, int cantidad_columnas)
        {
            escribir_banda(hoja, FormatoPlantilla.FilaTitulo, cantidad_columnas, "NINOS - PLANTILLA DE INVENTARIO", 12, true);
            escribir_banda(hoja, FormatoPlantilla.FilaSubtitulo, cantidad_columnas, FormatoPlantilla.texto_titulo(cabecera), 10, true);
            escribir_banda(hoja, FormatoPlantilla.FilaInstruccion, cantidad_columnas, FormatoPlantilla.texto_instruccion(cabecera), 9, false);
        }

        private static void escribir_banda(IXLWorksheet hoja, int fila, int cantidad_columnas, string texto, double tamano, bool negrita)
        {
            IXLRange rango = hoja.Range(fila, 1, fila, cantidad_columnas);
            rango.Merge();
            rango.Value = texto;
            rango.Style.Font.Bold = negrita;
            rango.Style.Font.FontSize = tamano;
            rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void escribir_encabezados(IXLWorksheet hoja, IReadOnlyList<string> encabezados)
        {
            for (int indice = 0; indice < encabezados.Count; indice++)
            {
                IXLCell celda = hoja.Cell(FormatoPlantilla.FilaEncabezado, indice + 1);
                celda.Value = encabezados[indice];
                celda.Style.Font.Bold = true;
                celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D62B5");
                celda.Style.Font.FontColor = XLColor.White;
                celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        private static void escribir_productos(
            IXLWorksheet hoja,
            CabeceraPlantilla cabecera,
            IReadOnlyList<ProductoPlantilla> productos,
            IReadOnlyList<string> encabezados)
        {
            int columna_referencia = encabezados.ToList().IndexOf(FormatoPlantilla.encabezado_de_referencia(cabecera)) + 1;
            string formato = cabecera.tipo == TipoPlantilla.Cantidad ? "0" : "0.00";

            for (int indice = 0; indice < productos.Count; indice++)
            {
                ProductoPlantilla producto = productos[indice];
                int fila = FormatoPlantilla.PrimeraFilaDatos + indice;

                hoja.Cell(fila, 1).Value = producto.id_product;
                hoja.Cell(fila, 2).Value = producto.codigo;
                hoja.Cell(fila, 3).Value = producto.marca;
                hoja.Cell(fila, 4).Value = producto.descripcion;

                double referencia = (double)valor_de_referencia(cabecera, producto);
                hoja.Cell(fila, columna_referencia).Value = referencia;
                hoja.Cell(fila, columna_referencia).Style.NumberFormat.Format = formato;
            }

            int columna_editable = encabezados.Count;
            if (productos.Count > 0)
            {
                IXLRange editable = hoja.Range(
                    FormatoPlantilla.PrimeraFilaDatos, columna_editable,
                    FormatoPlantilla.PrimeraFilaDatos + productos.Count - 1, columna_editable);
                editable.Style.NumberFormat.Format = formato;
                editable.Style.Protection.SetLocked(false);
                editable.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF8DC");
            }
        }

        private static decimal valor_de_referencia(CabeceraPlantilla cabecera, ProductoPlantilla producto)
        {
            if (cabecera.tipo == TipoPlantilla.Cantidad) return producto.stock_quantity;
            return cabecera.es_principal ? producto.precio_principal : producto.precio_lista;
        }

        private static void proteger(IXLWorksheet hoja, CabeceraPlantilla cabecera, int cantidad_productos, int cantidad_columnas)
        {
            int ultima_fila = FormatoPlantilla.PrimeraFilaDatos + Math.Max(cantidad_productos, 1) - 1;
            hoja.Range(FormatoPlantilla.FilaEncabezado, 1, ultima_fila, cantidad_columnas).SetAutoFilter();
            hoja.SheetView.FreezeRows(FormatoPlantilla.FilaEncabezado);

            for (int columna = 1; columna <= cantidad_columnas; columna++)
            {
                hoja.Column(columna).Width = columna == 4 ? 42 : 16;
            }

            hoja.Protect()
                .AllowElement(XLSheetProtectionElements.AutoFilter);
        }

        private static void escribir_hoja_oculta(XLWorkbook libro, CabeceraPlantilla cabecera)
        {
            IXLWorksheet oculta = libro.Worksheets.Add(FormatoPlantilla.NombreHojaOculta);
            oculta.Visibility = XLWorksheetVisibility.VeryHidden;

            var pares = new List<KeyValuePair<string, string>>
            {
                new(FormatoPlantilla.ClaveMarca, FormatoPlantilla.MarcaArchivo),
                new(FormatoPlantilla.ClaveVersion, cabecera.version_formato.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new(FormatoPlantilla.ClaveTipo, cabecera.tipo == TipoPlantilla.Cantidad ? "cantidad" : "precio"),
                new(FormatoPlantilla.ClaveIdLista, cabecera.id_price_list?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty),
                new(FormatoPlantilla.ClaveNombreLista, cabecera.nombre_lista),
                new(FormatoPlantilla.ClaveEsPrincipal, cabecera.es_principal ? "si" : "no"),
                new(FormatoPlantilla.ClaveFecha, cabecera.fecha_descarga.ToString("o", System.Globalization.CultureInfo.InvariantCulture))
            };

            for (int indice = 0; indice < pares.Count; indice++)
            {
                oculta.Cell(indice + 1, 1).Value = pares[indice].Key;
                oculta.Cell(indice + 1, 2).Value = pares[indice].Value;
            }
        }
    }
}
