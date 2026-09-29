using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Views
{
    public class deleted_customer_row
    {
        public int id_customer { get; set; }
        public string code { get; set; } = string.Empty;
        public string business_name { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public string reason { get; set; } = string.Empty;
        public string deleted_on { get; set; } = string.Empty;
    }

    public class deleted_product_row
    {
        public int id_product { get; set; }
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string price { get; set; } = string.Empty;
        public string stock { get; set; } = string.Empty;
        public string reason { get; set; } = string.Empty;
        public string deleted_on { get; set; } = string.Empty;
    }

    public partial class AdminPanelWindow : Window
    {
        private readonly ICustomerService? _customer_service;
        private readonly IInventoryService? _inventory_service;
        private readonly IServiceScopeFactory? _db_context_provider;
        private bool _backup_in_progress;

        public ObservableCollection<deleted_customer_row> DeletedCustomers { get; } = new();
        public ObservableCollection<deleted_product_row> DeletedProducts { get; } = new();

        public AdminPanelWindow()
        {
            InitializeComponent();
            CustomersGrid.ItemsSource = DeletedCustomers;
            ProductsGrid.ItemsSource = DeletedProducts;

            var service_provider = (Application.Current as App)?.GetServiceProvider();
            _customer_service = service_provider?.GetService(typeof(ICustomerService)) as ICustomerService;
            _inventory_service = service_provider?.GetService(typeof(IInventoryService)) as IInventoryService;
            _db_context_provider = service_provider?.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;

            Loaded += async (s, e) => await load_all_async();
            Closed += (s, e) => AppLog.Info("Panel Admin cerrado");
        }

        private async Task load_all_async()
        {
            await load_customers_async();
            await load_products_async();
        }

        private async Task load_customers_async()
        {
            if (_customer_service == null) return;
            try
            {
                var customers = await _customer_service.GetDeletedCustomersAsync();
                DeletedCustomers.Clear();
                foreach (var c in customers)
                {
                    DeletedCustomers.Add(new deleted_customer_row
                    {
                        id_customer = c.id_customer,
                        code = c.customer_code ?? string.Empty,
                        business_name = c.business_name ?? string.Empty,
                        phone = c.phone_number ?? string.Empty,
                        seller_name = c.seller_name ?? string.Empty,
                        reason = c.deleted_reason ?? string.Empty,
                        deleted_on = c.deleted_at?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? string.Empty
                    });
                }
                CustomersCountText.Text = $"{DeletedCustomers.Count} cliente(s) en la papelera";
            }
            catch (System.Exception ex)
            {
                AppLog.Error($"Papelera clientes: {ex.Message}");
            }
        }

        private async Task load_products_async()
        {
            if (_inventory_service == null) return;
            try
            {
                var products = await _inventory_service.get_deleted_products_async();
                DeletedProducts.Clear();
                foreach (var p in products)
                {
                    DeletedProducts.Add(new deleted_product_row
                    {
                        id_product = p.id_product,
                        code = p.product_code ?? string.Empty,
                        name = p.name ?? string.Empty,
                        category = p.category ?? string.Empty,
                        price = p.unit_price_usd.ToString("N2"),
                        stock = p.stock_quantity.ToString(),
                        reason = p.deleted_reason ?? string.Empty,
                        deleted_on = p.deleted_at?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? string.Empty
                    });
                }
                ProductsCountText.Text = $"{DeletedProducts.Count} producto(s) en la papelera";
            }
            catch (System.Exception ex)
            {
                AppLog.Error($"Papelera productos: {ex.Message}");
            }
        }

        private async void OnRefreshCustomersClick(object sender, RoutedEventArgs e)
            => await load_customers_async();

        private async void OnRefreshProductsClick(object sender, RoutedEventArgs e)
            => await load_products_async();

        private async void OnRestoreCustomerClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not deleted_customer_row row) return;

            MessageBoxResult confirm = AppDialog.Show(
                $"¿Restaurar el cliente \"{row.business_name}\" ({row.code})?\n\nVolverá a estar disponible con su correlativo original, como si nada hubiera pasado.",
                "Restaurar cliente",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                if (_customer_service != null) await _customer_service.RestoreCustomerAsync(row.id_customer);
                await load_customers_async();
                AppDataEvents.raise_catalogs_changed();
                AppDialog.Show($"Cliente \"{row.business_name}\" restaurado.", "Listo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                AppDialog.Show($"No se pudo restaurar el cliente: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnRestoreProductClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not deleted_product_row row) return;

            MessageBoxResult confirm = AppDialog.Show(
                $"¿Restaurar el producto \"{row.name}\" ({row.code})?\n\nVolverá a estar disponible con su código original, como si nada hubiera pasado.",
                "Restaurar producto",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                if (_inventory_service != null) await _inventory_service.restore_product_async(row.id_product);
                await load_products_async();
                AppDataEvents.raise_catalogs_changed();
                AppDialog.Show($"Producto \"{row.name}\" restaurado.", "Listo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                AppDialog.Show($"No se pudo restaurar el producto: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnChangePasswordClick(object sender, RoutedEventArgs e)
        {
            string? nueva = PromptWindow.ask(this, "Nueva contraseña", "Escribe la nueva contraseña de administrador.");
            if (string.IsNullOrEmpty(nueva)) return;

            string? confirmacion = PromptWindow.ask(this, "Confirmar contraseña", "Repite la nueva contraseña.");
            if (!string.Equals(nueva, confirmacion, StringComparison.Ordinal))
            {
                AppDialog.Show("Las contraseñas no coinciden.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AdminSecurity.change_password(nueva);
            AppDialog.Show("Contraseña cambiada correctamente.", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ============ Purgar (borrado definitivo) ============

        private async void OnPurgeCustomerClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not deleted_customer_row row) return;

            MessageBoxResult confirm = AppDialog.Show(
                $"¿Borrar DEFINITIVAMENTE el cliente \"{row.business_name}\" ({row.code}) de la base de datos?\n\nEsta accion no se puede deshacer. Si tiene notas, creditos o movimientos, se mostrara un aviso y se conservara en la papelera.",
                "Purgar cliente",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                if (_customer_service != null) await _customer_service.PurgeCustomerAsync(row.id_customer);
                await load_customers_async();
                AppDataEvents.raise_catalogs_changed();
            }
            catch (InvalidOperationException ex)
            {
                AppDialog.Show(ex.Message, "No se pudo purgar", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"No se pudo purgar el cliente: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnPurgeProductClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not deleted_product_row row) return;

            MessageBoxResult confirm = AppDialog.Show(
                $"¿Borrar DEFINITIVAMENTE el producto \"{row.name}\" ({row.code}) de la base de datos?\n\nEsta accion no se puede deshacer. Si tiene notas, creditos, promociones o kardex, se mostrara un aviso y se conservara en la papelera.",
                "Purgar producto",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                if (_inventory_service != null) await _inventory_service.purge_product_async(row.id_product);
                await load_products_async();
                AppDataEvents.raise_catalogs_changed();
            }
            catch (InvalidOperationException ex)
            {
                AppDialog.Show(ex.Message, "No se pudo purgar", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"No se pudo purgar el producto: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============ Backup ============

        private async void OnBackupNowClick(object sender, RoutedEventArgs e)
        {
            if (_backup_in_progress) return;

            var dialog = new SaveFileDialog
            {
                Title = "Guardar backup de la base de datos",
                Filter = "Dump PostgreSQL (*.dump)|*.dump",
                FileName = $"ninos_backup_{DateTime.Now:yyyy-MM-dd_HHmmss}.dump",
                InitialDirectory = Directory.Exists(DatabaseBackup.BackupsFolder) ? DatabaseBackup.BackupsFolder : null
            };

            if (dialog.ShowDialog(this) != true) return;

            _backup_in_progress = true;
            BackupStatusText.Text = "Generando backup, espera un momento...";
            try
            {
                string result = await DatabaseBackup.create_backup_async(dialog.FileName);
                DatabaseBackup.prune_old_backups();
                BackupStatusText.Text = result;

                if (!result.StartsWith("Backup generado"))
                {
                    AppDialog.Show(result, "El backup fallo", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                BackupStatusText.Text = $"Error inesperado: {ErrorText.Get(ex)}";
            }
            finally
            {
                _backup_in_progress = false;
            }
        }

        private void OnOpenBackupsFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(DatabaseBackup.BackupsFolder);
                Process.Start(new ProcessStartInfo(DatabaseBackup.BackupsFolder) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppDialog.Show($"No se pudo abrir la carpeta:\n{DatabaseBackup.BackupsFolder}\n\n{ErrorText.Get(ex)}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnRestoreClick(object sender, RoutedEventArgs e)
        {
            if (_backup_in_progress) return;

            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar archivo de backup",
                Filter = "Dump PostgreSQL (*.dump)|*.dump|Todos los archivos (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true) return;

            MessageBoxResult confirm = AppDialog.Show(
                "ATENCION: se reemplazaran TODOS los datos actuales de la base de datos por los del archivo seleccionado.\n\nContinua solo si estas seguro.",
                "Restaurar base de datos",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            string? palabra = PromptWindow.ask(this, "Confirmacion final",
                "Escribe RESTAURAR en mayusculas para confirmar el restauro de la base de datos.");
            if (!string.Equals(palabra, "RESTAURAR", StringComparison.Ordinal))
            {
                BackupStatusText.Text = "Restauracion cancelada: no se escribio la palabra de confirmacion.";
                return;
            }

            _backup_in_progress = true;
            BackupStatusText.Text = "Restaurando la base de datos, espera un momento...";
            try
            {
                string result = await DatabaseBackup.restore_backup_async(dialog.FileName);
                BackupStatusText.Text = result;

                if (!result.StartsWith("Base de datos restaurada"))
                {
                    AppDialog.Show(result, "La restauracion fallo", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                BackupStatusText.Text = $"Error inesperado: {ErrorText.Get(ex)}";
            }
            finally
            {
                _backup_in_progress = false;
            }
        }

        // ============ Extras ============

        private async void OnDbStatusClick(object sender, RoutedEventArgs e)
        {
            if (_db_context_provider == null)
            {
                DbStatusText.Text = "Sin acceso a la base de datos.";
                return;
            }

            try
            {
                using IServiceScope scope = _db_context_provider.CreateScope();
                var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                int customers = await db_context.customers.CountAsync();
                int customers_inactive = await db_context.customers.CountAsync(c => !c.is_active);
                int products = await db_context.products.CountAsync();
                int products_inactive = await db_context.products.CountAsync(p => !p.is_active);
                int notes = await db_context.delivery_notes.CountAsync();
                int credit_notes = await db_context.credit_notes.CountAsync();
                int movements = await db_context.stock_movements.CountAsync();

                string db_name = db_context.Database.GetDbConnection().Database;

                DbStatusText.Text =
                    $"BD: {db_name} | Clientes: {customers} ({customers_inactive} en papelera) | " +
                    $"Productos: {products} ({products_inactive} en papelera)\n" +
                    $"Notas de entrega: {notes} | Notas de credito: {credit_notes} | Movimientos de kardex: {movements}";
            }
            catch (Exception ex)
            {
                DbStatusText.Text = $"Error al consultar: {ErrorText.Get(ex)}";
            }
        }

        private async void OnExportCustomersClick(object sender, RoutedEventArgs e)
        {
            if (_customer_service == null) return;

            var dialog = new SaveFileDialog
            {
                Title = "Exportar clientes",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"clientes_{DateTime.Now:yyyy-MM-dd}.csv"
            };

            if (dialog.ShowDialog(this) != true) return;

            try
            {
                export_result result = await DatabaseExporter.export_customers_csv_async(_customer_service, dialog.FileName);
                ExportStatusText.Text = $"{result.rows} cliente(s) exportados.";
            }
            catch (Exception ex)
            {
                ExportStatusText.Text = $"Error: {ErrorText.Get(ex)}";
            }
        }

        private async void OnExportProductsClick(object sender, RoutedEventArgs e)
        {
            if (_inventory_service == null) return;

            var dialog = new SaveFileDialog
            {
                Title = "Exportar productos",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"productos_{DateTime.Now:yyyy-MM-dd}.csv"
            };

            if (dialog.ShowDialog(this) != true) return;

            try
            {
                export_result result = await DatabaseExporter.export_products_csv_async(_inventory_service, dialog.FileName);
                ExportStatusText.Text = $"{result.rows} producto(s) exportados.";
            }
            catch (Exception ex)
            {
                ExportStatusText.Text = $"Error: {ErrorText.Get(ex)}";
            }
        }

        private void OnLoadLogClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(AppLog.FilePath))
                {
                    LogTextBox.Text = "(aun no hay registros)";
                    return;
                }

                string[] lines = File.ReadAllLines(AppLog.FilePath);
                LogTextBox.Text = string.Join(Environment.NewLine, lines.TakeLast(400));
                LogTextBox.ScrollToEnd();
            }
            catch (Exception ex)
            {
                LogTextBox.Text = $"Error al leer la bitacora: {ErrorText.Get(ex)}";
            }
        }

        private void OnOpenLogFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(AppLog.FilePath))
                {
                    AppDialog.Show("Aun no hay archivo de bitacora.", "Bitacora",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo(AppLog.FilePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppDialog.Show($"No se pudo abrir la bitacora: {ErrorText.Get(ex)}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}