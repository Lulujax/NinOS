using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;
using NinOS.UI.Views;

namespace NinOS.UI.Common.ViewModels
{
    public class accounts_receivable_row_dto : INotifyPropertyChanged
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public int id_seller { get; set; }

        // Zona del cliente de la nota. Se usa para agrupar el reporte de CxC por zona.
        public int? id_zona { get; set; }
        public string zone_name { get; set; } = string.Empty;

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
        public bool is_customer_ghost { get; set; }

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
        private readonly ISellerService? _seller_service;
        private readonly IZonaService? _zona_service;

        private SellerTabItem<accounts_receivable_row_dto>? _selected_tab;
        private string _search_query = string.Empty;
        private string _selected_month = string.Empty;
        private string _selected_filter = "Por Cobrar";
        private decimal _total_invoiced_usd;
        private decimal _total_paid_usd;
        private decimal _total_balance_usd;
        private bool _is_loading;
        private accounts_receivable_row_dto? _selected_note;

        // --- Configuracion del reporte de CxC ---
        // Arranca en "Por Vendedor". No hay modo de "cambiar alcance": las checklists de
        // vendedores y zonas son directamente el filtro.
        private string _selected_report_group = "Por Vendedor";
        private string _report_primary_mode = "Mes"; // "Mes", "Año", "Por Zona"
        private string _report_zone_subperiod = "Todas"; // "Todas", "Mes", "Año"
        private int _selected_report_year;
        private bool _all_report_sellers_selected = true;
        private bool _all_report_zones_selected = true;
        private bool _suppress_seller_sync;
        private bool _suppress_zone_sync;
        private Dictionary<string, int> _seller_name_to_id = new(StringComparer.OrdinalIgnoreCase);

        private List<accounts_receivable_row_dto> _all_notes_source = new();

        public ObservableCollection<SellerTabItem<accounts_receivable_row_dto>> seller_tabs { get; } = new();

