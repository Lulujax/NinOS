using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using NinOS.Domain.CargaMasiva;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common.CargaMasiva
{
    public static class PlantillaInventarioExcelReader
    {
        public const string MensajeNoEsPlantilla = "Este archivo no es una plantilla de NinOS. Descargue una plantilla nueva.";
        public const string MensajeAbierto = "No se pudo abrir el archivo. Si está abierto en Excel, ciérrelo e intente de nuevo.";
        public const string MensajeDanado = "El archivo está dañado o no es un Excel válido.";

        public static LecturaPlantilla leer(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) throw new ArgumentException("La ruta del archivo está vacía.", nameof(ruta));
            if (!File.Exists(ruta))
            {
                AppLog.Warn("Plantilla no encontrada al cargar.");
                throw new PlantillaInvalidaException("El archivo no existe.");
            }

            AppLog.Info("Leyendo plantilla de inventario.");
            using XLWorkbook libro = abrir(ruta);

            LecturaPlantilla lectura = leer_libro(libro);
            AppLog.Info("Plantilla leída. tipo=" + lectura.cabecera.tipo + ", filas=" + lectura.filas.Count);
            return lectura;
        }

        private static XLWorkbook abrir(string ruta)
        {
            try
            {
                byte[] contenido = File.ReadAllBytes(ruta);
                return new XLWorkbook(new MemoryStream(contenido));
            }
            catch (IOException ex)
            {
                AppLog.Warn("Plantilla bloqueada o en uso al cargar: " + ex.GetType().Name);
                throw new PlantillaInvalidaException(MensajeAbierto);
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLog.Error("Sin permisos para leer la plantilla", ex);
                throw new PlantillaInvalidaException("No hay permisos para leer el archivo.");
            }
            catch (Exception ex)
            {
                AppLog.Error("Plantilla dañada o no válida", ex);
                throw new PlantillaInvalidaException(MensajeDanado);
            }
        }

        private static LecturaPlantilla leer_libro(XLWorkbook libro)
        {
            if (!libro.Worksheets.TryGetWorksheet(FormatoPlantilla.NombreHojaOculta, out IXLWorksheet? oculta))
            {
                AppLog.Warn("Plantilla sin hoja de identificación.");
                throw new PlantillaInvalidaException(MensajeNoEsPlantilla);
            }

            CabeceraPlantilla cabecera = leer_cabecera(oculta);

            if (!libro.Worksheets.TryGetWorksheet(FormatoPlantilla.NombreHoja, out IXLWorksheet? hoja))
            {
                throw new PlantillaInvalidaException(MensajeDanado);
            }

            Dictionary<string, int> columnas = mapear_columnas(hoja, cabecera);
            IReadOnlyList<FilaPlantilla> filas = leer_filas(hoja, cabecera, columnas);

            return new LecturaPlantilla(cabecera, filas);
        }

        private static CabeceraPlantilla leer_cabecera(IXLWorksheet oculta)
        {
            Dictionary<string, string> claves = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int fila = 1; fila <= 20; fila++)
            {
                IXLCell clave = oculta.Cell(fila, 1);
                if (clave.IsEmpty()) continue;
                claves[clave.GetString().Trim()] = oculta.Cell(fila, 2).GetString().Trim();
            }

            if (!claves.TryGetValue(FormatoPlantilla.ClaveMarca, out string? marca) || marca != FormatoPlantilla.MarcaArchivo)
            {
                throw new PlantillaInvalidaException(MensajeNoEsPlantilla);
            }

            if (!int.TryParse(valor(claves, FormatoPlantilla.ClaveVersion), NumberStyles.Integer, CultureInfo.InvariantCulture, out int version)
                || version <= 0 || version > CabeceraPlantilla.VersionActual)
            {
                AppLog.Warn("Plantilla con versión de formato no compatible.");
                throw new PlantillaInvalidaException(MensajeNoEsPlantilla);
            }

            try
            {
                return construir_cabecera(claves, version);
            }
            catch (ArgumentException ex)
            {
                AppLog.Error("Cabecera de plantilla inválida", ex);
                throw new PlantillaInvalidaException(MensajeDanado);
            }
        }

        private static CabeceraPlantilla construir_cabecera(Dictionary<string, string> claves, int version)
        {
            string tipo_texto = valor(claves, FormatoPlantilla.ClaveTipo);
            if (tipo_texto != "cantidad" && tipo_texto != "precio")
            {
                throw new PlantillaInvalidaException(MensajeNoEsPlantilla);
            }

            TipoPlantilla tipo = tipo_texto == "cantidad" ? TipoPlantilla.Cantidad : TipoPlantilla.Precio;
            int? id_lista = int.TryParse(valor(claves, FormatoPlantilla.ClaveIdLista), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                ? id
                : null;
            bool es_principal = valor(claves, FormatoPlantilla.ClaveEsPrincipal) == "si";
            DateTime fecha = DateTime.TryParse(valor(claves, FormatoPlantilla.ClaveFecha), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime fecha_leida)
                ? fecha_leida
                : DateTime.UtcNow;

            return new CabeceraPlantilla(
                tipo,
                tipo == TipoPlantilla.Cantidad ? null : id_lista,
                valor(claves, FormatoPlantilla.ClaveNombreLista),
                es_principal,
                fecha,
                version);
        }

        private static string valor(Dictionary<string, string> claves, string clave)
        {
            return claves.TryGetValue(clave, out string? valor_leido) ? valor_leido : string.Empty;
        }

        private static Dictionary<string, int> mapear_columnas(IXLWorksheet hoja, CabeceraPlantilla cabecera)
        {
            int fila_encabezado = buscar_fila_encabezado(hoja);
            Dictionary<string, int> columnas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            IXLRow fila = hoja.Row(fila_encabezado);
            foreach (IXLCell celda in fila.CellsUsed())
            {
                columnas[celda.GetString().Trim()] = celda.Address.ColumnNumber;
            }

            string[] requeridas =
            {
                FormatoPlantilla.EncabezadoId,
                FormatoPlantilla.EncabezadoCodigo,
                FormatoPlantilla.encabezado_de_referencia(cabecera),
                FormatoPlantilla.encabezado_editable(cabecera)
            };

            foreach (string requerida in requeridas)
            {
                if (!columnas.ContainsKey(requerida))
                {
                    AppLog.Warn("Plantilla sin la columna requerida: " + requerida);
                    throw new PlantillaInvalidaException(MensajeNoEsPlantilla);
                }
            }

            return columnas;
        }

        private static int buscar_fila_encabezado(IXLWorksheet hoja)
        {
            for (int fila = 1; fila <= 10; fila++)
            {
                IXLCell celda = hoja.Cell(fila, 1);
                if (!celda.IsEmpty() && celda.GetString().Trim().Equals(FormatoPlantilla.EncabezadoId, StringComparison.OrdinalIgnoreCase))
                {
                    return fila;
                }
            }

            throw new PlantillaInvalidaException(MensajeNoEsPlantilla);
        }

        private static IReadOnlyList<FilaPlantilla> leer_filas(
            IXLWorksheet hoja,
            CabeceraPlantilla cabecera,
            Dictionary<string, int> columnas)
        {
            var filas = new List<FilaPlantilla>();
            int ultima_fila = hoja.LastRowUsed()?.RowNumber() ?? 0;
            int fila_encabezado = buscar_fila_encabezado(hoja);

            for (int numero = fila_encabezado + 1; numero <= ultima_fila; numero++)
            {
                filas.Add(leer_fila(hoja, numero, cabecera, columnas));
            }

            return filas;
        }

        private static FilaPlantilla leer_fila(
            IXLWorksheet hoja,
            int numero_fila,
            CabeceraPlantilla cabecera,
            Dictionary<string, int> columnas)
        {
            int? id = leer_entero(hoja.Cell(numero_fila, columnas[FormatoPlantilla.EncabezadoId]));
            string? codigo = leer_texto(hoja.Cell(numero_fila, columnas[FormatoPlantilla.EncabezadoCodigo]));
            string? descripcion = columnas.TryGetValue(FormatoPlantilla.EncabezadoDescripcion, out int columna_descripcion)
                ? leer_texto(hoja.Cell(numero_fila, columna_descripcion))
                : null;
            decimal? referencia = leer_numero(hoja.Cell(numero_fila, columnas[FormatoPlantilla.encabezado_de_referencia(cabecera)]));

            IXLCell celda_editable = hoja.Cell(numero_fila, columnas[FormatoPlantilla.encabezado_editable(cabecera)]);
            decimal? numero_escrito = leer_numero(celda_editable);
            string? texto_escrito = numero_escrito == null ? leer_texto(celda_editable) : null;

            return new FilaPlantilla(numero_fila, id, codigo, referencia, numero_escrito, texto_escrito, descripcion);
        }

        private static decimal? leer_numero(IXLCell celda)
        {
            if (celda.IsEmpty()) return null;
            if (!celda.Value.IsNumber) return null;
            return Convert.ToDecimal(celda.Value.GetNumber(), CultureInfo.InvariantCulture);
        }

        private static int? leer_entero(IXLCell celda)
        {
            decimal? numero = leer_numero(celda);
            if (numero == null || numero.Value <= 0 || numero.Value != decimal.Truncate(numero.Value)) return null;
            if (numero.Value > int.MaxValue) return null;
            return (int)numero.Value;
        }

        private static string? leer_texto(IXLCell celda)
        {
            if (celda.IsEmpty()) return null;
            string texto = celda.GetString().Trim();
            return texto.Length == 0 ? null : texto;
        }
    }
}
