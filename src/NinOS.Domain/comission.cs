using System;

namespace NinOS.Domain
{
    public class commission
    {
        private int _id_commission;
        private int _id_seller;
        private int _id_delivery_note;
        private decimal _commission_percentage;
        private decimal _amount_usd;
        private bool _is_paid;
        private DateTime? _payout_date;

        public int id_commission
        {
            get { return _id_commission; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_commission = value;
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

        public int id_delivery_note
        {
            get { return _id_delivery_note; }
            set
            {
                if (value <= 0) throw new ArgumentException();
                _id_delivery_note = value;
            }
        }

        public decimal commission_percentage
        {
            get { return _commission_percentage; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _commission_percentage = value;
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

        public bool is_paid
        {
            get { return _is_paid; }
            set { _is_paid = value; }
        }

        public DateTime? payout_date
        {
            get { return _payout_date; }
            set { _payout_date = value; }
        }

        protected commission()
        {
        }

        public commission(int id_seller, int id_delivery_note, decimal commission_percentage, decimal amount_usd, bool is_paid, DateTime? payout_date)
        {
            this.id_seller = id_seller;
            this.id_delivery_note = id_delivery_note;
            this.commission_percentage = commission_percentage;
            this.amount_usd = amount_usd;
            this.is_paid = is_paid;
            this.payout_date = payout_date;
        }
    }
}