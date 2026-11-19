using System;
using System.Globalization;
using NinOS.Domain.CargaMasiva;

namespace NinOS.UI.Common.CargaMasiva
{
    public sealed class FilaVistaPrevia
    {
        private const string Vacio = "—";

        public FilaVistaPrevia(
            string numero_fila,
            string codigo,
            string descripcion,
            string archivo,
            string actual,
            string nuevo,
            string cambio,
            string estado,
            string motivo)
        {
            this.numero_fila = numero_fila;
            this.codigo = codigo;
            this.descripcion = descripcion;
            this.archivo = archivo;
            this.actual = actual;
            this.nuevo = nuevo;
            this.cambio = cambio;
            this.estado = estado;
            this.motivo = motivo;
        }

        public string numero_fila { get; }
        public string codigo { get; }
        public string descripcion { get; }
        public string archivo { get; }
        public string actual { get; }
        public string nuevo { get; }
        public string cambio { get; }
        public string estado { get; }
        public string motivo { get; }

        public static FilaVistaPrevia desde(ResultadoFila resultado, TipoPlantilla tipo)
        {
            if (resultado == null) throw new ArgumentNullException(nameof(resultado));

            return new FilaVistaPrevia(
                resultado.numero_fila.ToString(CultureInfo.InvariantCulture),
                resultado.codigo ?? Vacio,
                resultado.descripcion ?? string.Empty,
                formatear_valor(resultado.valor_archivo, tipo),
                formatear_valor(resultado.valor_actual, tipo),
                formatear_valor(resultado.valor_final, tipo),
                formatear_cambio(resultado, tipo),
                texto_estado(resultado.estado),
                resultado.motivo ?? string.Empty);
        }

        public static string texto_estado(EstadoFila estado)
        {
            return estado switch
            {
                EstadoFila.Cambia => "Cambia",
                EstadoFila.Igual => "Sin cambio",
                EstadoFila.Aviso => "Revisar",
                EstadoFila.Error => "Error",
                _ => throw new ArgumentOutOfRangeException(nameof(estado))
            };
        }

        public static string formatear_valor(decimal? valor, TipoPlantilla tipo)
        {
            if (valor == null) return Vacio;
            return tipo == TipoPlantilla.Cantidad
                ? decimal.Truncate(valor.Value).ToString("0", CultureInfo.InvariantCulture)
                : valor.Value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static string formatear_cambio(ResultadoFila resultado, TipoPlantilla tipo)
        {
            decimal? diferencia = resultado.diferencia;
            if (diferencia == null) return Vacio;

            if (tipo == TipoPlantilla.Precio && resultado.porcentaje_cambio.HasValue)
            {
                return Math.Round(resultado.porcentaje_cambio.Value, 1).ToString("+0.#;-0.#;0", CultureInfo.InvariantCulture) + "%";
            }

            return diferencia.Value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
        }
    }
}
