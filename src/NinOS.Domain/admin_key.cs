using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NinOS.Domain
{
    public class admin_key
    {
        public const int SingleRowId = 1;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id_key { get; set; } = SingleRowId;

        [Required]
        [MaxLength(64)]
        public string password_salt { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string password_hash { get; set; } = string.Empty;

        public int iterations { get; set; }

        [Required]
        public DateTime updated_at_utc { get; set; }
    }
}
