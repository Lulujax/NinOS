using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Repositories.Implementations;
using NinOS.Infrastructure.Repositories.Interfaces;
using NinOS.Infrastructure.Services.Implementations;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common.ViewModels;
using NinOS.UI.ViewModels;
using NinOS.UI.Views;

namespace NinOS.UI
{
    public partial class App : Application
    {
        private ServiceProvider? _service_provider;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                ServiceCollection service_collection = new ServiceCollection();
                configure_services(service_collection);
                _service_provider = service_collection.BuildServiceProvider();

                using (IServiceScope scope = _service_provider.CreateScope())
                {
                    NinOSDbContext db_context = scope.ServiceProvider.GetRequiredService<NinOSDbContext>();
                    DbInitializer.initialize(db_context);
                }

                MainWindow main_window = _service_provider.GetRequiredService<MainWindow>();
                main_window.Show();
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.InnerException?.Message, "error");
                Current.Shutdown();
            }
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

            services.AddTransient<DeliveryNotesViewModel>();
            services.AddTransient<AccountsReceivableViewModel>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<PaymentsViewModel>();
            services.AddTransient<CommissionsViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
        }
    }
}