using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;
using NinOS.UI.Views;

namespace NinOS.UI.Common.ViewModels
{
    // Alimenta el mini menu de reporte de la pestana Notas de Credito:
    // el usuario elige el periodo (general o mensual) y la categoria, y descarga el PDF.
    public class CreditNotesReportViewModel : ViewModelBase
    {


        private const string CategoryBoth = "Ambos";
        private const string CategoryGift = "Obsequio";
        private const string CategoryReturns = "Devoluciones";

        // Como la columna guarda la categoria sin tilde.
        private const string StoredGift = "Obsequio";
        private const string StoredReturn = "Devolucion";

        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        private readonly ICreditNoteService _credit_note_service;

        private rango_fechas _rango_reporte = rango_fechas.mes_actual(AppTimeZone.to_local(DateTime.UtcNow).Date);
        private string _selected_category = CategoryBoth;
        private bool _include_annulled;
        private bool _is_loading;


        public ObservableCollection<string> category_options { get; }

        /// <summary>
        /// Las notas de credito anuladas salen apagadas en el reporte. Si se enciende aparecen
        /// marcadas en rojo y aparte, pero nunca suman al total ni al cumplimiento.
        /// </summary>
        public bool include_annulled
        {
            get => _include_annulled;
            set
            {
                if (_include_annulled == value) return;
                _include_annulled = value;
                on_property_changed();
            }
        }

        public rango_fechas rango_reporte
        {
            get => _rango_reporte;
            set
            {
                if (value == null || _rango_reporte == value) return;
                _rango_reporte = value;
                on_property_changed();
            }
        }

        public string selected_category
        {
            get => _selected_category;
            set
            {
                if (_selected_category == value) return;
                _selected_category = value;
                on_property_changed();
            }
        }

        public bool is_loading
        {
            get => _is_loading;
            private set
            {
                if (_is_loading == value) return;
                _is_loading = value;
                on_property_changed();
            }
        }

        public CreditNotesReportViewModel(ICreditNoteService credit_note_service)
        {
            _credit_note_service = credit_note_service ?? throw new ArgumentNullException(nameof(credit_note_service));

            category_options = new ObservableCollection<string> { CategoryBoth, CategoryGift, CategoryReturns };

        }

public async Task<credit_note_report_dto?> build_report_dto_async()
        {
            if (!resolve_period(out var from, out var to, out var period_label)) return null;

            var category = resolve_category();
            var report = await _credit_note_service.get_credit_note_report_async(from, to, category, null);

            report.period_label = period_label;
            report.category_label = _selected_category;
            report.include_annulled = _include_annulled;
            if (!_include_annulled)
            {
                report.rows = report.rows.Where(r => !r.esta_anulada).ToList();
            }

            if (_rango_reporte.desde == null && _rango_reporte.hasta == null)
            {
                report.previous_period_label = string.Empty;
                report.previous_total_usd = 0;
                report.previous_total_notes = 0;
            }

            if (!report.has_data)
            {
                AppDialog.Show(
                    "No hay notas de credito para el periodo y la categoria seleccionados.",
                    "Reporte de Notas de Credito",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return null;
            }

            return report;
        }

        public async Task descargar_reporte_async()
        {
            try
            {
                is_loading = true;
                var report = await build_report_dto_async();
                if (report != null)
                    CreditNotesReportPdfGenerator.generate(report);
            }
            catch (Exception ex)
            {
                AppDialog.Show(
                    $"Error al generar el reporte: {ErrorText.Get(ex)}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                is_loading = false;
            }
        }

        public async Task preview_reporte_async()
        {
            try
            {
                is_loading = true;
                var report = await build_report_dto_async();
                if (report == null) return;

                var doc = CreditNotesReportPdfGenerator.build_document(report);
                var fileName = CreditNotesReportPdfGenerator.get_default_file_name(report);
                var preview = new PdfPreviewWindow(
                    doc,
                    $"Reporte de Notas de Crédito - {report.period_label}",
                    fileName,
                    () => CreditNotesReportExcelExporter.export(report));
                preview.Owner = System.Windows.Application.Current?.MainWindow;
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                AppDialog.Show(
                    $"Error en vista previa: {ErrorText.Get(ex)}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                is_loading = false;
            }
        }

        public async Task print_reporte_async()
        {
            try
            {
                is_loading = true;
                var report = await build_report_dto_async();
                if (report == null) return;

                var doc = CreditNotesReportPdfGenerator.build_document(report);
                var fileName = CreditNotesReportPdfGenerator.get_default_file_name(report);
                ReportPrintService.DirectPrint(doc, System.Windows.Application.Current?.MainWindow, $"Reporte de Notas de Crédito - {report.period_label}", fileName);
            }
            catch (Exception ex)
            {
                AppDialog.Show(
                    $"Error al imprimir: {ErrorText.Get(ex)}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                is_loading = false;
            }
        }

        public async Task excel_reporte_async()
        {
            try
            {
                is_loading = true;
                var report = await build_report_dto_async();
                if (report == null) return;

                CreditNotesReportExcelExporter.export(report);
            }
            catch (Exception ex)
            {
                AppDialog.Show(
                    $"Error al exportar Excel: {ErrorText.Get(ex)}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                is_loading = false;
            }
        }

        private bool resolve_period(out DateTime from, out DateTime to, out string label)
        {
            from = _rango_reporte.desde ?? new DateTime(2000, 1, 1);
            to = _rango_reporte.hasta ?? DateTime.Today;
            label = _rango_reporte.texto_titulo_mayusculas();
            return true;
        }

        private string? resolve_category()
        {
            if (_selected_category == CategoryBoth) return null;
            if (_selected_category == CategoryGift) return StoredGift;
            if (_selected_category == CategoryReturns) return StoredReturn;
            return null;
        }
    }
}
