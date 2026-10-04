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
        private CreditNotesViewModel? _credit_notes_vm;

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

        public CreditNotesViewModel? credit_notes_vm
        {
            get => _credit_notes_vm;
            set { _credit_notes_vm = value; on_property_changed(); }
        }

        private SellersViewModel? _sellers_vm;
        public SellersViewModel? sellers_vm
        {
            get => _sellers_vm;
            set { _sellers_vm = value; on_property_changed(); }
        }

        public MainWindowViewModel(
            DeliveryNotesViewModel deliveryNotesVm,
            AccountsReceivableViewModel accountsReceivableVm,
            SalesViewModel salesVm,
            PaymentsViewModel paymentsVm,
            CommissionsViewModel commissionsVm,
            CustomerViewModel customerVm,
            InventoryViewModel inventoryVm,
            ProVentaViewModel proVentaVm,
            CreditNotesViewModel creditNotesVm,
            SellersViewModel sellersVm)
        {
            delivery_notes_vm = deliveryNotesVm;
            accounts_receivable_vm = accountsReceivableVm;
            sales_vm = salesVm;
            payments_vm = paymentsVm;
            commissions_vm = commissionsVm;
            customer_vm = customerVm;
            inventory_vm = inventoryVm;
            pro_venta_vm = proVentaVm;
            credit_notes_vm = creditNotesVm;
            sellers_vm = sellersVm;

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

            if (credit_notes_vm != null)
            {
                credit_notes_vm.OnCreditNoteSaved += () =>
                {
                    accounts_receivable_vm?.refresh_data();
                    payments_vm?.refresh_data();
                    inventory_vm?.refresh_data();
                    pro_venta_vm?.refresh_data();
                    credit_notes_vm?.refresh_data();
                };
            }

            // Cuando el Panel Administrador restaura o elimina clientes/productos/vendedores,
            // todos los listados se recargan para que el cambio se vea de inmediato.
            AppDataEvents.CatalogsChanged += () =>
            {
                delivery_notes_vm?.refresh_data();
                accounts_receivable_vm?.refresh_data();
                sales_vm?.refresh_data();
                payments_vm?.refresh_data();
                commissions_vm?.refresh_data();
                customer_vm?.refresh_data();
                inventory_vm?.refresh_data();
                pro_venta_vm?.refresh_data();
                credit_notes_vm?.refresh_data();
                sellers_vm?.refresh_data();
            };
        }
    }
}
