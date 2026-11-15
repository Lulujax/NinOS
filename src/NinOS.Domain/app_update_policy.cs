using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NinOS.Domain
{
    public class app_update_policy
    {
        public const int SingleRowId = 1;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id_policy { get; set; } = SingleRowId;

        [Required]
        [MaxLength(20)]
        public string minimum_version { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string recommended_version { get; set; } = string.Empty;

        public DateTime? notice_at_utc { get; set; }

        [MaxLength(500)]
        public string? notice_message { get; set; }

        [Required]
        public DateTime updated_at_utc { get; set; }
    }
}
