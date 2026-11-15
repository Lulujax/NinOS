using System;

namespace NinOS.Domain
{
    public enum notice_state
    {
        sin_aviso,
        contando,
        vencido,
        cancelado
    }

    public record notice_countdown(notice_state state, TimeSpan remaining, string text);

    public static class UpdateNoticeCountdown
    {
        public static notice_countdown calculate(DateTime? notice_at_utc, DateTime now_utc, bool had_notice_before)
        {
            if (now_utc == default)
                throw new ArgumentException("La hora actual no puede estar vacia.", nameof(now_utc));

            if (notice_at_utc == null)
            {
                return had_notice_before
                    ? new notice_countdown(notice_state.cancelado, TimeSpan.Zero, string.Empty)
                    : new notice_countdown(notice_state.sin_aviso, TimeSpan.Zero, string.Empty);
            }

            TimeSpan remaining = notice_at_utc.Value.ToUniversalTime() - now_utc.ToUniversalTime();

            if (remaining <= TimeSpan.Zero)
                return new notice_countdown(notice_state.vencido, TimeSpan.Zero, "el sistema debe reiniciarse ahora para actualizar");

            return new notice_countdown(notice_state.contando, remaining, "en " + describe_remaining(remaining) + " el sistema se reinicia para actualizar");
        }

        private static string describe_remaining(TimeSpan remaining)
        {
            if (remaining < TimeSpan.FromMinutes(1))
                return "menos de 1 minuto";

            int minutes = (int)Math.Ceiling(remaining.TotalMinutes);
            return minutes == 1 ? "1 minuto" : minutes + " minutos";
        }
    }
}
