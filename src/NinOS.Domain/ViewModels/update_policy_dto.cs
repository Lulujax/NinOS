using System;

namespace NinOS.Domain.ViewModels
{
    public class update_policy_dto
    {
        public string minimum_version { get; set; } = string.Empty;
        public string recommended_version { get; set; } = string.Empty;
        public DateTime? notice_at_utc { get; set; }
        public string? notice_message { get; set; }
        public DateTime server_time_utc { get; set; }
    }
}
