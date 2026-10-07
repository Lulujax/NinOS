using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using NinOS.UI.Common.ViewModels;
using NinOS.UI.Common;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Views
{
    public partial class InventoryView : UserControl
    {
        private Window? _active_product_window;
        private Window? _active_promotion_window;

        public InventoryView()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                if (DataContext is InventoryViewModel view_model)
                {
                    view_model.on_request_add_window = () =>
                    {
                        if (_active_product_window != null)
                        {
                            _active_product_window.Focus();
                            return;
                        }
                        
                        _active_product_window = new AddProductWindow();
                        _active_product_window.DataContext = view_model;
                        _active_product_window.Owner = Application.Current.MainWindow;
                        _active_product_window.Closed += (sender, args) => _active_product_window = null;
                        _active_product_window.Show();
                    };

                    view_model.on_close_add_window = () =>
                    {
                        if (_active_product_window != null)
                        {
                            _active_product_window.Close();
                            _active_product_window = null;
                        }
                    };

                    view_model.on_request_add_promotion_window = () =>
                    {
                        if (_active_promotion_window != null)
                        {
                            _active_promotion_window.Focus();
                            return;
                        }
                        
                        _active_promotion_window = new AddPromotionWindow();
                        _active_promotion_window.DataContext = view_model;
                        _active_promotion_window.Owner = Application.Current.MainWindow;
                        _active_promotion_window.Closed += (sender, args) => _active_promotion_window = null;
                        _active_promotion_window.Show();
                    };

                    view_model.on_close_add_promotion_window = () =>
                    {
                        if (_active_promotion_window != null)
                        {
                            _active_promotion_window.Close();
                            _active_promotion_window = null;
                        }
                    };

                    view_model.on_request_edit_lines_window = () =>
                    {
                        var serviceProvider = (Application.Current as App)?.GetServiceProvider();
                        var lineService = serviceProvider?.GetService(typeof(NinOS.Infrastructure.Services.Interfaces.IProductLineService)) as NinOS.Infrastructure.Services.Interfaces.IProductLineService;
                        if (lineService != null)
                        {
                            var vm = new ProductLineEditViewModel(lineService);
                            var wnd = new EditProductLinesWindow
                            {
                                DataContext = vm,
                                Owner = Window.GetWindow(this) ?? Application.Current.MainWindow
                            };
                            wnd.ShowDialog();
                        }
                    };
                }
            };
        }

        private void on_generate_price_list_click(object sender, RoutedEventArgs e)
        {
            popup_price_list.IsOpen = !popup_price_list.IsOpen;
        }

        private void on_download_price_list_click(object sender, RoutedEventArgs e)
        {
            popup_price_list.IsOpen = false;

            if (DataContext is InventoryViewModel view_model)
            {
                view_model.generate_price_list_command.Execute(null);
            }
        }

        private async void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not InventoryViewModel view_model) return;
            if (sender is not DataGrid grid) return;
            if (grid.SelectedItem is not inventory_item_dto dto) return;
            if (dto.product_ref == null && dto.promo_ref == null) return;

            try
            {
                if (dto.is_promotion && dto.promo_ref != null)
                {
                    var promo_history = await view_model.get_promotion_history_async(dto.promo_ref.id_promotion);

                    var promo_window = new PromotionSalesHistoryWindow(dto.promo_ref, promo_history);
                    promo_window.Owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
                    promo_window.ShowDialog();
                    return;
                }

                if (dto.product_ref == null)
                {
                    AppDialog.Show("No se encontró la información del producto.\nCierra la pantalla y vuelve a abrirla.", "No se pudo ver el historial");
                    return;
                }

                var history = await view_model.get_product_history_async(dto.product_ref.id_product);

                var window = new ProductSalesHistoryWindow(dto.product_ref, history);
                window.Owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
                window.ShowDialog();
            }
            catch (System.Exception ex)
            {
                AppLog.Error($"Historial de producto: no se pudo abrir para '{dto.item_name}'.", ex);
                AppDialog.Show(
                    "No se pudo abrir el historial.\n\n" + ErrorText.Get(ex, "historial de producto") +
                    "\n\nCierra la pantalla de Inventario, ábrela de nuevo e inténtalo otra vez.",
                    "No se pudo ver el historial");
            }
        }
    }
}