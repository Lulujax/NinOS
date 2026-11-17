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

        public VesuprontaViewModel(
            IServiceScopeFactory scope_factory,
            ISellerService seller_service,
            IZonaService zona_service)
        {
            ArgumentNullException.ThrowIfNull(scope_factory);
            ArgumentNullException.ThrowIfNull(seller_service);
            ArgumentNullException.ThrowIfNull(zona_service);

            AppLog.Info("Creando pestana Vesupronta con servicios de empresa Vesupronta.");

            var receivable_service = new AccountsReceivableService(scope_factory, business_unit.Vesupronta);
            var payment_service = new PaymentService(scope_factory, business_unit.Vesupronta);

            sales_vm = new SalesViewModel(receivable_service, seller_service, zona_service, business_unit.Vesupronta);
            accounts_receivable_vm = new AccountsReceivableViewModel(receivable_service, seller_service, zona_service, business_unit.Vesupronta);
            payments_vm = new PaymentsViewModel(payment_service, receivable_service, seller_service, zona_service, business_unit.Vesupronta);
        }

        public void refresh_data()
        {
            sales_vm.refresh_data();
            accounts_receivable_vm.refresh_data();
            payments_vm.refresh_data();
        }
    }
}
