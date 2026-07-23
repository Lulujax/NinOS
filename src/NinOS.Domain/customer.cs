using System;

namespace NinOS.Domain
{
    public class customer
    {
        private int _id_customer;
        private string _customer_code;
        private string _full_name;
        private string _phone_number;
        private string _address;

        public int id_customer
        {
            get { return _id_customer; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_customer = value;
            }
        }

        public string customer_code
        {
            get { return _customer_code; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _customer_code = value;
            }
        }

        public string full_name
        {
            get { return _full_name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _full_name = value;
            }
        }

        public string phone_number
        {
            get { return _phone_number; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _phone_number = value;
            }
        }

        public string address
        {
            get { return _address; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _address = value;
            }
        }

        protected customer()
        {
            _customer_code = "-";
            _full_name = "-";
            _phone_number = "-";
            _address = "-";
        }

        public customer(string customer_code, string full_name, string phone_number, string address)
        {
            this.customer_code = customer_code;
            this.full_name = full_name;
            this.phone_number = phone_number;
            this.address = address;
        }
    }
}