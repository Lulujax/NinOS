using System;

namespace NinOS.Domain
{
    public static class pro_venta_week
    {
        public static DateTime lunes_de(DateTime instante_utc)
        {
            if (instante_utc == default)
                throw new ArgumentException("La fecha de la nota no puede estar vacía.", nameof(instante_utc));

            DateTime dia_local = AppTimeZone.to_local(instante_utc).Date;
            int desplazamiento_desde_lunes = ((int)dia_local.DayOfWeek + 6) % 7;
            return dia_local.AddDays(-desplazamiento_desde_lunes);
        }
    }
}
