using System;

namespace NinOS.Domain
{
    public class payment
    {
        private int _id_payment;
        private int _id_delivery_note;
        private DateTime _payment_date;
        private decimal _amount_usd;
        private decimal _amount_bs;
        private decimal? _exchange_rate;
        private string _payment_type = string.Empty;
        private string _reference_number = string.Empty;

        public int id_payment
        {
            get { return _id_payment; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_payment = value;
            }
        }

        public int id_delivery_note
        {
            get { return _id_delivery_note; }
            set
            {
                if (value <= 0) throw new ArgumentException();
                _id_delivery_note = value;
            }
        }

        public DateTime payment_date
        {
            get { return _payment_date; }
            set
            {
                if (value == default) throw new ArgumentException();
                _payment_date = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            }
        }

        public decimal amount_usd
        {
            get { return _amount_usd; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _amount_usd = value;
            }
        }

        public decimal amount_bs
        {
            get { return _amount_bs; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _amount_bs = value;
            }
        }

        public decimal? exchange_rate
        {
            get { return _exchange_rate; }
            set { _exchange_rate = value; }
        }

        public string payment_type
        {
            get { return _payment_type; }
            set { _payment_type = value ?? string.Empty; }
        }

        public string reference_number
        {
            get { return _reference_number; }
            set { _reference_number = value ?? string.Empty; }
        }

        protected payment()
        {
        }

        public payment(int id_delivery_note, DateTime payment_date, decimal amount_usd, decimal amount_bs, decimal? exchange_rate, string payment_type = "", string reference_number = "")
        {
            this.id_delivery_note = id_delivery_note;
            this.payment_date = payment_date;
            this.amount_usd = amount_usd;
            this.amount_bs = amount_bs;
            this.exchange_rate = exchange_rate;
            this.payment_type = payment_type;
            this.reference_number = reference_number;
        }
    }
}