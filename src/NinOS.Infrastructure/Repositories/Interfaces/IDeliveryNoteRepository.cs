using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Repositories.Interfaces
{
    public interface IDeliveryNoteRepository : IGenericRepository<delivery_note>
    {
        Task<string> get_next_correlative_async(int id_seller);
    }
}