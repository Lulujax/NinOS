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
                _creation_date = value;
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

        public string status
        {
            get { return _status; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _status = value;
            }
        }

        protected delivery_note()
        {
            _note_number = "-";
            _status = "-";
        }

        public delivery_note(string note_number, DateTime creation_date, int id_seller, int id_customer, decimal total_amount_usd, string status)
        {
            this.note_number = note_number;
            this.creation_date = creation_date;
            this.id_seller = id_seller;
            this.id_customer = id_customer;
            this.total_amount_usd = total_amount_usd;
            this.status = status;
        }
    }
}