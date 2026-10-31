using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class EditListsWindow : Window
    {
        public EditListsWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                if (DataContext is PriceListManagerViewModel vm)
                {
                    await vm.LoadAsync();
                }
            };
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
