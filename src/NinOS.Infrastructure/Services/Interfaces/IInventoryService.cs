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
        Task<int> restore_product_async(int id_product);
        Task<IEnumerable<promotion>> get_all_promotions_async();
        Task<IEnumerable<promotion>> get_deleted_promotions_async();
        Task add_promotion_async(promotion new_promotion);
        Task update_promotion_async(promotion promotion_to_update);
        Task<IEnumerable<promotion>> get_promotions_using_product_async(int id_product);
        Task soft_delete_promotion_async(int id_promotion, string? reason);
        Task restore_promotion_async(int id_promotion);
        Task<IEnumerable<product_sales_history_dto>> get_product_sales_history_async(int id_product);
        Task<IEnumerable<promotion_sales_history_dto>> get_promotion_sales_history_async(int id_promotion);
    }
}