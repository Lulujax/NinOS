using System;

namespace NinOS.Domain.ViewModels
{
    public class commission_payment_dto
    {
        public int id_commission_payment { get; set; }
        public int id_commission { get; set; }
        public decimal amount_usd { get; set; }
        public decimal amount_bs { get; set; }
        public decimal exchange_rate { get; set; }
        public string payment_type { get; set; } = string.Empty;
        public string reference_number { get; set; } = string.Empty;
        public string bank_name { get; set; } = string.Empty;
        public string notes { get; set; } = string.Empty;
        public DateTime payment_date { get; set; }
    }
}