using System;
using System.Collections.Generic;

namespace NinOS.Domain.ViewModels
{
    public class CustomerHistoryProductDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal LastPriceUsd { get; set; }
        public DateTime LastPurchaseDate { get; set; }
        public decimal TotalAmountUsd { get; set; }
    }

    public class CustomerHistoryDataDto
    {
        public customer Customer { get; set; } = null!;
        public List<accounts_receivable_dto> DeliveryNotes { get; set; } = new();
        public List<credit_note_dto> CreditNotes { get; set; } = new();
        public List<payment_dto> Payments { get; set; } = new();
        public List<CustomerHistoryProductDto> TopProducts { get; set; } = new();

        public decimal TotalInvoicedUsd { get; set; }
        public decimal TotalPaidUsd { get; set; }
        public decimal BalanceDueUsd { get; set; }
        public int TotalNotesCount { get; set; }
        public int TotalCreditNotesCount { get; set; }
        public decimal TotalCreditNotesUsd { get; set; }
    }
}
