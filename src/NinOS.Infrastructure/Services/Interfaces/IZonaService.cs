using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IZonaService
    {
        Task<List<zona>> GetAllAsync(bool includeInactive = false);
        Task<List<zona>> GetActiveAsync();
        Task<List<zona>> GetDeletedAsync();
        Task<zona?> GetByIdAsync(int id);
        Task<zona?> GetByCodeAsync(string code);
        Task CreateAsync(zona zona);
        Task UpdateAsync(zona zona);
        Task DeleteAsync(int id, string reason);
        Task RestoreAsync(int id);
        Task<bool> ExistsActiveAsync(string name, string code, int? excludeId = null);
        Task<string> GetNextCodeAsync();
    }
}
