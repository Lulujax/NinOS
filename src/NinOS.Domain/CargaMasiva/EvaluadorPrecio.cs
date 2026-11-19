using System;
using System.Globalization;

namespace NinOS.Domain.CargaMasiva
{
    public static class EvaluadorPrecio
    {
        public const decimal UmbralCambioGrandePorcentaje = 50m;

        public static ResultadoFila evaluar(FilaPlantilla fila, ProductoActual producto)
        {
            if (fila == null) throw new ArgumentNullException(nameof(fila));
            if (producto == null) throw new ArgumentNullException(nameof(producto));

            if (fila.texto_escrito != null)
            {
                return ResultadoFila.error(fila, producto.valor_actual, "el precio no es un número");
            }

            decimal precio_actual = producto.valor_actual;
            decimal escrito = fila.valor_escrito!.Value;

            if (escrito < 0)
            {
                return ResultadoFila.error(fila, precio_actual, "el precio no puede ser negativo");
            }

            decimal precio_nuevo = Money.round(escrito);
            if (precio_nuevo == 0 || precio_nuevo == precio_actual)
            {
                return new ResultadoFila(fila, precio_actual, precio_actual, EstadoFila.Igual, null);
            }

            if (fila.valor_archivo.HasValue && fila.valor_archivo.Value != precio_actual)
            {
                string texto = "Cambió desde que descargó: archivo " + formatear(fila.valor_archivo.Value)
                    + ", ahora " + formatear(precio_actual);
                return new ResultadoFila(fila, precio_actual, precio_nuevo, EstadoFila.Aviso, texto);
            }

            if (precio_actual > 0)
            {
                decimal porcentaje = (precio_nuevo - precio_actual) / precio_actual * 100m;
                if (Math.Abs(porcentaje) > UmbralCambioGrandePorcentaje)
                {
                    string texto = "Cambio grande: " + Math.Round(porcentaje).ToString("+0;-0", CultureInfo.InvariantCulture) + "%";
                    return new ResultadoFila(fila, precio_actual, precio_nuevo, EstadoFila.Aviso, texto);
                }
            }

            return new ResultadoFila(fila, precio_actual, precio_nuevo, EstadoFila.Cambia, null);
        }

        private static string formatear(decimal valor)
        {
            return valor.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
