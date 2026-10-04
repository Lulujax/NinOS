using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface ISellerService
    {
        Task<IEnumerable<seller>> GetAllActiveAsync();
        Task<IEnumerable<seller>> GetAllDeletedAsync();
        Task<seller?> GetByIdAsync(int id);
        Task<seller?> GetByCodeAsync(string code);
        Task<string> GetNextSellerCodeAsync();
        Task<seller> CreateAsync(seller newSeller, IEnumerable<int> assignedZonaIds);
        Task<seller> UpdateAsync(seller updatedSeller, IEnumerable<int> assignedZonaIds);
        Task SoftDeleteAsync(int id, string? reason);
        Task RestoreAsync(int id);
        Task<IEnumerable<zona>> GetAssignedZonasAsync(int sellerId);
    }
}
