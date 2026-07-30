using System;

namespace NinOS.Domain
{
    public class promotion_item
    {
        public int id_promotion_item { get; set; }
        
        public int id_promotion { get; set; }
        public promotion promotion { get; set; }

        public int id_product { get; set; }
        public product product { get; set; }

        public int quantity_required { get; set; }

        public promotion_item(int id_product, int quantity_required)
        {
            if (quantity_required <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero.");
            
            this.id_product = id_product;
            this.quantity_required = quantity_required;
        }

        public promotion_item() { }
    }
}