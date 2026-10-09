using System;

namespace NinOS.Domain.ViewModels
{
    public class product_sales_history_dto
    {
        public int? id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public string line_description { get; set; } = string.Empty;
        public string sold_as { get; set; } = string.Empty;
        public int units_sold { get; set; }
        public decimal unit_price_usd { get; set; }
        public decimal line_subtotal_usd { get; set; }
        public string status { get; set; } = string.Empty;
        public string movement_type { get; set; } = string.Empty;
        public string movement_reason { get; set; } = string.Empty;
        public string document_type { get; set; } = string.Empty;
        public bool is_credit_note { get; set; }

        // La fecha del movimiento se guarda en UTC. Sin convertir a hora local, un movimiento
        // hecho de noche (despues de las 20:00 en Venezuela, UTC-4) se muestra con la fecha del
        // dia anterior, que es justo lo que pasaba con las notas de credito.
        public string fecha_display => AppTimeZone.to_local(creation_date).ToString("dd/MM/yyyy");
        public int signed_units => movement_type == "ENTRADA" ? units_sold : -units_sold;
        public string unidades_display => units_sold.ToString();
        public string precio_display => unit_price_usd.ToString("N2");
        public string subtotal_display => line_subtotal_usd.ToString("N2");

        // Con signo, para que en la grilla se lea "entro 5" o "salieron 3" y no un
        // numero suelto del que hay que adivinar la direccion.
        public string unidades_con_signo_display => signed_units > 0 ? "+" + signed_units : signed_units.ToString();

        // Textos listos para pintar en la grilla del historial.
        public string motivo_display => movement_reason switch
        {
            "VENTA" => "Venta",
            "DEVOLUCION" => "Devolucion",
            "OBSEQUIO" => "Obsequio",
            "ANULACION" => "Anulacion de nota",
            "CARGA INICIAL" => "Carga inicial",
            "AJUSTE" => "Ajuste manual",
            "REPOSICION" => "Reposicion",
            "MERMA" => "Merma o rotura",
            "CORRECCION CONTEO" => "Correccion de conteo",
            "DEVOLUCION PROVEEDOR" => "Devolucion a proveedor",
            "AJUSTE SISTEMA" => "Ajuste del sistema",
            _ => movement_reason
        };

        public string documento_display => document_type switch
        {
            "NOTA DE ENTREGA" => "Nota de entrega",
            "NOTA DE CREDITO" => "Nota de credito",
            "INVENTARIO" => "Inventario",
            _ => document_type
        };
    }
}
