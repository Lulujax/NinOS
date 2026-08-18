using System;
using System.Collections.Generic;

namespace NinOS.Domain.ViewModels
{
    public class note_print_dto
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public DateTime due_date { get; set; }
        public string status { get; set; } = string.Empty;
        public decimal gross_total_usd { get; set; }
        public decimal discount_percentage { get; set; }
        public decimal discount_amount { get; set; }
        public decimal total_amount_usd { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public string seller_name { get; set; } = string.Empty;
        public string customer_code { get; set; } = string.Empty;
        public string customer_business_name { get; set; } = string.Empty;
        public string customer_rif { get; set; } = string.Empty;
        public string customer_phone { get; set; } = string.Empty;
        public string customer_delivery_address { get; set; } = string.Empty;
        public string fiscal_address { get; set; } = string.Empty;
        public string conditions_text { get; set; } = string.Empty;
        public string discount_conditions_text { get; set; } = string.Empty;
        public List<note_detail_print_dto> details { get; set; } = new();
    }

    public class note_detail_print_dto
    {
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public int quantity { get; set; }
        public decimal unit_price_usd { get; set; }
        public decimal promo_price_usd { get; set; }
        public decimal subtotal_usd { get; set; }
    }
}
