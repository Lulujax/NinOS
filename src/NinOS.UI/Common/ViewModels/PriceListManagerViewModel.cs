using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    /// <summary>
    /// Ventana Editar Listas: crear, editar nombre/descripcion y eliminar listas.
    /// Mismo patron simple que Editar Zonas. La Principal aparece siempre:
    /// se puede editar, pero no eliminar. Sus precios se editan en la pantalla
    /// de Listas de Precios.
    /// </summary>
    public class PriceListManagerViewModel : ViewModelBase
    {
        private readonly IPriceListService _priceListService;
        private bool _isLoading;
        private bool _isEditing;
        private string _errorMessage = string.Empty;
        private string _newName = string.Empty;
        private string _newDescription = string.Empty;
        private int? _editingId;

        public PriceListManagerViewModel(IPriceListService priceListService)
        {
            _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
            PriceLists = new ObservableCollection<PriceListRowViewModel>();

            AddListCommand = new RelayCommand(_ => ExecuteAddList());
            SaveListCommand = new RelayCommand(async _ => await ExecuteSaveListAsync(), _ => CanExecuteSaveList());
            EditListCommand = new RelayCommand(param => ExecuteEditList(param));
            DeleteListCommand = new RelayCommand(async param => await ExecuteDeleteListAsync(param));
            CancelEditCommand = new RelayCommand(_ => ExecuteCancelEdit());
        }

        public ObservableCollection<PriceListRowViewModel> PriceLists { get; }

        public ICommand AddListCommand { get; }
        public ICommand SaveListCommand { get; }
        public ICommand EditListCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand CancelEditCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; on_property_changed(); }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                on_property_changed();
                on_property_changed(nameof(TitleText));
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; on_property_changed(); }
        }

        public string NewName
        {
            get => _newName;
            set
            {
                _newName = value;
                on_property_changed();
                ((RelayCommand)SaveListCommand).RaiseCanExecuteChanged();
            }
        }

        public string NewDescription
        {
            get => _newDescription;
            set { _newDescription = value; on_property_changed(); }
        }

        public string TitleText => _editingId.HasValue ? "Editar Lista" : "Agregar Lista";

        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var principal = await _priceListService.GetPrincipalAsync();
                var listas = await _priceListService.GetAllAsync(includeInactive: false);

                PriceLists.Clear();

                // La Principal siempre va primera. Se edita igual que las demas,
                // pero no tiene boton de eliminar.
                PriceLists.Add(new PriceListRowViewModel
                {
                    IdPriceList = principal.id_price_list,
                    Name = principal.name,
                    Description = principal.description,
                    SortOrder = principal.sort_order,
                    IsActive = principal.is_active,
                    IsPrincipal = true
                });

                foreach (var lista in listas.OrderBy(l => l.sort_order).ThenBy(l => l.name))
                {
                    PriceLists.Add(new PriceListRowViewModel
                    {
                        IdPriceList = lista.id_price_list,
                        Name = lista.name,
                        Description = lista.description,
                        SortOrder = lista.sort_order,
                        IsActive = lista.is_active
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar listas: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteAddList()
        {
            _editingId = null;
            NewName = string.Empty;
            NewDescription = string.Empty;
            ErrorMessage = string.Empty;
            IsEditing = true;
            ((RelayCommand)SaveListCommand).RaiseCanExecuteChanged();
        }

        private void ExecuteEditList(object? param)
        {
            if (param is not PriceListRowViewModel row) return;

            _editingId = row.IdPriceList;
            NewName = row.Name;
            NewDescription = row.Description ?? string.Empty;
            ErrorMessage = string.Empty;
            IsEditing = true;
            ((RelayCommand)SaveListCommand).RaiseCanExecuteChanged();
        }

        private bool CanExecuteSaveList() => !string.IsNullOrWhiteSpace(NewName);

        private async Task ExecuteSaveListAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                ErrorMessage = "El nombre de la lista es obligatorio.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (_editingId.HasValue)
                {
                    await _priceListService.UpdateAsync(_editingId.Value, NewName, NewDescription);
                }
                else
                {
                    // Al no tener precios propios, la lista nueva hereda automaticamente
                    // todos los precios de la Principal.
                    await _priceListService.CreateAsync(NewName, NewDescription);
                }

                ExecuteCancelEdit();
                await LoadAsync();
                AppDataEvents.raise_catalogs_changed();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteDeleteListAsync(object? param)
        {
            if (param is not PriceListRowViewModel row) return;

            if (row.IsPrincipal)
            {
                ErrorMessage = "La lista Principal no se puede eliminar.";
                return;
            }

            MessageBoxResult confirm = AppDialog.Show(
                $"¿Eliminar la lista \"{row.Name}\"?\n\n" +
                "Dejará de estar disponible para nuevas notas. Las notas ya emitidas no cambian.",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await _priceListService.DeleteAsync(row.IdPriceList);
                await LoadAsync();
                AppDataEvents.raise_catalogs_changed();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar la lista: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancelEdit()
        {
            _editingId = null;
            NewName = string.Empty;
            NewDescription = string.Empty;
            ErrorMessage = string.Empty;
            IsEditing = false;
        }
    }
}
