using System;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventory_service;

        public InventoryViewModel(IInventoryService inventory_service)
        {
            if (inventory_service == null)
            {
                throw new ArgumentNullException(nameof(inventory_service));
            }
            
            _inventory_service = inventory_service;
        }
    }
}