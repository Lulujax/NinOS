using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
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
        private readonly ObservableCollection<note_detail_row>? _owner;
        private readonly DispatcherTimer _filter_debounce;
        private billable_item? _selected_item;
        private string _item_search_text = string.Empty;
        private string _item_search_code = string.Empty;
        private int _quantity;
        private decimal _unit_price_usd;
        private string _promo_price_usd_text = string.Empty;
        private decimal _subtotal_usd;
        private bool _show_search_popup_text;
        private bool _show_search_popup_code;
        private decimal _promo_price_unit;

        public ObservableCollection<billable_item> available_items { get; }

        public bool show_search_popup_text
        {
            get { return _show_search_popup_text; }
            set { _show_search_popup_text = value; on_property_changed(); }
        }

        public bool show_search_popup_code
        {
            get { return _show_search_popup_code; }
            set { _show_search_popup_code = value; on_property_changed(); }
        }

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

                _filter_debounce.Stop();
                _filter_debounce.Start();
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
                }

                _filter_debounce.Stop();
                _filter_debounce.Start();
            }
        }

        public billable_item? selected_item
        {
            get { return _selected_item; }
            set
            {
                if (_selected_item == value) return;
                _selected_item = value;
                show_search_popup_text = false;
                show_search_popup_code = false;
                if (_selected_item != null)
                {
                    if (RemainingStock(_selected_item) <= 0)
                    {
                        _selected_item = null;
                        on_property_changed(nameof(item_search_code));
                        on_property_changed(nameof(item_search_text));
                        return;
                    }

                    unit_price_usd = _selected_item.unit_price_usd;
                    _item_search_code = _selected_item.code;
                    _item_search_text = _selected_item.name;

                    int remaining = RemainingStock(_selected_item);
                    if (_quantity > remaining) _quantity = remaining;
                }
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
                int new_value = value < 0 ? 0 : value;
                if (_quantity == new_value) return;
                _quantity = new_value;
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

        public decimal promo_price_usd
        {
            get { return _promo_price_unit; }
        }

        public Action? on_subtotal_changed;

        public ICommand select_item_command { get; }
        public ICommand toggle_popup_command { get; }

        public note_detail_row(IEnumerable<billable_item> items, ObservableCollection<note_detail_row>? owner = null)
        {
            _all_items_ref = items.ToList();
            _owner = owner;
            available_items = new ObservableCollection<billable_item>();
            _quantity = 1;
            _unit_price_usd = 0;
            _subtotal_usd = 0;
            select_item_command = new RelayCommand(execute_select_item);
            toggle_popup_command = new RelayCommand(execute_toggle_popup);

            populate_available(null);

            _filter_debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _filter_debounce.Tick += (s, e) =>
            {
                _filter_debounce.Stop();
                filter_items();
                update_popups();
            };
        }

        private void execute_select_item(object? parameter)
        {
            if (parameter is billable_item item)
            {
                if (IsAlreadyInNote(item))
                {
                    System.Windows.MessageBox.Show($"El articulo '{item.name}' ya esta en la nota. Solo puedes colocarlo una vez.", "Item duplicado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                int remaining = RemainingStock(item);
                if (remaining <= 0)
                {
                    System.Windows.MessageBox.Show($"El articulo {item.name} esta agotado (stock 0).", "Sin stock", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                selected_item = item;
            }
        }

        private void execute_toggle_popup(object? parameter)
        {
            _filter_debounce.Stop();

            string kind = parameter as string ?? string.Empty;

            _item_search_text = string.Empty;
            _item_search_code = string.Empty;
            on_property_changed(nameof(item_search_text));
            on_property_changed(nameof(item_search_code));

            populate_available(null);

            bool has_items = available_items.Count > 0;
            if (!has_items)
            {
                show_search_popup_text = false;
                show_search_popup_code = false;
                return;
            }

            if (string.Equals(kind, "code", StringComparison.OrdinalIgnoreCase))
            {
                show_search_popup_code = !show_search_popup_code;
                show_search_popup_text = false;
            }
            else
            {
                show_search_popup_text = !show_search_popup_text;
                show_search_popup_code = false;
            }
        }

        private void update_popups()
        {
            bool has_items = available_items.Count > 0;
            bool text_filled = !string.IsNullOrWhiteSpace(_item_search_text);
            bool code_filled = !string.IsNullOrWhiteSpace(_item_search_code);

            show_search_popup_text = text_filled && has_items;
            show_search_popup_code = code_filled && has_items;
        }

        private void filter_items()
        {
            string? search_term = null;
            if (!string.IsNullOrWhiteSpace(_item_search_text))
            {
                search_term = _item_search_text;
            }
            else if (!string.IsNullOrWhiteSpace(_item_search_code))
            {
                search_term = _item_search_code;
            }

            if (search_term != null && _selected_item != null)
            {
                bool is_name_match = _selected_item.name != null && _selected_item.name.Equals(search_term, StringComparison.OrdinalIgnoreCase);
                bool is_code_match = _selected_item.code != null && _selected_item.code.Equals(search_term, StringComparison.OrdinalIgnoreCase);
                if (is_name_match || is_code_match)
                {
                    return;
                }
            }

            populate_available(search_term);
        }

        public bool SameItem(billable_item a, billable_item b)
        {
            if (a.id_product > 0 && b.id_product > 0) return a.id_product == b.id_product;
            if (a.id_promotion > 0 && b.id_promotion > 0) return a.id_promotion == b.id_promotion;
            return a.code == b.code;
        }

        private bool IsAlreadyInNote(billable_item item)
        {
            if (_owner == null) return false;
            foreach (note_detail_row r in _owner)
            {
                if (r == this) continue;
                if (r.selected_item != null && SameItem(r.selected_item, item)) return true;
            }
            return false;
        }

        private int UsedQuantityInOtherRows(billable_item item)
        {
            int used = 0;
            if (_owner == null) return used;
            foreach (note_detail_row r in _owner)
            {
                if (r == this) continue;
                if (r.selected_item != null && SameItem(r.selected_item, item)) used += r.quantity;
            }
            return used;
        }

        public int RemainingStock(billable_item item)
        {
            int remaining = item.available_stock - UsedQuantityInOtherRows(item);
            return remaining < 0 ? 0 : remaining;
        }

        private void populate_available(string? search_term)
        {
            available_items.Clear();
            foreach (billable_item p in _all_items_ref)
            {
                if (_owner != null && IsAlreadyInNote(p) && _selected_item == null) continue;

                bool matches = true;
                if (!string.IsNullOrWhiteSpace(search_term))
                {
                    bool name_match = p.name != null && p.name.Contains(search_term, StringComparison.OrdinalIgnoreCase);
                    bool code_match = p.code != null && p.code.Contains(search_term, StringComparison.OrdinalIgnoreCase);
                    matches = name_match || code_match;
                }
                if (!matches) continue;

                available_items.Add(p);
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

            _promo_price_unit = effective_price;

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
        private readonly IGenericRepository<note_type> _note_type_repository;
        private bool _is_loading;

        private List<customer> _all_customers_cache;
        private List<note_type> _all_note_types_cache;
        private seller? _selected_seller;
        private note_type? _selected_note_type;
        private customer? _selected_customer;
        private string _note_number = string.Empty;
        private DateTime _creation_date = DateTime.UtcNow;
        private DateTime _due_date = DateTime.UtcNow.AddDays(15);
        
        private decimal _gross_total_usd;
        private string _discount_percentage_text = "0";
        private decimal _discount_amount;
        private decimal _discounted_total_usd;
        private decimal _total_amount_usd;
        private string _header_title = "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE";
        private string _promo_discount_percentage_text = string.Empty;
        private decimal _promo_discount_amount;
        private string _volume_discount_percentage_text = string.Empty;
        private decimal _volume_discount_amount;
        
        private string _conditions_text = "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO";
        private string _discount_conditions_text = "Descuento 10% SOLO\nCONTADO";

        private string _credit_days_text = "21 dias de credito";
        private string _customer_code_text = string.Empty;
        private string _contact_name_text = string.Empty;

        public ObservableCollection<seller> sellers { get; }
        public ObservableCollection<note_type> note_type_options { get; }
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

                if (value != null && has_pending_data)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Hay datos sin guardar en la nota actual. Si cambias de vendedora se descartarán y el stock será liberado.\n\n¿Desea continuar?",
                        "Descartar cambios",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        on_property_changed(nameof(selected_seller));
                        return;
                    }

                    reset_unsaved_note();
                }

                ApplySeller(value);

                RefreshNoteTypeOptions();

                _selected_note_type = null;
                ApplyNoteType(null);
                on_property_changed(nameof(selected_note_type));
                on_property_changed(nameof(has_selection));
            }
        }

        public note_type? selected_note_type
        {
            get { return _selected_note_type; }
            set
            {
                if (_selected_note_type == value) return;

                if (value != null && has_pending_data)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Hay datos sin guardar en la nota actual. Si cambias el tipo de nota se descartarán y el stock será liberado.\n\n¿Desea continuar?",
                        "Descartar cambios",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        on_property_changed(nameof(selected_note_type));
                        return;
                    }

                    reset_unsaved_note();
                }

                ApplyNoteType(value);
                on_property_changed(nameof(has_selection));
            }
        }

        public bool has_selection
        {
            get { return _selected_seller != null && _selected_note_type != null; }
        }

        private void ApplyNoteType(note_type? nt)
        {
            _selected_note_type = nt;
            on_property_changed(nameof(selected_note_type));
            on_property_changed(nameof(has_promo_discount));
            on_property_changed(nameof(document_label));
            on_property_changed(nameof(is_pro_venta));
            on_property_changed(nameof(note_accent_color));
            on_property_changed(nameof(note_soft_color));
            on_property_changed(nameof(note_payment_bg));
            on_property_changed(nameof(note_guardar_color));

            if (nt == null)
            {
                header_title = "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE";
                conditions_text = "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO";
                discount_conditions_text = "Descuento 10% SOLO\nCONTADO";
                volume_discount_percentage_text = string.Empty;
                recalculate_total();
                return;
            }

            header_title = string.IsNullOrWhiteSpace(nt.header_title) ? "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE" : nt.header_title;
            if (!string.IsNullOrWhiteSpace(nt.conditions_template))
            {
                conditions_text = nt.conditions_template;
            }
            if (!string.IsNullOrWhiteSpace(nt.discount_conditions_template))
            {
                discount_conditions_text = nt.discount_conditions_template;
            }
            discount_percentage_text = CleanPercent(nt.default_discount_percentage);
            promo_discount_percentage_text = nt.promo_discount_percentage.HasValue ? CleanPercent(nt.promo_discount_percentage.Value) : string.Empty;
            volume_discount_percentage_text = string.Empty;
        }

        private static string CleanPercent(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public string document_label
        {
            get { return _selected_note_type != null && _selected_note_type.code == "MAR" ? "NOTA DE DESPACHO" : "NOTA DE ENTREGA"; }
        }

        public bool is_pro_venta
        {
            get { return _selected_note_type != null && _selected_note_type.code == "MAR"; }
        }

        public string note_accent_color
        {
            get { return is_pro_venta ? "#1565C0" : "#1B3A2D"; }
        }

        public string note_soft_color
        {
            get { return is_pro_venta ? "#E3F2FD" : "#F5F9F6"; }
        }

        public string note_payment_bg
        {
            get { return is_pro_venta ? "#BBDEFB" : "#DCE6C8"; }
        }

        public string note_guardar_color
        {
            get { return is_pro_venta ? "#2196F3" : "#4CAF50"; }
        }

        private void RefreshNoteTypeOptions()
        {
            note_type_options.Clear();
            if (_selected_seller == null) return;

            foreach (note_type nt in _all_note_types_cache
                .Where(t => t.is_active)
                .OrderBy(t => t.sort_order))
            {
                note_type_options.Add(nt);
            }
        }

        private void ApplySeller(seller? s)
        {
            _selected_seller = s;
            on_property_changed(nameof(selected_seller));
            on_property_changed(nameof(has_selection));

            filtered_customers.Clear();
            selected_customer = null;

            if (s != null)
            {
                IEnumerable<customer> match = _all_customers_cache.Where(c =>
                    (!string.IsNullOrWhiteSpace(c.customer_code) && c.customer_code.StartsWith(s.customer_code_prefix)) ||
                    c.seller_name == s.full_name);

                foreach (customer c in match)
                {
                    filtered_customers.Add(c);
                }
            }

            update_correlative_async();
        }

        public customer? selected_customer
        {
            get { return _selected_customer; }
            set
            {
                if (_selected_customer == value) return;
                _selected_customer = value;
                if (_selected_customer != null)
                {
                    customer_code_text = _selected_customer.customer_code;
                    contact_name_text = _selected_customer.contact_name;
                }
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

        public decimal discounted_total_usd
        {
            get { return _discounted_total_usd; }
            private set
            {
                if (_discounted_total_usd == value) return;
                _discounted_total_usd = value;
                on_property_changed();
            }
        }

        public string volume_discount_percentage_text
        {
            get { return _volume_discount_percentage_text; }
            set
            {
                if (_volume_discount_percentage_text == value) return;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    string normalized = value.Replace(",", ".");
                    if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) || parsed < 0 || parsed > 100)
                    {
                        throw new ArgumentException();
                    }
                }

                _volume_discount_percentage_text = value;
                on_property_changed();
                recalculate_total();
            }
        }

        public decimal volume_discount_amount
        {
            get { return _volume_discount_amount; }
            private set
            {
                if (_volume_discount_amount == value) return;
                _volume_discount_amount = value;
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

        public string header_title
        {
            get { return _header_title; }
            private set
            {
                if (_header_title == value) return;
                _header_title = value;
                on_property_changed();
            }
        }

        public string promo_discount_percentage_text
        {
            get { return _promo_discount_percentage_text; }
            set
            {
                if (_promo_discount_percentage_text == value) return;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    string normalized = value.Replace(",", ".");
                    if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) || parsed < 0 || parsed > 100)
                    {
                        throw new ArgumentException();
                    }
                }

                _promo_discount_percentage_text = value;
                on_property_changed();
                recalculate_total();
            }
        }

        public decimal promo_discount_amount
        {
            get { return _promo_discount_amount; }
            private set
            {
                if (_promo_discount_amount == value) return;
                _promo_discount_amount = value;
                on_property_changed();
            }
        }

        public bool has_promo_discount
        {
            get { return _selected_note_type != null && string.Equals(_selected_note_type.calculation_type, "promo", StringComparison.OrdinalIgnoreCase); }
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

        public string credit_days_text
        {
            get { return _credit_days_text; }
            set
            {
                if (_credit_days_text == value) return;
                _credit_days_text = value;
                on_property_changed();
            }
        }

        public string customer_code_text
        {
            get { return _customer_code_text; }
            set
            {
                if (_customer_code_text == value) return;
                _customer_code_text = value;
                on_property_changed();
            }
        }

        public string contact_name_text
        {
            get { return _contact_name_text; }
            set
            {
                if (_contact_name_text == value) return;
                _contact_name_text = value;
                on_property_changed();
            }
        }

        public DeliveryNotesViewModel(
            IDeliveryNoteService delivery_note_service,
            ICustomerService customer_service,
            IInventoryService inventory_service,
            IGenericRepository<seller> seller_repository,
            IGenericRepository<note_type> note_type_repository)
        {
            if (delivery_note_service == null) throw new ArgumentNullException(nameof(delivery_note_service));
            if (customer_service == null) throw new ArgumentNullException(nameof(customer_service));
            if (inventory_service == null) throw new ArgumentNullException(nameof(inventory_service));
            if (seller_repository == null) throw new ArgumentNullException(nameof(seller_repository));
            if (note_type_repository == null) throw new ArgumentNullException(nameof(note_type_repository));

            _delivery_note_service = delivery_note_service;
            _customer_service = customer_service;
            _inventory_service = inventory_service;
            _seller_repository = seller_repository;
            _note_type_repository = note_type_repository;

            _all_customers_cache = new List<customer>();
            _all_note_types_cache = new List<note_type>();
            sellers = new ObservableCollection<seller>();
            note_type_options = new ObservableCollection<note_type>();
            filtered_customers = new ObservableCollection<customer>();
            all_items = new ObservableCollection<billable_item>();
            note_details = new ObservableCollection<note_detail_row>();

            add_item_command = new RelayCommand(execute_add_item);
            remove_item_command = new RelayCommand(execute_remove_item);
            save_note_command = new RelayCommand(execute_save_note);

            load_initial_data_async();
        }

        public async void refresh_data()
        {
            if (_is_loading) return;
            _is_loading = true;
            try
            {
                var db_customers = await _customer_service.GetAllCustomersAsync();
                _all_customers_cache.Clear();
                foreach (customer c in db_customers) _all_customers_cache.Add(c);

                var db_sellers = await _seller_repository.get_all_async();
                sellers.Clear();
                foreach (seller s in db_sellers) sellers.Add(s);

                var db_note_types = await _note_type_repository.get_all_async();
                _all_note_types_cache.Clear();
                foreach (note_type nt in db_note_types) _all_note_types_cache.Add(nt);

                RefreshNoteTypeOptions();

                if (_selected_seller != null)
                {
                    filtered_customers.Clear();
                    IEnumerable<customer> match = _all_customers_cache.Where(c =>
                        (!string.IsNullOrWhiteSpace(c.customer_code) && c.customer_code.StartsWith(_selected_seller.customer_code_prefix)) ||
                        c.seller_name == _selected_seller.full_name);
                    foreach (customer c in match) filtered_customers.Add(c);
                }

                all_items.Clear();
                IEnumerable<promotion> db_promotions = await _inventory_service.get_all_promotions_async();
                foreach (promotion pr in db_promotions)
                {
                    if (pr.items == null || !pr.items.Any()) continue;
                    if (pr.items.Any(i => i.product == null || i.quantity_required <= 0)) continue;

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

                if (_selected_seller != null)
                {
                    seller? reconnected = sellers.FirstOrDefault(s => s.id_seller == _selected_seller.id_seller);
                    _selected_seller = reconnected ?? _selected_seller;
                    on_property_changed(nameof(selected_seller));
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al actualizar datos: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                _is_loading = false;
            }
        }

        private async void load_initial_data_async()
        {
            if (_is_loading) return;
            _is_loading = true;
            try
            {
                IEnumerable<customer> db_customers = await _customer_service.GetAllCustomersAsync();
                foreach (customer c in db_customers) _all_customers_cache.Add(c);

                seller[] db_sellers = await _seller_repository.get_all_async();
                foreach (seller s in db_sellers) sellers.Add(s);

                note_type[] db_note_types = await _note_type_repository.get_all_async();
                foreach (note_type nt in db_note_types) _all_note_types_cache.Add(nt);

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
            finally
            {
                _is_loading = false;
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

        private const int MAX_NOTE_ITEMS = 20;

        private void execute_add_item(object? parameter)
        {
            try
            {
                if (note_details.Count >= MAX_NOTE_ITEMS)
                {
                    System.Windows.MessageBox.Show($"La nota de entrega solo puede tener un maximo de {MAX_NOTE_ITEMS} items.", "Limite", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }
                note_detail_row new_row = new note_detail_row(all_items, note_details);
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

            decimal running = gross_total_usd;

            decimal promo_pct = 0;
            if (has_promo_discount)
            {
                string normalized_promo = string.IsNullOrWhiteSpace(_promo_discount_percentage_text) ? "0" : _promo_discount_percentage_text.Replace(",", ".");
                if (decimal.TryParse(normalized_promo, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed_promo))
                {
                    promo_pct = parsed_promo;
                }
            }
            promo_discount_amount = running * (promo_pct / 100m);
            running -= promo_discount_amount;

            string normalized_discount = string.IsNullOrWhiteSpace(_discount_percentage_text) ? "0" : _discount_percentage_text.Replace(",", ".");
            if (decimal.TryParse(normalized_discount, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed_discount))
            {
                discount_amount = running * (parsed_discount / 100m);
            }
            else
            {
                discount_amount = 0;
            }
            running -= discount_amount;
            discounted_total_usd = running;

            string normalized_volume = string.IsNullOrWhiteSpace(_volume_discount_percentage_text) ? "0" : _volume_discount_percentage_text.Replace(",", ".");
            if (decimal.TryParse(normalized_volume, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed_volume))
            {
                volume_discount_amount = running * (parsed_volume / 100m);
            }
            else
            {
                volume_discount_amount = 0;
            }
            running -= volume_discount_amount;

            total_amount_usd = running;
        }

        private note_print_dto build_preview_dto()
        {
            note_print_dto dto = new note_print_dto
            {
                note_number = _note_number,
                creation_date = _creation_date,
                due_date = _due_date,
                gross_total_usd = gross_total_usd,
                discount_percentage = string.IsNullOrWhiteSpace(_discount_percentage_text) ? 0 : (decimal.TryParse(_discount_percentage_text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dp) ? dp : 0),
                discount_amount = discount_amount,
                total_amount_usd = total_amount_usd,
                seller_name = _selected_seller?.full_name ?? string.Empty,
                customer_code = customer_code_text,
                customer_business_name = _selected_customer?.business_name ?? string.Empty,
                customer_rif = _selected_customer?.rif ?? string.Empty,
                customer_phone = _selected_customer?.phone_number ?? string.Empty,
                customer_contact = _contact_name_text,
                credit_days_text = _credit_days_text,
                customer_delivery_address = _selected_customer?.effective_delivery_address ?? string.Empty,
                fiscal_address = _selected_customer?.fiscal_address ?? string.Empty,
                conditions_text = _conditions_text,
                discount_conditions_text = _discount_conditions_text,
                company_name = "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE",
                header_title = string.IsNullOrWhiteSpace(_header_title) ? "DEFILE_REMBRANT_OLEOS_FLYING_BIOLINE" : _header_title,
                promo_discount_percentage = has_promo_discount && decimal.TryParse(string.IsNullOrWhiteSpace(_promo_discount_percentage_text) ? "0" : _promo_discount_percentage_text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed_promo) ? parsed_promo : null,
                promo_discount_amount = promo_discount_amount,
                volume_discount_percentage = string.IsNullOrWhiteSpace(_volume_discount_percentage_text) ? 0 : (decimal.TryParse(_volume_discount_percentage_text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vp) ? vp : 0),
                volume_discount_amount = volume_discount_amount,
                discounted_total_usd = discounted_total_usd,
                document_label = document_label,
                accent_color = _selected_note_type != null && _selected_note_type.code == "MAR" ? "#1565C0" : "#1B3A2D",
                accent_soft_color = _selected_note_type != null && _selected_note_type.code == "MAR" ? "#E3F2FD" : "#F0F4EC"
            };

            foreach (note_detail_row row in note_details)
            {
                if (row.selected_item == null) continue;
                dto.details.Add(new note_detail_print_dto
                {
                    code = row.selected_item.code,
                    name = row.selected_item.name,
                    quantity = row.quantity,
                    unit_price_usd = row.unit_price_usd,
                    promo_price_usd = row.promo_price_usd,
                    subtotal_usd = row.subtotal_usd
                });
            }

            return dto;
        }

        private async void execute_save_note(object? parameter)
        {
            try
            {
                if (_selected_seller == null) throw new InvalidOperationException("Tienes que llenar los campos obligatorios.");
                if (_selected_customer == null) throw new InvalidOperationException("Tienes que llenar los campos obligatorios.");
                if (note_details.Count == 0) throw new InvalidOperationException("Tienes que llenar los campos obligatorios.");
                if (note_details.Count > MAX_NOTE_ITEMS) throw new InvalidOperationException($"La nota de entrega no puede tener mas de {MAX_NOTE_ITEMS} items.");
                if (_due_date.Date < _creation_date.Date) throw new InvalidOperationException("La fecha de vencimiento es invalida.");
                if (string.IsNullOrWhiteSpace(_conditions_text)) throw new InvalidOperationException("Tienes que llenar los campos obligatorios.");
                if (string.IsNullOrWhiteSpace(_discount_conditions_text)) throw new InvalidOperationException("Tienes que llenar los campos obligatorios.");
                decimal validated_discount = decimal.TryParse(string.IsNullOrWhiteSpace(_discount_percentage_text) ? "0" : _discount_percentage_text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed_discount)
                    ? parsed_discount
                    : -1;
                if (validated_discount < 0 || validated_discount > 100)
                    throw new InvalidOperationException("El porcentaje de descuento debe estar entre 0 y 100.");

                for (int i = 0; i < note_details.Count; i++)
                {
                    note_detail_row row = note_details[i];
                    if (row.selected_item == null) throw new InvalidOperationException("Hay un renglon sin producto seleccionado.");
                    if (row.quantity <= 0) throw new InvalidOperationException($"La cantidad de '{row.selected_item.name}' debe ser mayor a 0.");

                    for (int j = 0; j < note_details.Count; j++)
                    {
                        if (i == j) continue;
                        if (note_details[j].selected_item != null && row.SameItem(row.selected_item, note_details[j].selected_item))
                        {
                            throw new InvalidOperationException($"El articulo '{row.selected_item.name}' esta repetido en la nota. Cada producto solo puede aparecer una vez.");
                        }
                    }

                    int available = row.RemainingStock(row.selected_item);
                    if (row.quantity > available)
                    {
                        throw new InvalidOperationException(
                            $"Inventario insuficiente para '{row.selected_item.name}'. Tienes {available} unidades disponibles y colocaste {row.quantity}.");
                    }
                }

                note_print_dto preview = build_preview_dto();
                NinOS.UI.Views.NotePreviewWindow preview_window = new NinOS.UI.Views.NotePreviewWindow(preview) { Owner = System.Windows.Application.Current?.MainWindow };
                preview_window.ShowDialog();

                if (!preview_window.Confirmed) return;
                bool generate_pdf = preview_window.PdfRequested;

                delivery_note new_note = new delivery_note(
                    _note_number,
                    _creation_date,
                    _selected_seller.id_seller,
                    _selected_customer.id_customer,
                    _total_amount_usd,
                    "Pendiente",
                    _total_amount_usd
                );
                new_note.discount_percentage = string.IsNullOrWhiteSpace(_discount_percentage_text) ? null
                    : (decimal.TryParse(_discount_percentage_text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dp) ? dp : null);
                new_note.note_type_id = _selected_note_type?.id_note_type;
                new_note.promo_discount_percentage = has_promo_discount && decimal.TryParse(string.IsNullOrWhiteSpace(_promo_discount_percentage_text) ? "0" : _promo_discount_percentage_text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pp) ? pp : null;
                new_note.volume_discount_percentage = decimal.TryParse(string.IsNullOrWhiteSpace(_volume_discount_percentage_text) ? "0" : _volume_discount_percentage_text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vp) ? vp : null;

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
                promo_discount_percentage_text = (has_promo_discount && _selected_note_type?.promo_discount_percentage != null)
                    ? CleanPercent(_selected_note_type.promo_discount_percentage.Value)
                    : string.Empty;
                volume_discount_percentage_text = string.Empty;
                recalculate_total();
                update_correlative_async();
                OnNoteSaved?.Invoke();

                if (generate_pdf)
                    NinOS.UI.Common.NotePdfGenerator.generate(preview);

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

        public bool has_pending_data
        {
            get
            {
                if (note_details.Count > 0) return true;
                if (!string.IsNullOrWhiteSpace(customer_code_text)) return true;
                return false;
            }
        }

        public void reset_unsaved_note()
        {
            foreach (note_detail_row row in note_details)
            {
                row.on_subtotal_changed = null;
            }
            note_details.Clear();
            discount_percentage_text = "0";
            promo_discount_percentage_text = (has_promo_discount && _selected_note_type?.promo_discount_percentage != null)
                ? CleanPercent(_selected_note_type.promo_discount_percentage.Value)
                : string.Empty;
            volume_discount_percentage_text = string.Empty;
            customer_code_text = string.Empty;
            contact_name_text = string.Empty;
            _selected_customer = null;
            on_property_changed(nameof(selected_customer));
            on_property_changed(nameof(customer_code_text));
            recalculate_total();
            update_correlative_async();
        }
    }
}