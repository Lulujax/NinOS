using System;
using System.Collections.Generic;
using System.Globalization;

namespace NinOS.Domain.ViewModels
{
    // Opcion de un combo del reporte: label visible + id nullable (null = "Todos").
    public class report_filter_option_dto
    {
        public int? id { get; set; }
        public string label { get; set; } = string.Empty;

        public override string ToString() => label;
    }

    // Opcion del menu de reporte de notas de credito: el valor real del periodo
    // (GENERAL o el mes en formato "MMMM yyyy") y el texto que se ve.
    // Fila del detalle del reporte: una nota de credito del periodo.
    public class credit_note_report_row_dto
    {
        public int id_credit_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string source_note_number { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public int id_seller { get; set; }
        public int? id_zona { get; set; }
        public string zone_name { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public decimal total_amount_usd { get; set; }

        // creation_date viene en UTC; sin convertir a hora local, una nota de credito emitida de
        // noche se reporta con la fecha del dia anterior.
        public string fecha_display => creation_date.ToLocalTime().ToString("dd/MM/yyyy", new CultureInfo("es-VE"));
        public string categoria_display => string.IsNullOrWhiteSpace(category) ? "-" : category.Trim();
        public string entrega_display => string.IsNullOrWhiteSpace(source_note_number) ? "OBSEQUIO" : source_note_number;
        public string monto_display => total_amount_usd.ToString("N2", new CultureInfo("es-VE"));
        public bool es_obsequio => string.Equals(category?.Trim(), "Obsequio", StringComparison.OrdinalIgnoreCase);
        public bool esta_anulada => string.Equals(status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);
    }

    // Barra horizontal del desglose por vendedor.
    public class credit_note_report_seller_dto
    {
        public string seller_name { get; set; } = string.Empty;
        public int notes_count { get; set; }
        public decimal total_usd { get; set; }
        public decimal gift_usd { get; set; }
        public decimal return_usd { get; set; }

        public string notas_display => notes_count == 1 ? "1 nota" : $"{notes_count} notas";
        public string total_display => total_usd.ToString("N2", new CultureInfo("es-VE"));
        public string breakdown_display
        {
            get
            {
                var culture = new CultureInfo("es-VE");
                return $"{notas_display}   |   Obsequio {gift_usd.ToString("N2", culture)}   |   Devolucion {return_usd.ToString("N2", culture)}";
            }
        }

        // Proporcion 0..1 ya normalizada contra el vendedor mas alto del periodo.
        public double total_ratio { get; set; }
        public double gift_ratio { get; set; }
        public double return_ratio { get; set; }
    }

    // Barra vertical del desglose por dia del periodo.
    public class credit_note_report_day_dto
    {
        public int day { get; set; }
        public int notes_count { get; set; }
        public decimal total_usd { get; set; }

        public string day_display => day.ToString("00", CultureInfo.InvariantCulture);
        public string day_label => $"Dia {day}";
        public string total_display => total_usd == 0 ? string.Empty : total_usd.ToString("N2", new CultureInfo("es-VE"));
        public double total_ratio { get; set; }
        public bool tiene_movimiento => notes_count > 0;
    }

    // Reporte consolidado de notas de credito de un periodo.
    public class credit_note_report_dto
    {
        public DateTime from_date { get; set; }
        public DateTime to_date { get; set; }
        public string period_label { get; set; } = string.Empty;
        public string category_label { get; set; } = "Todas";
        public string seller_label { get; set; } = "Todos";

        public int total_notes { get; set; }
        public decimal total_usd { get; set; }
        public int gift_notes { get; set; }
        public decimal gift_usd { get; set; }
        public int return_notes { get; set; }
        public decimal return_usd { get; set; }
        public int voided_notes { get; set; }
        public decimal voided_usd { get; set; }
        public int affected_customers { get; set; }
        public decimal average_note_usd { get; set; }

        // Periodo anterior de igual longitud, para el comparativo.
        public decimal previous_total_usd { get; set; }
        public int previous_total_notes { get; set; }

        public bool has_data => total_notes > 0;

        /// <summary>
        /// True cuando el usuario eligio mostrar las notas anuladas. Siempre aparecen sin sumar a
        /// los totales: el generador las excluye de los importes y las marca en rojo.
        /// </summary>
        public bool include_annulled { get; set; }
        public string total_usd_display => total_usd.ToString("N2", new CultureInfo("es-VE"));
        public string gift_usd_display => gift_usd.ToString("N2", new CultureInfo("es-VE"));
        public string return_usd_display => return_usd.ToString("N2", new CultureInfo("es-VE"));
        public string average_display => average_note_usd.ToString("N2", new CultureInfo("es-VE"));
        public string previous_total_display => previous_total_usd.ToString("N2", new CultureInfo("es-VE"));
        public string notes_display => total_notes == 1 ? "1 nota" : $"{total_notes} notas";
        public string gift_share_display => total_usd <= 0
            ? "0%"
            : (gift_usd / total_usd * 100m).ToString("N1", new CultureInfo("es-VE")) + "%";
        public string return_share_display => total_usd <= 0
            ? "0%"
            : (return_usd / total_usd * 100m).ToString("N1", new CultureInfo("es-VE")) + "%";
        public string previous_period_label { get; set; } = string.Empty;
        public string variation_display
        {
            get
            {
                if (previous_total_usd <= 0) return total_usd <= 0 ? "Sin comparativo" : "Sin base previa";
                decimal pct = (total_usd - previous_total_usd) / previous_total_usd * 100m;
                string sign = pct > 0 ? "+" : string.Empty;
                return sign + pct.ToString("N1", new CultureInfo("es-VE")) + "% vs. periodo anterior";
            }
        }
        public bool variation_up => total_usd > previous_total_usd;
        public bool variation_down => total_usd < previous_total_usd;

        public List<credit_note_report_row_dto> rows { get; set; } = new();
        public List<credit_note_report_seller_dto> by_seller { get; set; } = new();
        public List<credit_note_report_day_dto> by_day { get; set; } = new();
    }
}
