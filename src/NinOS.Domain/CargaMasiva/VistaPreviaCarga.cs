using System;
using System.Collections.Generic;
using System.Linq;

namespace NinOS.Domain.CargaMasiva
{
    public sealed class VistaPreviaCarga
    {
        public CabeceraPlantilla cabecera { get; }
        public TipoCargaCantidad? tipo_carga { get; }
        public IReadOnlyList<ResultadoFila> resultados { get; }
        public int productos_en_otras_listas { get; }

        public VistaPreviaCarga(
            CabeceraPlantilla cabecera,
            TipoCargaCantidad? tipo_carga,
            IReadOnlyList<ResultadoFila> resultados,
            int productos_en_otras_listas)
        {
            this.cabecera = cabecera ?? throw new ArgumentNullException(nameof(cabecera));
            this.resultados = resultados ?? throw new ArgumentNullException(nameof(resultados));
            if (cabecera.tipo == TipoPlantilla.Cantidad && tipo_carga == null)
            {
                throw new ArgumentException("La carga de cantidad necesita el tipo Conteo o Ajuste.", nameof(tipo_carga));
            }
            if (productos_en_otras_listas < 0) throw new ArgumentOutOfRangeException(nameof(productos_en_otras_listas));

            this.tipo_carga = tipo_carga;
            this.productos_en_otras_listas = productos_en_otras_listas;
        }

        public int cambian => contar(EstadoFila.Cambia);
        public int avisos => contar(EstadoFila.Aviso);
        public int iguales => contar(EstadoFila.Igual);
        public int errores => contar(EstadoFila.Error);

        public bool puede_aplicar => errores == 0 && cambian + avisos > 0;

        private int contar(EstadoFila estado)
        {
            return resultados.Count(r => r.estado == estado);
        }
    }
}
