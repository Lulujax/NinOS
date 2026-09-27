using System;

namespace NinOS.Domain
{
    // Historial de movimientos de inventario: una fila por cada vez que el stock de un
    // producto entra o sale, con el documento que lo provoco. Es la unica fuente que
    // debe usarse para mover existencias (ver stock_movement_writer).
    public class stock_movement
    {
        public const string Entrada = "ENTRADA";
        public const string Salida = "SALIDA";

        public const string RazonVenta = "VENTA";
        public const string RazonDevolucion = "DEVOLUCION";
        public const string RazonObsequio = "OBSEQUIO";
        public const string RazonAnulacion = "ANULACION";
        public const string RazonCargaInicial = "CARGA INICIAL";
        public const string RazonAjuste = "AJUSTE";

        public const string DocumentoEntrega = "NOTA DE ENTREGA";
        public const string DocumentoCredito = "NOTA DE CREDITO";
        public const string DocumentoInventario = "INVENTARIO";

        private int _id_stock_movement;
        private DateTime _movement_date;
        private int _id_product;
        private int _quantity;
        private string _movement_type;
        private string _reason;
        private string _document_type;
        private string _document_number;
        private string _sold_as;
        private decimal _unit_price_usd;

        public int id_stock_movement
        {
            get { return _id_stock_movement; }
            set { if (value < 0) throw new ArgumentException(); _id_stock_movement = value; }
        }

        // Fecha del documento que genero el movimiento.
        public DateTime movement_date
        {
            get { return _movement_date; }
            set
            {
                if (value == default) throw new ArgumentException();
                _movement_date = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            }
        }

        public int id_product
        {
            get { return _id_product; }
            set { if (value <= 0) throw new ArgumentException(); _id_product = value; }
        }

        // Siempre la magnitud en positivo; el signo lo define movement_type.
        public int quantity
        {
            get { return _quantity; }
            set { if (value <= 0) throw new ArgumentException(); _quantity = value; }
        }

        public string movement_type
        {
            get { return _movement_type; }
            set
            {
                if (value != Entrada && value != Salida) throw new ArgumentException();
                _movement_type = value;
            }
        }

        // Por que se movio: VENTA, DEVOLUCION, OBSEQUIO, ANULACION, CARGA INICIAL o AJUSTE.
        public string reason
        {
            get { return _reason; }
            set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(); _reason = value; }
        }

        public string document_type
        {
            get { return _document_type; }
            set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(); _document_type = value; }
        }

        // Numero del documento, copiado para que el historial no dependa de la nota.
        public string document_number
        {
            get { return _document_number; }
            set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(); _document_number = value; }
        }

        public string? document_status { get; set; }

        public int? id_delivery_note { get; set; }
        public int? id_credit_note { get; set; }
        public int? id_seller { get; set; }
        public int? id_customer { get; set; }

        // Promocion que consumio o devolvio el producto, si aplica.
        public int? id_promotion { get; set; }
        public int? promotion_units { get; set; }

        public string sold_as
        {
            get { return _sold_as; }
            set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(); _sold_as = value; }
        }

        public string? line_description { get; set; }

        public decimal unit_price_usd
        {
            get { return _unit_price_usd; }
            set { if (value < 0) throw new ArgumentException(); _unit_price_usd = value; }
        }

        public int signed_quantity => movement_type == Entrada ? quantity : -quantity;

        protected stock_movement()
        {
            _movement_type = "-";
            _reason = "-";
            _document_type = "-";
            _document_number = "-";
            _sold_as = "-";
        }

        public stock_movement(
            DateTime movement_date,
            int id_product,
            int quantity,
            string movement_type,
            string reason,
            string document_type,
            string document_number,
            string sold_as,
            decimal unit_price_usd)
        {
            this.movement_date = movement_date;
            this.id_product = id_product;
            this.quantity = quantity;
            this.movement_type = movement_type;
            this.reason = reason;
            this.document_type = document_type;
            this.document_number = document_number;
            this.sold_as = sold_as;
            this.unit_price_usd = unit_price_usd;
        }
    }
}
