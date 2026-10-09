using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NinOS.Domain.ViewModels
{
    public class accounts_receivable_dto : INotifyPropertyChanged
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public int id_seller { get; set; }
        public string seller_name { get; set; } = string.Empty;

        /// <summary>
        /// Cliente zombie (customer.is_ghost). El reporte anual de CxC los coloca
        /// primero, antes que la cartera organica del mismo vendedor.
        /// </summary>
        public bool is_customer_ghost { get; set; }
        public int? id_zona { get; set; }
        public string zone_name { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public DateTime? dispatch_date { get; set; }
        public decimal total_amount_usd { get; set; }
        public decimal gross_total_usd { get; set; }
        public decimal discount_amount { get; set; }
        public decimal? discount_percentage { get; set; }
        public decimal? volume_discount_percentage { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public DateTime? last_payment_date { get; set; }
        public string payment_method_text { get; set; } = string.Empty;
        public string bank_name_text { get; set; } = string.Empty;
        private string _status = string.Empty;
        private string _note_type_name = string.Empty;
        private string _note_type_code = string.Empty;

        public string status
        {
            get => _status;
            set { _status = value ?? string.Empty; on_property_changed(); }
        }

        public string note_type_name
        {
            get => _note_type_name;
            set
            {
                _note_type_name = value ?? string.Empty;
                on_property_changed();
                on_property_changed(nameof(note_type_display));
                on_property_changed(nameof(sales_observations_display));
            }
        }

        public string note_type_code
        {
            get => _note_type_code;
            set { _note_type_code = value ?? string.Empty; on_property_changed(); }
        }

        /// <summary>
        /// Tipo de nota para mostrar en su propia columna. Cuando la nota no tiene tipo
        /// asignado se muestra "General", que es la misma convencion del resto de la app.
        /// </summary>
        public string note_type_display => _note_type_name.Length == 0 ? "General" : _note_type_name;

        private string _cxc_observations = string.Empty;
        private string _sales_observations = string.Empty;

        public string cxc_observations
        {
            get => _cxc_observations;
            set { _cxc_observations = value ?? string.Empty; on_property_changed(); }
        }

        public string sales_observations
        {
            get => _sales_observations;
            set
            {
                _sales_observations = value ?? string.Empty;
                on_property_changed();
                on_property_changed(nameof(sales_observations_display));
            }
        }

        /// <summary>
        /// Observacion de ventas para mostrar en la tabla. Lo que en realidad se esta
        /// mostrando es el tipo de nota, asi que no se repite: el tipo ya sale en su
        /// columna propia. Cubre los dos casos: nota con tipo asignado, y nota sin tipo
        /// (donde la app usa "General" como tipo por defecto).
        ///
        /// No se borra nada de la base, esto es solo lo que se ve.
        /// </summary>
        public string sales_observations_display
        {
            get
            {
                if (_sales_observations.Length == 0) return string.Empty;
                if (_sales_observations.Equals(_note_type_name, StringComparison.OrdinalIgnoreCase)) return string.Empty;
                if (_note_type_name.Length == 0 &&
                    _sales_observations.Equals("General", StringComparison.OrdinalIgnoreCase)) return string.Empty;
                return _sales_observations;
            }
        }

        public string? saved_observations { get; set; }

        private bool _is_editing_observations;

        public bool is_editing_observations
        {
            get => _is_editing_observations;
            set { _is_editing_observations = value; on_property_changed(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void on_property_changed([CallerMemberName] string? property_name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        }
    }
}