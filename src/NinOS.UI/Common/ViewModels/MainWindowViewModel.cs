using System;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public DeliveryNotesViewModel delivery_notes_vm { get; }
        public AccountsReceivableViewModel accounts_receivable_vm { get; }
        public SalesViewModel sales_vm { get; }
        public PaymentsViewModel payments_vm { get; }
        public CommissionsViewModel commissions_vm { get; }
        public InventoryViewModel inventory_vm { get; }

        public MainWindowViewModel(
            DeliveryNotesViewModel delivery_notes,
            AccountsReceivableViewModel accounts_receivable,
            SalesViewModel sales,
            PaymentsViewModel payments,
            CommissionsViewModel commissions,
            InventoryViewModel inventory)
        {
            if (delivery_notes == null) throw new ArgumentNullException(nameof(delivery_notes));
            if (accounts_receivable == null) throw new ArgumentNullException(nameof(accounts_receivable));
            if (sales == null) throw new ArgumentNullException(nameof(sales));
            if (payments == null) throw new ArgumentNullException(nameof(payments));
            if (commissions == null) throw new ArgumentNullException(nameof(commissions));
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));

            delivery_notes_vm = delivery_notes;
            accounts_receivable_vm = accounts_receivable;
            sales_vm = sales;
            payments_vm = payments;
            commissions_vm = commissions;
            inventory_vm = inventory;
        }
    }
}