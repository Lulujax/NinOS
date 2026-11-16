using System;

namespace NinOS.Domain
{
    public sealed class AdminLoginLimiter
    {
        public const int MaxAttempts = 5;
        public const int MaxDelaySeconds = 60;

        private int _failed_attempts;
        private DateTime _unlock_at = DateTime.MinValue;

        public bool is_locked(DateTime now)
        {
            return now < _unlock_at;
        }

        public void register_failure(DateTime now)
        {
            _failed_attempts++;
            if (_failed_attempts < MaxAttempts) return;

            int delay_seconds = Math.Min(MaxDelaySeconds, 5 * (_failed_attempts - MaxAttempts + 1));
            _unlock_at = now.AddSeconds(delay_seconds);
        }

        public void reset()
        {
            _failed_attempts = 0;
            _unlock_at = DateTime.MinValue;
        }

        public int remaining_delay_seconds(DateTime now)
        {
            if (now >= _unlock_at) return 0;

            return (int)Math.Ceiling((_unlock_at - now).TotalSeconds);
        }
    }
}
