using System;
using System.Globalization;

namespace NinOS.Domain.CargaMasiva
{
    public static class EvaluadorCantidad
    {
        public static ResultadoFila evaluar(FilaPlantilla fila, ProductoActual producto, TipoCargaCantidad tipo_carga)
        {
            if (fila == null) throw new ArgumentNullException(nameof(fila));
            if (producto == null) throw new ArgumentNullException(nameof(producto));

            if (fila.texto_escrito != null)
            {
                return ResultadoFila.error(fila, producto.valor_actual, "la cantidad tiene letras o no es un número");
            }

            decimal valor = fila.valor_escrito!.Value;
            if (valor != decimal.Truncate(valor))
            {
                return ResultadoFila.error(fila, producto.valor_actual, "la cantidad debe ser un número entero");
            }

            if (Math.Abs(valor) > int.MaxValue)
            {
                return ResultadoFila.error(fila, producto.valor_actual, "la cantidad es demasiado grande");
            }

            int cantidad = (int)valor;

            return tipo_carga == TipoCargaCantidad.Conteo
                ? evaluar_conteo(fila, producto, cantidad)
                : evaluar_ajuste(fila, producto, cantidad);
        }

        private static ResultadoFila evaluar_conteo(FilaPlantilla fila, ProductoActual producto, int cantidad_contada)
        {
            if (cantidad_contada < 0)
            {
                return ResultadoFila.error(fila, producto.valor_actual, "la cantidad contada no puede ser negativa");
            }

            decimal stock_actual = producto.valor_actual;
            if (cantidad_contada == stock_actual)
            {
                return new ResultadoFila(fila, stock_actual, stock_actual, EstadoFila.Igual, null);
            }

            if (fila.valor_archivo.HasValue && fila.valor_archivo.Value != stock_actual)
            {
                string texto = "Cambió desde que descargó: archivo " + formatear(fila.valor_archivo.Value)
                    + ", ahora " + formatear(stock_actual);
                return new ResultadoFila(fila, stock_actual, cantidad_contada, EstadoFila.Aviso, texto);
            }

            return new ResultadoFila(fila, stock_actual, cantidad_contada, EstadoFila.Cambia, null);
        }

        private static ResultadoFila evaluar_ajuste(FilaPlantilla fila, ProductoActual producto, int cantidad_a_sumar)
        {
            decimal stock_actual = producto.valor_actual;
            if (cantidad_a_sumar == 0)
            {
                return new ResultadoFila(fila, stock_actual, stock_actual, EstadoFila.Igual, null);
            }

            decimal stock_final = stock_actual + cantidad_a_sumar;
            if (stock_final < 0)
            {
                return ResultadoFila.error(
                    fila,
                    stock_actual,
                    "el ajuste dejaría el stock en " + formatear(stock_final) + " (stock disponible " + formatear(stock_actual) + ")");
            }

            return new ResultadoFila(fila, stock_actual, stock_final, EstadoFila.Cambia, null);
        }

        private static string formatear(decimal valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
