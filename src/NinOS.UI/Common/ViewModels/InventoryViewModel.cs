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
        private int _rowNumber;
        public int RowNumber 
        { 
            get => _rowNumber; 
            set { _rowNumber = value; on_property_changed(); }
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
        private List<product> _all_products_source = new List<product>();
        private List<promotion> _all_promotions_source = new List<promotion>();
        private product? _product_being_edited;
        private promotion? _promotion_being_edited;
        private string _errorMessage = string.Empty;
        private bool _isLoading = false;

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

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; on_property_changed(); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; on_property_changed(); }
        }

        public string search_query
        {
            get { return _search_query; }
            set { _search_query = value; on_property_changed(); filter_data(); }
        }

        public int selected_tab_index
        {
            get { return _selected_tab_index; }
            set { _selected_tab_index = value; on_property_changed(); update_category_from_tab(); }
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
            
            load_initial_data();
        }

        private async void load_initial_data()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                
                var products = await _inventory_service.get_all_products_async();
                _all_products_source = products.ToList();
                
                var promotions = await _inventory_service.get_all_promotions_async();
                _all_promotions_source = promotions.ToList();

                filter_data();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
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

        private void AddRowNumbers(ObservableCollection<inventory_item_dto> list)
        {
            int rowNumber = 1;
            foreach (var item in list)
            {
                item.RowNumber = rowNumber++;
            }
        }

        private void filter_data()
        {
            try
            {
                var filtered_products = _all_products_source.Where(p =>
                    string.IsNullOrWhiteSpace(_search_query) ||
                    p.name.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                    p.product_code.Contains(_search_query, StringComparison.OrdinalIgnoreCase)).ToList();

                var filtered_promos = _all_promotions_source.Where(p =>
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
                    var dto = new inventory_item_dto
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

                    todos_list.Add(dto);

                    string cat = p.category ?? string.Empty;

                    if (cat.Contains("Defile", StringComparison.OrdinalIgnoreCase))
                        defile_list.Add(dto);
                    else if (cat.Contains("Ole", StringComparison.OrdinalIgnoreCase))
                        oleos_list.Add(dto);
                    else if (cat.Contains("Rembrandt", StringComparison.OrdinalIgnoreCase))
                        rembrandt_list.Add(dto);
                    else if (cat.Contains("Bioline", StringComparison.OrdinalIgnoreCase))
                        bioline_list.Add(dto);
                    else if (cat.Contains("Amazonia", StringComparison.OrdinalIgnoreCase))
                        amazonia_list.Add(dto);
                    else if (cat.Contains("Kedam", StringComparison.OrdinalIgnoreCase))
                        kedam_list.Add(dto);
                    else if (cat.Contains("Depil", StringComparison.OrdinalIgnoreCase))
                        depil_list.Add(dto);
                    else if (cat.Contains("Estilista", StringComparison.OrdinalIgnoreCase))
                        estilista_list.Add(dto);
                    else if (cat.Contains("Cutique", StringComparison.OrdinalIgnoreCase))
                        cutique_list.Add(dto);
                    else
                        otros_list.Add(dto);
                }

                foreach (promotion p in filtered_promos)
                {
                    int calculated_available = 0;
                    if (p.items != null && p.items.Count > 0)
                    {
                        try
                        {
                            calculated_available = p.items.Min(i => (i.product != null && i.quantity_required > 0) ? (i.product.stock_quantity / i.quantity_required) : 0);
                        }
                        catch { calculated_available = 0; }
                    }

                    string display_code = p.promotion_code.Replace("C-PROMO-", "").Replace("C-KIT-", "KIT-").Replace("C-COMBO-", "COMBO-");

                    var dto = new inventory_item_dto
                    {
                        id_display = p.id_promotion.ToString(),
                        item_code = display_code,
                        item_name = p.name,
                        item_category = p.category,
                        item_quantity = calculated_available.ToString(),
                        item_price = p.unit_price_usd,
                        is_promotion = true,
                        is_default_combo = false,
                        promo_ref = p
                    };

                    todos_list.Add(dto);
                    promociones_list.Add(dto);
                }

                AddRowNumbers(todos_list);
                AddRowNumbers(defile_list);
                AddRowNumbers(oleos_list);
                AddRowNumbers(rembrandt_list);
                AddRowNumbers(bioline_list);
                AddRowNumbers(amazonia_list);
                AddRowNumbers(kedam_list);
                AddRowNumbers(depil_list);
                AddRowNumbers(estilista_list);
                AddRowNumbers(cutique_list);
                AddRowNumbers(otros_list);
                AddRowNumbers(promociones_list);

                on_property_changed(nameof(todos_list));
                on_property_changed(nameof(defile_list));
                on_property_changed(nameof(oleos_list));
                on_property_changed(nameof(rembrandt_list));
                on_property_changed(nameof(bioline_list));
                on_property_changed(nameof(amazonia_list));
                on_property_changed(nameof(kedam_list));
                on_property_changed(nameof(depil_list));
                on_property_changed(nameof(estilista_list));
                on_property_changed(nameof(cutique_list));
                on_property_changed(nameof(otros_list));
                on_property_changed(nameof(promociones_list));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error filtrando: {ex.Message}";
            }
        }

        private void filter_promo_search()
        {
            promo_search_results.Clear();
            if (string.IsNullOrWhiteSpace(_promo_search_query)) return;

            var results = _all_products_source.Where(p => 
                p.name.Contains(_promo_search_query, StringComparison.OrdinalIgnoreCase) || 
                p.product_code.Contains(_promo_search_query, StringComparison.OrdinalIgnoreCase)).ToList();

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
                on_request_add_window?.Invoke();
            }
        }

        private void execute_edit_promotion(object? parameter)
        {
            if (parameter is inventory_item_dto dto && dto.is_promotion && dto.promo_ref != null)
            {
                _promotion_being_edited = dto.promo_ref;
                promo_type_index = 2;
                new_promo_name = dto.promo_ref.name;
                new_promo_price = dto.promo_ref.unit_price_usd.ToString();
                
                builder_items.Clear();
                if (dto.promo_ref.items != null)
                {
                    foreach (var item in dto.promo_ref.items)
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
                
                on_request_add_promotion_window?.Invoke();
            }
        }

        private async void execute_delete_product(object? parameter)
        {
            if (parameter is inventory_item_dto dto && !dto.is_promotion && dto.product_ref != null)
            {
                await _inventory_service.delete_product_async(dto.product_ref);
                load_initial_data();
            }
        }

        private async void execute_delete_promotion(object? parameter)
        {
            if (parameter is inventory_item_dto dto && dto.is_promotion && dto.promo_ref != null)
            {
                await _inventory_service.delete_promotion_async(dto.promo_ref);
                load_initial_data();
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
                var new_prod = new product(new_code, new_name, new_category, parsed_price, parsed_quantity);
                await _inventory_service.add_product_async(new_prod);
            }
            
            _product_being_edited = null;
            load_initial_data();
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
                _promotion_being_edited.name = new_promo_name;
                _promotion_being_edited.unit_price_usd = parsed_price;
                await _inventory_service.update_promotion_async(_promotion_being_edited);
            }
            else if (_promo_type_index == 0)
            {
                if (_selected_promo_product == null) return;

                string new_code = "C-PROMO-" + _selected_promo_product.product_code;
                string final_name = string.IsNullOrWhiteSpace(new_promo_name) ? "PROMO " + _selected_promo_product.name : new_promo_name;
                
                var promo = new promotion(new_code, final_name, _selected_promo_product.category, parsed_price);
                promo.items.Add(new promotion_item(_selected_promo_product.id_product, 1));
                await _inventory_service.add_promotion_async(promo);
            }
            else
            {
                if (builder_items.Count == 0 || string.IsNullOrWhiteSpace(new_promo_name)) return;

                string prefix = _promo_type_index == 1 ? "C-KIT-" : "C-COMBO-";
                string new_code = prefix + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                
                string final_category = builder_items.First().product_ref?.category ?? "Otros";

                var promo = new promotion(new_code, new_promo_name, final_category, parsed_price);
                foreach (var item in builder_items)
                {
                    if (item.product_ref != null)
                    {
                        promo.items.Add(new promotion_item(item.product_ref.id_product, item.quantity));
                    }
                }
                await _inventory_service.add_promotion_async(promo);
            }

            _promotion_being_edited = null;
            load_initial_data();
            on_close_add_promotion_window?.Invoke();
        }
    }
}