using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain.ViewModels;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IProVentaService
    {
        Task<List<pro_venta_month_option>> get_available_months_async();
        List<pro_venta_week_info> build_weeks(int year, int month);
        Task<pro_venta_weekly_dto> get_weekly_report_async(pro_venta_week_info week);
        Task<List<pro_venta_weekly_row>> get_relation_notes_async(int id_relacion);
        Task<List<pro_venta_relation_row>> get_pending_relations_async();
        Task<List<pro_venta_relation_row>> get_paid_relations_async();
    }
}