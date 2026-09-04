using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private bool TryGetTabItem(DependencyObject? source, out TabItem? tab_item)
        {
            tab_item = null;
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is TabItem item)
                {
                    tab_item = item;
                    return true;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void MainTabControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel == null) return;
            if (e.OriginalSource is not DependencyObject source) return;

            if (!TryGetTabItem(source, out TabItem? target)) return;
            if (target == null) return;

            int target_index = MainTabControl.Items.IndexOf(target);
            if (target_index == 0) return;

            if (_viewModel.delivery_notes_vm?.has_pending_data == true)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Hay datos sin guardar en la Nota de Entrega. Si sales de esta pestaña se perderán y el stock será liberado.\n\n¿Desea salir de todas formas?",
                    "Datos sin guardar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Handled = true;
                    return;
                }

                _viewModel.delivery_notes_vm.reset_unsaved_note();
                MainTabControl.SelectedIndex = target_index;
                e.Handled = true;
            }
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl) return;
            if (_viewModel == null) return;

            int tab_index = MainTabControl.SelectedIndex;

            switch (tab_index)
            {
                case 0:
                    _viewModel.delivery_notes_vm?.refresh_data();
                    break;
                case 1:
                    _viewModel.accounts_receivable_vm?.refresh_data();
                    break;
                case 2:
                    _viewModel.sales_vm?.refresh_data();
                    break;
                case 3:
                    _viewModel.payments_vm?.refresh_data();
                    break;
                case 4:
                    _viewModel.commissions_vm?.refresh_data();
                    break;
                case 6:
                    _viewModel.inventory_vm?.refresh_data();
                    break;
            }
        }
    }
}
