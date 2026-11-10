using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NinOS.Domain.ViewModels
{
    public class pro_venta_month_option
    {
        public DateTime value { get; set; }
        public string label { get; set; } = string.Empty;

        public override string ToString()
        {
            return label;
        }
    }

    public class pro_venta_week_info
    {
        public int week_index { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string label { get; set; } = string.Empty;
    }

    public class pro_venta_weekly_row : INotifyPropertyChanged
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public decimal commission_luis { get; set; }
        public decimal gastos_25 { get; set; }
        public decimal gastos_15 { get; set; }
        public string payment_date_display { get; set; } = string.Empty;
        public string payment_method_display { get; set; } = string.Empty;

        /// <summary>
        /// Total bruto de la nota antes de descuentos (suma de subtotales).
        /// </summary>
        public decimal gross_total_usd { get; set; }

        /// <summary>
        /// Porcentaje de descuento comercial aplicado a la nota.
        /// </summary>
        public decimal? discount_percentage { get; set; }

        public string discount_percentage_text { get; set; } = "0%";

        /// <summary>
        /// Vendedor al que pertenece la nota. La tabla Pro Venta mezcla vendedores (Anais 3300 y
        /// Juan Luis 3400 tienen notas Pro Venta, y hay relaciones mezcladas), asi que sin esto no
        /// se puede saber de quien es la nota que se esta anulando.
        /// </summary>
        public string seller_name { get; set; } = string.Empty;
        public string seller_code { get; set; } = string.Empty;

        /// <summary>
        /// Estado de la nota (Pendiente, Pagada, Anulada). Antes no se traia, asi que el historial
        /// no podia distinguir una nota anulada de una vigente.
        /// </summary>
        public string status { get; set; } = string.Empty;

        public bool esta_anulada => string.Equals(status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

        public string status_display => esta_anulada ? "ANULADA" : string.Empty;

        // --- Estado de edición en línea (similar a cuentas por cobrar) ---
        private bool _is_editing;
        private string _edit_monto_text = string.Empty;
        private string _edit_dcto_text = string.Empty;
        private decimal _edit_base_total;
        private bool _syncing;

        public bool is_editing
        {
            get => _is_editing;
            set { _is_editing = value; on_property_changed(); }
        }

        public string edit_monto_text
        {
            get => _edit_monto_text;
            set
            {
                if (_edit_monto_text == value) return;
                _edit_monto_text = value;
                on_property_changed();
            }
        }

        public string edit_dcto_text
        {
            get => _edit_dcto_text;
            set
            {
                if (_edit_dcto_text == value) return;
                _edit_dcto_text = value;
                on_property_changed();
                if (!_syncing)
                {
                    _syncing = true;
                    apply_dcto_to_monto();
                    _syncing = false;
                }
            }
        }

        public decimal edit_result_condition_pct { get; private set; }

        public void begin_edit()
        {
            decimal cond = discount_percentage ?? 0m;
            decimal total = amount;

            decimal denom = 1m - cond / 100m;
            _edit_base_total = denom > 0.0000001m
                ? total / denom
                : (gross_total_usd > 0 ? gross_total_usd : total);
            edit_result_condition_pct = cond;

            _edit_monto_text = $"{total:0.##}";
            _edit_dcto_text = cond > 0 ? $"{cond:0.###}" : "0";
            on_property_changed(nameof(edit_monto_text));
            on_property_changed(nameof(edit_dcto_text));
        }

        private void apply_dcto_to_monto()
        {
            decimal dcto = parse_numeric(_edit_dcto_text);
            if (dcto < 0) dcto = 0;
            if (dcto > 100) dcto = 100;

            edit_result_condition_pct = dcto;

            decimal monto = _edit_base_total * (1m - dcto / 100m);
            edit_monto_text = $"{Math.Max(0, monto):0.##}";
        }

        public static decimal parse_numeric(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return -1;
            string t = text.Trim().Replace(" ", "");
            if (t.Length == 0) return -1;

            string sep = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
            string normalized;
            int last_comma = t.LastIndexOf(',');
            int last_dot = t.LastIndexOf('.');

            if (last_comma >= 0 && last_dot >= 0)
            {
                if (last_comma > last_dot)
                    normalized = t.Replace(".", "").Replace(",", sep);
                else
                    normalized = t.Replace(",", "").Replace(".", sep);
            }
            else if (last_comma >= 0)
                normalized = t.Replace(",", sep);
            else if (last_dot >= 0)
                normalized = t.Replace(".", sep);
            else
                normalized = t;

            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)) return result;
            return -1;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void on_property_changed([CallerMemberName] string? property_name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        }
    }

    public class pro_venta_relation_row
    {
        public int id_relacion { get; set; }
        public int relation_number { get; set; }
        public DateTime week_start { get; set; }
        public DateTime week_end { get; set; }
        public decimal amount { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public string status { get; set; } = string.Empty;

        /// <summary>
        /// Notas de la relacion que siguen vigentes. Las anuladas no se cuentan.
        /// </summary>
        public int note_count { get; set; }

        /// <summary>
        /// Lo que suman las notas anuladas de la relacion. Se lleva aparte del monto para poder
        /// mostrarlas sin que entren en el total ni en la liquidacion.
        /// </summary>
        public decimal annulled_amount { get; set; }
        public int annulled_count { get; set; }
        public bool has_annulled => annulled_count > 0;

        public string relation_label => $"NRO {relation_number} ({week_start:dd/MM} AL {week_end:dd/MM})";
    }

    public class pro_venta_relation_option
    {
        public int id_relacion { get; set; }
        public int relation_number { get; set; }
        public DateTime week_start { get; set; }
        public DateTime week_end { get; set; }
        public string label { get; set; } = string.Empty;

        public override string ToString()
        {
            return label;
        }
    }

    public class pro_venta_weekly_dto
    {
        public int id_relacion { get; set; }
        public int relation_number { get; set; }
        public DateTime week_start { get; set; }
        public DateTime week_end { get; set; }
        public string city { get; set; } = "MARACAY";
        public List<pro_venta_weekly_row> rows { get; set; } = new();

        /// <summary>
        /// Totales de la semana. Cagan solo las notas vigentes: las anuladas se siguen mostrando en
        /// la tabla, en rojo, pero no suman ni a la venta ni a la comision ni a los gastos.
        /// </summary>
        public decimal total_amount { get; set; }
        public decimal total_commission_luis { get; set; }
        public decimal total_gastos_25 { get; set; }
        public decimal total_gastos_15 { get; set; }

        /// <summary>
        /// Resumen de lo que se dejo fuera de los totales, para que la leyenda de la pantalla y
        /// del PDF pueda decir cuantas y por cuanto son.
        /// </summary>
        public decimal annulled_amount { get; set; }
        public int annulled_count { get; set; }
        public bool has_annulled => annulled_count > 0;
    }
}
