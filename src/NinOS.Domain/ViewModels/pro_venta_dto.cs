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
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public decimal commission_luis { get; set; }
        public decimal gastos_25 { get; set; }
        public decimal gastos_15 { get; set; }
    }

    public class pro_venta_pending_row
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public string status { get; set; } = string.Empty;
    }

    public class pro_venta_weekly_dto
    {
        public int relation_number { get; set; }
        public DateTime week_start { get; set; }
        public DateTime week_end { get; set; }
        public string city { get; set; } = "MARACAY";
        public List<pro_venta_weekly_row> rows { get; set; } = new();
        public decimal total_amount { get; set; }
        public decimal total_commission_luis { get; set; }
        public decimal total_gastos_25 { get; set; }
        public decimal total_gastos_15 { get; set; }
    }
}