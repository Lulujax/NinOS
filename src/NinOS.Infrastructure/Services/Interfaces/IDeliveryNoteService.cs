using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IDeliveryNoteService
    {
        Task<IEnumerable<delivery_note>> get_all_notes_async();
        Task create_delivery_note_async(delivery_note new_note, IEnumerable<note_detail> details);
        Task<string> generate_correlative_async(int id_seller);
    }
}