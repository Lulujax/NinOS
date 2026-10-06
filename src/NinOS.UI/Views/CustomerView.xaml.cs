using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

                viewModel.OnRequestCustomerHistoryWindow = (CustomerRowDto selected) =>
                {
                    OpenCustomerHistory(selected);
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

        private void CustomerGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IsInsideButton(e.OriginalSource as DependencyObject)) return;

            if (sender is DataGrid dg && dg.SelectedItem is CustomerRowDto selected)
            {
                OpenCustomerHistory(selected);
            }
        }

        private void OpenCustomerHistory(CustomerRowDto selected)
        {
            if (selected?.CustomerRef == null) return;

            var serviceProvider = (Application.Current as App)?.GetServiceProvider();
            if (serviceProvider == null) return;

            var vm = serviceProvider.GetService(typeof(CustomerHistoryViewModel)) as CustomerHistoryViewModel;
            if (vm != null)
            {
                _ = vm.LoadHistoryAsync(selected.CustomerRef.id_customer);
                var window = new CustomerHistoryWindow(vm, serviceProvider)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
        }

        private static bool IsInsideButton(DependencyObject? source)
        {
            try
            {
                while (source != null)
                {
                    if (source is Button) return true;
                    source = System.Windows.Media.VisualTreeHelper.GetParent(source);
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetupEvents();
        }
    }
}
