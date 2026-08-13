namespace NinOS.Domain.ViewModels
{
    public class accounts_receivable_dto
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public decimal total_amount_usd { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public string status { get; set; } = string.Empty;
    }
}