using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IPriceListService
    {
        /// <summary>Solo las listas creadas por el usuario (sin la Principal).</summary>
        Task<List<price_list>> GetAllAsync(bool includeInactive = false);
        Task<List<price_list>> GetActiveAsync();
        Task<price_list?> GetByIdAsync(int id);

        /// <summary>
        /// La lista Principal del sistema. Siempre existe y se crea sola si la base
        /// viene de una version anterior. Se puede editar, nunca eliminar.
        /// </summary>
        Task<price_list> GetPrincipalAsync();
        Task<price_list> CreateAsync(string name, string? description = null);
        Task UpdateAsync(int id, string name, string? description);

        /// <summary>
        /// Elimina una lista. Si ya fue usada en notas emitidas se desactiva en vez
        /// de borrarse, para no romper el historial. La lista Principal no puede
        /// eliminarse.
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Guarda el precio base (lista Principal) de un producto. Es el precio que
        /// heredan todas las listas que no tengan un precio propio.
        /// </summary>
        Task SavePrincipalPriceAsync(int idProduct, decimal price);

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

        /// <summary>
        /// Obtiene todos los productos activos con su precio para una lista dada.
        /// Si idPriceList es null, retorna todos los productos con su precio base original.
        /// </summary>
        Task<List<product_price_dto>> GetProductPricesForListAsync(int? idPriceList);

        /// <summary>
        /// Guarda de forma masiva los precios de varios productos en una lista específica.
        /// </summary>
        Task SaveProductPricesBulkAsync(int idPriceList, IEnumerable<(int idProduct, decimal? price)> priceUpdates);
    }
}
