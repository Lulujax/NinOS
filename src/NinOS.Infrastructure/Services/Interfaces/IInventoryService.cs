using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<product>> get_all_products_async();
        Task add_product_async(product new_product);
        Task update_product_async(product product_to_update);
        Task delete_product_async(product product_to_delete);
        Task<IEnumerable<promotion>> get_all_promotions_async();
        Task add_promotion_async(promotion new_promotion);
        Task update_promotion_async(promotion promotion_to_update);
        Task delete_promotion_async(promotion promotion_to_delete);
        Task<IEnumerable<product_sales_history_dto>> get_product_sales_history_async(int id_product);
    }
}