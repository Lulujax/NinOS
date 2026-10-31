using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Logging;
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

        private decimal _item_price;
        public decimal item_price 
        { 
            get => _item_price;
            set
            {
                if (_item_price != value)
                {
                    _item_price = value;
                    on_property_changed();
                    on_property_changed(nameof(item_price_display));
                }
            }
        }

        public string item_price_display => $"$ {item_price.ToString("N2", CultureInfo.InvariantCulture)}";

        private bool _has_custom_price;
        public bool has_custom_price
        {
            get => _has_custom_price;
            set
            {
                if (_has_custom_price != value)
                {
                    _has_custom_price = value;
                    on_property_changed();
                    on_property_changed(nameof(can_delete_row));
                    on_property_changed(nameof(delete_tooltip));
                    on_property_changed(nameof(price_origin_tooltip));
                }
            }
        }

        private bool _is_custom_list;
        public bool is_custom_list
        {
            get => _is_custom_list;
            set
            {
                if (_is_custom_list != value)
                {
                    _is_custom_list = value;
                    on_property_changed();
                    on_property_changed(nameof(can_delete_row));
                    on_property_changed(nameof(delete_tooltip));
                    on_property_changed(nameof(price_origin_tooltip));
                }
            }
        }

        public bool can_delete_row
        {
            get
            {
                if (is_promotion) return true;
                if (!is_custom_list) return true;
                return has_custom_price;
            }
        }

        public string delete_tooltip
        {
            get
            {
                if (is_promotion) return "Eliminar promoción";
                if (!is_custom_list) return "Eliminar producto del inventario";
                return has_custom_price
                    ? "Quitar precio propio y volver a heredar de la lista Principal"
                    : "Este producto ya hereda el precio de la lista Principal";
            }
        }

        public string price_origin_tooltip
        {
            get
            {
                if (is_promotion) return "Precio de promoción";
                if (!is_custom_list) return "Precio base del inventario (Lista Principal)";
                return has_custom_price
                    ? "Precio propio configurado para esta lista"
                    : "Heredado de la lista Principal";
            }
        }

        public bool is_promotion { get; set; }
        public bool is_default_combo { get; set; }
        public product? product_ref { get; set; }
        public promotion? promo_ref { get; set; }
        public string promo_type_display { get; set; } = string.Empty;
        public bool is_negative_stock { get; set; }
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

    public class price_list_selection_option : ViewModelBase
    {
        private bool _is_checked;
        public int id_price_list { get; set; }
        public string name { get; set; } = string.Empty;

        public bool is_checked
        {
            get => _is_checked;
            set
            {
                if (_is_checked == value) return;
                _is_checked = value;
                on_property_changed();
            }
        }
    }

    public abstract class InventoryTabItemBase : ViewModelBase
    {
        private string _header = string.Empty;
        public string Header
        {
            get => _header;
            set
            {
                if (_header != value)
                {
                    _header = value;
                    on_property_changed();
                }
            }
        }

        public ObservableCollection<inventory_item_dto> Items { get; } = new ObservableCollection<inventory_item_dto>();
    }

    public class TodosTabItem : InventoryTabItemBase
    {
        public TodosTabItem()
        {
            Header = "Todos";
        }
    }

    public class BrandTabItem : InventoryTabItemBase
    {
        public int? IdProductLine { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string CodePrefix { get; set; } = string.Empty;

        public BrandTabItem(string brandName, string codePrefix = "", int? idProductLine = null)
        {
            BrandName = brandName;
            CodePrefix = codePrefix;
            IdProductLine = idProductLine;
            Header = brandName;
        }
    }

    public class PromocionesTabItem : InventoryTabItemBase
    {
        public PromocionesTabItem()
        {
            Header = "Promociones";
        }
    }



    public class InventoryViewModel : ViewModelBase
    {
        // Codigos de promocion con el mismo formato correlativo de los productos: prefijo + 5 digitos.
        public const string promo_prefix_oferta = "OF";
        public const string promo_prefix_kit = "KIT";
        public const string promo_prefix_combo = "COM";
        private const int promo_code_digits = 5;

        private static readonly string[] DefaultProductLines = new[]
        {
            "DEFILE", "OLEOS", "REMBRANDT", "BIOLINE", "AMAZONIA SECRET", "KEDAM", "DEPIL CLEAR", "ESTILISTA", "CUTIQUE", "OTROS"
        };

        private readonly IInventoryService _inventory_service;
        private readonly IProductLineService? _product_line_service;
        private readonly IPriceListService? _price_list_service;
        public ObservableCollection<price_list> price_list_options { get; } = new ObservableCollection<price_list>();
        private price_list? _selected_price_list;
        public price_list? selected_price_list
        {
            get => _selected_price_list;
            set
            {
                if (_selected_price_list != value)
                {
                    _selected_price_list = value;
                    on_property_changed();
                    on_property_changed(nameof(is_principal_list_selected));
                    on_property_changed(nameof(is_custom_list_selected));
                    on_property_changed(nameof(price_field_label));
                    on_property_changed(nameof(price_field_hint));
                    on_property_changed(nameof(is_add_product_button_visible));
                    update_displayed_prices();
                }
            }
        }

        public bool is_principal_list_selected => _selected_price_list == null || _selected_price_list.is_principal;
        public bool is_custom_list_selected => !is_principal_list_selected;
        public bool is_add_product_button_visible => true;

        public string price_field_label => is_principal_list_selected || selected_price_list == null
            ? "Precio ($):"
            : $"Precio ({selected_price_list.name}) ($):";

        public string price_field_hint => selected_price_list != null && !selected_price_list.is_principal
            ? $"Se asignará a la lista \"{selected_price_list.name}\" y como base del inventario."
            : string.Empty;
        public Func<string, string, string, string?>? prompt_price_edit;
        private Dictionary<int, Dictionary<int, decimal>> _all_own_prices = new Dictionary<int, Dictionary<int, decimal>>();
        private List<product> _all_products_source;
        private List<promotion> _all_promotions_source;
        private HashSet<string> _trashed_promotion_codes = new(StringComparer.Ordinal);
        private product? _product_being_edited;
        private promotion? _promotion_being_edited;
        private string _error_message = string.Empty;
        private bool _is_loading = false;

        private string _search_query = string.Empty;
        private int _selected_tab_index = 0;
        private InventoryTabItemBase? _selected_tab;
        private string _new_code = string.Empty;
        private string _new_name = string.Empty;
        private string _new_category = string.Empty;
        private string _new_quantity = string.Empty;
        private string _new_price = string.Empty;
        private string _add_button_text = "+ AÑADIR PRODUCTO";

        private int _promo_type_index = 0;
        private string _promo_search_query = string.Empty;
        private string _new_promo_name = string.Empty;
        private string _new_promo_price = string.Empty;
        private bool _all_brands_selected = true;
        private bool _suppress_brand_sync;

        // Stock que tenia el producto al abrirlo para edicion. Sirve para saber si la
        // cantidad cambio (y por tanto pedir el motivo del ajuste) y cuanto sumo o resto.
        private int _original_quantity_for_edit;
        private int _code_request_generation;
        private int _adjustment_reason_index = -1;
        private string _original_code_for_edit = string.Empty;
        private string _original_brand_for_edit = string.Empty;
        private string _preview_code_for_new_brand = string.Empty;

        private bool _is_stock_addition_mode = false;
        private bool _is_stock_removal_mode = false;
        private string _stock_addition_quantity = string.Empty;
        private string _stock_addition_reason = string.Empty;
        private int _stock_addition_current_quantity = 0;

        public ObservableCollection<InventoryTabItemBase> inventory_tabs { get; } = new ObservableCollection<InventoryTabItemBase>();
        public ObservableCollection<string> category_options { get; }
        public ObservableCollection<brand_selection_option> price_list_brand_options { get; }
        public ObservableCollection<price_list_selection_option> price_list_selection_options { get; } = new ObservableCollection<price_list_selection_option>();

        private string _principal_price_list_name = "Principal";
        public string principal_price_list_name => _principal_price_list_name;

        private bool _is_principal_included_in_report = true;
        public bool is_principal_included_in_report
        {
            get => _is_principal_included_in_report;
            set
            {
                if (_is_principal_included_in_report != value)
                {
                    _is_principal_included_in_report = value;
                    on_property_changed();
                }
            }
        }
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
        public ICommand edit_lines_command { get; }
        public ICommand edit_price_lists_command { get; }
        public ICommand save_product_command { get; }
        public ICommand edit_command { get; }
        public ICommand delete_command { get; }
        public ICommand delete_promotion_command { get; }
        public ICommand save_promotion_command { get; }
        public ICommand add_to_builder_command { get; }
        public ICommand remove_from_builder_command { get; }
        public ICommand edit_promotion_command { get; }
        public ICommand generate_price_list_command { get; }
        public ICommand generate_price_list_excel_command { get; }
        public ICommand print_current_inventory_command { get; }
        public ICommand print_all_movements_command { get; }
        public ICommand clear_price_list_command { get; }
        public ICommand open_stock_section_command { get; }
        public ICommand close_stock_section_command { get; }
        
        public Action? on_request_add_window;
        public Action? on_request_add_promotion_window;
        public Action? on_request_edit_lines_window;
        public Action? on_request_edit_price_lists_window;
        public Action? on_close_add_window;
        public Action? on_close_add_promotion_window;

        // La marca se puede cambiar al crear y al editar. Al editar el sistema reasigna el
        // codigo con la serie de la marca destino, asi que no se rompe el estandar.
        public bool can_edit_category => true;

        public bool is_editing_product => _product_being_edited != null;

        public bool is_stock_addition_mode
        {
            get { return _is_stock_addition_mode; }
            set
            {
                if (_is_stock_addition_mode == value) return;
                _is_stock_addition_mode = value;
                on_property_changed();
                on_property_changed(nameof(code_is_locked));
                on_property_changed(nameof(name_is_locked));
            }
        }

        public bool is_stock_removal_mode
        {
            get { return _is_stock_removal_mode; }
            set
            {
                if (_is_stock_removal_mode == value) return;
                _is_stock_removal_mode = value;
                on_property_changed();
                notify_stock_total_changed();
            }
        }

        public string stock_addition_quantity
        {
            get { return _stock_addition_quantity; }
            set
            {
                if (_stock_addition_quantity == value) return;
                _stock_addition_quantity = value;
                on_property_changed();
                notify_stock_total_changed();
            }
        }

        public string stock_addition_reason
        {
            get { return _stock_addition_reason; }
            set { _stock_addition_reason = value; on_property_changed(); }
        }

        public int stock_addition_current_quantity
        {
            get { return _stock_addition_current_quantity; }
            set
            {
                if (_stock_addition_current_quantity == value) return;
                _stock_addition_current_quantity = value;
                on_property_changed();
                notify_stock_total_changed();
            }
        }

        private int stock_addition_quantity_value
        {
            get
            {
                return int.TryParse(stock_addition_quantity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty)
                    ? (qty < 0 ? 0 : qty)
                    : 0;
            }
        }

        // Quitar mas de lo que hay no da un total de 0: es un error. Si se mostrara 0 en
        // verde la clienta creeria que el ajuste es valido y el guardado la frenaria
        // despues, sin relacion aparente con lo que acaba de leer en pantalla.
        public bool stock_addition_exceeds_stock =>
            is_stock_removal_mode && stock_addition_quantity_value > stock_addition_current_quantity;

        public int stock_addition_total_after
        {
            get
            {
                int qty = stock_addition_quantity_value;
                if (is_stock_removal_mode)
                {
                    int result = stock_addition_current_quantity - qty;
                    return result < 0 ? 0 : result;
                }
                return stock_addition_current_quantity + qty;
            }
        }

        // Lo que se pinta: el total real, o un guion cuando el ajuste no es posible.
        public string stock_addition_total_display =>
            stock_addition_exceeds_stock ? "—" : stock_addition_total_after.ToString();

        private void notify_stock_total_changed()
        {
            on_property_changed(nameof(stock_addition_total_after));
            on_property_changed(nameof(stock_addition_total_display));
            on_property_changed(nameof(stock_addition_exceeds_stock));
        }

        // Mientras el apartado de agregar/quitar stock esta desplegado, el codigo y el
        // nombre quedan bloqueados: el ajuste es de existencias, no de identidad del
        // producto, y cambiar el codigo a medias dejaria el kardex apuntando a otro.
        public bool code_is_locked => is_stock_addition_mode;

        public bool name_is_locked => is_stock_addition_mode;

        // La cantidad inicial solo se escribe al crear: al editar, el stock se mueve
        // unicamente por el apartado de agregar/quitar.
        public bool show_initial_quantity_box => !is_editing_product;

        // Codigo que tenia el producto al abrirlo para editar, y el que le asignaria el
        // sistema si se confirma el cambio de marca. Solo se muestran si hay cambio.
        public string original_code_for_edit => is_editing_product ? _original_code_for_edit : string.Empty;

        public string preview_code_for_new_brand => _preview_code_for_new_brand;

        public bool has_pending_brand_change => is_editing_product
            && _original_brand_for_edit.Length > 0
            && !string.Equals(new_category, _original_brand_for_edit, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(_preview_code_for_new_brand);

        // Texto de la linea roja de aviso: "DEF30104 → OLE30319".
        public string brand_change_warning_display => has_pending_brand_change
            ? $"El codigo cambiara de {original_code_for_edit} a {preview_code_for_new_brand}."
            : string.Empty;

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

        public InventoryTabItemBase? selected_tab
        {
            get => _selected_tab;
            set
            {
                if (_selected_tab != value)
                {
                    _selected_tab = value;
                    on_property_changed();
                    int idx = value != null ? inventory_tabs.IndexOf(value) : -1;
                    if (idx >= 0 && _selected_tab_index != idx)
                    {
                        _selected_tab_index = idx;
                        on_property_changed(nameof(selected_tab_index));
                    }
                    update_category_from_tab();
                    on_property_changed(nameof(can_edit_category));


                }
            }
        }

        public int selected_tab_index
        {
            get { return _selected_tab_index; }
            set 
            { 
                if (_selected_tab_index != value)
                {
                    _selected_tab_index = value; 
                    on_property_changed(); 
                    if (value >= 0 && value < inventory_tabs.Count)
                    {
                        selected_tab = inventory_tabs[value];
                    }
                    else
                    {
                        update_category_from_tab(); 
                        on_property_changed(nameof(can_edit_category)); 
                    }
                }
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
                on_property_changed(nameof(can_edit_category));

                if (_product_being_edited == null)
                {
                    request_next_code_async(value);
                }
                else
                {
                    request_brand_change_preview_async(value);
                }
            }
        }

        public string new_quantity
        {
            get { return _new_quantity; }
            set
            {
                if (_new_quantity == value) return;
                _new_quantity = value;
                on_property_changed();
            }
        }

        // Motivos de ajuste de cantidad, tomados del dominio para que el kardex y el
        // formulario no se desincronicen.
        public IReadOnlyList<string> adjustment_reason_options => stock_movement.AdjustmentReasons;

        public string[] adjustment_reason_display_options { get; }

        public static string adjustment_reason_display(string reason)
        {
            switch (reason)
            {
                case stock_movement.RazonReposicion: return "Reposicion / compra a proveedor";
                case stock_movement.RazonMerma: return "Merma, rotura o producto vencido";
                case stock_movement.RazonCorreccionConteo: return "Correccion de conteo fisico";
                case stock_movement.RazonDevolucionProveedor: return "Devolucion a proveedor";
                case stock_movement.RazonAjusteSistema: return "Ajuste del sistema";
                default: return reason;
            }
        }

        public int adjustment_reason_index
        {
            get { return _adjustment_reason_index; }
            set
            {
                if (_adjustment_reason_index == value) return;
                _adjustment_reason_index = value;
                on_property_changed();
                on_property_changed(nameof(adjustment_reason_selected));
            }
        }

        public string? adjustment_reason_selected =>
            _adjustment_reason_index >= 0 && _adjustment_reason_index < adjustment_reason_options.Count
                ? adjustment_reason_options[_adjustment_reason_index]
                : null;

        public string new_price
        {
            get { return _new_price; }
            set
            {
                _new_price = value;
                on_property_changed();
            }
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

        public InventoryViewModel(IInventoryService inventory_service, IProductLineService? product_line_service = null)
        {
            if (inventory_service == null) throw new ArgumentNullException(nameof(inventory_service));
            _inventory_service = inventory_service;
            _product_line_service = product_line_service
                ?? ((Application.Current as App)?.GetServiceProvider()?.GetService(typeof(IProductLineService)) as IProductLineService);
            _price_list_service = (Application.Current as App)?.GetServiceProvider()?.GetService(typeof(IPriceListService)) as IPriceListService;

            _all_products_source = new List<product>();
            _all_promotions_source = new List<promotion>();

            // En mayusculas porque es como queda guardada la categoria en product. Si aqui
            // estuvieran en mayusculas distintas, al editar el ComboBox no encontraria
            // coincidencia con el valor que viene de la base y se veria vacio.
            category_options = new ObservableCollection<string>(DefaultProductLines);
            
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

            adjustment_reason_display_options = stock_movement.AdjustmentReasons
                .Select(adjustment_reason_display)
                .ToArray();

            open_add_window_command = new RelayCommand(execute_open_add_window);
            edit_lines_command = new RelayCommand(execute_edit_lines);
            edit_price_lists_command = new RelayCommand(execute_edit_price_lists);

            save_product_command = new RelayCommand(execute_save_product);
            edit_command = new RelayCommand(execute_edit_product);
            delete_command = new RelayCommand(execute_delete_product);
            delete_promotion_command = new RelayCommand(execute_delete_promotion);
            save_promotion_command = new RelayCommand(execute_save_promotion);
            add_to_builder_command = new RelayCommand(execute_add_to_builder);
            remove_from_builder_command = new RelayCommand(execute_remove_from_builder);
            edit_promotion_command = new RelayCommand(execute_edit_promotion);
            generate_price_list_command = new RelayCommand(execute_generate_price_list);
            generate_price_list_excel_command = new RelayCommand(execute_generate_price_list_excel);
            print_current_inventory_command = new RelayCommand(execute_print_current_inventory);
            print_all_movements_command = new RelayCommand(execute_print_all_movements);
            clear_price_list_command = new RelayCommand(execute_clear_price_list);
            open_stock_section_command = new RelayCommand(execute_open_stock_section);
            close_stock_section_command = new RelayCommand(execute_close_stock_section);
            
            new_category = "DEFILE";

            // Inicializar pestañas con valores predeterminados de inmediato
            sync_tabs(new List<product_line>());

            AppDataEvents.CatalogsChanged += OnCatalogsChanged;

            load_initial_data_async();
        }

        private void OnCatalogsChanged()
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.InvokeAsync(refresh_data);
            }
            else
            {
                refresh_data();
            }
        }

        private async void load_price_list_selections_async()
        {
            if (_price_list_service == null) return;
            try
            {
                var principal = await _price_list_service.GetPrincipalAsync();
                var listas = await _price_list_service.GetActiveAsync();
                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _principal_price_list_name = principal.name;
                        on_property_changed(nameof(principal_price_list_name));
                        fill_price_list_selections(listas);
                    });
                }
                else
                {
                    _principal_price_list_name = principal.name;
                    on_property_changed(nameof(principal_price_list_name));
                    fill_price_list_selections(listas);
                }
            }
            catch (Exception ex)
            {
                AppLog.Warn($"No se pudieron cargar las listas de precios para el PDF: {ex.Message}");
            }
        }

        private void fill_price_list_selections(List<Domain.price_list> listas)
        {
            price_list_selection_options.Clear();
            foreach (var lista in listas.OrderBy(x => x.sort_order).ThenBy(x => x.name))
            {
                price_list_selection_options.Add(new price_list_selection_option
                {
                    id_price_list = lista.id_price_list,
                    name = lista.name,
                    is_checked = false
                });
            }
        }

        private void execute_edit_lines(object? parameter)
        {
            on_request_edit_lines_window?.Invoke();
        }

        private void execute_edit_price_lists(object? parameter)
        {
            on_request_edit_price_lists_window?.Invoke();
        }

        public void sync_tabs(List<product_line> active_lines)
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => sync_tabs(active_lines));
                return;
            }

            string? previouslySelectedBrand = (_selected_tab as BrandTabItem)?.BrandName;
            bool wasPromocionesSelected = _selected_tab is PromocionesTabItem;
            bool wasTodosSelected = _selected_tab is TodosTabItem;


            // 1. Asegurar pestaña "Todos" en posición 0
            TodosTabItem? todosTab = inventory_tabs.OfType<TodosTabItem>().FirstOrDefault();
            if (todosTab == null)
            {
                todosTab = new TodosTabItem();
                inventory_tabs.Insert(0, todosTab);
            }

            // 2. Líneas ordenadas
            List<product_line> orderedLines;
            if (active_lines != null && active_lines.Count > 0)
            {
                orderedLines = active_lines.OrderBy(l => l.sort_order).ThenBy(l => l.name).ToList();
            }
            else
            {
                orderedLines = DefaultProductLines.Select((name, idx) => new product_line
                {
                    name = name,
                    code_prefix = product_code_rules.TryGetPrefix(name, out var p) ? p : "OTR",
                    sort_order = idx,
                    is_active = true
                }).ToList();
            }

            // 3. Eliminar pestañas de marcas que ya no estén activas
            var activeNames = new HashSet<string>(orderedLines.Select(l => l.name), StringComparer.OrdinalIgnoreCase);
            for (int i = inventory_tabs.Count - 1; i >= 0; i--)
            {
                if (inventory_tabs[i] is BrandTabItem bTab && !activeNames.Contains(bTab.BrandName))
                {
                    inventory_tabs.RemoveAt(i);
                }
            }

            // 4. Asegurar pestaña Promociones
            PromocionesTabItem? promoTab = inventory_tabs.OfType<PromocionesTabItem>().FirstOrDefault();
            if (promoTab == null)
            {
                promoTab = new PromocionesTabItem();
            }
            else
            {
                inventory_tabs.Remove(promoTab);
            }

            // 5. Agregar o actualizar pestañas de marcas en orden
            for (int i = 0; i < orderedLines.Count; i++)
            {
                var line = orderedLines[i];
                int targetIndex = i + 1; // 0 es Todos
                var existingTab = inventory_tabs.OfType<BrandTabItem>()
                    .FirstOrDefault(t => string.Equals(t.BrandName, line.name, StringComparison.OrdinalIgnoreCase));

                if (existingTab != null)
                {
                    existingTab.Header = line.name;
                    existingTab.BrandName = line.name;
                    existingTab.CodePrefix = line.code_prefix;
                    existingTab.IdProductLine = line.id_product_line;
                    int currentIndex = inventory_tabs.IndexOf(existingTab);
                    if (currentIndex != targetIndex && targetIndex < inventory_tabs.Count)
                    {
                        inventory_tabs.Move(currentIndex, targetIndex);
                    }
                }
                else
                {
                    var newTab = new BrandTabItem(line.name, line.code_prefix, line.id_product_line);
                    if (targetIndex <= inventory_tabs.Count)
                        inventory_tabs.Insert(targetIndex, newTab);
                    else
                        inventory_tabs.Add(newTab);
                }
            }

            // 6. Colocar Promociones al final
            inventory_tabs.Add(promoTab);



            // 7. Sincronizar category_options y price_list_brand_options
            category_options.Clear();
            var checkedBrands = new HashSet<string>(
                price_list_brand_options.Where(o => o.is_checked).Select(o => o.name),
                StringComparer.OrdinalIgnoreCase);

            price_list_brand_options.Clear();

            foreach (var line in orderedLines)
            {
                category_options.Add(line.name);
                var opt = new brand_selection_option(line.name, on_brand_option_changed);
                if (_all_brands_selected || checkedBrands.Contains(line.name))
                {
                    opt.is_checked = true;
                }
                price_list_brand_options.Add(opt);
            }
            load_price_list_selections_async();

            if (!category_options.Contains(new_category))
            {
                new_category = category_options.FirstOrDefault() ?? "DEFILE";
            }

            // 8. Restaurar pestaña seleccionada
            if (wasPromocionesSelected)
            {
                selected_tab = promoTab;
            }

            else if (!string.IsNullOrEmpty(previouslySelectedBrand))
            {
                selected_tab = inventory_tabs.OfType<BrandTabItem>()
                    .FirstOrDefault(t => string.Equals(t.BrandName, previouslySelectedBrand, StringComparison.OrdinalIgnoreCase))
                    ?? (InventoryTabItemBase)todosTab;
            }
            else if (wasTodosSelected)
            {
                selected_tab = todosTab;
            }
            else if (selected_tab == null && inventory_tabs.Count > 0)
            {
                selected_tab = inventory_tabs[0];
            }
        }

        private async Task load_product_lines_async()
        {
            try
            {
                List<product_line> activeLines;
                if (_product_line_service != null)
                {
                    var lines = await _product_line_service.GetActiveAsync();
                    activeLines = lines?.ToList() ?? new List<product_line>();
                }
                else
                {
                    activeLines = new List<product_line>();
                }

                sync_tabs(activeLines);
            }
            catch (Exception ex)
            {
                AppLog.Warn($"Error loading product lines: {ErrorText.Get(ex)}");
                sync_tabs(new List<product_line>());
            }
        }

        private async void load_initial_data_async()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                
                await load_product_lines_async();

                if (_price_list_service != null)
                {
                    _all_own_prices = await _price_list_service.GetAllOwnPricesAsync();
                    await load_price_list_options_async();
                }

                IEnumerable<product> products = await _inventory_service.get_all_products_async();
                _all_products_source = products.Where(p => p.product_code != "Z-9999-INT" && !p.product_code.StartsWith("Z-")).ToList();
                
                IEnumerable<promotion> promotions = await _inventory_service.get_all_promotions_async();
                _all_promotions_source = promotions.ToList();

                await load_trashed_promotion_codes_async();

                filter_data();
                load_price_list_selections_async();
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

        public async Task load_price_list_options_async(int? selectListId = null)
        {
            if (_price_list_service == null) return;
            try
            {
                var principal = await _price_list_service.GetPrincipalAsync();
                var listas = await _price_list_service.GetActiveAsync();

                void updateList()
                {
                    int? currentId = selectListId ?? selected_price_list?.id_price_list;

                    price_list_options.Clear();
                    price_list_options.Add(principal);
                    foreach (var l in listas.OrderBy(x => x.sort_order).ThenBy(x => x.name))
                    {
                        price_list_options.Add(l);
                    }

                    price_list target = principal;
                    if (currentId.HasValue)
                    {
                        target = price_list_options.FirstOrDefault(x => x.id_price_list == currentId.Value) ?? principal;
                    }

                    _selected_price_list = target;
                    on_property_changed(nameof(selected_price_list));
                    on_property_changed(nameof(is_principal_list_selected));
                    on_property_changed(nameof(is_custom_list_selected));
                    on_property_changed(nameof(price_field_label));
                    on_property_changed(nameof(price_field_hint));
                    on_property_changed(nameof(is_add_product_button_visible));
                }

                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(updateList);
                }
                else
                {
                    updateList();
                }
            }
            catch (Exception ex)
            {
                AppLog.Warn($"No se pudieron cargar opciones de lista de precios: {ex.Message}");
            }
        }

        private void update_displayed_prices()
        {
            void runUpdate()
            {
                bool isCustomList = selected_price_list != null && !selected_price_list.is_principal;
                int listId = selected_price_list?.id_price_list ?? 0;

                foreach (var tab in inventory_tabs)
                {
                    foreach (var item in tab.Items)
                    {
                        if (item.is_promotion || item.product_ref == null) continue;

                        decimal price = item.product_ref.unit_price_usd;
                        bool hasCustom = false;

                        if (isCustomList && _all_own_prices.TryGetValue(item.product_ref.id_product, out var ownPrices)
                            && ownPrices != null
                            && ownPrices.TryGetValue(listId, out decimal customPrice))
                        {
                            price = customPrice;
                            hasCustom = true;
                        }

                        item.item_price = price;
                        item.has_custom_price = hasCustom;
                        item.is_custom_list = isCustomList;
                    }
                }
            }

            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(runUpdate);
            }
            else
            {
                runUpdate();
            }
        }

        public async void refresh_data()
        {
            try
            {
                await load_product_lines_async();

                if (_price_list_service != null)
                {
                    _all_own_prices = await _price_list_service.GetAllOwnPricesAsync();
                    await load_price_list_options_async();
                }

                IEnumerable<product> products = await _inventory_service.get_all_products_async();
                _all_products_source = products.Where(p => p.product_code != "Z-9999-INT" && !p.product_code.StartsWith("Z-")).ToList();
                
                IEnumerable<promotion> promotions = await _inventory_service.get_all_promotions_async();
                _all_promotions_source = promotions.ToList();

                await load_trashed_promotion_codes_async();

                filter_data();
                load_price_list_selections_async();
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
            if (_selected_tab is PromocionesTabItem)
            {
                add_button_text = "+ AÑADIR PROMOCIÓN";
            }
            else
            {
                add_button_text = "+ AÑADIR PRODUCTO";

                if (_selected_tab is BrandTabItem brandTab)
                {
                    new_category = brandTab.BrandName;
                }
            }
        }

        /// <summary>
        /// Pide al servicio el siguiente codigo de la marca y lo deja en new_code. Va al
        /// servicio (y no aqui) porque el correlativo tiene que considerar tambien los
        /// productos que estan en la papelera, si no un codigo dado de baja se reutiliza.
        /// El contador de generación evita que una consulta lenta pise el codigo de una marca nueva.
        /// </summary>
        private async void request_next_code_async(string category)
        {
            if (!product_code_rules.TryGetPrefix(category, out _)) return;

            int generation = ++_code_request_generation;

            try
            {
                string code = await _inventory_service.get_next_product_code_async(category);

                // El usuario cambio de marca otra vez mientras esperabamos la respuesta.
                if (generation != _code_request_generation) return;
                if (_product_being_edited != null) return;

                new_code = code;
            }
            catch (Exception ex)
            {
                if (generation != _code_request_generation) return;

                AppLog.Warn($"No se pudo generar el codigo para la marca {category}. " +
                            $"Detalle: {ex.GetType().Name}: {ex.Message}");

                new_code = string.Empty;
                ErrorMessage = "No se pudo consultar el siguiente código. Revisa la conexión y vuelve a abrir la ventana.";
            }
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

        private inventory_item_dto create_product_dto(product p)
        {
            decimal price = p.unit_price_usd;
            bool hasCustom = false;
            bool isCustomList = selected_price_list != null && !selected_price_list.is_principal;

            if (isCustomList && selected_price_list != null)
            {
                if (_all_own_prices.TryGetValue(p.id_product, out var ownPrices) && ownPrices != null)
                {
                    if (ownPrices.TryGetValue(selected_price_list.id_price_list, out decimal customPrice))
                    {
                        price = customPrice;
                        hasCustom = true;
                    }
                }
            }

            return new inventory_item_dto
            {
                id_display = p.id_product.ToString(),
                item_code = p.product_code,
                item_name = p.name,
                item_category = p.category,
                item_quantity = p.stock_quantity.ToString(),
                item_price = price,
                has_custom_price = hasCustom,
                is_custom_list = isCustomList,
                is_promotion = false,
                is_default_combo = false,
                product_ref = p,
                is_negative_stock = p.stock_quantity < 0
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
                promo_ref = p,
                is_negative_stock = calculated_available < 0
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

            foreach (var tab in inventory_tabs)
            {
                tab.Items.Clear();
            }

            var todosTab = inventory_tabs.OfType<TodosTabItem>().FirstOrDefault();
            var promoTab = inventory_tabs.OfType<PromocionesTabItem>().FirstOrDefault();
            var brandTabMap = inventory_tabs.OfType<BrandTabItem>()
                .ToDictionary(t => t.BrandName, t => t, StringComparer.OrdinalIgnoreCase);

            foreach (product p in filtered_products)
            {
                var dto = create_product_dto(p);
                todos_list.Add(dto);
                todosTab?.Items.Add(dto);

                string safe_category = (p.category ?? string.Empty).Trim();

                // Pestaña dinámica por marca
                if (brandTabMap.TryGetValue(safe_category, out var bTab))
                {
                    bTab.Items.Add(create_product_dto(p));
                }
                else if (brandTabMap.TryGetValue("OTROS", out var otrosTabDynamic))
                {
                    otrosTabDynamic.Items.Add(create_product_dto(p));
                }

                // Colecciones legacy para retrocompatibilidad
                inventory_item_dto category_dto = create_product_dto(p);
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
                todosTab?.Items.Add(new_dto);

                promociones_list.Add(new_dto);
                promoTab?.Items.Add(new_dto);
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

            foreach (var tab in inventory_tabs)
            {
                assign_row_numbers(tab.Items);
            }
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

        /// <summary>
        /// Despliega el apartado de ajuste de stock dentro de Editar Producto. El parametro
        /// dice la direccion: "add" para sumar, "remove" para restar. Si el apartado ya
        /// estaba abierto en la otra direccion, solo se cambia la direccion y se conserva
        /// lo que la clienta ya habia tecleado.
        /// </summary>
        private void execute_open_stock_section(object? parameter)
        {
            if (!is_editing_product || _product_being_edited == null) return;

            bool quitar = string.Equals(parameter as string, "remove", StringComparison.OrdinalIgnoreCase);
            bool ya_abierto = is_stock_addition_mode;

            if (!ya_abierto)
            {
                stock_addition_quantity = string.Empty;
                stock_addition_reason = string.Empty;
                reset_adjustment_reason();
            }

            // El total se calcula siempre sobre el stock que tenia el producto al abrir la
            // ventana, no sobre lo que se haya tecleado antes.
            stock_addition_current_quantity = _original_quantity_for_edit;
            is_stock_removal_mode = quitar;
            is_stock_addition_mode = true;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Colapsa el apartado de ajuste y deja sus campos limpios. No cierra la ventana:
        /// la clienta puede seguir editando nombre, categoria o precio.
        /// </summary>
        private void execute_close_stock_section(object? parameter)
        {
            is_stock_addition_mode = false;
            is_stock_removal_mode = false;
            stock_addition_quantity = string.Empty;
            stock_addition_reason = string.Empty;
            stock_addition_current_quantity = _original_quantity_for_edit;
            reset_adjustment_reason();
            ErrorMessage = string.Empty;
        }

        private void execute_open_add_window(object? parameter)
        {
            is_stock_addition_mode = false;
            is_stock_removal_mode = false;
            stock_addition_quantity = string.Empty;
            stock_addition_reason = string.Empty;
            stock_addition_current_quantity = 0;
            if (_selected_tab is PromocionesTabItem || _selected_tab_index == inventory_tabs.Count - 1)
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
            _original_quantity_for_edit = 0;
            _original_code_for_edit = string.Empty;
            _original_brand_for_edit = string.Empty;
            _preview_code_for_new_brand = string.Empty;
            new_name = string.Empty;
            new_quantity = string.Empty;
            new_price = string.Empty;
            reset_adjustment_reason();

            update_category_from_tab();

            // La categoria fija el prefijo del codigo; el correlativo lo pide el servicio.
            new_code = string.Empty;
            request_next_code_async(new_category);

            on_property_changed(nameof(can_edit_category));
            on_property_changed(nameof(is_editing_product));
            on_property_changed(nameof(code_is_locked));
            on_property_changed(nameof(name_is_locked));
            on_property_changed(nameof(show_initial_quantity_box));
            on_property_changed(nameof(has_pending_brand_change));
            on_property_changed(nameof(is_principal_list_selected));
            on_property_changed(nameof(is_custom_list_selected));
            on_property_changed(nameof(price_field_label));
            on_property_changed(nameof(price_field_hint));
            on_request_add_window?.Invoke();
        }

        /// <summary>
        /// Al cambiar la marca de un producto que se esta editando, calcula que codigo le
        /// tocaria en la marca destino y lo pone en la caja, que es de solo lectura, para
        /// que se vea de inmediato. Si se vuelve a la marca original, la caja recupera el
        /// codigo de siempre.
        ///
        /// Es solo una previsualizacion: el codigo definitivo lo recalcula y asigna el
        /// servicio al guardar, asi que esto no puede dejar un codigo mal puesto.
        /// </summary>
        private async void request_brand_change_preview_async(string category)
        {
            _preview_code_for_new_brand = string.Empty;
            on_property_changed(nameof(preview_code_for_new_brand));
            on_property_changed(nameof(has_pending_brand_change));
            on_property_changed(nameof(brand_change_warning_display));

            if (!is_editing_product) return;

            if (string.Equals(category, _original_brand_for_edit, StringComparison.OrdinalIgnoreCase))
            {
                new_code = _original_code_for_edit;
                return;
            }

            if (!product_code_rules.TryGetPrefix(category, out _)) return;

            int generation = ++_code_request_generation;

            try
            {
                string code = await _inventory_service.get_next_product_code_async(category);

                if (generation != _code_request_generation) return;
                if (_product_being_edited == null) return;
                if (!string.Equals(new_category, category, StringComparison.OrdinalIgnoreCase)) return;

                _preview_code_for_new_brand = code;

                // Se muestra el codigo de la marca destino en la propia caja.
                new_code = code;

                on_property_changed(nameof(preview_code_for_new_brand));
                on_property_changed(nameof(has_pending_brand_change));
                on_property_changed(nameof(brand_change_warning_display));
            }
            catch (Exception ex)
            {
                if (generation != _code_request_generation) return;

                AppLog.Warn($"No se pudo calcular el codigo para la marca {category}. " +
                            $"Detalle: {ex.GetType().Name}: {ex.Message}");

                // Sin previsualizacion no se pisa el codigo real del producto.
                new_code = _original_code_for_edit;
            }
        }

        private void reset_adjustment_reason()
        {
            _adjustment_reason_index = -1;
            on_property_changed(nameof(adjustment_reason_index));
            on_property_changed(nameof(adjustment_reason_selected));
        }

        private async void execute_edit_product(object? parameter)
        {
            if (parameter is not inventory_item_dto dto || dto.is_promotion || dto.product_ref == null) return;

            if (is_principal_list_selected)
            {
                _product_being_edited = dto.product_ref;
                _original_quantity_for_edit = dto.product_ref.stock_quantity;
                _original_code_for_edit = dto.product_ref.product_code;
                _original_brand_for_edit = dto.product_ref.category;
                _preview_code_for_new_brand = string.Empty;
                reset_adjustment_reason();

                // El apartado de ajuste arranca cerrado cada vez que se abre la ventana.
                is_stock_addition_mode = false;
                is_stock_removal_mode = false;
                stock_addition_quantity = string.Empty;
                stock_addition_reason = string.Empty;
                stock_addition_current_quantity = dto.product_ref.stock_quantity;

                new_code = dto.product_ref.product_code;
                new_name = dto.product_ref.name;
                new_category = dto.product_ref.category;
                new_quantity = dto.product_ref.stock_quantity.ToString();
                new_price = dto.product_ref.unit_price_usd.ToString();

                on_property_changed(nameof(can_edit_category));
                on_property_changed(nameof(is_editing_product));
                on_property_changed(nameof(code_is_locked));
                on_property_changed(nameof(name_is_locked));
                on_property_changed(nameof(show_initial_quantity_box));
                on_property_changed(nameof(original_code_for_edit));
                on_property_changed(nameof(has_pending_brand_change));
                on_request_add_window?.Invoke();
            }
            else
            {
                // Edición de precio para la lista seleccionada
                if (selected_price_list == null || _price_list_service == null) return;

                string currentVal = dto.item_price.ToString("N2", CultureInfo.InvariantCulture);
                string? input = prompt_price_edit?.Invoke(
                    $"Precio en {selected_price_list.name}",
                    $"Nuevo precio ($) para \"{dto.item_name}\" en {selected_price_list.name}:",
                    currentVal);

                if (string.IsNullOrWhiteSpace(input)) return;

                string clean = input.Trim().Replace("$", "").Trim().Replace(',', '.');
                if (!decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal newPrice) || newPrice < 0)
                {
                    AppDialog.Show("Ingresa un precio válido (número mayor o igual a 0).", "Precio inválido");
                    return;
                }

                try
                {
                    await _price_list_service.SaveProductPriceAsync(dto.product_ref.id_product, selected_price_list.id_price_list, newPrice);
                    _all_own_prices = await _price_list_service.GetAllOwnPricesAsync();
                    update_displayed_prices();
                    AppDataEvents.raise_catalogs_changed();
                }
                catch (Exception ex)
                {
                    AppLog.Error("Error al guardar precio en lista: " + ex.Message, ex);
                    AppDialog.Show("Error al guardar el precio: " + ex.Message, "Error");
                }
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

        private async Task<(List<price_list_item> items, List<price_list_column> columns)?> prepare_price_list_data_async()
        {
            if (price_list_brand_options.Count == 0 || price_list_brand_options.All(o => !o.is_checked))
            {
                AppDialog.Show(
                    "Seleccione al menos una marca/categoría para generar la lista de precios.",
                    "Lista de precios",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return null;
            }

            // Cargar precios propios de manera asíncrona sin bloquear el hilo de la UI
            Dictionary<int, Dictionary<int, decimal>> allOwn = new Dictionary<int, Dictionary<int, decimal>>();
            if (_price_list_service != null)
            {
                try
                {
                    allOwn = await _price_list_service.GetAllOwnPricesAsync();
                }
                catch (Exception ex)
                {
                    AppLog.Warn($"No se pudieron cargar precios propios: {ex.Message}");
                    allOwn = new Dictionary<int, Dictionary<int, decimal>>();
                }
            }

            // Determinar listas seleccionadas
            var selectedLists = price_list_selection_options.Where(x => x.is_checked).ToList();
            bool includePrincipal = is_principal_included_in_report;

            if (!includePrincipal && selectedLists.Count == 0)
            {
                AppDialog.Show(
                    "Seleccione al menos una lista de precios para generar el reporte.",
                    "Lista de precios",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return null;
            }

            var columns = new List<price_list_column>();
            if (includePrincipal)
            {
                columns.Add(new price_list_column { key = "ORIGINAL", name = _principal_price_list_name, is_original = true });
            }
            foreach (var sl in selectedLists)
            {
                columns.Add(new price_list_column { key = sl.id_price_list.ToString(), name = sl.name, is_original = false });
            }

            var items = new List<price_list_item>();

            foreach (product p in _all_products_source)
            {
                if (string.IsNullOrWhiteSpace(p.product_code) || string.IsNullOrWhiteSpace(p.name)) continue;
                if (p.unit_price_usd <= 0) continue;
                if (!is_brand_selected(p.category)) continue;

                var pricesByKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                if (allOwn.TryGetValue(p.id_product, out var ownDict))
                {
                    foreach (var kv in ownDict)
                    {
                        pricesByKey[kv.Key.ToString()] = kv.Value;
                    }
                }

                items.Add(new price_list_item
                {
                    product_code = p.product_code,
                    name = p.name,
                    brand = p.category,
                    unit_price_usd = p.unit_price_usd,
                    prices_by_key = pricesByKey
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
                        unit_price_usd = promo.unit_price_usd,
                        prices_by_key = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    });
                }
            }

            return (items, columns);
        }

        private async void execute_generate_price_list(object? parameter)
        {
            try
            {
                var data = await prepare_price_list_data_async();
                if (data == null) return;

                PriceListPdfGenerator.generate(data.Value.items, data.Value.columns);
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al generar la lista de precios en PDF", ex);
                AppDialog.Show("No se pudo generar la lista de precios en PDF: " + ErrorText.Get(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void execute_generate_price_list_excel(object? parameter)
        {
            try
            {
                var data = await prepare_price_list_data_async();
                if (data == null) return;

                PriceListExcelGenerator.generate(data.Value.items, data.Value.columns);
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al exportar la lista de precios a Excel", ex);
                AppDialog.Show("No se pudo exportar la lista de precios a Excel: " + ErrorText.Get(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool is_brand_selected(string? category)
        {
            string safe = string.IsNullOrWhiteSpace(category) ? "Otros" : category;
            return price_list_brand_options.Any(o =>
                o.is_checked && o.name.Equals(safe, StringComparison.OrdinalIgnoreCase));
        }

        private void execute_print_current_inventory(object? parameter)
        {
            try
            {
                var items = _all_products_source
                    .Where(p => !string.IsNullOrWhiteSpace(p.product_code) && p.is_active && p.product_code != "Z-9999-INT" && !p.product_code.StartsWith("Z-"))
                    .Select(p => new CurrentInventoryPdfItem
                    {
                        ProductCode = p.product_code,
                        Name = p.name,
                        Category = p.category,
                        StockQuantity = p.stock_quantity,
                        UnitPriceUsd = p.unit_price_usd
                    })
                    .ToList();

                var model = new CurrentInventoryPdfModel
                {
                    EmissionDate = DateTime.Now,
                    Items = items
                };

                CurrentInventoryPdfGenerator.generate(model);
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al generar el reporte de inventario actual.", ex);
                AppDialog.Show(
                    "No se pudo generar el reporte de inventario actual.\n\n" + ErrorText.Get(ex),
                    "Error de impresión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void execute_print_all_movements(object? parameter)
        {
            try
            {
                IsLoading = true;

                var movements = await _inventory_service.get_all_stock_movements_async();
                var list = movements
                    .Where(m => m.product_code != "Z-9999-INT" && !m.product_code.StartsWith("Z-") && !string.IsNullOrWhiteSpace(m.product_code) && m.product_code != "-")
                    .ToList();

                var productGroups = list
                    .GroupBy(m => m.id_product)
                    .Select(g =>
                    {
                        var first = g.First();
                        return new ProductMovementGroup
                        {
                            IdProduct = first.id_product,
                            ProductCode = first.product_code,
                            ProductName = first.product_name,
                            Category = first.product_category,
                            CurrentStock = first.product_current_stock,
                            UnitPriceUsd = first.unit_price_usd,
                            Rows = g.OrderByDescending(r => r.creation_date)
                                    .ThenByDescending(r => r.id_delivery_note)
                                    .Select(ToPdfRow)
                                    .ToList()
                        };
                    })
                    .OrderBy(g => g.Category)
                    .ThenBy(g => g.ProductCode)
                    .ToList();

                var model = new AllMovementsPdfModel
                {
                    EmissionDate = DateTime.Now,
                    ProductGroups = productGroups
                };

                AllMovementsPdfGenerator.generate(model);
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al generar el reporte de todos los movimientos de inventario.", ex);
                AppDialog.Show(
                    "No se pudo generar el reporte de movimientos de inventario.\n\n" + ErrorText.Get(ex),
                    "Error de impresión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static InventoryHistoryPdfRow ToPdfRow(product_sales_history_dto dto)
        {
            return new InventoryHistoryPdfRow
            {
                NoteNumber = dto.note_number ?? string.Empty,
                FechaLocal = AppTimeZone.to_local(dto.creation_date),
                Documento = dto.documento_display,
                Motivo = dto.motivo_display,
                Cliente = dto.customer_name ?? string.Empty,
                Vendedor = dto.seller_name ?? string.Empty,
                Movimiento = dto.movement_type,
                Unidades = dto.units_sold,
                PrecioUsd = dto.unit_price_usd,
                SubtotalUsd = dto.line_subtotal_usd,
                Estado = dto.status ?? string.Empty
            };
        }

        private async void execute_delete_product(object? parameter)
        {
            if (parameter is not inventory_item_dto dto || dto.is_promotion || dto.product_ref == null) return;

            if (is_principal_list_selected)
            {
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
            else
            {
                // En lista personalizada: quitar precio propio y volver a heredar de la Principal
                if (selected_price_list == null || _price_list_service == null) return;
                if (!dto.has_custom_price) return;

                var principal = await _price_list_service.GetPrincipalAsync();
                string principalName = principal?.name ?? "Principal";

                MessageBoxResult confirm = AppDialog.Show(
                    $"¿Quitar el precio propio de \"{dto.item_name}\" en la lista \"{selected_price_list.name}\"?\n\n" +
                    $"El producto volverá a usar el precio de la lista Principal (${dto.product_ref.unit_price_usd:N2}).",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    await _price_list_service.SaveProductPriceAsync(dto.product_ref.id_product, selected_price_list.id_price_list, null);
                    _all_own_prices = await _price_list_service.GetAllOwnPricesAsync();
                    update_displayed_prices();
                    AppDataEvents.raise_catalogs_changed();
                }
                catch (Exception ex)
                {
                    AppLog.Error("Error al borrar precio propio: " + ex.Message, ex);
                    AppDialog.Show("Error al quitar el precio propio: " + ex.Message, "Error");
                }
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

            // Normaliza el codigo a mayusculas para evitar duplicados por minusculas/espacios.
            string normalized_code = new_code.Trim().ToUpperInvariant();
            new_code = normalized_code;

            if (string.IsNullOrWhiteSpace(new_quantity))
            {
                ErrorMessage = "La cantidad es obligatoria.";
                return;
            }

            if (InputRestrictions.has_letters(new_quantity))
            {
                ErrorMessage = "No se permiten letras en la cantidad. Ingrese solo números enteros.";
                return;
            }

            bool es_edicion = _product_being_edited != null;

            if (!int.TryParse(new_quantity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed_quantity))
            {
                ErrorMessage = "La cantidad debe ser un número entero válido.";
                return;
            }

            if (!es_edicion && parsed_quantity < 0)
            {
                ErrorMessage = "La cantidad inicial del producto no puede ser negativa.";
                return;
            }

            if (string.IsNullOrWhiteSpace(new_price))
            {
                ErrorMessage = "El precio es obligatorio.";
                return;
            }

            if (InputRestrictions.has_letters(new_price))
            {
                ErrorMessage = "No se permiten letras en el precio. Ingrese solo números.";
                return;
            }

            if (!decimal.TryParse((new_price ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed_price) || parsed_price < 0)
            {
                ErrorMessage = "El precio debe ser un número mayor o igual a 0.";
                return;
            }

            // El apartado puede quedar abierto sin que la clienta haya escrito nada: en ese
            // caso no hay ajuste que hacer y se guarda el resto de los cambios sin exigir
            // cantidad ni motivo. Frenarla con "indica la cantidad" seria un error falso.
            bool apartado_vacio = is_stock_addition_mode
                && string.IsNullOrWhiteSpace(stock_addition_quantity)
                && string.IsNullOrEmpty(adjustment_reason_selected);

            bool hay_ajuste_de_stock = is_stock_addition_mode && !apartado_vacio && _product_being_edited != null;

            if (hay_ajuste_de_stock)
            {
                if (string.IsNullOrWhiteSpace(stock_addition_quantity))
                {
                    ErrorMessage = is_stock_removal_mode ? "Indica la cantidad a quitar." : "Indica la cantidad a agregar.";
                    return;
                }
                if (InputRestrictions.has_letters(stock_addition_quantity))
                {
                    ErrorMessage = "No se permiten letras en la cantidad. Ingrese solo números enteros.";
                    return;
                }
                if (!int.TryParse(stock_addition_quantity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty_move) || qty_move <= 0)
                {
                    ErrorMessage = is_stock_removal_mode ? "La cantidad a quitar debe ser un número entero mayor a 0." : "La cantidad a agregar debe ser un número entero mayor a 0.";
                    return;
                }

                // El motivo sale del combo: es lo que clasifica el movimiento en el kardex.
                if (string.IsNullOrEmpty(adjustment_reason_selected))
                {
                    ErrorMessage = "Selecciona el motivo del ajuste.";
                    return;
                }

                if (is_stock_removal_mode)
                {
                    if (qty_move > stock_addition_current_quantity)
                    {
                        ErrorMessage = "No hay suficiente stock para quitar esa cantidad.";
                        return;
                    }
                    parsed_quantity = stock_addition_current_quantity - qty_move;
                }
                else
                {
                    parsed_quantity = stock_addition_current_quantity + qty_move;
                }
            }

            // Validar codigo si es nuevo o si se modifico el codigo manualmente
            if (!es_edicion || (!has_pending_brand_change && !string.Equals(normalized_code, _original_code_for_edit, StringComparison.OrdinalIgnoreCase)))
            {
                if (string.IsNullOrWhiteSpace(normalized_code))
                {
                    ErrorMessage = "El código del producto es obligatorio.";
                    return;
                }

                if (!product_code_rules.TryGetPrefix(new_category, out string prefix))
                {
                    ErrorMessage = $"La marca {new_category} no tiene un prefijo de código configurado.";
                    return;
                }

                if (!product_code_rules.IsValidFor(normalized_code, new_category))
                {
                    ErrorMessage = $"El código debe tener el formato de la marca {new_category} " +
                                   $"(prefijo {prefix} y {product_code_rules.DigitsFor(prefix)} dígitos).";
                    return;
                }
            }

            // Cambiar de marca cambia el codigo, y con el los numeros de serie de esa marca.
            // Se pide confirmacion para que no sea un cambio sin querer.
            if (has_pending_brand_change)
            {
                MessageBoxResult confirmacion = AppDialog.Show(
                    $"El producto pasara de la marca {_original_brand_for_edit} a {new_category}.\n\n" +
                    $"Su codigo cambiara de {original_code_for_edit} a {preview_code_for_new_brand}.\n\n" +
                    "Las notas ya emitidas siguen mostrando el codigo con el que se crearon.\n\n" +
                    "¿Deseas continuar?",
                    "Cambio de marca",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmacion != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                bool cambio_de_codigo = es_edicion && (has_pending_brand_change || !string.Equals(normalized_code, _original_code_for_edit, StringComparison.OrdinalIgnoreCase));

                if (_product_being_edited != null)
                {
                    // El codigo no se toca desde aca: si cambio la marca, el servicio calcula
                    // el codigo de la serie nueva y asigna los dos juntos.
                    _product_being_edited.name = new_name;
                    _product_being_edited.unit_price_usd = parsed_price;
                    _product_being_edited.stock_quantity = parsed_quantity;
                    _product_being_edited.product_code = normalized_code;

                    string? motivoParaPasar = null;
                    if (hay_ajuste_de_stock)
                    {
                        motivoParaPasar = adjustment_reason_selected;
                    }
                    await _inventory_service.update_product_async(
                        _product_being_edited,
                        motivoParaPasar,
                        has_pending_brand_change ? new_category : null);
                }
                else
                {
                    product new_prod = new product(normalized_code, new_name, new_category, parsed_price, parsed_quantity);
                    await _inventory_service.add_product_async(new_prod);

                    if (!is_principal_list_selected && selected_price_list != null && _price_list_service != null)
                    {
                        try
                        {
                            await _price_list_service.SaveProductPriceAsync(new_prod.id_product, selected_price_list.id_price_list, parsed_price);
                        }
                        catch (Exception ex)
                        {
                            AppLog.Warn($"No se pudo asignar el precio personalizado para la lista {selected_price_list.name}: {ex.Message}");
                        }
                    }
                }

                _product_being_edited = null;
                _original_quantity_for_edit = 0;
                _original_code_for_edit = string.Empty;
                _original_brand_for_edit = string.Empty;
                _preview_code_for_new_brand = string.Empty;
                is_stock_addition_mode = false;
                is_stock_removal_mode = false;
                stock_addition_quantity = string.Empty;
                stock_addition_reason = string.Empty;
                stock_addition_current_quantity = 0;
                on_property_changed(nameof(is_editing_product));
                on_property_changed(nameof(code_is_locked));
                on_property_changed(nameof(name_is_locked));
                on_property_changed(nameof(show_initial_quantity_box));
                on_property_changed(nameof(has_pending_brand_change));
                load_initial_data_async();

                // Las ventanas de ventas, notas y lista de precios tienen el codigo en memoria:
                // sin este aviso seguirian mostrando el anterior.
                if (cambio_de_codigo || !es_edicion)
                {
                    AppDataEvents.raise_catalogs_changed();
                }

                on_close_add_window?.Invoke();
                AppDialog.Show(
                    es_edicion ? "Producto actualizado exitosamente." : "Producto creado exitosamente.",
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

            if (InputRestrictions.has_letters(new_promo_price))
            {
                ErrorMessage = "No se permiten letras en el precio de la promoción. Ingrese solo números.";
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