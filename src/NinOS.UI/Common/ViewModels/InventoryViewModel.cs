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
    public class InventoryViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventory_service;
        private List<product> _all_products_source;
        private product? _product_being_edited;

        private string _search_query = string.Empty;
        private int _selected_tab_index = 0;
        private string _new_code = string.Empty;
        private string _new_name = string.Empty;
        private string _new_category = string.Empty;
        private string _new_quantity = string.Empty;
        private string _new_price = string.Empty;

        public ObservableCollection<string> category_options { get; }
        public ObservableCollection<product> products_list { get; }
        public ObservableCollection<product> defile_list { get; }
        public ObservableCollection<product> oleos_list { get; }
        public ObservableCollection<product> rembrandt_list { get; }
        public ObservableCollection<product> bioline_list { get; }
        public ObservableCollection<product> amazonia_list { get; }
        public ObservableCollection<product> kedam_list { get; }
        public ObservableCollection<product> depil_list { get; }
        public ObservableCollection<product> estilista_list { get; }
        public ObservableCollection<product> otros_list { get; }

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
                filter_products();
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
            
            category_options = new ObservableCollection<string> { "Defile", "Óleos", "Rembrandt", "Bioline", "Amazonia Secret", "Kedam", "Depil Clear", "Estilista", "Otros" };
            
            products_list = new ObservableCollection<product>();
            defile_list = new ObservableCollection<product>();
            oleos_list = new ObservableCollection<product>();
            rembrandt_list = new ObservableCollection<product>();
            bioline_list = new ObservableCollection<product>();
            amazonia_list = new ObservableCollection<product>();
            kedam_list = new ObservableCollection<product>();
            depil_list = new ObservableCollection<product>();
            estilista_list = new ObservableCollection<product>();
            otros_list = new ObservableCollection<product>();

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
                IEnumerable<product> data = await _inventory_service.get_all_products_async();
                _all_products_source = data.ToList();
                filter_products();
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

        private void filter_products()
        {
            List<product> filtered = _all_products_source.Where(p =>
                string.IsNullOrWhiteSpace(_search_query) ||
                p.name.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                p.product_code.Contains(_search_query, StringComparison.OrdinalIgnoreCase)).ToList();

            products_list.Clear();
            defile_list.Clear();
            oleos_list.Clear();
            rembrandt_list.Clear();
            bioline_list.Clear();
            amazonia_list.Clear();
            kedam_list.Clear();
            depil_list.Clear();
            estilista_list.Clear();
            otros_list.Clear();

            foreach (product p in filtered)
            {
                products_list.Add(p);
                if (p.category == "Defile") defile_list.Add(p);
                else if (p.category == "Óleos") oleos_list.Add(p);
                else if (p.category == "Rembrandt") rembrandt_list.Add(p);
                else if (p.category == "Bioline") bioline_list.Add(p);
                else if (p.category == "Amazonia Secret") amazonia_list.Add(p);
                else if (p.category == "Kedam") kedam_list.Add(p);
                else if (p.category == "Depil Clear") depil_list.Add(p);
                else if (p.category == "Estilista") estilista_list.Add(p);
                else if (p.category == "Otros") otros_list.Add(p);
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
            if (parameter is product prod)
            {
                _product_being_edited = prod;
                new_code = prod.product_code;
                new_name = prod.name;
                new_category = prod.category;
                new_quantity = prod.stock_quantity.ToString();
                new_price = prod.unit_price_usd.ToString();
                on_request_add_window?.Invoke();
            }
        }

        private async void execute_delete_product(object? parameter)
        {
            if (parameter is product prod)
            {
                await _inventory_service.delete_product_async(prod);
                _all_products_source.Remove(prod);
                filter_products();
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
                _all_products_source.Add(new_prod);
            }
            
            _product_being_edited = null;
            filter_products();
            on_close_add_window?.Invoke();
        }
    }
}