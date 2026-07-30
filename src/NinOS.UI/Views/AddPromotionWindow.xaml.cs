using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AddPromotionWindow : Window
    {
        public AddPromotionWindow()
        {
            InitializeComponent();
            
            this.Loaded += (s, e) =>
            {
                if (DataContext is InventoryViewModel view_model)
                {
                    view_model.on_close_add_promotion_window = () =>
                    {
                        this.Close();
                    };
                }
            };
        }
    }
}