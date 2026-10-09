using System;

namespace NinOS.Domain
{
    /// <summary>
    /// Zona horaria de Venezuela (UTC-4, sin horario de verano).
    ///
    /// Las fechas se guardan en UTC, pero el sistema opera en Caracas. Tomar el
    /// desplazamiento de la maquina (TimeZoneInfo.Local o ToLocalTime) produce
    /// fechas equivocadas cuando el equipo esta en otra zona horaria: una nota
    /// creada a medianoche se reportaba un dia antes, y en el borde de mes caia en
    /// el mes anterior. Todos los calculos de anio, mes y dia deben pasar por aqui.
    /// </summary>
    public static class AppTimeZone
    {
        public static readonly TimeSpan Offset = TimeSpan.FromHours(-4);

        /// <summary>Instante UTC convertido a hora de Venezuela.</summary>
        public static DateTime to_local(DateTime utc)
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc) + Offset;
        }

        public static DateTime to_local(DateTimeOffset utc)
        {
            return utc.UtcDateTime + Offset;
        }

        /// <summary>Hora de Venezuela convertida al instante UTC que se guarda en la base.</summary>
        public static DateTime to_utc(DateTime local_caracas)
        {
            return DateTime.SpecifyKind(local_caracas, DateTimeKind.Utc) - Offset;
        }

        /// <summary>
        /// Desplazamiento en horas para aplicar dentro de consultas a la base,
        /// donde no se puede invocar la conversion en C#.
        /// </summary>
        public static double offset_hours => Offset.TotalHours;
    }
}