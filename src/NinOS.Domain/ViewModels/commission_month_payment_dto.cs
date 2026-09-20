using System;

namespace NinOS.Domain.ViewModels
{
    public class commission_month_payment_dto
    {
        public int id_commission_payment { get; set; }
        public int id_commission { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public string payment_type { get; set; } = string.Empty;
        public string reference_number { get; set; } = string.Empty;
        public string bank_name { get; set; } = string.Empty;
        public string observations { get; set; } = string.Empty;
        public decimal amount_usd { get; set; }
        public decimal amount_bs { get; set; }
        public decimal exchange_rate { get; set; }
        public DateTime payment_date { get; set; }

        public DateTime? dispatch_date { get; set; }
        public DateTime? note_payment_date { get; set; }
        public decimal invoiced_amount { get; set; }
        public decimal paid_amount { get; set; }
        public decimal early_payment_discount { get; set; }
        public decimal commission_10 { get; set; }

        public string fecha_display => payment_date.ToString("dd/MM/yyyy");
        public string monto_display => amount_usd.ToString("N2");

        public bool is_paid => payment_date != default(DateTime);
    }
}