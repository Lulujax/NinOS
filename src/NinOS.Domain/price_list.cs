using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NinOS.Domain
{
    /// <summary>
    /// Lista de precios con nombre. Un producto puede tener un precio propio en cada
    /// lista; si no lo tiene, esa lista usa el precio original del producto
    /// (product.unit_price_usd). La herencia es por ausencia: no existe fila en
    /// product_price para ese par producto/lista.
    /// </summary>
    public class price_list
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_price_list { get; set; }

        [Required]
        [MaxLength(100)]
        public string name { get; set; } = string.Empty;

        [Required]
        public bool is_active { get; set; } = true;

        [Required]
        public int sort_order { get; set; }

        public DateTime created_at { get; set; } = DateTime.UtcNow;
    }
}
