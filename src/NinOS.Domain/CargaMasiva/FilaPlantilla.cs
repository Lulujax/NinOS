using System;

namespace NinOS.Domain.CargaMasiva
{
    public sealed class FilaPlantilla
    {
        public int numero_fila { get; }
        public int? id_product { get; }
        public string? codigo { get; }
        public decimal? valor_archivo { get; }
        public decimal? valor_escrito { get; }
        public string? texto_escrito { get; }
        public string? descripcion { get; }

        public FilaPlantilla(
            int numero_fila,
            int? id_product,
            string? codigo,
            decimal? valor_archivo,
            decimal? valor_escrito,
            string? texto_escrito,
            string? descripcion = null)
        {
            if (numero_fila <= 0) throw new ArgumentOutOfRangeException(nameof(numero_fila), "El número de fila debe ser mayor que cero.");

            this.numero_fila = numero_fila;
            this.id_product = id_product;
            this.codigo = string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim();
            this.valor_archivo = valor_archivo;
            this.valor_escrito = valor_escrito;
            this.texto_escrito = string.IsNullOrWhiteSpace(texto_escrito) ? null : texto_escrito.Trim();
            this.descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        }

        public bool esta_vacia => valor_escrito == null && texto_escrito == null;
    }
}
