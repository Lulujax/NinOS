using System;

namespace NinOS.Domain.ViewModels
{
    public class product_sales_history_dto
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public string line_description { get; set; } = string.Empty;
        public string sold_as { get; set; } = string.Empty;
        public int units_sold { get; set; }
        public decimal unit_price_usd { get; set; }
        public decimal line_subtotal_usd { get; set; }
        public string status { get; set; } = string.Empty;
        public string movement_type { get; set; } = string.Empty;

        public string fecha_display => creation_date.ToString("dd/MM/yyyy");
        public int signed_units => movement_type == "ENTRADA" ? units_sold : -units_sold;
        public string unidades_display => signed_units >= 0 ? $"+{signed_units}" : signed_units.ToString();
        public string precio_display => unit_price_usd.ToString("N2");
        public string subtotal_display => line_subtotal_usd.ToString("N2");
    }
}
