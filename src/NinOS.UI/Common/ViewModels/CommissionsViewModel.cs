using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class commission_row_dto : INotifyPropertyChanged
    {
        public int id_commission { get; set; }
        public int id_seller { get; set; }
        public string seller_name { get; set; } = string.Empty;
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public DateTime creation_date { get; set; }
        public string month_key { get; set; } = string.Empty;
        public decimal commission_percentage { get; set; }
        public decimal sale_amount_usd { get; set; }
        public decimal amount_usd { get; set; }
        public decimal paid_amount_usd { get; set; }
        public decimal amount_bs { get; set; }
        public decimal exchange_rate { get; set; }
        public string reference_number { get; set; } = string.Empty;
        public DateTime? payout_date { get; set; }
        public bool is_paid { get; set; }
        public string status => is_paid ? "Pagada" : "Pendiente";
        public decimal remaining_amount_usd => amount_usd - paid_amount_usd;

        private bool _is_selected;

        public bool is_selected
        {
            get => _is_selected;
            set { _is_selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(is_selected))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class CommissionsViewModel : ViewModelBase
    {
        private readonly ICommissionService _commission_service;

        private int _selected_tab_index;
        private string _search_query = string.Empty;
        private string _selected_month = string.Empty;
        private string _selected_filter = "Pendientes";
        private decimal _total_sold_usd;
        private decimal _total_commission_usd;
        private decimal _total_pending_usd;
        private bool _is_loading;

        private List<commission_row_dto> _all_rows_source = new();

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<string> filter_options { get; }
        public ObservableCollection<commission_row_dto> all_rows { get; }
        public ObservableCollection<commission_row_dto> sandra_rows { get; }
        public ObservableCollection<commission_row_dto> anais_rows { get; }
        public ObservableCollection<commission_row_dto> alejandra_rows { get; }

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

        public string search_query
        {
            get => _search_query;
            set { _search_query = value; on_property_changed(); apply_filters(); }
        }

        public string selected_filter
        {
            get => _selected_filter;
            set { if (_selected_filter == value) return; _selected_filter = value; on_property_changed(); apply_filters(); }
        }

        public decimal total_sold_usd
        {
            get => _total_sold_usd;
            private set { _total_sold_usd = value; on_property_changed(); }
        }

        public decimal total_commission_usd
        {
            get => _total_commission_usd;
            private set { _total_commission_usd = value; on_property_changed(); }
        }

        public decimal total_pending_usd
        {
            get => _total_pending_usd;
            private set { _total_pending_usd = value; on_property_changed(); }
        }

        public ICommand add_commission_payment_command { get; }
        public Action<List<commission_row_dto>>? on_request_add_commission_payment_window { get; set; }
        public Action<commission_row_dto>? on_request_commission_history_window { get; set; }

        public CommissionsViewModel(ICommissionService commission_service)
        {
            _commission_service = commission_service ?? throw new ArgumentNullException(nameof(commission_service));

            pending_months = new ObservableCollection<string>();
            filter_options = new ObservableCollection<string>();
            all_rows = new ObservableCollection<commission_row_dto>();
            sandra_rows = new ObservableCollection<commission_row_dto>();
            anais_rows = new ObservableCollection<commission_row_dto>();
            alejandra_rows = new ObservableCollection<commission_row_dto>();

            filter_options.Add("Pendientes");
            filter_options.Add("Pagadas");
            filter_options.Add("Todas");

            add_commission_payment_command = new RelayCommand(execute_add_commission_payment);

            load_all_async();
        }

        public void refresh_data() => load_all_async();

        private async void load_all_async()
        {
            try
            {
                _is_loading = true;

                var raw = await _commission_service.get_all_commissions_async();
                var all_rows = raw.Select(map_to_row).ToList();

                var unique_months = all_rows
                    .Select(n => new DateTime(n.creation_date.Year, n.creation_date.Month, 1))
                    .Distinct()
                    .OrderBy(d => d)
                    .Select(d => d.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE")))
                    .ToList();

                pending_months.Clear();
                foreach (var m in unique_months) pending_months.Add(m);

                _all_rows_source = all_rows;

                var current_month = DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE"));
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
            var filtered = filter_by_month_and_search(_all_rows_source, _selected_month, query);

            if (_selected_filter == "Pendientes")
                filtered = filtered.Where(n => !n.is_paid).ToList();
            else if (_selected_filter == "Pagadas")
                filtered = filtered.Where(n => n.is_paid).ToList();

            update_collection(all_rows, filtered);
            update_collection(sandra_rows, filtered.Where(n => n.seller_name == "Sandra").ToList());
            update_collection(anais_rows, filtered.Where(n => n.seller_name == "Anais").ToList());
            update_collection(alejandra_rows, filtered.Where(n => n.seller_name == "Alejandra").ToList());

            recalc_totals();
        }

        private List<commission_row_dto> filter_by_month_and_search(List<commission_row_dto> source, string selected_month, string query)
        {
            var result = source.AsEnumerable();

            if (!string.IsNullOrEmpty(selected_month))
                result = result.Where(n => n.month_key == selected_month);

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
                0 => all_rows.ToList(),
                1 => sandra_rows.ToList(),
                2 => anais_rows.ToList(),
                3 => alejandra_rows.ToList(),
                _ => new List<commission_row_dto>()
            };

            total_sold_usd = list.Sum(n => n.sale_amount_usd);
            total_commission_usd = list.Sum(n => n.amount_usd);
            total_pending_usd = list.Where(n => !n.is_paid).Sum(n => n.remaining_amount_usd);
        }

        private commission_row_dto map_to_row(commission_dto c)
        {
            return new commission_row_dto
            {
                id_commission = c.id_commission,
                id_seller = c.id_seller,
                seller_name = c.seller_name,
                id_delivery_note = c.id_delivery_note,
                note_number = c.note_number,
                customer_name = c.customer_name,
                creation_date = c.creation_date,
                month_key = new DateTime(c.creation_date.Year, c.creation_date.Month, 1)
                    .ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE")),
                commission_percentage = c.commission_percentage,
                sale_amount_usd = c.commission_percentage > 0 ? Math.Round(c.amount_usd / c.commission_percentage, 2) : c.amount_usd,
                amount_usd = c.amount_usd,
                paid_amount_usd = c.paid_amount_usd,
                amount_bs = c.amount_bs,
                exchange_rate = c.exchange_rate,
                reference_number = c.reference_number,
                payout_date = c.note_last_payment_date ?? c.payout_date,
                is_paid = c.is_paid
            };
        }

        private void update_collection(ObservableCollection<commission_row_dto> collection, List<commission_row_dto> items)
        {
            collection.Clear();
            foreach (var item in items) collection.Add(item);
        }

        private List<commission_row_dto> get_available_pending_rows()
        {
            return _selected_tab_index switch
            {
                0 => all_rows.Where(r => !r.is_paid).ToList(),
                1 => sandra_rows.Where(r => !r.is_paid).ToList(),
                2 => anais_rows.Where(r => !r.is_paid).ToList(),
                3 => alejandra_rows.Where(r => !r.is_paid).ToList(),
                _ => new List<commission_row_dto>()
            };
        }

        private void execute_add_commission_payment(object? parameter)
        {
            var available = get_available_pending_rows();
            if (available.Count == 0)
            {
                System.Windows.MessageBox.Show("No hay comisiones pendientes para pagar.", "Pago de comision",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            on_request_add_commission_payment_window?.Invoke(available);
        }

        public async Task<IEnumerable<commission_dto>> get_all_commissions_async()
        {
            return await _commission_service.get_all_commissions_async();
        }

        public async Task pay_commissions_async(int[] id_commissions, decimal amount_usd, decimal exchange_rate, string payment_type, string reference_number, decimal amount_bs, DateTime payment_date, string bank_name, string observations)
        {
            try
            {
                await _commission_service.register_commission_payment_async(id_commissions, amount_usd, exchange_rate, payment_type, reference_number, amount_bs, payment_date, bank_name, observations);
                System.Windows.MessageBox.Show("Comision(es) liquidadas exitosamente.", "Exito");
                load_all_async();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        public async Task<IEnumerable<commission_payment_dto>> get_commission_payments_async(int id_commission)
        {
            return await _commission_service.get_commission_payments_async(id_commission);
        }
    }
}