        private int _selected_year = DateTime.Now.Year;
        public ObservableCollection<int> available_years { get; } = new ObservableCollection<int>();
        public ObservableCollection<string> available_months { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> pending_months => available_months;

        public int selected_year
        {
            get => _selected_year;
            set
            {
                if (_selected_year == value) return;
                _selected_year = value;
                on_property_changed();
                update_available_months();
                if (!_is_loading) apply_filters();
            }
        }
        public ObservableCollection<int> pending_years { get; }
        public ObservableCollection<string> filter_options { get; }
        public ObservableCollection<accounts_receivable_row_dto> all_notes { get; }
        public ObservableCollection<accounts_receivable_row_dto> sandra_notes { get; }
        public ObservableCollection<accounts_receivable_row_dto> anais_notes { get; }
        public ObservableCollection<accounts_receivable_row_dto> alejandra_notes { get; }
        public ObservableCollection<accounts_receivable_row_dto> juan_luis_notes { get; }

        public ObservableCollection<string> report_group_options { get; } = new();
        public ObservableCollection<filter_selection_option> report_seller_options { get; } = new();
        public ObservableCollection<filter_selection_option> report_zone_options { get; } = new();

        public string selected_report_group
        {
            get => _selected_report_group;
            set
            {
                if (_selected_report_group == value) return;
                _selected_report_group = string.IsNullOrWhiteSpace(value) ? "Por Vendedor" : value;
                on_property_changed();
            }
        }

        public bool all_report_sellers_selected
        {
            get => _all_report_sellers_selected;
            set
            {
                if (_all_report_sellers_selected == value) return;
                _all_report_sellers_selected = value;
                on_property_changed();
                foreach (var opt in report_seller_options) opt.is_checked = value;
            }
        }

        public bool all_report_zones_selected
        {
            get => _all_report_zones_selected;
            set
            {
                if (_all_report_zones_selected == value) return;
                _all_report_zones_selected = value;
                on_property_changed();
                foreach (var opt in report_zone_options) opt.is_checked = value;
            }
        }

        public string selected_month
        {
            get => _selected_month;
            set { if (_selected_month == value) return; _selected_month = value ?? string.Empty; on_property_changed(); if (!_is_loading) apply_filters(); }
        }

        public string report_primary_mode
        {
            get => _report_primary_mode;
            set
            {
                if (_report_primary_mode == value) return;
                _report_primary_mode = value;
                on_property_changed();
                on_property_changed(nameof(is_report_zone_mode));
                on_property_changed(nameof(is_report_year_mode));
                on_property_changed(nameof(is_report_month_mode));
            }
        }

        public bool is_report_zone_mode => _report_primary_mode == "Por Zona";

        public string report_zone_subperiod
        {
            get => _report_zone_subperiod;
            set
            {
                if (_report_zone_subperiod == value) return;
                _report_zone_subperiod = value;
                on_property_changed();
            }
        }

        /// <summary>
        /// Cuando es true el reporte de CxC se arma por año completo en vez de por mes.
        /// Solo afecta al PDF del popup de reporte; el combo de la barra de tools
        /// sigue siendo mensual para el grid.
        /// </summary>
        public bool is_report_year_mode
        {
            get => _report_primary_mode == "Año";
            set
            {
                if (value) report_primary_mode = "Año";
                else if (_report_primary_mode == "Año") report_primary_mode = "Mes";
            }
        }

        public bool is_report_month_mode
        {
            get => _report_primary_mode == "Mes";
            set
            {
                if (value) report_primary_mode = "Mes";
                else if (_report_primary_mode == "Mes") report_primary_mode = "Año";
            }
        }

        public int selected_report_year
        {
            get => _selected_report_year;
            set { if (_selected_report_year == value) return; _selected_report_year = value; on_property_changed(); }
        }

        public SellerTabItem<accounts_receivable_row_dto>? selected_tab
        {
            get => _selected_tab;
            set
            {
                if (_selected_tab == value) return;
                _selected_tab = value;
                on_property_changed();
                on_property_changed(nameof(selected_tab_index));
                if (!_is_loading)
                {
                    recalc_totals();
                }
            }
        }

        public int selected_tab_index
        {
            get
            {
                if (_selected_tab == null) return 0;
                int idx = seller_tabs.IndexOf(_selected_tab);
                return idx >= 0 ? idx : 0;
            }
            set
            {
                if (value >= 0 && value < seller_tabs.Count)
                {
                    selected_tab = seller_tabs[value];
                }
            }
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
        public ICommand month_report_preview_command { get; }
        public ICommand month_report_print_command { get; }
        public ICommand month_report_excel_command { get; }
        public ICommand export_note_excel_command { get; }
        public ICommand clear_report_sellers_command { get; }
        public ICommand clear_report_zones_command { get; }
        public ICommand select_all_report_options_command { get; }
        public ICommand clear_all_report_options_command { get; }

        public Action<accounts_receivable_row_dto>? on_request_preview_window;
        public Action? on_request_confirmation_window;
        public Action<accounts_receivable_row_dto>? on_request_add_payment_for_note;
        public Action<string>? on_request_add_payment_for_month;

        public AccountsReceivableViewModel(
            IAccountsReceivableService receivable_service,
            ISellerService? seller_service = null,
            IZonaService? zona_service = null)
        {
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));
            _seller_service = seller_service;
            _zona_service = zona_service;

            // available_months already initialized
            pending_years = new ObservableCollection<int>();
            filter_options = new ObservableCollection<string>();
            all_notes = new ObservableCollection<accounts_receivable_row_dto>();
            sandra_notes = new ObservableCollection<accounts_receivable_row_dto>();
            anais_notes = new ObservableCollection<accounts_receivable_row_dto>();
            alejandra_notes = new ObservableCollection<accounts_receivable_row_dto>();
            juan_luis_notes = new ObservableCollection<accounts_receivable_row_dto>();

            filter_options.Add("Por Cobrar");
            filter_options.Add("Anuladas");
            filter_options.Add("Devueltas");
            filter_options.Add("Todas");

            report_group_options.Add("Por Vendedor");
            report_group_options.Add("Por Zona");

            annul_note_command = new RelayCommand(execute_annul_note);
            preview_note_command = new RelayCommand(execute_preview_note);
            print_pdf_command = new RelayCommand(execute_print_pdf);
            start_edit_command = new RelayCommand(execute_start_edit);
            apply_edit_command = new RelayCommand(execute_apply_edit);
            cancel_edit_command = new RelayCommand(execute_cancel_edit);
            request_payment_command = new RelayCommand(execute_request_payment);
            add_payment_command = new RelayCommand(execute_add_payment);

            month_report_command = new RelayCommand(execute_month_report);
            month_report_preview_command = new RelayCommand(execute_month_report_preview);
            month_report_print_command = new RelayCommand(execute_month_report_print);
            month_report_excel_command = new RelayCommand(execute_month_report_excel);
            export_note_excel_command = new RelayCommand(execute_export_note_excel);
            clear_report_sellers_command = new RelayCommand(execute_clear_report_sellers);
            clear_report_zones_command = new RelayCommand(execute_clear_report_zones);
            select_all_report_options_command = new RelayCommand(execute_select_all_report_options);
            clear_all_report_options_command = new RelayCommand(execute_clear_all_report_options);

            AppDataEvents.CatalogsChanged += () =>
            {
                System.Windows.Application.Current?.Dispatcher?.InvokeAsync(refresh_data);
            };

            _ = load_all_async();
        }

