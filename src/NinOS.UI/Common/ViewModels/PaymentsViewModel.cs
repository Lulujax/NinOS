using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class payment_row_dto
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public decimal total_amount_usd { get; set; }
        public decimal gross_total_usd { get; set; }
        public decimal discount_amount { get; set; }
        public string discount_percentage_text { get; set; } = "0%";
        public decimal paid_amount_usd { get; set; }
        public decimal balance_due_usd { get; set; }
        public DateTime? last_payment_date { get; set; }
        public string payment_method_text { get; set; } = string.Empty;
        public string bank_name_text { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public int id_seller { get; set; }
        public DateTime creation_date { get; set; }
        public string month_key { get; set; } = string.Empty;
    }

    public class PaymentsViewModel : ViewModelBase
    {
        private readonly IPaymentService _payment_service;
        private readonly IAccountsReceivableService _receivable_service;

        private int _selected_tab_index;
        private string _search_query = string.Empty;
        private string _selected_month = string.Empty;
        private string _selected_filter = "Pagadas";
        private decimal _total_invoiced_usd;
        private decimal _total_paid_usd;
        private decimal _total_balance_usd;
        private bool _is_loading;

        private List<payment_row_dto> _all_notes_source = new();

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<string> filter_options { get; }
        public ObservableCollection<payment_row_dto> all_notes { get; }
        public ObservableCollection<payment_row_dto> sandra_notes { get; }
        public ObservableCollection<payment_row_dto> anais_notes { get; }
        public ObservableCollection<payment_row_dto> alejandra_notes { get; }
        public ObservableCollection<payment_row_dto> juan_luis_notes { get; }

        public string selected_month
        {
            get => _selected_month;
            set { if (_selected_month == value) return; _selected_month = value; on_property_changed(); if (!_is_loading) apply_filters(); }
        }

        public int selected_tab_index
        {
            get => _selected_tab_index;
            set { _selected_tab_index = value; on_property_changed(); apply_filters(); }
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

        public decimal total_invoiced_usd
        {
            get => _total_invoiced_usd;
            private set { _total_invoiced_usd = value; on_property_changed(); }
        }

        public decimal total_paid_usd
        {
            get => _total_paid_usd;
            private set { _total_paid_usd = value; on_property_changed(); }
        }

        public decimal total_balance_usd
        {
            get => _total_balance_usd;
            private set { _total_balance_usd = value; on_property_changed(); }
        }

        public ICommand add_payment_command { get; }
        public ICommand month_report_command { get; }
        public Action? on_request_add_payment_window { get; set; }

        public PaymentsViewModel(IPaymentService payment_service, IAccountsReceivableService receivable_service)
        {
            _payment_service = payment_service ?? throw new ArgumentNullException(nameof(payment_service));
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));

            pending_months = new ObservableCollection<string>();
            filter_options = new ObservableCollection<string>();
            all_notes = new ObservableCollection<payment_row_dto>();
            sandra_notes = new ObservableCollection<payment_row_dto>();
            anais_notes = new ObservableCollection<payment_row_dto>();
            alejandra_notes = new ObservableCollection<payment_row_dto>();
            juan_luis_notes = new ObservableCollection<payment_row_dto>();

            filter_options.Add("Pagadas");
            filter_options.Add("Pendientes");
            filter_options.Add("Todas");

            add_payment_command = new RelayCommand(execute_add_payment);

            month_report_command = new RelayCommand(execute_month_report);

            load_all_async();
        }

        public void refresh_data() => load_all_async();

        private async void load_all_async()
        {
            try
            {
                _is_loading = true;

                var raw = await _receivable_service.get_all_notes_async();
                var all_rows = raw.Where(n => n.status != "Anulada").Select(map_to_row).ToList();

                var unique_months = all_rows
                    .Select(n => new DateTime(n.creation_date.Year, n.creation_date.Month, 1))
                    .Distinct()
                    .OrderBy(d => d)
                    .Select(d => d.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE")))
                    .ToList();

                pending_months.Clear();
                pending_months.Add("");
                foreach (var m in unique_months) pending_months.Add(m);

                _all_notes_source = all_rows;

                if (!pending_months.Contains(_selected_month)) _selected_month = string.Empty;
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

            IEnumerable<payment_row_dto> source = _all_notes_source;
            if (_selected_filter == "Pagadas")
                source = source.Where(n => n.status == "Pagada");
            else if (_selected_filter == "Pendientes")
                source = source.Where(n => n.status == "Pendiente");

            var filtered = filter_by_month_and_search(source.ToList(), _selected_month, query);

            update_collection(all_notes, filtered);
            update_collection(sandra_notes, filtered.Where(n => n.seller_name == "Sandra").ToList());
            update_collection(anais_notes, filtered.Where(n => n.seller_name == "Anais").ToList());
            update_collection(alejandra_notes, filtered.Where(n => n.seller_name == "Alejandra").ToList());
            update_collection(juan_luis_notes, filtered.Where(n => n.seller_name == "Juan Luis").ToList());

            recalc_totals();
        }

        private List<payment_row_dto> filter_by_month_and_search(List<payment_row_dto> source, string selected_month, string query)
        {
            if (string.IsNullOrEmpty(selected_month))
                return new List<payment_row_dto>();

            var result = source.AsEnumerable();

            if (!string.IsNullOrEmpty(selected_month))
            {
                result = result.Where(n => n.month_key == selected_month);
            }

            if (!string.IsNullOrEmpty(query))
            {
                result = result.Where(n =>
                    (n.note_number?.ToLower().Contains(query) ?? false) ||
                    (n.customer_name?.ToLower().Contains(query) ?? false));
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
                4 => juan_luis_notes.ToList(),
                _ => new List<payment_row_dto>()
            };

            total_invoiced_usd = list.Sum(n => n.total_amount_usd);
            total_paid_usd = list.Sum(n => n.paid_amount_usd);
            total_balance_usd = total_invoiced_usd - total_paid_usd;
        }

        private payment_row_dto map_to_row(accounts_receivable_dto n)
        {
            return new payment_row_dto
            {
                id_delivery_note = n.id_delivery_note,
                note_number = n.note_number,
                customer_name = n.customer_name,
                total_amount_usd = n.total_amount_usd,
                gross_total_usd = n.gross_total_usd,
                discount_amount = n.discount_amount,
                discount_percentage_text = n.discount_percentage.HasValue
                    ? $"{n.discount_percentage.Value:0.##}%"
                    : (n.gross_total_usd > 0 ? $"{((n.discount_amount / n.gross_total_usd) * 100):0.##}%" : "0%"),
                paid_amount_usd = n.paid_amount_usd,
                balance_due_usd = n.balance_due_usd,
                last_payment_date = n.last_payment_date,
                payment_method_text = n.payment_method_text,
                bank_name_text = n.bank_name_text,
                status = n.status,
                seller_name = n.seller_name,
                id_seller = n.id_seller,
                creation_date = n.creation_date,
                month_key = new DateTime(n.creation_date.Year, n.creation_date.Month, 1)
                    .ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE"))
            };
        }

        private void update_collection(ObservableCollection<payment_row_dto> collection, List<payment_row_dto> items)
        {
            collection.Clear();
            foreach (var item in items) collection.Add(item);
        }

        private void execute_add_payment(object? parameter) => on_request_add_payment_window?.Invoke();

        public async Task confirm_payment_async(int id_delivery_note, decimal amount_usd, decimal? exchange_rate, string payment_type, string reference_number, DateTime payment_date, decimal amount_bs = 0, string bank_name = "", string observations = "")
        {
            try
            {
                payment new_payment = new payment(
                    id_delivery_note, payment_date,
                    amount_usd, amount_bs, exchange_rate, payment_type, reference_number, bank_name, observations);
                await _payment_service.register_payment_async(new_payment);
                System.Windows.MessageBox.Show("Pago registrado exitosamente.", "Exito");
                await Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    System.Windows.Application.Current.Dispatcher.Invoke(() => load_all_async());
                });
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
        }

        public async Task<accounts_receivable_dto?> search_note_async(string note_number)
        {
            return await _receivable_service.search_note_by_number_async(note_number);
        }

        public async Task<IEnumerable<payment_dto>> get_payments_by_note_async(int id_delivery_note)
        {
            return await _payment_service.get_payments_by_note_async(id_delivery_note);
        }

        public async Task update_payment_async(payment_dto dto, decimal amount_usd, decimal? exchange_rate, string payment_type, string reference_number, DateTime payment_date, decimal amount_bs = 0, string bank_name = "", string observations = "")
        {
            try
            {
                payment updated = new payment(
                    dto.id_delivery_note, payment_date,
                    amount_usd, amount_bs, exchange_rate, payment_type, reference_number, bank_name, observations)
                {
                    id_payment = dto.id_payment
                };
                await _payment_service.update_payment_async(updated);
                await Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    System.Windows.Application.Current.Dispatcher.Invoke(() => load_all_async());
                });
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
        }

        public async Task<IEnumerable<string>> get_all_months_async()
        {
            return await _receivable_service.get_all_months_async();
        }

        public async Task<IEnumerable<accounts_receivable_dto>> get_notes_by_month_async(string month_year)
        {
            return await _receivable_service.get_all_by_month_async(month_year);
        }

        private async void execute_month_report(object? parameter)
        {
            try
            {
                var payments = (await _payment_service.get_payments_by_month_async(_selected_month)).ToList();
                PaymentsReportPdfGenerator.generate(_selected_month, payments);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al generar el reporte: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
