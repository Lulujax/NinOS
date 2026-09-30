using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AddPromotionWindow : Window
    {
        private InventoryViewModel? _view_model;

        public AddPromotionWindow()
        {
            InitializeComponent();

            this.DataContextChanged += OnViewModelChanged;
            this.Loaded += (s, e) =>
            {
                if (_view_model != null)
                {
                    _view_model.on_close_add_promotion_window = () => this.Close();
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
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnViewModelChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_view_model != null)
            {
                _view_model.PropertyChanged -= OnViewModelPropertyChanged;
                _view_model.builder_items.CollectionChanged -= OnBuilderItemsChanged;
            }

            _view_model = e.NewValue as InventoryViewModel;

            if (_view_model != null)
            {
                _view_model.PropertyChanged += OnViewModelPropertyChanged;
                _view_model.builder_items.CollectionChanged += OnBuilderItemsChanged;
            }

            refresh_window();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryViewModel.promo_type_index)
                || e.PropertyName == nameof(InventoryViewModel.promo_search_query)
                || e.PropertyName == nameof(InventoryViewModel.is_editing_promotion))
            {
                refresh_window();
            }
        }

        private void OnBuilderItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            refresh_window();
        }

        private void refresh_window()
        {
            if (_view_model == null) return;

            bool editando = _view_model.is_editing_promotion;
            int tipo = _view_model.promo_type_index;

            this.Title = editando ? "Editar Promocion" : "Nueva Promocion";

            SearchEmptyText.Visibility = !string.IsNullOrWhiteSpace(_view_model.promo_search_query)
                                          && _view_model.promo_search_results.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            CompositionGrid.ToolTip = tipo == 0
                ? "Oferta individual: un solo producto con cantidad 1."
                : "Arma el kit o combo con los productos y las cantidades que forman la promocion.";

            HintText.Text = tipo switch
            {
                0 => _view_model.builder_items.Count > 0
                    ? "OFERTA INDIVIDUAL: ya tiene un producto. Para cambiarlo, quitalo con la X y elige otro."
                    : "OFERTA INDIVIDUAL: elija un solo producto, se guarda con cantidad 1 y codigo OF#####.",
                1 => "KIT: se guarda con codigo KIT#####. Todas las cantidades deben ser 1 o mas.",
                _ => "COMBO: se guarda con codigo COM#####. Todas las cantidades deben ser 1 o mas."
            };
        }

        private void OnPricePreview(object sender, TextCompositionEventArgs e)
        {
            InputRestrictions.numbers_only(sender, e);
        }
    }
}
