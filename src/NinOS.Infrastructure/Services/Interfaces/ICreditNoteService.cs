using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface ICreditNoteService
    {
        Task<string> generate_credit_correlative_async(int id_seller);
        Task<IEnumerable<string>> get_credit_note_months_async();
        Task<IEnumerable<seller>> get_sellers_async();
        Task<IEnumerable<credit_note_dto>> get_all_credit_notes_async();
        Task<IEnumerable<credit_note_dto>> get_credit_notes_by_month_async(string month_year);
        Task<IEnumerable<credit_note_dto>> get_credit_notes_by_month_and_seller_async(string month_year, int id_seller);
        Task<credit_note_source_dto?> get_credit_source_by_note_number_async(string note_number);
        Task<credit_note_dto> create_credit_note_async(credit_note new_note, IEnumerable<credit_note_detail> details);
        Task<IEnumerable<credit_note_detail_dto>> get_credit_note_details_async(int id_credit_note);
        Task<note_print_dto> get_printable_credit_note_async(int id_credit_note);
    }
}