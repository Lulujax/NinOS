using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class SellersView : UserControl
    {
        public SellersView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => SetupEvents();
        }

        private void SetupEvents()
        {
            if (DataContext is SellersViewModel viewModel)
            {
                viewModel.OnRequestAddSellerWindow = () =>
                {
                    AddSellerWindow window = new AddSellerWindow();
                    window.DataContext = viewModel;
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                };

                viewModel.OnRequestEditSellerWindow = (SellerRowDto selected) =>
                {
                    AddSellerWindow window = new AddSellerWindow();
                    window.DataContext = viewModel;
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                };
            }
        }
    }
}
