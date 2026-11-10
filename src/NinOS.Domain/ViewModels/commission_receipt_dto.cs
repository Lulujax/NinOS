using System;
using System.Collections.Generic;
using System.Globalization;

namespace NinOS.Domain.ViewModels
{
    public class commission_receipt_row_dto
    {
        public DateTime? dispatch_date { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public decimal invoiced_amount { get; set; }
        public DateTime? payment_date { get; set; }
        public List<commission_note_payment_dto> note_payments { get; set; } = new();
        public string note_payments_text { get; set; } = string.Empty;
        public decimal paid_amount { get; set; }
        public decimal early_payment_discount { get; set; }
        public decimal early_payment_discount_pct { get; set; }
        public decimal commission_percentage { get; set; } = 0.10m;
        public decimal commission_10 { get; set; }
        public DateTime? commission_paid_date { get; set; }
        public string commission_paid_date_dmon => commission_paid_date?.ToString("dd-MMM", new CultureInfo("es-VE")).Replace(".", "").ToLowerInvariant() ?? string.Empty;
    }

    public class commission_receipt_dto
    {
        public string seller_name { get; set; } = string.Empty;
        public DateTime payment_date { get; set; }
        public string bank_name { get; set; } = string.Empty;
        public string reference_number { get; set; } = string.Empty;
        public string payment_reference => reference_number;
        public decimal total_bs { get; set; }
        public decimal total_usd { get; set; }
        public List<commission_receipt_row_dto> rows { get; set; } = new();
    }
}
