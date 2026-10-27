using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NinOS.Domain
{
    /// <summary>
    /// Precio propio de un producto dentro de una lista de precios. La fila existe
    /// solo cuando el precio de la lista difiere del original: si no hay fila, la
    /// lista hereda product.unit_price_usd. Asi cambiar el precio original actualiza
    /// automaticamente todas las listas que no tengan precio propio.
    /// </summary>
    public class product_price
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_product_price { get; set; }

        [Required]
        public int id_product { get; set; }

        [Required]
        public int id_price_list { get; set; }

        [Required]
        public decimal unit_price_usd { get; set; }

        public product? product { get; set; }

        public price_list? price_list { get; set; }
    }
}
