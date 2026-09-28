using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class CreditNotesViewModel : ViewModelBase
    {
        private readonly ICreditNoteService _credit_note_service;
        private readonly IAccountsReceivableService _receivable_service;
        private readonly IInventoryService _inventory_service;
        private readonly ICustomerService _customer_service;

        private string _selected_month = string.Empty;
        private string _selected_category_filter = "Todas";
        private List<credit_note_dto> _all_credit_rows = new();
        private bool _is_loading;
        private decimal _total_credit_usd;
        private int _selected_tab_index;

        public ObservableCollection<string> credit_note_months { get; }
        public ObservableCollection<string> category_filters { get; } = new() { "Todas", "Devolución", "Obsequio" };
        public ObservableCollection<credit_note_dto> notes { get; }
        public ObservableCollection<credit_note_dto> sandra_notes { get; }
        public ObservableCollection<credit_note_dto> anais_notes { get; }
        public ObservableCollection<credit_note_dto> alejandra_notes { get; }
        public ObservableCollection<credit_note_dto> juan_luis_notes { get; }

        public string selected_category_filter
        {
            get => _selected_category_filter;
            set
            {
                if (_selected_category_filter == value) return;
                _selected_category_filter = value;
                on_property_changed();
                on_property_changed(nameof(total_label));
                apply_filters();
            }
        }

        public string total_label
        {
            get
            {
                if (string.Equals(_selected_category_filter, "Obsequio", StringComparison.OrdinalIgnoreCase))
                    return "TOTAL OBSEQUIADO: $";
                if (string.Equals(_selected_category_filter?.Replace("ó", "o"), "Devolucion", StringComparison.OrdinalIgnoreCase))
                    return "TOTAL DEVUELTO: $";
                return "TOTAL: $";
            }
        }

        public int selected_tab_index
        {
            get => _selected_tab_index;
            set
            {
                if (_selected_tab_index == value) return;
                _selected_tab_index = value;
                on_property_changed();
                recalc_totals();
            }
        }

        public string selected_month
        {
            get => _selected_month;
            set
            {
                if (_selected_month == value) return;
                _selected_month = value ?? string.Empty;
                on_property_changed();
                if (!_is_loading) apply_filters();
            }
        }

        public decimal total_credit_usd
        {
            get => _total_credit_usd;
            private set { _total_credit_usd = value; on_property_changed(); }
        }

        public ICommand new_credit_note_command { get; }
        public Action? on_request_new_credit_note_window { get; set; }
        public Action? OnCreditNoteSaved { get; set; }

        public CreditNotesViewModel(ICreditNoteService credit_note_service, IAccountsReceivableService receivable_service, IInventoryService inventory_service, ICustomerService customer_service)
        {
            _credit_note_service = credit_note_service ?? throw new ArgumentNullException(nameof(credit_note_service));
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));
            _inventory_service = inventory_service ?? throw new ArgumentNullException(nameof(inventory_service));
            _customer_service = customer_service ?? throw new ArgumentNullException(nameof(customer_service));

            credit_note_months = new ObservableCollection<string>();
            notes = new ObservableCollection<credit_note_dto>();
            sandra_notes = new ObservableCollection<credit_note_dto>();
            anais_notes = new ObservableCollection<credit_note_dto>();
            alejandra_notes = new ObservableCollection<credit_note_dto>();
            juan_luis_notes = new ObservableCollection<credit_note_dto>();

            new_credit_note_command = new RelayCommand(execute_new_credit_note);

            load_months_async();
        }

        public void refresh_data() => load_months_async();

        private async void load_months_async()
        {
            try
            {
                var current_selection = _selected_month;
                _is_loading = true;

                var months = (await _credit_note_service.get_credit_note_months_async()).ToList();

                var new_months = new List<string> { "" };
                new_months.AddRange(months);

                // Si el usuario ya tenia un mes seleccionado y sigue existiendo, conservarlo.
                // Si tenia un mes que ya no existe, buscar el mes actual o el mas reciente.
                // Si nunca habia seleccionado nada (primera carga, string.Empty), dejarlo vacio.
                string desired = current_selection ?? string.Empty;
                if (!string.IsNullOrEmpty(desired) && !new_months.Contains(desired))
                {
                    var current_month_str = DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE"));
                    desired = new_months.Contains(current_month_str)
                        ? current_month_str
                        : (months.Count > 0 ? months[months.Count - 1] : string.Empty);
                }

                // Reconstruir la lista de meses sin vaciarla: el item seleccionado nunca se pierde y el orden cronologico estricto se preserva.
                for (int i = credit_note_months.Count - 1; i >= 0; i--)
                {
                    if (!new_months.Contains(credit_note_months[i]))
                        credit_note_months.RemoveAt(i);
                }
                for (int i = 0; i < new_months.Count; i++)
                {
                    var item = new_months[i];
                    int currentIndex = credit_note_months.IndexOf(item);
                    if (currentIndex < 0)
                        credit_note_months.Insert(i, item);
                    else if (currentIndex != i)
                        credit_note_months.Move(currentIndex, i);
                }

                if (_selected_month != desired)
                {
                    _selected_month = desired;
                    on_property_changed(nameof(selected_month));
                }

                _is_loading = false;
                await load_notes_async();
            }
            catch (Exception ex)
            {
                _is_loading = false;
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        public async Task load_notes_async()
        {
            try
            {
                _all_credit_rows = (await _credit_note_service.get_all_credit_notes_async()).ToList();
                apply_filters();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private void apply_filters()
        {
            notes.Clear();
            sandra_notes.Clear();
            anais_notes.Clear();
            alejandra_notes.Clear();
            juan_luis_notes.Clear();
            total_credit_usd = 0;

            IEnumerable<credit_note_dto> filtered = string.IsNullOrEmpty(_selected_month)
                ? Enumerable.Empty<credit_note_dto>()
                : _all_credit_rows.Where(r => string.Equals(r.delivery_month_label, _selected_month, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(_selected_category_filter) && !_selected_category_filter.Equals("Todas", StringComparison.OrdinalIgnoreCase))
            {
                string target = _selected_category_filter.Replace("ó", "o");
                filtered = filtered.Where(r => string.Equals(r.category?.Replace("ó", "o"), target, StringComparison.OrdinalIgnoreCase));
            }

            var rows = filtered
                .OrderByCorrelative(r => r.note_number)
                .ToList();

            foreach (var row in rows) notes.Add(row);
            foreach (var row in rows.Where(r => string.Equals(r.seller_name?.Trim(), "Sandra", StringComparison.OrdinalIgnoreCase))) sandra_notes.Add(row);
            foreach (var row in rows.Where(r => string.Equals(r.seller_name?.Trim(), "Anais", StringComparison.OrdinalIgnoreCase))) anais_notes.Add(row);
            foreach (var row in rows.Where(r => string.Equals(r.seller_name?.Trim(), "Alejandra", StringComparison.OrdinalIgnoreCase))) alejandra_notes.Add(row);
            foreach (var row in rows.Where(r => string.Equals(r.seller_name?.Trim(), "Juan Luis", StringComparison.OrdinalIgnoreCase))) juan_luis_notes.Add(row);

            recalc_totals();
        }

        private void recalc_totals()
        {
            var list = _selected_tab_index switch
            {
                0 => notes.ToList(),
                1 => sandra_notes.ToList(),
                2 => anais_notes.ToList(),
                3 => alejandra_notes.ToList(),
                4 => juan_luis_notes.ToList(),
                _ => new List<credit_note_dto>()
            };

            total_credit_usd = list.Sum(r => r.total_amount_usd);
        }

        private void execute_new_credit_note(object? parameter) => on_request_new_credit_note_window?.Invoke();

        public async Task<credit_note_source_dto?> get_credit_source_async(string note_number)
            => await _credit_note_service.get_credit_source_by_note_number_async(note_number);

        public async Task<IEnumerable<accounts_receivable_dto>> get_delivery_notes_for_credit_async(int id_seller, string? month_year = null)
            => await _credit_note_service.get_delivery_notes_for_credit_async(id_seller, month_year);

        public async Task<IEnumerable<accounts_receivable_dto>> get_notes_by_month_async(string month_year)
            => await _receivable_service.get_all_by_month_async(month_year);

        public async Task<IEnumerable<string>> get_delivery_note_months_for_seller_async(int id_seller)
            => await _credit_note_service.get_delivery_note_months_for_seller_async(id_seller);

        public async Task<IEnumerable<string>> get_all_months_async()
            => await _receivable_service.get_all_months_async();

        public async Task<string> generate_credit_correlative_async(int id_seller)
            => await _credit_note_service.generate_credit_correlative_async(id_seller);

        public async Task<credit_note_dto> save_credit_note_async(credit_note new_note, IEnumerable<credit_note_detail> details)
        {
            int attempts = 0;
            while (true)
            {
                attempts++;
                try
                {
                    return await _credit_note_service.create_credit_note_async(new_note, details);
                }
                catch (InvalidOperationException ex)
                    when (ex.Message.IndexOf("correlativo", StringComparison.OrdinalIgnoreCase) >= 0 && attempts <= 3)
                {
                    string regenerated = await _credit_note_service.generate_credit_correlative_async(new_note.id_seller);
                    new_note.note_number = regenerated;
                }
            }
        }

        public async Task<IEnumerable<credit_note_detail_dto>> get_credit_note_details_async(int id_credit_note)
            => await _credit_note_service.get_credit_note_details_async(id_credit_note);

        public async Task<(note_print_dto? original, note_print_dto credit)> get_printable_pair_async(credit_note_dto credit_note)
        {
            // El obsequio no va anclado a una nota de entrega: se genera solo el PDF de la NC.
            note_print_dto? original = null;
            if (!string.Equals(credit_note.category, "Obsequio", StringComparison.OrdinalIgnoreCase) && credit_note.id_delivery_note > 0)
                original = await _receivable_service.get_printable_note_async(credit_note.id_delivery_note);
            var credit = await _credit_note_service.get_printable_credit_note_async(credit_note.id_credit_note);
            return (original, credit);
        }

        public async Task<IEnumerable<product>> get_obsequio_products_async()
            => await _inventory_service.get_all_products_async();

        public async Task<IEnumerable<customer>> get_customers_async()
            => await _customer_service.GetAllCustomersAsync();

        public async Task<IEnumerable<seller>> get_sellers_async()
            => await _credit_note_service.get_sellers_async();
    }
}