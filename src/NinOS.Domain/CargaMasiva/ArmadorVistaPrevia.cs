using System;
using System.Collections.Generic;
using System.Linq;

namespace NinOS.Domain.CargaMasiva
{
    public static class ArmadorVistaPrevia
    {
        public static VistaPreviaCarga armar(
            CabeceraPlantilla cabecera,
            TipoCargaCantidad? tipo_carga,
            IReadOnlyList<FilaPlantilla> filas,
            IReadOnlyDictionary<int, ProductoActual> productos_por_id,
            int productos_en_otras_listas)
        {
            if (cabecera == null) throw new ArgumentNullException(nameof(cabecera));
            if (filas == null) throw new ArgumentNullException(nameof(filas));
            if (productos_por_id == null) throw new ArgumentNullException(nameof(productos_por_id));

            List<FilaPlantilla> filas_con_valor = filas.Where(f => !f.esta_vacia).ToList();
            HashSet<int> ids_repetidos = ids_que_se_repiten(filas_con_valor);

            List<ResultadoFila> resultados = filas_con_valor
                .Select(fila => evaluar_fila(cabecera, tipo_carga, fila, productos_por_id, ids_repetidos))
                .ToList();

            return new VistaPreviaCarga(cabecera, tipo_carga, resultados, productos_en_otras_listas);
        }

        private static HashSet<int> ids_que_se_repiten(IEnumerable<FilaPlantilla> filas)
        {
            return filas
                .Where(f => f.id_product.HasValue)
                .GroupBy(f => f.id_product!.Value)
                .Where(grupo => grupo.Count() > 1)
                .Select(grupo => grupo.Key)
                .ToHashSet();
        }

        private static ResultadoFila evaluar_fila(
            CabeceraPlantilla cabecera,
            TipoCargaCantidad? tipo_carga,
            FilaPlantilla fila,
            IReadOnlyDictionary<int, ProductoActual> productos_por_id,
            HashSet<int> ids_repetidos)
        {
            if (fila.id_product == null)
            {
                return ResultadoFila.error(fila, null, "el ID no es válido");
            }

            int id = fila.id_product.Value;
            if (ids_repetidos.Contains(id))
            {
                return ResultadoFila.error(fila, null, "el producto aparece más de una vez en el archivo");
            }

            if (!productos_por_id.TryGetValue(id, out ProductoActual? producto))
            {
                return ResultadoFila.error(fila, null, "el producto ya no existe");
            }

            if (!producto.activo)
            {
                return ResultadoFila.error(fila, producto.valor_actual, "el producto fue eliminado");
            }

            if (!string.Equals(fila.codigo, producto.codigo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return ResultadoFila.error(fila, producto.valor_actual, "el código no coincide con el ID");
            }

            return cabecera.tipo == TipoPlantilla.Cantidad
                ? EvaluadorCantidad.evaluar(fila, producto, tipo_carga!.Value)
                : EvaluadorPrecio.evaluar(fila, producto);
        }
    }
}
