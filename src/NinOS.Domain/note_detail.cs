using System;

namespace NinOS.Domain
{
    public class note_detail
    {
        private int _id_note_detail;
        private int _id_delivery_note;
        private int? _id_product;
        private int? _id_promotion;
        private int _quantity;
        private decimal _unit_price_usd;
        private decimal _subtotal_usd;

        public int id_note_detail
        {
            get { return _id_note_detail; }
            set { if (value < 0) throw new ArgumentException(); _id_note_detail = value; }
        }

        public int id_delivery_note
        {
            get { return _id_delivery_note; }
            set { if (value <= 0) throw new ArgumentException(); _id_delivery_note = value; }
        }

        public int? id_product
        {
            get { return _id_product; }
            set { if (value != null && value <= 0) throw new ArgumentException(); _id_product = value; }
        }

        public int? id_promotion
        {
            get { return _id_promotion; }
            set { if (value != null && value <= 0) throw new ArgumentException(); _id_promotion = value; }
        }

        public int quantity
        {
            get { return _quantity; }
            set { if (value <= 0) throw new ArgumentException(); _quantity = value; }
        }

        public decimal unit_price_usd
        {
            get { return _unit_price_usd; }
            set { if (value < 0) throw new ArgumentException(); _unit_price_usd = value; }
        }

        public decimal subtotal_usd
        {
            get { return _subtotal_usd; }
            set { if (value < 0) throw new ArgumentException(); _subtotal_usd = value; }
        }

        protected note_detail() { }

        public note_detail(int id_delivery_note, int? id_product, int? id_promotion, int quantity, decimal unit_price_usd, decimal subtotal_usd)
        {
            if (id_product == null && id_promotion == null) throw new ArgumentException("La linea debe tener un producto o una promocion.");
            
            this.id_delivery_note = id_delivery_note;
            this.id_product = id_product;
            this.id_promotion = id_promotion;
            this.quantity = quantity;
            this.unit_price_usd = unit_price_usd;
            this.subtotal_usd = subtotal_usd;
        }
    }
}