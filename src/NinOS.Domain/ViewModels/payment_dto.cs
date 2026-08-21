using System;

namespace NinOS.Domain.ViewModels
{
    public class payment_dto
    {
        public int id_payment { get; set; }
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public int id_seller { get; set; }
        public DateTime payment_date { get; set; }
        public decimal amount_usd { get; set; }
        public decimal amount_bs { get; set; }
        public decimal? exchange_rate { get; set; }
        public string payment_type { get; set; } = string.Empty;
        public string reference_number { get; set; } = string.Empty;
        public decimal total_note_usd { get; set; }
        public decimal balance_due_usd { get; set; }
    }
}
