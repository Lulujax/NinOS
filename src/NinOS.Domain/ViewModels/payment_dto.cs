using System;

namespace NinOS.Domain.ViewModels
{
    public class payment_dto
    {
        public int id_payment { get; set; }
        public int id_delivery_note { get; set; }
        public int? id_relacion { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public int id_seller { get; set; }

        /// <summary>Zona del cliente de la nota pagada. Se usa para agrupar el reporte por zona.</summary>
        public string zone_name { get; set; } = string.Empty;

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
        public bool es_negativo => amount_usd < 0;

        /// <summary>
        /// Marca los abonos que no son pagos reales sino el asiento que deja la anulacion de una
        /// nota. Se guardan con monto negativo para que salgan en rojo igual que una nota de
        /// credito, pero no se pueden editar: el monto lo fija la nota anulada.
        /// </summary>
        public bool es_anulacion => string.Equals(payment_type, AnulacionPaymentType, StringComparison.OrdinalIgnoreCase);

        public const string AnulacionPaymentType = "Anulacion";
        public decimal monto_usd_magnitud => Math.Abs(amount_usd);
    }
}
