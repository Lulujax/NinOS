using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AddSellerWindow : Window
    {
        public AddSellerWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (DataContext is SellersViewModel vm)
                {
                    vm.OnCloseAddSellerWindow = () => Close();
                }
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
