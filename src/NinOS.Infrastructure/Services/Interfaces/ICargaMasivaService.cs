using System.Collections.Generic;
using System.Threading.Tasks;
using NinOS.Domain.CargaMasiva;

namespace NinOS.Infrastructure.Services.Interfaces
{
    public interface ICargaMasivaService
    {
        Task<CabeceraPlantilla> obtener_cabecera_async(TipoPlantilla tipo, int? id_price_list);
        Task<IReadOnlyList<ProductoPlantilla>> obtener_productos_async(TipoPlantilla tipo, int? id_price_list);
        Task<VistaPreviaCarga> generar_vista_previa_async(LecturaPlantilla lectura, TipoCargaCantidad? tipo_carga);
        Task<int> aplicar_async(VistaPreviaCarga vista, bool incluir_avisos, string? motivo, string? nota);
    }
}
