using System;
using System.Collections.Generic;

namespace NinOS.Domain.ViewModels
{
    public class pro_venta_month_option
    {
        public DateTime value { get; set; }
        public string label { get; set; } = string.Empty;

        public override string ToString()
        {
            return label;
        }
    }

    public class pro_venta_week_info
    {
        public int week_index { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string label { get; set; } = string.Empty;
    }

    public class pro_venta_weekly_row
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public decimal commission_luis { get; set; }
        public decimal gastos_25 { get; set; }
        public decimal gastos_15 { get; set; }
public string payment_date_display { get; set; } = string.Empty;
      public string payment_method_display { get; set; } = string.Empty;
      
      /// <summary>
      /// Vendedor al que pertenece la nota. La tabla Pro Venta mezcla vendedores (Anais 3300 y
      /// Juan Luis 3400 tienen notas Pro Venta, y hay relaciones mezcladas), asi que sin esto no
      /// se puede saber de quien es la nota que se esta anulando.
      /// </summary>
      public string seller_name { get; set; } = string.Empty;
      public string seller_code { get; set; } = string.Empty;

        /// <summary>
        /// Estado de la nota (Pendiente, Pagada, Anulada). Antes no se traia, asi que el historial
        /// no podia distinguir una nota anulada de una vigente.
        /// </summary>
        public string status { get; set; } = string.Empty;

        public bool esta_anulada => string.Equals(status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

        public string status_display => esta_anulada ? "ANULADA" : string.Empty;
    }

    public class pro_venta_relation_row
    {
        public int id_relacion { get; set; }
        public int relation_number { get; set; }
        public DateTime week_start { get; set; }
        public DateTime week_end { get; set; }
        public decimal amount { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public string status { get; set; } = string.Empty;

        /// <summary>
        /// Notas de la relacion que siguen vigentes. Las anuladas no se cuentan.
        /// </summary>
        public int note_count { get; set; }

        /// <summary>
        /// Lo que suman las notas anuladas de la relacion. Se lleva aparte del monto para poder
        /// mostrarlas sin que entren en el total ni en la liquidacion.
        /// </summary>
        public decimal annulled_amount { get; set; }
        public int annulled_count { get; set; }
        public bool has_annulled => annulled_count > 0;

        public string relation_label => $"NRO {relation_number} ({week_start:dd/MM} AL {week_end:dd/MM})";
    }

    public class pro_venta_relation_option
    {
        public int id_relacion { get; set; }
        public int relation_number { get; set; }
        public DateTime week_start { get; set; }
        public DateTime week_end { get; set; }
        public string label { get; set; } = string.Empty;

        public override string ToString()
        {
            return label;
        }
    }

    public class pro_venta_weekly_dto
    {
        public int id_relacion { get; set; }
        public int relation_number { get; set; }
        public DateTime week_start { get; set; }
        public DateTime week_end { get; set; }
        public string city { get; set; } = "MARACAY";
        public List<pro_venta_weekly_row> rows { get; set; } = new();

        /// <summary>
        /// Totales de la semana. Cagan solo las notas vigentes: las anuladas se siguen mostrando en
        /// la tabla, en rojo, pero no suman ni a la venta ni a la comision ni a los gastos.
        /// </summary>
        public decimal total_amount { get; set; }
        public decimal total_commission_luis { get; set; }
        public decimal total_gastos_25 { get; set; }
        public decimal total_gastos_15 { get; set; }

        /// <summary>
        /// Resumen de lo que se dejo fuera de los totales, para que la leyenda de la pantalla y
        /// del PDF pueda decir cuantas y por cuanto son.
        /// </summary>
        public decimal annulled_amount { get; set; }
        public int annulled_count { get; set; }
        public bool has_annulled => annulled_count > 0;
    }
}