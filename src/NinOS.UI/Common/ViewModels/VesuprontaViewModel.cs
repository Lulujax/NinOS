using System;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Implementations;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.UI.Common.ViewModels
{
    public class VesuprontaViewModel : ViewModelBase
    {
        public SalesViewModel sales_vm { get; }
        public AccountsReceivableViewModel accounts_receivable_vm { get; }
        public PaymentsViewModel payments_vm { get; }
        public CreditNotesViewModel credit_notes_vm { get; }

        public VesuprontaViewModel(
            IServiceScopeFactory scope_factory,
            ISellerService seller_service,
            IZonaService zona_service,
            ICustomerService customer_service,
            IInventoryService inventory_service)
        {
            ArgumentNullException.ThrowIfNull(scope_factory);
            ArgumentNullException.ThrowIfNull(seller_service);
            ArgumentNullException.ThrowIfNull(zona_service);
            ArgumentNullException.ThrowIfNull(customer_service);
            ArgumentNullException.ThrowIfNull(inventory_service);

            AppLog.Info("Creando pestana Vesupronta con servicios de empresa Vesupronta.");

            var receivable_service = new AccountsReceivableService(scope_factory, business_unit.Vesupronta);
            var payment_service = new PaymentService(scope_factory, business_unit.Vesupronta);
            var credit_note_service = new CreditNoteService(scope_factory, business_unit.Vesupronta);

            sales_vm = new SalesViewModel(receivable_service, seller_service, zona_service, business_unit.Vesupronta);
            accounts_receivable_vm = new AccountsReceivableViewModel(receivable_service, seller_service, zona_service);
            payments_vm = new PaymentsViewModel(payment_service, receivable_service, seller_service, zona_service);
            credit_notes_vm = new CreditNotesViewModel(
                credit_note_service,
                receivable_service,
                inventory_service,
                customer_service,
                seller_service,
                zona_service);

            credit_notes_vm.OnCreditNoteSaved = () =>
            {
                accounts_receivable_vm.refresh_data();
                payments_vm.refresh_data();
                credit_notes_vm.refresh_data();
            };
        }

        public void refresh_data()
        {
            sales_vm.refresh_data();
            accounts_receivable_vm.refresh_data();
            payments_vm.refresh_data();
            credit_notes_vm.refresh_data();
        }
    }
}
