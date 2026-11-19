using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NinOS.Domain
{
    public class relacion_vesupronta_fuente
    {
        private const string ESTADO_ANULADA = "Anulada";

        public int id_delivery_note { get; }
        public string note_number { get; }
        public string customer_name { get; }
        public DateTime creation_date { get; }
        public DateTime? dispatch_date { get; }
        public string status { get; }
        public decimal monto_facturado { get; }
        public bool descuento_aplicado { get; }
        public decimal monto_registrado { get; }
        public decimal monto_abonado { get; }
        public DateTime? fecha_ultimo_pago { get; }

        public bool esta_anulada => string.Equals(status?.Trim(), ESTADO_ANULADA, StringComparison.OrdinalIgnoreCase);

        public relacion_vesupronta_fuente(
            int id_delivery_note,
            string note_number,
            string customer_name,
            DateTime creation_date,
            DateTime? dispatch_date,
            string status,
            decimal monto_facturado,
            bool descuento_aplicado,
            decimal monto_registrado,
            decimal monto_abonado,
            DateTime? fecha_ultimo_pago)
        {
            if (id_delivery_note <= 0)
                throw new ArgumentException("El identificador de la nota debe ser mayor que cero.", nameof(id_delivery_note));
            if (string.IsNullOrWhiteSpace(note_number))
                throw new ArgumentException("La nota necesita un número.", nameof(note_number));
            if (creation_date == default)
                throw new ArgumentException("La nota necesita fecha de emisión.", nameof(creation_date));
            if (monto_facturado < 0m)
                throw new ArgumentOutOfRangeException(nameof(monto_facturado), "El monto facturado no puede ser negativo.");
            if (monto_registrado < 0m)
                throw new ArgumentOutOfRangeException(nameof(monto_registrado), "El monto registrado no puede ser negativo.");
            if (monto_abonado < 0m)
                throw new ArgumentOutOfRangeException(nameof(monto_abonado), "El monto abonado no puede ser negativo.");

            this.id_delivery_note = id_delivery_note;
            this.note_number = note_number;
            this.customer_name = customer_name ?? string.Empty;
            this.creation_date = creation_date;
            this.dispatch_date = dispatch_date;
            this.status = status ?? string.Empty;
            this.monto_facturado = monto_facturado;
            this.descuento_aplicado = descuento_aplicado;
            this.monto_registrado = monto_registrado;
            this.monto_abonado = monto_abonado;
            this.fecha_ultimo_pago = fecha_ultimo_pago;
        }
    }

    public class relacion_vesupronta_fila
    {
        public DateTime? fecha_despacho { get; init; }
        public string nro { get; init; } = string.Empty;
        public string cliente { get; init; } = string.Empty;
        public decimal monto_facturado { get; init; }
        public bool descuento_aplicado { get; init; }
        public DateTime? fecha_pago { get; init; }
        public decimal monto_pagado { get; init; }
        public decimal comision { get; init; }
        public decimal saldo_actualizado { get; init; }
        public DateTime? fecha_comision_cancelada { get; init; }

        public string despacho_texto => FechaTexto(fecha_despacho);
        public string descuento_texto => descuento_aplicado ? "SI" : "NO";
        public string fecha_pago_texto => FechaTexto(fecha_pago);
        public string fecha_comision_texto => FechaTexto(fecha_comision_cancelada);
        public string monto_facturado_texto => $"{monto_facturado:N2}";
        public string monto_pagado_texto => $"{monto_pagado:N2}";
        public string comision_texto => $"{comision:N2}";
        public string saldo_actualizado_texto => $"{saldo_actualizado:N2}";

        private static string FechaTexto(DateTime? fecha_utc)
        {
            return fecha_utc.HasValue ? AppTimeZone.to_local_text(fecha_utc, "dd/MM/yyyy") : string.Empty;
        }
    }

    public class relacion_vesupronta_reporte
    {
        public string titulo { get; init; } = string.Empty;
        public string periodo_texto { get; init; } = string.Empty;
        public string etiqueta_total_ventas { get; init; } = string.Empty;
        public IReadOnlyList<relacion_vesupronta_fila> filas { get; init; } = Array.Empty<relacion_vesupronta_fila>();
        public decimal total_ventas { get; init; }
        public decimal total_por_cobrar { get; init; }
    }

    public static class relacion_vesupronta_calculator
    {
        public const decimal PORCENTAJE_COMISION = 0.10m;
        public static relacion_vesupronta_reporte construir(IEnumerable<relacion_vesupronta_fuente> fuentes, rango_fechas rango)
        {
            ArgumentNullException.ThrowIfNull(fuentes);
            ArgumentNullException.ThrowIfNull(rango);

            List<relacion_vesupronta_fila> filas = fuentes
                .Where(fuente => !fuente.esta_anulada)
                .Where(fuente => rango.contiene(fuente.creation_date))
                .OrderBy(fuente => fuente.dispatch_date ?? fuente.creation_date)
                .ThenBy(fuente => fuente.id_delivery_note)
                .Select(FilaDesde)
                .ToList();

            decimal total_ventas = Money.round(filas.Sum(fila => fila.monto_pagado));
            decimal total_por_cobrar = Money.round(filas.Sum(fila => fila.saldo_actualizado));

            if (total_por_cobrar > total_ventas)
                throw new InvalidOperationException("El total por cobrar no puede superar el total de ventas de la relación.");

            return new relacion_vesupronta_reporte
            {
                titulo = titulo(rango),
                periodo_texto = rango.texto_titulo_mayusculas(),
                etiqueta_total_ventas = $"TOTAL VENTAS {rango.texto_titulo_mayusculas()}",
                filas = filas,
                total_ventas = total_ventas,
                total_por_cobrar = total_por_cobrar
            };
        }

        public static string titulo(rango_fechas rango)
        {
            return $"RELACION POR COBRAR/VENTAS {rango.texto_titulo_mayusculas()} VESU PRONTA";
        }

        private static relacion_vesupronta_fila FilaDesde(relacion_vesupronta_fuente fuente)
        {
            decimal saldo = Money.round(Math.Max(0m, fuente.monto_registrado - fuente.monto_abonado));
            bool pagada = saldo == 0m && fuente.monto_registrado > 0m;
            decimal monto_pagado = Money.round(fuente.monto_registrado);

            return new relacion_vesupronta_fila
            {
                fecha_despacho = fuente.dispatch_date,
                nro = fuente.note_number,
                cliente = fuente.customer_name,
                monto_facturado = Money.round(fuente.monto_facturado),
                descuento_aplicado = fuente.descuento_aplicado,
                fecha_pago = pagada ? fuente.fecha_ultimo_pago : null,
                monto_pagado = monto_pagado,
                comision = Money.round(monto_pagado * PORCENTAJE_COMISION),
                saldo_actualizado = saldo,
                fecha_comision_cancelada = null
            };
        }

    }
}
