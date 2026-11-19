using System.Collections.Generic;
using NinOS.Domain.CargaMasiva;

namespace NinOS.UI.Common.CargaMasiva
{
    public static class FormatoPlantilla
    {
        public const string NombreHoja = "Plantilla";
        public const string NombreHojaOculta = "_ninos";
        public const string MarcaArchivo = "NINOS-PLANTILLA-INVENTARIO";

        public const string ClaveMarca = "marca";
        public const string ClaveVersion = "version";
        public const string ClaveTipo = "tipo";
        public const string ClaveIdLista = "id_lista";
        public const string ClaveNombreLista = "lista";
        public const string ClaveEsPrincipal = "principal";
        public const string ClaveFecha = "fecha";

        public const int FilaTitulo = 1;
        public const int FilaSubtitulo = 2;
        public const int FilaInstruccion = 3;
        public const int FilaEncabezado = 4;
        public const int PrimeraFilaDatos = 5;

        public const string EncabezadoId = "ID";
        public const string EncabezadoCodigo = "CÓDIGO";
        public const string EncabezadoMarca = "MARCA";
        public const string EncabezadoDescripcion = "DESCRIPCIÓN";
        public const string EncabezadoStock = "STOCK EN SISTEMA";
        public const string EncabezadoCantidad = "CANTIDAD";
        public const string EncabezadoPrecioActual = "PRECIO ACTUAL";
        public const string EncabezadoPrecioPrincipal = "PRECIO PRINCIPAL";
        public const string EncabezadoPrecioEnEstaLista = "PRECIO EN ESTA LISTA";
        public const string EncabezadoPrecioNuevo = "PRECIO NUEVO";

        public static IReadOnlyList<string> encabezados(CabeceraPlantilla cabecera)
        {
            if (cabecera.tipo == TipoPlantilla.Cantidad)
            {
                return new[] { EncabezadoId, EncabezadoCodigo, EncabezadoMarca, EncabezadoDescripcion, EncabezadoStock, EncabezadoCantidad };
            }

            if (cabecera.es_principal)
            {
                return new[] { EncabezadoId, EncabezadoCodigo, EncabezadoMarca, EncabezadoDescripcion, EncabezadoPrecioActual, EncabezadoPrecioNuevo };
            }

            return new[]
            {
                EncabezadoId, EncabezadoCodigo, EncabezadoMarca, EncabezadoDescripcion,
                EncabezadoPrecioPrincipal, EncabezadoPrecioEnEstaLista, EncabezadoPrecioNuevo
            };
        }

        public static string encabezado_de_referencia(CabeceraPlantilla cabecera)
        {
            if (cabecera.tipo == TipoPlantilla.Cantidad) return EncabezadoStock;
            return cabecera.es_principal ? EncabezadoPrecioActual : EncabezadoPrecioEnEstaLista;
        }

        public static string encabezado_editable(CabeceraPlantilla cabecera)
        {
            return cabecera.tipo == TipoPlantilla.Cantidad ? EncabezadoCantidad : EncabezadoPrecioNuevo;
        }

        public static string texto_titulo(CabeceraPlantilla cabecera)
        {
            if (cabecera.tipo == TipoPlantilla.Cantidad) return "CAMBIO DE CANTIDAD";
            return cabecera.es_principal
                ? "CAMBIO DE PRECIO - LISTA PRINCIPAL"
                : "CAMBIO DE PRECIO - LISTA " + cabecera.nombre_lista.ToUpperInvariant();
        }

        public static string texto_instruccion(CabeceraPlantilla cabecera)
        {
            return "Escriba solo en la columna " + encabezado_editable(cabecera)
                + ". Las filas vacías no se modifican.";
        }
    }
}
