using System.Windows;
using System.Windows.Input;
using NinOS.UI.Common;

namespace NinOS.UI.Views
{
    public partial class AddProductWindow : Window
    {
        public AddProductWindow()
        {
            InitializeComponent();
        }

        private void OnQuantityPreview(object sender, TextCompositionEventArgs e)
        {
            InputRestrictions.digits_only(sender, e);
        }

        private void OnPricePreview(object sender, TextCompositionEventArgs e)
        {
            InputRestrictions.numbers_only(sender, e);
        }
    }
}