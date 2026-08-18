using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Infrastructure.Repositories.Interfaces;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class billable_item
    {
        public int id_product { get; set; }
        public int id_promotion { get; set; }
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public decimal unit_price_usd { get; set; }
        public int available_stock { get; set; }
    }

    public class note_detail_row : ViewModelBase
    {
        private readonly List<billable_item> _all_items_ref;
        private billable_item? _selected_item;
        private string _item_search_text = string.Empty;
        private string _item_search_code = string.Empty;
        private int _quantity;
        private decimal _unit_price_usd;
        private string _promo_price_usd_text = string.Empty;
        private decimal _subtotal_usd;

        public ObservableCollection<billable_item> available_items { get; }

        public string item_search_text
        {
            get { return _item_search_text; }
            set
            {
                if (_item_search_text == value) return;
                _item_search_text = value;
                
                if (string.IsNullOrWhiteSpace(_item_search_text))
                {
                    _selected_item = null;
                    _item_search_code = string.Empty;
                    _unit_price_usd = 0;
                    _promo_price_usd_text = string.Empty;
                    on_property_changed(nameof(selected_item));
                    on_property_changed(nameof(item_search_code));
                    on_property_changed(nameof(unit_price_usd));
                    on_property_changed(nameof(promo_price_usd_text));
                }
                
                on_property_changed();
                filter_items();
                calculate_subtotal();
            }
        }

        public string item_search_code
        {
            get { return _item_search_code; }
            set
            {
                if (_item_search_code == value) return;
                _item_search_code = value;
                
                if (string.IsNullOrWhiteSpace(_item_search_code))
                {
                    _selected_item = null;
                    _item_search_text = string.Empty;
                    _unit_price_usd = 0;
                    _promo_price_usd_text = string.Empty;
                    on_property_changed(nameof(selected_item));
                    on_property_changed(nameof(item_search_text));
                    on_property_changed(nameof(unit_price_usd));
                    on_property_changed(nameof(promo_price_usd_text));
                    on_property_changed();
                    calculate_subtotal();
                    return;
                }

                on_property_changed();
                
                if (_selected_item != null && _selected_item.code.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                
                billable_item? found = _all_items_ref.FirstOrDefault(p => p.code.Equals(value, StringComparison.OrdinalIgnoreCase));
                if (found != null && _selected_item != found)
                {
                    selected_item = found;
                }
            }
        }

        public billable_item? selected_item
        {
            get { return _selected_item; }
            set
            {
                if (_selected_item == value) return;
                _selected_item = value;
                if (_selected_item != null)
                {
                    if (_selected_item.available_stock <= 0) throw new InvalidOperationException($"STOCK EN 0: El articulo {_selected_item.name} esta agotado.");
                    
                    unit_price_usd = _selected_item.unit_price_usd;
                    _item_search_code = _selected_item.code;
                    _item_search_text = _selected_item.name;

                    if (_quantity > _selected_item.available_stock) _quantity = _selected_item.available_stock;
                }
                on_property_changed();
                on_property_changed(nameof(item_search_code));
                on_property_changed(nameof(item_search_text));
                on_property_changed(nameof(quantity));
                calculate_subtotal();
            }
        }

        public int quantity
        {
            get { return _quantity; }
            set
            {
                if (value <= 0) throw new ArgumentException("La cantidad no puede ser 0 o menor.");
                if (_selected_item != null && value > _selected_item.available_stock) throw new InvalidOperationException($"Solo tienes {_selected_item.available_stock} unidades disponibles.");
                
                _quantity = value;
                on_property_changed();
                calculate_subtotal();
            }
        }

        public decimal unit_price_usd
        {
            get { return _unit_price_usd; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _unit_price_usd = value;
                on_property_changed();
                calculate_subtotal();
            }
        }

        public string promo_price_usd_text
        {
            get { return _promo_price_usd_text; }
            set
            {
                if (_promo_price_usd_text == value) return;
                
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string normalized = value.Replace(",", ".");
                    if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        throw new ArgumentException();
                    }
                }
                
                _promo_price_usd_text = value;
                on_property_changed();
                calculate_subtotal();
            }
        }

        public decimal subtotal_usd
        {
            get { return _subtotal_usd; }
            private set
            {
                _subtotal_usd = value;
                on_property_changed();
            }
        }

        public Action? on_subtotal_changed;

        public note_detail_row(IEnumerable<billable_item> items)
        {
            _all_items_ref = items.ToList();
            available_items = new ObservableCollection<billable_item>(_all_items_ref);
            _quantity = 1;
            _unit_price_usd = 0;
            _subtotal_usd = 0;
        }

        private void filter_items()
        {
            if (_selected_item != null && _selected_item.name == _item_search_text) 
            {
                return; 
            }

            available_items.Clear();
            if (string.IsNullOrWhiteSpace(_item_search_text))
            {
                foreach (billable_item p in _all_items_ref) available_items.Add(p);
            }
            else
            {
                string lower_search = _item_search_text.ToLower();
                IEnumerable<billable_item> filtered = _all_items_ref.Where(p => 
                    (p.name != null && p.name.ToLower().Contains(lower_search)) || 
                    (p.code != null && p.code.ToLower().Contains(lower_search))
                );
                foreach (billable_item p in filtered) available_items.Add(p);
            }
        }

        private void calculate_subtotal()
        {
            decimal effective_price = _unit_price_usd;
            
            string normalized_promo = _promo_price_usd_text.Replace(",", ".");
            if (decimal.TryParse(normalized_promo, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal promo_price) && promo_price > 0)
            {
                effective_price = promo_price;
            }

            subtotal_usd = _quantity * effective_price;
            on_subtotal_changed?.Invoke();
        }
    }

    public class DeliveryNotesViewModel : ViewModelBase
    {
        private readonly IDeliveryNoteService _delivery_note_service;
        private readonly ICustomerService _customer_service;
        private readonly IInventoryService _inventory_service;
        private readonly IGenericRepository<seller> _seller_repository;

        private List<customer> _all_customers_cache;
        private seller? _selected_seller;
        private customer? _selected_customer;
        private string _note_number = string.Empty;
        private DateTime _creation_date = DateTime.UtcNow;
        private DateTime _due_date = DateTime.UtcNow.AddDays(15);
        
        private decimal _gross_total_usd;
        private string _discount_percentage_text = "0";
        private decimal _discount_amount;
        private decimal _total_amount_usd;
        
        private string _conditions_text = "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO";
        private string _discount_conditions_text = "Descuento 10% SOLO\nCONTADO";

        public ObservableCollection<seller> sellers { get; }
        public ObservableCollection<customer> filtered_customers { get; }
        public ObservableCollection<billable_item> all_items { get; }
        public ObservableCollection<note_detail_row> note_details { get; }

        public ICommand add_item_command { get; }
        public ICommand remove_item_command { get; }
        public ICommand save_note_command { get; }

        public Action? OnNoteSaved;

        public seller? selected_seller
        {
            get { return _selected_seller; }
            set
            {
                if (_selected_seller == value) return;
                _selected_seller = value;
                on_property_changed();
                
                filtered_customers.Clear();
                selected_customer = null;

                if (_selected_seller != null)
                {
                    IEnumerable<customer> match = _all_customers_cache.Where(c => 
                        (!string.IsNullOrWhiteSpace(c.customer_code) && c.customer_code.StartsWith(_selected_seller.customer_code_prefix)) || 
                        c.seller_name == _selected_seller.full_name);

                    foreach (customer c in match)
                    {
                        filtered_customers.Add(c);
                    }
                }
                
                update_correlative_async();
            }
        }

        public customer? selected_customer
        {
            get { return _selected_customer; }
            set
            {
                if (_selected_customer == value) return;
                _selected_customer = value;
                on_property_changed();
            }
        }

        public string note_number
        {
            get { return _note_number; }
            private set
            {
                if (_note_number == value) return;
                _note_number = value;
                on_property_changed();
            }
        }

        public DateTime creation_date
        {
            get { return _creation_date; }
            set
            {
                if (_creation_date == value) return;
                _creation_date = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
                on_property_changed();
                
                if (_due_date.Date < _creation_date.Date)
                {
                    due_date = _creation_date;
                }
            }
        }

        public DateTime due_date
        {
            get { return _due_date; }
            set
            {
                if (_due_date == value) return;
                if (value.Date < _creation_date.Date) throw new ArgumentException();
                _due_date = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
                on_property_changed();
            }
        }

        public decimal gross_total_usd
        {
            get { return _gross_total_usd; }
            private set
            {
                if (_gross_total_usd == value) return;
                _gross_total_usd = value;
                on_property_changed();
            }
        }

        public string discount_percentage_text
        {
            get { return _discount_percentage_text; }
            set
            {
                if (_discount_percentage_text == value) return;
                
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string normalized = value.Replace(",", ".");
                    if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        throw new ArgumentException();
                    }
                }

                _discount_percentage_text = value;
                on_property_changed();
                recalculate_total();
            }
        }

        public decimal discount_amount
        {
            get { return _discount_amount; }
            private set
            {
                if (_discount_amount == value) return;
                _discount_amount = value;
                on_property_changed();
            }
        }

        public decimal total_amount_usd
        {
            get { return _total_amount_usd; }
            private set
            {
                if (_total_amount_usd == value) return;
                _total_amount_usd = value;
                on_property_changed();
            }
        }

        public string conditions_text
        {
            get { return _conditions_text; }
            set
            {
                if (_conditions_text == value) return;
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _conditions_text = value;
                on_property_changed();
            }
        }

        public string discount_conditions_text
        {
            get { return _discount_conditions_text; }
            set
            {
                if (_discount_conditions_text == value) return;
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException();
                _discount_conditions_text = value;
                on_property_changed();
            }
        }

        public DeliveryNotesViewModel(
            IDeliveryNoteService delivery_note_service,
            ICustomerService customer_service,
            IInventoryService inventory_service,
            IGenericRepository<seller> seller_repository)
        {
            if (delivery_note_service == null) throw new ArgumentNullException(nameof(delivery_note_service));
            if (customer_service == null) throw new ArgumentNullException(nameof(customer_service));
            if (inventory_service == null) throw new ArgumentNullException(nameof(inventory_service));
            if (seller_repository == null) throw new ArgumentNullException(nameof(seller_repository));

            _delivery_note_service = delivery_note_service;
            _customer_service = customer_service;
            _inventory_service = inventory_service;
            _seller_repository = seller_repository;

            _all_customers_cache = new List<customer>();
            sellers = new ObservableCollection<seller>();
            filtered_customers = new ObservableCollection<customer>();
            all_items = new ObservableCollection<billable_item>();
            note_details = new ObservableCollection<note_detail_row>();

            add_item_command = new RelayCommand(execute_add_item);
            remove_item_command = new RelayCommand(execute_remove_item);
            save_note_command = new RelayCommand(execute_save_note);

            load_initial_data_async();
        }

        private async void load_initial_data_async()
        {
            try
            {
                seller[] db_sellers = await _seller_repository.get_all_async();
                foreach (seller s in db_sellers) sellers.Add(s);

                IEnumerable<customer> db_customers = await _customer_service.GetAllCustomersAsync();
                foreach (customer c in db_customers) _all_customers_cache.Add(c);

                IEnumerable<promotion> db_promotions = await _inventory_service.get_all_promotions_async();
                foreach (promotion pr in db_promotions)
                {
                    if (pr.items == null || !pr.items.Any())
                    {
                        continue;
                    }

                    bool has_invalid_item = false;
                    foreach (var item in pr.items)
                    {
                        if (item.product == null || item.quantity_required <= 0)
                        {
                            has_invalid_item = true;
                            break;
                        }
                    }

                    if (has_invalid_item) continue;

                    int promo_stock = int.MaxValue;
                    foreach (var item in pr.items)
                    {
                        int max_combos = item.product!.stock_quantity / item.quantity_required;
                        if (max_combos < promo_stock) promo_stock = max_combos;
                    }

                    if (promo_stock <= 0) continue;

                    all_items.Add(new billable_item { 
                        id_promotion = pr.id_promotion, 
                        code = pr.promotion_code, 
                        name = pr.name, 
                        unit_price_usd = pr.unit_price_usd,
                        available_stock = promo_stock == int.MaxValue ? 0 : promo_stock
                    });
                }

                IEnumerable<product> db_products = await _inventory_service.get_all_products_async();
                foreach (product p in db_products)
                {
                    all_items.Add(new billable_item { 
                        id_product = p.id_product, 
                        code = p.product_code, 
                        name = p.name, 
                        unit_price_usd = p.unit_price_usd,
                        available_stock = p.stock_quantity
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void update_correlative_async()
        {
            if (_selected_seller == null)
            {
                note_number = string.Empty;
                return;
            }

            try
            {
                note_number = await _delivery_note_service.generate_correlative_async(_selected_seller.id_seller);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al generar correlativo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_add_item(object? parameter)
        {
            try
            {
                note_detail_row new_row = new note_detail_row(all_items);
                new_row.on_subtotal_changed = recalculate_total;
                note_details.Add(new_row);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al agregar item: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_remove_item(object? parameter)
        {
            if (parameter is note_detail_row row)
            {
                row.on_subtotal_changed = null;
                note_details.Remove(row);
                recalculate_total();
            }
        }

        private void recalculate_total()
        {
            decimal sum = 0;
            foreach (note_detail_row row in note_details)
            {
                sum += row.subtotal_usd;
            }
            gross_total_usd = sum;

            string normalized_discount = string.IsNullOrWhiteSpace(_discount_percentage_text) ? "0" : _discount_percentage_text.Replace(",", ".");
            if (decimal.TryParse(normalized_discount, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed_discount))
            {
                discount_amount = gross_total_usd * (parsed_discount / 100m);
            }
            else
            {
                discount_amount = 0;
            }

            total_amount_usd = gross_total_usd - discount_amount;
        }

        private async void execute_save_note(object? parameter)
        {
            try
            {
                if (_selected_seller == null) throw new InvalidOperationException("Debe seleccionar un vendedor.");
                if (_selected_customer == null) throw new InvalidOperationException("Debe seleccionar un cliente.");
                if (note_details.Count == 0) throw new InvalidOperationException("La nota no puede estar vacia. Agregue productos.");
                if (_due_date.Date < _creation_date.Date) throw new InvalidOperationException("La fecha de vencimiento es invalida.");
                if (string.IsNullOrWhiteSpace(_conditions_text)) throw new InvalidOperationException("Las condiciones no pueden estar vacias.");
                if (string.IsNullOrWhiteSpace(_discount_conditions_text)) throw new InvalidOperationException("Las condiciones de descuento no pueden estar vacias.");
                if (string.IsNullOrWhiteSpace(_discount_percentage_text)) throw new InvalidOperationException("El porcentaje de descuento no puede estar vacio.");

                delivery_note new_note = new delivery_note(
                    _note_number,
                    _creation_date,
                    _selected_seller.id_seller,
                    _selected_customer.id_customer,
                    _total_amount_usd,
                    "Pendiente"
                );

                List<note_detail> domain_details = new List<note_detail>();
                foreach (note_detail_row row in note_details)
                {
                    if (row.selected_item == null) throw new InvalidOperationException("Renglon invalido: Debe seleccionar un producto o promocion.");
                    if (row.quantity <= 0) throw new InvalidOperationException("La cantidad debe ser mayor a 0.");
                    
                    int? id_product = null;
                    int? id_promotion = null;
                    
                    if (row.selected_item.id_product > 0)
                    {
                        id_product = row.selected_item.id_product;
                    }
                    else if (row.selected_item.id_promotion > 0)
                    {
                        id_promotion = row.selected_item.id_promotion;
                    }
                    else
                    {
                        throw new InvalidOperationException("El item seleccionado no tiene producto ni promocion asociada.");
                    }
                    
                    domain_details.Add(new note_detail(
                        0,
                        id_product,
                        id_promotion,
                        row.quantity,
                        row.unit_price_usd,
                        row.subtotal_usd
                    ));
                }

                await _delivery_note_service.create_delivery_note_async(new_note, domain_details);

                note_details.Clear();
                discount_percentage_text = "0";
                recalculate_total();
                update_correlative_async();

                OnNoteSaved?.Invoke();

                System.Windows.MessageBox.Show("Nota guardada exitosamente", "Exito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string full_message = ex.Message;
                if (ex.InnerException != null)
                {
                    full_message += Environment.NewLine + "Detalle: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        full_message += Environment.NewLine + "Detalle 2: " + ex.InnerException.InnerException.Message;
                    }
                }
                System.Windows.MessageBox.Show($"Error al guardar nota: {full_message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}