using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CustomerView : UserControl
    {
        public CustomerView()
        {
            InitializeComponent();
            DataContextChanged += UserControl_DataContextChanged;
        }

        private void SetupEvents()
        {
            if (DataContext is CustomerViewModel viewModel)
            {
                viewModel.OnRequestAddCustomerWindow = () =>
                {
                    AddCustomerWindow window = new AddCustomerWindow();
                    window.DataContext = viewModel;
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                };

                viewModel.OnRequestEditCustomerWindow = (CustomerRowDto selected) =>
                {
                    AddCustomerWindow window = new AddCustomerWindow();
                    window.DataContext = viewModel;
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                };

                viewModel.EditZonasRequested = () =>
                {
                    var serviceProvider = (Application.Current as App)?.GetServiceProvider();
                    var zonaService = serviceProvider?.GetService(typeof(NinOS.Infrastructure.Services.Interfaces.IZonaService)) as NinOS.Infrastructure.Services.Interfaces.IZonaService;
                    if (zonaService != null)
                    {
                        var vm = new ZonaEditViewModel(zonaService);
                        var wnd = new EditZonasWindow
                        {
                            DataContext = vm,
                            Owner = Window.GetWindow(this)
                        };
                        wnd.ShowDialog();
                        _ = viewModel.LoadZonasAsync();
                    }
                };
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetupEvents();
        }
    }
}
