using System;
using System.Linq;

namespace NinOS.Domain
{
    public class seller
    {
        private int _id_seller;
        private string _full_name;
        private string _seller_code;
        private string _customer_code_prefix;
        private decimal _commission_percentage = 0.10m;

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
        public bool can_receive_commissions { get; set; } = true;

        public decimal commission_percentage
        {
            get { return _commission_percentage; }
            set
            {
                if (value < 0) throw new ArgumentException("El porcentaje de comisión no puede ser negativo.");
                _commission_percentage = value;
            }
        }
        public DateTime? deleted_at { get; set; }
        public string? deleted_reason { get; set; }

        public virtual System.Collections.Generic.ICollection<seller_zone> seller_zones { get; set; } = new System.Collections.Generic.List<seller_zone>();

        /// <summary>
        /// Indica si el vendedor opera exclusivamente en la modalidad Pro Venta
        /// (tiene al menos una zona asignada y todas sus zonas asignadas son Pro Venta,
        /// sin ninguna zona General).
        /// </summary>
        public bool is_pro_venta_only =>
            seller_zones != null &&
            seller_zones.Count > 0 &&
            seller_zones.All(sz => sz.zona != null && sz.zona.is_pro_venta);

        public seller()
        {
            _full_name = "-";
            _seller_code = "-";
            _customer_code_prefix = "-";
            is_active = true;
            can_receive_commissions = true;
        }

        public seller(string full_name, string seller_code, string customer_code_prefix, decimal commission_percentage = 0.10m)
        {
            this.full_name = full_name;
            this.seller_code = seller_code;
            this.customer_code_prefix = customer_code_prefix;
            this.commission_percentage = commission_percentage;
            is_active = true;
            can_receive_commissions = true;
        }
    }
}