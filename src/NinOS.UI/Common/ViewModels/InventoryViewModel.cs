using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NinOS.Domain;
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
    }

    public class promo_builder_item : ViewModelBase
    {
        private int _quantity = 1;
        public product? product_ref { get; set; }
        public int quantity
        {
            get { return _quantity; }
            set { _quantity = value; on_property_changed(); }
        }
    }

    public class InventoryViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventory_service;
        private List<product> _all_products_source;
        private List<promotion> _all_promotions_source;
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
        private product? _selected_promo_product;
        private string _new_promo_name = string.Empty;
        private string _new_promo_price = string.Empty;

        public ObservableCollection<string> category_options { get; }
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

        public ICommand open_add_window_command { get; }
        public ICommand save_product_command { get; }
        public ICommand edit_command { get; }
        public ICommand delete_command { get; }
        public ICommand delete_promotion_command { get; }
        public ICommand save_promotion_command { get; }
        public ICommand add_to_builder_command { get; }
        public ICommand remove_from_builder_command { get; }
        public ICommand edit_promotion_command { get; }
        
        public Action? on_request_add_window;
        public Action? on_request_add_promotion_window;
        public Action? on_close_add_window;
        public Action? on_close_add_promotion_window;

        public bool can_edit_category
        {
            get { return _selected_tab_index == 0; }
        }

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
            set { _new_category = value; on_property_changed(); }
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
            set { _promo_type_index = value; on_property_changed(); }
        }

        public string promo_search_query
        {
            get { return _promo_search_query; }
            set { _promo_search_query = value; on_property_changed(); filter_promo_search(); }
        }

        public product? selected_promo_product
        {
            get { return _selected_promo_product; }
            set { _selected_promo_product = value; on_property_changed(); }
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

        public InventoryViewModel(IInventoryService inventory_service)
        {
            if (inventory_service == null) throw new ArgumentNullException(nameof(inventory_service));
            _inventory_service = inventory_service;

            _all_products_source = new List<product>();
            _all_promotions_source = new List<promotion>();

            category_options = new ObservableCollection<string> { "Defile", "Oleos", "Rembrandt", "Bioline", "Amazonia Secret", "Kedam", "Depil Clear", "Estilista", "Cutique", "Otros" };
            
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

                filter_data();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
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

                filter_data();
            }
            catch (Exception)
            {
            }
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

        private void assign_row_numbers(ObservableCollection<inventory_item_dto> target_list)
        {
            int row = 1;
            foreach (inventory_item_dto item in target_list)
            {
                item.RowNumber = row++;
            }
        }

        private void filter_data()
        {
            List<product> filtered_products = _all_products_source.Where(p =>
                string.IsNullOrWhiteSpace(_search_query) ||
                p.name.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                p.product_code.Contains(_search_query, StringComparison.OrdinalIgnoreCase)).ToList();

            List<promotion> filtered_promos = _all_promotions_source.Where(p =>
                string.IsNullOrWhiteSpace(_search_query) ||
                p.name.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                p.promotion_code.Contains(_search_query, StringComparison.OrdinalIgnoreCase)).ToList();

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
                inventory_item_dto new_dto = new inventory_item_dto
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

                todos_list.Add(new_dto);

                string safe_category = p.category ?? string.Empty;

                if (safe_category.Contains("Defile", StringComparison.OrdinalIgnoreCase))
                    defile_list.Add(new_dto);
                else if (safe_category.Contains("Ole", StringComparison.OrdinalIgnoreCase))
                    oleos_list.Add(new_dto);
                else if (safe_category.Contains("Rembrandt", StringComparison.OrdinalIgnoreCase))
                    rembrandt_list.Add(new_dto);
                else if (safe_category.Contains("Bioline", StringComparison.OrdinalIgnoreCase))
                    bioline_list.Add(new_dto);
                else if (safe_category.Contains("Amazonia", StringComparison.OrdinalIgnoreCase))
                    amazonia_list.Add(new_dto);
                else if (safe_category.Contains("Kedam", StringComparison.OrdinalIgnoreCase))
                    kedam_list.Add(new_dto);
                else if (safe_category.Contains("Depil", StringComparison.OrdinalIgnoreCase))
                    depil_list.Add(new_dto);
                else if (safe_category.Contains("Estilista", StringComparison.OrdinalIgnoreCase))
                    estilista_list.Add(new_dto);
                else if (safe_category.Contains("Cutique", StringComparison.OrdinalIgnoreCase))
                    cutique_list.Add(new_dto);
                else
                    otros_list.Add(new_dto);
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

                string display_code = p.promotion_code.Replace("C-PROMO-", "").Replace("C-KIT-", "KIT-").Replace("C-COMBO-", "COMBO-");

                inventory_item_dto new_dto = new inventory_item_dto
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
                p.name.Contains(_promo_search_query, StringComparison.OrdinalIgnoreCase) || 
                p.product_code.Contains(_promo_search_query, StringComparison.OrdinalIgnoreCase));

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
                promo_type_index = 0;
                promo_search_query = string.Empty;
                selected_promo_product = null;
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
            on_property_changed(nameof(can_edit_category));
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
                on_request_add_window?.Invoke();
            }
        }

        private void execute_edit_promotion(object? parameter)
        {
            if (parameter is inventory_item_dto dto && dto.is_promotion && dto.promo_ref != null)
            {
                _promotion_being_edited = dto.promo_ref;
                new_promo_name = dto.promo_ref.name;
                new_promo_price = dto.promo_ref.unit_price_usd.ToString();
                
                builder_items.Clear();
                
                if (dto.promo_ref.items != null && dto.promo_ref.items.Count == 1 && dto.promo_ref.items.First().quantity_required == 1)
                {
                    promo_type_index = 0;
                    promotion_item item = dto.promo_ref.items.First();
                    selected_promo_product = _all_products_source.FirstOrDefault(p => p.id_product == item.id_product);
                }
                else
                {
                    promo_type_index = 2;
                    if (dto.promo_ref.items != null)
                    {
                        foreach (promotion_item item in dto.promo_ref.items)
                        {
                            if (item.product != null)
                            {
                                builder_items.Add(new promo_builder_item { 
                                    product_ref = item.product, 
                                    quantity = item.quantity_required 
                                });
                            }
                        }
                    }
                }
                
                on_request_add_promotion_window?.Invoke();
            }
        }

        private async void execute_delete_product(object? parameter)
        {
            if (parameter is inventory_item_dto dto && !dto.is_promotion && dto.product_ref != null)
            {
                await _inventory_service.delete_product_async(dto.product_ref);
                load_initial_data_async();
            }
        }

        private async void execute_delete_promotion(object? parameter)
        {
            if (parameter is inventory_item_dto dto && dto.is_promotion && dto.promo_ref != null)
            {
                await _inventory_service.delete_promotion_async(dto.promo_ref);
                load_initial_data_async();
            }
        }

        private async void execute_save_product(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(new_code) || string.IsNullOrWhiteSpace(new_name) || string.IsNullOrWhiteSpace(new_category))
                return;

            decimal parsed_price = decimal.TryParse(new_price, out decimal temp_price) ? temp_price : 0m;
            int parsed_quantity = int.TryParse(new_quantity, out int temp_qty) ? temp_qty : 0;

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
            
            _product_being_edited = null;
            load_initial_data_async();
            on_close_add_window?.Invoke();
        }

        private void execute_add_to_builder(object? parameter)
        {
            if (parameter is product prod && !builder_items.Any(i => i.product_ref != null && i.product_ref.id_product == prod.id_product))
            {
                builder_items.Add(new promo_builder_item { product_ref = prod });
            }
        }

        private void execute_remove_from_builder(object? parameter)
        {
            if (parameter is promo_builder_item item)
            {
                builder_items.Remove(item);
            }
        }

        private async void execute_save_promotion(object? parameter)
        {
            decimal parsed_price = decimal.TryParse(new_promo_price, out decimal temp_price) ? temp_price : 0m;

            if (_promotion_being_edited != null)
            {
                promotion promo_update = new promotion(_promotion_being_edited.promotion_code, new_promo_name, "Promociones", parsed_price);
                promo_update.id_promotion = _promotion_being_edited.id_promotion;
                
                if (_promo_type_index == 0)
                {
                    if (_selected_promo_product != null)
                    {
                        promo_update.items.Add(new promotion_item(_selected_promo_product.id_product, 1));
                    }
                }
                else
                {
                    foreach (promo_builder_item item in builder_items)
                    {
                        if (item.product_ref != null)
                        {
                            promo_update.items.Add(new promotion_item(item.product_ref.id_product, item.quantity));
                        }
                    }
                }

                await _inventory_service.update_promotion_async(promo_update);
            }
            else if (_promo_type_index == 0)
            {
                if (_selected_promo_product == null) return;

                string new_code = "C-PROMO-" + _selected_promo_product.product_code;
                string final_name = string.IsNullOrWhiteSpace(new_promo_name) ? "PROMO " + _selected_promo_product.name : new_promo_name;
                
                promotion new_promo = new promotion(new_code, final_name, "Promociones", parsed_price);
                new_promo.items.Add(new promotion_item(_selected_promo_product.id_product, 1));
                await _inventory_service.add_promotion_async(new_promo);
            }
            else
            {
                if (builder_items.Count == 0 || string.IsNullOrWhiteSpace(new_promo_name)) return;

                string prefix = _promo_type_index == 1 ? "C-KIT-" : "C-COMBO-";
                string new_code = prefix + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                
                promotion new_promo = new promotion(new_code, new_promo_name, "Promociones", parsed_price);
                foreach (promo_builder_item item in builder_items)
                {
                    if (item.product_ref != null)
                    {
                        new_promo.items.Add(new promotion_item(item.product_ref.id_product, item.quantity));
                    }
                }
                await _inventory_service.add_promotion_async(new_promo);
            }

            _promotion_being_edited = null;
            load_initial_data_async();
            on_close_add_promotion_window?.Invoke();
        }
    }
}