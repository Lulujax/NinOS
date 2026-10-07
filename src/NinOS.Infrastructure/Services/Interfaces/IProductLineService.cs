using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IProductLineService
    {
        Task<List<product_line>> GetAllAsync(bool includeInactive = false);
        Task<List<product_line>> GetActiveAsync();
        Task<List<product_line>> GetDeletedAsync();
        Task<product_line?> GetByIdAsync(int id);
        Task<product_line?> GetByCodePrefixAsync(string codePrefix);
        Task CreateAsync(product_line productLine);
        Task UpdateAsync(product_line productLine);
        Task DeleteAsync(int id, string reason);
        Task RestoreAsync(int id);
        Task<bool> ExistsActiveAsync(string name, string codePrefix, int? excludeId = null);
        Task<bool> HasProductsAsync(int id);
    }
}
