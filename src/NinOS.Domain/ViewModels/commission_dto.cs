using System;

using System.Collections.Generic;



namespace NinOS.Domain.ViewModels

{

    public class commission_note_payment_dto

    {

        public DateTime payment_date { get; set; }

        public decimal amount_bs { get; set; }

        public decimal amount_usd { get; set; }

        public string reference_number { get; set; } = string.Empty;

        public bool is_abono { get; set; }

    }



    public class commission_dto

    {

        public int id_commission { get; set; }

        public int id_seller { get; set; }

        public string seller_name { get; set; } = string.Empty;

        public int id_delivery_note { get; set; }

        public string note_number { get; set; } = string.Empty;

        public string customer_name { get; set; } = string.Empty;

        public DateTime creation_date { get; set; }

        public decimal commission_percentage { get; set; }

        public decimal amount_usd { get; set; }

        public decimal paid_amount_usd { get; set; }

        public decimal amount_bs { get; set; }

        public decimal exchange_rate { get; set; }

        public string reference_number { get; set; } = string.Empty;

        public string bank_name { get; set; } = string.Empty;

        public bool is_paid { get; set; }

        public DateTime? payout_date { get; set; }

        public DateTime? note_last_payment_date { get; set; }

        public List<commission_note_payment_dto> note_payments { get; set; } = new();

        public string note_payments_text { get; set; } = string.Empty;

        public decimal invoiced_amount { get; set; }

        public decimal paid_amount { get; set; }

        public decimal? discount_percentage { get; set; }

        public DateTime? commission_paid_date { get; set; }

        public bool is_selected { get; set; }

    }

}

