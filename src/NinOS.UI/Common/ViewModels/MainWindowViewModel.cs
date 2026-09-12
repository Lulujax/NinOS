using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private DeliveryNotesViewModel? _delivery_notes_vm;
        private AccountsReceivableViewModel? _accounts_receivable_vm;
        private SalesViewModel? _sales_vm;
        private PaymentsViewModel? _payments_vm;
        private CommissionsViewModel? _commissions_vm;
        private CustomerViewModel? _customer_vm;
        private InventoryViewModel? _inventory_vm;
        private ProVentaViewModel? _pro_venta_vm;

        public DeliveryNotesViewModel? delivery_notes_vm
        {
            get => _delivery_notes_vm;
            set { _delivery_notes_vm = value; on_property_changed(); }
        }

        public AccountsReceivableViewModel? accounts_receivable_vm
        {
            get => _accounts_receivable_vm;
            set { _accounts_receivable_vm = value; on_property_changed(); }
        }

        public SalesViewModel? sales_vm
        {
            get => _sales_vm;
            set { _sales_vm = value; on_property_changed(); }
        }

        public PaymentsViewModel? payments_vm
        {
            get => _payments_vm;
            set { _payments_vm = value; on_property_changed(); }
        }

        public CommissionsViewModel? commissions_vm
        {
            get => _commissions_vm;
            set { _commissions_vm = value; on_property_changed(); }
        }

        public CustomerViewModel? customer_vm
        {
            get => _customer_vm;
            set { _customer_vm = value; on_property_changed(); }
        }

        public InventoryViewModel? inventory_vm
        {
            get => _inventory_vm;
            set { _inventory_vm = value; on_property_changed(); }
        }

        public ProVentaViewModel? pro_venta_vm
        {
            get => _pro_venta_vm;
            set { _pro_venta_vm = value; on_property_changed(); }
        }

        public MainWindowViewModel(
            DeliveryNotesViewModel deliveryNotesVm,
            AccountsReceivableViewModel accountsReceivableVm,
            SalesViewModel salesVm,
            PaymentsViewModel paymentsVm,
            CommissionsViewModel commissionsVm,
            CustomerViewModel customerVm,
            InventoryViewModel inventoryVm,
            ProVentaViewModel proVentaVm)
        {
            delivery_notes_vm = deliveryNotesVm;
            accounts_receivable_vm = accountsReceivableVm;
            sales_vm = salesVm;
            payments_vm = paymentsVm;
            commissions_vm = commissionsVm;
            customer_vm = customerVm;
            inventory_vm = inventoryVm;
            pro_venta_vm = proVentaVm;

            if (delivery_notes_vm != null)
            {
                delivery_notes_vm.OnNoteSaved += () =>
                {
                    accounts_receivable_vm?.refresh_data();
                    delivery_notes_vm?.refresh_data();
                    inventory_vm?.refresh_data();
                    pro_venta_vm?.refresh_data();
                };
            }
        }
    }
}
