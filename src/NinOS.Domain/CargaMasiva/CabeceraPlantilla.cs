using System;

namespace NinOS.Domain.CargaMasiva
{
    public sealed class CabeceraPlantilla
    {
        public const int VersionActual = 1;

        public TipoPlantilla tipo { get; }
        public int? id_price_list { get; }
        public string nombre_lista { get; }
        public bool es_principal { get; }
        public int version_formato { get; }
        public DateTime fecha_descarga { get; }

        public CabeceraPlantilla(
            TipoPlantilla tipo,
            int? id_price_list,
            string nombre_lista,
            bool es_principal,
            DateTime fecha_descarga,
            int version_formato = VersionActual)
        {
            if (version_formato <= 0) throw new ArgumentOutOfRangeException(nameof(version_formato), "La versión de la plantilla no es válida.");

            if (tipo == TipoPlantilla.Cantidad)
            {
                if (id_price_list != null) throw new ArgumentException("La plantilla de cantidad no lleva lista de precios.", nameof(id_price_list));
            }
            else
            {
                if (id_price_list == null || id_price_list <= 0) throw new ArgumentException("La plantilla de precio necesita una lista.", nameof(id_price_list));
                if (string.IsNullOrWhiteSpace(nombre_lista)) throw new ArgumentException("La plantilla de precio necesita el nombre de la lista.", nameof(nombre_lista));
            }

            this.tipo = tipo;
            this.id_price_list = id_price_list;
            this.nombre_lista = nombre_lista?.Trim() ?? string.Empty;
            this.es_principal = es_principal;
            this.version_formato = version_formato;
            this.fecha_descarga = fecha_descarga;
        }
    }
}
