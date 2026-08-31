using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using NinOS.UI.Common.ViewModels;

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
                }
            };
        }

        private async void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not InventoryViewModel view_model) return;
            if (sender is not DataGrid grid) return;
            if (grid.SelectedItem is not inventory_item_dto dto) return;
            if (dto.is_promotion || dto.product_ref == null) return;

            try
            {
                var history = await view_model.get_product_history_async(dto.product_ref.id_product);

                var window = new ProductSalesHistoryWindow(dto.product_ref, history);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error al cargar el historial: {ex.Message}", "Error");
            }
        }
    }
}