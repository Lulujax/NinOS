using System;
using System.Collections.Generic;
using System.Linq;

namespace NinOS.Domain
{
    public readonly record struct linea_nota(int id_nota, int? id_producto, int? id_promocion, int cantidad, decimal precio_unitario);

    public readonly record struct devolucion_nota(int id_nota, int? id_producto, int? id_promocion, int cantidad);

    public static class original_amount_calculator
    {
        public static Dictionary<int, decimal> calcular(
            IReadOnlyList<linea_nota> lineas,
            IReadOnlyList<devolucion_nota> devoluciones)
        {
            ArgumentNullException.ThrowIfNull(lineas);
            ArgumentNullException.ThrowIfNull(devoluciones);
            validar_lineas(lineas);
            validar_devoluciones(devoluciones);

            var devuelto_por_clave = devoluciones
                .GroupBy(d => (d.id_nota, d.id_producto, d.id_promocion))
                .ToDictionary(g => g.Key, g => g.Sum(d => d.cantidad));

            var resultado = new Dictionary<int, decimal>();

            foreach (var grupo in lineas.GroupBy(l => l.id_nota))
            {
                decimal total = 0m;

                foreach (var linea in grupo.GroupBy(l => (l.id_producto, l.id_promocion)))
                {
                    int por_descontar = devuelto_por_clave.TryGetValue((grupo.Key, linea.Key.id_producto, linea.Key.id_promocion), out int devuelto)
                        ? devuelto
                        : 0;

                    foreach (linea_nota renglon in linea)
                    {
                        int devuelto_en_renglon = Math.Min(renglon.cantidad, por_descontar);
                        por_descontar -= devuelto_en_renglon;
                        int vigente = renglon.cantidad - devuelto_en_renglon;
                        total += vigente * renglon.precio_unitario;
                    }
                }

                resultado[grupo.Key] = Money.round(total);
            }

            validar_resultado(resultado);
            return resultado;
        }

        public static decimal monto_al_emitir(IReadOnlyList<(int cantidad, decimal precio_unitario)> renglones)
        {
            ArgumentNullException.ThrowIfNull(renglones);
            if (renglones.Count == 0)
                throw new ArgumentException("Una nota Pro Venta debe tener al menos un renglón.", nameof(renglones));

            var lineas = renglones
                .Select(r => new linea_nota(0, null, null, r.cantidad, r.precio_unitario))
                .ToList();

            return calcular(lineas, Array.Empty<devolucion_nota>())[0];
        }

        private static void validar_lineas(IReadOnlyList<linea_nota> lineas)
        {
            foreach (linea_nota linea in lineas)
            {
                if (linea.cantidad < 0)
                    throw new ArgumentException($"La cantidad del renglón de la nota {linea.id_nota} no puede ser negativa.", nameof(lineas));
                if (linea.precio_unitario < 0m)
                    throw new ArgumentException($"El precio del renglón de la nota {linea.id_nota} no puede ser negativo.", nameof(lineas));
            }
        }

        private static void validar_devoluciones(IReadOnlyList<devolucion_nota> devoluciones)
        {
            foreach (devolucion_nota devolucion in devoluciones)
            {
                if (devolucion.cantidad < 0)
                    throw new ArgumentException($"La cantidad devuelta de la nota {devolucion.id_nota} no puede ser negativa.", nameof(devoluciones));
            }
        }

        private static void validar_resultado(Dictionary<int, decimal> resultado)
        {
            if (resultado.Values.Any(total => total < 0m))
                throw new InvalidOperationException("El monto original calculado no puede ser negativo.");
        }
    }
}
