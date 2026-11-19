using System;
using System.Globalization;

namespace NinOS.Domain
{
    public sealed record rango_fechas
    {
        private const int ANIO_MINIMO = 2000;

        public DateTime? desde { get; }
        public DateTime? hasta { get; }

        public rango_fechas(DateTime? desde, DateTime? hasta)
        {
            validar_fecha(desde, nameof(desde));
            validar_fecha(hasta, nameof(hasta));

            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
                throw new ArgumentException("La fecha Desde no puede ser posterior a la fecha Hasta.", nameof(desde));

            this.desde = desde?.Date;
            this.hasta = hasta?.Date;
        }

        public static rango_fechas mes_actual(DateTime hoy_local)
        {
            DateTime primer_dia = new DateTime(hoy_local.Year, hoy_local.Month, 1);
            DateTime ultimo_dia = primer_dia.AddMonths(1).AddDays(-1);
            return new rango_fechas(primer_dia, ultimo_dia);
        }

        public static rango_fechas sin_limites() => new rango_fechas(null, null);

        public bool contiene(DateTime fecha_utc)
        {
            DateTime dia_local = AppTimeZone.to_local(fecha_utc).Date;
            return cubre_dia(dia_local);
        }

        public bool se_cruza_con(DateTime inicio, DateTime fin)
        {
            if (fin.Date < inicio.Date)
                throw new ArgumentException("El fin no puede ser anterior al inicio.", nameof(fin));

            bool empieza_antes_del_hasta = !hasta.HasValue || inicio.Date <= hasta.Value;
            bool termina_despues_del_desde = !desde.HasValue || fin.Date >= desde.Value;
            return empieza_antes_del_hasta && termina_despues_del_desde;
        }

        public (int anio, int mes)? mes_unico()
        {
            if (!desde.HasValue || !hasta.HasValue) return null;
            if (desde.Value.Year != hasta.Value.Year || desde.Value.Month != hasta.Value.Month) return null;
            return (desde.Value.Year, desde.Value.Month);
        }

        public (DateTime? desde_utc, DateTime? hasta_utc_exclusivo) a_limites_utc()
        {
            DateTime? desde_utc = desde.HasValue ? AppTimeZone.to_utc(desde.Value) : null;
            DateTime? hasta_utc_exclusivo = hasta.HasValue ? AppTimeZone.to_utc(hasta.Value.AddDays(1)) : null;
            return (desde_utc, hasta_utc_exclusivo);
        }

        public string texto_titulo()
        {
            if (!desde.HasValue && !hasta.HasValue) return "TODOS LOS PERIODOS";
            if (!desde.HasValue) return $"Desde el inicio hasta el {hasta!.Value:dd/MM/yyyy}";
            if (!hasta.HasValue) return $"Desde el {desde.Value:dd/MM/yyyy}";
            return $"Del {desde.Value:dd/MM/yyyy} al {hasta.Value:dd/MM/yyyy}";
        }

        public string texto_titulo_mayusculas() => texto_titulo().ToUpperInvariant();

        private bool cubre_dia(DateTime dia_local)
        {
            bool despues_del_desde = !desde.HasValue || dia_local >= desde.Value;
            bool antes_del_hasta = !hasta.HasValue || dia_local <= hasta.Value;
            return despues_del_desde && antes_del_hasta;
        }

        private static void validar_fecha(DateTime? fecha, string nombre_parametro)
        {
            if (!fecha.HasValue) return;
            if (fecha.Value.Year < ANIO_MINIMO)
                throw new ArgumentOutOfRangeException(nombre_parametro, "La fecha del filtro no es válida.");
        }
    }
}
