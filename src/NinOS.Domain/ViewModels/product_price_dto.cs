using System;

namespace NinOS.Domain.ViewModels
{
    /// <summary>
    /// DTO para la visualización y edición masiva de precios de productos en listas de precios.
    /// Si custom_price_usd es null, el producto hereda el original_price_usd.
    /// </summary>
    public class product_price_dto
    {
        public int id_product { get; set; }
        public string product_code { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public decimal original_price_usd { get; set; }
        public decimal? custom_price_usd { get; set; }

        public bool is_inherited => !custom_price_usd.HasValue;
        public decimal effective_price_usd => custom_price_usd ?? original_price_usd;
    }
}

