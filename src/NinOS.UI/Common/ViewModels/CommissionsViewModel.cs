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
        public DateTime dispatch_date { get; set; }                    // FECHA DE LA NOTA DE ENTREGA
        public decimal invoiced_amount { get; set; }                   // MONTO FACTURADO
        public decimal early_payment_discount { get; set; }            // DESCUENTO PRONTO PAGO QUE ES EL PRECIO CON LOS DESCUENTOS APLICADOS AUN NO TRABAJREMOS ESTO PERO MANTEN LA IDEA AQUI DEBE SALIR CUANTO ES EL MONTO DEL DESCUENTO EN USD
        public decimal volume_discount { get; set; }                   // DESCUENTO POR VOLUMEN PARA LO DE ARRIBA
        public decimal commission_10 { get; set; }                     // 10% COMISIÓN
        public decimal updated_balance { get; set; }                   // SALDO ACTUALIZADO POR COBRAR
        public DateTime? commission_cancelled_date { get; set; }       // FECHA COMISIÓN PAGADA
        public decimal updated_receivable { get; set; }                // POR COBRAR ACTUALIZADO
        public decimal amount_bs { get; set; }
        public decimal exchange_rate { get; set; }
        public string reference_number { get; set; } = string.Empty;
        public DateTime? payout_date { get; set; }
        public bool is_paid { get; set; }
        public string status
        {
            get
            {
                if (remaining_amount_usd <= 0.005m) return "Pagada";
                return paid_amount_usd > 0 ? "Parcial" : "Pendiente";
            }
        }
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
        public ObservableCollection<commission_row_dto> juan_luis_rows { get; }

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
        public ICommand print_commission_pdf_command { get; }
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
            juan_luis_rows = new ObservableCollection<commission_row_dto>();

            filter_options.Add("Pendientes");
            filter_options.Add("Pagadas");
            filter_options.Add("Todas");

            add_commission_payment_command = new RelayCommand(execute_add_commission_payment);
            print_commission_pdf_command = new RelayCommand(execute_print_commission_pdf);

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
                pending_months.Add("");
                foreach (var m in unique_months) pending_months.Add(m);

                _all_rows_source = all_rows;

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
            var filtered = filter_by_month_and_search(_all_rows_source, _selected_month, query);

            if (_selected_filter == "Pendientes")
                filtered = filtered.Where(n => n.remaining_amount_usd > 0.005m).ToList();
            else if (_selected_filter == "Pagadas")
                filtered = filtered.Where(n => n.remaining_amount_usd <= 0.005m).ToList();

            update_collection(all_rows, filtered);
            update_collection(sandra_rows, filtered.Where(n => n.seller_name == "Sandra").ToList());
            update_collection(anais_rows, filtered.Where(n => n.seller_name == "Anais").ToList());
            update_collection(alejandra_rows, filtered.Where(n => n.seller_name == "Alejandra").ToList());
            update_collection(juan_luis_rows, filtered.Where(n => n.seller_name == "Juan Luis").ToList());

            recalc_totals();
        }

        private List<commission_row_dto> filter_by_month_and_search(List<commission_row_dto> source, string selected_month, string query)
        {
            if (string.IsNullOrEmpty(selected_month))
                return new List<commission_row_dto>();

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
                4 => juan_luis_rows.ToList(),
                _ => new List<commission_row_dto>()
            };

            total_sold_usd = list.Sum(n => n.sale_amount_usd);
            total_commission_usd = list.Sum(n => n.amount_usd);
            total_pending_usd = list.Where(n => n.remaining_amount_usd > 0.005m).Sum(n => n.remaining_amount_usd);
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
                0 => all_rows.Where(r => r.remaining_amount_usd > 0.005m).ToList(),
                1 => sandra_rows.Where(r => r.remaining_amount_usd > 0.005m).ToList(),
                2 => anais_rows.Where(r => r.remaining_amount_usd > 0.005m).ToList(),
                3 => alejandra_rows.Where(r => r.remaining_amount_usd > 0.005m).ToList(),
                4 => juan_luis_rows.Where(r => r.remaining_amount_usd > 0.005m).ToList(),
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

        private async void execute_print_commission_pdf(object? parameter)
        {
            if (parameter is not commission_row_dto row) return;

            try
            {
                var payments = (await _commission_service.get_commission_payments_async(row.id_commission)).ToList();
                if (payments.Count == 0)
                {
                    System.Windows.MessageBox.Show("No hay pagos de comision para imprimir.", "Imprimir",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                string reference = payments[0].reference_number;
                // Un mismo pago (referencia) puede cubrir varias notas; el comprobante las incluye todas.
                var receipt = await _commission_service.get_commission_receipt_async(new[] { row.id_commission }, reference);
                if (receipt == null || receipt.rows.Count == 0)
                {
                    System.Windows.MessageBox.Show("No se encontro informacion del comprobante.", "Comprobante",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                CommissionPdfGenerator.generate(receipt);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task<bool> pay_commissions_async(int[] id_commissions, decimal amount_usd, decimal exchange_rate, string payment_type, string reference_number, decimal amount_bs, DateTime payment_date, string bank_name, string observations)
        {
            try
            {
                await _commission_service.register_commission_payment_async(id_commissions, amount_usd, exchange_rate, payment_type, reference_number, amount_bs, payment_date, bank_name, observations);
                System.Windows.MessageBox.Show("Comision(es) liquidadas exitosamente.", "Exito");
                load_all_async();
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error");
                return false;
            }
        }

        public async Task<IEnumerable<commission_payment_dto>> get_commission_payments_async(int id_commission)
        {
            return await _commission_service.get_commission_payments_async(id_commission);
        }

        public async Task update_commission_payment_async(int id_commission_payment, decimal amount_usd, decimal exchange_rate, string payment_type, string reference_number, decimal amount_bs, DateTime payment_date, string bank_name, string observations)
        {
            await _commission_service.update_commission_payment_async(id_commission_payment, amount_usd, exchange_rate, payment_type, reference_number, amount_bs, payment_date, bank_name, observations);
            load_all_async();
        }

        public async Task<commission_receipt_dto?> get_commission_receipt_async(int[] commission_ids, string reference_number)
        {
            return await _commission_service.get_commission_receipt_async(commission_ids, reference_number);
        }
    }
}