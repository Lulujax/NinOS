using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Common;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public class credit_note_edit_row : ViewModelBase
    {
        public int? id_product { get; set; }
        public int? id_promotion { get; set; }
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;

        /// <summary>
        /// Precio unitario NETO de la linea: el que el cliente realmente pago, con el descuento de
        /// la nota de entrega ya descontado. En un obsequio es el precio del producto, porque ahi
        /// no hay descuento de por medio.
        /// </summary>
        public decimal unit_price_usd { get; set; }

        public int delivered_quantity { get; set; }
        public int already_returned_quantity { get; set; }
        public int remaining_quantity { get; set; }

        // Factor de recorte por saldo disponible. 1 = sin recorte. Lo aplica la ventana, no el
        // usuario, para que la nota nunca acredite mas dinero del que le queda a la nota de entrega.
        private decimal _cap_factor = 1m;

        // Cuanto se absorbs el redondeo del recorte. Es la diferencia entre el saldo disponible y
        // la suma de los subtotales ya redondeados, y se entera a la ultima linea con cantidad.
        private decimal? _subtotal_override;

        public void set_cap_factor(decimal factor)
        {
            decimal clamped = factor < 0m ? 0m : (factor > 1m ? 1m : factor);
            _subtotal_override = null;
            if (_cap_factor == clamped) return;
            _cap_factor = clamped;
            on_property_changed(nameof(display_unit_price_usd));
            on_property_changed(nameof(subtotal_usd));
        }

        public void set_subtotal_override(decimal? value)
        {
            if (_subtotal_override == value) return;
            _subtotal_override = value;
            on_property_changed(nameof(subtotal_usd));
        }

        // Precio que se muestra y se suma. Con recorte activo es el neto ya ajustado, para que
        // la columna SUBTOTAL siempre cuadre con cantidad por el precio que se ve en pantalla.
        public decimal display_unit_price_usd => Money.round(unit_price_usd * _cap_factor);

        private int _return_quantity;
        public int return_quantity
        {
            get => _return_quantity;
            set
            {
                int clamped = Math.Clamp(value, 0, remaining_quantity);
                if (_return_quantity == clamped) return;
                _return_quantity = clamped;
                on_property_changed();
                on_property_changed(nameof(subtotal_usd));
            }
        }

        public decimal subtotal_usd => _subtotal_override ?? (return_quantity * display_unit_price_usd);
    }

    public partial class AddCreditNoteWindow : Window
    {
        private readonly CreditNotesViewModel _vm;
        private credit_note_source_dto? _source;
        private List<note_combo_item> _all_combo_items = new();

        private List<seller> _all_sellers = new();
        private seller? _selected_seller;
        private List<customer> _all_customers = new();
        private List<customer> _seller_customers = new();
        private customer? _selected_customer;

        private List<product> _available_products = new();
        private ObservableCollection<credit_note_edit_row> _gift_items = new();

        public event EventHandler? CreditNoteCreated;

        private bool _is_updating_cascade;
        private bool _is_gift;
        private readonly string? _initial_seller_name;

        private bool _notePopupWasOpen;
        private bool _customerPopupWasOpen;
        private bool _productPopupWasOpen;

        public void OnToggleDropdownPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _notePopupWasOpen = NotePopup != null && NotePopup.IsOpen;
        }

        public void OnToggleCustomerDropdownPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _customerPopupWasOpen = CustomerPopup != null && CustomerPopup.IsOpen;
        }

        public void OnToggleProductDropdownPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _productPopupWasOpen = ProductPopup != null && ProductPopup.IsOpen;
        }

        /// <summary>
        /// Ajusta el tamano al area de trabajo de la pantalla.
        ///
        /// Con una altura fija, en una pantalla baja la ventana se pasaba del borde de arriba y el
        /// contenido quedaba pegado al tope, ademas de que la tabla de productos se quedaba sin
        /// espacio y su barra de scroll no servia de nada. Limitandola al area util, la banda de
        /// titulo queda siempre visible y la tabla recibe lo que sobra.
        /// </summary>
        private void FitToWorkingArea()
        {
            var work_area = SystemParameters.WorkArea;

            // Margen para que la ventana no quede pegada a los bordes de la pantalla.
            double max_height = work_area.Height - 40;
            double max_width = work_area.Width - 40;

            if (max_height > 0 && Height > max_height) Height = max_height;
            if (max_width > 0 && Width > max_width) Width = max_width;

            MaxHeight = max_height > 0 ? max_height : Height;
            MaxWidth = max_width > 0 ? max_width : Width;
        }

        public AddCreditNoteWindow(CreditNotesViewModel vm, string? current_month = null, string? initial_seller_name = null)
        {
            InitializeComponent();
            _vm = vm;
            _initial_seller_name = initial_seller_name;

            FitToWorkingArea();

            Loaded += async (_, _) =>
            {
                _is_updating_cascade = true;

                if (CmbCategory.SelectedItem is ComboBoxItem selected)
                    _is_gift = string.Equals(selected.Tag as string, "Obsequio", StringComparison.OrdinalIgnoreCase);

                ApplyCategoryMode();

                var sellersTask = LoadSellersAsync();
                var obsequioTask = LoadObsequioDataAsync();
                await Task.WhenAll(sellersTask, obsequioTask);

                seller? preselected = null;
                if (!string.IsNullOrWhiteSpace(_initial_seller_name))
                {
                    preselected = _all_sellers.FirstOrDefault(s => string.Equals(s.full_name?.Trim(), _initial_seller_name.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (preselected != null)
                {
                    CmbSeller.SelectedItem = preselected;
                    _selected_seller = preselected;
                    if (_is_gift)
                    {
                        ApplySellerForObsequio(preselected);
                    }
                    else
                    {
                        await ApplySellerForDevolucionAsync(preselected);
                    }
                }
                else
                {
                    if (_is_gift)
                    {
                        ApplySellerForObsequio(null);
                    }
                    else
                    {
                        ResetDevolucionState();
                        if (CmbSeller != null) CmbSeller.SelectedItem = null;
                        if (CmbMonth != null)
                        {
                            CmbMonth.Items.Clear();
                            CmbMonth.SelectedIndex = -1;
                            CmbMonth.IsEnabled = false;
                            CmbMonth.ToolTip = "Seleccione primero un vendedor...";
                        }
                        if (NoteTextBox != null)
                        {
                            NoteTextBox.IsEnabled = false;
                            NoteTextBox.ToolTip = "Seleccione primero un mes para buscar notas...";
                        }
                        if (BtnToggleDropdown != null)
                        {
                            BtnToggleDropdown.IsEnabled = false;
                        }
                    }
                }

                _is_updating_cascade = false;
            };
        }

        private async Task LoadSellersAsync()
        {
            try
            {
                if (_all_sellers.Count == 0)
                {
                    _all_sellers = (await _vm.get_sellers_async()).OrderBy(s => s.full_name).ToList();
                    CmbSeller.ItemsSource = _all_sellers;
                }
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private async void OnMonthSelected(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _is_updating_cascade) return;
            try
            {
                ResetDevolucionState();
                string? selected_month = CmbMonth.SelectedItem as string;
                if (!string.IsNullOrWhiteSpace(selected_month) && _selected_seller != null)
                {
                    if (NoteTextBox != null)
                    {
                        NoteTextBox.IsEnabled = true;
                        NoteTextBox.ToolTip = "Escriba el numero de nota o nombre del cliente...";
                    }
                    if (BtnToggleDropdown != null) BtnToggleDropdown.IsEnabled = true;
                    await LoadNotesAsync();
                }
                else
                {
                    if (NoteTextBox != null)
                    {
                        NoteTextBox.IsEnabled = false;
                        NoteTextBox.ToolTip = "Seleccione primero un mes para buscar notas...";
                    }
                    if (BtnToggleDropdown != null) BtnToggleDropdown.IsEnabled = false;
                    _all_combo_items.Clear();
                    FilterNotes(string.Empty);
                }
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private async void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (CmbCategory.SelectedItem is not ComboBoxItem selected) return;
            _is_gift = string.Equals(selected.Tag as string, "Obsequio", StringComparison.OrdinalIgnoreCase);
            ApplyCategoryMode();

            var currentSeller = CmbSeller.SelectedItem as seller;

            try
            {
                if (_is_gift)
                {
                    ResetDevolucionState();
                    ResetObsequioState(preserve_seller: true);
                    if (ItemsGrid != null) ItemsGrid.ItemsSource = _gift_items;
                    await LoadObsequioDataAsync();
                    ApplySellerForObsequio(currentSeller);
                }
                else
                {
                    ResetObsequioState(preserve_seller: true);
                    ResetDevolucionState();
                    await ApplySellerForDevolucionAsync(currentSeller);
                }
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private void ResetDevolucionState()
        {
            _source = null;
            if (NoteTextBox != null)
            {
                NoteTextBox.IsReadOnly = false;
                NoteTextBox.Text = string.Empty;
            }
            if (BtnClearNote != null) BtnClearNote.Visibility = Visibility.Collapsed;
            if (CustomerText != null) CustomerText.Text = string.Empty;
            if (SellerText != null) SellerText.Text = string.Empty;
            if (NoteInfoText != null) NoteInfoText.Text = string.Empty;
            if (NoteInfoBorder != null) NoteInfoBorder.Visibility = Visibility.Collapsed;
            if (!_is_gift && ItemsGrid != null) ItemsGrid.ItemsSource = null;
            RecalcTotal();
            if (NotePopup != null) NotePopup.IsOpen = false;
        }

        private void ResetObsequioState(bool preserve_seller = false)
        {
            if (!preserve_seller)
            {
                _selected_seller = null;
                if (CmbSeller != null) CmbSeller.SelectedItem = null;
            }
            _selected_customer = null;
            _seller_customers.Clear();

            if (CustomerTextBox != null)
            {
                CustomerTextBox.IsEnabled = preserve_seller && _selected_seller != null;
                CustomerTextBox.IsReadOnly = false;
                CustomerTextBox.Text = string.Empty;
                CustomerTextBox.ToolTip = preserve_seller && _selected_seller != null
                    ? "Escriba el nombre, codigo o RIF del cliente..."
                    : "Seleccione primero un vendedor...";
            }
            if (BtnClearCustomer != null) BtnClearCustomer.Visibility = Visibility.Collapsed;
            if (CustomerPopup != null) CustomerPopup.IsOpen = false;
            if (CustomerListBox != null) CustomerListBox.ItemsSource = null;

            if (ProductSearchTextBox != null) ProductSearchTextBox.Text = string.Empty;
            if (ProductPopup != null) ProductPopup.IsOpen = false;
            if (ProductListBox != null) ProductListBox.ItemsSource = null;

            foreach (var item in _gift_items) item.PropertyChanged -= OnRowPropertyChanged;
            _gift_items.Clear();

            if (ItemsGrid != null) ItemsGrid.ItemsSource = _is_gift ? _gift_items : null;
            RecalcTotal();
        }

        private void ApplyCategoryMode()
        {
            if (SellerRow == null) return;

            SellerRow.Visibility = Visibility.Visible;
            if (FechaRow != null) FechaRow.Visibility = _is_gift ? Visibility.Collapsed : Visibility.Visible;
            if (BuscadorRow != null) BuscadorRow.Visibility = _is_gift ? Visibility.Collapsed : Visibility.Visible;
            if (NoteInfoBorder != null) NoteInfoBorder.Visibility = !_is_gift && _source != null ? Visibility.Visible : Visibility.Collapsed;
            if (BandGrid != null) BandGrid.Visibility = _is_gift ? Visibility.Collapsed : Visibility.Visible;

            if (ClienteRow != null) ClienteRow.Visibility = _is_gift ? Visibility.Visible : Visibility.Collapsed;
            if (ProductSearchRow != null) ProductSearchRow.Visibility = _is_gift ? Visibility.Visible : Visibility.Collapsed;

            if (BandReturnText != null) BandReturnText.Text = _is_gift ? "A OBSEQUIAR" : "A DEVOLVER";
            if (BandQuantitiesText != null) BandQuantitiesText.Text = _is_gift ? "STOCK DISPONIBLE" : "CANTIDADES DE LA NOTA";
            if (ColReturn != null) ColReturn.Header = _is_gift ? "A OBSEQUIAR" : "A DEVOLVER";
            if (ColDelivered != null) ColDelivered.Visibility = _is_gift ? Visibility.Collapsed : Visibility.Visible;
            if (ColReturned != null) ColReturned.Visibility = _is_gift ? Visibility.Collapsed : Visibility.Visible;
            if (ColAction != null) ColAction.Visibility = _is_gift ? Visibility.Visible : Visibility.Collapsed;

            if (TotalLabel != null) TotalLabel.Text = _is_gift ? "TOTAL A OBSEQUIAR (USD):" : "TOTAL A DEVOLVER (USD):";
            if (HintText != null)
            {
                HintText.Text = _is_gift
                    ? "Busque y agregue los productos a obsequiar. Indique la cantidad de cada uno (no puede superar el STOCK DISPONIBLE)."
                    : "Escriba arriba la cantidad devuelta de cada producto. No puede superar la columna DISPONIBLE.";
            }

            if (ItemsGrid != null)
            {
                ItemsGrid.ItemsSource = _is_gift ? _gift_items : null;
            }
        }

        private async Task LoadObsequioDataAsync()
        {
            try
            {
                await LoadSellersAsync();

                if (_all_customers.Count == 0)
                {
                    _all_customers = (await _vm.get_customers_async()).OrderBy(c => c.business_name).ToList();
                }

                if (_available_products.Count == 0)
                {
                    _available_products = (await _vm.get_obsequio_products_async())
                        .Where(p => p.stock_quantity > 0)
                        .OrderBy(p => p.name)
                        .ToList();
                }

                if (_selected_seller != null)
                {
                    _seller_customers = _all_customers
                        .Where(c => !string.IsNullOrWhiteSpace(c.seller_name)
                            ? string.Equals(c.seller_name.Trim(), _selected_seller.full_name?.Trim(), StringComparison.OrdinalIgnoreCase)
                            : (!string.IsNullOrWhiteSpace(c.customer_code) && c.customer_code.StartsWith(_selected_seller.seller_code)))
                        .OrderBy(c => c.business_name)
                        .ToList();
                    FilterCustomers(CustomerTextBox?.Text?.Trim().ToLower() ?? string.Empty);
                }

                FilterProducts(ProductSearchTextBox?.Text?.Trim().ToLower() ?? string.Empty);

                if (_is_gift && ItemsGrid != null)
                {
                    ItemsGrid.ItemsSource = _gift_items;
                }
                RecalcTotal();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private void ApplySellerForObsequio(seller? s)
        {
            _selected_seller = s;
            _selected_customer = null;

            if (CustomerTextBox != null)
            {
                CustomerTextBox.IsReadOnly = false;
                CustomerTextBox.Text = string.Empty;
                CustomerTextBox.IsEnabled = s != null;
                CustomerTextBox.ToolTip = s != null
                    ? "Escriba el nombre, codigo o RIF del cliente..."
                    : "Seleccione primero un vendedor...";
            }
            if (BtnClearCustomer != null) BtnClearCustomer.Visibility = Visibility.Collapsed;
            if (CustomerPopup != null) CustomerPopup.IsOpen = false;

            if (ProductSearchTextBox != null) ProductSearchTextBox.Text = string.Empty;
            if (ProductPopup != null) ProductPopup.IsOpen = false;

            foreach (var item in _gift_items) item.PropertyChanged -= OnRowPropertyChanged;
            _gift_items.Clear();

            if (ItemsGrid != null) ItemsGrid.ItemsSource = _gift_items;
            RecalcTotal();

            if (s != null)
            {
                _seller_customers = _all_customers
                    .Where(c => !string.IsNullOrWhiteSpace(c.seller_name)
                        ? string.Equals(c.seller_name.Trim(), s.full_name?.Trim(), StringComparison.OrdinalIgnoreCase)
                        : (!string.IsNullOrWhiteSpace(c.customer_code) && c.customer_code.StartsWith(s.seller_code)))
                    .OrderBy(c => c.business_name)
                    .ToList();
                FilterCustomers(string.Empty);
            }
            else
            {
                _seller_customers.Clear();
                if (CustomerListBox != null) CustomerListBox.ItemsSource = null;
            }
        }

        private async Task ApplySellerForDevolucionAsync(seller? s)
        {
            ResetDevolucionState();

            if (NoteTextBox != null)
            {
                NoteTextBox.IsEnabled = false;
                NoteTextBox.ToolTip = "Seleccione primero un mes para buscar notas...";
            }
            if (BtnToggleDropdown != null) BtnToggleDropdown.IsEnabled = false;

            _is_updating_cascade = true;
            if (CmbMonth != null)
            {
                CmbMonth.Items.Clear();
                CmbMonth.SelectedIndex = -1;
            }
            _is_updating_cascade = false;

            if (s != null)
            {
                if (CmbMonth != null)
                {
                    CmbMonth.IsEnabled = true;
                    CmbMonth.ToolTip = "Seleccione el mes de la nota de entrega";
                }
                try
                {
                    var months = (await _vm.get_delivery_note_months_for_seller_async(s.id_seller)).ToList();
                    _is_updating_cascade = true;
                    if (CmbMonth != null)
                    {
                        CmbMonth.Items.Clear();
                        foreach (var m in months) CmbMonth.Items.Add(m);
                        CmbMonth.SelectedIndex = -1;
                    }
                    _is_updating_cascade = false;
                }
                catch (Exception ex)
                {
                    AppDialog.Show(ErrorText.Get(ex), "Error");
                }
            }
            else
            {
                if (CmbMonth != null)
                {
                    CmbMonth.IsEnabled = false;
                    CmbMonth.ToolTip = "Seleccione primero un vendedor...";
                }
            }
        }

        private async void OnSellerChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _is_updating_cascade) return;
            _selected_seller = CmbSeller.SelectedItem as seller;

            if (_is_gift)
            {
                ApplySellerForObsequio(_selected_seller);
            }
            else
            {
                await ApplySellerForDevolucionAsync(_selected_seller);
            }
        }

        private void OnCustomerSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (CustomerTextBox == null || CustomerTextBox.IsReadOnly) return;
            string query = CustomerTextBox.Text?.Trim().ToLower() ?? string.Empty;
            FilterCustomers(query);
            if (CustomerPopup != null && CustomerListBox != null)
            {
                CustomerPopup.IsOpen = !string.IsNullOrEmpty(query) && CustomerListBox.Items.Count > 0;
            }
        }

        private async void OnToggleCustomerDropdown(object sender, RoutedEventArgs e)
        {
            if (CustomerPopup == null || CustomerListBox == null) return;

            if (_customerPopupWasOpen)
            {
                _customerPopupWasOpen = false;
                CustomerPopup.IsOpen = false;
                return;
            }

            if (CustomerPopup.IsOpen)
            {
                CustomerPopup.IsOpen = false;
            }
            else
            {
                if (_selected_seller == null)
                {
                    AppDialog.Show("Seleccione primero un vendedor para ver sus clientes.", "Aviso");
                    return;
                }

                if (_all_customers.Count == 0)
                {
                    await LoadObsequioDataAsync();
                }

                FilterCustomers(CustomerTextBox?.Text?.Trim().ToLower() ?? string.Empty);
                CustomerPopup.IsOpen = CustomerListBox.Items.Count > 0;
                if (CustomerListBox.Items.Count == 0)
                {
                    AppDialog.Show("No se encontraron clientes para el vendedor seleccionado.", "Aviso");
                }
            }
        }

        private void FilterCustomers(string query)
        {
            if (CustomerListBox == null) return;
            if (string.IsNullOrEmpty(query))
            {
                CustomerListBox.ItemsSource = _seller_customers;
            }
            else
            {
                CustomerListBox.ItemsSource = _seller_customers
                    .Where(c => (c.business_name?.ToLower().Contains(query) ?? false) ||
                                (c.customer_code?.ToLower().Contains(query) ?? false) ||
                                (c.rif?.ToLower().Contains(query) ?? false))
                    .ToList();
            }
        }

        private void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerListBox?.SelectedItem is customer c)
            {
                _selected_customer = c;
                if (CustomerTextBox != null)
                {
                    CustomerTextBox.Text = $"{c.business_name} ({c.customer_code})";
                    CustomerTextBox.IsReadOnly = true;
                }
                if (BtnClearCustomer != null) BtnClearCustomer.Visibility = Visibility.Visible;
                if (CustomerPopup != null) CustomerPopup.IsOpen = false;
                CustomerListBox.SelectedItem = null;
            }
        }

        private void OnClearCustomerClick(object sender, RoutedEventArgs e)
        {
            _selected_customer = null;
            if (CustomerTextBox != null)
            {
                CustomerTextBox.IsReadOnly = false;
                CustomerTextBox.Text = string.Empty;
            }
            if (BtnClearCustomer != null) BtnClearCustomer.Visibility = Visibility.Collapsed;
            FilterCustomers(string.Empty);
            if (CustomerPopup != null) CustomerPopup.IsOpen = _seller_customers.Count > 0;
        }

        private void OnProductSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (ProductSearchTextBox == null) return;
            string query = ProductSearchTextBox.Text?.Trim().ToLower() ?? string.Empty;
            FilterProducts(query);
            if (ProductPopup != null && ProductListBox != null)
            {
                ProductPopup.IsOpen = !string.IsNullOrEmpty(query) && ProductListBox.Items.Count > 0;
            }
        }

        private async void OnToggleProductDropdown(object sender, RoutedEventArgs e)
        {
            if (ProductPopup == null || ProductListBox == null) return;

            if (_productPopupWasOpen)
            {
                _productPopupWasOpen = false;
                ProductPopup.IsOpen = false;
                return;
            }

            if (ProductPopup.IsOpen)
            {
                ProductPopup.IsOpen = false;
            }
            else
            {
                if (_available_products.Count == 0)
                {
                    await LoadObsequioDataAsync();
                }

                FilterProducts(ProductSearchTextBox?.Text?.Trim().ToLower() ?? string.Empty);
                ProductPopup.IsOpen = ProductListBox.Items.Count > 0;
                if (ProductListBox.Items.Count == 0)
                {
                    AppDialog.Show("No hay productos con stock disponible para obsequiar.", "Aviso");
                }
            }
        }

        private void FilterProducts(string query)
        {
            if (ProductListBox == null) return;
            if (string.IsNullOrEmpty(query))
            {
                ProductListBox.ItemsSource = _available_products;
            }
            else
            {
                ProductListBox.ItemsSource = _available_products
                    .Where(p => (p.name?.ToLower().Contains(query) ?? false) ||
                                (p.product_code?.ToLower().Contains(query) ?? false))
                    .ToList();
            }
        }

        private void OnProductSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ProductListBox?.SelectedItem is product p)
            {
                if (ItemsGrid != null && ItemsGrid.ItemsSource != _gift_items)
                {
                    ItemsGrid.ItemsSource = _gift_items;
                }

                var existing = _gift_items.FirstOrDefault(r => r.id_product == p.id_product);
                if (existing != null)
                {
                    if (existing.return_quantity < existing.remaining_quantity)
                    {
                        existing.return_quantity++;
                    }
                    else
                    {
                        AppDialog.Show($"El producto {p.name} ya esta en la lista y alcanzo el maximo disponible ({existing.remaining_quantity}).", "Aviso");
                    }
                }
                else
                {
                    var new_row = new credit_note_edit_row
                    {
                        id_product = p.id_product,
                        code = p.product_code,
                        name = p.name,
                        unit_price_usd = p.unit_price_usd,
                        delivered_quantity = 0,
                        already_returned_quantity = 0,
                        remaining_quantity = p.stock_quantity,
                        return_quantity = 1
                    };
                    new_row.PropertyChanged += OnRowPropertyChanged;
                    _gift_items.Add(new_row);
                }

                if (ProductSearchTextBox != null) ProductSearchTextBox.Text = string.Empty;
                if (ProductPopup != null) ProductPopup.IsOpen = false;
                ProductListBox.SelectedItem = null;
                RecalcTotal();
            }
        }

        private void OnRemoveGiftRowClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is credit_note_edit_row row)
            {
                row.PropertyChanged -= OnRowPropertyChanged;
                _gift_items.Remove(row);
                RecalcTotal();
            }
        }

        private async Task LoadNotesAsync()
        {
            try
            {
                if (_selected_seller == null)
                {
                    _all_combo_items.Clear();
                    FilterNotes(string.Empty);
                    return;
                }

                string month_label = CmbMonth.SelectedItem as string ?? string.Empty;

                var notes = await _vm.get_delivery_notes_for_credit_async(_selected_seller.id_seller, month_label);

                // Solo notas con saldo pendiente: una nota de entrega ya pagada no admite nota de
                // credito porque no hay nada por devolver. El servicio ya aplica este mismo filtro;
                // se repite aqui para que la lista sea coherente aunque el dato se haya movido
                // despues de cargado el combo.
                _all_combo_items = notes
                    .Where(n => n.status != "Anulada" && n.balance_due_usd > 0)
                    .OrderByCorrelative(n => n.note_number)
                    .Select(n => new note_combo_item
                    {
                        id_delivery_note = n.id_delivery_note,
                        note_number = n.note_number,
                        customer_name = n.customer_name,
                        balance_due_usd = n.balance_due_usd,
                        total_amount_usd = n.total_amount_usd,
                        paid_amount_usd = n.paid_amount_usd,
                        status = n.status,
                        note_type = !string.IsNullOrWhiteSpace(n.note_type_name) ? n.note_type_name : n.sales_observations
                    }).ToList();

                FilterNotes(string.Empty);
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private void OnNoteSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (_selected_seller == null) return;
            string query = NoteTextBox.Text?.Trim().ToLower() ?? string.Empty;
            FilterNotes(query);
            NotePopup.IsOpen = !string.IsNullOrEmpty(query) && NoteListBox.Items.Count > 0;
        }

        private void OnToggleDropdown(object sender, RoutedEventArgs e)
        {
            if (NotePopup == null || NoteListBox == null) return;

            if (_notePopupWasOpen)
            {
                _notePopupWasOpen = false;
                NotePopup.IsOpen = false;
                return;
            }

            if (NotePopup.IsOpen)
            {
                NotePopup.IsOpen = false;
            }
            else
            {
                if (_selected_seller == null)
                {
                    AppDialog.Show("Seleccione primero un vendedor para buscar sus notas de entrega.", "Aviso");
                    return;
                }
                if (string.IsNullOrWhiteSpace(CmbMonth.SelectedItem as string))
                {
                    AppDialog.Show("Seleccione primero un mes para buscar notas de entrega.", "Aviso");
                    return;
                }
                FilterNotes(string.Empty);
                NotePopup.IsOpen = NoteListBox.Items.Count > 0;
                if (NoteListBox.Items.Count == 0)
                {
                    AppDialog.Show("No se encontraron notas de entrega para este vendedor en el mes indicado.", "Aviso");
                }
            }
        }

        private void FilterNotes(string query)
        {
            List<note_combo_item> filtered;
            if (string.IsNullOrEmpty(query))
            {
                filtered = _all_combo_items;
            }
            else
            {
                filtered = _all_combo_items
                    .Where(n => (n.note_number?.ToLower().Contains(query) ?? false) ||
                                (n.customer_name?.ToLower().Contains(query) ?? false) ||
                                (n.note_type?.ToLower().Contains(query) ?? false))
                    .ToList();
            }

            NoteListBox.ItemsSource = filtered;
        }

        private void OnNoteListSelected(object sender, SelectionChangedEventArgs e)
        {
            if (NoteListBox?.SelectedItem is note_combo_item item)
            {
                _ = LoadSourceAsync(item);
                NoteListBox.SelectedItem = null;
            }
        }

        private async Task LoadSourceAsync(note_combo_item item)
        {
            try
            {
                var source = await _vm.get_credit_source_async(item.note_number);
                if (source == null)
                {
                    AppDialog.Show($"No se encontro la nota de entrega {item.note_number}.", "Aviso");
                    return;
                }
                if (source.status == "Anulada")
                {
                    AppDialog.Show("La nota de entrega esta anulada; no se pueden registrar devoluciones.", "Aviso");
                    return;
                }

                _source = source;
                NoteTextBox.Text = item.Display;
                NotePopup.IsOpen = false;
                NoteTextBox.IsReadOnly = true;
                BtnClearNote.Visibility = Visibility.Visible;

                CustomerText.Text = $"{source.customer_name}  ({source.customer_code})";
                SellerText.Text = "Vendedor: " + source.seller_name;

                // Tipo heredado de la nota de entrega que se revierte. Se lee de la nota y no del
                // desplegable para que no pueda quedar desactualizado si el tipo cambia despues.
                string type_name = !string.IsNullOrWhiteSpace(source.note_type) ? source.note_type : item.note_type;
                string typeLabel = string.IsNullOrWhiteSpace(type_name) ? string.Empty : $"   |   Tipo: {type_name}";

                string discount_note = source.has_discount
                    ? $"   |   Descuento nota: {source.discount_percentage:N2}%"
                    : string.Empty;

                NoteInfoText.Text = $"Nota: {source.note_number}{typeLabel}   |   Fecha: {source.creation_date:dd/MM/yyyy}   |   Total: {source.adjusted_total_usd:N2} USD{discount_note}   |   Ya devuelto: {source.already_returned_usd:N2} USD   |   Disponible: {source.available_usd:N2} USD";
                NoteInfoBorder.Visibility = Visibility.Visible;

                var rows = source.lines
                    .Where(l => l.remaining_quantity > 0)
                    .Select(line => new credit_note_edit_row
                    {
                        id_product = line.id_product,
                        id_promotion = line.id_promotion,
                        code = line.code,
                        name = line.name,
                        unit_price_usd = line.net_unit_price_usd,
                        delivered_quantity = line.delivered_quantity,
                        already_returned_quantity = line.already_returned_quantity,
                        remaining_quantity = line.remaining_quantity
                    })
                    .ToList();

                foreach (var row in rows) row.PropertyChanged += OnRowPropertyChanged;

                ItemsGrid.ItemsSource = rows;
                RecalcTotal();

                if (rows.Count == 0)
                    AppDialog.Show("Esta nota ya fue devuelta por completo; no quedan cantidades disponibles.", "Aviso");
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private void OnClearNoteClick(object sender, RoutedEventArgs e)
        {
            ResetDevolucionState();
            FilterNotes(string.Empty);
            NotePopup.IsOpen = NoteListBox.Items.Count > 0;
        }

        private void OnQuantityPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OnReturnQuantityLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (box.DataContext is not credit_note_edit_row row) return;

            string raw = box.Text?.Trim() ?? string.Empty;
            if (!int.TryParse(raw, out int parsed) || parsed < 0)
                parsed = 0;

            row.return_quantity = parsed;
            box.Text = row.return_quantity.ToString();
            RecalcTotal();
        }

        private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(credit_note_edit_row.return_quantity)) return;
            RecalcTotal();
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e) => RecalcTotal();

        private void RecalcTotal()
        {
            if (TotalText == null) return;
            var rows = (ItemsGrid?.ItemsSource as IEnumerable<credit_note_edit_row>)?.ToList() ?? new List<credit_note_edit_row>();

            bool capped = ApplyAvailableCap(rows);
            TotalText.Text = Money.round(rows.Sum(r => r.subtotal_usd)).ToString("N2");

            if (CapText != null)
            {
                CapText.Visibility = capped ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Limita el total a lo que le queda por devolver de la nota, sin avisarle al usuario.
        ///
        /// No se toca la cantidad devuelta, se reparte el mismo factor entre todas las lineas para
        /// que el ajuste caiga parejo, y el residuo del redondeo se absorbe en la ultima linea con
        /// cantidad para que la suma final sea exactamente el saldo disponible.
        ///
        /// Devuelve true si hubo que recortar. El mismo calculo se repite en el servicio al guardar,
        /// que es quien manda; aca esta para que lo que se ve antes de confirmar ya sea el importe final.
        /// </summary>
        private bool ApplyAvailableCap(List<credit_note_edit_row> rows)
        {
            foreach (var row in rows)
            {
                row.set_cap_factor(1m);
                row.set_subtotal_override(null);
            }

            // El obsequio no cuelga de una nota de entrega, asi que no tiene saldo que respetar.
            if (_is_gift || _source == null || rows.Count == 0) return false;

            decimal available_usd = _source.available_usd;
            decimal raw_total = Money.round(rows.Sum(r => r.subtotal_usd));
            if (raw_total <= available_usd + 0.005m) return false;

            var weighted = rows
                .Where(r => r.return_quantity > 0 && r.subtotal_usd > 0m)
                .ToList();
            if (weighted.Count == 0) return false;

            decimal factor = available_usd / raw_total;
            foreach (var row in weighted)
            {
                row.set_cap_factor(factor);
            }

            decimal residue = Money.round(available_usd - rows.Sum(r => r.subtotal_usd));
            if (residue != 0m)
            {
                var absorber = weighted[weighted.Count - 1];
                absorber.set_subtotal_override(absorber.subtotal_usd + residue);
            }

            return true;
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e) => await SaveAsync(false);

        private async void OnSavePdfClick(object sender, RoutedEventArgs e) => await SaveAsync(true);

        private async Task SaveAsync(bool with_pdf)
        {
            try
            {
                credit_note new_note;
                List<credit_note_edit_row> rows;

                if (_is_gift)
                {
                    if (CmbSeller.SelectedValue is not int id_seller)
                    {
                        AppDialog.Show("Seleccione el vendedor que emite la nota de credito.", "Aviso");
                        return;
                    }
                    if (_selected_customer == null)
                    {
                        AppDialog.Show("Busque y seleccione el cliente del obsequio.", "Aviso");
                        return;
                    }

                    rows = _gift_items.Where(r => r.return_quantity > 0).ToList();
                    if (rows.Count == 0)
                    {
                        AppDialog.Show("Debe agregar al menos un producto a obsequiar con cantidad mayor a cero.", "Aviso");
                        return;
                    }

                    foreach (var r in rows)
                    {
                        if (r.return_quantity > r.remaining_quantity)
                        {
                            AppDialog.Show($"La cantidad a obsequiar de {r.name} ({r.return_quantity}) supera el stock disponible ({r.remaining_quantity}).", "Aviso");
                            return;
                        }
                    }

                    decimal total = rows.Sum(r => r.subtotal_usd);
                    string correlative = await _vm.generate_credit_correlative_async(id_seller);

                    new_note = new credit_note(
                        note_number: correlative,
                        creation_date: DateTime.UtcNow,
                        id_delivery_note: null,
                        id_seller: id_seller,
                        id_customer: _selected_customer.id_customer,
                        total_amount_usd: total,
                        status: "Registrada",
                        category: "Obsequio")
                    {
                        observations = ObsBox.Text?.Trim()
                    };
                }
                else
                {
                    if (_selected_seller == null)
                    {
                        AppDialog.Show("Seleccione el vendedor que emite la nota de credito.", "Aviso");
                        return;
                    }
                    if (_source == null)
                    {
                        AppDialog.Show("Busque primero la nota de entrega a la que corresponde la devolucion.", "Aviso");
                        return;
                    }

                    rows = ((ItemsGrid.ItemsSource as IEnumerable<credit_note_edit_row>) ?? Enumerable.Empty<credit_note_edit_row>())
                        .Where(r => r.return_quantity > 0)
                        .ToList();

                    if (rows.Count == 0)
                    {
                        AppDialog.Show("Debe indicar al menos una cantidad a devolver.", "Aviso");
                        return;
                    }

                    decimal total = rows.Sum(r => r.subtotal_usd);
                    string correlative = await _vm.generate_credit_correlative_async(_source.id_seller);

                    new_note = new credit_note(
                        note_number: correlative,
                        // Una nota de credito se emite el dia en que se recibe la mercancia de vuelta,
                        // no el dia en que se emitio la nota que se esta revirtiendo. Anclar la NC a la
                        // fecha de la entrega arrastra al karded, al abono negativo y al filtro por mes
                        // del modulo: una devolucion de una nota de marzo caeria en el mes que se registro.
                        creation_date: DateTime.UtcNow,
                        id_delivery_note: _source.id_delivery_note,
                        id_seller: _source.id_seller,
                        id_customer: _source.id_customer,
                        total_amount_usd: total,
                        status: "Registrada",
                        category: "Devolucion")
                    {
                        observations = ObsBox.Text?.Trim()
                    };
                }

                var details = rows.Select(r => new credit_note_detail(
                    id_credit_note: 0,
                    id_product: r.id_product,
                    id_promotion: r.id_promotion,
                    quantity: r.return_quantity,
                    unit_price_usd: r.unit_price_usd,
                    subtotal_usd: r.subtotal_usd)).ToList();

                var created = await _vm.save_credit_note_async(new_note, details);

                if (with_pdf)
                {
                    var pair = await _vm.get_printable_pair_async(created);
                    if (pair.original != null)
                        NotePdfGenerator.generate(pair.original, pair.credit);
                    else
                        NotePdfGenerator.generate(pair.credit);
                }

                CreditNoteCreated?.Invoke(this, EventArgs.Empty);
                AppDialog.Show(_is_gift
                    ? $"Nota de credito {created.note_number} por obsequio registrada por {created.total_amount_usd:N2} USD."
                    : $"Nota de credito {created.note_number} registrada por {created.total_amount_usd:N2} USD.", "Exito");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }
    }
}