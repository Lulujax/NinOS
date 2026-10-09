using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Repositories.Implementations;
using NinOS.Infrastructure.Repositories.Interfaces;
using NinOS.Infrastructure.Services.Implementations;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;
using NinOS.UI.Views;

namespace NinOS.UI
{
    public partial class App : Application
    {
        private ServiceProvider? _service_provider;
        private static readonly string _error_log_path = Path.Combine(AppContext.BaseDirectory, "ninos-ui-error.log");

        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                log_error("DispatcherUnhandledException", e.Exception);
                e.Handled = true;
                try
                {
                    AppDialog.Show(
                        $"Ocurrió un error inesperado. Se guardó el detalle en:\n{_error_log_path}\n\nDetalle: {ErrorText.Get(e.Exception)}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch { /* el mensaje es solo informativo */ }
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception exception)
                {
                    log_error("AppDomain.UnhandledException", exception);
                }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                log_error("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };
        }

        public IServiceProvider GetServiceProvider()
        {
            return _service_provider!;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                log_startup_message("OnStartup begin");

                // Antes que la pantalla de carga: así todas las ventanas, incluidas las
                // que se abren después, respetan la escala guardada.
                UiScale.init();

                // Los botones van en mayúsculas (solo visual, la BD y los PDF no cambian).
                UpperCaseText.EnableGlobally();

                SplashWindow splash = new SplashWindow();
                splash.ShowWithAnimation();
                DateTime started_at = DateTime.Now;

                ServiceCollection service_collection = new ServiceCollection();
                configure_services(service_collection);
                _service_provider = service_collection.BuildServiceProvider();

                System.Threading.Tasks.Task.Run(async () =>
                {
                    log_startup_message("Before DbInitializer");
                    using (var scope = _service_provider.CreateScope())
                    {
                        var db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                        DbInitializer.initialize(db_context);

                        // Pre-calentar conexion y datos en paralelo durante la pantalla de carga
                        var receivable = scope.ServiceProvider.GetRequiredService<IAccountsReceivableService>();
                        var commission = scope.ServiceProvider.GetRequiredService<ICommissionService>();
                        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                        var credit = scope.ServiceProvider.GetRequiredService<ICreditNoteService>();

                        await System.Threading.Tasks.Task.WhenAll(
                            receivable.get_all_notes_async(),
                            commission.get_all_commissions_async(),
                            inventory.get_all_products_async(),
                            credit.get_credit_note_months_async()
                        );
                    }
                    log_startup_message("After DbInitializer");
                }).ContinueWith(previous =>
                {
                    if (previous.IsFaulted)
                    {
                        Exception exception = previous.Exception?.GetBaseException() ?? new Exception("Error de arranque");
                        Dispatcher.BeginInvoke(() =>
                        {
                            log_startup_message("OnStartup exception", exception);
                            AppDialog.Show(
                                "NinOS no pudo iniciar.\n\n" + ErrorText.Get(exception) +
                                "\n\nEl detalle técnico quedó guardado en:\n" + _error_log_path,
                                "No se pudo iniciar NinOS");
                            Current.Shutdown();
                        });
                        return;
                    }

                    log_startup_message("After DbInitializer");

                    DateTime ready_at = DateTime.Now;
                    int remaining_ms = Math.Max(0, (2 * 1000) - (int)((ready_at - started_at).TotalMilliseconds));

                    System.Windows.Threading.DispatcherTimer ready_timer = new System.Windows.Threading.DispatcherTimer();
                    ready_timer.Interval = TimeSpan.FromMilliseconds(remaining_ms);
                    ready_timer.Tick += (s, args) =>
                    {
                        ready_timer.Stop();
                        splash.SetReady();

                        System.Windows.Threading.DispatcherTimer build_timer = new System.Windows.Threading.DispatcherTimer();
                        build_timer.Interval = TimeSpan.FromMilliseconds(400);
                        build_timer.Tick += (s2, args2) =>
                        {
                            build_timer.Stop();

                            log_startup_message("Before MainWindow resolve");
                            MainWindow main_window = _service_provider!.GetRequiredService<MainWindow>();
                            Application.Current.MainWindow = main_window;
                            log_startup_message("Before MainWindow show");
                            main_window.WindowState = WindowState.Maximized;
                            main_window.Show();
                            main_window.Activate();
                            main_window.Focus();
                            splash.CloseWithAnimation();
                            AppLog.Info("Aplicación iniciada correctamente");
                            base.OnStartup(e);
                            log_startup_message("OnStartup end");
                        };
                        build_timer.Start();
                    };
                    ready_timer.Start();
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());

            }
            catch (Exception ex)
            {
                log_startup_message("OnStartup exception", ex);

                // Lo mas comun al iniciar es que la base no este todavia. Cerrar y obligar a
                // abrir la app de nuevo es una molestia, asi que se ofrece reintentar y solo
                // se sale si el usuario elige salir o se acabaron los intentos.
                const int max_intentos = 5;
                bool salir = false;

                for (int intento = 1; intento <= max_intentos && !salir; intento++)
                {
                    string detalle_intento = max_intentos > 1
                        ? $"\n\n(Intento {intento} de {max_intentos}.)"
                        : string.Empty;

                    var reintentar = AppDialog.Show(
                        "NinOS no pudo iniciar.\n\n" + ErrorText.Get(ex) + detalle_intento +
                        "\n\nSi la base de datos o la red todavia no estan listas, puede reintentar." +
                        "\nEl detalle técnico quedó guardado en:\n" + _error_log_path,
                        "No se pudo iniciar NinOS",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error,
                        MessageBoxResult.No,
                        "Reintentar", "Salir");

                    if (reintentar != MessageBoxResult.Yes)
                    {
                        salir = true;
                        break;
                    }

                    log_startup_message($"Reintento {intento} de arranque");

                    try
                    {
                    OnStartup(e);
                    return;
                    }
                    catch (Exception retry_ex)
                    {
                        ex = retry_ex;
                        log_startup_message("OnStartup exception (reintento)", retry_ex);
                    }
                }

                Current.Shutdown();
            }
        }

        private static void log_startup_message(string message, Exception? exception = null)
        {
            string log_entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";

            if (exception != null)
            {
                log_entry += Environment.NewLine + exception;
            }

            File.AppendAllText(_error_log_path, log_entry + Environment.NewLine + Environment.NewLine);

            if (exception != null)
            {
                AppLog.Error(message, exception);
            }
            else
            {
                AppLog.Info(message);
            }
        }

        private static void log_error(string source, Exception exception)
        {
            string entry =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {source}{Environment.NewLine}" +
                $"Mensaje: {exception.Message}{Environment.NewLine}" +
                $"Stack Trace:{Environment.NewLine}{exception}" +
                $"{Environment.NewLine}-----------------------------";

            try { File.AppendAllText(_error_log_path, entry + Environment.NewLine); }
            catch { /* el logging nunca debe derribar la app */ }

            AppLog.Error(source, exception);
        }

        private void configure_services(ServiceCollection services)
        {
            services.AddDbContext<NinOSDbContext>(options =>
            {
                options.UseNpgsql(DbConnectionFactory.GetConnectionString());
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IAccountsReceivableService, AccountsReceivableService>();
            services.AddScoped<IDeliveryNoteRepository, DeliveryNoteRepository>();
            services.AddScoped<IDeliveryNoteService, DeliveryNoteService>();
            services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
            services.AddScoped<ICreditNoteService, CreditNoteService>();
            services.AddScoped<IZonaService, ZonaService>();
            services.AddScoped<IProductLineService, ProductLineService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICommissionService, CommissionService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ISellerService, SellerService>();
            services.AddScoped<IProVentaService, ProVentaService>();

            services.AddTransient<DeliveryNotesViewModel>();
            services.AddTransient<AccountsReceivableViewModel>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<PaymentsViewModel>();
            services.AddTransient<CommissionsViewModel>();
            services.AddTransient<CustomerViewModel>();
            services.AddTransient<CustomerHistoryViewModel>();
            services.AddTransient<SellersViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<ProVentaViewModel>();
            services.AddTransient<CreditNotesViewModel>();
            services.AddTransient<CreditNotesReportViewModel>();
            
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
        }
    }
}