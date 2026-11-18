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
using NinOS.UI.Updates;
using NinOS.UI.Views;

namespace NinOS.UI
{
    public partial class App : Application
    {
        private ServiceProvider? _service_provider;
        private static readonly string _error_log_path = Path.Combine(AppContext.BaseDirectory, "ninos-ui-error.log");

        public App()
        {
            Velopack.VelopackApp.Build().Run();

            DispatcherUnhandledException += (s, e) =>
            {
                log_error("DispatcherUnhandledException", e.Exception);
                e.Handled = true;
                try
                {
                    AppDialog.Show(
                        ErrorText.Get(e.Exception),
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
            base.OnStartup(e);

            // Argumento oculto para crear o cambiar la clave del Panel Admin sin entrar al
            // sistema. Es la unica forma de ponerla la primera vez, porque ya no existe una
            // clave por defecto escrita en el codigo.
            if (Array.Exists(e.Args, argument => string.Equals(argument, SetAdminPasswordArgument, StringComparison.OrdinalIgnoreCase)))
            {
                run_set_admin_password();
                return;
            }

            // Argumento oculto para cargar los datos iniciales en una base NUEVA. Se niega
            // si la base ya tiene datos: el arranque normal no siembra nada (spec 007).
            if (Array.Exists(e.Args, argument => string.Equals(argument, SeedInitialDataArgument, StringComparison.OrdinalIgnoreCase)))
            {
                run_seed_initial_data();
                return;
            }

            try
            {
                log_startup_message("OnStartup begin");

                // Antes que la pantalla de carga: así todas las ventanas, incluidas las
                // que se abren después, respetan la escala guardada.
                UiScale.init();

                // Los botones van en mayúsculas (solo visual, la BD y los PDF no cambian).
                UpperCaseText.EnableGlobally();

                // La conexion se valida antes de la pantalla de carga: sin ella no hay nada
                // que mostrar, y asi el usuario ve el motivo real en vez de un error de red.
                if (!DbConnectionFactory.TryGetConnectionString(out _, out string connection_source))
                {
                    log_startup_message("OnStartup abortado: falta la configuracion de conexion");
                    AppDialog.Show(
                        DbConnectionFactory.MissingConfigurationMessage,
                        "Falta la configuracion de conexion");
                    Shutdown();
                    return;
                }

                log_startup_message($"Conexion configurada desde {connection_source}");

                SplashWindow splash = new SplashWindow();
                splash.ShowWithAnimation();

                ServiceCollection service_collection = new ServiceCollection();
                configure_services(service_collection);
                _service_provider = service_collection.BuildServiceProvider();

                splash.ExitRequested += (s, args) =>
                {
                    Current.Shutdown();
                };

                splash.RetryRequested += (s, args) =>
                {
                    start_startup_pipeline(splash);
                };

                start_startup_pipeline(splash);
            }
            catch (Exception ex)
            {
                log_startup_message("OnStartup critical exception", ex);
                AppDialog.Show(
                    "NinOS no pudo iniciar.\n\n" + ErrorText.Get(ex),
                    "No se pudo iniciar NinOS");
                Current.Shutdown();
            }
        }

        private void start_startup_pipeline(SplashWindow splash)
        {
            DateTime started_at = DateTime.Now;

            System.Threading.Tasks.Task.Run(async () =>
            {
                await run_update_check_async(splash);

                log_startup_message("Before DbInitializer (attempt)");

                // Cada intento crea su propio scope limpio para evitar instancias de DbContext o conexiones dañadas
                using (var scope = _service_provider!.CreateScope())
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
                log_startup_message("After DbInitializer (success)");
            }).ContinueWith(previous =>
            {
                if (previous.IsFaulted)
                {
                    Exception exception = previous.Exception?.GetBaseException() ?? new Exception("Error de conexión");
                    log_startup_message("Startup task faulted", exception);

                    // El mensaje dice la causa real (configuracion, red, credenciales, base
                    // inexistente, permisos o migracion) en vez de culpar siempre a internet.
                    startup_diagnosis diagnostico = StartupDiagnosis.describe(exception);
                    log_startup_message($"Causa detectada del fallo de arranque: {diagnostico.cause}");

                    Dispatcher.BeginInvoke(() =>
                    {
                        splash.SetError(diagnostico.mensaje);
                    });
                    return;
                }

                Dispatcher.BeginInvoke(() =>
                {
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
                            log_startup_message("OnStartup end");
                        };
                        build_timer.Start();
                    };
                    ready_timer.Start();
                });
            });
        }

        private async System.Threading.Tasks.Task run_update_check_async(SplashWindow splash)
        {
            IAppUpdater updater = _service_provider!.GetRequiredService<IAppUpdater>();
            show_splash_status(splash, "Buscando actualizaciones...");

            update_check_result check = await updater.check_for_update_async(System.Threading.CancellationToken.None);
            if (!check.is_available)
            {
                if (check.error_message != null) show_splash_status(splash, check.error_message);
                return;
            }

            try
            {
                show_splash_status(splash, $"Descargando la version {check.available_version}...");
                var progress = new Progress<int>(percent => show_splash_status(splash, $"Descargando actualizacion {percent}%"));
                await updater.download_update_async(progress, System.Threading.CancellationToken.None);

                show_splash_status(splash, "Aplicando actualizacion, NinOS se reiniciara...");
                updater.apply_update_and_restart();
            }
            catch (Exception exception)
            {
                log_startup_message("Actualizacion fallida; se sigue con la version actual", exception);
                show_splash_status(splash, "No se pudo aplicar la actualizacion. Se sigue con la version actual.");
            }
        }

        private static void show_splash_status(SplashWindow splash, string text)
        {
            splash.Dispatcher.BeginInvoke(() => splash.SetStatus(text));
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

        private const string SeedInitialDataArgument = "--seed-initial-data";

        private void run_seed_initial_data()
        {
            UiScale.init();

            if (!DbConnectionFactory.TryGetConnectionString(out _, out string origen))
            {
                AppDialog.Show(DbConnectionFactory.MissingConfigurationMessage, "Datos iniciales");
                Shutdown();
                return;
            }

            try
            {
                ServiceCollection service_collection = new ServiceCollection();
                configure_services(service_collection);

                using ServiceProvider provider = service_collection.BuildServiceProvider();
                using var scope = provider.CreateScope();
                NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();

                db_context.Database.Migrate();

                if (!DbSeeder.esta_vacia(db_context))
                {
                    AppDialog.Show(DbSeeder.MensajeBaseConDatos, "Datos iniciales",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Shutdown();
                    return;
                }

                MessageBoxResult confirmacion = AppDialog.Show(
                    $"La base configurada en {origen} esta vacia. Se van a cargar los datos iniciales (tipos de nota, zonas, vendedoras, lineas de producto y catalogo). Desea continuar?",
                    "Datos iniciales",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmacion != MessageBoxResult.Yes)
                {
                    Shutdown();
                    return;
                }

                DbSeeder.seed_initial_data(db_context);
                AppDialog.Show("Datos iniciales cargados correctamente.", "Datos iniciales");
            }
            catch (Exception ex)
            {
                log_startup_message("Fallo la carga de datos iniciales", ex);
                AppDialog.Show(
                    "No se pudieron cargar los datos iniciales." + Environment.NewLine + Environment.NewLine + ErrorText.Get(ex),
                    "Datos iniciales",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            Shutdown();
        }

        private const string SetAdminPasswordArgument = "--set-admin-password";

        private void run_set_admin_password()
        {
            UiScale.init();

            PromptWindow first = new PromptWindow(
                "Clave del Panel Admin",
                $"Escriba la clave nueva (minimo {AdminSecurity.MinimumPasswordLength} caracteres).");
            first.ShowDialog();

            if (!first.Accepted || first.Value.Length < AdminSecurity.MinimumPasswordLength)
            {
                AppDialog.Show("No se cambio la clave del Panel Admin.", "Panel Admin");
                Shutdown();
                return;
            }

            PromptWindow confirmation = new PromptWindow(
                "Clave del Panel Admin",
                "Repita la clave para confirmar.");
            confirmation.ShowDialog();

            if (!confirmation.Accepted || !string.Equals(first.Value, confirmation.Value, StringComparison.Ordinal))
            {
                AppDialog.Show("Las dos claves no coinciden. No se cambio nada.", "Panel Admin");
                Shutdown();
                return;
            }

            if (AdminSecurity.change_password(confirmation.Value))
            {
                AppDialog.Show("Clave del Panel Admin guardada en la base de datos.", "Panel Admin");
            }
            else
            {
                AppDialog.Show($"No se pudo guardar la clave. El detalle quedo en: {AppLog.FilePath}", "Panel Admin");
            }

            Shutdown();
        }

        private static IAppUpdater create_app_updater()
        {
            if (!UpdateSettings.try_get_base_url(out string base_url, out string reason))
            {
                AppLog.Warn($"Actualizaciones desactivadas: {reason}");
                return new DisabledAppUpdater();
            }

            try
            {
                return new VelopackAppUpdater(base_url);
            }
            catch (Exception exception)
            {
                AppLog.Warn($"Actualizaciones desactivadas: el actualizador no se pudo iniciar ({exception.GetType().Name}).");
                return new DisabledAppUpdater();
            }
        }

        private void configure_services(ServiceCollection services)
        {
            services.AddDbContext<NinOSDbContext>(options =>
            {
                // Sin silenciar PendingModelChangesWarning: si alguien cambia una entidad y
                // olvida crear la migracion, la app lo dice en vez de trabajar con la base
                // desalineada del modelo.
                options.UseNpgsql(DbConnectionFactory.GetConnectionString());
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
            services.AddScoped<IPriceListService, PriceListService>();
            services.AddSingleton<IAppUpdater>(_ => create_app_updater());

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
            services.AddTransient<VesuprontaViewModel>();
            services.AddTransient<CreditNotesViewModel>();
            services.AddTransient<CreditNotesReportViewModel>();
            
            NinOS.UI.Updates.UpdatePolicyServices.add_update_policy_services(services);
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
        }
    }
}
