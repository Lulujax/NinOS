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



using NinOS.Infrastructure.Services.Implementations;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;



using NinOS.UI.Common;







namespace NinOS.UI.Common.ViewModels



{



    public class CreditNotesViewModel : ViewModelBase



    {



        private ICreditNoteService _credit_note_service;



        private IAccountsReceivableService _receivable_service;
        private readonly IServiceScopeFactory _scope_factory;
        private business_unit _selected_unit = business_unit.General;



        private readonly IInventoryService _inventory_service;



        private readonly ICustomerService _customer_service;



        private readonly ISellerService _seller_service;



        private readonly IZonaService _zona_service;







        private List<seller> _cached_sellers = new();



        private List<zona> _cached_zonas = new();







        private rango_fechas _rango_filtro = rango_fechas.mes_actual(AppTimeZone.to_local(DateTime.UtcNow).Date);



        private string _selected_category_filter = "Todas";



        private List<credit_note_dto> _all_credit_rows = new();



        private bool _is_loading;



        private decimal _total_credit_usd;











        public ObservableCollection<string> category_filters { get; } = new() { "Todas", "Devolución", "Obsequio" };



        public ObservableCollection<credit_note_dto> notes { get; }








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







        public ObservableCollection<SellerTabItem<credit_note_dto>> seller_tabs { get; } = new();



        private SellerTabItem<credit_note_dto>? _selected_tab;







        public SellerTabItem<credit_note_dto>? selected_tab



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







