using System.Windows;
using System.Windows.Input;
using NinOS.UI.Common;
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

        private void OnPricePreview(object sender, TextCompositionEventArgs e)
        {
            InputRestrictions.numbers_only(sender, e);
        }
    }
}