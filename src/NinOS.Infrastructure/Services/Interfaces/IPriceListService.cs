using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IPriceListService
    {
        Task<List<price_list>> GetAllAsync(bool includeInactive = false);
        Task<List<price_list>> GetActiveAsync();
        Task<price_list?> GetByIdAsync(int id);
        Task<price_list> CreateAsync(string name);
        Task RenameAsync(int id, string newName);
        Task SetActiveAsync(int id, bool isActive);

        /// <summary>Precios propios de UN producto, indexados por id de lista.</summary>
        Task<Dictionary<int, decimal>> GetProductOwnPricesAsync(int idProduct);

        /// <summary>
        /// Precios propios de todos los productos, agrupados por id de producto y
        /// luego por id de lista. Una sola consulta: la nota y el PDF resuelven la
        /// herencia en memoria con price_resolver.
        /// </summary>
        Task<Dictionary<int, Dictionary<int, decimal>>> GetAllOwnPricesAsync();

        /// <summary>
        /// Guarda el precio propio de un producto en una lista. Con price null se
        /// elimina la fila y el producto vuelve a heredar el precio original.
        /// </summary>
        Task SaveProductPriceAsync(int idProduct, int idPriceList, decimal? price);
    }
}
