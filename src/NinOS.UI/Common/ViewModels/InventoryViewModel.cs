using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class inventory_item_dto
    {
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
        
        public Action? on_request_add_window;
        public Action? on_request_add_promotion_window;
        public Action? on_close_add_window;
        public Action? on_close_add_promotion_window;

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

            _all_products_source = new List<product>();
            _all_promotions_source = new List<promotion>();
            
            category_options = new ObservableCollection<string> { "Defile", "Óleos", "Rembrandt", "Bioline", "Amazonia Secret", "Kedam", "Depil Clear", "Estilista", "Otros" };
            
            todos_list = new ObservableCollection<inventory_item_dto>();
            defile_list = new ObservableCollection<inventory_item_dto>();
            oleos_list = new ObservableCollection<inventory_item_dto>();
            rembrandt_list = new ObservableCollection<inventory_item_dto>();
            bioline_list = new ObservableCollection<inventory_item_dto>();
            amazonia_list = new ObservableCollection<inventory_item_dto>();
            kedam_list = new ObservableCollection<inventory_item_dto>();
            depil_list = new ObservableCollection<inventory_item_dto>();
            estilista_list = new ObservableCollection<inventory_item_dto>();
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
            
            new_category = "Defile";
            
            load_initial_data();
        }

        private async void load_initial_data()
        {
            try
            {
                IEnumerable<product> products_data = await _inventory_service.get_all_products_async();
                _all_products_source = products_data.ToList();

                IEnumerable<promotion> promos_data = await _inventory_service.get_all_promotions_async();
                _all_promotions_source = promos_data.ToList();

                filter_data();
            }
            catch { }
        }

        private void update_category_from_tab()
        {
            add_button_text = (_selected_tab_index == 10) ? "+ Añadir Promoción" : "+ Añadir Producto";

            switch (_selected_tab_index)
            {
                case 1: new_category = "Defile"; break;
                case 2: new_category = "Óleos"; break;
                case 3: new_category = "Rembrandt"; break;
                case 4: new_category = "Bioline"; break;
                case 5: new_category = "Amazonia Secret"; break;
                case 6: new_category = "Kedam"; break;
                case 7: new_category = "Depil Clear"; break;
                case 8: new_category = "Estilista"; break;
                case 9: new_category = "Otros"; break;
                default: break;
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
            otros_list.Clear();
            promociones_list.Clear();

            foreach (product p in filtered_products)
            {
                inventory_item_dto dto = new inventory_item_dto {
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
                
                if (p.category == "Defile") defile_list.Add(dto);
                else if (p.category == "Óleos") oleos_list.Add(dto);
                else if (p.category == "Rembrandt") rembrandt_list.Add(dto);
                else if (p.category == "Bioline") bioline_list.Add(dto);
                else if (p.category == "Amazonia Secret") amazonia_list.Add(dto);
                else if (p.category == "Kedam") kedam_list.Add(dto);
                else if (p.category == "Depil Clear") depil_list.Add(dto);
                else if (p.category == "Estilista") estilista_list.Add(dto);
                else if (p.category == "Otros") otros_list.Add(dto);
            }

            foreach (promotion p in filtered_promos)
            {
                int calculated_available = 0;
                if (p.items != null && p.items.Count > 0)
                {
                    calculated_available = p.items.Min(i => (i.product != null && i.quantity_required > 0) ? (i.product.stock_quantity / i.quantity_required) : 0);
                }

                bool is_default = !p.promotion_code.StartsWith("C-KIT-") && !p.promotion_code.StartsWith("C-COMBO-") && !p.promotion_code.StartsWith("C-PROMO-");
                string display_code = p.promotion_code.Replace("C-PROMO-", "").Replace("C-KIT-", "KIT-").Replace("C-COMBO-", "COMBO-");

                inventory_item_dto dto = new inventory_item_dto {
                    id_display = p.id_promotion.ToString(),
                    item_code = display_code,
                    item_name = p.name,
                    item_category = p.category,
                    item_quantity = calculated_available.ToString(),
                    item_price = p.unit_price_usd,
                    is_promotion = true,
                    is_default_combo = is_default,
                    promo_ref = p
                };

                todos_list.Add(dto);
                promociones_list.Add(dto);
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
            if (_selected_tab_index == 10)
            {
                promo_type_index = 0;
                promo_search_query = string.Empty;
                selected_promo_product = null;
                new_promo_name = string.Empty;
                new_promo_price = string.Empty;
                promo_search_results.Clear();
                builder_items.Clear();
                
                if (on_request_add_promotion_window != null)
                {
                    on_request_add_promotion_window.Invoke();
                }
                return;
            }

            _product_being_edited = null;
            new_code = string.Empty;
            new_name = string.Empty;
            new_quantity = string.Empty;
            new_price = string.Empty;
            update_category_from_tab();
            
            if (on_request_add_window != null)
            {
                on_request_add_window.Invoke();
            }
        }

        private void execute_edit_product(object? parameter)
        {
            if (parameter is inventory_item_dto dto)
            {
                if (dto.is_promotion || dto.product_ref == null) return; 

                _product_being_edited = dto.product_ref;
                new_code = dto.product_ref.product_code;
                new_name = dto.product_ref.name;
                new_category = dto.product_ref.category;
                new_quantity = dto.product_ref.stock_quantity.ToString();
                new_price = dto.product_ref.unit_price_usd.ToString();
                
                if (on_request_add_window != null)
                {
                    on_request_add_window.Invoke();
                }
            }
        }

        private async void execute_delete_product(object? parameter)
        {
            if (parameter is inventory_item_dto dto)
            {
                if (dto.is_promotion || dto.product_ref == null) return;
                await _inventory_service.delete_product_async(dto.product_ref);
                load_initial_data(); 
            }
        }

        private async void execute_delete_promotion(object? parameter)
        {
            if (parameter is inventory_item_dto dto && dto.is_promotion && dto.promo_ref != null)
            {
                if (dto.is_default_combo) return;
                await _inventory_service.delete_promotion_async(dto.promo_ref);
                load_initial_data();
            }
        }

        private async void execute_save_product(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(new_code) || string.IsNullOrWhiteSpace(new_name) || string.IsNullOrWhiteSpace(new_category)) return;

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
            load_initial_data();
            
            if (on_close_add_window != null)
            {
                on_close_add_window.Invoke();
            }
        }

        private void execute_add_to_builder(object? parameter)
        {
            if (parameter is product prod)
            {
                if (!builder_items.Any(i => i.product_ref != null && i.product_ref.id_product == prod.id_product))
                {
                    builder_items.Add(new promo_builder_item { product_ref = prod });
                }
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

            if (_promo_type_index == 0)
            {
                if (_selected_promo_product == null) return;

                string new_code = "C-PROMO-" + _selected_promo_product.product_code;
                string final_name = string.IsNullOrWhiteSpace(new_promo_name) ? "PROMO " + _selected_promo_product.name : new_promo_name;
                
                promotion promo = new promotion(new_code, final_name, _selected_promo_product.category, parsed_price);
                promo.items.Add(new promotion_item(_selected_promo_product.id_product, 1));
                
                await _inventory_service.add_promotion_async(promo);
            }
            else
            {
                if (builder_items.Count == 0 || string.IsNullOrWhiteSpace(new_promo_name)) return;

                string prefix = _promo_type_index == 1 ? "C-KIT-" : "C-COMBO-";
                string new_code = prefix + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                
                string final_category = "Otros";
                if (builder_items.First().product_ref != null)
                {
                    final_category = builder_items.First().product_ref!.category;
                }

                promotion promo = new promotion(new_code, new_promo_name, final_category, parsed_price);
                foreach (promo_builder_item item in builder_items)
                {
                    if (item.product_ref != null)
                    {
                        promo.items.Add(new promotion_item(item.product_ref.id_product, item.quantity));
                    }
                }
                
                await _inventory_service.add_promotion_async(promo);
            }

            load_initial_data();
            
            if (on_close_add_promotion_window != null)
            {
                on_close_add_promotion_window.Invoke();
            }
        }
    }
}