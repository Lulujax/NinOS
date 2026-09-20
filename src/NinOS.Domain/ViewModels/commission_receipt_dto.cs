using System;
using System.Collections.Generic;

namespace NinOS.Domain.ViewModels
{
    public class commission_receipt_row_dto
    {
        public DateTime? dispatch_date { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public decimal invoiced_amount { get; set; }
        public DateTime? payment_date { get; set; }
        public decimal paid_amount { get; set; }
        public decimal early_payment_discount { get; set; }
        public decimal commission_10 { get; set; }
        public DateTime? commission_paid_date { get; set; }
    }

    public class commission_receipt_dto
    {
        public string seller_name { get; set; } = string.Empty;
        public DateTime payment_date { get; set; }
        public string bank_name { get; set; } = string.Empty;
        public string reference_number { get; set; } = string.Empty;
        public decimal total_bs { get; set; }
        public decimal total_usd { get; set; }
        public List<commission_receipt_row_dto> rows { get; set; } = new();
    }
}
