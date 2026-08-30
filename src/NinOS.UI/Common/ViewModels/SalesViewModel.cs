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
    public class SalesViewModel : ViewModelBase
    {
        private readonly IAccountsReceivableService _receivable_service;

        private int _selected_tab_index;
        private string _search_query = string.Empty;
        private string _selected_month = string.Empty;
        private string _selected_filter = "Todas";
        private decimal _total_sales_usd;
        private bool _is_loading;

        private List<accounts_receivable_dto> _all_notes_source = new();

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<string> filter_options { get; }
        public ObservableCollection<accounts_receivable_dto> all_notes { get; }
        public ObservableCollection<accounts_receivable_dto> sandra_notes { get; }
        public ObservableCollection<accounts_receivable_dto> anais_notes { get; }
        public ObservableCollection<accounts_receivable_dto> alejandra_notes { get; }

        public string selected_month
        {
            get => _selected_month;
            set { if (_selected_month == value) return; _selected_month = value; on_property_changed(); if (!_is_loading) apply_filters(); }
        }

        public int selected_tab_index
        {
            get => _selected_tab_index;
            set { _selected_tab_index = value; on_property_changed(); if (!_is_loading) apply_filters(); }
        }

        public string selected_filter
        {
            get => _selected_filter;
            set { if (_selected_filter == value) return; _selected_filter = value; on_property_changed(); apply_filters(); }
        }

        public string search_query
        {
            get => _search_query;
            set { _search_query = value; on_property_changed(); apply_filters(); }
        }

        public decimal total_sales_usd
        {
            get => _total_sales_usd;
            private set { _total_sales_usd = value; on_property_changed(); }
        }

        public ICommand preview_note_command { get; }
        public ICommand print_pdf_command { get; }

        public Action<accounts_receivable_dto>? on_request_preview_window;

        public SalesViewModel(IAccountsReceivableService receivable_service)
        {
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));

            pending_months = new ObservableCollection<string>();
            filter_options = new ObservableCollection<string>();
            all_notes = new ObservableCollection<accounts_receivable_dto>();
            sandra_notes = new ObservableCollection<accounts_receivable_dto>();
            anais_notes = new ObservableCollection<accounts_receivable_dto>();
            alejandra_notes = new ObservableCollection<accounts_receivable_dto>();

            filter_options.Add("Todas");
            filter_options.Add("Por Cobrar");
            filter_options.Add("Pagadas");
            filter_options.Add("Anuladas");

            preview_note_command = new RelayCommand(execute_preview_note);
            print_pdf_command = new RelayCommand(execute_print_pdf);

            load_all_async();
        }

        public void refresh_data() => load_all_async();

        public async Task<note_print_dto> get_printable_note_async(int id_delivery_note)
        {
            return await _receivable_service.get_printable_note_async(id_delivery_note);
        }

        private async void load_all_async()
        {
            try
            {
                _is_loading = true;

                var raw = await _receivable_service.get_all_notes_async();
                var all_rows = raw.ToList();

                var unique_months = all_rows
                    .Select(n => new DateTime(n.creation_date.Year, n.creation_date.Month, 1))
                    .Distinct()
                    .OrderBy(d => d)
                    .Select(d => d.ToString("MMMM yyyy", new CultureInfo("es-VE")))
                    .ToList();

                pending_months.Clear();
                foreach (var m in unique_months) pending_months.Add(m);

                _all_notes_source = all_rows;

                var current_month = DateTime.Now.ToString("MMMM yyyy", new CultureInfo("es-VE"));
                _selected_month = pending_months.Contains(current_month) ? current_month : pending_months.LastOrDefault() ?? string.Empty;
                on_property_changed(nameof(selected_month));

                _is_loading = false;
                apply_filters();
            }
            catch (Exception ex)
            {
                _is_loading = false;
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void apply_filters()
        {
            var query = _search_query?.Trim().ToLower() ?? string.Empty;
            var filtered = filter_by_month_and_search(_all_notes_source, _selected_month, query);

            if (_selected_filter == "Por Cobrar")
                filtered = filtered.Where(n => n.status == "Pendiente").ToList();
            else if (_selected_filter == "Pagadas")
                filtered = filtered.Where(n => n.status == "Pagada").ToList();
            else if (_selected_filter == "Anuladas")
                filtered = filtered.Where(n => n.status == "Anulada").ToList();

            update_collection(all_notes, filtered);
            update_collection(sandra_notes, filtered.Where(n => n.seller_name == "Sandra").ToList());
            update_collection(anais_notes, filtered.Where(n => n.seller_name == "Anais").ToList());
            update_collection(alejandra_notes, filtered.Where(n => n.seller_name == "Alejandra").ToList());

            recalc_totals();
        }

        private List<accounts_receivable_dto> filter_by_month_and_search(List<accounts_receivable_dto> source, string selected_month, string query)
        {
            var result = source.AsEnumerable();

            if (!string.IsNullOrEmpty(selected_month))
                result = result.Where(n =>
                    new DateTime(n.creation_date.Year, n.creation_date.Month, 1)
                        .ToString("MMMM yyyy", new CultureInfo("es-VE")) == selected_month);

            if (!string.IsNullOrEmpty(query))
            {
                result = result.Where(n =>
                    (n.note_number?.ToLower().Contains(query) ?? false) ||
                    (n.customer_name?.ToLower().Contains(query) ?? false) ||
                    (n.seller_name?.ToLower().Contains(query) ?? false));
            }

            return result.ToList();
        }

        private void recalc_totals()
        {
            var list = _selected_tab_index switch
            {
                0 => all_notes.ToList(),
                1 => sandra_notes.ToList(),
                2 => anais_notes.ToList(),
                3 => alejandra_notes.ToList(),
                _ => new List<accounts_receivable_dto>()
            };

            total_sales_usd = list.Sum(n => n.total_amount_usd);
        }

        private void update_collection(ObservableCollection<accounts_receivable_dto> collection, List<accounts_receivable_dto> items)
        {
            collection.Clear();
            foreach (var item in items) collection.Add(item);
        }

        private void execute_preview_note(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
                on_request_preview_window?.Invoke(note);
        }

        private async void execute_print_pdf(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                try
                {
                    note_print_dto printable = await _receivable_service.get_printable_note_async(note.id_delivery_note);
                    NotePdfGenerator.generate(printable);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}