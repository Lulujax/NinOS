using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Repositories.Interfaces
{
    public interface ICreditNoteRepository : IGenericRepository<credit_note>
    {
        Task<string> get_next_credit_correlative_async(int id_seller);
    }
}