using System.Threading.Tasks;

namespace NinOS.Infrastructure.Repositories.Interfaces
{
    public interface IGenericRepository<t_entity> where t_entity : class
    {
        Task add_async(t_entity entity);
        Task<t_entity?> get_by_id_async(int id);
        Task<t_entity[]> get_all_async();
        Task update_async(t_entity entity);
        Task delete_async(t_entity entity);
    }
}