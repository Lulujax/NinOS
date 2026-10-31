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

        [MaxLength(500)]
        public string? description { get; set; }

        [Required]
        public bool is_active { get; set; } = true;

        /// <summary>
        /// Marca la lista Principal. Siempre existe, se puede editar pero no eliminar.
        /// Sus precios no viven en product_price: son el precio base del producto.
        /// </summary>
        public bool is_principal { get; set; }

        [Required]
        public int sort_order { get; set; }

        public DateTime created_at { get; set; } = DateTime.UtcNow;
    }
}
