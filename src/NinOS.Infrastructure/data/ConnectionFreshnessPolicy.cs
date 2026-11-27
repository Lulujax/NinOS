using System;

namespace NinOS.Infrastructure.Data
{
    public static class ConnectionFreshnessPolicy
    {
        public static readonly TimeSpan DEFAULT_IDLE_THRESHOLD = TimeSpan.FromSeconds(15);

        public static bool needs_ping(DateTime? last_activity_utc, DateTime now_utc, TimeSpan idle_threshold)
        {
            if (idle_threshold <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(idle_threshold), "El umbral de inactividad debe ser mayor que cero.");
            }

            if (last_activity_utc is null)
            {
                return false;
            }

            if (now_utc < last_activity_utc.Value)
            {
                return false;
            }

            return now_utc - last_activity_utc.Value >= idle_threshold;
        }
    }
}
