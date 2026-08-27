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
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public decimal amount_usd { get; set; }
        public decimal amount_bs { get; set; }
        public decimal? exchange_rate { get; set; }
        public string payment_type { get; set; } = string.Empty;
        public string bank_name { get; set; } = string.Empty;
        public string reference_number { get; set; } = string.Empty;
        public string notes { get; set; } = string.Empty;
        public decimal total_note_usd { get; set; }
        public decimal balance_due_usd { get; set; }

        public string monto_bs_display => payment_type == "Efectivo" ? "-" : amount_bs.ToString("N2");
        public string tasa_display => exchange_rate.HasValue ? exchange_rate.Value.ToString("N2") : "-";
        public string ult_edicion_display => updated_at.HasValue ? updated_at.Value.ToString("dd/MM/yyyy HH:mm") : "-";
        public string fecha_registro_display => created_at.ToString("dd/MM/yyyy HH:mm");
    }
}
