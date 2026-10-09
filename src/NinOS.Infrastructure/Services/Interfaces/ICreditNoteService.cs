using System;
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
        Task<IEnumerable<string>> get_delivery_note_months_for_seller_async(int id_seller);
        Task<IEnumerable<accounts_receivable_dto>> get_delivery_notes_for_credit_async(int id_seller, string? month_year = null);
        Task<credit_note_dto> create_credit_note_async(credit_note new_note, IEnumerable<credit_note_detail> details);
        Task<IEnumerable<credit_note_detail_dto>> get_credit_note_details_async(int id_credit_note);
        Task<note_print_dto> get_printable_credit_note_async(int id_credit_note);
        Task<credit_note_report_dto> get_credit_note_report_async(DateTime from_date, DateTime to_date, string? category, int? id_seller);

        /// <summary>
        /// Anula una nota de credito, sea de devolucion o de obsequio. Revierte lo que hizo al
        /// crearse: devuelve el stock que habia ingresado y, si era devolucion, restaura el monto que se
        /// habia descontado de la nota de entrega. Nunca borra la nota, queda con status "Anulada".
        /// </summary>
        Task annul_credit_note_async(int id_credit_note);
    }
}