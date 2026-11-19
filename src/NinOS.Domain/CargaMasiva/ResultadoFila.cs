using System;

namespace NinOS.Domain.CargaMasiva
{
    public sealed class ResultadoFila
    {
        public int numero_fila { get; }
        public int? id_product { get; }
        public string? codigo { get; }
        public string? descripcion { get; }
        public decimal? valor_archivo { get; }
        public decimal? valor_actual { get; }
        public decimal? valor_final { get; }
        public EstadoFila estado { get; }
        public string? motivo { get; }

        public ResultadoFila(
            FilaPlantilla fila,
            decimal? valor_actual,
            decimal? valor_final,
            EstadoFila estado,
            string? motivo)
        {
            if (fila == null) throw new ArgumentNullException(nameof(fila));

            numero_fila = fila.numero_fila;
            id_product = fila.id_product;
            codigo = fila.codigo;
            descripcion = fila.descripcion;
            valor_archivo = fila.valor_archivo;
            this.valor_actual = valor_actual;
            this.valor_final = valor_final;
            this.estado = estado;
            this.motivo = motivo;
        }

        public decimal? diferencia => valor_final.HasValue && valor_actual.HasValue
            ? valor_final.Value - valor_actual.Value
            : null;

        public decimal? porcentaje_cambio => valor_final.HasValue && valor_actual.HasValue && valor_actual.Value > 0
            ? (valor_final.Value - valor_actual.Value) / valor_actual.Value * 100m
            : null;

        public static ResultadoFila error(FilaPlantilla fila, decimal? valor_actual, string texto_error)
        {
            return new ResultadoFila(
                fila,
                valor_actual,
                null,
                EstadoFila.Error,
                "Fila " + fila.numero_fila + ": " + texto_error);
        }
    }
}
