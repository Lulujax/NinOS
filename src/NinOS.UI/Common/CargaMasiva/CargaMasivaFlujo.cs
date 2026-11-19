using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using NinOS.Domain;
using NinOS.Domain.CargaMasiva;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Views;

namespace NinOS.UI.Common.CargaMasiva
{
    public sealed class CargaMasivaFlujo
    {
        private const string FiltroExcel = "Libro de Excel (*.xlsx)|*.xlsx";

        private readonly ICargaMasivaService _carga;
        private readonly IPriceListService _listas;

        public CargaMasivaFlujo(ICargaMasivaService carga, IPriceListService listas)
        {
            _carga = carga ?? throw new ArgumentNullException(nameof(carga));
            _listas = listas ?? throw new ArgumentNullException(nameof(listas));
        }

        public static string nombre_sugerido(CabeceraPlantilla cabecera, DateTime ahora)
        {
            if (cabecera == null) throw new ArgumentNullException(nameof(cabecera));

            string marca_de_tiempo = ahora.ToString("yyyy-MM-dd_HHmm", CultureInfo.InvariantCulture);
            if (cabecera.tipo == TipoPlantilla.Cantidad)
            {
                return "Plantilla_Cantidad_" + marca_de_tiempo + ".xlsx";
            }

            string lista = cabecera.es_principal ? "Principal" : limpiar_nombre_de_archivo(cabecera.nombre_lista);
            return "Plantilla_Precio_" + lista + "_" + marca_de_tiempo + ".xlsx";
        }

        public async Task descargar_plantilla_async(Window owner)
        {
            try
            {
                List<price_list> listas = await listas_disponibles_async();
                var ventana = new DescargarPlantillaWindow(listas) { Owner = owner };
                if (ventana.ShowDialog() != true) return;

                CabeceraPlantilla cabecera = await _carga.obtener_cabecera_async(ventana.tipo_elegido, ventana.id_lista_elegida);
                IReadOnlyList<ProductoPlantilla> productos = await _carga.obtener_productos_async(ventana.tipo_elegido, ventana.id_lista_elegida);

                var dialogo = new SaveFileDialog
                {
                    Filter = FiltroExcel,
                    FileName = nombre_sugerido(cabecera, DateTime.Now)
                };
                if (dialogo.ShowDialog(owner) != true) return;

                PlantillaInventarioExcelWriter.generar(cabecera, productos, dialogo.FileName);
                AppDialog.Show(
                    "Plantilla guardada. Escriba los valores nuevos en la columna indicada y cárguela con «Cargar plantilla».",
                    "Plantilla de inventario",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al descargar la plantilla de inventario", ex);
                AppDialog.Show("No se pudo descargar la plantilla: " + ErrorText.Get(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<bool> cargar_plantilla_async(Window owner)
        {
            try
            {
                var abrir = new OpenFileDialog { Filter = FiltroExcel, Title = "Cargar plantilla de inventario" };
                if (abrir.ShowDialog(owner) != true) return false;

                LecturaPlantilla lectura = PlantillaInventarioExcelReader.leer(abrir.FileName);
                TipoCargaCantidad? tipo_carga = elegir_tipo_carga(lectura, owner);
                if (lectura.cabecera.tipo == TipoPlantilla.Cantidad && tipo_carga == null) return false;

                VistaPreviaCarga vista = await _carga.generar_vista_previa_async(lectura, tipo_carga);
                var ventana = new VistaPreviaCargaWindow(vista) { Owner = owner };
                if (ventana.ShowDialog() != true) return false;

                string? motivo = null;
                string? nota = null;
                if (vista.cabecera.tipo == TipoPlantilla.Cantidad)
                {
                    motivo = PromptWindow.ask(owner, "Motivo del cambio", "Escriba el motivo (ejemplo: Conteo fin de mes)");
                    if (string.IsNullOrWhiteSpace(motivo)) return false;
                    nota = PromptWindow.ask(owner, "Nota (opcional)", "Escriba una nota o déjelo vacío");
                }
                else if (!confirmar_precios(vista, ventana.incluir_avisos))
                {
                    return false;
                }

                int aplicados = await _carga.aplicar_async(vista, ventana.incluir_avisos, motivo, nota);
                AppDataEvents.raise_data_changed();
                AppDataEvents.raise_catalogs_changed();

                AppDialog.Show(
                    "Se actualizaron " + aplicados + " productos.",
                    "Carga completa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return true;
            }
            catch (PlantillaInvalidaException ex)
            {
                AppDialog.Show(ex.Message, "Plantilla no válida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (CargaCambiadaException ex)
            {
                AppDialog.Show(ex.Message, "Los datos cambiaron", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al cargar la plantilla de inventario", ex);
                AppDialog.Show("No se pudo aplicar la carga: " + ErrorText.Get(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static TipoCargaCantidad? elegir_tipo_carga(LecturaPlantilla lectura, Window owner)
        {
            if (lectura.cabecera.tipo != TipoPlantilla.Cantidad) return null;

            var elegir = new ElegirTipoCargaWindow { Owner = owner };
            return elegir.ShowDialog() == true ? elegir.tipo_elegido : null;
        }

        private static bool confirmar_precios(VistaPreviaCarga vista, bool incluir_avisos)
        {
            int cambiaran = vista.cambian + (incluir_avisos ? vista.avisos : 0);
            MessageBoxResult respuesta = AppDialog.Show(
                "Se cambiarán " + cambiaran + " precios. ¿Continuar?",
                "Confirmar precios",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            return respuesta == MessageBoxResult.Yes;
        }

        private async Task<List<price_list>> listas_disponibles_async()
        {
            price_list principal = await _listas.GetPrincipalAsync();
            List<price_list> activas = await _listas.GetActiveAsync();
            return new List<price_list> { principal }.Concat(activas).ToList();
        }

        private static string limpiar_nombre_de_archivo(string nombre)
        {
            char[] invalidos = Path.GetInvalidFileNameChars();
            string con_separadores = new string(nombre.Select(c => invalidos.Contains(c) || c == ' ' ? ' ' : c).ToArray());
            string[] palabras = con_separadores.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return palabras.Length == 0 ? "Lista" : string.Join("_", palabras);
        }
    }
}
