using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AddProductWindow : Window
    {
        private InventoryViewModel? _view_model;

        public AddProductWindow()
        {
            InitializeComponent();

            this.DataContextChanged += (s, e) =>
            {
                if (_view_model != null)
                {
                    _view_model.PropertyChanged -= OnViewModelPropertyChanged;
                    _view_model = null;
                }

                if (e.NewValue is InventoryViewModel view_model)
                {
                    _view_model = view_model;
                    _view_model.PropertyChanged += OnViewModelPropertyChanged;
                    this.Title = view_model.is_editing_product ? "Editar Producto" : "Nuevo Producto";
                }
            };

            // Esta ventana se abre con Show(), no con ShowDialog(), asi que el
            // cierre hay que hacerlo a mano (IsCancel no cierra ventanas no modales).
            //
            // Con el apartado de stock desplegado, Esc cierra el apartado y no la ventana:
            // si cerrara todo, se perderia el producto entero por querer salir del ajuste.
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key != Key.Escape) return;

                if (DataContext is InventoryViewModel view_model && view_model.is_stock_addition_mode)
                {
                    view_model.close_stock_section_command.Execute(null);
                    e.Handled = true;
                    return;
                }

                Close();
                e.Handled = true;
            };

            this.Loaded += (s, e) =>
            {
                InputRestrictions.attach_integer(QuantityBox);
                InputRestrictions.attach_integer(StockMoveBox);
                InputRestrictions.attach_decimal(PriceBox);

                fit_to_screen();
            };

            this.Closed += (s, e) =>
            {
                if (_view_model != null)
                {
                    _view_model.PropertyChanged -= OnViewModelPropertyChanged;
                    _view_model = null;
                }
            };
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // El apartado de stock agranda la ventana. Si esa altura no cabe en la
            // pantalla, se recorta aqui y el ScrollViewer hace el resto.
            if (e.PropertyName == nameof(InventoryViewModel.is_stock_addition_mode))
            {
                fit_to_screen();
            }
        }

        /// <summary>
        /// Deja la ventana dentro del area de trabajo de la pantalla. En monitores bajos
        /// (o con escalado de Windows alto) la altura fija del XAML se salia por abajo y
        /// los botones Guardar/Cancelar quedaban fuera de alcance.
        /// </summary>
        private void fit_to_screen()
        {
            Rect area = SystemParameters.WorkArea;

            // Un margen para que no quede pegada a los bordes de la pantalla.
            double alto_maximo = Math.Max(MinHeight, area.Height - 40);
            double ancho_maximo = Math.Max(MinWidth, area.Width - 40);

            if (Height > alto_maximo) Height = alto_maximo;
            if (Width > ancho_maximo) Width = ancho_maximo;

            // Si la ventana ya no cabe donde estaba centrada, se recentra.
            if (Top + Height > area.Bottom || Top < area.Top)
            {
                Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
            }
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
