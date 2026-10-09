using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface IProductLineService
    {
        Task<List<product_line>> GetAllAsync(bool includeInactive = false);
        Task<List<product_line>> GetActiveAsync();
        Task<List<product_line>> GetDeletedAsync();
        Task<product_line?> GetByIdAsync(int id);
        Task<product_line?> GetByCodePrefixAsync(string codePrefix);
        Task CreateAsync(product_line productLine);
        Task UpdateAsync(product_line productLine);
        Task DeleteAsync(int id, string reason);
        Task RestoreAsync(int id);
        Task<bool> ExistsActiveAsync(string name, string codePrefix, int? excludeId = null);
        Task<bool> HasProductsAsync(int id);

        /// <summary>
        /// Resumen de lo que se reescribiria si la linea pasara de <paramref name="old_prefix"/>
        /// a <paramref name="new_prefix"/>. Sirve para mostrarle al usuario el alcance antes de
        /// confirmar una migracion de codigos.
        /// </summary>
        Task<product_code_migration_preview> preview_prefix_change_async(int id_product_line, string old_prefix, string new_prefix);

        /// <summary>
        /// Reescribe el prefijo de todos los productos de la linea, conservando el correlativo
        /// de cada uno, y deja el historial alineado: los snapshots de las notas y notas de
        /// credito, y los documentos de ajuste del kardex que lo tenian embebido.
        /// </summary>
        Task<product_code_migration_result> migrate_prefix_async(int id_product_line, string old_prefix, string new_prefix);
    }
}
