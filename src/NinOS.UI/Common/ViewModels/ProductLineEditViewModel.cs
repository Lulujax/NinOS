using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using NinOS.Domain;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class ProductLineEditViewModel : INotifyPropertyChanged
    {
        private readonly IProductLineService _productLineService;

        public ObservableCollection<ProductLineViewModel> LineasActivas { get; set; } = new();
        public ObservableCollection<ProductLineViewModel> LineasEliminadas { get; set; } = new();

        public Action? OnCloseWindow { get; set; }

        private string _title = "Agregar Línea";
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); OnPropertyChanged(nameof(TitleText)); }
        }

        public string TitleText => Title;

        private string _newName = string.Empty;
        public string NewName
        {
            get => _newName;
            set
            {
                _newName = value;
                OnPropertyChanged();
                ((RelayCommand)SaveLineCommand).RaiseCanExecuteChanged();
            }
        }

        private string _newPrefix = string.Empty;
        public string NewPrefix
        {
            get => _newPrefix;
            set
            {
                _newPrefix = value?.ToUpper() ?? string.Empty;
                OnPropertyChanged();
                ((RelayCommand)SaveLineCommand).RaiseCanExecuteChanged();
            }
        }

        private int _newSortOrder;
        public int NewSortOrder
        {
            get => _newSortOrder;
            set { _newSortOrder = value; OnPropertyChanged(); }
        }

        private bool _newIsActive = true;
        public bool NewIsActive
        {
            get => _newIsActive;
            set { _newIsActive = value; OnPropertyChanged(); }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }
        
        private bool _canEditPrefix = true;

        /// <summary>
        /// True mientras se esta reescribiendo una linea. El guardado de una linea con productos
        /// reescribe el inventario y todo el historial, y eso tarda: sin este bloqueo el usuario
        /// le da otra vez al boton y se lanzan dos migraciones.
        /// </summary>
        private bool _is_loading;
        public bool IsLoading
        {
            get => _is_loading;
            private set { _is_loading = value; OnPropertyChanged(); }
        }

        public bool IsBusy => _is_loading;

        public bool CanEditPrefix
        {
            get => _canEditPrefix;
            set { _canEditPrefix = value; OnPropertyChanged(); OnPropertyChanged(nameof(PrefixReadOnly)); }
        }

        public bool PrefixReadOnly => !CanEditPrefix;

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private ProductLineViewModel? _editingLine;
        public ProductLineViewModel? EditingLine
        {
            get => _editingLine;
            set { _editingLine = value; OnPropertyChanged(); }
        }

        public ICommand AddLineCommand { get; }
        public ICommand SaveLineCommand { get; }
        public ICommand EditLineCommand { get; }
        public ICommand DeleteLineCommand { get; }
        public ICommand RestoreLineCommand { get; }
        public ICommand ToggleVisibilityCommand { get; }
        public ICommand CancelEditCommand { get; }

        public ProductLineEditViewModel(IProductLineService productLineService)
        {
            _productLineService = productLineService ?? throw new ArgumentNullException(nameof(productLineService));

            AddLineCommand = new RelayCommand(ExecuteAddLine);
            SaveLineCommand = new RelayCommand(async _ => await ExecuteSaveLineAsync(), CanExecuteSaveLine);
            EditLineCommand = new RelayCommand(ExecuteEditLine);
            DeleteLineCommand = new RelayCommand(async param => await ExecuteDeleteLineAsync(param));
            RestoreLineCommand = new RelayCommand(async param => await ExecuteRestoreLineAsync(param));
            ToggleVisibilityCommand = new RelayCommand(async param => await ExecuteToggleVisibilityAsync(param));
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
        }

        public async Task LoadLineasAsync() => await LoadDataAsync();

        public async Task LoadDataAsync()
        {
            try
            {
                ErrorMessage = string.Empty;
                var allLines = await _productLineService.GetAllAsync(includeInactive: true);
                var deletedLines = await _productLineService.GetDeletedAsync();

                LineasActivas.Clear();
                foreach (var line in allLines)
                {
                    bool hasProducts = await _productLineService.HasProductsAsync(line.id_product_line);
                    LineasActivas.Add(new ProductLineViewModel
                    {
                        IdProductLine = line.id_product_line,
                        Name = line.name,
                        CodePrefix = line.code_prefix,
                        SortOrder = line.sort_order,
                        IsActive = line.is_active,
                        HasProducts = hasProducts
                    });
                }

                LineasEliminadas.Clear();
                foreach (var line in deletedLines)
                {
                    LineasEliminadas.Add(new ProductLineViewModel
                    {
                        IdProductLine = line.id_product_line,
                        Name = line.name,
                        CodePrefix = line.code_prefix,
                        SortOrder = line.sort_order,
                        IsActive = line.is_active,
                        DeletedAt = line.deleted_at,
                        DeletedReason = line.deleted_reason
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar líneas: {ex.Message}";
            }
        }

        private void ExecuteAddLine(object? parameter)
        {
            Title = "Agregar Línea";
            IsEditing = true;
            CanEditPrefix = true;
            EditingLine = null;
            NewName = string.Empty;
            NewPrefix = string.Empty;
            NewIsActive = true;
            NewSortOrder = (LineasActivas.Count > 0 ? LineasActivas.Max(x => x.SortOrder) : 0) + 1;
            ErrorMessage = string.Empty;
            ((RelayCommand)SaveLineCommand).RaiseCanExecuteChanged();
        }

        private void ExecuteEditLine(object? parameter)
        {
            if (parameter is ProductLineViewModel line)
            {
                Title = "Editar Línea";
                IsEditing = true;
                EditingLine = line;
                NewName = line.Name;
                NewPrefix = line.CodePrefix;
                NewSortOrder = line.SortOrder;
                NewIsActive = line.IsActive;
                // El prefijo queda editable aunque la linea ya tenga productos. El correlativo se
                // arma sobre todos los codigos en uso y saltea los que ya existen, asi que cambiar
                // el prefijo no genera duplicados: los productos viejos conservan su codigo y los
                // nuevos salen con el prefijo nuevo. Lo unico que se prohibe es repetir un
                // prefijo que ya use otra linea, y eso lo valida ExistsActiveAsync.
                CanEditPrefix = true;
                ErrorMessage = string.Empty;
                ((RelayCommand)SaveLineCommand).RaiseCanExecuteChanged();
            }
        }

        private bool CanExecuteSaveLine(object? parameter)
        {
            // While migrating, Guardar stays disabled: it prevents double-submitting the same
            // migration if the user gets impatient and clicks again.
            return !_is_loading &&
                   !string.IsNullOrWhiteSpace(NewName) &&
                   !string.IsNullOrWhiteSpace(NewPrefix) &&
                   NewPrefix.Trim().Length >= 2 && NewPrefix.Trim().Length <= 4 &&
                   NewPrefix.Trim().All(char.IsLetter);
        }

        private async Task ExecuteSaveLineAsync()
{
            if (!CanExecuteSaveLine(null)) return;

            ErrorMessage = string.Empty;

            // Solo el guardado de una línea que ya tiene productos es lento: ahí se reescribe el
            // inventario y todo el historial. Se bloquea la ventana para que se vea que está
            // trabajando y no se pueda volver a disparar.
            bool posible_migracion = EditingLine != null && _canEditPrefix;
            if (posible_migracion) IsLoading = true;

            try
            {
                await save_line_internal_async();
            }
            finally
            {
                IsLoading = false;
                ((RelayCommand)SaveLineCommand).RaiseCanExecuteChanged();
            }
        }

        private async Task save_line_internal_async()
        {
            bool exists = await _productLineService.ExistsActiveAsync(
                NewName.Trim(), 
                NewPrefix.Trim(), 
                EditingLine?.IdProductLine);
                
            if (exists)
            {
                ErrorMessage = "Ya existe una línea con ese nombre o prefijo de código.";
                return;
            }

            // Si cambia el prefijo hay que reescribir los codigos de todos los productos de la linea
            // y dejar el historial alineado. Se muestra primero cuanto se va a tocar.
            string prefijo_viejo = EditingLine?.CodePrefix?.Trim().ToUpperInvariant() ?? string.Empty;
            string prefijo_nuevo = NewPrefix?.Trim().ToUpperInvariant() ?? string.Empty;

            bool cambia_prefijo = EditingLine != null
                                  && !string.Equals(prefijo_viejo, prefijo_nuevo, StringComparison.Ordinal);

            product_code_migration_preview? preview = null;

            if (cambia_prefijo)
            {
                preview = await _productLineService.preview_prefix_change_async(
                    EditingLine!.IdProductLine, prefijo_viejo, prefijo_nuevo);

                if (preview.total_productos == 0)
                {
                    var solo_prefijo = AppDialog.Show(
                        $"No hay productos con el prefijo {prefijo_viejo}.\n\n" +
                        $"Se va a cambiar el prefijo de la línea a {prefijo_nuevo}. Los productos que se creen " +
                        "de aquí en adelante saldrán con el prefijo nuevo.\n\n¿Desea continuar?",
                        "Cambiar prefijo de código",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (solo_prefijo != MessageBoxResult.Yes) return;
                }
                else
                {
                    string detalle = "";
                    if (preview.a_reescribir > 0)
                        detalle += $"  - {preview.a_reescribir} producto(s): {prefijo_viejo}... → {prefijo_nuevo}...\n";

                    if (preview.detalles_de_notas_afectados > 0)
                        detalle += $"  - {preview.detalles_de_notas_afectados} línea(s) de notas de entrega\n";

                    if (preview.detalles_de_notas_de_credito_afectados > 0)
                        detalle += $"  - {preview.detalles_de_notas_de_credito_afectados} línea(s) de notas de crédito\n";

                    if (preview.ajustes_de_kardex_afectados > 0)
                        detalle += $"  - {preview.ajustes_de_kardex_afectados} ajuste(s) del inventario\n";

                    if (preview.hay_conflicto)
                    {
                        detalle += $"\nEstos {preview.quedan_iguales} producto(s) NO se van a cambiar:\n";
                        foreach (var item in preview.items.Where(i => i.queda_igual))
                            detalle += $"  {item.old_code} — {item.motivo}\n";
                    }

                    var confirmacion = AppDialog.Show(
                        $"Se va a cambiar el prefijo de \"{EditingLine.Name}\" de {prefijo_viejo} a {prefijo_nuevo}.\n\n" +
                        "Se reescriben los códigos, conservando el número de cada producto " +
                        $"(por ejemplo {prefijo_viejo}30508 → {prefijo_nuevo}30508), y también el historial:\n\n" +
                        detalle + "\nEsta acción no se puede deshacer.\n\n¿Desea continuar?",
                        "Cambiar prefijo de código",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (confirmacion != MessageBoxResult.Yes) return;
                }
            }

            try
            {
                if (EditingLine == null)
                {
                    var line = new product_line
                    {
                        name = NewName.Trim().ToUpperInvariant(),
                        code_prefix = NewPrefix.Trim().ToUpperInvariant(),
                        sort_order = NewSortOrder,
                        is_active = NewIsActive
                    };
                    await _productLineService.CreateAsync(line);
                }
                else if (cambia_prefijo)
                {
                    // El nombre, el orden y la visibilidad van por su cuenta; el prefijo y todos
                    // los códigos que dependen de él los reescribe la migración.
                    var linea = await _productLineService.GetByIdAsync(EditingLine.IdProductLine);
                    if (linea != null)
                    {
                        linea.name = NewName.Trim().ToUpperInvariant();
                        linea.sort_order = NewSortOrder;
                        linea.is_active = NewIsActive;
                        await _productLineService.UpdateAsync(linea);
                    }

                    var resultado = await _productLineService.migrate_prefix_async(
                        EditingLine.IdProductLine, prefijo_viejo, prefijo_nuevo);

                    if (!resultado.sucesso)
                    {
                        ErrorMessage = resultado.mensaje;
                        return;
                    }

                    await LoadDataAsync();
                    AppDataEvents.raise_catalogs_changed();

                    AppDialog.Show(
                        resultado.mensaje + "\n\nHistorial alineado: " +
                        $"{resultado.snapshots_de_notas_actualizados} línea(s) de notas, " +
                        $"{resultado.snapshots_de_notas_de_credito_actualizados} de notas de crédito y " +
                        $"{resultado.ajustes_de_kardex_actualizados} ajuste(s) de inventario.",
                        "Prefijo actualizado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    var line = await _productLineService.GetByIdAsync(EditingLine.IdProductLine);
                    if (line != null)
                    {
                        line.name = NewName.Trim().ToUpperInvariant();
                        line.code_prefix = NewPrefix.Trim().ToUpperInvariant();
                        line.sort_order = NewSortOrder;
                        line.is_active = NewIsActive;
                        await _productLineService.UpdateAsync(line);
                    }
                }

                IsEditing = false;
                await LoadDataAsync();
                AppDataEvents.raise_catalogs_changed();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar: {ex.Message}";
            }
        }

        private async Task ExecuteToggleVisibilityAsync(object? parameter)
        {
            if (parameter is ProductLineViewModel line)
            {
                try
                {
                    var entity = await _productLineService.GetByIdAsync(line.IdProductLine);
                    if (entity != null)
                    {
                        entity.is_active = !entity.is_active;
                        await _productLineService.UpdateAsync(entity);
                        await LoadDataAsync();
                        AppDataEvents.raise_catalogs_changed();
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al cambiar estado: {ex.Message}";
                }
            }
        }

        private async Task ExecuteDeleteLineAsync(object? parameter)
        {
            if (parameter is ProductLineViewModel line)
            {
                if (line.HasProducts)
                {
                    AppDialog.Show(
                        $"No se puede eliminar la línea \"{line.Name}\" porque tiene productos asociados en el inventario.\n\nPuedes ocultarla usando el botón \"Ocultar\" para que no aparezca en las pestañas.",
                        "Línea con productos",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult confirm = AppDialog.Show(
                    $"¿Está seguro de eliminar la línea de productos \"{line.Name}\" ({line.CodePrefix})?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;
                
                try
                {
                    await _productLineService.DeleteAsync(line.IdProductLine, "Eliminado desde Editor de Líneas");
                    await LoadDataAsync();
                    AppDataEvents.raise_catalogs_changed();
                    AppDialog.Show($"Línea \"{line.Name}\" eliminada exitosamente.", "Línea eliminada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al eliminar: {ex.Message}";
                }
            }
        }

        private async Task ExecuteRestoreLineAsync(object? parameter)
        {
            if (parameter is ProductLineViewModel line)
            {
                try
                {
                    await _productLineService.RestoreAsync(line.IdProductLine);
                    await LoadDataAsync();
                    AppDataEvents.raise_catalogs_changed();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error al restaurar: {ex.Message}";
                }
            }
        }

        private void ExecuteCancelEdit(object? parameter)
        {
            IsEditing = false;
            ErrorMessage = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
