using System;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class CommissionsViewModel : ViewModelBase
    {
        private readonly ICommissionService _commission_service;

        public CommissionsViewModel(ICommissionService commission_service)
        {
            if (commission_service == null)
            {
                throw new ArgumentNullException(nameof(commission_service));
            }
            
            _commission_service = commission_service;
        }
    }
}