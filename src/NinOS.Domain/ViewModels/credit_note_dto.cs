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
        public DateTime creation_date { get; set; }
        public decimal total_amount_usd { get; set; }
        public string status { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string note_type_name { get; set; } = string.Empty;

        // Fecha de la nota de entrega en formato mes y ano, igual que el selector del modulo.
        public string delivery_month_label => creation_date.ToString("MMMM yyyy", new CultureInfo("es-VE"));
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
        public string status { get; set; } = string.Empty;
        public string note_type { get; set; } = string.Empty;
        public List<credit_note_source_line_dto> lines { get; set; } = new();
    }

    public class credit_note_source_line_dto
    {
        public int? id_product { get; set; }
        public int? id_promotion { get; set; }
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public decimal unit_price_usd { get; set; }
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