using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<product[]> get_all_products_async();
        Task add_product_async(product new_product);
        Task update_product_async(product target_product);
    }
}