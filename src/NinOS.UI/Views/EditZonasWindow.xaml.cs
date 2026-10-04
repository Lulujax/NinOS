using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class EditZonasWindow : Window
    {
        public EditZonasWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                if (DataContext is ZonaEditViewModel vm)
                {
                    vm.OnCloseWindow = () => Close();
                    await vm.LoadZonasAsync();
                }
            };
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
