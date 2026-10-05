using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class SellerRowDto
    {
        public int IdSeller { get; set; }
        public string SellerCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CustomerCodePrefix { get; set; } = string.Empty;
        public string AssignedZonasText { get; set; } = string.Empty;
        public List<int> AssignedZonaIds { get; set; } = new List<int>();
        public bool IsActive { get; set; } = true;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedReason { get; set; }
        public seller? SellerRef { get; set; }
    }

    public class SellerZonaCheckboxItem : ViewModelBase
    {
        public int IdZona { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayText => $"{Code} - {Name}";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; on_property_changed(); }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; on_property_changed(); }
        }

        private string _toolTip = string.Empty;
        public string ToolTip
        {
            get => _toolTip;
            set { _toolTip = value; on_property_changed(); }
        }
    }

    public class SellersViewModel : ViewModelBase
    {
        private readonly ISellerService _sellerService;
        private readonly IZonaService _zonaService;
        private List<SellerRowDto> _allSellersSource = new List<SellerRowDto>();
        private SellerRowDto? _editingSeller;
        private bool _isLoading;
        private string _errorMessage = string.Empty;

        private string _searchQuery = string.Empty;
        private string _newSellerCode = string.Empty;
        private string _newFullName = string.Empty;
        private string _addOrEditTitle = "Agregar Vendedor";
        private string _saveButtonText = "Agregar Vendedor";
        private bool _canEditCode = false;

        public ObservableCollection<SellerRowDto> ActiveSellers { get; } = new ObservableCollection<SellerRowDto>();
        public ObservableCollection<SellerRowDto> DeletedSellers { get; } = new ObservableCollection<SellerRowDto>();
        public ObservableCollection<SellerZonaCheckboxItem> AvailableZonas { get; } = new ObservableCollection<SellerZonaCheckboxItem>();

        public Action? OnRequestAddSellerWindow { get; set; }
        public Action<SellerRowDto>? OnRequestEditSellerWindow { get; set; }
        public Action? OnCloseAddSellerWindow { get; set; }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    on_property_changed();
                    FilterSellers();
                }
            }
        }

        public string NewSellerCode
        {
            get => _newSellerCode;
            set { _newSellerCode = value; on_property_changed(); }
        }

        public bool CanEditCode
        {
            get => _canEditCode;
            set { _canEditCode = value; on_property_changed(); }
        }

        public string NewFullName
        {
            get => _newFullName;
            set
            {
                _newFullName = value;
                on_property_changed();
                UpdateZonaExclusivity();
                (SaveSellerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
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

        public ICommand AddSellerCommand { get; }
        public ICommand EditSellerCommand { get; }
        public ICommand DeleteSellerCommand { get; }
        public ICommand RestoreSellerCommand { get; }
        public ICommand SaveSellerCommand { get; }
        public ICommand LoadSellersCommand { get; }

        public SellersViewModel(ISellerService sellerService, IZonaService zonaService)
        {
            _sellerService = sellerService ?? throw new ArgumentNullException(nameof(sellerService));
            _zonaService = zonaService ?? throw new ArgumentNullException(nameof(zonaService));

            AddSellerCommand = new RelayCommand(ExecuteAddSeller);
            EditSellerCommand = new RelayCommand(ExecuteEditSeller, CanExecuteEditSeller);
            DeleteSellerCommand = new RelayCommand(ExecuteDeleteSeller, CanExecuteDeleteSeller);
            RestoreSellerCommand = new RelayCommand(ExecuteRestoreSeller, CanExecuteRestoreSeller);
            SaveSellerCommand = new RelayCommand(ExecuteSaveSeller, CanExecuteSaveSeller);
            LoadSellersCommand = new RelayCommand(_ => LoadSellersAsync());

            LoadSellersAsync();
        }

        public void refresh_data() => LoadSellersAsync();

        public async void LoadSellersAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var sellers = await _sellerService.GetAllActiveAsync();
                var deleted = await _sellerService.GetAllDeletedAsync();

                _allSellersSource = sellers.Select(MapToDto).ToList();
                FilterSellers();

                DeletedSellers.Clear();
                foreach (var d in deleted)
                {
                    DeletedSellers.Add(MapToDto(d));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar vendedores: {ErrorText.Get(ex)}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private SellerRowDto MapToDto(seller s)
        {
            var assignedZones = s.seller_zones?
                .Where(sz => sz.zona != null)
                .Select(sz => sz.zona!)
                .OrderBy(z => z.sort_order)
                .ToList() ?? new List<zona>();

            string zonasText = assignedZones.Count > 0
                ? string.Join(", ", assignedZones.Select(z => $"{z.code} {z.name}"))
                : "Sin zonas asignadas";

            return new SellerRowDto
            {
                IdSeller = s.id_seller,
                SellerCode = s.seller_code ?? string.Empty,
                FullName = s.full_name ?? string.Empty,
                CustomerCodePrefix = s.customer_code_prefix ?? string.Empty,
                AssignedZonasText = zonasText,
                AssignedZonaIds = assignedZones.Select(z => z.id_zona).ToList(),
                IsActive = s.is_active,
                DeletedAt = s.deleted_at,
                DeletedReason = s.deleted_reason,
                SellerRef = s
            };
        }

        private void FilterSellers()
        {
            string query = _searchQuery?.Trim().ToLower() ?? string.Empty;
            List<SellerRowDto> filtered;

            if (string.IsNullOrWhiteSpace(query))
            {
                filtered = _allSellersSource.ToList();
            }
            else
            {
                filtered = _allSellersSource.Where(s =>
                    (s.SellerCode?.ToLower().Contains(query) ?? false) ||
                    (s.FullName?.ToLower().Contains(query) ?? false) ||
                    (s.AssignedZonasText?.ToLower().Contains(query) ?? false)
                ).ToList();
            }

            ActiveSellers.Clear();
            foreach (var item in filtered)
            {
                ActiveSellers.Add(item);
            }
        }

        private async void ExecuteAddSeller(object? parameter)
        {
            ClearForm();
            _editingSeller = null;
            AddOrEditTitle = "Agregar Vendedor";
            SaveButtonText = "Agregar Vendedor";
            CanEditCode = false;

            try
            {
                NewSellerCode = await _sellerService.GetNextSellerCodeAsync();
            }
            catch
            {
                NewSellerCode = "001";
            }

            await PopulateAvailableZonasAsync(new List<int>());
            OnRequestAddSellerWindow?.Invoke();
        }

        private bool CanExecuteEditSeller(object? parameter) => parameter is SellerRowDto;

        private async void ExecuteEditSeller(object? parameter)
        {
            if (parameter is SellerRowDto selected)
            {
                _editingSeller = selected;
                AddOrEditTitle = "Editar Vendedor";
                SaveButtonText = "Guardar Cambios";
                CanEditCode = false;
                NewSellerCode = selected.SellerCode;
                NewFullName = selected.FullName;
                ErrorMessage = string.Empty;

                await PopulateAvailableZonasAsync(selected.AssignedZonaIds);
                OnRequestEditSellerWindow?.Invoke(selected);
            }
        }

        private async Task PopulateAvailableZonasAsync(List<int> assignedIds)
        {
            AvailableZonas.Clear();
            var zonas = await _zonaService.GetActiveAsync();

            foreach (var z in zonas)
            {
                AvailableZonas.Add(new SellerZonaCheckboxItem
                {
                    IdZona = z.id_zona,
                    Code = z.code,
                    Name = z.name,
                    IsSelected = assignedIds.Contains(z.id_zona),
                    IsEnabled = true,
                    ToolTip = string.Empty
                });
            }
        }

        private void UpdateZonaExclusivity()
        {
            foreach (var item in AvailableZonas)
            {
                item.IsEnabled = true;
                item.ToolTip = string.Empty;
            }
        }

        private bool CanExecuteSaveSeller(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(_newSellerCode) &&
                   !string.IsNullOrWhiteSpace(_newFullName);
        }

        private async void ExecuteSaveSeller(object? parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(_newFullName))
                {
                    ErrorMessage = "El nombre del vendedor es obligatorio.";
                    return;
                }

                var selectedZonaIds = AvailableZonas
                    .Where(z => z.IsSelected)
                    .Select(z => z.IdZona)
                    .ToList();

                if (_editingSeller != null && _editingSeller.SellerRef != null)
                {
                    var existing = _editingSeller.SellerRef;
                    existing.full_name = _newFullName.Trim();
                    await _sellerService.UpdateAsync(existing, selectedZonaIds);
                }
                else
                {
                    var newSeller = new seller(_newFullName.Trim(), _newSellerCode.Trim(), _newSellerCode.Trim());
                    await _sellerService.CreateAsync(newSeller, selectedZonaIds);
                }

                ClearForm();
                OnCloseAddSellerWindow?.Invoke();
                LoadSellersAsync();
                AppDataEvents.raise_catalogs_changed();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar vendedor: {ErrorText.Get(ex)}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteDeleteSeller(object? parameter) => parameter is SellerRowDto;

        private async void ExecuteDeleteSeller(object? parameter)
        {
            if (parameter is SellerRowDto selected && selected.SellerRef != null)
            {
                MessageBoxResult confirm = AppDialog.Show(
                    $"¿Está seguro de eliminar al vendedor \"{selected.FullName}\" ({selected.SellerCode})?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;
                    await _sellerService.SoftDeleteAsync(selected.IdSeller, "Eliminado desde panel de vendedores");
                    LoadSellersAsync();
                    AppDataEvents.raise_catalogs_changed();
                    AppDialog.Show($"Vendedor \"{selected.FullName}\" eliminado exitosamente.",
                        "Vendedor eliminado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al eliminar vendedor: {ErrorText.Get(ex)}";
                    AppDialog.Show($"Error al eliminar vendedor: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private bool CanExecuteRestoreSeller(object? parameter) => parameter is SellerRowDto;

        private async void ExecuteRestoreSeller(object? parameter)
        {
            if (parameter is SellerRowDto selected)
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;
                    await _sellerService.RestoreAsync(selected.IdSeller);
                    LoadSellersAsync();
                    AppDataEvents.raise_catalogs_changed();
                    AppDialog.Show($"Vendedor \"{selected.FullName}\" restaurado exitosamente.",
                        "Vendedor restaurado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al restaurar vendedor: {ErrorText.Get(ex)}";
                    AppDialog.Show($"Error al restaurar vendedor: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ClearForm()
        {
            NewSellerCode = string.Empty;
            NewFullName = string.Empty;
            ErrorMessage = string.Empty;
            _editingSeller = null;
            AvailableZonas.Clear();
            (SaveSellerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
