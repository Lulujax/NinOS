using System;

namespace NinOS.Domain
{
    public class product
    {
        private int _id_product;
        private string _product_code;
        private string _name;
        private string _category;
        private decimal _unit_price_usd;
        private int _stock_quantity;

        public int id_product
        {
            get { return _id_product; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_product = value;
            }
        }

        public string product_code
        {
            get { return _product_code; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _product_code = value;
            }
        }

        public string name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _name = value;
            }
        }

        public string category
        {
            get { return _category; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _category = value;
            }
        }

        public decimal unit_price_usd
        {
            get { return _unit_price_usd; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _unit_price_usd = value;
            }
        }

        public int stock_quantity
        {
            get { return _stock_quantity; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _stock_quantity = value;
            }
        }

        protected product()
        {
            _product_code = "-";
            _name = "-";
            _category = "-";
        }

        public product(string product_code, string name, string category, decimal unit_price_usd, int stock_quantity)
        {
            this.product_code = product_code;
            this.name = name;
            this.category = category;
            this.unit_price_usd = unit_price_usd;
            this.stock_quantity = stock_quantity;
        }
    }
}