        public void refresh_data() => _ = load_all_async();

        private void sync_seller_tabs(IEnumerable<seller> sellers)
        {
            var activeSellers = sellers.Where(s => s.is_active && !s.is_pro_venta_only).OrderBy(s => s.seller_code).ToList();

            if (seller_tabs.Count == 0 || seller_tabs[0].IdSeller != null)
            {
                seller_tabs.Insert(0, new SellerTabItem<accounts_receivable_row_dto>(null, "Todos"));
            }
            else
            {
                seller_tabs[0].Header = "Todos";
            }

            var currentSellerTabs = seller_tabs.Skip(1).ToList();
            foreach (var tab in currentSellerTabs)
            {
                if (!activeSellers.Any(s => s.id_seller == tab.IdSeller))
                {
                    seller_tabs.Remove(tab);
                }
            }

            int targetIndex = 1;
            foreach (var s in activeSellers)
            {
                var existing = seller_tabs.FirstOrDefault(t => t.IdSeller == s.id_seller);
                if (existing == null)
                {
                    var newTab = new SellerTabItem<accounts_receivable_row_dto>(s.id_seller, s.full_name ?? string.Empty);
                    seller_tabs.Insert(targetIndex, newTab);
                }
                else
                {
                    existing.Header = s.full_name ?? string.Empty;
                    int currentIndex = seller_tabs.IndexOf(existing);
                    if (currentIndex != targetIndex)
                    {
                        seller_tabs.Move(currentIndex, targetIndex);
                    }
                }
                targetIndex++;
            }

            if (selected_tab == null || !seller_tabs.Contains(selected_tab))
            {
                selected_tab = seller_tabs[0];
            }
        }

