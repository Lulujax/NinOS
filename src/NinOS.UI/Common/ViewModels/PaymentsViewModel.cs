using System;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.ViewModels
{
    public class PaymentsViewModel : ViewModelBase
    {
        private readonly IPaymentService _payment_service;

        public PaymentsViewModel(IPaymentService payment_service)
        {
            if (payment_service == null)
            {
                throw new ArgumentNullException(nameof(payment_service));
            }
            
            _payment_service = payment_service;
        }
    }
}