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
    // Este DTO unificado nos permite meter Productos y Promociones en la misma tabla "Todos"
    public class inventory_item_dto
    {
        public string id_display { get; set; } = string.Empty;
        public string item_code { get; set; } = string.Empty;
        public string item_name { get; set; } = string.Empty;
        public string item_category { get; set; } = string.Empty;
        public string item_quantity { get; set; } = string.Empty;
        public decimal item_price { get; set; }
        public bool is_promotion { get; set; }
        public product? product_ref { get; set; }
        public promotion? promo_ref { get; set; }
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

        public ICommand open_add_window_command { get; }
        public ICommand save_product_command { get; }
        public ICommand edit_command { get; }
        public ICommand delete_command { get; }
        
        public Action? on_request_add_window;
        public Action? on_close_add_window;

        public string search_query
        {
            get { return _search_query; }
            set
            {
                _search_query = value;
                on_property_changed();
                filter_data(); // Ahora el buscador filtra TODO
            }
        }

        public int selected_tab_index
        {
            get { return _selected_tab_index; }
            set
            {
                _selected_tab_index = value;
                on_property_changed();
                update_category_from_tab();
            }
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

            open_add_window_command = new RelayCommand(execute_open_add_window);
            save_product_command = new RelayCommand(execute_save_product);
            edit_command = new RelayCommand(execute_edit_product);
            delete_command = new RelayCommand(execute_delete_product);
            
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
            // Filtrar productos
            List<product> filtered_products = _all_products_source.Where(p =>
                string.IsNullOrWhiteSpace(_search_query) ||
                p.name.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                p.product_code.Contains(_search_query, StringComparison.OrdinalIgnoreCase)).ToList();

            // Filtrar promociones
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

            // Llenar listas con productos físicos
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

            // Llenar listas con promociones calculadas
            foreach (promotion p in filtered_promos)
            {
                int calculated_available = 0;
                if (p.items != null && p.items.Count > 0)
                {
                    calculated_available = p.items.Min(i => i.product.stock_quantity / i.quantity_required);
                }

                inventory_item_dto dto = new inventory_item_dto {
                    id_display = p.id_promotion.ToString(),
                    item_code = p.promotion_code,
                    item_name = p.name,
                    item_category = p.category,
                    item_quantity = calculated_available.ToString(),
                    item_price = p.unit_price_usd,
                    is_promotion = true,
                    promo_ref = p
                };

                // ¡AQUÍ ESTÁ LA MAGIA! Las promos van tanto a la pestaña Todos como a su propia pestaña
                todos_list.Add(dto);
                promociones_list.Add(dto);
            }
        }

        private void execute_open_add_window(object? parameter)
        {
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
            if (parameter is inventory_item_dto dto)
            {
                // Protegemos el código: No se editan promociones desde esta ventana
                if (dto.is_promotion || dto.product_ref == null) return; 

                _product_being_edited = dto.product_ref;
                new_code = dto.product_ref.product_code;
                new_name = dto.product_ref.name;
                new_category = dto.product_ref.category;
                new_quantity = dto.product_ref.stock_quantity.ToString();
                new_price = dto.product_ref.unit_price_usd.ToString();
                on_request_add_window?.Invoke();
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
            load_initial_data(); // Esto refresca todo, incluyendo el recálculo matemático de las promociones
            on_close_add_window?.Invoke();
        }
    }
}