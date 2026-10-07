using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NinOS.Domain
{
    public class product_line
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_product_line { get; set; }
        
        [Required] [MaxLength(100)]
        public string name { get; set; } = string.Empty;
        
        [Required] [MaxLength(10)]
        public string code_prefix { get; set; } = string.Empty;
        
        [Required]
        public int sort_order { get; set; }
        
        [Required]
        public bool is_active { get; set; } = true;
        
        public DateTime? deleted_at { get; set; }
        
        [MaxLength(200)]
        public string? deleted_reason { get; set; }
    }
}
