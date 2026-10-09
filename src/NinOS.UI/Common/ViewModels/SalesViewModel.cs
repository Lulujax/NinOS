using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    public class SalesViewModel : ViewModelBase
    {
        private readonly IAccountsReceivableService _receivable_service;
        private readonly ISellerService? _seller_service;
        private readonly IZonaService? _zona_service;

        private string _search_query = string.Empty;
        private string _selected_month = string.Empty;
        private string _selected_zone = "Todas";
        private string _selected_filter = "Todas";
        private string _selected_report_type = "Todas"; // = TipoNotaTodas
        private string _selected_report_group = "Por Vendedor";
        private bool _report_include_goal = true;
        private bool _report_include_collections = true;
        private bool _report_include_voided_and_returned = false;
        private bool _all_report_sellers_selected = true;
        private bool _all_report_zones_selected = true;
        private bool _suppress_seller_sync;
        private bool _suppress_zone_sync;
        private decimal _total_sales_usd;
        private bool _is_loading;

        private decimal _month_total_usd;
        private decimal? _sales_goal_usd;
        private string _goal_edit_text = string.Empty;
        private bool _is_editing_goal;
        private double _goal_progress_percent;
        private decimal _goal_remaining_usd;

        private List<accounts_receivable_dto> _all_notes_source = new();
        private Dictionary<string, int> _seller_name_to_id = new(StringComparer.OrdinalIgnoreCase);

        public ObservableCollection<SellerTabItem<accounts_receivable_dto>> seller_tabs { get; } = new();
        private SellerTabItem<accounts_receivable_dto>? _selected_tab;

        public SellerTabItem<accounts_receivable_dto>? selected_tab
        {
            get => _selected_tab;
            set
            {
                if (_selected_tab == value) return;
                _selected_tab = value;
                on_property_changed();
                on_property_changed(nameof(selected_tab_index));
                on_property_changed(nameof(report_scope_text));
                if (!_is_loading)
                {
                    is_editing_goal = false;
                    update_goal_month_display();
                    apply_filters();
                    _ = load_goal_for_selected_month_async();
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

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<string> zone_options { get; } = new();
        public ObservableCollection<string> filter_options { get; }
        public ObservableCollection<string> report_type_options { get; }
        public ObservableCollection<string> report_group_options { get; } = new();
        public ObservableCollection<filter_selection_option> report_seller_options { get; } = new();
        public ObservableCollection<filter_selection_option> report_zone_options { get; } = new();
        public ObservableCollection<accounts_receivable_dto> all_notes { get; }
        public ObservableCollection<accounts_receivable_dto> sandra_notes { get; }
        public ObservableCollection<accounts_receivable_dto> anais_notes { get; }
        public ObservableCollection<accounts_receivable_dto> alejandra_notes { get; }
        public ObservableCollection<accounts_receivable_dto> juan_luis_notes { get; }

        public string selected_month
        {
            get => _selected_month;
            set
            {
                if (_selected_month == value) return;
                _selected_month = value ?? string.Empty;
                on_property_changed();
                on_property_changed(nameof(report_scope_text));
                if (_is_loading) return;
                update_goal_month_display();
                apply_filters();
                _ = load_goal_for_selected_month_async();
            }
        }

        public string selected_zone
        {
            get => string.IsNullOrWhiteSpace(_selected_zone) ? "Todas" : _selected_zone;
            set
            {
                if (_is_loading && string.IsNullOrWhiteSpace(value)) return;
                var normalized = string.IsNullOrWhiteSpace(value) ? "Todas" : value.Trim();
                if (_selected_zone == normalized) return;
                _selected_zone = normalized;
                on_property_changed();
                on_property_changed(nameof(report_scope_text));
                if (_is_loading) return;
                apply_filters();
            }
        }

        public string selected_filter
        {
            get => _selected_filter;
            set { if (_selected_filter == value) return; _selected_filter = value; on_property_changed(); apply_filters(); }
        }

        public string selected_report_type
        {
            get => _selected_report_type;
            set { if (_selected_report_type == value) return; _selected_report_type = value ?? "Todas"; on_property_changed(); } // "Todas" = TipoNotaTodas
        }

        public string selected_report_group
        {
            get => _selected_report_group;
            set { if (_selected_report_group == value) return; _selected_report_group = value ?? "Por Vendedor"; on_property_changed(); }
        }

        public bool report_include_goal
        {
            get => _report_include_goal;
            set { if (_report_include_goal == value) return; _report_include_goal = value; on_property_changed(); }
        }

        public bool report_include_collections
        {
            get => _report_include_collections;
            set { if (_report_include_collections == value) return; _report_include_collections = value; on_property_changed(); }
        }

        public bool report_include_voided_and_returned
        {
            get => _report_include_voided_and_returned;
            set { if (_report_include_voided_and_returned == value) return; _report_include_voided_and_returned = value; on_property_changed(); }
        }

        /// <summary>
        /// false (por defecto): el reporte usa el mes, la zona del combo y la pestaña de vendedor
        /// que ya están en pantalla, y las checklist quedan bloqueadas. true: el usuario arma el
        /// alcance a mano en el popup.
        /// </summary>
public bool report_manual_scope
{
    // Ya no tiene efecto: las checklists de vendedores y zonas son directamente el filtro del
    // reporte. Se deja la propiedad porque el binding del popup sigue existiendo, pero siempre
    // devuelve false para que ningun trigger lareative active.
    get => false;
    set { }
}

        /// <summary>Resumen del alcance heredado: lo que va a salir en el PDF.</summary>
        public string report_scope_text
        {
            get
            {
                string mes = string.IsNullOrEmpty(_selected_month) ? "sin mes seleccionado" : _selected_month;
                string zona = string.IsNullOrWhiteSpace(_selected_zone) || _selected_zone.Trim() == "Todas"
                    ? "todas las zonas"
                    : _selected_zone.Trim();
                string vendedor = selected_tab?.IdSeller == null
                    ? "todos los vendedores"
                    : (selected_tab.Header ?? "todos los vendedores");
                return $"Se usará: Mes {mes} · Zona {zona} · Vendedor {vendedor}";
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
                _suppress_seller_sync = true;
                foreach (var opt in report_seller_options) opt.is_checked = value;
                _suppress_seller_sync = false;
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
                _suppress_zone_sync = true;
                foreach (var opt in report_zone_options) opt.is_checked = value;
                _suppress_zone_sync = false;
            }
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

        public decimal month_total_usd
        {
            get => _month_total_usd;
            private set { _month_total_usd = value; on_property_changed(); }
        }

        public decimal? sales_goal_usd
        {
            get => _sales_goal_usd;
            private set { _sales_goal_usd = value; on_property_changed(); }
        }

        public string goal_edit_text
        {
            get => _goal_edit_text;
            set { _goal_edit_text = value ?? string.Empty; on_property_changed(); }
        }

        public bool is_editing_goal
        {
            get => _is_editing_goal;
            set { _is_editing_goal = value; on_property_changed(); }
        }

        public double goal_progress_percent
        {
            get => _goal_progress_percent;
            private set { _goal_progress_percent = value; on_property_changed(); }
        }

        public decimal goal_remaining_usd
        {
            get => _goal_remaining_usd;
            private set { _goal_remaining_usd = value; on_property_changed(); }
        }

        private string _goal_status_text = string.Empty;
        private string _goal_month_display = string.Empty;
        private string _goal_label_display = "META:";

        public string goal_status_text
        {
            get => _goal_status_text;
            private set { _goal_status_text = value ?? string.Empty; on_property_changed(); }
        }

        public string goal_month_display
        {
            get => _goal_month_display;
            private set { _goal_month_display = value ?? string.Empty; on_property_changed(); }
        }

        public string goal_label_display
        {
            get => _goal_label_display;
            private set { _goal_label_display = value ?? "META:"; on_property_changed(); }
        }

        private bool _is_month_selected;

        public bool is_month_selected
        {
            get => _is_month_selected;
            private set { _is_month_selected = value; on_property_changed(); }
        }

        public ICommand preview_note_command { get; }
        public ICommand print_pdf_command { get; }
        public ICommand sales_report_command { get; }
        public ICommand save_goal_command { get; }
        public ICommand edit_goal_command { get; }
        public ICommand cancel_goal_edit_command { get; }
        public ICommand clear_report_sellers_command { get; }
        public ICommand clear_report_zones_command { get; }
        public ICommand select_all_report_options_command { get; }
        public ICommand clear_all_report_options_command { get; }

        public Action<accounts_receivable_dto>? on_request_preview_window;

        public SalesViewModel(
            IAccountsReceivableService receivable_service,
            ISellerService? seller_service = null,
            IZonaService? zona_service = null)
        {
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));
            _seller_service = seller_service;
            _zona_service = zona_service;

            pending_months = new ObservableCollection<string>();
            filter_options = new ObservableCollection<string>();
            all_notes = new ObservableCollection<accounts_receivable_dto>();
            sandra_notes = new ObservableCollection<accounts_receivable_dto>();
            anais_notes = new ObservableCollection<accounts_receivable_dto>();
            alejandra_notes = new ObservableCollection<accounts_receivable_dto>();
            juan_luis_notes = new ObservableCollection<accounts_receivable_dto>();

            filter_options.Add("Todas");
            filter_options.Add("Por Cobrar");
            filter_options.Add("Pagadas");
            filter_options.Add("Anuladas");

            zone_options.Add("Todas");
            _selected_zone = "Todas";

            report_type_options = new ObservableCollection<string>();
            report_type_options.Add(TipoNotaTodas);
            report_type_options.Add(TipoNotaGeneral);
            report_type_options.Add(TipoNotaPromocion);
            report_type_options.Add(TipoNotaProVenta);
            report_type_options.Add(TipoNotaPromoProVenta);

            report_group_options.Add("Por Vendedor");
            report_group_options.Add("Por Zona");

            preview_note_command = new RelayCommand(execute_preview_note);
            print_pdf_command = new RelayCommand(execute_print_pdf);

            sales_report_command = new RelayCommand(execute_sales_report);
            save_goal_command = new RelayCommand(execute_save_goal);
            edit_goal_command = new RelayCommand(execute_edit_goal);
            cancel_goal_edit_command = new RelayCommand(execute_cancel_goal_edit);
            clear_report_sellers_command = new RelayCommand(execute_clear_report_sellers);
            clear_report_zones_command = new RelayCommand(execute_clear_report_zones);
            select_all_report_options_command = new RelayCommand(execute_select_all_report_options);
            clear_all_report_options_command = new RelayCommand(execute_clear_all_report_options);

            AppDataEvents.CatalogsChanged += () =>
            {
                System.Windows.Application.Current?.Dispatcher?.InvokeAsync(refresh_data);
            };

            load_all_async();
        }

        public void refresh_data() => load_all_async();

        public async Task<note_print_dto> get_printable_note_async(int id_delivery_note)
        {
            return await _receivable_service.get_printable_note_async(id_delivery_note);
        }

        private void sync_seller_tabs(IEnumerable<seller> sellers)
        {
            var activeSellers = sellers.Where(s => s.is_active).OrderBy(s => s.seller_code).ToList();

            if (seller_tabs.Count == 0 || seller_tabs[0].IdSeller != null)
            {
                seller_tabs.Insert(0, new SellerTabItem<accounts_receivable_dto>(null, "Todos"));
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
                    var newTab = new SellerTabItem<accounts_receivable_dto>(s.id_seller, s.full_name ?? string.Empty);
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

        // Seleccionar / Quitar vendedores y zonas de una sola vez. Al tocarlos el reporte
        // sale del modo heredado y pasa a alcance manual.
        private void execute_select_all_report_options(object? parameter)
        {
            report_manual_scope = true;
            all_report_sellers_selected = true;
            all_report_zones_selected = true;
        }

        private void execute_clear_all_report_options(object? parameter)
        {
            report_manual_scope = true;
            execute_clear_report_sellers(null);
            execute_clear_report_zones(null);
        }

        /// <summary>
        /// Deja las checklist del popup reflejando lo que está en pantalla (mes, zona del combo y
        /// pestaña de vendedor), para que al abrir el reporte se vea qué va a salir.
        /// </summary>
        public void sync_report_scope_from_screen()
        {
            string tab_name = (selected_tab?.Header ?? string.Empty).Trim();
            bool all_sellers = selected_tab?.IdSeller == null || tab_name.Length == 0;

            _suppress_seller_sync = true;
            foreach (var opt in report_seller_options)
                opt.is_checked = all_sellers || string.Equals(opt.name.Trim(), tab_name, StringComparison.OrdinalIgnoreCase);
            _suppress_seller_sync = false;
            _all_report_sellers_selected = report_seller_options.Count > 0 && report_seller_options.All(o => o.is_checked);
            on_property_changed(nameof(all_report_sellers_selected));

            bool all_zones = string.IsNullOrWhiteSpace(_selected_zone) || _selected_zone.Trim() == "Todas";
            string zone_name = _selected_zone.Trim();

            _suppress_zone_sync = true;
            foreach (var opt in report_zone_options)
                opt.is_checked = all_zones || string.Equals(opt.name.Trim(), zone_name, StringComparison.OrdinalIgnoreCase);
            _suppress_zone_sync = false;
            _all_report_zones_selected = report_zone_options.Count > 0 && report_zone_options.All(o => o.is_checked);
            on_property_changed(nameof(all_report_zones_selected));
        }

        private async void load_all_async()
        {
            if (_is_loading) return;
            try
            {
                var previous_selection = _selected_month;
                _is_loading = true;

                var dbSellers = _seller_service != null
                    ? await _seller_service.GetAllActiveAsync()
                    : await _receivable_service.get_sellers_async();
                sync_seller_tabs(dbSellers);

                var dbZones = _zona_service != null
                    ? await _zona_service.GetActiveAsync()
                    : new List<zona>();

                var raw = await _receivable_service.get_all_sales_notes_async();
                var all_rows = raw.ToList();

                // Mapa nombre -> id del vendedor. Además de los activos se completa con los que
                // aparezcan en las notas: un vendedor desactivado sigue teniendo documentos y hay
                // que poder resolver su meta para el reporte.
                _seller_name_to_id = dbSellers
                    .GroupBy(s => (s.full_name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Key.Length > 0)
                    .ToDictionary(g => g.Key, g => g.First().id_seller, StringComparer.OrdinalIgnoreCase);

                foreach (var row in all_rows)
                {
                    string row_seller = (row.seller_name ?? string.Empty).Trim();
                    if (row_seller.Length > 0 && row.id_seller > 0 && !_seller_name_to_id.ContainsKey(row_seller))
                        _seller_name_to_id[row_seller] = row.id_seller;
                }

                var unique_months = all_rows
                    .Where(n => n.creation_date.Year >= 2000)
                    .Select(n => new DateTime(n.creation_date.Year, n.creation_date.Month, 1))
                    .Distinct()
                    .OrderBy(d => d)
                    .Select(d => d.ToString("MMMM yyyy", new CultureInfo("es-VE")))
                    .ToList();

                var new_months = new List<string> { "" };
                new_months.AddRange(unique_months);

                string desired = previous_selection ?? string.Empty;
                if (!string.IsNullOrEmpty(desired) && !new_months.Contains(desired))
                {
                    var current_month_str = DateTime.Now.ToString("MMMM yyyy", new CultureInfo("es-VE"));
                    desired = new_months.Contains(current_month_str)
                        ? current_month_str
                        : (unique_months.Count > 0 ? unique_months[unique_months.Count - 1] : string.Empty);
                }

                for (int i = pending_months.Count - 1; i >= 0; i--)
                {
                    if (!new_months.Contains(pending_months[i]))
                        pending_months.RemoveAt(i);
                }
                for (int i = 0; i < new_months.Count; i++)
                {
                    var item = new_months[i];
                    int currentIndex = pending_months.IndexOf(item);
                    if (currentIndex < 0)
                        pending_months.Insert(i, item);
                    else if (currentIndex != i)
                        pending_months.Move(currentIndex, i);
                }

                // Poblar opciones de zona para filtro en pantalla
                var prev_zone = string.IsNullOrWhiteSpace(_selected_zone) ? "Todas" : _selected_zone;
                var new_zones = new List<string> { "Todas" };
                new_zones.AddRange(dbZones.Select(z => z.name));
                if (all_rows.Any(n => string.IsNullOrWhiteSpace(n.zone_name) || n.zone_name == "Sin zona"))
                {
                    if (!new_zones.Contains("Sin zona")) new_zones.Add("Sin zona");
                }

                for (int i = zone_options.Count - 1; i >= 0; i--)
                {
                    if (!new_zones.Contains(zone_options[i]))
                        zone_options.RemoveAt(i);
                }
                for (int i = 0; i < new_zones.Count; i++)
                {
                    var item = new_zones[i];
                    int currentIndex = zone_options.IndexOf(item);
                    if (currentIndex < 0)
                        zone_options.Insert(i, item);
                    else if (currentIndex != i)
                        zone_options.Move(currentIndex, i);
                }

                _selected_zone = zone_options.Contains(prev_zone) ? prev_zone : "Todas";
                on_property_changed(nameof(selected_zone));

                // Poblar opciones para el reporte de vendedores: los activos primero y, además,
                // cualquiera que tenga notas en el mes aunque esté desactivado. Si no, sus notas
                // quedarían fuera del reporte sin avisar.
                var active_seller_names = dbSellers
                    .Select(s => (s.full_name ?? string.Empty).Trim())
                    .Where(n => n.Length > 0)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var report_seller_rows = dbSellers
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

                // Poblar opciones para el reporte de zonas: las activas y, además, las que tengan
                // notas en el mes aunque ya no estén activas.
                var active_zone_names = dbZones
                    .Select(z => z.name.Trim())
                    .Where(n => n.Length > 0)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var report_zone_rows = dbZones
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

                // Las checklist arrancan reflejando la pantalla (pestana de vendedor y zona del combo).
                sync_report_scope_from_screen();

                _all_notes_source = all_rows;

                if (_selected_month != desired)
                {
                    _selected_month = desired;
                    on_property_changed(nameof(selected_month));
                }

                _is_loading = false;
                update_goal_month_display();
                apply_filters();
                _ = load_goal_for_selected_month_async();
            }
            catch (Exception ex)
            {
                _is_loading = false;
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private void apply_filters()
        {
            var query = _search_query?.Trim().ToLower() ?? string.Empty;
            var filtered = filter_by_month_and_search(_all_notes_source, _selected_month, query);

            if (!string.IsNullOrEmpty(_selected_zone) && _selected_zone != "Todas")
            {
                filtered = filtered.Where(n =>
                {
                    var noteZone = string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name.Trim();
                    return string.Equals(noteZone, _selected_zone.Trim(), StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            filtered = filtered.OrderByCorrelative(n => n.note_number).ToList();

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

        private List<accounts_receivable_dto> filter_by_month_and_search(List<accounts_receivable_dto> source, string selected_month, string query)
        {
            if (string.IsNullOrEmpty(selected_month))
                return new List<accounts_receivable_dto>();

            var result = source.AsEnumerable().Where(n =>
                new DateTime(n.creation_date.Year, n.creation_date.Month, 1)
                    .ToString("MMMM yyyy", new CultureInfo("es-VE")) == selected_month);

            if (!string.IsNullOrEmpty(query))
            {
                result = result.Where(n =>
                    SearchText.combine(
                        n.note_number,
                        n.customer_name,
                        n.seller_name,
                        n.zone_name,
                        n.status,
                        n.note_type_name,
                        n.sales_observations,
                        n.payment_method_text,
                        n.bank_name_text,
                        SearchText.date(n.creation_date),
                        SearchText.date(n.dispatch_date),
                        SearchText.date(n.last_payment_date),
                        SearchText.money(n.total_amount_usd),
                        SearchText.money(n.gross_total_usd),
                        SearchText.money(n.discount_amount),
                        SearchText.money(n.paid_amount_usd),
                        SearchText.money(n.balance_due_usd),
                        SearchText.pct(n.discount_percentage),
                        SearchText.pct(n.volume_discount_percentage)
                    ).Contains(query));
            }

            return result.ToList();
        }

        // Etiquetas del combo TIPO DE NOTA. Cada una cubre una familia de códigos del sistema:
        // GEN = General, PRM = Promocion, MAR = Pro Venta, PVP = Promocion Pro Venta.
        private const string TipoNotaTodas = "Todas";
        private const string TipoNotaGeneral = "General";
        private const string TipoNotaPromocion = "Promocion";
        private const string TipoNotaProVenta = "Pro Venta";
        private const string TipoNotaPromoProVenta = "Promocion Pro Venta";

        // Familias de nota del negocio. "Pro Venta" abarca MAR y PVP porque las zonas Pro Venta
        // trabajan con las dos; "Promocion Pro Venta" deja ver solo las PVP. En pantalla van con
        // el nombre del tipo, sin códigos: el cliente no tiene por qué ver MAR/PVP. Las notas sin
        // tipo se tratan como General: son legacy y siempre lo fueron.
        private static bool matches_note_type(accounts_receivable_dto note, string filter)
        {
            string code = note.note_type_code.Trim().ToUpperInvariant();

            if (string.Equals(filter, TipoNotaGeneral, StringComparison.OrdinalIgnoreCase))
                return code.Length == 0 || code == "GEN";
            if (string.Equals(filter, TipoNotaPromocion, StringComparison.OrdinalIgnoreCase))
                return code == "PRM";
            if (string.Equals(filter, TipoNotaProVenta, StringComparison.OrdinalIgnoreCase))
                return code == "MAR" || code == "PVP";
            if (string.Equals(filter, TipoNotaPromoProVenta, StringComparison.OrdinalIgnoreCase))
                return code == "PVP";
            return true; // "Todas" (y "Ambas", por compatibilidad con el nombre viejo)
        }

        // Sufijo del título del PDF para cada tipo de nota.
        private static string note_type_suffix(string filter)
        {
            if (string.Equals(filter, TipoNotaGeneral, StringComparison.OrdinalIgnoreCase)) return "GENERAL";
            if (string.Equals(filter, TipoNotaPromocion, StringComparison.OrdinalIgnoreCase)) return "PROMOCION";
            if (string.Equals(filter, TipoNotaProVenta, StringComparison.OrdinalIgnoreCase)) return "PRO VENTA";
            if (string.Equals(filter, TipoNotaPromoProVenta, StringComparison.OrdinalIgnoreCase)) return "PROMOCION PRO VENTA";
            return string.Empty;
        }

        private int? get_selected_seller_id()
        {
            return selected_tab?.IdSeller;
        }

        private string get_selected_tab_name()
        {
            return selected_tab?.IdSeller == null ? "General" : (selected_tab.Header ?? "General");
        }

        private void recalc_totals()
        {
            var list = (selected_tab?.Items ?? all_notes).ToList();

            total_sales_usd = list
                .Where(n => !string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)
                         && (string.Equals(n.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(n.status?.Trim(), "Pagada", StringComparison.OrdinalIgnoreCase)))
                .Sum(n => n.total_amount_usd);

            var month_rows = filter_by_month_and_search(_all_notes_source, _selected_month, string.Empty);
            if (!string.IsNullOrEmpty(_selected_zone) && _selected_zone != "Todas")
            {
                month_rows = month_rows.Where(n =>
                {
                    var noteZone = string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name.Trim();
                    return string.Equals(noteZone, _selected_zone.Trim(), StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }
            var eligible_notes = month_rows
                .Where(n => !string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)
                         && (string.Equals(n.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(n.status?.Trim(), "Pagada", StringComparison.OrdinalIgnoreCase)));

            string current_seller = get_selected_tab_name();
            if (current_seller != "General")
            {
                month_total_usd = eligible_notes
                    .Where(n => string.Equals(n.seller_name, current_seller, StringComparison.OrdinalIgnoreCase))
                    .Sum(n => n.total_amount_usd);
            }
            else
            {
                month_total_usd = eligible_notes.Sum(n => n.total_amount_usd);
            }

            recalc_goal_progress();
        }

        private void recalc_goal_progress()
        {
            if (_sales_goal_usd == null || _sales_goal_usd <= 0)
            {
                goal_progress_percent = 0;
                goal_remaining_usd = 0;
                goal_status_text = string.Empty;
                return;
            }

            decimal progress = _month_total_usd / _sales_goal_usd.Value * 100m;
            goal_progress_percent = Math.Min(100d, (double)progress);
            goal_remaining_usd = Math.Max(0m, _sales_goal_usd.Value - _month_total_usd);
            goal_status_text = goal_remaining_usd > 0
                ? $"Falta {goal_remaining_usd:N2} $ para la meta"
                : "¡Meta alcanzada!";
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
                    AppDialog.Show($"Error al generar el PDF: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public async Task save_observations_async(accounts_receivable_dto note, string text)
        {
            try
            {
                await _receivable_service.update_note_sales_observations_async(note.id_delivery_note, text);
                note.sales_observations = text;
            }
            catch (Exception ex)
            {
                note.sales_observations = note.saved_observations ?? string.Empty;
                AppDialog.Show($"No se pudo guardar la observación: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void execute_sales_report(object? parameter)
        {
            try
            {
                if (string.IsNullOrEmpty(_selected_month))
                {
                    AppDialog.Show("Seleccione un mes para generar el reporte.", "Reporte de ventas", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                // --- Alcance del reporte -------------------------------------------------
                // Las checklists son el filtro. Con "Todos/Todas" marcado no se filtra, para
                // que también entren notas de vendedores o zonas que no aparezcan en la lista.
                var seller_scope = report_seller_options.Where(o => o.is_checked).Select(o => o.name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var zone_scope = report_zone_options.Where(o => o.is_checked).Select(o => o.name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                bool filter_sellers = !_all_report_sellers_selected;
                bool filter_zones = !_all_report_zones_selected;

                if (filter_sellers && seller_scope.Count == 0)
                {
                    AppDialog.Show("Seleccione al menos un vendedor para incluir en el reporte.", "Reporte de ventas", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                if (filter_zones && zone_scope.Count == 0)
                {
                    AppDialog.Show("Seleccione al menos una zona para incluir en el reporte.", "Reporte de ventas", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                var month_rows = filter_by_month_and_search(_all_notes_source, _selected_month, string.Empty).ToList();

                // Filtro de vendedores
                if (filter_sellers)
                    month_rows = month_rows.Where(n => seller_scope.Contains((n.seller_name ?? string.Empty).Trim())).ToList();

                // Filtro de zonas
                if (filter_zones)
                    month_rows = month_rows.Where(n => zone_scope.Contains(string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name.Trim())).ToList();

                // Filtro de tipo de nota
                string tipo_nota = string.IsNullOrWhiteSpace(_selected_report_type) ? TipoNotaTodas : _selected_report_type;
                if (!string.Equals(tipo_nota, TipoNotaTodas, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(tipo_nota, "Ambas", StringComparison.OrdinalIgnoreCase))
                {
                    month_rows = month_rows.Where(n => matches_note_type(n, tipo_nota)).ToList();
                }

                // Filtro de estado de nota
                if (!_report_include_voided_and_returned)
                {
                    month_rows = month_rows.Where(n => n.status == "Pendiente" || n.status == "Pagada").ToList();
                }

                string type_suffix = note_type_suffix(tipo_nota);
                string seller_suffix = !filter_sellers
                    ? string.Empty
                    : (seller_scope.Count == 1 ? seller_scope.First().ToUpperInvariant() : "VARIOS VENDEDORES");
                string zone_suffix = !filter_zones
                    ? string.Empty
                    : (zone_scope.Count == 1 ? zone_scope.First().ToUpperInvariant() : "VARIAS ZONAS");
                string combined = string.Join(" - ", new[] { type_suffix, seller_suffix, zone_suffix }.Where(s => !string.IsNullOrEmpty(s)));

                DateTime? monthStart = parse_selected_month();
                decimal cum_venta = month_rows
                    .Where(n => !string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)
                             && (string.Equals(n.status?.Trim(), "Pendiente", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(n.status?.Trim(), "Pagada", StringComparison.OrdinalIgnoreCase)))
                    .Sum(n => n.total_amount_usd);

                string group_mode = string.IsNullOrWhiteSpace(_selected_report_group) ? "Por Vendedor" : _selected_report_group;

                // --- Meta de ventas -------------------------------------------------------
                // La meta es por vendedor o del mes completo, nunca por zona. Solo se imprime
                // cuando el alcance del reporte coincide con lo que cubre la meta: si el PDF
                // filtra zonas o tipos de nota, la venta que queda no es la del mes completo y
                // el cumplimiento que saldría sería mentira.
                bool filtrado_por_tipo = !string.Equals(tipo_nota, TipoNotaTodas, StringComparison.OrdinalIgnoreCase)
                                      && !string.Equals(tipo_nota, "Ambas", StringComparison.OrdinalIgnoreCase);
                decimal? goal_usd = null;
                var goal_by_group = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
                bool show_goal = _report_include_goal && monthStart.HasValue && !filter_zones && !filtrado_por_tipo;
                string goal_scope_label = "META DEL MES";

                if (show_goal && monthStart.HasValue)
                {
                    DateTime month_start_date = monthStart.Value;

                    if (string.Equals(group_mode, "Por Zona", StringComparison.OrdinalIgnoreCase))
                    {
                        show_goal = false; // las metas no son por zona
                    }
                    else if (string.Equals(group_mode, "Por Vendedor", StringComparison.OrdinalIgnoreCase))
                    {
                        goal_scope_label = "META DEL VENDEDOR";

                        // Una meta por página: la del vendedor de esa página, contra su propia venta.
                        foreach (var seller_name in month_rows
                            .Select(n => (n.seller_name ?? string.Empty).Trim())
                            .Where(s => s.Length > 0)
                            .Distinct(StringComparer.OrdinalIgnoreCase))
                        {
                            int? seller_id = _seller_name_to_id.TryGetValue(seller_name, out var mapped_id) ? (int?)mapped_id : null;
                            goal_by_group[seller_name] = await _receivable_service.get_sales_goal_async(month_start_date, seller_id);
                        }

                        if (goal_by_group.Count == 0) show_goal = false;
                    }
                    else
                    {
                        // Solo existen "Por Vendedor" y "Por Zona". Si algún día aparece otra
                        // agrupación, no se imprime meta: no hay una que le corresponda.
                        show_goal = false;
                    }
                }

                var report = new monthly_report_dto
                {
                    title = string.IsNullOrEmpty(combined)
                        ? "VENTAS - DETALLE DEL MES"
                        : $"VENTAS - DETALLE DEL MES ({combined})",
                    month = _selected_month,
                    report_name = "ventas",
                    detail_column_header = "ABONADO",
                    show_paid_balance_summary = _report_include_collections,
                    empty_text = "Sin ventas para los filtros seleccionados.",
                    group_mode = group_mode,
                    rows = month_rows
                        .OrderByCorrelative(n => n.note_number)
                        .Select(n => new monthly_report_row_dto
                        {
                            date = n.creation_date,
                            document_number = n.note_number,
                            customer_name = n.customer_name,
                            seller_name = n.seller_name,
                            zone_name = string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name,
                            amount_usd = n.total_amount_usd,
                            paid_amount_usd = n.paid_amount_usd,
                            balance_due_usd = n.balance_due_usd,
                            detail_text = $"{n.paid_amount_usd:N2}",
                            status = n.status
                        })
                        .ToList(),
                    sales_goal_usd = goal_usd,
                    month_total_usd = cum_venta,
                    goal_by_group = goal_by_group,
                    goal_scope_label = goal_scope_label,
                    show_goal_block = show_goal
                };

                MonthlyReportPdfGenerator.generate(report);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al generar el reporte: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_edit_goal(object? parameter)
        {
            goal_edit_text = _sales_goal_usd?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
            is_editing_goal = true;
        }

        private void execute_cancel_goal_edit(object? parameter)
        {
            is_editing_goal = false;
        }

        private async void execute_save_goal(object? parameter)
        {
            try
            {
                DateTime? month = parse_selected_month();
                if (month == null)
                {
                    AppDialog.Show("Seleccione un mes para definir la meta.", "Meta de venta", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                string normalized = string.IsNullOrWhiteSpace(goal_edit_text) ? "0" : goal_edit_text.Replace(",", ".");
                if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount < 0)
                {
                    AppDialog.Show("Ingrese un monto válido para la meta.", "Meta de venta", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                int? seller_id = get_selected_seller_id();
                await _receivable_service.set_sales_goal_async(month.Value, seller_id, amount);
                await load_goal_for_selected_month_async();

                is_editing_goal = false;
                recalc_totals();
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al guardar la meta: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private DateTime? parse_selected_month()
        {
            if (string.IsNullOrEmpty(_selected_month)) return null;
            DateTime parsed;
            if (DateTime.TryParseExact(_selected_month, "MMMM yyyy", new CultureInfo("es-VE"), DateTimeStyles.None, out parsed))
                return parsed;
            if (DateTime.TryParseExact(_selected_month, "MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed;
            return null;
        }

        private void update_goal_month_display()
        {
            DateTime? month = parse_selected_month();
            string month_label = month?.ToString("MMMM yyyy", new CultureInfo("es-VE")) ?? string.Empty;
            goal_month_display = month_label;
            string tab_name = get_selected_tab_name().ToUpperInvariant();
            if (string.IsNullOrEmpty(month_label))
            {
                goal_label_display = $"META {tab_name}:";
            }
            else
            {
                goal_label_display = $"META {tab_name} ({month_label}):";
            }
            is_month_selected = month != null;
        }

        private async Task load_goal_for_selected_month_async()
        {
            DateTime? month = parse_selected_month();
            if (month == null)
            {
                sales_goal_usd = null;
                recalc_goal_progress();
                return;
            }
            int? seller_id = get_selected_seller_id();
            sales_goal_usd = await _receivable_service.get_sales_goal_async(month.Value, seller_id);
            recalc_goal_progress();
        }
    }
}