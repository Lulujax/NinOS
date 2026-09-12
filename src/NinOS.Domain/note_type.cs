using System;

namespace NinOS.Domain
{
    public class note_type
    {
        private int _id_note_type;
        private string _name;
        private string _code;
        private string _header_title;
        private string _calculation_type;
        private bool _discount_configurable;
        private decimal _default_discount_percentage;
        private string _conditions_template;
        private string _discount_conditions_template;

        public int id_note_type
        {
            get { return _id_note_type; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_note_type = value;
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

        public string code
        {
            get { return _code; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _code = value;
            }
        }

        public int? id_seller { get; set; }

        public string header_title
        {
            get { return _header_title; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _header_title = value;
            }
        }

        public string calculation_type
        {
            get { return _calculation_type; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _calculation_type = value;
            }
        }

        public bool discount_configurable
        {
            get { return _discount_configurable; }
            set { _discount_configurable = value; }
        }

        public decimal default_discount_percentage
        {
            get { return _default_discount_percentage; }
            set
            {
                if (value < 0 || value > 100) throw new ArgumentException();
                _default_discount_percentage = value;
            }
        }

        public decimal? promo_discount_percentage { get; set; }

        public decimal? mandatory_discount_percentage { get; set; }

        public string conditions_template
        {
            get { return _conditions_template; }
            set { _conditions_template = value; }
        }

        public string discount_conditions_template
        {
            get { return _discount_conditions_template; }
            set { _discount_conditions_template = value; }
        }

        public bool is_active { get; set; } = true;

        public int sort_order { get; set; }

        protected note_type()
        {
            _name = "-";
            _code = "-";
            _header_title = "-";
            _calculation_type = "standard";
            _conditions_template = string.Empty;
            _discount_conditions_template = string.Empty;
        }

        public note_type(string name, string code, string header_title, string calculation_type, bool discount_configurable, decimal default_discount_percentage, string conditions_template, string discount_conditions_template)
        {
            this.name = name;
            this.code = code;
            this.header_title = header_title;
            this.calculation_type = calculation_type;
            this.discount_configurable = discount_configurable;
            this.default_discount_percentage = default_discount_percentage;
            this.conditions_template = conditions_template;
            this.discount_conditions_template = discount_conditions_template;
        }
    }
}