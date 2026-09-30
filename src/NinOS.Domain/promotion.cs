using System;
using System.Collections.Generic;

namespace NinOS.Domain
{
    public class promotion
    {
        public int id_promotion { get; set; }
        public string promotion_code { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public decimal unit_price_usd { get; set; }

        public List<promotion_item> items { get; set; }

        // Borrado logico: la promocion sale del inventario pero queda en la papelera
        // del panel de administrador y se puede restaurar con su codigo original.
        public bool is_active { get; set; } = true;

        public DateTime? deleted_at { get; set; }

        public string? deleted_reason { get; set; }

        public promotion(string promotion_code, string name, string category, decimal unit_price_usd)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre no puede estar vacío.");
            if (unit_price_usd < 0) throw new ArgumentException("El precio debe ser positivo.");

            this.promotion_code = promotion_code;
            this.name = name;
            this.category = category;
            this.unit_price_usd = unit_price_usd;
            this.items = new List<promotion_item>();
            this.is_active = true;
        }

        public promotion() 
        { 
            items = new List<promotion_item>(); 
        }
    }
}