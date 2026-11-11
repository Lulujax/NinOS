using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NinOS.Infrastructure.Logging;
using NinOS.UI.Common;
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
            Closed += (s, e) => AppLog.Info("Aplicación cerrada");
            Closing += (s, e) =>
            {
                if (_viewModel?.delivery_notes_vm?.has_pending_data == true)
                {
                    string message = _viewModel.delivery_notes_vm.note_details.Count > 0
                        ? "Hay datos sin guardar en la Nota de Entrega. Si sales de la aplicación se perderán y el stock será liberado.\n\n¿Desea salir de todas formas?"
                        : "Hay cambios sin guardar en la Nota de Entrega (datos de pago, cliente u observaciones). Si sales de la aplicación se perderán los cambios.\n\n¿Desea salir de todas formas?";

                    MessageBoxResult result = AppDialog.Show(
                        message,
                        "Datos sin guardar",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        e.Cancel = true;
                        return;
                    }

                    _viewModel.delivery_notes_vm.reset_unsaved_note();
                }
            };
        }

        private DateTime _last_title_click;
        private int _title_click_count;

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModifierKeys modifiers = Keyboard.Modifiers;

            if (e.Key == Key.F12 && modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                TryOpenAdminLogin();
                return;
            }

            if (e.Key == Key.P && modifiers.HasFlag(ModifierKeys.Control) && modifiers.HasFlag(ModifierKeys.Alt))
            {
                e.Handled = true;
                TryOpenAdminLogin();
            }
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DateTime now = DateTime.Now;
            if (_title_click_count == 0 || (now - _last_title_click).TotalMilliseconds > 1500)
                _title_click_count = 1;
            else
                _title_click_count++;

            _last_title_click = now;

            if (_title_click_count >= 5)
            {
                _title_click_count = 0;
                TryOpenAdminLogin();
            }
        }

        private void TryOpenAdminLogin()
        {
            var login = new AdminLoginWindow();
            login.Owner = this;
            login.ShowDialog();

            if (!login.Unlocked) return;

            AppLog.Info("Acceso admin autorizado");
            var panel = new AdminPanelWindow();
            panel.Owner = this;
            panel.ShowDialog();
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
            if (target_index == MainTabControl.SelectedIndex) return;

            if (MainTabControl.SelectedIndex == 0 && _viewModel.delivery_notes_vm?.has_pending_data == true)
            {
                string message = _viewModel.delivery_notes_vm.note_details.Count > 0
                    ? "Hay datos sin guardar en la Nota de Entrega. Si sales de esta pestaña se perderán y el stock será liberado.\n\n¿Desea salir de todas formas?"
                    : "Hay cambios sin guardar en la Nota de Entrega (datos de pago, cliente u observaciones). Si sales de esta pestaña se perderán los cambios.\n\n¿Desea salir de todas formas?";

                MessageBoxResult result = AppDialog.Show(
                    message,
                    "Datos sin guardar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    e.Handled = true;
                    return;
                }

                _viewModel.delivery_notes_vm.reset_unsaved_note();
                MainTabControl.SelectedIndex = target_index;
                e.Handled = true;
            }
        }

        private void MainTabControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;
            if (MainTabControl.SelectedIndex != 0) return;
            if (_viewModel.delivery_notes_vm?.has_pending_data != true) return;

            bool isTabKey = e.Key == Key.Tab && Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool isArrowOnTab = (e.Key == Key.Left || e.Key == Key.Right) && (e.OriginalSource is TabItem || Keyboard.FocusedElement is TabItem);

            if (isTabKey || isArrowOnTab)
            {
                string message = _viewModel.delivery_notes_vm.note_details.Count > 0
                    ? "Hay datos sin guardar en la Nota de Entrega. Si sales de esta pestaña se perderán y el stock será liberado.\n\n¿Desea salir de todas formas?"
                    : "Hay cambios sin guardar en la Nota de Entrega (datos de pago, cliente u observaciones). Si sales de esta pestaña se perderán los cambios.\n\n¿Desea salir de todas formas?";

                MessageBoxResult result = AppDialog.Show(
                    message,
                    "Datos sin guardar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    e.Handled = true;
                    return;
                }

                _viewModel.delivery_notes_vm.reset_unsaved_note();
            }
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.Source, MainTabControl)) return;
            if (_viewModel == null) return;

            int tab_index = MainTabControl.SelectedIndex;

            switch (tab_index)
            {
                case 0:
                    AppLog.Info("Módulo: Notas de Entrega");
                    _viewModel.delivery_notes_vm?.refresh_data();
                    break;
                case 1:
                    AppLog.Info("Módulo: Cuentas por Cobrar");
                    _viewModel.accounts_receivable_vm?.refresh_data();
                    break;
                case 2:
                    AppLog.Info("Módulo: Ventas");
                    _viewModel.sales_vm?.refresh_data();
                    break;
                case 3:
                    AppLog.Info("Módulo: Pagos");
                    _viewModel.payments_vm?.refresh_data();
                    break;
                case 4:
                    AppLog.Info("Módulo: Comisiones");
                    _viewModel.commissions_vm?.refresh_data();
                    break;
                case 6:
                    AppLog.Info("Módulo: Inventario");
                    _viewModel.inventory_vm?.refresh_data();
                    break;
                case 8:
                    AppLog.Info("Módulo: Notas de Credito");
                    _viewModel.credit_notes_vm?.refresh_data();
                    break;
            }
        }
    }
}
