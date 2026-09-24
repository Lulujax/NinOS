using System;
using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AccountsReceivableView : UserControl
    {
        public static readonly DependencyProperty PaymentsContextProperty =
            DependencyProperty.Register(nameof(PaymentsContext), typeof(PaymentsViewModel), typeof(AccountsReceivableView),
                new PropertyMetadata(null));

        public PaymentsViewModel? PaymentsContext
        {
            get => (PaymentsViewModel?)GetValue(PaymentsContextProperty);
            set => SetValue(PaymentsContextProperty, value);
        }

        public AccountsReceivableView()
        {
            InitializeComponent();
            DataContextChanged += UserControl_DataContextChanged;
        }

        private void SetupEvents()
        {
            if (DataContext is AccountsReceivableViewModel viewModel)
            {
                viewModel.on_request_preview_window = async (note) =>
                {
                    try
                    {
                        note_print_dto printable = await viewModel.get_printable_note_async(note.id_delivery_note);
                        NotePreviewWindow preview = new NotePreviewWindow(printable);
                        preview.Owner = Window.GetWindow(this);
                        preview.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al cargar la nota: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                viewModel.on_request_confirmation_window = () =>
                {
                    if (viewModel.selected_note == null) return;

                    string message = $"Esta seguro de anular la nota {viewModel.selected_note.note_number}?\nEl stock sera restituido.";
                    MessageBoxImage icon = MessageBoxImage.Question;

                    if (viewModel.selected_note.paid_amount_usd > 0)
                    {
                        message += $"\n\nADVERTENCIA: Esta nota tiene pagos registrados ({viewModel.selected_note.paid_amount_usd:N2} USD).\nSi la anula, esos pagos quedaran asociados a una nota anulada.";
                        icon = MessageBoxImage.Warning;
                    }

                    MessageBoxResult result = MessageBox.Show(
                        message,
                        "Confirmar Anulacion",
                        MessageBoxButton.YesNo,
                        icon);

                    if (result == MessageBoxResult.Yes)
                    {
                        _ = viewModel.confirm_annulation_async();
                    }
                };

                viewModel.on_request_add_payment_for_month = (month) => OpenPaymentWindow(viewModel, month);

                viewModel.on_request_add_payment_for_note = (note) =>
                {
                    string month = note.creation_date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE"));
                    OpenPaymentWindow(viewModel, month, note);
                };
            }
        }

        private void OpenPaymentWindow(AccountsReceivableViewModel ar_vm, string month, accounts_receivable_row_dto? preselected_note = null)
        {
            if (PaymentsContext == null) return;

            accounts_receivable_dto? note = null;
            if (preselected_note != null)
            {
                note = new accounts_receivable_dto
                {
                    id_delivery_note = preselected_note.id_delivery_note,
                    note_number = preselected_note.note_number,
                    customer_name = preselected_note.customer_name,
                    total_amount_usd = preselected_note.total_amount_usd,
                    paid_amount_usd = preselected_note.paid_amount_usd,
                    balance_due_usd = preselected_note.balance_due_usd,
                    status = preselected_note.status
                };
            }

            var window = new AddPaymentWindow(PaymentsContext, month, null, note);
            window.Owner = Window.GetWindow(this);
            window.PaymentRegistered += (_, _) =>
            {
                PaymentsContext.refresh_data();
                ar_vm.refresh_data();
            };
            window.ShowDialog();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetupEvents();
        }

        private void txt_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_clear_search.Visibility = !string.IsNullOrEmpty(txt_search.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void btn_clear_search_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountsReceivableViewModel vm) vm.search_query = "";
        }

        private void DataGrid_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.CurrentColumn?.Header?.ToString() != "OBSERVACION") return;
            if (grid.SelectedItem is not accounts_receivable_row_dto row) return;
            row.saved_observations = row.observations;
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
            if (textbox.DataContext is not accounts_receivable_row_dto row) return;
            if (!row.is_editing_observations) return;

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                string text = textbox.Text?.Trim() ?? string.Empty;
                row.is_editing_observations = false;
                if (DataContext is AccountsReceivableViewModel vm)
                    await vm.save_observations_async(row, text);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                row.observations = row.saved_observations ?? string.Empty;
                row.is_editing_observations = false;
            }
        }

        private async void ObsTextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox textbox) return;
            if (textbox.DataContext is not accounts_receivable_row_dto row) return;
            if (!row.is_editing_observations) return;
            string text = textbox.Text?.Trim() ?? string.Empty;
            row.is_editing_observations = false;
            if (DataContext is AccountsReceivableViewModel vm)
                await vm.save_observations_async(row, text);
        }

        private async void DispatchDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not DatePicker picker) return;
            if (picker.DataContext is not accounts_receivable_row_dto row) return;
            if (row.dispatch_date == picker.SelectedDate) return;
            if (DataContext is AccountsReceivableViewModel vm)
                await vm.save_dispatch_date_async(row, picker.SelectedDate);
        }
    }
}