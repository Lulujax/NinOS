using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class accounts_receivable_row_dto : INotifyPropertyChanged
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public int id_seller { get; set; }
        public DateTime creation_date { get; set; }
        public DateTime? dispatch_date { get; set; }
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
        public string month_key { get; set; } = string.Empty;

        private string _observations = string.Empty;

        public string observations
        {
            get => _observations;
            set { _observations = value ?? string.Empty; on_property_changed(); }
        }

        public string? saved_observations { get; set; }

        private bool _is_editing_observations;

        public bool is_editing_observations
        {
            get => _is_editing_observations;
            set { _is_editing_observations = value; on_property_changed(); }
        }

        public decimal? discount_condition_percentage { get; set; }
        public decimal? discount_volume_percentage { get; set; }

        private bool _is_editing;
        private string _edit_monto_text = string.Empty;
        private string _edit_dcto_text = string.Empty;
        private bool _syncing;
        public bool edited_dcto_directly;

        private decimal _edit_base_total;
        public decimal edit_result_condition_pct { get; private set; }

        public bool is_editing
        {
            get => _is_editing;
            set { _is_editing = value; on_property_changed(); }
        }

        public string edit_monto_text
        {
            get => _edit_monto_text;
            set
            {
                if (_edit_monto_text == value) return;
                _edit_monto_text = value;
                on_property_changed();
            }
        }

        public string edit_dcto_text
        {
            get => _edit_dcto_text;
            set
            {
                if (_edit_dcto_text == value) return;
                _edit_dcto_text = value;
                on_property_changed();
                if (!_syncing)
                {
                    _syncing = true;
                    edited_dcto_directly = true;
                    apply_dcto_to_monto();
                    _syncing = false;
                }
            }
        }

        public void begin_edit()
        {
            // En CxC el DCTO de trabajo es un único valor: condición + volumen (si aún no se ha compactado).
            decimal cond = (discount_condition_percentage ?? 0) + (discount_volume_percentage ?? 0);
            decimal total = total_amount_usd;

            decimal denom = 1 - cond / 100m;
            _edit_base_total = denom > 0.0000001m
                ? total / denom
                : (gross_total_usd > 0 ? gross_total_usd : total);
            edit_result_condition_pct = cond;

            _edit_monto_text = $"{total:0.##}";
            _edit_dcto_text = cond > 0 ? $"{cond:0.###}" : "0";
            on_property_changed(nameof(edit_monto_text));
            on_property_changed(nameof(edit_dcto_text));
            edited_dcto_directly = false;
        }

        private void apply_dcto_to_monto()
        {
            decimal dcto = parse_numeric(_edit_dcto_text);
            if (dcto < 0) dcto = 0;
            if (dcto > 100) dcto = 100;

            edit_result_condition_pct = dcto;

            decimal monto = _edit_base_total * (1 - dcto / 100m);
            edit_monto_text = $"{Math.Max(0, monto):0.###}";
        }

        public static decimal parse_numeric(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return -1;
            string t = text.Trim().Replace(" ", "");
            if (t.Length == 0) return -1;

            string sep = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
            string normalized;
            int last_comma = t.LastIndexOf(',');
            int last_dot = t.LastIndexOf('.');

            if (last_comma >= 0 && last_dot >= 0)
            {
                if (last_comma > last_dot)
                    normalized = t.Replace(".", "").Replace(",", sep);
                else
                    normalized = t.Replace(",", "").Replace(".", sep);
            }
            else if (last_comma >= 0)
                normalized = t.Replace(",", sep);
            else if (last_dot >= 0)
                normalized = t.Replace(".", sep);
            else
                normalized = t;

            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)) return result;
            return -1;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void on_property_changed([CallerMemberName] string? property_name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        }
    }

    public class AccountsReceivableViewModel : ViewModelBase
    {
        private readonly IAccountsReceivableService _receivable_service;

        private int _selected_tab_index;
        private string _search_query = string.Empty;
        private string _selected_month = string.Empty;
        private string _selected_filter = "Por Cobrar";
        private decimal _total_invoiced_usd;
        private decimal _total_paid_usd;
        private decimal _total_balance_usd;
        private bool _is_loading;
        private accounts_receivable_row_dto? _selected_note;

        private List<accounts_receivable_row_dto> _all_notes_source = new();

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<string> filter_options { get; }
        public ObservableCollection<accounts_receivable_row_dto> all_notes { get; }
        public ObservableCollection<accounts_receivable_row_dto> sandra_notes { get; }
        public ObservableCollection<accounts_receivable_row_dto> anais_notes { get; }
        public ObservableCollection<accounts_receivable_row_dto> alejandra_notes { get; }
        public ObservableCollection<accounts_receivable_row_dto> juan_luis_notes { get; }

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

        public int filter_mode => _selected_filter switch
        {
            "Anuladas" => 1,
            "Todas" => 2,
            _ => 0
        };

        public string selected_filter
        {
            get => _selected_filter;
            set { if (_selected_filter == value) return; _selected_filter = value; on_property_changed(); apply_filters(); }
        }

        public accounts_receivable_row_dto? selected_note
        {
            get => _selected_note;
            set { _selected_note = value; on_property_changed(); }
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

        public ICommand annul_note_command { get; }
        public ICommand preview_note_command { get; }
        public ICommand print_pdf_command { get; }
        public ICommand start_edit_command { get; }
        public ICommand apply_edit_command { get; }
        public ICommand cancel_edit_command { get; }
        public ICommand request_payment_command { get; }
        public ICommand add_payment_command { get; }
        public ICommand month_report_command { get; }

        public Action<accounts_receivable_row_dto>? on_request_preview_window;
        public Action? on_request_confirmation_window;
        public Action<accounts_receivable_row_dto>? on_request_add_payment_for_note;
        public Action<string>? on_request_add_payment_for_month;

        public AccountsReceivableViewModel(IAccountsReceivableService receivable_service)
        {
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));

            pending_months = new ObservableCollection<string>();
            filter_options = new ObservableCollection<string>();
            all_notes = new ObservableCollection<accounts_receivable_row_dto>();
            sandra_notes = new ObservableCollection<accounts_receivable_row_dto>();
            anais_notes = new ObservableCollection<accounts_receivable_row_dto>();
            alejandra_notes = new ObservableCollection<accounts_receivable_row_dto>();
            juan_luis_notes = new ObservableCollection<accounts_receivable_row_dto>();

            filter_options.Add("Por Cobrar");
            filter_options.Add("Anuladas");
            filter_options.Add("Todas");

            annul_note_command = new RelayCommand(execute_annul_note);
            preview_note_command = new RelayCommand(execute_preview_note);
            print_pdf_command = new RelayCommand(execute_print_pdf);
            start_edit_command = new RelayCommand(execute_start_edit);
            apply_edit_command = new RelayCommand(execute_apply_edit);
            cancel_edit_command = new RelayCommand(execute_cancel_edit);
            request_payment_command = new RelayCommand(execute_request_payment);
            add_payment_command = new RelayCommand(execute_add_payment);

            month_report_command = new RelayCommand(execute_month_report);

            _ = load_all_async();
        }

        public void refresh_data() => _ = load_all_async();

        private async Task load_all_async()
        {
            try
            {
                _is_loading = true;

                var raw = await _receivable_service.get_all_notes_async();
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
            var filtered = filter_by_month_and_search(_all_notes_source, _selected_month, query);

            if (_selected_filter == "Por Cobrar")
                filtered = filtered.Where(n => n.status == "Pendiente").ToList();
            else if (_selected_filter == "Anuladas")
                filtered = filtered.Where(n => n.status == "Anulada").ToList();
            else if (_selected_filter == "Todas")
                filtered = filtered.Where(n => n.status != "Pagada").ToList();

            update_collection(all_notes, filtered);
            update_collection(sandra_notes, filtered.Where(n => n.seller_name == "Sandra").ToList());
            update_collection(anais_notes, filtered.Where(n => n.seller_name == "Anais").ToList());
            update_collection(alejandra_notes, filtered.Where(n => n.seller_name == "Alejandra").ToList());
            update_collection(juan_luis_notes, filtered.Where(n => n.seller_name == "Juan Luis").ToList());

            recalc_totals();
        }

        private List<accounts_receivable_row_dto> filter_by_month_and_search(List<accounts_receivable_row_dto> source, string selected_month, string query)
        {
            if (string.IsNullOrEmpty(selected_month))
                return new List<accounts_receivable_row_dto>();

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
                0 => all_notes.ToList(),
                1 => sandra_notes.ToList(),
                2 => anais_notes.ToList(),
                3 => alejandra_notes.ToList(),
                4 => juan_luis_notes.ToList(),
                _ => new List<accounts_receivable_row_dto>()
            };
            list = list.Where(n => n.status != "Anulada").ToList();

            total_invoiced_usd = list.Sum(n => n.total_amount_usd);
            total_paid_usd = list.Sum(n => n.paid_amount_usd);
            total_balance_usd = list.Sum(n => n.balance_due_usd);
        }

        private accounts_receivable_row_dto map_to_row(accounts_receivable_dto n)
        {
            return new accounts_receivable_row_dto
            {
                id_delivery_note = n.id_delivery_note,
                note_number = n.note_number,
                customer_name = n.customer_name,
                seller_name = n.seller_name,
                id_seller = n.id_seller,
                creation_date = n.creation_date,
                dispatch_date = n.dispatch_date,
                total_amount_usd = n.total_amount_usd,
                gross_total_usd = n.gross_total_usd,
                discount_amount = n.discount_amount,
                discount_condition_percentage = n.discount_percentage,
                discount_volume_percentage = n.volume_discount_percentage,
                discount_percentage_text = (n.discount_percentage.HasValue || n.volume_discount_percentage.HasValue)
                    ? $"{((n.discount_percentage ?? 0) + (n.volume_discount_percentage ?? 0)):0.##}%"
                    : (n.gross_total_usd > 0 ? $"{((n.discount_amount / n.gross_total_usd) * 100):0.##}%" : "0%"),
                paid_amount_usd = n.paid_amount_usd,
                balance_due_usd = n.balance_due_usd,
                last_payment_date = n.last_payment_date,
                payment_method_text = n.payment_method_text,
                bank_name_text = n.bank_name_text,
                status = n.status,
                observations = n.cxc_observations,
                month_key = new DateTime(n.creation_date.Year, n.creation_date.Month, 1)
                    .ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE"))
            };
        }

        private void update_collection(ObservableCollection<accounts_receivable_row_dto> collection, List<accounts_receivable_row_dto> items)
        {
            collection.Clear();
            foreach (var item in items) collection.Add(item);
        }

        private void execute_annul_note(object? parameter)
        {
            if (parameter is accounts_receivable_row_dto note)
            {
                selected_note = note;
                on_request_confirmation_window?.Invoke();
            }
        }

        public async Task confirm_annulation_async()
        {
            if (selected_note == null) return;
            try
            {
                await _receivable_service.annul_delivery_note_async(selected_note.id_delivery_note);
                selected_note = null;
                await load_all_async();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_preview_note(object? parameter)
        {
            if (parameter is accounts_receivable_row_dto note)
                on_request_preview_window?.Invoke(note);
        }

        public async Task<note_print_dto> get_printable_note_async(int id_delivery_note)
        {
            return await _receivable_service.get_printable_note_async(id_delivery_note);
        }

        private async void execute_print_pdf(object? parameter)
        {
            if (parameter is not accounts_receivable_row_dto note) return;
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

        private void execute_start_edit(object? parameter)
        {
            if (parameter is not accounts_receivable_row_dto note) return;
            if (note.status == "Pagada")
            {
                System.Windows.MessageBox.Show("Esta nota ya esta pagada y no puede editarse.", "Editar Nota",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
            if (note.status == "Anulada") return;
            foreach (var row in _all_notes_source) row.is_editing = false;
            note.begin_edit();
            note.is_editing = true;
        }

        private void execute_cancel_edit(object? parameter)
        {
            if (parameter is accounts_receivable_row_dto note) note.is_editing = false;
        }

        private async void execute_apply_edit(object? parameter)
        {
            if (parameter is not accounts_receivable_row_dto note) return;
            if (note.status == "Anulada" || note.status == "Pagada") { note.is_editing = false; return; }
            try
            {
                decimal monto = accounts_receivable_row_dto.parse_numeric(note.edit_monto_text);
                if (monto < 0) throw new InvalidOperationException("El monto no puede ser negativo.");
                decimal dcto_entered = string.IsNullOrWhiteSpace(note.edit_dcto_text)
                    ? 0
                    : accounts_receivable_row_dto.parse_numeric(note.edit_dcto_text);
                if (dcto_entered < 0 || dcto_entered > 100)
                    throw new InvalidOperationException("El porcentaje de descuento debe estar entre 0 y 100.");

                decimal adjusted = Math.Max(0, monto);

                // CxC trabaja un único DCTO: se guarda en discount_percentage y se limpia el volumen de trabajo.
                decimal dcto = note.edit_result_condition_pct;

                await _receivable_service.update_note_total_async(note.id_delivery_note, adjusted, dcto, null);

                note.is_editing = false;
                await load_all_async();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al guardar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_request_payment(object? parameter)
        {
            if (parameter is accounts_receivable_row_dto note)
                on_request_add_payment_for_note?.Invoke(note);
        }

        public async Task save_observations_async(accounts_receivable_row_dto note, string text)
        {
            try
            {
                await _receivable_service.update_note_cxc_observations_async(note.id_delivery_note, text);
                note.observations = text;
            }
            catch (Exception ex)
            {
                note.observations = note.saved_observations ?? string.Empty;
                System.Windows.MessageBox.Show($"No se pudo guardar la observación: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task save_dispatch_date_async(accounts_receivable_row_dto note, DateTime? date)
        {
            if (date == note.dispatch_date) return;
            try
            {
                await _receivable_service.update_note_dispatch_date_async(note.id_delivery_note, date);
                note.dispatch_date = date;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo guardar la fecha de despacho: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_add_payment(object? parameter)
        {
            on_request_add_payment_for_month?.Invoke(selected_month);
        }

        private void execute_month_report(object? parameter)
        {
            try
            {
                var month_rows = filter_by_month_and_search(_all_notes_source, _selected_month, string.Empty)
                    .Where(n => n.status != "Anulada")
                    .ToList();

                var report = new monthly_report_dto
                {
                    title = "CUENTAS POR COBRAR - DETALLE DEL MES",
                    month = _selected_month,
                    report_name = "cuentas por cobrar",
                    detail_column_header = "SALDO",
                    show_paid_balance_summary = true,
                    empty_text = "Sin cuentas por cobrar para el mes seleccionado.",
                    rows = month_rows
                        .OrderBy(n => n.creation_date)
                        .Select(n => new monthly_report_row_dto
                        {
                            date = n.creation_date,
                            document_number = n.note_number,
                            customer_name = n.customer_name,
                            seller_name = n.seller_name,
                            amount_usd = n.total_amount_usd,
                            paid_amount_usd = n.paid_amount_usd,
                            balance_due_usd = n.balance_due_usd,
                            detail_text = $"{n.balance_due_usd:N2}",
                            status = n.status
                        })
                        .ToList()
                };

                MonthlyReportPdfGenerator.generate(report);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al generar el reporte: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}