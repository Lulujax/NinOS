using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class PriceListEditViewModel : INotifyPropertyChanged
    {
        private readonly IPriceListService _priceListService;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private string _newName = string.Empty;
        private bool _isEditing;
        private int? _editingId;

        public PriceListEditViewModel(IPriceListService priceListService)
        {
            _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
            PriceLists = new ObservableCollection<PriceListRowViewModel>();

            AddListCommand = new RelayCommand(_ => ExecuteAddList(), _ => true);
            SaveListCommand = new RelayCommand(async _ => await ExecuteSaveListAsync(), _ => CanExecuteSaveList());
            RenameListCommand = new RelayCommand(param => ExecuteRenameList(param), _ => true);
            ToggleActiveCommand = new RelayCommand(async param => await ExecuteToggleActiveAsync(param), _ => true);
            CancelEditCommand = new RelayCommand(_ => ExecuteCancelEdit(), _ => true);
        }

        public ObservableCollection<PriceListRowViewModel> PriceLists { get; }

        public ICommand AddListCommand { get; }
        public ICommand SaveListCommand { get; }
        public ICommand RenameListCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand CancelEditCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string NewName
        {
            get => _newName;
            set
            {
                _newName = value;
                OnPropertyChanged();
                ((RelayCommand)SaveListCommand).RaiseCanExecuteChanged();
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); OnPropertyChanged(nameof(TitleText)); }
        }

        public string TitleText => _editingId != null ? "Renombrar Lista" : "Nueva Lista";

        public async Task LoadPriceListsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                PriceLists.Clear();

                // Se muestran todas, tambien las inactivas: desactivar no es borrar y el
                // usuario tiene que poder reactivarlas desde aqui.
                var listas = await _priceListService.GetAllAsync(includeInactive: true);
                foreach (var l in listas.OrderBy(x => x.sort_order).ThenBy(x => x.name))
                {
                    PriceLists.Add(new PriceListRowViewModel
                    {
                        IdPriceList = l.id_price_list,
                        Name = l.name,
                        SortOrder = l.sort_order,
                        IsActive = l.is_active
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar listas de precios: {ex.Message}";
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
            ErrorMessage = string.Empty;
            IsEditing = true;
            ((RelayCommand)SaveListCommand).RaiseCanExecuteChanged();
        }

        private bool CanExecuteSaveList() => !string.IsNullOrWhiteSpace(NewName);

        private async Task ExecuteSaveListAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(NewName))
                {
                    ErrorMessage = "El nombre de la lista es obligatorio.";
                    return;
                }

                if (_editingId == null)
                    await _priceListService.CreateAsync(NewName);
                else
                    await _priceListService.RenameAsync(_editingId.Value, NewName);

                ExecuteCancelEdit();
                await LoadPriceListsAsync();
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

        private void ExecuteRenameList(object? param)
        {
            if (param is PriceListRowViewModel row)
            {
                _editingId = row.IdPriceList;
                NewName = row.Name;
                ErrorMessage = string.Empty;
                IsEditing = true;
                ((RelayCommand)SaveListCommand).RaiseCanExecuteChanged();
            }
        }

        private async Task ExecuteToggleActiveAsync(object? param)
        {
            if (param is not PriceListRowViewModel row) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (row.IsActive)
                {
                    MessageBoxResult confirm = AppDialog.Show(
                        $"¿Desactivar la lista \"{row.Name}\"? Dejará de aparecer al facturar y en la ventana de producto. Los precios guardados no se pierden y las notas ya emitidas no cambian.",
                        "Confirmar desactivación",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirm != MessageBoxResult.Yes) return;
                }

                await _priceListService.SetActiveAsync(row.IdPriceList, !row.IsActive);
                await LoadPriceListsAsync();
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

        private void ExecuteCancelEdit()
        {
            _editingId = null;
            NewName = string.Empty;
            ErrorMessage = string.Empty;
            IsEditing = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