        public rango_fechas rango_filtro
        {
            get => _rango_filtro;
            set
            {
                if (value == null || _rango_filtro == value) return;
                _rango_filtro = value;
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



        public ICreditNoteService credit_note_service => _credit_note_service;

        public ObservableCollection<BusinessUnitOption> unit_options { get; }

        public business_unit selected_unit
        {
            get => _selected_unit;
            set
            {
                if (_selected_unit == value) return;
                _selected_unit = value;
                _credit_note_service = new CreditNoteService(_scope_factory, value);
                _receivable_service = new AccountsReceivableService(_scope_factory, value);
                AppLog.Info($"Notas de credito: cambiando a la empresa {BusinessUnitRules.label_of(value)}.");
                on_property_changed();
                refresh_data();
            }
        }

        public Action? OnCreditNoteSaved { get; set; }







        public CreditNotesViewModel(



            IServiceScopeFactory scope_factory,






            IInventoryService inventory_service,



            ICustomerService customer_service,



            ISellerService seller_service,



            IZonaService zona_service)



        {



            _scope_factory = scope_factory ?? throw new ArgumentNullException(nameof(scope_factory));
            _credit_note_service = new CreditNoteService(scope_factory, business_unit.General);



            _receivable_service = new AccountsReceivableService(scope_factory, business_unit.General);
            unit_options = new ObservableCollection<BusinessUnitOption>(
                new[] { business_unit.General, business_unit.ProVenta, business_unit.Vesupronta }.Select(unit => new BusinessUnitOption(unit)));



            _inventory_service = inventory_service ?? throw new ArgumentNullException(nameof(inventory_service));



            _customer_service = customer_service ?? throw new ArgumentNullException(nameof(customer_service));



            _seller_service = seller_service ?? throw new ArgumentNullException(nameof(seller_service));



            _zona_service = zona_service ?? throw new ArgumentNullException(nameof(zona_service));











            notes = new ObservableCollection<credit_note_dto>();







            new_credit_note_command = new RelayCommand(execute_new_credit_note);







            AppDataEvents.CatalogsChanged += () =>



            {



                System.Windows.Application.Current?.Dispatcher?.InvokeAsync(refresh_data);



            };







            load_months_async();



        }







        public void refresh_data() => load_months_async();







        private void sync_tabs()



        {



            bool isGiftMode = string.Equals(_selected_category_filter, "Obsequio", StringComparison.OrdinalIgnoreCase);



            if (isGiftMode)



            {



                sync_zona_tabs();



            }



            else



            {



                sync_seller_tabs();



            }



        }







        private void sync_zona_tabs()



        {



            if (seller_tabs.Count == 0 || seller_tabs[0].IdZona != null || seller_tabs[0].Header != "Todas")



            {



                seller_tabs.Clear();



                seller_tabs.Add(new SellerTabItem<credit_note_dto>(null, null, "Todas"));



            }







            var currentZonaTabs = seller_tabs.Skip(1).ToList();



            foreach (var tab in currentZonaTabs)



            {



                if (!tab.IdZona.HasValue || !_cached_zonas.Any(z => z.id_zona == tab.IdZona.Value))



                {



                    seller_tabs.Remove(tab);



                }



            }







            int targetIndex = 1;



            foreach (var z in _cached_zonas)



            {



                var existing = seller_tabs.FirstOrDefault(t => t.IdZona == z.id_zona);



                if (existing == null)



                {



                    var newTab = new SellerTabItem<credit_note_dto>(null, z.id_zona, z.name ?? string.Empty);



                    seller_tabs.Insert(targetIndex, newTab);



                }



                else



                {



                    existing.Header = z.name ?? string.Empty;



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







        private void sync_seller_tabs()



        {



            if (seller_tabs.Count == 0 || seller_tabs[0].IdSeller != null || seller_tabs[0].Header != "Todos")



            {



                seller_tabs.Clear();



                seller_tabs.Add(new SellerTabItem<credit_note_dto>(null, null, "Todos"));



            }







            var currentSellerTabs = seller_tabs.Skip(1).ToList();



            foreach (var tab in currentSellerTabs)



            {



                if (!tab.IdSeller.HasValue || !_cached_sellers.Any(s => s.id_seller == tab.IdSeller.Value))



                {



                    seller_tabs.Remove(tab);



                }



            }







            int targetIndex = 1;



            foreach (var s in _cached_sellers)



            {



                var existing = seller_tabs.FirstOrDefault(t => t.IdSeller == s.id_seller);



                if (existing == null)



                {



                    var newTab = new SellerTabItem<credit_note_dto>(s.id_seller, null, s.full_name ?? string.Empty);



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







        private async void load_months_async()



        {



            try



            {







                _is_loading = true;







                _cached_sellers = (await _seller_service.GetAllActiveAsync()).Where(s => !s.is_pro_venta_only).OrderBy(s => s.seller_code).ToList();



                _cached_zonas = (await _zona_service.GetActiveAsync()).OrderBy(z => z.name).ToList();



                sync_tabs();







                _all_credit_rows = (await _credit_note_service.get_all_credit_notes_async()).ToList();















                _is_loading = false;



                apply_filters();



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



            sync_tabs();







            notes.Clear();



            total_credit_usd = 0;







        

        IEnumerable<credit_note_dto> filtered = _all_credit_rows.Where(r => _rango_filtro.contiene(r.creation_date));
        if (!string.IsNullOrEmpty(_selected_category_filter) && !_selected_category_filter.Equals("Todas", StringComparison.OrdinalIgnoreCase))



            {



                string target = _selected_category_filter.Replace("ó", "o");



                filtered = filtered.Where(r => string.Equals(r.category?.Replace("ó", "o"), target, StringComparison.OrdinalIgnoreCase));



            }







            var rows = filtered



                .OrderByCorrelative(r => r.note_number)



                .ToList();







            foreach (var row in rows) notes.Add(row);










            bool isGiftMode = string.Equals(_selected_category_filter, "Obsequio", StringComparison.OrdinalIgnoreCase);







            if (isGiftMode)



            {



                foreach (var tab in seller_tabs)



                {



                    tab.Items.Clear();



                    if (tab.IdZona == null)



                    {



                        foreach (var row in rows) tab.Items.Add(row);



                    }



                    else



                    {



                        foreach (var row in rows.Where(r => (r.id_zona.HasValue && r.id_zona.Value == tab.IdZona.Value)



                                                         || string.Equals(r.zone_name?.Trim(), tab.Header?.Trim(), StringComparison.OrdinalIgnoreCase)))



                        {



                            tab.Items.Add(row);



                        }



                    }



                }



            }



            else



            {



                foreach (var tab in seller_tabs)



                {



                    tab.Items.Clear();



                    if (tab.IdSeller == null)



                    {



                        foreach (var row in rows) tab.Items.Add(row);



                    }



                    else



                    {



                        foreach (var row in rows.Where(r => (tab.IdSeller.HasValue && r.id_seller == tab.IdSeller.Value)



                                                         || string.Equals(r.seller_name?.Trim(), tab.Header?.Trim(), StringComparison.OrdinalIgnoreCase)))



                        {



                            tab.Items.Add(row);



                        }



                    }



                }



            }







            recalc_totals();



        }







        private void recalc_totals()



        {



            var list = (selected_tab?.Items ?? notes)



                .Where(r => !string.Equals(r.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase))



                .ToList();



            total_credit_usd = list.Sum(r => r.total_amount_usd);



        }







        private void execute_new_credit_note(object? parameter) => on_request_new_credit_note_window?.Invoke();







        public async Task<credit_note_source_dto?> get_credit_source_async(string note_number)



            => await _credit_note_service.get_credit_source_by_note_number_async(note_number);







        public async Task<IEnumerable<accounts_receivable_dto>> get_delivery_notes_for_credit_async(int id_seller, rango_fechas? rango = null)



            => await _credit_note_service.get_delivery_notes_for_credit_async(id_seller, rango);







        public async Task<IEnumerable<accounts_receivable_dto>> get_notes_by_month_async(string month_year)



            => await _receivable_service.get_all_by_month_async(month_year);
















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







        public async Task<(note_print_dto? original, note_print_dto credit)> get_draft_print_pair_async(credit_note draft, IEnumerable<note_detail_print_dto> details)
        {
            note_print_dto? original = null;

            if (!string.Equals(draft.category, "Obsequio", StringComparison.OrdinalIgnoreCase) && draft.id_delivery_note is int id_delivery_note && id_delivery_note > 0)
                original = await _receivable_service.get_printable_note_async(id_delivery_note);

            note_print_dto credit = await _credit_note_service.get_draft_print_credit_note_async(draft, details);

            return (original, credit);
        }

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







        /// <summary>



        /// Anula una nota de credito (devolucion o obsequio). El servicio revierte el stock y,



        /// si era devolucion, restaura el monto de la nota de entrega.



        /// </summary>



        public async Task annul_credit_note_async(int id_credit_note)



        {



            await _credit_note_service.annul_credit_note_async(id_credit_note);



            await load_notes_async();



            OnCreditNoteSaved?.Invoke();



        }







        public async Task<IEnumerable<customer>> get_customers_async()



            => await _customer_service.GetVisibleCustomersAsync();







        public async Task<IEnumerable<seller>> get_sellers_async()



            => (await _seller_service.GetAllActiveAsync()).Where(s => !s.is_pro_venta_only);







        public async Task<IEnumerable<zona>> get_assigned_zonas_for_seller_async(int id_seller)



            => await _seller_service.GetAssignedZonasAsync(id_seller);







        public async Task<List<zona>> get_zonas_async()



            => await _zona_service.GetActiveAsync();







        public async Task<seller?> get_admin_seller_async()



            => (await _seller_service.GetAllActiveAsync()).FirstOrDefault(s => SellerCodes.is_obsequio_admin_seller(s.seller_code, s.full_name));



    }



}