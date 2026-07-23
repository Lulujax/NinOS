using System;

namespace NinOS.Domain
{
    public class seller
    {
        private int _id_seller;
        private string _full_name;
        private string _seller_code;

        public int id_seller
        {
            get { return _id_seller; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_seller = value;
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

        public string seller_code
        {
            get { return _seller_code; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _seller_code = value;
            }
        }

        protected seller()
        {
            _full_name = "-";
            _seller_code = "-";
        }

        public seller(string full_name, string seller_code)
        {
            this.full_name = full_name;
            this.seller_code = seller_code;
        }
    }
}