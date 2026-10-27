using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class EditPriceListsWindow : Window
    {
        public EditPriceListsWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                if (DataContext is PriceListEditViewModel vm)
                {
                    await vm.LoadPriceListsAsync();
                }
            };
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
