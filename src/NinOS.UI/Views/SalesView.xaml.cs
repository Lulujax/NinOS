using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class SalesView : UserControl
    {
        public SalesView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => SetupEvents();
        }

        private void SetupEvents()
        {
            if (DataContext is SalesViewModel vm)
            {
                vm.on_request_preview_window = async (note) =>
                {
                    try
                    {
                        note_print_dto printable = await vm.get_printable_note_async(note.id_delivery_note);
                        NotePreviewWindow preview = new NotePreviewWindow(printable, view_only: true);
                        preview.Owner = Window.GetWindow(this);
                        preview.ShowDialog();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Error al cargar la nota: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
            }
        }
    private void txt_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_clear_search.Visibility = !string.IsNullOrEmpty(txt_search.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void btn_clear_search_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SalesViewModel vm) vm.search_query = "";
        }

        private void GoalTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;
            e.Handled = true;
            if (DataContext is SalesViewModel vm) vm.save_goal_command.Execute(null);
        }

        private void DataGrid_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.CurrentColumn?.Header?.ToString() != "OBSERVACION") return;
            if (grid.SelectedItem is not accounts_receivable_dto row) return;
            row.saved_observations = row.sales_observations;
            row.is_editing_observations = true;
            FocusObservationsTextBox(e);
        }

        private static void FocusObservationsTextBox(System.Windows.Input.MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as System.Windows.DependencyObject;
            while (source != null && source is not DataGridCell)
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            if (source is not DataGridCell cell) return;

            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Input,
                new System.Action(() =>
                {
                    var textbox = FindVisualChild<TextBox>(cell);
                    if (textbox == null) return;
                    textbox.Focus();
                    textbox.CaretIndex = textbox.Text.Length;
                }));
        }

        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private async void ObsTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is not TextBox textbox) return;
            if (textbox.DataContext is not accounts_receivable_dto row) return;
            if (!row.is_editing_observations) return;

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                string text = textbox.Text?.Trim() ?? string.Empty;
                row.is_editing_observations = false;
                if (DataContext is SalesViewModel vm)
                    await vm.save_observations_async(row, text);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                row.sales_observations = row.saved_observations ?? string.Empty;
                row.is_editing_observations = false;
            }
        }

        private async void ObsTextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox textbox) return;
            if (textbox.DataContext is not accounts_receivable_dto row) return;
            if (!row.is_editing_observations) return;
            string text = textbox.Text?.Trim() ?? string.Empty;
            row.is_editing_observations = false;
            if (DataContext is SalesViewModel vm)
                await vm.save_observations_async(row, text);
        }
    }
}
