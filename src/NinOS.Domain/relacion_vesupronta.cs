using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NinOS.Domain
{
    public enum relacion_modo_periodo
    {
        Mes,
        Anio,
        Todos
    }

    public readonly record struct relacion_periodo(relacion_modo_periodo modo, int anio, int mes);

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
        private const int ANIO_MINIMO = 2000;
        private static readonly CultureInfo CultureEs = new CultureInfo("es-VE");

        public static relacion_vesupronta_reporte construir(IEnumerable<relacion_vesupronta_fuente> fuentes, relacion_periodo periodo)
        {
            ArgumentNullException.ThrowIfNull(fuentes);
            validar_periodo(periodo);

            List<relacion_vesupronta_fila> filas = fuentes
                .Where(fuente => !fuente.esta_anulada)
                .Where(fuente => pertenece_al_periodo(fuente.creation_date, periodo))
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
                titulo = titulo(periodo),
                periodo_texto = periodo_texto(periodo),
                etiqueta_total_ventas = $"TOTAL VENTAS {periodo_texto(periodo)}",
                filas = filas,
                total_ventas = total_ventas,
                total_por_cobrar = total_por_cobrar
            };
        }

        public static bool pertenece_al_periodo(DateTime creation_utc, relacion_periodo periodo)
        {
            if (periodo.modo == relacion_modo_periodo.Todos) return true;

            DateTime local = AppTimeZone.to_local(creation_utc);
            if (local.Year != periodo.anio) return false;

            return periodo.modo == relacion_modo_periodo.Anio || local.Month == periodo.mes;
        }

        public static string titulo(relacion_periodo periodo)
        {
            return $"RELACION POR COBRAR/VENTAS {periodo_texto(periodo)} VESU PRONTA";
        }

        public static string periodo_texto(relacion_periodo periodo)
        {
            return periodo.modo switch
            {
                relacion_modo_periodo.Mes => $"{CultureEs.DateTimeFormat.GetMonthName(periodo.mes).ToUpperInvariant()} {periodo.anio}",
                relacion_modo_periodo.Anio => periodo.anio.ToString(CultureInfo.InvariantCulture),
                _ => "TODOS LOS PERIODOS"
            };
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

        private static void validar_periodo(relacion_periodo periodo)
        {
            if (periodo.modo == relacion_modo_periodo.Todos) return;

            if (periodo.anio < ANIO_MINIMO)
                throw new ArgumentOutOfRangeException(nameof(periodo), "El año del reporte no es válido.");

            if (periodo.modo == relacion_modo_periodo.Mes && (periodo.mes < 1 || periodo.mes > 12))
                throw new ArgumentOutOfRangeException(nameof(periodo), "El mes del reporte debe estar entre 1 y 12.");
        }
    }
}
