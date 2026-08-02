using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Repositories.Implementations;
using NinOS.Infrastructure.Repositories.Interfaces;
using NinOS.Infrastructure.Services.Implementations;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common.ViewModels;
using NinOS.UI.Views;

namespace NinOS.UI
{
    public partial class App : Application
    {
        private ServiceProvider? _service_provider;
        private static readonly string _startup_log_path = Path.Combine(AppContext.BaseDirectory, "ninos-ui-startup.log");

        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                log_startup_message("DispatcherUnhandledException", e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception exception)
                {
                    log_startup_message("AppDomain.UnhandledException", exception);
                }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                log_startup_message("TaskScheduler.UnobservedTaskException", e.Exception);
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
                ServiceCollection service_collection = new ServiceCollection();
                configure_services(service_collection);
                _service_provider = service_collection.BuildServiceProvider();

                using (IServiceScope scope = _service_provider.CreateScope())
                {
                    log_startup_message("Before DbInitializer");
                    NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                    DbInitializer.initialize(db_context);
                    log_startup_message("After DbInitializer");
                }

                log_startup_message("Before MainWindow resolve");
                MainWindow main_window = _service_provider.GetRequiredService<MainWindow>();
                log_startup_message("Before MainWindow show");
                main_window.Show();
                
                base.OnStartup(e);
                log_startup_message("OnStartup end");
            }
            catch (Exception ex)
            {
                log_startup_message("OnStartup exception", ex);
                MessageBox.Show(ex.Message + "\n" + ex.InnerException?.Message, "error");
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

            File.AppendAllText(_startup_log_path, log_entry + Environment.NewLine + Environment.NewLine);
        }

        private void configure_services(ServiceCollection services)
        {
            string connection_string = "Host=localhost;Database=ninos_db;Username=postgres;Password=1234";
            
            services.AddDbContext<NinOSDbContext>(options => options.UseNpgsql(connection_string));
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IDeliveryNoteService, DeliveryNoteService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICommissionService, CommissionService>();
            services.AddScoped<ICustomerService, CustomerService>();

            services.AddTransient<DeliveryNotesViewModel>();
            services.AddTransient<AccountsReceivableViewModel>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<PaymentsViewModel>();
            services.AddTransient<CommissionsViewModel>();
            services.AddTransient<CustomerViewModel>();
            services.AddTransient<InventoryViewModel>();
            
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
        }
    }
}