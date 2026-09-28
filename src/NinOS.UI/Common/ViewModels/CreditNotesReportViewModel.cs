using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    // Alimenta el mini menu de reporte de la pestana Notas de Credito:
    // el usuario elige el periodo (general o mensual) y la categoria, y descarga el PDF.
    public class CreditNotesReportViewModel : ViewModelBase
    {
        public const string PeriodGeneral = "GENERAL";

        private const string CategoryBoth = "Ambos";
        private const string CategoryGift = "Obsequio";
        private const string CategoryReturns = "Devoluciones";

        // Como la columna guarda la categoria sin tilde.
        private const string StoredGift = "Obsequio";
        private const string StoredReturn = "Devolucion";

        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        private readonly ICreditNoteService _credit_note_service;

        private bool _is_general = true;
        private bool _is_mensual;
        private string _selected_month = string.Empty;
        private string _selected_category = CategoryBoth;
        private bool _is_loading;

        public ObservableCollection<string> month_options { get; }
        public ObservableCollection<string> category_options { get; }

        // Los dos checkboxes son excluyentes: al marcar uno se desmarca el otro.
        public bool is_general
        {
            get => _is_general;
            set
            {
                if (_is_general == value) return;
                _is_general = value;
                if (value) _is_mensual = false;
                on_property_changed();
                on_property_changed(nameof(is_mensual));
            }
        }

        public bool is_mensual
        {
            get => _is_mensual;
            set
            {
                if (_is_mensual == value) return;
                _is_mensual = value;
                if (value) _is_general = false;
                on_property_changed();
                on_property_changed(nameof(is_general));
            }
        }

        public string selected_month
        {
            get => _selected_month;
            set
            {
                if (_selected_month == value) return;
                _selected_month = value;
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
            month_options = new ObservableCollection<string>();
        }

        // Se recarga cada vez que se abre el menu: si se registro una nota nueva,
        // su mes tiene que aparecer entre las opciones.
        public async Task load_options_async()
        {
            try
            {
                is_loading = true;

                var months = await _credit_note_service.get_credit_note_months_async();

                var previous = _selected_month;

                month_options.Clear();
                foreach (var m in months)
                {
                    if (string.IsNullOrWhiteSpace(m)) continue;
                    month_options.Add(m);
                }

                if (month_options.Count == 0)
                {
                    is_mensual = false;
                    is_general = true;
                    _selected_month = string.Empty;
                }
                var current_month_str = DateTime.Now.ToString("MMMM yyyy", Ve);
                if (!string.IsNullOrWhiteSpace(previous) && month_options.Contains(previous))
                {
                    _selected_month = previous;
                }
                else if (month_options.Contains(current_month_str))
                {
                    _selected_month = current_month_str;
                }
                else
                {
                    _selected_month = month_options.Count > 0 ? month_options[month_options.Count - 1] : string.Empty;
                }

                on_property_changed(nameof(selected_month));
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
            finally
            {
                is_loading = false;
            }
        }

        public async Task descargar_reporte_async()
        {
            try
            {
                is_loading = true;

                if (!resolve_period(out var from, out var to, out var period_label)) return;

                var category = resolve_category();

                var report = await _credit_note_service.get_credit_note_report_async(from, to, category, null);

                report.period_label = period_label;
                report.category_label = _selected_category;

                if (period_label == PeriodGeneral)
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
                    return;
                }

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

        private bool resolve_period(out DateTime from, out DateTime to, out string label)
        {
            if (_is_general)
            {
                // GENERAL: todo el historial, hasta hoy.
                from = new DateTime(2000, 1, 1);
                to = DateTime.Today;
                label = PeriodGeneral;
                return true;
            }

            if (!DateTime.TryParseExact(_selected_month, "MMMM yyyy", Ve, DateTimeStyles.None, out var month))
            {
                AppDialog.Show(
                    "Seleccione un mes para el reporte mensual.",
                    "Reporte de Notas de Credito",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                from = default;
                to = default;
                label = string.Empty;
                return false;
            }

            from = new DateTime(month.Year, month.Month, 1);
            to = from.AddMonths(1).AddDays(-1);
            label = month.ToString("MMMM yyyy", Ve);
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
