using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class CreditNotesReportViewModel : ViewModelBase
    {
        public const string PeriodGeneral = "GENERAL";

        private const string CategoryAll = "Todas";
        private const string CategoryGift = "Obsequio";
        private const string CategoryReturn = "Devolucion";

        private static readonly CultureInfo Ve = new CultureInfo("es-VE");

        private readonly ICreditNoteService _credit_note_service;

        private string _selected_period = PeriodGeneral;
        private string _selected_category = CategoryAll;
        private bool _is_loading;
        private credit_note_report_dto _report = new credit_note_report_dto();

        public ObservableCollection<string> period_options { get; }
        public ObservableCollection<string> category_options { get; }

        public ICommand refresh_command { get; }
        public ICommand export_pdf_command { get; }

        public string selected_period
        {
            get => _selected_period;
            set
            {
                if (_selected_period == value) return;
                _selected_period = value;
                on_property_changed();
                reload();
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
                reload();
            }
        }

        public credit_note_report_dto report
        {
            get => _report;
            private set
            {
                _report = value;
                on_property_changed();
                on_property_changed(nameof(has_data));
                on_property_changed(nameof(period_label));
                on_property_changed(nameof(show_day_bars));
            }
        }

        public bool has_data => _report.has_data;
        public bool is_loading => _is_loading;

        // El desglose diario solo tiene sentido cuando el reporte es de un mes.
        public bool show_day_bars => _report.by_day.Count > 0;

        public string period_label => string.IsNullOrWhiteSpace(_report.period_label) ? "Sin periodo" : _report.period_label;

        public CreditNotesReportViewModel(ICreditNoteService credit_note_service)
        {
            _credit_note_service = credit_note_service ?? throw new ArgumentNullException(nameof(credit_note_service));

            period_options = new ObservableCollection<string> { PeriodGeneral };
            category_options = new ObservableCollection<string> { CategoryAll, CategoryGift, CategoryReturn };

            refresh_command = new RelayCommand(_ => reload());
            export_pdf_command = new RelayCommand(_ => execute_export_pdf());

            _ = initialize_async();
        }

        // Permite que el mini menu del boton abra el reporte en un periodo concreto.
        public void select_period(string? period)
        {
            string wanted = string.IsNullOrWhiteSpace(period) ? PeriodGeneral : period.Trim();

            if (!period_options.Contains(wanted))
                period_options.Add(wanted);

            if (_selected_period == wanted)
            {
                reload();
                return;
            }

            _selected_period = wanted;
            on_property_changed(nameof(selected_period));
            reload();
        }

        public void refresh_data() => _ = initialize_async();

        private async Task initialize_async()
        {
            try
            {
                _is_loading = true;
                on_property_changed(nameof(is_loading));

                var months = (await _credit_note_service.get_credit_note_months_async()).ToList();

                period_options.Clear();
                period_options.Add(PeriodGeneral);
                foreach (var m in months) period_options.Add(m);

                if (!period_options.Contains(_selected_period))
                    period_options.Add(_selected_period);

                on_property_changed(nameof(selected_period));

                _is_loading = false;
                on_property_changed(nameof(is_loading));

                await load_report_async();
            }
            catch (Exception ex)
            {
                _is_loading = false;
                on_property_changed(nameof(is_loading));
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private bool is_general => string.Equals(_selected_period, PeriodGeneral, StringComparison.OrdinalIgnoreCase);

        private bool resolve_period(out DateTime from, out DateTime to)
        {
            if (is_general)
            {
                // GENERAL: todo el historial, desde la primera nota hasta hoy.
                from = new DateTime(2000, 1, 1);
                to = DateTime.Today;
                return true;
            }

            if (!DateTime.TryParseExact(_selected_period, "MMMM yyyy", Ve, DateTimeStyles.None, out var month))
            {
                AppDialog.Show(
                    "Seleccione un periodo valido para el reporte.",
                    "Reporte de Notas de Credito",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                from = default;
                to = default;
                return false;
            }

            from = new DateTime(month.Year, month.Month, 1);
            to = from.AddMonths(1).AddDays(-1);
            return true;
        }

        private void reload()
        {
            if (_is_loading) return;
            _ = load_report_async();
        }

        private async Task load_report_async()
        {
            if (!resolve_period(out var from, out var to)) return;

            try
            {
                _is_loading = true;
                on_property_changed(nameof(is_loading));

                var category = _selected_category == CategoryAll ? null : _selected_category;

                var result = await _credit_note_service.get_credit_note_report_async(from, to, category, null);

                if (is_general)
                {
                    result.period_label = PeriodGeneral;
                    result.previous_period_label = string.Empty;
                    result.previous_total_usd = 0;
                    result.previous_total_notes = 0;
                }

                report = result;
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
            finally
            {
                _is_loading = false;
                on_property_changed(nameof(is_loading));
            }
        }

        private void execute_export_pdf()
        {
            try
            {
                if (!report.has_data)
                {
                    AppDialog.Show(
                        "No hay notas de credito en el periodo seleccionado.",
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
        }
    }
}
