using System;

namespace NinOS.Domain.ViewModels
{
    public class commission_dto
    {
        public int id_commission { get; set; }
        public int id_seller { get; set; }
        public string seller_name { get; set; } = string.Empty;
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public decimal commission_percentage { get; set; }
        public decimal amount_usd { get; set; }
        public decimal amount_bs { get; set; }
        public decimal exchange_rate { get; set; }
        public string reference_number { get; set; } = string.Empty;
        public bool is_paid { get; set; }
        public DateTime? payout_date { get; set; }
        public bool is_selected { get; set; }
    }
}
