using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<product>> get_all_products_async();
        Task<IEnumerable<product>> get_deleted_products_async();
        Task add_product_async(product new_product);
        Task update_product_async(product product_to_update);
        Task soft_delete_product_async(int id_product, string? reason);
        Task restore_product_async(int id_product);
        Task purge_product_async(int id_product);
        Task<IEnumerable<promotion>> get_all_promotions_async();
        Task add_promotion_async(promotion new_promotion);
        Task update_promotion_async(promotion promotion_to_update);
        Task delete_promotion_async(promotion promotion_to_delete);
        Task<IEnumerable<product_sales_history_dto>> get_product_sales_history_async(int id_product);
        Task<IEnumerable<promotion_sales_history_dto>> get_promotion_sales_history_async(int id_promotion);
    }
}