using System.Globalization;
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

using NinOS.UI.Views;



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



        // Zona del cliente de la nota: se usa para agrupar el reporte de pagos por zona.

        public int? id_zona { get; set; }

        public string zone_name { get; set; } = string.Empty;



        public DateTime creation_date { get; set; }

        public string month_key { get; set; } = string.Empty;

    }



    public class PaymentsViewModel : ViewModelBase

    {

        private readonly IPaymentService _payment_service;

        private readonly IAccountsReceivableService _receivable_service;

        private readonly ISellerService? _seller_service;

        private readonly IZonaService? _zona_service;



        private SellerTabItem<payment_row_dto>? _selected_tab;

        private string _search_query = string.Empty;

        private string _selected_month = string.Empty;

        private string _selected_filter = "Pagadas";

        private decimal _total_invoiced_usd;

        private decimal _total_paid_usd;

        private decimal _total_balance_usd;

        private bool _is_loading;



        // --- Configuracion del reporte de pagos ---

        // Sin modo "alcance manual": las checklists siempre mandan. No hay checkbox de cambiar

        // alcance porque las listas de vendedores y zonas ya son el filtro, y un interruptor

        // encima solo confunde.

        private string _selected_report_group = "Por Vendedor";

        private bool _all_report_sellers_selected = true;

        private bool _all_report_zones_selected = true;

        private bool _suppress_seller_sync;

        private bool _suppress_zone_sync;

        private Dictionary<string, int> _seller_name_to_id = new(StringComparer.OrdinalIgnoreCase);



        private List<payment_row_dto> _all_notes_source = new();



        public ObservableCollection<SellerTabItem<payment_row_dto>> seller_tabs { get; } = new();



        public SellerTabItem<payment_row_dto>? selected_tab

        {

            get => _selected_tab;

            set

            {

                if (_selected_tab == value) return;

                _selected_tab = value;

                on_property_changed();

                on_property_changed(nameof(selected_tab_index));

                recalc_totals();

            }

        }



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

        public ObservableCollection<string> filter_options { get; }

        public ObservableCollection<payment_row_dto> all_notes { get; }

        public ObservableCollection<payment_row_dto> sandra_notes { get; }

        public ObservableCollection<payment_row_dto> anais_notes { get; }

        public ObservableCollection<payment_row_dto> alejandra_notes { get; }

        public ObservableCollection<payment_row_dto> juan_luis_notes { get; }



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



        /// <summary>

        /// Refleja la pestaña de vendedor activa en las checklists. Se llama al abrir el popup:

        /// la pantalla manda por defecto, y desmarcar vendedor o zona es cambiar el alcance.

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



            _suppress_zone_sync = true;

            foreach (var opt in report_zone_options) opt.is_checked = true;

            _suppress_zone_sync = false;

            _all_report_zones_selected = report_zone_options.Count > 0 && report_zone_options.All(o => o.is_checked);

            on_property_changed(nameof(all_report_zones_selected));

        }



        public string selected_month

        {

            get => _selected_month;

            set { if (_selected_month == value) return; _selected_month = value ?? string.Empty; on_property_changed(); if (!_is_loading) apply_filters(); }

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

        public ICommand month_report_preview_command { get; }

        public ICommand month_report_print_command { get; }

        public ICommand month_report_excel_command { get; }

        public ICommand clear_report_sellers_command { get; }

        public ICommand clear_report_zones_command { get; }

        public ICommand select_all_report_options_command { get; }

        public ICommand clear_all_report_options_command { get; }

        public Action? on_request_add_payment_window { get; set; }



        public PaymentsViewModel(

            IPaymentService payment_service,

            IAccountsReceivableService receivable_service,

            ISellerService? seller_service = null,

            IZonaService? zona_service = null)

        {

            _payment_service = payment_service ?? throw new ArgumentNullException(nameof(payment_service));

            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));

            _seller_service = seller_service;

            _zona_service = zona_service;



            // available_months already initialized

            filter_options = new ObservableCollection<string>();

            all_notes = new ObservableCollection<payment_row_dto>();

            sandra_notes = new ObservableCollection<payment_row_dto>();

            anais_notes = new ObservableCollection<payment_row_dto>();

            alejandra_notes = new ObservableCollection<payment_row_dto>();

            juan_luis_notes = new ObservableCollection<payment_row_dto>();



            filter_options.Add("Pagadas");

            filter_options.Add("Pendientes");

            filter_options.Add("Devueltas");

            filter_options.Add("Todas");



            report_group_options.Add("Por Vendedor");

            report_group_options.Add("Por Zona");



            add_payment_command = new RelayCommand(execute_add_payment);



            month_report_command = new RelayCommand(execute_month_report);

            month_report_preview_command = new RelayCommand(execute_month_report_preview);

            month_report_print_command = new RelayCommand(execute_month_report_print);

            month_report_excel_command = new RelayCommand(execute_month_report_excel);

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



        private void sync_seller_tabs(IEnumerable<seller> sellers)

        {

            var activeSellers = sellers.Where(s => s.is_active && !s.is_pro_venta_only).OrderBy(s => s.seller_code).ToList();



            if (seller_tabs.Count == 0 || seller_tabs[0].IdSeller != null)

            {

                seller_tabs.Insert(0, new SellerTabItem<payment_row_dto>(null, "Todos"));

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

                    var newTab = new SellerTabItem<payment_row_dto>(s.id_seller, s.full_name ?? string.Empty);

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

        /// Arma las checklists de vendedores y zonas del popup de reporte. Primero los activos y

        /// despues cualquiera que tenga notas en la base aunque este desactivado, para que sus

        /// notas no queden fuera del reporte sin avisar.

        /// </summary>

        private void sync_report_options(

            IEnumerable<seller> sellers,

            List<zona> zones,

            List<payment_row_dto> all_rows)

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



        private async void load_all_async()

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

                var all_rows = raw.Where(n => !string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)).Select(map_to_row).ToList();



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



                _is_loading = false;

                apply_filters();

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



            // Filtrar según la opción seleccionada (Pagadas, Pendientes, Devueltas, Todas).

            IEnumerable<payment_row_dto> source = _all_notes_source;

            if (_selected_filter == "Pagadas")

                source = source.Where(n => n.status == "Pagada");

            else if (_selected_filter == "Pendientes")

                source = source.Where(n => n.status == "Pendiente" && n.paid_amount_usd > 0);

            else if (_selected_filter == "Devueltas")

                source = source.Where(n => n.status == "Devuelta");

            else if (_selected_filter == "Todas")

                source = source.Where(n => n.status == "Pagada" || n.status == "Devuelta" || n.paid_amount_usd > 0);

            else

                source = source.Where(n => n.status == "Pagada" || n.paid_amount_usd > 0);



            // Sin mes seleccionado (estado vacio) no mostrar filas.

            var filtered = string.IsNullOrEmpty(_selected_month)

                ? new List<payment_row_dto>()

                : filter_by_month_and_search(source.ToList(), _selected_month, query)

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

                var tabItems = tab.IdSeller == null

                    ? filtered

                    : filtered.Where(n => (tab.IdSeller.HasValue && n.id_seller == tab.IdSeller.Value) || string.Equals(n.seller_name?.Trim(), tab.Header?.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

                update_collection(tab.Items, tabItems);

            }



            recalc_totals();

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



        private List<payment_row_dto> filter_by_month_and_search(List<payment_row_dto> source, string selected_month, string query)

        {

            if (string.IsNullOrEmpty(selected_month))

                return new List<payment_row_dto>();



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

                        n.payment_method_text,

                        n.bank_name_text,

                        n.discount_percentage_text,

                        n.month_key,

                        SearchText.date(n.creation_date),

                        SearchText.date(n.last_payment_date),

                        SearchText.money(n.total_amount_usd),

                        SearchText.money(n.gross_total_usd),

                        SearchText.money(n.discount_amount),

                        SearchText.money(n.paid_amount_usd),

                        SearchText.money(n.balance_due_usd)

                    ).Contains(query));

            }



            return result.ToList();

        }



        private void recalc_totals()

        {

            var list = (selected_tab != null ? selected_tab.Items.ToList() : all_notes.ToList())

                .Where(n => !string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))

                .ToList();



            // Ojo: aqui el abono de anulacion SI suma, a diferencia del reporte de pagos. Esta fila

            // es una nota, no un pago: su paid_amount_usd ya viene neto de todos los asientos.

            // Si la nota de credito se anulo, el abono de +X cancela el -X de la NC y dejarlo

            // fuera devolveria a mostrar el descuento de la NC como si nunca se hubiera devuelto.

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

                id_zona = n.id_zona,

                zone_name = string.IsNullOrWhiteSpace(n.zone_name) ? "Sin zona" : n.zone_name,

                creation_date = n.creation_date,

                month_key = new DateTime(

                    AppTimeZone.to_local(n.creation_date).Year,

                    AppTimeZone.to_local(n.creation_date).Month, 1)

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

                AppDialog.Show("Pago registrado exitosamente.", "Exito");

                _ = System.Windows.Application.Current?.Dispatcher?.InvokeAsync(load_all_async);

            }

            catch (Exception ex) { AppDialog.Show(ErrorText.Get(ex), "Error"); }

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

                _ = System.Windows.Application.Current?.Dispatcher?.InvokeAsync(load_all_async);

            }

            catch (Exception ex) { AppDialog.Show(ErrorText.Get(ex), "Error"); }

        }



        public async Task<IEnumerable<string>> get_all_months_async()

        {

            return await _receivable_service.get_all_months_async();

        }



        public async Task<IEnumerable<accounts_receivable_dto>> get_notes_by_month_async(string month_year)

        {

            return await _receivable_service.get_all_by_month_async(month_year);

        }



        private async Task<(string month, List<payment_dto> payments, string groupMode, HashSet<string>? zoneScope, bool filterZones, bool filterSellers)?> prepare_payments_report_data_async()

        {

            if (string.IsNullOrWhiteSpace(_selected_month))

            {

                AppDialog.Show("Seleccione un mes para generar el reporte.", "Reporte de pagos",

                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                return null;

            }



            var seller_scope = report_seller_options.Where(o => o.is_checked).Select(o => o.name)

                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var zone_scope = report_zone_options.Where(o => o.is_checked).Select(o => o.name)

                .ToHashSet(StringComparer.OrdinalIgnoreCase);



            bool filter_sellers = !_all_report_sellers_selected;

            bool filter_zones = !_all_report_zones_selected;



            if (filter_sellers && seller_scope.Count == 0)

            {

                AppDialog.Show("Seleccione al menos un vendedor para incluir en el reporte.", "Reporte de pagos",

                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                return null;

            }



            if (filter_zones && zone_scope.Count == 0)

            {

                AppDialog.Show("Seleccione al menos una zona para incluir en el reporte.", "Reporte de pagos",

                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                return null;

            }



            List<payment_dto> payments;

            string period_label;

            if (_selected_month == "Todos")

            {

                payments = (await _payment_service.get_payments_by_period_async(_selected_year, null)).ToList();

                period_label = $"AÑO {_selected_year}";

            }

            else

            {

                int month_num = DateTime.ParseExact(_selected_month, "MMMM", new CultureInfo("es-VE")).Month;

                payments = (await _payment_service.get_payments_by_period_async(_selected_year, month_num)).ToList();

                period_label = $"{_selected_month} {_selected_year}".ToUpperInvariant();

            }



            if (filter_sellers)

                payments = payments.Where(p => seller_scope.Contains((p.seller_name ?? string.Empty).Trim())).ToList();



            if (filter_zones)

            {

                var note_ids = payments.Select(p => p.id_delivery_note).Where(id => id > 0).Distinct().ToList();

                var zone_por_nota = await _receivable_service.get_all_notes_async();



                var zonas = zone_por_nota

                    .Where(n => note_ids.Contains(n.id_delivery_note))

                    .GroupBy(n => n.id_delivery_note)

                    .ToDictionary(

                        g => g.Key,

                        g => string.IsNullOrWhiteSpace(g.First().zone_name) ? "Sin zona" : g.First().zone_name.Trim());



                payments = payments

                    .Where(p => zonas.TryGetValue(p.id_delivery_note, out var zona) && zone_scope.Contains(zona))

                    .ToList();

            }



            string group_mode = string.IsNullOrWhiteSpace(_selected_report_group) ? "Por Vendedor" : _selected_report_group;

            return (period_label, payments, group_mode, zone_scope, filter_zones, filter_sellers);

        }



        private async void execute_month_report(object? parameter)

        {

            try

            {

                var data = await prepare_payments_report_data_async();

                if (data == null) return;



                PaymentsReportPdfGenerator.generate(data.Value.month, data.Value.payments, data.Value.groupMode, data.Value.zoneScope, data.Value.filterZones, data.Value.filterSellers);

            }

            catch (Exception ex)

            {

                AppDialog.Show($"Error al generar el reporte: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            }

        }



        private async void execute_month_report_preview(object? parameter)

        {

            try

            {

                var data = await prepare_payments_report_data_async();

                if (data == null) return;



                var doc = PaymentsReportPdfGenerator.build_document(data.Value.month, data.Value.payments, data.Value.groupMode, data.Value.zoneScope, data.Value.filterZones, data.Value.filterSellers);

                var fileName = PaymentsReportPdfGenerator.get_default_file_name(data.Value.month);

                var preview = new PdfPreviewWindow(

                    doc,

                    $"Reporte de Pagos - {data.Value.month}",

                    fileName,

                    () => PaymentsReportExcelExporter.export(data.Value.month, data.Value.payments, data.Value.groupMode));

                preview.Owner = System.Windows.Application.Current?.MainWindow;

                preview.ShowDialog();

            }

            catch (Exception ex)

            {

                AppDialog.Show($"Error en vista previa: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            }

        }



        private async void execute_month_report_print(object? parameter)

        {

            try

            {

                var data = await prepare_payments_report_data_async();

                if (data == null) return;



                var doc = PaymentsReportPdfGenerator.build_document(data.Value.month, data.Value.payments, data.Value.groupMode, data.Value.zoneScope, data.Value.filterZones, data.Value.filterSellers);

                var fileName = PaymentsReportPdfGenerator.get_default_file_name(data.Value.month);

                ReportPrintService.DirectPrint(doc, System.Windows.Application.Current?.MainWindow, $"Reporte de Pagos - {data.Value.month}", fileName);

            }

            catch (Exception ex)

            {

                AppDialog.Show($"Error al imprimir: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            }

        }



        private async void execute_month_report_excel(object? parameter)

        {

            try

            {

                var data = await prepare_payments_report_data_async();

                if (data == null) return;



                PaymentsReportExcelExporter.export(data.Value.month, data.Value.payments, data.Value.groupMode);

            }

            catch (Exception ex)

            {

                AppDialog.Show($"Error al exportar Excel: {ErrorText.Get(ex)}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            }

        }

    }

}

