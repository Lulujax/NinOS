using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class inventory_item_dto : ViewModelBase
    {
        private int _row_number;
        public int RowNumber 
        { 
            get { return _row_number; }
            set { _row_number = value; on_property_changed(); }
        }
        public string id_display { get; set; } = string.Empty;
        public string item_code { get; set; } = string.Empty;
        public string item_name { get; set; } = string.Empty;
        public string item_category { get; set; } = string.Empty;
        public string item_quantity { get; set; } = string.Empty;
        public decimal item_price { get; set; }
        public bool is_promotion { get; set; }
        public bool is_default_combo { get; set; }
        public product? product_ref { get; set; }
        public promotion? promo_ref { get; set; }
        public string promo_type_display { get; set; } = string.Empty;
    }

    public class promo_builder_item : ViewModelBase
    {
        private int _quantity = 1;
        private bool _quantity_editable = true;

        public product? product_ref { get; set; }
        public int quantity
        {
            get { return _quantity; }
            set { _quantity = value; on_property_changed(); }
        }

        // En "Producto Individual en Oferta" la cantidad siempre es 1, por eso
        // la celda se muestra bloqueada.
        public bool quantity_editable
        {
            get { return _quantity_editable; }
            set { _quantity_editable = value; on_property_changed(); }
        }
    }

    // Marca/categoria seleccionable en el popup de la lista de precios.
    public class brand_selection_option : ViewModelBase
    {
        private bool _is_checked;
        private readonly Action? _on_changed;

        public string name { get; }

        public bool is_checked
        {
            get => _is_checked;
            set
            {
                if (_is_checked == value) return;
                _is_checked = value;
                on_property_changed();
                _on_changed?.Invoke();
            }
        }

        public brand_selection_option(string name, Action? on_changed)
        {
            this.name = name;
            _on_changed = on_changed;
        }
    }

    public class InventoryViewModel : ViewModelBase
    {
        // Codigos de promocion con el mismo formato correlativo de los productos: prefijo + 5 digitos.
        public const string promo_prefix_oferta = "OF";
        public const string promo_prefix_kit = "KIT";
        public const string promo_prefix_combo = "COM";
        private const int promo_code_digits = 5;

        private readonly IInventoryService _inventory_service;
        private List<product> _all_products_source;
        private List<promotion> _all_promotions_source;
        private HashSet<string> _trashed_promotion_codes = new(StringComparer.Ordinal);
        private product? _product_being_edited;
        private promotion? _promotion_being_edited;
        private string _error_message = string.Empty;
        private bool _is_loading = false;

        private string _search_query = string.Empty;
        private int _selected_tab_index = 0;
        private string _new_code = string.Empty;
        private string _new_name = string.Empty;
        private string _new_category = string.Empty;
        private string _new_quantity = string.Empty;
        private string _new_price = string.Empty;
        private string _add_button_text = "+ Añadir Producto";

        private int _promo_type_index = 0;
        private string _promo_search_query = string.Empty;
        private string _new_promo_name = string.Empty;
        private string _new_promo_price = string.Empty;
        private bool _all_brands_selected = true;
        private bool _suppress_brand_sync;

        public ObservableCollection<string> category_options { get; }
        public ObservableCollection<brand_selection_option> price_list_brand_options { get; }
        public ObservableCollection<inventory_item_dto> todos_list { get; }
        public ObservableCollection<inventory_item_dto> defile_list { get; }
        public ObservableCollection<inventory_item_dto> oleos_list { get; }
        public ObservableCollection<inventory_item_dto> rembrandt_list { get; }
        public ObservableCollection<inventory_item_dto> bioline_list { get; }
        public ObservableCollection<inventory_item_dto> amazonia_list { get; }
        public ObservableCollection<inventory_item_dto> kedam_list { get; }
        public ObservableCollection<inventory_item_dto> depil_list { get; }
        public ObservableCollection<inventory_item_dto> estilista_list { get; }
        public ObservableCollection<inventory_item_dto> cutique_list { get; }
        public ObservableCollection<inventory_item_dto> otros_list { get; }
        public ObservableCollection<inventory_item_dto> promociones_list { get; }
        
        public ObservableCollection<product> promo_search_results { get; }
        public ObservableCollection<promo_builder_item> builder_items { get; }

        public bool is_editing_promotion => _promotion_being_edited != null;

        // En oferta individual solo cabe un producto: cuando ya hay uno en la
        // tabla se oculta el boton Agregar y hay que quitarlo con la X primero.
        public bool can_add_products => _promo_type_index != 0 || builder_items.Count == 0;

        public ICommand open_add_window_command { get; }
        public ICommand save_product_command { get; }
        public ICommand edit_command { get; }
        public ICommand delete_command { get; }
        public ICommand delete_promotion_command { get; }
        public ICommand save_promotion_command { get; }
        public ICommand add_to_builder_command { get; }
        public ICommand remove_from_builder_command { get; }
        public ICommand edit_promotion_command { get; }
        public ICommand generate_price_list_command { get; }
        public ICommand clear_price_list_command { get; }
        
        public Action? on_request_add_window;
        public Action? on_request_add_promotion_window;
        public Action? on_close_add_window;
        public Action? on_close_add_promotion_window;

        public bool can_edit_category
        {
            get { return _selected_tab_index == 0; }
        }

        public bool is_editing_product => _product_being_edited != null;

        public string ErrorMessage
        {
            get { return _error_message; }
            set { _error_message = value; on_property_changed(); }
        }

        public bool IsLoading
        {
            get { return _is_loading; }
            set { _is_loading = value; on_property_changed(); }
        }

        public string search_query
        {
            get { return _search_query; }
            set { _search_query = value; on_property_changed(); filter_data(); }
        }

        public int selected_tab_index
        {
            get { return _selected_tab_index; }
            set 
            { 
                _selected_tab_index = value; 
                on_property_changed(); 
                update_category_from_tab(); 
                on_property_changed(nameof(can_edit_category)); 
            }
        }

        public string add_button_text
        {
            get { return _add_button_text; }
            set { _add_button_text = value; on_property_changed(); }
        }

        public string new_code
        {
            get { return _new_code; }
            set { _new_code = value; on_property_changed(); }
        }

        public string new_name
        {
            get { return _new_name; }
            set { _new_name = value; on_property_changed(); }
        }

        public string new_category
        {
            get { return _new_category; }
            set
            {
                if (_new_category == value) return;
                _new_category = value;
                on_property_changed();
                if (_product_being_edited == null)
                {
                    new_code = generate_next_product_code(value);
                }
            }
        }

        public string new_quantity
        {
            get { return _new_quantity; }
            set { _new_quantity = value; on_property_changed(); }
        }

        public string new_price
        {
            get { return _new_price; }
            set { _new_price = value; on_property_changed(); }
        }

        public int promo_type_index
        {
            get { return _promo_type_index; }
            set
            {
                _promo_type_index = value;
                on_property_changed();
                refresh_builder_state();
            }
        }

        public string promo_search_query
        {
            get { return _promo_search_query; }
            set { _promo_search_query = value; on_property_changed(); filter_promo_search(); }
        }

        public string new_promo_name
        {
            get { return _new_promo_name; }
            set { _new_promo_name = value; on_property_changed(); }
        }

        public string new_promo_price
        {
            get { return _new_promo_price; }
            set { _new_promo_price = value; on_property_changed(); }
        }

        // "Todas a la vez" en el popup de la lista de precios.
        public bool all_brands_selected
        {
            get { return _all_brands_selected; }
            set
            {
                if (_all_brands_selected == value) return;
                _all_brands_selected = value;
                on_property_changed();
                if (value)
                {
                    _suppress_brand_sync = true;
                    foreach (brand_selection_option option in price_list_brand_options)
                    {
                        option.is_checked = true;
                    }
                    _suppress_brand_sync = false;
                }
            }
        }

        public InventoryViewModel(IInventoryService inventory_service)
        {
            if (inventory_service == null) throw new ArgumentNullException(nameof(inventory_service));
            _inventory_service = inventory_service;

            _all_products_source = new List<product>();
            _all_promotions_source = new List<promotion>();

            category_options = new ObservableCollection<string> { "Defile", "Oleos", "Rembrandt", "Bioline", "Amazonia Secret", "Kedam", "Depil Clear", "Estilista", "Cutique", "Otros" };
            
            price_list_brand_options = new ObservableCollection<brand_selection_option>();

            foreach (string brand in category_options)
            {
                price_list_brand_options.Add(new brand_selection_option(brand, on_brand_option_changed));
            }
            
            todos_list = new ObservableCollection<inventory_item_dto>();
            defile_list = new ObservableCollection<inventory_item_dto>();
            oleos_list = new ObservableCollection<inventory_item_dto>();
            rembrandt_list = new ObservableCollection<inventory_item_dto>();
            bioline_list = new ObservableCollection<inventory_item_dto>();
            amazonia_list = new ObservableCollection<inventory_item_dto>();
            kedam_list = new ObservableCollection<inventory_item_dto>();
            depil_list = new ObservableCollection<inventory_item_dto>();
            estilista_list = new ObservableCollection<inventory_item_dto>();
            cutique_list = new ObservableCollection<inventory_item_dto>();
            otros_list = new ObservableCollection<inventory_item_dto>();
            promociones_list = new ObservableCollection<inventory_item_dto>();
            
            promo_search_results = new ObservableCollection<product>();
        builder_items = new ObservableCollection<promo_builder_item>();

            open_add_window_command = new RelayCommand(execute_open_add_window);
            save_product_command = new RelayCommand(execute_save_product);
            edit_command = new RelayCommand(execute_edit_product);
            delete_command = new RelayCommand(execute_delete_product);
            delete_promotion_command = new RelayCommand(execute_delete_promotion);
            save_promotion_command = new RelayCommand(execute_save_promotion);
            add_to_builder_command = new RelayCommand(execute_add_to_builder);
            remove_from_builder_command = new RelayCommand(execute_remove_from_builder);
            edit_promotion_command = new RelayCommand(execute_edit_promotion);
            generate_price_list_command = new RelayCommand(execute_generate_price_list);
            clear_price_list_command = new RelayCommand(execute_clear_price_list);
            
            new_category = "Defile";
            
            load_initial_data_async();
        }

        private async void load_initial_data_async()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                
                IEnumerable<product> products = await _inventory_service.get_all_products_async();
                _all_products_source = products.ToList();
                
                IEnumerable<promotion> promotions = await _inventory_service.get_all_promotions_async();
                _all_promotions_source = promotions.ToList();

                await load_trashed_promotion_codes_async();

                filter_data();
            }
            catch (Exception ex)
            {
                ErrorMessage = ErrorText.Get(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async void refresh_data()
        {
            try
            {
                IEnumerable<product> products = await _inventory_service.get_all_products_async();
                _all_products_source = products.ToList();
                
                IEnumerable<promotion> promotions = await _inventory_service.get_all_promotions_async();
                _all_promotions_source = promotions.ToList();

                await load_trashed_promotion_codes_async();

                filter_data();
            }
            catch (Exception)
            {
            }
        }

        private async Task load_trashed_promotion_codes_async()
        {
            try
            {
                IEnumerable<promotion> deleted_promos = await _inventory_service.get_deleted_promotions_async();
                _trashed_promotion_codes = deleted_promos
                    .Select(p => p.promotion_code ?? string.Empty)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToHashSet();
            }
            catch (Exception)
            {
                _trashed_promotion_codes.Clear();
            }
        }

        public async Task<IEnumerable<product_sales_history_dto>> get_product_history_async(int id_product)
        {
            return await _inventory_service.get_product_sales_history_async(id_product);
        }

        public async Task<IEnumerable<promotion_sales_history_dto>> get_promotion_history_async(int id_promotion)
        {
            return await _inventory_service.get_promotion_sales_history_async(id_promotion);
        }

        private void update_category_from_tab()
        {
            add_button_text = (_selected_tab_index == 11) ? "+ Añadir Promoción" : "+ Añadir Producto";

            switch (_selected_tab_index)
            {
                case 1: new_category = "Defile"; break;
                case 2: new_category = "Oleos"; break;
                case 3: new_category = "Rembrandt"; break;
                case 4: new_category = "Bioline"; break;
                case 5: new_category = "Amazonia Secret"; break;
                case 6: new_category = "Kedam"; break;
                case 7: new_category = "Depil Clear"; break;
                case 8: new_category = "Estilista"; break;
                case 9: new_category = "Cutique"; break;
                case 10: new_category = "Otros"; break;
                default: break;
            }
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> category_code_prefixes = new()
        {
            ["Defile"] = "DEF",
            ["Oleos"] = "OLE",
            ["Rembrandt"] = "REM",
            ["Bioline"] = "BIO",
            ["Amazonia Secret"] = "AMA",
            ["Kedam"] = "KED",
            ["Depil Clear"] = "DEP",
            ["Estilista"] = "EST",
            ["Cutique"] = "CUT",
            ["Otros"] = "OTR"
        };

        private string generate_next_product_code(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || !category_code_prefixes.TryGetValue(category, out string? prefix))
            {
                return string.Empty;
            }

            int digit_count = prefix.EndsWith("-") ? 3 : 5;
            int max_number = 0;

            foreach (product p in _all_products_source)
            {
                if (string.IsNullOrWhiteSpace(p.product_code)) continue;
                if (!p.product_code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                string suffix = p.product_code.Substring(prefix.Length);
                if (suffix.Length == digit_count && int.TryParse(suffix, out int value) && value > max_number)
                {
                    max_number = value;
                }
            }

            return prefix + (max_number + 1).ToString(new string('0', digit_count));
        }

        // 0 = Oferta de un producto, 1 = Kit, 2 = Combo.
        public static string promo_prefix_for_type(int type_index)
        {
            if (type_index == 0) return promo_prefix_oferta;
            if (type_index == 1) return promo_prefix_kit;
            return promo_prefix_combo;
        }

        public static string promo_type_name(int type_index)
        {
            if (type_index == 0) return "Oferta";
            if (type_index == 1) return "Kit";
            return "Combo";
        }

        public static bool promo_code_is_type(string? code, int type_index)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            string prefix = promo_prefix_for_type(type_index);
            if (!code!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

            string suffix = code.Substring(prefix.Length);
            if (suffix.Length != promo_code_digits) return false;

            foreach (char c in suffix)
            {
                if (!char.IsDigit(c)) return false;
            }

            return true;
        }

        public static bool promo_is_oferta(string? code) => promo_code_is_type(code, 0);

        public static bool promo_is_kit(string? code) => promo_code_is_type(code, 1);

        public static bool promo_is_combo(string? code) => promo_code_is_type(code, 2);

        /// <summary>
        /// Devuelve el tipo de una promocion a partir de su codigo. Si el codigo no
        /// sigue el formato nuevo (prefijo + 5 digitos) se deduce de sus componentes.
        /// </summary>
        public static int promo_type_from_code(string? code, int cantidad_componentes, bool componentes_de_uno)
        {
            if (promo_code_is_type(code, 1)) return 1;
            if (promo_code_is_type(code, 2)) return 2;
            if (promo_code_is_type(code, 0)) return 0;

            // Codigos del formato anterior (C-KIT-XXXX, C-COMBO-XXXX, C-PROMO-XXXX).
            if (!string.IsNullOrWhiteSpace(code))
            {
                if (code!.StartsWith("C-KIT-", StringComparison.OrdinalIgnoreCase)) return 1;
                if (code.StartsWith("C-COMBO-", StringComparison.OrdinalIgnoreCase)) return 2;
                if (code.StartsWith("C-PROMO-", StringComparison.OrdinalIgnoreCase)) return 0;
            }

            return componentes_de_uno ? 0 : (cantidad_componentes > 1 ? 2 : 0);
        }

        private string generate_next_promotion_code(int type_index)
        {
            string prefix = promo_prefix_for_type(type_index);
            int max_number = 0;

            foreach (promotion p in _all_promotions_source)
            {
                if (p == null) continue;
                if (!promo_code_is_type(p.promotion_code, type_index)) continue;

                string suffix = p.promotion_code!.Substring(prefix.Length);
                if (int.TryParse(suffix, out int value) && value > max_number)
                {
                    max_number = value;
                }
            }

            // Las promociones en la papelera conservan su codigo, asi que tambien cuentan
            // para el correlativo: si no, una nueva promocion chocaria con el indice unico
            // cuando se restaure la anterior.
            if (_trashed_promotion_codes.Count > 0)
            {
                foreach (string trashed_code in _trashed_promotion_codes)
                {
                    if (!promo_code_is_type(trashed_code, type_index)) continue;

                    string suffix = trashed_code.Substring(prefix.Length);
                    if (int.TryParse(suffix, out int value) && value > max_number)
                    {
                        max_number = value;
                    }
                }
            }

            int next_number = max_number + 1;
            while (_trashed_promotion_codes.Contains(prefix + next_number.ToString(new string('0', promo_code_digits))))
            {
                next_number++;
            }

            return prefix + next_number.ToString(new string('0', promo_code_digits));
        }

        private void assign_row_numbers(ObservableCollection<inventory_item_dto> target_list)        {
            int row = 1;
            foreach (inventory_item_dto item in target_list)
            {
                item.RowNumber = row++;
            }
        }

        private static inventory_item_dto create_product_dto(product p)
        {
            return new inventory_item_dto
            {
                id_display = p.id_product.ToString(),
                item_code = p.product_code,
                item_name = p.name,
                item_category = p.category,
                item_quantity = p.stock_quantity.ToString(),
                item_price = p.unit_price_usd,
                is_promotion = false,
                is_default_combo = false,
                product_ref = p
            };
        }

        private static inventory_item_dto create_promotion_dto(promotion p, string display_code, int calculated_available)
        {
            return new inventory_item_dto
            {
                id_display = p.id_promotion.ToString(),
                item_code = display_code,
                item_name = p.name,
                item_category = "Promociones",
                item_quantity = calculated_available.ToString(),
                item_price = p.unit_price_usd,
                is_promotion = true,
                is_default_combo = false,
                promo_ref = p
            };
        }

        private void filter_data()
        {
            List<product> filtered_products = _all_products_source.Where(p =>
                string.IsNullOrWhiteSpace(_search_query) ||
                SearchText.combine(
                    p.name,
                    p.product_code,
                    p.category,
                    p.stock_quantity.ToString("0.##", CultureInfo.InvariantCulture),
                    SearchText.money(p.unit_price_usd)
                ).Contains(_search_query.ToLowerInvariant())).ToList();

            List<promotion> filtered_promos = _all_promotions_source.Where(p =>
                string.IsNullOrWhiteSpace(_search_query) ||
                SearchText.combine(
                    p.name,
                    p.promotion_code,
                    p.category,
                    SearchText.money(p.unit_price_usd)
                ).Contains(_search_query.ToLowerInvariant())).ToList();

            todos_list.Clear();
            defile_list.Clear();
            oleos_list.Clear();
            rembrandt_list.Clear();
            bioline_list.Clear();
            amazonia_list.Clear();
            kedam_list.Clear();
            depil_list.Clear();
            estilista_list.Clear();
            cutique_list.Clear();
            otros_list.Clear();
            promociones_list.Clear();

            foreach (product p in filtered_products)
            {
                todos_list.Add(create_product_dto(p));

                inventory_item_dto category_dto = create_product_dto(p);

                string safe_category = p.category ?? string.Empty;

                if (safe_category.Equals("Defile", StringComparison.OrdinalIgnoreCase))
                    defile_list.Add(category_dto);
                else if (safe_category.Equals("Oleos", StringComparison.OrdinalIgnoreCase))
                    oleos_list.Add(category_dto);
                else if (safe_category.Equals("Rembrandt", StringComparison.OrdinalIgnoreCase))
                    rembrandt_list.Add(category_dto);
                else if (safe_category.Equals("Bioline", StringComparison.OrdinalIgnoreCase))
                    bioline_list.Add(category_dto);
                else if (safe_category.Equals("Amazonia Secret", StringComparison.OrdinalIgnoreCase))
                    amazonia_list.Add(category_dto);
                else if (safe_category.Equals("Kedam", StringComparison.OrdinalIgnoreCase))
                    kedam_list.Add(category_dto);
                else if (safe_category.Equals("Depil Clear", StringComparison.OrdinalIgnoreCase))
                    depil_list.Add(category_dto);
                else if (safe_category.Equals("Estilista", StringComparison.OrdinalIgnoreCase))
                    estilista_list.Add(category_dto);
                else if (safe_category.Equals("Cutique", StringComparison.OrdinalIgnoreCase))
                    cutique_list.Add(category_dto);
                else
                    otros_list.Add(category_dto);
            }

            foreach (promotion p in filtered_promos)
            {
                if (p.items == null || p.items.Count == 0) continue;
                if (p.items.Any(i => i.product == null || i.quantity_required <= 0)) continue;

                int calculated_available = int.MaxValue;
                foreach (var item in p.items)
                {
                    int max_combos = item.product!.stock_quantity / item.quantity_required;
                    if (max_combos < calculated_available) calculated_available = max_combos;
                }
                if (calculated_available <= 0) continue;

                string display_code = p.promotion_code ?? string.Empty;

                inventory_item_dto new_dto = create_promotion_dto(p, display_code, calculated_available);
                new_dto.promo_type_display = promo_type_name(
                    promo_type_from_code(p.promotion_code, p.items.Count, false));

                todos_list.Add(new_dto);
                promociones_list.Add(new_dto);
            }

            assign_row_numbers(todos_list);
            assign_row_numbers(defile_list);
            assign_row_numbers(oleos_list);
            assign_row_numbers(rembrandt_list);
            assign_row_numbers(bioline_list);
            assign_row_numbers(amazonia_list);
            assign_row_numbers(kedam_list);
            assign_row_numbers(depil_list);
            assign_row_numbers(estilista_list);
            assign_row_numbers(cutique_list);
            assign_row_numbers(otros_list);
            assign_row_numbers(promociones_list);
        }

        private void filter_promo_search()
        {
            promo_search_results.Clear();
            if (string.IsNullOrWhiteSpace(_promo_search_query)) return;

            IEnumerable<product> results = _all_products_source.Where(p =>
                SearchText.combine(
                    p.name,
                    p.product_code,
                    p.category,
                    p.stock_quantity.ToString("0.##", CultureInfo.InvariantCulture),
                    SearchText.money(p.unit_price_usd)
                ).Contains(_promo_search_query.ToLowerInvariant()));

            foreach (product p in results)
            {
                promo_search_results.Add(p);
            }
        }

        private void execute_open_add_window(object? parameter)
        {
            if (_selected_tab_index == 11)
            {
                _promotion_being_edited = null;
                on_property_changed(nameof(is_editing_promotion));
                _promo_type_index = 0;
                on_property_changed(nameof(promo_type_index));
                _promo_search_query = string.Empty;
                on_property_changed(nameof(promo_search_query));
                new_promo_name = string.Empty;
                new_promo_price = string.Empty;
                promo_search_results.Clear();
                builder_items.Clear();
                on_request_add_promotion_window?.Invoke();
                return;
            }

            _product_being_edited = null;
            new_code = string.Empty;
            new_name = string.Empty;
            new_quantity = string.Empty;
            new_price = string.Empty;
            update_category_from_tab();
            new_code = generate_next_product_code(new_category);
            on_property_changed(nameof(can_edit_category));
            on_property_changed(nameof(is_editing_product));
            on_request_add_window?.Invoke();
        }

        private void execute_edit_product(object? parameter)
        {
            if (parameter is inventory_item_dto dto && !dto.is_promotion && dto.product_ref != null)
            {
                _product_being_edited = dto.product_ref;
                new_code = dto.product_ref.product_code;
                new_name = dto.product_ref.name;
                new_category = dto.product_ref.category;
                new_quantity = dto.product_ref.stock_quantity.ToString();
                new_price = dto.product_ref.unit_price_usd.ToString();
                on_property_changed(nameof(can_edit_category));
                on_property_changed(nameof(is_editing_product));
                on_request_add_window?.Invoke();
            }
        }

        private void execute_edit_promotion(object? parameter)
        {
            if (parameter is inventory_item_dto dto && dto.is_promotion && dto.promo_ref != null)
            {
                _promotion_being_edited = dto.promo_ref;
                on_property_changed(nameof(is_editing_promotion));

                new_promo_name = dto.promo_ref.name;
                new_promo_price = dto.promo_ref.unit_price_usd.ToString();

                builder_items.Clear();
                _promo_search_query = string.Empty;
                on_property_changed(nameof(promo_search_query));
                promo_search_results.Clear();

                var componentes = dto.promo_ref.items?
                    .Where(i => i != null && i.product != null && i.quantity_required > 0)
                    .ToList() ?? new List<promotion_item>();

                foreach (promotion_item item in componentes)
                {
                    builder_items.Add(new promo_builder_item
                    {
                        product_ref = item.product,
                        quantity = item.quantity_required
                    });
                }

                bool es_oferta_individual = componentes.Count == 1 && componentes[0].quantity_required == 1;
                _promo_type_index = promo_type_from_code(
                    dto.promo_ref.promotion_code, componentes.Count, es_oferta_individual);
                on_property_changed(nameof(promo_type_index));
                refresh_builder_state();

                on_request_add_promotion_window?.Invoke();
            }
        }

        // Al cambiar de tipo, la oferta individual se queda con un solo producto
        // y con cantidad 1, por lo que la celda de cantidad se bloquea.
        private void refresh_builder_state()
        {
            bool es_oferta_individual = _promo_type_index == 0;

            foreach (promo_builder_item item in builder_items)
            {
                item.quantity_editable = !es_oferta_individual;
                if (es_oferta_individual) item.quantity = 1;
            }

            on_property_changed(nameof(can_add_products));
        }

        private void on_brand_option_changed()
        {
            if (_suppress_brand_sync) return;

            bool all_checked = price_list_brand_options.Any() &&
                price_list_brand_options.All(o => o.is_checked);

            if (all_checked && !_all_brands_selected)
            {
                _all_brands_selected = true;
                on_property_changed(nameof(all_brands_selected));
            }
            else if (!all_checked && _all_brands_selected)
            {
                _all_brands_selected = false;
                on_property_changed(nameof(all_brands_selected));
            }
        }

        private void execute_clear_price_list(object? parameter)
        {
            foreach (brand_selection_option option in price_list_brand_options)
            {
                if (option.is_checked) option.is_checked = false;
            }

            if (_all_brands_selected)
            {
                _all_brands_selected = false;
                on_property_changed(nameof(all_brands_selected));
            }
        }

        private void execute_generate_price_list(object? parameter)
        {
            if (price_list_brand_options.Count == 0 || price_list_brand_options.All(o => !o.is_checked))
            {
                AppDialog.Show(
                    "Seleccione al menos una marca/categoría para generar la lista de precios.",
                    "Lista de precios",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                var items = new List<price_list_item>();

                foreach (product p in _all_products_source)
                {
                    if (string.IsNullOrWhiteSpace(p.product_code) || string.IsNullOrWhiteSpace(p.name)) continue;
                    if (p.unit_price_usd <= 0) continue;
                    if (!is_brand_selected(p.category)) continue;

                    items.Add(new price_list_item
                    {
                        product_code = p.product_code,
                        name = p.name,
                        brand = p.category,
                        unit_price_usd = p.unit_price_usd
                    });
                }

                foreach (promotion promo in _all_promotions_source)
                {
                    if (promo.items == null || promo.items.Count == 0) continue;
                    if (promo.items.Any(i => i.product == null || i.quantity_required <= 0)) continue;

                    string raw = promo.promotion_code ?? string.Empty;

                    // Las ofertas no son precios de lista estandar.
                    if (promo_is_oferta(raw)) continue;

                    if (promo.unit_price_usd <= 0) continue;

                    // Solo kits y combos (productos empaquetados con precio fijo). Si el
                    // codigo viene del formato antiguo se deduce de la composicion.
                    int tipo = promo_type_from_code(
                        raw,
                        promo.items?.Count ?? 0,
                        promo.items != null && promo.items.Count == 1 && promo.items[0].quantity_required == 1);

                    if (tipo == 0) continue;

                    string code = raw;

                    var brands = promo.items
                        .Select(i => string.IsNullOrWhiteSpace(i.product!.category) ? "Otros" : i.product!.category!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Where(is_brand_selected)
                        .ToList();

                    if (brands.Count == 0) continue;

                    foreach (string brand in brands)
                    {
                        items.Add(new price_list_item
                        {
                            product_code = code,
                            name = promo.name,
                            brand = brand,
                            unit_price_usd = promo.unit_price_usd
                        });
                    }
                }

                PriceListPdfGenerator.generate(items);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo generar la lista de precios: {ErrorText.Get(ex)}";
            }
        }

        private bool is_brand_selected(string? category)
        {
            string safe = string.IsNullOrWhiteSpace(category) ? "Otros" : category;
            return price_list_brand_options.Any(o =>
                o.is_checked && o.name.Equals(safe, StringComparison.OrdinalIgnoreCase));
        }

        private async void execute_delete_product(object? parameter)
        {
            if (parameter is not inventory_item_dto dto || dto.is_promotion || dto.product_ref == null) return;

            List<promotion> affected_promos = new();
            try
            {
                affected_promos = (await _inventory_service.get_promotions_using_product_async(dto.product_ref.id_product)).ToList();
            }
            catch (Exception)
            {
                // Si no se pueden consultar, se sigue con el aviso normal de siempre.
            }

            string mensaje = $"¿Seguro de eliminar el producto \"{dto.product_ref.name}\"?";
            if (affected_promos.Count > 0)
            {
                string lista = string.Join("\n", affected_promos.Select(p => $"   • {p.promotion_code} — {p.name}"));
                mensaje += $"\n\nEste producto forma parte de {affected_promos.Count} promoción(es) activa(s):\n{lista}\n\n" +
                           "Esas promociones también se eliminarán, porque sin el producto no se pueden vender. " +
                           "Si solo querías quitar el producto de la promoción, edita la promoción y quítalo de su composición.";
            }

            MessageBoxResult confirm = AppDialog.Show(mensaje, "Confirmar eliminación",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _inventory_service.soft_delete_product_async(dto.product_ref.id_product, null);
                load_initial_data_async();
                AppDataEvents.raise_catalogs_changed();

                if (affected_promos.Count > 0)
                {
                    AppDialog.Show(
                        $"Producto eliminado exitosamente.\n\nTambién se eliminaron {affected_promos.Count} promoción(es) que lo usaban.",
                        "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AppDialog.Show("Producto eliminado exitosamente.", "Exito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo eliminar el producto: {ErrorText.Get(ex)}";
                AppDialog.Show($"Error al eliminar el producto: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void execute_delete_promotion(object? parameter)
        {
            if (parameter is not inventory_item_dto dto || !dto.is_promotion || dto.promo_ref == null) return;

            MessageBoxResult confirm = AppDialog.Show(
                $"¿Seguro de eliminar la promoción \"{dto.promo_ref.name}\" ({dto.promo_ref.promotion_code})?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _inventory_service.soft_delete_promotion_async(dto.promo_ref.id_promotion, null);
                load_initial_data_async();
                AppDataEvents.raise_catalogs_changed();
                AppDialog.Show("Promoción eliminada exitosamente.", "Exito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo eliminar la promoción: {ErrorText.Get(ex)}";
            }
        }

        private async void execute_save_product(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(new_code) || string.IsNullOrWhiteSpace(new_name) || string.IsNullOrWhiteSpace(new_category))
            {
                ErrorMessage = "Tienes que llenar los campos obligatorios.";
                return;
            }

            if (!int.TryParse(new_quantity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed_quantity) || parsed_quantity < 0)
            {
                ErrorMessage = "La cantidad debe ser un número entero mayor o igual a 0.";
                return;
            }

            if (!decimal.TryParse((new_price ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed_price) || parsed_price < 0)
            {
                ErrorMessage = "El precio debe ser un número mayor o igual a 0.";
                return;
            }

            try
            {
                if (_product_being_edited != null)
                {
                    _product_being_edited.product_code = new_code;
                    _product_being_edited.name = new_name;
                    _product_being_edited.category = new_category;
                    _product_being_edited.unit_price_usd = parsed_price;
                    _product_being_edited.stock_quantity = parsed_quantity;
                    await _inventory_service.update_product_async(_product_being_edited);
                }
                else
                {
                    product new_prod = new product(new_code, new_name, new_category, parsed_price, parsed_quantity);
                    await _inventory_service.add_product_async(new_prod);
                }

                bool era_edicion = _product_being_edited != null;
                _product_being_edited = null;
                on_property_changed(nameof(is_editing_product));
                load_initial_data_async();
                on_close_add_window?.Invoke();
                AppDialog.Show(
                    era_edicion ? "Producto actualizado exitosamente." : "Producto creado exitosamente.",
                    "Exito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo guardar el producto: {ErrorText.Get(ex)}";
            }
        }

        private void execute_add_to_builder(object? parameter)
        {
            if (parameter is not product prod) return;

            if (_promo_type_index == 0)
            {
                // Oferta individual: una sola pieza, agregar otra reemplaza la anterior.
                builder_items.Clear();
                builder_items.Add(new promo_builder_item { product_ref = prod, quantity = 1 });
                refresh_builder_state();
                return;
            }

            if (builder_items.Any(i => i.product_ref != null && i.product_ref.id_product == prod.id_product)) return;

            builder_items.Add(new promo_builder_item { product_ref = prod, quantity = 1 });
            refresh_builder_state();
        }

        private void execute_remove_from_builder(object? parameter)
        {
            if (parameter is promo_builder_item item)
            {
                builder_items.Remove(item);
                on_property_changed(nameof(can_add_products));
            }
        }

        private async void execute_save_promotion(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(new_promo_price))
            {
                ErrorMessage = "Tienes que llenar los campos obligatorios.";
                return;
            }

            if (!decimal.TryParse((new_promo_price ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed_price) || parsed_price < 0)
            {
                ErrorMessage = "El precio de la promoción debe ser un número mayor o igual a 0.";
                return;
            }

            if (_promo_type_index != 0 && builder_items.Any(it => it.product_ref != null && it.quantity < 1))
            {
                ErrorMessage = "La cantidad de cada producto en el Kit/Combo debe ser al menos 1.";
                return;
            }

            // La tabla de composicion es la unica fuente de la verdad para los tres
            // tipos: en oferta individual debe quedar exactamente un producto.
            List<promo_builder_item> composicion = builder_items.Where(it => it.product_ref != null).ToList();

            if (composicion.Count == 0)
            {
                ErrorMessage = "Agrega al menos un producto a la composición.";
                return;
            }

            if (_promo_type_index == 0 && composicion.Count > 1)
            {
                ErrorMessage = "Una oferta individual solo lleva un producto. Si quieres varios, elige Combo.";
                return;
            }

            if (_promo_type_index != 0 && string.IsNullOrWhiteSpace(new_promo_name))
            {
                ErrorMessage = "Escribe el nombre del Kit/Combo.";
                return;
            }

            try
            {
                // Se conserva el codigo, salvo que el tipo haya cambiado (por ejemplo
                // de Oferta a Kit), en cuyo caso se asigna uno del tipo nuevo.
                string code = generate_next_promotion_code(_promo_type_index);
                if (_promotion_being_edited != null)
                {
                    string code_to_keep = _promotion_being_edited.promotion_code ?? string.Empty;
                    if (promo_code_is_type(code_to_keep, _promo_type_index)) code = code_to_keep;
                }

                string final_name = new_promo_name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(final_name))
                {
                    final_name = "PROMO " + (composicion[0].product_ref?.name ?? "PROMOCION");
                }

                promotion promo_to_save = new promotion(code, final_name, "Promociones", parsed_price);

                if (_promotion_being_edited != null)
                {
                    promo_to_save.id_promotion = _promotion_being_edited.id_promotion;
                }

                foreach (promo_builder_item item in composicion)
                {
                    if (item.product_ref == null) continue;
                    int cantidad = _promo_type_index == 0 ? 1 : Math.Max(1, item.quantity);
                    promo_to_save.items.Add(new promotion_item(item.product_ref.id_product, cantidad));
                }

                if (_promotion_being_edited != null)
                {
                    await _inventory_service.update_promotion_async(promo_to_save);
                }
                else
                {
                    await _inventory_service.add_promotion_async(promo_to_save);
                }

                bool era_edicion = _promotion_being_edited != null;
                _promotion_being_edited = null;
                on_property_changed(nameof(is_editing_promotion));
                load_initial_data_async();
                on_close_add_promotion_window?.Invoke();
                AppDialog.Show(
                    build_promo_success_message(era_edicion),
                    "Exito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo guardar la promoción: {ErrorText.Get(ex)}";
            }
        }

        // "Oferta creada exitosamente." / "Kit actualizado exitosamente." / etc.
        // Kit y Combo son masculino, por eso el participio cambia segun el tipo.
        private string build_promo_success_message(bool es_edicion)
        {
            bool es_femenino = _promo_type_index == 0;
            string nombre = _promo_type_index == 0 ? "Oferta" : _promo_type_index == 1 ? "Kit" : "Combo";
            return es_edicion
                ? $"{nombre} actualizado exitosamente."
                : $"{nombre} {(es_femenino ? "creada" : "creado")} exitosamente.";
        }
    }
}