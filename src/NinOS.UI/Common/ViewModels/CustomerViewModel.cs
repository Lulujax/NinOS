using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Repositories.Interfaces;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class CustomerRowDto
    {
        public string IdDisplay { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string Rif { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FiscalAddress { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public int? IdZona { get; set; }
        public string ZonaName { get; set; } = string.Empty;
        public string ZonaCode { get; set; } = string.Empty;
        public customer? CustomerRef { get; set; }

        public string EffectiveDeliveryAddress =>
            string.IsNullOrWhiteSpace(DeliveryAddress) ? FiscalAddress : DeliveryAddress;
    }

    public class CustomerTabItem : ViewModelBase
    {
        private string _header = string.Empty;
        public int? IdZona { get; set; }
        public string? ZonaCode { get; set; }

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

        public ObservableCollection<CustomerRowDto> Customers { get; } = new ObservableCollection<CustomerRowDto>();
    }

    public class CustomerViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;
        private readonly IGenericRepository<seller> _sellerRepository;
        private readonly IZonaService _zonaService;
        private readonly Dictionary<string, string> _sellerPrefixMap;
        private readonly Dictionary<string, long> _sellerLastNumberMap;
        private List<CustomerRowDto> _allCustomersSource;
        private CustomerRowDto? _editingCustomer;
        private bool _isLoading;
        private string _errorMessage = string.Empty;

        private string _searchQuery = string.Empty;
        private int _selectedTabIndex;
        private CustomerTabItem? _selectedTab;
        private string _newCustomerCode = string.Empty;
        private string _newBusinessName = string.Empty;
        private string _newRifNumber = string.Empty;
        private string _newRifType = "J";
        private string _newContactName = string.Empty;
        private string _newPhoneNumber = string.Empty;
        private string _newFiscalAddress = string.Empty;
        private string _newDeliveryAddress = string.Empty;
        private string _newSellerName = string.Empty;
        private int? _newZonaId;
        private bool _canEditSeller = true;
        private bool _canEditCode = false;
        private string _addOrEditTitle = "Agregar Cliente";
        private string _saveButtonText = "Agregar Cliente";

        // Pestañas dinámicas de Zonas
        public ObservableCollection<CustomerTabItem> ZonaTabs { get; } = new ObservableCollection<CustomerTabItem>();

        // Colección global
        public ObservableCollection<CustomerRowDto> AllCustomers { get; }

        // Colecciones legacy mantenidas por retrocompatibilidad
        public ObservableCollection<CustomerRowDto> IsabelicaCustomers { get; }
        public ObservableCollection<CustomerRowDto> SanDiegoCustomers { get; }
        public ObservableCollection<CustomerRowDto> TocuyitoCustomers { get; }
        public ObservableCollection<CustomerRowDto> CentroCustomers { get; }
        public ObservableCollection<CustomerRowDto> FlorAmarilloCustomers { get; }
        public ObservableCollection<CustomerRowDto> MaracayCustomers { get; }

        public ObservableCollection<CustomerRowDto> AnaisCustomers { get; }
        public ObservableCollection<CustomerRowDto> SandraCustomers { get; }
        public ObservableCollection<CustomerRowDto> AlejandraCustomers { get; }
        public ObservableCollection<CustomerRowDto> JuanLuisCustomers { get; }

        public ObservableCollection<string> SellerOptions { get; }
        public ObservableCollection<zona> ZonasOptions { get; } = new ObservableCollection<zona>();
        public Action? EditZonasRequested { get; set; }
        public ObservableCollection<string> RifTypeOptions { get; }
        public Action? OnCloseAddCustomerWindow { get; set; }
        public Action? OnRequestAddCustomerWindow { get; set; }
        public Action<CustomerRowDto>? OnRequestEditCustomerWindow { get; set; }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    on_property_changed();
                    FilterCustomers();
                }
            }
        }

        public CustomerTabItem? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    on_property_changed();
                    int idx = value != null ? ZonaTabs.IndexOf(value) : -1;
                    if (idx >= 0 && _selectedTabIndex != idx)
                    {
                        _selectedTabIndex = idx;
                        on_property_changed(nameof(SelectedTabIndex));
                    }
                    SetDefaultZonaFromTab();
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    on_property_changed();
                    if (value >= 0 && value < ZonaTabs.Count)
                    {
                        SelectedTab = ZonaTabs[value];
                    }
                    SetDefaultZonaFromTab();
                }
            }
        }

        public string NewCustomerCode
        {
            get => _newCustomerCode;
            set { _newCustomerCode = value; on_property_changed(); }
        }

        public bool CanEditCode
        {
            get => _canEditCode;
            set { _canEditCode = value; on_property_changed(); }
        }

        public string NewBusinessName
        {
            get => _newBusinessName;
            set
            {
                _newBusinessName = value;
                on_property_changed();
                (SaveCustomerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string NewRifType
        {
            get => _newRifType;
            set
            {
                _newRifType = value;
                on_property_changed();
                (SaveCustomerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string NewRifNumber
        {
            get => _newRifNumber;
            set
            {
                _newRifNumber = value;
                on_property_changed();
                (SaveCustomerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string NewContactName
        {
            get => _newContactName;
            set { _newContactName = value; on_property_changed(); }
        }

        public string NewPhoneNumber
        {
            get => _newPhoneNumber;
            set { _newPhoneNumber = value; on_property_changed(); }
        }

        public string NewFiscalAddress
        {
            get => _newFiscalAddress;
            set { _newFiscalAddress = value; on_property_changed(); }
        }

        public string NewDeliveryAddress
        {
            get => _newDeliveryAddress;
            set { _newDeliveryAddress = value; on_property_changed(); }
        }

        public string NewSellerName
        {
            get => _newSellerName;
            set
            {
                _newSellerName = value;
                on_property_changed();
            }
        }

        public int? NewZonaId
        {
            get => _newZonaId;
            set
            {
                if (_newZonaId != value)
                {
                    _newZonaId = value;
                    on_property_changed();
                    (SaveCustomerCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanEditSeller
        {
            get => _canEditSeller;
            set { _canEditSeller = value; on_property_changed(); }
        }

        public string AddOrEditTitle
        {
            get => _addOrEditTitle;
            set { _addOrEditTitle = value; on_property_changed(); }
        }

        public string SaveButtonText
        {
            get => _saveButtonText;
            set { _saveButtonText = value; on_property_changed(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; on_property_changed(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; on_property_changed(); }
        }

        public ICommand SaveCustomerCommand { get; }
        public ICommand AddCustomerCommand { get; }
        public ICommand EditCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand EditZonasCommand { get; }
        public ICommand LoadCustomersCommand { get; }

        public CustomerViewModel(
            ICustomerService customerService,
            IGenericRepository<seller> sellerRepository,
            IZonaService zonaService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _sellerRepository = sellerRepository ?? throw new ArgumentNullException(nameof(sellerRepository));
            _zonaService = zonaService ?? throw new ArgumentNullException(nameof(zonaService));

            _sellerPrefixMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Sandra", "001" },
                { "Anais", "002" },
                { "Alejandra", "003" },
                { "Juan Luis", "004" }
            };

            _sellerLastNumberMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            _allCustomersSource = new List<CustomerRowDto>();
            _isLoading = false;

            AllCustomers = new ObservableCollection<CustomerRowDto>();
            IsabelicaCustomers = new ObservableCollection<CustomerRowDto>();
            SanDiegoCustomers = new ObservableCollection<CustomerRowDto>();
            TocuyitoCustomers = new ObservableCollection<CustomerRowDto>();
            CentroCustomers = new ObservableCollection<CustomerRowDto>();
            FlorAmarilloCustomers = new ObservableCollection<CustomerRowDto>();
            MaracayCustomers = new ObservableCollection<CustomerRowDto>();

            AnaisCustomers = new ObservableCollection<CustomerRowDto>();
            SandraCustomers = new ObservableCollection<CustomerRowDto>();
            AlejandraCustomers = new ObservableCollection<CustomerRowDto>();
            JuanLuisCustomers = new ObservableCollection<CustomerRowDto>();

            SellerOptions = new ObservableCollection<string> { "Sandra", "Anais", "Alejandra", "Juan Luis" };
            RifTypeOptions = new ObservableCollection<string> { "V", "E", "J", "G", "P" };
            _newSellerName = "Anais";
            _newRifType = "J";

            SaveCustomerCommand = new RelayCommand(ExecuteSaveCustomer, CanExecuteSaveCustomer);
            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            EditCustomerCommand = new RelayCommand(ExecuteEditCustomer, CanExecuteEditCustomer);
            DeleteCustomerCommand = new RelayCommand(ExecuteDeleteCustomer, CanExecuteDeleteCustomer);
            EditZonasCommand = new RelayCommand(ExecuteEditZonas);
            LoadCustomersCommand = new RelayCommand(ExecuteLoadCustomers);

            AppDataEvents.CatalogsChanged += OnCatalogsChanged;

            LoadCustomersAsync();
        }

        private void OnCatalogsChanged()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.InvokeAsync(LoadCustomersAsync);
            }
            else
            {
                LoadCustomersAsync();
            }
        }

        private void ExecuteEditZonas(object? parameter)
        {
            EditZonasRequested?.Invoke();
            _ = LoadZonasAsync();
        }

        private void ExecuteLoadCustomers(object? parameter)
        {
            LoadCustomersAsync();
        }

        public void refresh_data() => LoadCustomersAsync();

        public static string FormatTabHeader(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            // Si el nombre viene con prefijo numérico como "01 Zona 1" o "05 Zona 5",
            // quitamos el número inicial para mostrar solo el nombre limpio.
            var match = Regex.Match(name.Trim(), @"^\d+\s*[-_.]?\s*(.*)$");
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                return match.Groups[1].Value.Trim();
            }
            return name.Trim();
        }

        public void SyncZonaTabs(List<zona> activeZonas)
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => SyncZonaTabs(activeZonas));
                return;
            }

            int? previouslySelectedZonaId = SelectedTab?.IdZona;
            bool wasTodosSelected = SelectedTab != null && SelectedTab.IdZona == null;

            // 1. Asegurar pestaña "Todos" en posición 0
            CustomerTabItem? todosTab = ZonaTabs.FirstOrDefault(t => t.IdZona == null);
            if (todosTab == null)
            {
                todosTab = new CustomerTabItem { Header = "Todos", IdZona = null };
                ZonaTabs.Insert(0, todosTab);
            }
            else
            {
                todosTab.Header = "Todos";
            }

            var orderedZonas = activeZonas.OrderBy(z => z.code).ToList();
            var activeIds = new HashSet<int>(orderedZonas.Select(z => z.id_zona));

            // 2. Eliminar pestañas de zonas que ya no existen o fueron desactivadas
            for (int i = ZonaTabs.Count - 1; i >= 0; i--)
            {
                var tab = ZonaTabs[i];
                if (tab.IdZona.HasValue && !activeIds.Contains(tab.IdZona.Value))
                {
                    ZonaTabs.RemoveAt(i);
                }
            }

            // 3. Agregar o actualizar pestañas activas en orden
            for (int i = 0; i < orderedZonas.Count; i++)
            {
                var z = orderedZonas[i];
                int targetIndex = i + 1; // 0 es Todos
                string cleanHeader = FormatTabHeader(z.name);

                var existingTab = ZonaTabs.FirstOrDefault(t => t.IdZona == z.id_zona);
                if (existingTab != null)
                {
                    existingTab.Header = cleanHeader;
                    existingTab.ZonaCode = z.code;
                    int currentIndex = ZonaTabs.IndexOf(existingTab);
                    if (currentIndex != targetIndex && targetIndex < ZonaTabs.Count)
                    {
                        ZonaTabs.Move(currentIndex, targetIndex);
                    }
                }
                else
                {
                    var newTab = new CustomerTabItem
                    {
                        IdZona = z.id_zona,
                        ZonaCode = z.code,
                        Header = cleanHeader
                    };
                    if (targetIndex <= ZonaTabs.Count)
                        ZonaTabs.Insert(targetIndex, newTab);
                    else
                        ZonaTabs.Add(newTab);
                }
            }

            // 4. Restaurar pestaña seleccionada
            if (wasTodosSelected)
            {
                SelectedTab = todosTab;
            }
            else if (previouslySelectedZonaId.HasValue)
            {
                SelectedTab = ZonaTabs.FirstOrDefault(t => t.IdZona == previouslySelectedZonaId.Value) ?? todosTab;
            }
            else if (SelectedTab == null && ZonaTabs.Count > 0)
            {
                SelectedTab = ZonaTabs[0];
            }
        }

        public async Task LoadZonasAsync()
        {
            try
            {
                var zonas = await _zonaService.GetActiveAsync();
                var activeList = zonas?.ToList() ?? new List<zona>();

                ZonasOptions.Clear();
                foreach (var z in activeList)
                {
                    ZonasOptions.Add(z);
                }

                SyncZonaTabs(activeList);
                FilterCustomers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading zonas: {ErrorText.Get(ex)}");
            }
        }

        private async void LoadCustomersAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await LoadZonasAsync();

                IEnumerable<customer> customers = await _customerService.GetAllCustomersAsync();

                try
                {
                    seller[] sellers = await _sellerRepository.get_all_async();
                    foreach (seller s in sellers)
                    {
                        if (!string.IsNullOrWhiteSpace(s.full_name) && !string.IsNullOrWhiteSpace(s.seller_code))
                        {
                            _sellerPrefixMap[s.full_name] = s.seller_code;
                        }
                        if (!string.IsNullOrWhiteSpace(s.full_name))
                        {
                            _sellerLastNumberMap[s.full_name] = s.last_customer_number;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading sellers: {ErrorText.Get(ex)}");
                }

                if (customers == null)
                {
                    _allCustomersSource = new List<CustomerRowDto>();
                    ErrorMessage = "No se encontraron clientes.";
                }
                else
                {
                    _allCustomersSource = customers.Select(c => new CustomerRowDto
                    {
                        IdDisplay = c.id_customer.ToString(),
                        CustomerCode = c.customer_code ?? string.Empty,
                        BusinessName = c.business_name ?? string.Empty,
                        Rif = c.rif ?? string.Empty,
                        ContactName = c.contact_name ?? string.Empty,
                        PhoneNumber = c.phone_number ?? string.Empty,
                        FiscalAddress = c.fiscal_address ?? string.Empty,
                        DeliveryAddress = c.delivery_address ?? string.Empty,
                        SellerName = c.seller_name ?? string.Empty,
                        IdZona = c.id_zona,
                        ZonaName = c.zona?.name ?? (c.id_zona.HasValue ? $"Zona {c.id_zona}" : "-"),
                        ZonaCode = c.zona?.code ?? string.Empty,
                        CustomerRef = c
                    }).ToList();
                }

                FilterCustomers();
                await GenerateNextCustomerCodeAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar clientes: {ErrorText.Get(ex)}";
                _allCustomersSource = new List<CustomerRowDto>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateNextCustomerCodeAsync()
        {
            if (_editingCustomer != null) return;
            try
            {
                NewCustomerCode = await _customerService.GetNextCustomerCodeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating customer code: {ErrorText.Get(ex)}");
                NewCustomerCode = "00001";
            }
        }

        private void SetDefaultZonaFromTab()
        {
            if (_editingCustomer != null) return;

            if (_selectedTab?.IdZona != null)
            {
                NewZonaId = _selectedTab.IdZona;
            }
        }

        private void FilterCustomers()
        {
            try
            {
                string query = _searchQuery?.Trim().ToLower() ?? string.Empty;
                List<CustomerRowDto> filtered;

                if (string.IsNullOrWhiteSpace(query))
                {
                    filtered = _allCustomersSource.ToList();
                }
                else
                {
                    filtered = _allCustomersSource.Where(c =>
                        (c.CustomerCode?.ToLower().Contains(query) ?? false) ||
                        (c.BusinessName?.ToLower().Contains(query) ?? false) ||
                        (c.Rif?.ToLower().Contains(query) ?? false) ||
                        (c.ContactName?.ToLower().Contains(query) ?? false) ||
                        (c.PhoneNumber?.ToLower().Contains(query) ?? false) ||
                        (c.FiscalAddress?.ToLower().Contains(query) ?? false) ||
                        (c.EffectiveDeliveryAddress?.ToLower().Contains(query) ?? false) ||
                        (c.SellerName?.ToLower().Contains(query) ?? false) ||
                        (c.ZonaName?.ToLower().Contains(query) ?? false) ||
                        (c.ZonaCode?.ToLower().Contains(query) ?? false)
                    ).ToList();
                }

                UpdateCollection(AllCustomers, filtered);

                // Actualizar pestañas dinámicas de ZonaTabs
                foreach (var tab in ZonaTabs)
                {
                    if (tab.IdZona == null)
                    {
                        UpdateCollection(tab.Customers, filtered);
                    }
                    else
                    {
                        var tabItems = filtered.Where(c => c.IdZona == tab.IdZona.Value).ToList();
                        UpdateCollection(tab.Customers, tabItems);
                    }
                }

                // Colecciones legacy divididas por Zona
                UpdateCollection(IsabelicaCustomers, filtered.Where(c => c.ZonaCode == "01" || c.ZonaName.Contains("Zona 1", StringComparison.OrdinalIgnoreCase) || c.ZonaName.Contains("Isabelica", StringComparison.OrdinalIgnoreCase)).ToList());
                UpdateCollection(SanDiegoCustomers, filtered.Where(c => c.ZonaCode == "02" || c.ZonaName.Contains("Zona 2", StringComparison.OrdinalIgnoreCase) || c.ZonaName.Contains("San Diego", StringComparison.OrdinalIgnoreCase)).ToList());
                UpdateCollection(TocuyitoCustomers, filtered.Where(c => c.ZonaCode == "03" || c.ZonaName.Contains("Zona 3", StringComparison.OrdinalIgnoreCase) || c.ZonaName.Contains("Tocuyito", StringComparison.OrdinalIgnoreCase)).ToList());
                UpdateCollection(CentroCustomers, filtered.Where(c => c.ZonaCode == "04" || c.ZonaName.Contains("Zona 4", StringComparison.OrdinalIgnoreCase) || c.ZonaName.Contains("Centro", StringComparison.OrdinalIgnoreCase)).ToList());
                UpdateCollection(FlorAmarilloCustomers, filtered.Where(c => c.ZonaCode == "05" || c.ZonaName.Contains("Zona 5", StringComparison.OrdinalIgnoreCase) || c.ZonaName.Contains("Flor Amarillo", StringComparison.OrdinalIgnoreCase)).ToList());
                UpdateCollection(MaracayCustomers, filtered.Where(c => c.ZonaCode == "06" || c.ZonaName.Contains("Maracay", StringComparison.OrdinalIgnoreCase)).ToList());

                // Colecciones legacy por vendedor
                UpdateCollection(AnaisCustomers, filtered.Where(c => string.Equals(c.SellerName?.Trim(), "Anais", StringComparison.OrdinalIgnoreCase)).ToList());
                UpdateCollection(SandraCustomers, filtered.Where(c => string.Equals(c.SellerName?.Trim(), "Sandra", StringComparison.OrdinalIgnoreCase)).ToList());
                UpdateCollection(AlejandraCustomers, filtered.Where(c => string.Equals(c.SellerName?.Trim(), "Alejandra", StringComparison.OrdinalIgnoreCase)).ToList());
                UpdateCollection(JuanLuisCustomers, filtered.Where(c => string.Equals(c.SellerName?.Trim(), "Juan Luis", StringComparison.OrdinalIgnoreCase)).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering customers: {ErrorText.Get(ex)}");
            }
        }

        private void UpdateCollection(ObservableCollection<CustomerRowDto> collection, List<CustomerRowDto> items)
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => UpdateCollection(collection, items));
                return;
            }
            collection.Clear();
            foreach (CustomerRowDto item in items)
            {
                collection.Add(item);
            }
        }

        private bool CanExecuteSaveCustomer(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(_newCustomerCode) &&
                   !string.IsNullOrWhiteSpace(_newBusinessName) &&
                   !string.IsNullOrWhiteSpace(_newRifNumber) &&
                   _newZonaId.HasValue;
        }

        private async void ExecuteSaveCustomer(object? parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(_newCustomerCode) ||
                    string.IsNullOrWhiteSpace(_newBusinessName) ||
                    string.IsNullOrWhiteSpace(_newRifNumber))
                {
                    ErrorMessage = "Tienes que llenar los campos obligatorios: Razón Social (Negocio/Nombre) e Identificación.";
                    return;
                }

                if (!_newZonaId.HasValue)
                {
                    ErrorMessage = "Debes seleccionar una Zona obligatoriamente.";
                    return;
                }

                string fullRif = string.IsNullOrWhiteSpace(_newRifNumber) ? "" : $"{_newRifType}-{_newRifNumber}";

                customer newCustomer = new customer(
                    _newCustomerCode,
                    _newBusinessName,
                    fullRif,
                    _newContactName,
                    _newPhoneNumber,
                    _newFiscalAddress,
                    _newDeliveryAddress,
                    _newSellerName
                )
                {
                    id_zona = _newZonaId
                };

                if (_editingCustomer != null)
                {
                    customer existing = _editingCustomer.CustomerRef!;
                    existing.customer_code = _newCustomerCode;
                    existing.business_name = _newBusinessName;
                    existing.rif = fullRif;
                    existing.contact_name = _newContactName;
                    existing.phone_number = _newPhoneNumber;
                    existing.fiscal_address = _newFiscalAddress;
                    existing.delivery_address = _newDeliveryAddress;
                    existing.seller_name = _newSellerName;
                    existing.id_zona = _newZonaId;

                    await _customerService.UpdateCustomerAsync(existing);
                }
                else
                {
                    await _customerService.AddCustomerAsync(newCustomer);
                }

                ClearForm();
                OnCloseAddCustomerWindow?.Invoke();
                LoadCustomersAsync();
                AppDataEvents.raise_catalogs_changed();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar: {ErrorText.Get(ex)}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteAddCustomer(object? parameter)
        {
            ClearForm();
            _editingCustomer = null;
            _canEditCode = false;
            AddOrEditTitle = "Agregar Cliente";
            SaveButtonText = "Agregar Cliente";
            CanEditSeller = true;
            SetDefaultZonaFromTab();
            await GenerateNextCustomerCodeAsync();
            OnRequestAddCustomerWindow?.Invoke();
        }

        private bool CanExecuteEditCustomer(object? parameter)
        {
            return parameter is CustomerRowDto;
        }

        private void ExecuteEditCustomer(object? parameter)
        {
            if (parameter is CustomerRowDto selected)
            {
                StartEditCustomer(selected);
            }
        }

        public void StartEditCustomer(CustomerRowDto selected)
        {
            _editingCustomer = selected;
            _canEditCode = true;
            NewCustomerCode = selected.CustomerCode;
            NewBusinessName = selected.BusinessName;

            if (!string.IsNullOrEmpty(selected.Rif) && selected.Rif.Contains("-"))
            {
                string[] parts = selected.Rif.Split('-');
                NewRifType = parts.Length > 0 ? parts[0] : "J";
                NewRifNumber = parts.Length > 1 ? parts[1] : "";
            }
            else
            {
                NewRifType = "J";
                NewRifNumber = selected.Rif ?? "";
            }

            NewContactName = selected.ContactName;
            NewPhoneNumber = selected.PhoneNumber;
            NewFiscalAddress = selected.FiscalAddress;
            NewDeliveryAddress = selected.DeliveryAddress;
            NewSellerName = selected.SellerName;
            NewZonaId = selected.CustomerRef?.id_zona ?? selected.IdZona;
            CanEditSeller = false;
            AddOrEditTitle = "Editar Cliente";
            SaveButtonText = "Guardar Cambios";
            (SaveCustomerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            OnRequestEditCustomerWindow?.Invoke(selected);
        }

        private bool CanExecuteDeleteCustomer(object? parameter)
        {
            return parameter is CustomerRowDto;
        }

        private async void ExecuteDeleteCustomer(object? parameter)
        {
            if (parameter is CustomerRowDto selected && selected.CustomerRef != null)
            {
                MessageBoxResult confirm = AppDialog.Show(
                    $"¿Está seguro de eliminar el cliente \"{selected.BusinessName}\"?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;
                    await _customerService.SoftDeleteCustomerAsync(selected.CustomerRef.id_customer, null);
                    LoadCustomersAsync();
                    AppDataEvents.raise_catalogs_changed();
                    AppDialog.Show($"Cliente \"{selected.BusinessName}\" eliminado exitosamente.",
                        "Cliente eliminado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al eliminar: {ErrorText.Get(ex)}";
                    AppDialog.Show($"Error al eliminar el cliente: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ClearForm()
        {
            NewCustomerCode = string.Empty;
            NewBusinessName = string.Empty;
            NewRifNumber = string.Empty;
            NewRifType = "J";
            NewContactName = string.Empty;
            NewPhoneNumber = string.Empty;
            NewFiscalAddress = string.Empty;
            NewDeliveryAddress = string.Empty;
            NewSellerName = "Anais";
            NewZonaId = null;
            _editingCustomer = null;
            _canEditCode = false;
            SetDefaultZonaFromTab();
            _ = GenerateNextCustomerCodeAsync();
            (SaveCustomerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
