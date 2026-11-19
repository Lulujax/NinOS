using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IProVentaService
    {
        List<pro_venta_week_info> build_weeks(int year, int month);
        Task<pro_venta_weekly_dto> get_weekly_report_async(pro_venta_week_info week);
        Task<pro_venta_weekly_dto> get_relation_report_async(int id_relacion);
        Task<List<pro_venta_weekly_row>> get_relation_notes_async(int id_relacion);
        Task<List<pro_venta_relation_row>> get_pending_relations_async();
        Task<List<pro_venta_relation_row>> get_paid_relations_async();
        Task<List<pro_venta_relation_option>> get_all_relations_async();
        Task update_note_amount_async(int id_delivery_note, decimal monto_a_cobrar, decimal monto_esperado);
        Task update_note_discount_async(int id_delivery_note, decimal descuento, decimal monto_esperado);
        Task<List<pro_venta_relation_row>> get_relations_at_cutoff_async(DateTime fecha_corte);
    }
}