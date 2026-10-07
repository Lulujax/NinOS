using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class EditProductLinesWindow : Window
    {
        public EditProductLinesWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                if (DataContext == null)
                {
                    var sp = (Application.Current as App)?.GetServiceProvider();
                    var lineService = sp?.GetService(typeof(NinOS.Infrastructure.Services.Interfaces.IProductLineService)) as NinOS.Infrastructure.Services.Interfaces.IProductLineService;
                    if (lineService != null)
                    {
                        DataContext = new ProductLineEditViewModel(lineService);
                    }
                }

                if (DataContext is ProductLineEditViewModel vm)
                {
                    vm.OnCloseWindow = () => Close();
                    await vm.LoadLineasAsync();
                }
            };
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
