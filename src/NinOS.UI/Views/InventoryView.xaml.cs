using System.Windows.Controls;
using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                if (DataContext is InventoryViewModel view_model)
                {
                    view_model.on_request_add_window = () =>
                    {
                        AddProductWindow window = new AddProductWindow();
                        window.DataContext = view_model;
                        window.ShowDialog();
                    };

                    view_model.on_request_add_promotion_window = () =>
                    {
                        AddPromotionWindow promo_window = new AddPromotionWindow();
                        promo_window.DataContext = view_model;
                        promo_window.Owner = Application.Current.MainWindow;
                        promo_window.Show();
                    };
                }
            };
        }
    }
}