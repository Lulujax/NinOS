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

        public promotion(string promotion_code, string name, string category, decimal unit_price_usd)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre no puede estar vacío.");
            if (unit_price_usd < 0) throw new ArgumentException("El precio debe ser positivo.");

            this.promotion_code = promotion_code;
            this.name = name;
            this.category = category;
            this.unit_price_usd = unit_price_usd;
            this.items = new List<promotion_item>();
        }

        public promotion() 
        { 
            items = new List<promotion_item>(); 
        }
    }
}