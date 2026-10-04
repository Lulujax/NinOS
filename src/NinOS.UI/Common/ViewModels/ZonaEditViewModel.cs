using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.UI.Common.ViewModels
{
    public class ZonaEditViewModel : INotifyPropertyChanged
    {
        private readonly IZonaService _zonaService;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private string _newName = string.Empty;
        private string _newCode = string.Empty;
        private string _newSortOrder = "0";
        private bool _isEditing;
        private zona? _editingZona;

        public ZonaEditViewModel(IZonaService zonaService)
        {
            _zonaService = zonaService;
            ZonasActivas = new ObservableCollection<ZonaViewModel>();
            ZonasEliminadas = new ObservableCollection<ZonaViewModel>();

            AddZonaCommand = new RelayCommand(_ => ExecuteAddZona(), _ => CanExecuteAddZona());
            SaveZonaCommand = new RelayCommand(async _ => await ExecuteSaveZonaAsync(), _ => CanExecuteSaveZona());
            EditZonaCommand = new RelayCommand(param => ExecuteEditZona(param), _ => true);
            DeleteZonaCommand = new RelayCommand(async param => await ExecuteDeleteZonaAsync(param), _ => true);
            RestoreZonaCommand = new RelayCommand(async param => await ExecuteRestoreZonaAsync(param), _ => true);
            CancelEditCommand = new RelayCommand(_ => ExecuteCancelEdit(), _ => true);
            RefreshCommand = new RelayCommand(async _ => await LoadZonasAsync(), _ => true);
        }

        public ObservableCollection<ZonaViewModel> ZonasActivas { get; }
        public ObservableCollection<ZonaViewModel> ZonasEliminadas { get; }

        public ICommand AddZonaCommand { get; }
        public ICommand SaveZonaCommand { get; }
        public ICommand EditZonaCommand { get; }
        public ICommand DeleteZonaCommand { get; }
        public ICommand RestoreZonaCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand RefreshCommand { get; }

        public Action? OnCloseWindow { get; set; }

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
            set { _newName = value; OnPropertyChanged(); ((RelayCommand)SaveZonaCommand).RaiseCanExecuteChanged(); }
        }

        public string NewCode
        {
            get => _newCode;
            set { _newCode = value; OnPropertyChanged(); }
        }

        public string NewSortOrder
        {
            get => _newSortOrder;
            set { _newSortOrder = value; OnPropertyChanged(); }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); OnPropertyChanged(nameof(TitleText)); }
        }

        public string TitleText => IsEditing ? "Editar Zona" : "Agregar Zona";

        public async Task LoadZonasAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                ZonasActivas.Clear();
                ZonasEliminadas.Clear();

                var activas = await _zonaService.GetActiveAsync();
                foreach (var z in activas)
                {
                    ZonasActivas.Add(new ZonaViewModel
                    {
                        IdZona = z.id_zona,
                        Code = z.code,
                        Name = z.name,
                        SortOrder = z.sort_order,
                        IsActive = z.is_active
                    });
                }

                var eliminadas = await _zonaService.GetDeletedAsync();
                foreach (var z in eliminadas)
                {
                    ZonasEliminadas.Add(new ZonaViewModel
                    {
                        IdZona = z.id_zona,
                        Code = z.code,
                        Name = z.name,
                        SortOrder = z.sort_order,
                        IsActive = z.is_active,
                        DeletedAt = z.deleted_at,
                        DeletedReason = z.deleted_reason
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar zonas: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteAddZona() => true;
        private void ExecuteAddZona()
        {
            _editingZona = null;
            NewName = string.Empty;
            NewCode = string.Empty;
            NewSortOrder = "0";
            IsEditing = false;
        }

        private bool CanExecuteSaveZona() => !string.IsNullOrWhiteSpace(NewName);

        private async Task ExecuteSaveZonaAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(NewName))
                {
                    ErrorMessage = "El nombre de la zona es obligatorio";
                    return;
                }

                int sort = 0;
                int.TryParse(NewSortOrder, out sort);

                if (_editingZona == null)
                {
                    var nueva = new zona
                    {
                        name = NewName.Trim(),
                        code = await _zonaService.GetNextCodeAsync(),
                        sort_order = sort,
                        is_active = true
                    };
                    await _zonaService.CreateAsync(nueva);
                }
                else
                {
                    _editingZona.name = NewName.Trim();
                    _editingZona.sort_order = sort;
                    await _zonaService.UpdateAsync(_editingZona);
                }

                ExecuteCancelEdit();
                await LoadZonasAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar zona: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteEditZona(object? param)
        {
            if (param is ZonaViewModel vm)
            {
                _editingZona = new zona
                {
                    id_zona = vm.IdZona,
                    code = vm.Code,
                    name = vm.Name,
                    sort_order = vm.SortOrder,
                    is_active = vm.IsActive
                };
                NewName = vm.Name;
                NewCode = vm.Code;
                NewSortOrder = vm.SortOrder.ToString();
                IsEditing = true;
            }
        }

        private async Task ExecuteDeleteZonaAsync(object? param)
        {
            if (param is ZonaViewModel vm)
            {
                try
                {
                    IsLoading = true;
                    await _zonaService.DeleteAsync(vm.IdZona, "Eliminado desde Editor de Zonas");
                    await LoadZonasAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al eliminar zona: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task ExecuteRestoreZonaAsync(object? param)
        {
            if (param is ZonaViewModel vm)
            {
                try
                {
                    IsLoading = true;
                    await _zonaService.RestoreAsync(vm.IdZona);
                    await LoadZonasAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al restaurar zona: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecuteCancelEdit()
        {
            _editingZona = null;
            NewName = string.Empty;
            NewCode = string.Empty;
            NewSortOrder = "0";
            IsEditing = false;
            ErrorMessage = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
