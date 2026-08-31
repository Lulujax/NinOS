using System;

namespace NinOS.Domain
{
    public class commission_payment
    {
        private int _id_commission_payment;
        private int _id_commission;
        private decimal _amount_usd;
        private decimal _amount_bs;
        private decimal _exchange_rate;
        private string _payment_type = string.Empty;
        private string _reference_number = string.Empty;
        private string _bank_name = string.Empty;
        private string _observations = string.Empty;
        private DateTime _payment_date;

        public int id_commission_payment
        {
            get { return _id_commission_payment; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_commission_payment = value;
            }
        }

        public int id_commission
        {
            get { return _id_commission; }
            set
            {
                if (value <= 0) throw new ArgumentException();
                _id_commission = value;
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

        public decimal exchange_rate
        {
            get { return _exchange_rate; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _exchange_rate = value;
            }
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

        public string bank_name
        {
            get { return _bank_name; }
            set { _bank_name = value ?? string.Empty; }
        }

        public string observations
        {
            get { return _observations; }
            set { _observations = value ?? string.Empty; }
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

        protected commission_payment()
        {
        }

        public commission_payment(int id_commission, decimal amount_usd, decimal amount_bs, decimal exchange_rate, string payment_type, string reference_number, DateTime payment_date, string bank_name = "", string observations = "")
        {
            this.id_commission = id_commission;
            this.amount_usd = amount_usd;
            this.amount_bs = amount_bs;
            this.exchange_rate = exchange_rate;
            this.payment_type = payment_type;
            this.reference_number = reference_number;
            this.payment_date = payment_date;
            this.bank_name = bank_name;
            this.observations = observations;
        }
    }
}