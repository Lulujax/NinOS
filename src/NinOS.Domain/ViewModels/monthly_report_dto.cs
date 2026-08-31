using System;
using System.Collections.Generic;

namespace NinOS.Domain.ViewModels
{
    public class monthly_report_row_dto
    {
        public DateTime date { get; set; }
        public string document_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public decimal amount_usd { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public string detail_text { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;

        public string fecha_display => date.ToString("dd/MM/yyyy");
        public string monto_display => amount_usd.ToString("N2");
    }

    public class monthly_report_dto
    {
        public string title { get; set; } = string.Empty;
        public string month { get; set; } = string.Empty;
        public string report_name { get; set; } = string.Empty;
        public string detail_column_header { get; set; } = string.Empty;
        public string status_column_header { get; set; } = "ESTADO";
        public bool show_paid_balance_summary { get; set; }
        public List<monthly_report_row_dto> rows { get; set; } = new();
    }
}
