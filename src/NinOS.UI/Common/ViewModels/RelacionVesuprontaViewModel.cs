using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Views;

namespace NinOS.UI.Common.ViewModels
{
    public sealed class RelacionVesuprontaViewModel : ViewModelBase
    {
        private readonly IAccountsReceivableService _service;
        private IReadOnlyList<relacion_vesupronta_fuente> _fuentes = Array.Empty<relacion_vesupronta_fuente>();
        private relacion_vesupronta_reporte? _reporte;
        private rango_fechas _rango_filtro = rango_fechas.mes_actual(AppTimeZone.to_local(DateTime.UtcNow).Date);
        private string _estado_texto = string.Empty;

        public ObservableCollection<relacion_vesupronta_fila> filas { get; } = new ObservableCollection<relacion_vesupronta_fila>();

        public ICommand preview_command { get; }
        public ICommand print_command { get; }
        public ICommand pdf_command { get; }
        public ICommand excel_command { get; }

        public RelacionVesuprontaViewModel(IAccountsReceivableService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            preview_command = new RelayCommand(_ => ejecutar_vista_previa());
            print_command = new RelayCommand(_ => ejecutar_impresion());
            pdf_command = new RelayCommand(_ => ejecutar_pdf());
            excel_command = new RelayCommand(_ => ejecutar_excel());

            refresh_data();
        }

        public rango_fechas rango_filtro
        {
            get => _rango_filtro;
            set
            {
                if (value == null || _rango_filtro == value) return;
                _rango_filtro = value;
                on_property_changed();
                recalcular();
            }
        }

        public string estado_texto
        {
            get => _estado_texto;
            private set { _estado_texto = value; on_property_changed(); }
        }

        public string etiqueta_total_ventas => _reporte?.etiqueta_total_ventas ?? "TOTAL VENTAS";
        public string total_ventas_texto => $"{(_reporte?.total_ventas ?? 0m):N2}";
        public string total_por_cobrar_texto => $"{(_reporte?.total_por_cobrar ?? 0m):N2}";

        public void refresh_data()
        {
            _ = cargar_async();
        }

        private async Task cargar_async()
        {
            try
            {
                _fuentes = await _service.get_relacion_fuentes_async();
                AppLog.Info($"Relacion Vesupronta: fuentes cargadas: {_fuentes.Count}.");
                recalcular();
            }
            catch (Exception ex)
            {
                AppLog.Error("Relacion Vesupronta: no se pudieron cargar las notas.", ex);
                AppDialog.Show($"No se pudo cargar la relación: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void recalcular()
        {
            try
            {
                _reporte = relacion_vesupronta_calculator.construir(_fuentes, _rango_filtro);
            }
            catch (Exception ex)
            {
                AppLog.Error("Relacion Vesupronta: no se pudo calcular la relación del rango.", ex);
                _reporte = null;
                filas.Clear();
                estado_texto = "No se pudo calcular la relación de este rango. Revise el log.";
                notificar_totales();
                return;
            }

            filas.Clear();
            foreach (relacion_vesupronta_fila fila in _reporte.filas) filas.Add(fila);

            estado_texto = _reporte.filas.Count == 0
                ? "No hay notas de Vesupronta en el rango seleccionado."
                : string.Empty;
            notificar_totales();
            AppLog.Info($"Relacion Vesupronta: rango '{_reporte.periodo_texto}' con {_reporte.filas.Count} notas.");
        }

        private void notificar_totales()
        {
            on_property_changed(nameof(etiqueta_total_ventas));
            on_property_changed(nameof(total_ventas_texto));
            on_property_changed(nameof(total_por_cobrar_texto));
        }

        private relacion_vesupronta_reporte? reporte_actual()
        {
            if (_reporte != null) return _reporte;
            AppDialog.Show("No hay una relación calculada para exportar.", "Relación Vesupronta",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        private void ejecutar_vista_previa()
        {
            try
            {
                relacion_vesupronta_reporte? reporte = reporte_actual();
                if (reporte == null) return;

                var doc = RelacionVesuprontaPdfGenerator.build_document(reporte);
                var preview = new PdfPreviewWindow(
                    doc,
                    reporte.titulo,
                    RelacionVesuprontaPdfGenerator.get_default_file_name(reporte),
                    () => RelacionVesuprontaExcelExporter.export(reporte));
                preview.Owner = Application.Current?.MainWindow;
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                AppLog.Error("Relacion Vesupronta: fallo la vista previa.", ex);
                AppDialog.Show($"Error en vista previa: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ejecutar_impresion()
        {
            try
            {
                relacion_vesupronta_reporte? reporte = reporte_actual();
                if (reporte == null) return;

                var doc = RelacionVesuprontaPdfGenerator.build_document(reporte);
                ReportPrintService.DirectPrint(doc, Application.Current?.MainWindow, reporte.titulo,
                    RelacionVesuprontaPdfGenerator.get_default_file_name(reporte));
            }
            catch (Exception ex)
            {
                AppLog.Error("Relacion Vesupronta: fallo la impresion.", ex);
                AppDialog.Show($"Error al imprimir: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ejecutar_pdf()
        {
            try
            {
                relacion_vesupronta_reporte? reporte = reporte_actual();
                if (reporte == null) return;

                RelacionVesuprontaPdfGenerator.generate(reporte);
            }
            catch (Exception ex)
            {
                AppLog.Error("Relacion Vesupronta: fallo la exportacion a PDF.", ex);
                AppDialog.Show($"Error al exportar PDF: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ejecutar_excel()
        {
            try
            {
                relacion_vesupronta_reporte? reporte = reporte_actual();
                if (reporte == null) return;

                RelacionVesuprontaExcelExporter.export(reporte);
            }
            catch (Exception ex)
            {
                AppLog.Error("Relacion Vesupronta: fallo la exportacion a Excel.", ex);
                AppDialog.Show($"Error al exportar Excel: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
