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
        public string status { get; set; } = string.Empty;

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
            set { _sales_observations = value ?? string.Empty; on_property_changed(); }
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