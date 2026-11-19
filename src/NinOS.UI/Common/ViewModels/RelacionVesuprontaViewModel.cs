using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        public const string MODO_MES = "Mes";
        public const string MODO_ANIO = "Año";
        public const string MODO_TODOS = "Todos";

        private readonly IAccountsReceivableService _service;
        private IReadOnlyList<relacion_vesupronta_fuente> _fuentes = Array.Empty<relacion_vesupronta_fuente>();
        private relacion_vesupronta_reporte? _reporte;
        private string _modo_seleccionado = MODO_MES;
        private int _anio_seleccionado = DateTime.Now.Year;
        private int _mes_seleccionado = DateTime.Now.Month;
        private string _estado_texto = string.Empty;

        public IReadOnlyList<string> modos { get; } = new[] { MODO_MES, MODO_ANIO, MODO_TODOS };
        public ObservableCollection<int> anios { get; } = new ObservableCollection<int>();
        public ObservableCollection<opcion_mes> meses { get; } = new ObservableCollection<opcion_mes>();
        public ObservableCollection<relacion_vesupronta_fila> filas { get; } = new ObservableCollection<relacion_vesupronta_fila>();

        public ICommand preview_command { get; }
        public ICommand print_command { get; }
        public ICommand pdf_command { get; }
        public ICommand excel_command { get; }

        public RelacionVesuprontaViewModel(IAccountsReceivableService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            for (int numero = 1; numero <= 12; numero++)
            {
                meses.Add(new opcion_mes(numero, System.Globalization.CultureInfo.GetCultureInfo("es-VE").DateTimeFormat.GetMonthName(numero)));
            }

            preview_command = new RelayCommand(_ => ejecutar_vista_previa());
            print_command = new RelayCommand(_ => ejecutar_impresion());
            pdf_command = new RelayCommand(_ => ejecutar_pdf());
            excel_command = new RelayCommand(_ => ejecutar_excel());

            refresh_data();
        }

        public string modo_seleccionado
        {
            get => _modo_seleccionado;
            set
            {
                if (_modo_seleccionado == value) return;
                _modo_seleccionado = value;
                on_property_changed();
                on_property_changed(nameof(es_modo_mes));
                on_property_changed(nameof(usa_anio));
                recalcular();
            }
        }

        public int? anio_seleccionado
        {
            get => _anio_seleccionado;
            set
            {
                if (value == null || _anio_seleccionado == value.Value) return;
                _anio_seleccionado = value.Value;
                on_property_changed();
                recalcular();
            }
        }

        public int mes_seleccionado
        {
            get => _mes_seleccionado;
            set
            {
                if (_mes_seleccionado == value) return;
                _mes_seleccionado = value;
                on_property_changed();
                recalcular();
            }
        }

        public bool es_modo_mes => _modo_seleccionado == MODO_MES;
        public bool usa_anio => _modo_seleccionado != MODO_TODOS;

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
                sincronizar_anios();
                recalcular();
            }
            catch (Exception ex)
            {
                AppLog.Error("Relacion Vesupronta: no se pudieron cargar las notas.", ex);
                AppDialog.Show($"No se pudo cargar la relación: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void sincronizar_anios()
        {
            var anios_disponibles = _fuentes
                .Select(f => AppTimeZone.to_local(f.creation_date).Year)
                .Append(DateTime.Now.Year)
                .Distinct()
                .OrderByDescending(anio => anio)
                .ToList();

            anios.Clear();
            foreach (int anio in anios_disponibles) anios.Add(anio);

            if (!anios.Contains(_anio_seleccionado))
            {
                _anio_seleccionado = anios[0];
            }
            on_property_changed(nameof(anio_seleccionado));
        }

        private void recalcular()
        {
            relacion_periodo periodo = construir_periodo();
            try
            {
                _reporte = relacion_vesupronta_calculator.construir(_fuentes, periodo);
            }
            catch (Exception ex)
            {
                AppLog.Error("Relacion Vesupronta: no se pudo calcular la relación del periodo.", ex);
                _reporte = null;
                filas.Clear();
                estado_texto = "No se pudo calcular la relación de este periodo. Revise el log.";
                notificar_totales();
                return;
            }

            filas.Clear();
            foreach (relacion_vesupronta_fila fila in _reporte.filas) filas.Add(fila);

            estado_texto = _reporte.filas.Count == 0
                ? "No hay notas de Vesupronta para el período seleccionado."
                : string.Empty;
            notificar_totales();
            AppLog.Info($"Relacion Vesupronta: periodo '{_reporte.periodo_texto}' con {_reporte.filas.Count} notas.");
        }

        private void notificar_totales()
        {
            on_property_changed(nameof(etiqueta_total_ventas));
            on_property_changed(nameof(total_ventas_texto));
            on_property_changed(nameof(total_por_cobrar_texto));
        }

        private relacion_periodo construir_periodo()
        {
            return _modo_seleccionado switch
            {
                MODO_MES => new relacion_periodo(relacion_modo_periodo.Mes, _anio_seleccionado, _mes_seleccionado),
                MODO_ANIO => new relacion_periodo(relacion_modo_periodo.Anio, _anio_seleccionado, 1),
                _ => new relacion_periodo(relacion_modo_periodo.Todos, DateTime.Now.Year, 1)
            };
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

    public sealed class opcion_mes
    {
        public opcion_mes(int numero, string nombre)
        {
            if (numero < 1 || numero > 12) throw new ArgumentOutOfRangeException(nameof(numero), "El mes debe estar entre 1 y 12.");
            this.numero = numero;
            this.nombre = string.IsNullOrWhiteSpace(nombre) ? throw new ArgumentException("El mes necesita nombre.", nameof(nombre)) : nombre;
        }

        public int numero { get; }
        public string nombre { get; }
    }
}
