using System.Windows;
using System.Windows.Input;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AddProductWindow : Window
    {
        public AddProductWindow()
        {
            InitializeComponent();

            this.DataContextChanged += (s, e) =>
            {
                if (e.NewValue is InventoryViewModel view_model)
                {
                    this.Title = view_model.is_editing_product ? "Editar Producto" : "Nuevo Producto";
                }
            };

            // Esta ventana se abre con Show(), no con ShowDialog(), asi que el
            // cierre hay que hacerlo a mano (IsCancel no cierra ventanas no modales).
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };

            this.Loaded += (s, e) =>
            {
                InputRestrictions.attach_integer(QuantityBox);
                InputRestrictions.attach_decimal(PriceBox);
            };
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
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
