using System;

namespace NinOS.Domain
{
    public class delivery_note
    {
        private int _id_delivery_note;
        private string _note_number;
        private DateTime _creation_date;
        private int _id_seller;
        private int _id_customer;
        private decimal _total_amount_usd;
        private decimal _adjusted_total_usd;
        private string _status;

        public int id_delivery_note
        {
            get { return _id_delivery_note; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_delivery_note = value;
            }
        }

        public string note_number
        {
            get { return _note_number; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _note_number = value;
            }
        }

        public DateTime creation_date
        {
            get { return _creation_date; }
            set
            {
                if (value == default) throw new ArgumentException();
                _creation_date = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            }
        }

        public int id_seller
        {
            get { return _id_seller; }
            set
            {
                if (value <= 0) throw new ArgumentException();
                _id_seller = value;
            }
        }

        public int id_customer
        {
            get { return _id_customer; }
            set
            {
                if (value <= 0) throw new ArgumentException();
                _id_customer = value;
            }
        }

        public decimal total_amount_usd
        {
            get { return _total_amount_usd; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _total_amount_usd = value;
            }
        }

        public decimal adjusted_total_usd
        {
            get { return _adjusted_total_usd; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _adjusted_total_usd = value;
            }
        }

        public string status
        {
            get { return _status; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _status = value;
            }
        }

        public string? cxc_observations { get; set; }
        public string? sales_observations { get; set; }
        // Fecha en que la nota realmente se despacha (ingresada manualmente, no es la fecha de emision).
        public DateTime? dispatch_date { get; set; }
        public decimal? discount_percentage { get; set; }
        public int? note_type_id { get; set; }
        public int? id_relacion { get; set; }
        public decimal? promo_discount_percentage { get; set; }
        public decimal? volume_discount_percentage { get; set; }
        // Valores originales congelados al crear la nota. Solo los usa el PDF/vista previa.
        public decimal? original_discount_percentage { get; set; }
        public decimal? original_volume_discount_percentage { get; set; }

        protected delivery_note()
        {
            _note_number = "-";
            _status = "-";
        }

        public delivery_note(string note_number, DateTime creation_date, int id_seller, int id_customer, decimal total_amount_usd, string status, decimal adjusted_total_usd)
        {
            this.note_number = note_number;
            this.creation_date = creation_date;
            this.id_seller = id_seller;
            this.id_customer = id_customer;
            this.total_amount_usd = total_amount_usd;
            this.adjusted_total_usd = adjusted_total_usd;
            this.status = status;
        }
    }
}