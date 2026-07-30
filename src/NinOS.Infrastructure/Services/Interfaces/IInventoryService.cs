using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<product>> get_all_products_async();
        Task add_product_async(product new_product);
        Task update_product_async(product product_to_update);
        Task delete_product_async(product product_to_delete);
        
        Task<IEnumerable<promotion>> get_all_promotions_async();
    }
}