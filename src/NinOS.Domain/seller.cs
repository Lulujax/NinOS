using System;

namespace NinOS.Domain
{
    public class seller
    {
        private int _id_seller;
        private string _full_name;
        private string _seller_code;
        private string _customer_code_prefix;

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

        public string customer_code_prefix
        {
            get { return _customer_code_prefix; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _customer_code_prefix = value;
            }
        }

        // Ultimo numero completo usado en la serie de codigos de cliente del vendedor
        // (ej: 3200005 para "3200_005"). Persistido para que un codigo borrado nunca se reutilice.
        public long last_customer_number { get; set; }

        public bool is_active { get; set; } = true;
        public DateTime? deleted_at { get; set; }
        public string? deleted_reason { get; set; }

        public virtual System.Collections.Generic.ICollection<seller_zone> seller_zones { get; set; } = new System.Collections.Generic.List<seller_zone>();

        public seller()
        {
            _full_name = "-";
            _seller_code = "-";
            _customer_code_prefix = "-";
            is_active = true;
        }

        public seller(string full_name, string seller_code, string customer_code_prefix)
        {
            this.full_name = full_name;
            this.seller_code = seller_code;
            this.customer_code_prefix = customer_code_prefix;
            is_active = true;
        }
    }
}