        /// <summary>
        /// Arma las checklists de vendedores y zonas del popup de reporte. Mismo criterio que
        /// el de Ventas: primero los activos y despues cualquiera que tenga notas en la base,
        /// aunque este desactivado, para que sus notas no queden fuera del reporte sin avisar.
        /// </summary>
        private void sync_report_options(
            IEnumerable<seller> sellers,
            List<zona> zones,
            List<accounts_receivable_row_dto> all_rows)
        {
            foreach (var s in sellers)
            {
                string name = (s.full_name ?? string.Empty).Trim();
                if (name.Length == 0) continue;
                _seller_name_to_id[name] = s.id_seller;
            }

            var generalSellers = sellers
                .Where(s => !s.is_pro_venta_only)
                .ToList();

            var active_seller_names = generalSellers
                .Select(s => (s.full_name ?? string.Empty).Trim())
                .Where(n => n.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var report_seller_rows = generalSellers
                .OrderBy(s => s.seller_code)
                .Select(s => (name: (s.full_name ?? string.Empty).Trim(), active: true, id: (int?)s.id_seller))
                .Where(t => t.name.Length > 0)
                .ToList();

            foreach (var note_seller in all_rows
                .Select(n => (n.seller_name ?? string.Empty).Trim())
                .Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (active_seller_names.Contains(note_seller)) continue;
                int? note_seller_id = _seller_name_to_id.TryGetValue(note_seller, out var mapped_id) ? (int?)mapped_id : null;
                report_seller_rows.Add((note_seller, false, note_seller_id));
            }

            _suppress_seller_sync = true;
            report_seller_options.Clear();
            foreach (var (name, active, id) in report_seller_rows)
            {
                report_seller_options.Add(new filter_selection_option(
                    name, on_report_seller_option_changed, true, id,
                    active ? name : $"{name} (inactivo)"));
            }
            _suppress_seller_sync = false;
            _all_report_sellers_selected = true;
            on_property_changed(nameof(all_report_sellers_selected));

            var active_zone_names = zones
                .Select(z => z.name.Trim())
                .Where(n => n.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var report_zone_rows = zones
                .Select(z => (name: z.name.Trim(), active: true))
                .Where(t => t.name.Length > 0)
                .ToList();

            foreach (var note_zone in all_rows
                .Select(n => string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!active_zone_names.Contains(note_zone))
                    report_zone_rows.Add((note_zone, false));
            }

            _suppress_zone_sync = true;
            report_zone_options.Clear();
            foreach (var (name, active) in report_zone_rows)
            {
                report_zone_options.Add(new filter_selection_option(
                    name, on_report_zone_option_changed, true, null,
                    active ? name : $"{name} (inactiva)"));
            }
            _suppress_zone_sync = false;
            _all_report_zones_selected = true;
            on_property_changed(nameof(all_report_zones_selected));

            // Las checklists arrancan reflejando la pestaña de vendedor activa.
            sync_report_scope_from_screen();
        }

        private void on_report_seller_option_changed()
        {
            if (_suppress_seller_sync) return;
            bool allChecked = report_seller_options.Count > 0 && report_seller_options.All(o => o.is_checked);
            if (_all_report_sellers_selected != allChecked)
            {
                _all_report_sellers_selected = allChecked;
                on_property_changed(nameof(all_report_sellers_selected));
            }
        }

        private void on_report_zone_option_changed()
        {
            if (_suppress_zone_sync) return;
            bool allChecked = report_zone_options.Count > 0 && report_zone_options.All(o => o.is_checked);
            if (_all_report_zones_selected != allChecked)
            {
                _all_report_zones_selected = allChecked;
                on_property_changed(nameof(all_report_zones_selected));
            }
        }

        private void execute_clear_report_sellers(object? parameter)
        {
            _suppress_seller_sync = true;
            foreach (var opt in report_seller_options) opt.is_checked = false;
            _suppress_seller_sync = false;
            _all_report_sellers_selected = false;
            on_property_changed(nameof(all_report_sellers_selected));
        }

        private void execute_clear_report_zones(object? parameter)
        {
            _suppress_zone_sync = true;
            foreach (var opt in report_zone_options) opt.is_checked = false;
            _suppress_zone_sync = false;
            _all_report_zones_selected = false;
            on_property_changed(nameof(all_report_zones_selected));
        }

        // Seleccionar o quitar todos a la vez. Las checklists son el filtro, asi que basta
        // con tocar "Todos"/"Todas" o el boton Quitar.
        private void execute_select_all_report_options(object? parameter)
        {
            all_report_sellers_selected = true;
            all_report_zones_selected = true;
        }

        private void execute_clear_all_report_options(object? parameter)
        {
            execute_clear_report_sellers(null);
            execute_clear_report_zones(null);
        }

        /// <summary>
        /// Deja las checklists reflejando lo que esta en pantalla: la pestana de vendedor activa
        /// y todas las zonas (este modulo no filtra por zona en la grilla).
        /// </summary>
        public void sync_report_scope_from_screen()
        {
            if (_report_primary_mode == "Por Zona")
            {
                _suppress_seller_sync = true;
                foreach (var opt in report_seller_options) opt.is_checked = true;
                _suppress_seller_sync = false;
                _all_report_sellers_selected = report_seller_options.Count > 0;
                on_property_changed(nameof(all_report_sellers_selected));

                _suppress_zone_sync = true;
                foreach (var opt in report_zone_options) opt.is_checked = true;
                _suppress_zone_sync = false;
                _all_report_zones_selected = report_zone_options.Count > 0;
                on_property_changed(nameof(all_report_zones_selected));
                return;
            }

            string tab_name = (selected_tab?.Header ?? string.Empty).Trim();
            bool all_sellers = selected_tab?.IdSeller == null || tab_name.Length == 0;

            _suppress_seller_sync = true;
            foreach (var opt in report_seller_options)
                opt.is_checked = all_sellers || string.Equals(opt.name.Trim(), tab_name, StringComparison.OrdinalIgnoreCase);
            _suppress_seller_sync = false;
            _all_report_sellers_selected = report_seller_options.Count > 0 && report_seller_options.All(o => o.is_checked);
            on_property_changed(nameof(all_report_sellers_selected));

            _suppress_zone_sync = true;
            foreach (var opt in report_zone_options) opt.is_checked = true;
            _suppress_zone_sync = false;
            _all_report_zones_selected = report_zone_options.Count > 0 && report_zone_options.All(o => o.is_checked);
            on_property_changed(nameof(all_report_zones_selected));
        }

        private async Task load_all_async()
        {
            try
            {
                var current_selection = _selected_month;
                _is_loading = true;

                var dbSellers = _seller_service != null
                    ? await _seller_service.GetAllActiveAsync()
                    : await _receivable_service.get_sellers_async();
                sync_seller_tabs(dbSellers);

                var dbZones = _zona_service != null
                    ? await _zona_service.GetActiveAsync()
                    : new List<zona>();

                var raw = await _receivable_service.get_all_notes_async();
                var all_rows = raw.Select(map_to_row).ToList();

                sync_report_options(dbSellers, dbZones, all_rows);

                _all_notes_source = all_rows;

                var detected_years = all_rows
                    .Where(n => n.creation_date.Year >= 2000)
                    .Select(n => AppTimeZone.to_local(n.creation_date).Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToList();

                if (detected_years.Count == 0)
                    detected_years.Add(DateTime.Now.Year);

                available_years.Clear();
                foreach (var y in detected_years) available_years.Add(y);

                if (!available_years.Contains(_selected_year))
                {
                    _selected_year = available_years[0];
                    on_property_changed(nameof(selected_year));
                }

                update_available_months();
                sync_report_years(all_rows);

                _is_loading = false;
                apply_filters();
            }
            catch (Exception ex)
            {
                _is_loading = false;
                AppDialog.Show($"Error: {ErrorText.Get(ex)}", "Error");
            }
        }

        /// <summary>
        /// Anios con notas cargadas, mas el anio en curso para que el reporte anual
        /// siempre tenga algo seleccionado aunque aun no haya notas de este anio.
        /// </summary>
        private void sync_report_years(List<accounts_receivable_row_dto> all_rows)
        {
            var years = all_rows
                .Where(n => n.creation_date.Year >= 2000)
                .Select(n => AppTimeZone.to_local(n.creation_date).Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            int current_year = DateTime.Now.Year;
            if (!years.Contains(current_year)) years.Insert(0, current_year);

            pending_years.Clear();
            foreach (var y in years) pending_years.Add(y);

            if (pending_years.Count > 0 && !pending_years.Contains(_selected_report_year))
            {
                _selected_report_year = pending_years[0];
                on_property_changed(nameof(selected_report_year));
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
            else if (_selected_filter == "Devueltas")
                filtered = filtered.Where(n => n.status == "Devuelta").ToList();
            else if (_selected_filter == "Todas")
                filtered = filtered.Where(n => n.status != "Pagada").ToList();

            filtered = filtered
                .OrderBy(n => n.creation_date)
                .ThenBy(n => n.id_delivery_note)
                .ToList();

            update_collection(all_notes, filtered);
            update_collection(sandra_notes, filtered.Where(n => n.seller_name == "Sandra").ToList());
            update_collection(anais_notes, filtered.Where(n => n.seller_name == "Anais").ToList());
            update_collection(alejandra_notes, filtered.Where(n => n.seller_name == "Alejandra").ToList());
            update_collection(juan_luis_notes, filtered.Where(n => n.seller_name == "Juan Luis").ToList());

            foreach (var tab in seller_tabs)
            {
                if (tab.IdSeller == null)
                {
                    update_collection(tab.Items, filtered);
                }
                else
                {
                    var tabRows = filtered.Where(n => (tab.IdSeller.HasValue && n.id_seller == tab.IdSeller.Value) || string.Equals(n.seller_name?.Trim(), tab.Header?.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                    update_collection(tab.Items, tabRows);
                }
            }

            recalc_totals();
        }

        /// <summary>
        /// Filtra por anio calendario usando la fecha en hora local: la columna se guarda en UTC
        /// y una nota creada de noche caeria en el anio equivocado si se lee la columna cruda.
        /// </summary>
        private List<accounts_receivable_row_dto> filter_by_year(List<accounts_receivable_row_dto> source, int year)
        {
            if (year <= 0) return new List<accounts_receivable_row_dto>();

            return source
                .Where(n => AppTimeZone.to_local(n.creation_date).Year == year)
                .ToList();
        }

        private void update_available_months()
        {
            var months_in_year = _all_notes_source
                .Where(n => n.creation_date.Year >= 2000 && AppTimeZone.to_local(n.creation_date).Year == _selected_year)
                .Select(n => AppTimeZone.to_local(n.creation_date).Month)
                .Distinct()
                .OrderBy(m => m)
                .Select(m => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(new DateTime(_selected_year, m, 1).ToString("MMMM", new CultureInfo("es-VE"))))
                .ToList();

            var new_months = new List<string> { "Todos", "" };
            new_months.AddRange(months_in_year);

            string desired = _selected_month;
            if (!new_months.Contains(desired))
            {
                desired = "";
            }

            available_months.Clear();
            foreach (var m in new_months) available_months.Add(m);

            if (_selected_month != desired)
            {
                _selected_month = desired;
                on_property_changed(nameof(selected_month));
            }
        }

        private List<accounts_receivable_row_dto> filter_by_month_and_search(List<accounts_receivable_row_dto> source, string selected_month, string query)
        {
            if (string.IsNullOrEmpty(selected_month))
                return new List<accounts_receivable_row_dto>();

            var result = source.AsEnumerable();

            if (selected_month == "Todos")
            {
                result = result.Where(n => AppTimeZone.to_local(n.creation_date).Year == _selected_year);
            }
            else
            {
                result = result.Where(n => AppTimeZone.to_local(n.creation_date).Year == _selected_year
                                        && string.Equals(AppTimeZone.to_local(n.creation_date).ToString("MMMM", new CultureInfo("es-VE")), selected_month, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query))
            {
                result = result.Where(n =>
                    SearchText.combine(
                        n.note_number,
                        n.customer_name,
                        n.seller_name,
                        n.status,
                        n.observations,
                        n.payment_method_text,
                        n.bank_name_text,
                        n.discount_percentage_text,
                        n.month_key,
                        SearchText.date(n.creation_date),
                        SearchText.date(n.dispatch_date),
                        SearchText.date(n.last_payment_date),
                        SearchText.money(n.total_amount_usd),
                        SearchText.money(n.gross_total_usd),
                        SearchText.money(n.discount_amount),
                        SearchText.money(n.paid_amount_usd),
                        SearchText.money(n.balance_due_usd),
                        SearchText.pct(n.discount_condition_percentage),
                        SearchText.pct(n.discount_volume_percentage)
                    ).Contains(query));
            }

            return result.ToList();
        }

        private void recalc_totals()
        {
            var list = (selected_tab?.Items ?? all_notes)
                .Where(n => !string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))
                .ToList();

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
                id_zona = n.id_zona,
                zone_name = string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name,
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
                is_customer_ghost = n.is_customer_ghost,
                observations = n.cxc_observations,
                month_key = new DateTime(
                        AppTimeZone.to_local(n.creation_date).Year,
                        AppTimeZone.to_local(n.creation_date).Month, 1)
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
                if (note.status == "Devuelta")
                {
                    AppDialog.Show("Esta nota fue devuelta en su totalidad; no se puede anular.", "Aviso");
                    return;
                }
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
                AppDialog.Show($"Error: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                AppDialog.Show($"Error al generar el PDF: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_start_edit(object? parameter)
        {
            if (parameter is not accounts_receivable_row_dto note) return;
            if (note.status == "Pagada")
            {
                AppDialog.Show("Esta nota ya esta pagada y no puede editarse.", "Editar Nota",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
            if (note.status == "Devuelta")
            {
                AppDialog.Show("Esta nota fue devuelta en su totalidad y no puede editarse.", "Editar Nota",
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
            if (note.status == "Anulada" || note.status == "Pagada" || note.status == "Devuelta") { note.is_editing = false; return; }
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
                AppDialog.Show($"Error al guardar: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_request_payment(object? parameter)
        {
            if (parameter is accounts_receivable_row_dto note)
            {
                if (note.status == "Devuelta")
                {
                    AppDialog.Show("Esta nota fue devuelta en su totalidad; no admite abonos.", "Aviso");
                    return;
                }
                on_request_add_payment_for_note?.Invoke(note);
            }
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
                AppDialog.Show($"No se pudo guardar la observación: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                AppDialog.Show($"No se pudo guardar la fecha de despacho: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_add_payment(object? parameter)
        {
            string month_param = string.Empty;
            if (!string.IsNullOrWhiteSpace(_selected_month) &&
                !string.Equals(_selected_month, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                month_param = $"{_selected_month} {_selected_year}";
            }
            on_request_add_payment_for_month?.Invoke(month_param);
        }

        public monthly_report_dto? build_cxc_report_dto()
        {
            if (_report_primary_mode == "Por Zona")
            {
                if (_report_zone_subperiod == "Año" && _selected_report_year <= 0)
                {
                    AppDialog.Show("Seleccione un año para generar el reporte.", "Reporte de cuentas por cobrar",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return null;
                }
                if (_report_zone_subperiod == "Mes")
                {
                    if (_selected_report_year <= 0)
                    {
                        AppDialog.Show("Seleccione un año para generar el reporte.", "Reporte de cuentas por cobrar",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        return null;
                    }
                    if (string.IsNullOrWhiteSpace(_selected_month))
                    {
                        AppDialog.Show("Seleccione un mes para generar el reporte.", "Reporte de cuentas por cobrar",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        return null;
                    }
                }
            }
            else if (is_report_year_mode)
            {
                if (_selected_report_year <= 0)
                {
                    AppDialog.Show("Seleccione un año para generar el reporte.", "Reporte de cuentas por cobrar",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return null;
                }
            }
            else if (string.IsNullOrWhiteSpace(_selected_month))
            {
                AppDialog.Show("Seleccione un mes para generar el reporte.", "Reporte de cuentas por cobrar",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return null;
            }

            // --- Alcance: vendedores y zonas ---
            var seller_scope = report_seller_options.Where(o => o.is_checked).Select(o => o.name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var zone_scope = report_zone_options.Where(o => o.is_checked).Select(o => o.name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool is_zona = _report_primary_mode == "Por Zona";
            bool filter_sellers = !is_zona && !_all_report_sellers_selected;
            bool filter_zones = !_all_report_zones_selected;

            if (filter_sellers && seller_scope.Count == 0)
            {
                AppDialog.Show("Seleccione al menos un vendedor para incluir en el reporte.", "Reporte de cuentas por cobrar",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return null;
            }

            if (filter_zones && zone_scope.Count == 0)
            {
                AppDialog.Show("Seleccione al menos una zona para incluir en el reporte.", "Reporte de cuentas por cobrar",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return null;
            }

            List<accounts_receivable_row_dto> month_rows;
            string period_label;
            string period_title;
            string group_mode;

            if (is_zona)
            {
                group_mode = "Por Zona";
                if (_report_zone_subperiod == "Todas")
                {
                    var today = DateTime.Today;
                    month_rows = _all_notes_source
                        .Where(n => string.Equals(n.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase)
                                 && AppTimeZone.to_local(n.creation_date).Date <= today)
                        .ToList();
                    period_label = $"A LA FECHA ({today:dd/MM/yyyy})";
                    period_title = "CUENTAS POR COBRAR - COBRANZA EN CALLE POR ZONAS";
                }
                else if (_report_zone_subperiod == "Año")
                {
                    month_rows = filter_by_year(_all_notes_source, _selected_report_year)
                        .Where(n => string.Equals(n.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    period_label = $"AÑO {_selected_report_year}";
                    period_title = $"CUENTAS POR COBRAR - POR ZONAS (AÑO {_selected_report_year})";
                }
                else // Mes
                {
                    month_rows = _all_notes_source
                        .Where(n => string.Equals(n.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase)
                                 && AppTimeZone.to_local(n.creation_date).Year == _selected_report_year
                                 && (string.Equals(_selected_month, "Todos", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(AppTimeZone.to_local(n.creation_date).ToString("MMMM", new CultureInfo("es-VE")), _selected_month, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    period_label = $"{_selected_month} {_selected_report_year}".ToUpperInvariant();
                    period_title = "CUENTAS POR COBRAR - POR ZONAS";
                }
            }
            else
            {
                group_mode = string.IsNullOrWhiteSpace(_selected_report_group) ? "Por Vendedor" : _selected_report_group;
                month_rows = (is_report_year_mode
                        ? filter_by_year(_all_notes_source, _selected_report_year)
                        : filter_by_month_and_search(_all_notes_source, _selected_month, string.Empty))
                    .Where(n => string.Equals(n.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                period_label = is_report_year_mode
                    ? $"AÑO {_selected_report_year}"
                    : _selected_month;
                period_title = is_report_year_mode
                    ? "CUENTAS POR COBRAR - DETALLE DEL AÑO"
                    : "CUENTAS POR COBRAR - DETALLE DEL MES";
            }

            if (filter_sellers)
                month_rows = month_rows.Where(n => seller_scope.Contains((n.seller_name ?? string.Empty).Trim())).ToList();

            if (filter_zones)
                month_rows = month_rows.Where(n => zone_scope.Contains(string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name.Trim())).ToList();

            string seller_suffix = !filter_sellers
                ? string.Empty
                : (seller_scope.Count == 1 ? seller_scope.First().ToUpperInvariant() : "VARIOS VENDEDORES");

            string zone_suffix = string.Empty;
            if (filter_zones && zone_scope.Count == 1)
            {
                zone_suffix = zone_scope.First().ToUpperInvariant();
            }
            else if (filter_zones)
            {
                zone_suffix = filter_sellers ? "VARIOS VENDEDORES Y ZONAS" : "VARIAS ZONAS";
            }
            string combined = string.Join(" - ", new[] { seller_suffix, zone_suffix }.Where(s => !string.IsNullOrEmpty(s)));

            bool is_full_detail = is_zona || is_report_year_mode;

            return new monthly_report_dto
            {
                title = string.IsNullOrEmpty(combined)
                    ? period_title
                    : $"{period_title} ({combined})",
                month = period_label,
                report_name = is_zona ? "cuentas por cobrar por zonas" : "cuentas por cobrar",
                detail_column_header = "SALDO",
                show_paid_balance_summary = true,
                empty_text = is_zona
                    ? "Sin cuentas por cobrar pendientes para los filtros de zona seleccionados."
                    : (is_report_year_mode
                        ? "Sin cuentas por cobrar para el año seleccionado."
                        : "Sin cuentas por cobrar para el mes seleccionado."),
                group_mode = group_mode,
                ordenar_zombies_primero = is_full_detail,
                show_dispatch_date_column = is_full_detail,
                rows = month_rows
                    .OrderBy(n => is_full_detail && n.is_customer_ghost ? 0 : 1)
                    .ThenBy(n => n.creation_date)
                    .ThenBy(n => n.id_delivery_note)
                    .Select(n => new monthly_report_row_dto
                    {
                        date = n.creation_date,
                        dispatch_date = n.dispatch_date,
                        document_number = n.note_number,
                        customer_name = n.customer_name,
                        seller_name = n.seller_name,
                        zone_name = string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name,
                        amount_usd = n.total_amount_usd,
                        paid_amount_usd = n.paid_amount_usd,
                        balance_due_usd = n.balance_due_usd,
                        detail_text = $"{n.balance_due_usd:N2}",
                        status = n.status,
                        is_customer_ghost = n.is_customer_ghost
                    })
                    .ToList()
            };
        }

        private void execute_month_report(object? parameter)
        {
            try
            {
                var report = build_cxc_report_dto();
                if (report != null)
                    MonthlyReportPdfGenerator.generate(report);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al generar el reporte: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_month_report_preview(object? parameter)
        {
            try
            {
                var report = build_cxc_report_dto();
                if (report == null) return;

                var doc = MonthlyReportPdfGenerator.build_document(report);
                var fileName = MonthlyReportPdfGenerator.get_default_file_name(report);
                var preview = new PdfPreviewWindow(
                    doc,
                    report.title,
                    fileName,
                    () => MonthlyReportExcelExporter.export(report));
                preview.Owner = System.Windows.Application.Current?.MainWindow;
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error en vista previa: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_month_report_print(object? parameter)
        {
            try
            {
                var report = build_cxc_report_dto();
                if (report == null) return;

                var doc = MonthlyReportPdfGenerator.build_document(report);
                var fileName = MonthlyReportPdfGenerator.get_default_file_name(report);
                ReportPrintService.DirectPrint(doc, System.Windows.Application.Current?.MainWindow, report.title, fileName);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al imprimir: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_month_report_excel(object? parameter)
        {
            try
            {
                var report = build_cxc_report_dto();
                if (report == null) return;

                MonthlyReportExcelExporter.export(report);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al exportar Excel: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void execute_export_note_excel(object? parameter)
        {
            if (parameter is not accounts_receivable_row_dto row) return;
            try
            {
                var note = await _receivable_service.get_printable_note_async(row.id_delivery_note);
                NoteExcelExporter.export(note);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al exportar Excel: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}