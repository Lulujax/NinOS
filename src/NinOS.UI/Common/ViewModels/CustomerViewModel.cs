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
        public customer? CustomerRef { get; set; }
    }

    public class CustomerViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;
        private List<CustomerRowDto> _allCustomersSource;
        private CustomerRowDto? _editingCustomer;
        private bool _isLoading;
        private string _errorMessage = string.Empty;

        private string _searchQuery = string.Empty;
        private int _selectedTabIndex;
        private string _newCustomerCode = string.Empty;
        private string _newBusinessName = string.Empty;
        private string _newRif = string.Empty;
        private string _newContactName = string.Empty;
        private string _newPhoneNumber = string.Empty;
        private string _newFiscalAddress = string.Empty;
        private string _newDeliveryAddress = string.Empty;
        private string _newSellerName = string.Empty;
        private bool _canEditSeller = true;
        private string _addOrEditTitle = "Agregar Cliente";
        private string _saveButtonText = "Agregar Cliente";

        public ObservableCollection<CustomerRowDto> AllCustomers { get; }
        public ObservableCollection<CustomerRowDto> JuanCustomers { get; }
        public ObservableCollection<CustomerRowDto> SandraCustomers { get; }
        public ObservableCollection<CustomerRowDto> AnaisCustomers { get; }
        public ObservableCollection<string> SellerOptions { get; }

        public Action? OnRequestAddCustomerWindow { get; set; }
        public Action<CustomerRowDto>? OnRequestEditCustomerWindow { get; set; }
        public Action? OnCloseAddCustomerWindow { get; set; }

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

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    on_property_changed();
                    FilterCustomers();
                }
            }
        }

        public string NewCustomerCode
        {
            get => _newCustomerCode;
            set { _newCustomerCode = value; on_property_changed(); }
        }

        public string NewBusinessName
        {
            get => _newBusinessName;
            set { _newBusinessName = value; on_property_changed(); }
        }

        public string NewRif
        {
            get => _newRif;
            set { _newRif = value; on_property_changed(); }
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
            set { _newSellerName = value; on_property_changed(); }
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
        public ICommand LoadCustomersCommand { get; }

        public CustomerViewModel(ICustomerService customerService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _allCustomersSource = new List<CustomerRowDto>();
            _isLoading = false;

            AllCustomers = new ObservableCollection<CustomerRowDto>();
            JuanCustomers = new ObservableCollection<CustomerRowDto>();
            SandraCustomers = new ObservableCollection<CustomerRowDto>();
            AnaisCustomers = new ObservableCollection<CustomerRowDto>();

            SellerOptions = new ObservableCollection<string> { "Sandra", "Anais", "Alejandra" };
            _newSellerName = "Anais";

            SaveCustomerCommand = new RelayCommand(ExecuteSaveCustomer, CanExecuteSaveCustomer);
            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            EditCustomerCommand = new RelayCommand(ExecuteEditCustomer, CanExecuteEditCustomer);
            DeleteCustomerCommand = new RelayCommand(ExecuteDeleteCustomer, CanExecuteDeleteCustomer);
            LoadCustomersCommand = new RelayCommand(ExecuteLoadCustomers);

            LoadCustomersAsync();
        }

        private void ExecuteLoadCustomers(object? parameter)
        {
            LoadCustomersAsync();
        }

        private async void LoadCustomersAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                
                IEnumerable<customer> customers = await _customerService.GetAllCustomersAsync();
                
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
                        CustomerRef = c
                    }).ToList();
                }

                FilterCustomers();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar clientes: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error loading customers: {ex.Message}");
                _allCustomersSource = new List<CustomerRowDto>();
            }
            finally
            {
                IsLoading = false;
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
                        (c.SellerName?.ToLower().Contains(query) ?? false)
                    ).ToList();
                }

                UpdateCollection(AllCustomers, filtered);

                if (_selectedTabIndex == 0)
                {
                    UpdateCollection(JuanCustomers, filtered.Where(c => c.SellerName == "Juan").ToList());
                    UpdateCollection(SandraCustomers, filtered.Where(c => c.SellerName == "Sandra").ToList());
                    UpdateCollection(AnaisCustomers, filtered.Where(c => c.SellerName == "Anais").ToList());
                }
                else if (_selectedTabIndex == 1)
                {
                    UpdateCollection(JuanCustomers, _allCustomersSource.Where(c => c.SellerName == "Juan").ToList());
                    UpdateCollection(SandraCustomers, _allCustomersSource.Where(c => c.SellerName == "Sandra").ToList());
                    UpdateCollection(AnaisCustomers, _allCustomersSource.Where(c => c.SellerName == "Anais").ToList());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering customers: {ex.Message}");
            }
        }

        private void UpdateCollection(ObservableCollection<CustomerRowDto> collection, List<CustomerRowDto> items)
        {
            collection.Clear();
            foreach (CustomerRowDto item in items)
            {
                collection.Add(item);
            }
        }

        private bool CanExecuteSaveCustomer(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(_newCustomerCode) &&
                   !string.IsNullOrWhiteSpace(_newBusinessName);
        }

        private async void ExecuteSaveCustomer(object? parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                customer newCustomer = new customer(
                    _newCustomerCode,
                    _newBusinessName,
                    _newRif,
                    _newContactName,
                    _newPhoneNumber,
                    _newFiscalAddress,
                    _newDeliveryAddress,
                    _newSellerName
                );

                if (_editingCustomer != null)
                {
                    customer existing = _editingCustomer.CustomerRef!;
                    existing.customer_code = _newCustomerCode;
                    existing.business_name = _newBusinessName;
                    existing.rif = _newRif;
                    existing.contact_name = _newContactName;
                    existing.phone_number = _newPhoneNumber;
                    existing.fiscal_address = _newFiscalAddress;
                    existing.delivery_address = _newDeliveryAddress;
                    existing.seller_name = _newSellerName;

                    await _customerService.UpdateCustomerAsync(existing);
                }
                else
                {
                    await _customerService.AddCustomerAsync(newCustomer);
                }

                ClearForm();
                OnCloseAddCustomerWindow?.Invoke();
                LoadCustomersAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error saving customer: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteAddCustomer(object? parameter)
        {
            ClearForm();
            _editingCustomer = null;
            AddOrEditTitle = "Agregar Cliente";
            SaveButtonText = "Agregar Cliente";
            CanEditSeller = true;
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
            NewCustomerCode = selected.CustomerCode;
            NewBusinessName = selected.BusinessName;
            NewRif = selected.Rif;
            NewContactName = selected.ContactName;
            NewPhoneNumber = selected.PhoneNumber;
            NewFiscalAddress = selected.FiscalAddress;
            NewDeliveryAddress = selected.DeliveryAddress;
            NewSellerName = selected.SellerName;
            CanEditSeller = false;
            AddOrEditTitle = "Editar Cliente";
            SaveButtonText = "Guardar Cambios";
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
                try
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;
                    await _customerService.DeleteCustomerAsync(selected.CustomerRef.id_customer);
                    LoadCustomersAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al eliminar: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"Error deleting customer: {ex.Message}");
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
            NewRif = string.Empty;
            NewContactName = string.Empty;
            NewPhoneNumber = string.Empty;
            NewFiscalAddress = string.Empty;
            NewDeliveryAddress = string.Empty;
            NewSellerName = "Anais";
            _editingCustomer = null;
        }
    }
}