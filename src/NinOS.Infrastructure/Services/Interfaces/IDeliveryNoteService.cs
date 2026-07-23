using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IDeliveryNoteService
    {
        Task create_delivery_note_async(delivery_note new_note, note_detail[] details);
    }
}