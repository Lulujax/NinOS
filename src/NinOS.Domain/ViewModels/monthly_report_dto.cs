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
        public string zone_name { get; set; } = string.Empty;
        public decimal amount_usd { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public string detail_text { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;

        // La fecha se guarda en UTC; sin convertir a hora local, un documento emitido de noche
        // se reporta con la fecha del dia anterior.
        public string fecha_display => date.ToLocalTime().ToString("dd/MM/yyyy");
        public string monto_display => amount_usd.ToString("N2");
    }

    public class monthly_report_dto
    {
        public string title { get; set; } = string.Empty;
        public string month { get; set; } = string.Empty;
        public string report_name { get; set; } = string.Empty;
        public string group_mode { get; set; } = "Por Vendedor";
        public string detail_column_header { get; set; } = string.Empty;
        public string status_column_header { get; set; } = "ESTADO";
        public bool show_paid_balance_summary { get; set; }
        public string empty_text { get; set; } = "Sin registros para el mes seleccionado.";
        public List<monthly_report_row_dto> rows { get; set; } = new();

        public decimal? sales_goal_usd { get; set; }
        public decimal month_total_usd { get; set; }
        public double goal_progress_percent { get; set; }
        public decimal goal_remaining_usd { get; set; }
        public string goal_status_text { get; set; } = string.Empty;
        public bool show_goal_block { get; set; }
    }
}
