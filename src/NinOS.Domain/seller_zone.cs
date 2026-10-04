using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NinOS.Domain
{
    public class seller_zone
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seller_zone { get; set; }

        [Required]
        public int id_seller { get; set; }

        [ForeignKey(nameof(id_seller))]
        public virtual seller? seller { get; set; }

        [Required]
        public int id_zona { get; set; }

        [ForeignKey(nameof(id_zona))]
        public virtual zona? zona { get; set; }
    }
}
