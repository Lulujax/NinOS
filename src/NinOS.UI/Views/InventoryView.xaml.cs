using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class InventoryView : UserControl
    {
        private AddProductWindow? _add_window;

        public InventoryView()
        {
            InitializeComponent();
            DataContextChanged += on_data_context_changed;
        }

        private void on_data_context_changed(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is InventoryViewModel view_model)
            {
                view_model.on_request_add_window = () =>
                {
                    _add_window = new AddProductWindow();
                    _add_window.DataContext = view_model;
                    _add_window.Owner = System.Windows.Window.GetWindow(this);
                    _add_window.ShowDialog();
                };

                view_model.on_close_add_window = () =>
                {
                    _add_window?.Close();
                    _add_window = null;
                };
            }
        }
    }
}