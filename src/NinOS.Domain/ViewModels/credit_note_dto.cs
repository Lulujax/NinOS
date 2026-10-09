using System;
using System.Collections.Generic;
using System.Globalization;

namespace NinOS.Domain.ViewModels
{
    // Una Nota de Credito en la lista de la pestana.
    public class credit_note_dto
    {
        public int id_credit_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string source_note_number { get; set; } = string.Empty;
        public int id_delivery_note { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public int id_seller { get; set; }
        public string seller_name { get; set; } = string.Empty;
        public int? id_zona { get; set; }
        public string zone_name { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public decimal total_amount_usd { get; set; }
        public string status { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string note_type_name { get; set; } = string.Empty;

        /// <summary>
        /// La categoria que se ve en la grilla. Una nota anulada muestra "Anulada" en vez de
        /// "Devolucion" u "Obsequio": asi queda claro de un vistazo que esa nota ya no esta
        /// vigente, sin tener que ir a buscar la columna de estado.
        /// </summary>
        public string category_display => esta_anulada
            ? "Anulada"
            : (string.IsNullOrWhiteSpace(category) ? "-" : category.Trim());

        /// <summary>Salen en rojo: las notas de credito anuladas.</summary>
        public bool esta_anulada => string.Equals(status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

        // Fecha en que se emitio la nota de credito. No es la fecha de la nota de entrega: una
        // devolucion se registra el dia que se recibe la mercancia, y el filtro por mes del
        // modulo tambien tiene que seguir a la NC, no a la nota que se esta revirtiendo.
        public string fecha_nc_display => creation_date.ToString("dd/MM/yyyy");

        // Mismo formato que arma get_credit_note_months_async en el servicio, para que el filtro
        // por mes de la barra lo pueda comparar sin andar parseando la fecha.
        public string fecha_nc_month_label => creation_date.ToString("MMMM yyyy", new CultureInfo("es-VE"));
    }

    // Nota de entrega origen + lineas devolubles para armar la Nota de Credito.
    public class credit_note_source_dto
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public int id_customer { get; set; }
        public string customer_code { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public int id_seller { get; set; }
        public string seller_name { get; set; } = string.Empty;
        public decimal adjusted_total_usd { get; set; }
        public decimal already_returned_usd { get; set; }
        public decimal paid_amount_usd { get; set; }
        public string status { get; set; } = string.Empty;
        public string note_type { get; set; } = string.Empty;

        /// <summary>
        /// Descuento de la nota de entrega, en porcentaje. La devolucion se calcula sobre el precio
        /// que el cliente realmente pago (neto), no sobre el precio de lista: devolver un producto
        /// que se facturo con 12% de descuento vale lo descontado, no el precio de catalogo.
        /// </summary>
        public decimal discount_percentage { get; set; }

        /// <summary>
        /// Cuanto mas se puede acreditar todavia en esta nota. Es el total actual de la nota
        /// menos los abonos reales ya registrados (para no generar saldo a favor).
        /// </summary>
        public decimal available_usd => Math.Max(adjusted_total_usd - paid_amount_usd, 0);

        public bool has_discount => discount_percentage > 0;

        public List<credit_note_source_line_dto> lines { get; set; } = new();
    }

    public class credit_note_source_line_dto
    {
        public int? id_product { get; set; }
        public int? id_promotion { get; set; }
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;

        /// <summary>
        /// Precio de lista de la linea en la nota de entrega. Solo informativo: es lo que se
        /// imprime en la columna IMPORTE de la pantalla.
        /// </summary>
        public decimal unit_price_usd { get; set; }

        /// <summary>
        /// Precio unitario NETO: el de lista con el descuento de la nota ya aplicado. Es el que
        /// multiplica a la cantidad devuelta para formar el importe de la nota de credito.
        /// Se calcula sobre subtotal_usd del renglon (que ya trae el precio de promocion
        /// resuelto), y no sobre unit_price_usd, para no perder el descuento de la linea.
        /// </summary>
        public decimal net_unit_price_usd { get; set; }

        public int delivered_quantity { get; set; }
        public int already_returned_quantity { get; set; }
        public int remaining_quantity { get; set; }
    }

    // Detalle de una NC ya guardada (relacion de lo que regreso).
    public class credit_note_detail_dto
    {
        public int id_credit_note_detail { get; set; }
        public int? id_product { get; set; }
        public int? id_promotion { get; set; }
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public int quantity { get; set; }
        public decimal unit_price_usd { get; set; }
        public decimal subtotal_usd { get; set; }
    }
}