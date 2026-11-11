using System;
using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;
using NinOS.UI.Common;

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
                        var doc = NotePdfGenerator.build_document(printable);
                        var fileName = NotePdfGenerator.get_default_file_name(printable);
                        var preview = new PdfPreviewWindow(
                            doc,
                            $"Nota de Entrega {printable.note_number}",
                            fileName,
                            () => NoteExcelExporter.export(printable));
                        preview.Owner = Window.GetWindow(this);
                        preview.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        AppDialog.Show($"Error al cargar la nota: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                viewModel.on_request_confirmation_window = () =>
                {
                    if (viewModel.selected_note == null) return;

                    if (viewModel.selected_note.paid_amount_usd > 0)
                    {
                        AppDialog.Show(
                            $"No se puede anular la nota {viewModel.selected_note.note_number}.\nTiene abonos registrados ({viewModel.selected_note.paid_amount_usd:N2} USD). Elimine primero los pagos.",
                            "Anulacion bloqueada",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    string message = $"Esta seguro de anular la nota {viewModel.selected_note.note_number}?\nEl stock sera restituido.";
                    MessageBoxImage icon = MessageBoxImage.Question;

                    MessageBoxResult result = AppDialog.Show(
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

            string safe_month = string.IsNullOrWhiteSpace(month) || string.Equals(month, "Todos", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : month;

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

            var window = new AddPaymentWindow(PaymentsContext, safe_month, null, note);
            window.Owner = Window.GetWindow(this);
            window.PaymentRegistered += (_, _) =>
            {
                PaymentsContext.refresh_data();
                ar_vm.refresh_data();
            };
            window.ShowDialog();
        }

        private void on_reporte_click(object sender, RoutedEventArgs e)
        {
            popup_reporte.IsOpen = !popup_reporte.IsOpen;

            // Al abrir, las checklist quedan reflejando la pestaña de vendedor activa hasta
            // que el usuario desmarque lo que quiera dejar fuera.
            if (popup_reporte.IsOpen && DataContext is AccountsReceivableViewModel vm)
            {
                if (vm.is_report_zone_mode) rbtn_periodo_zona.IsChecked = true;
                else if (vm.is_report_year_mode) rbtn_periodo_anio.IsChecked = true;
                else rbtn_periodo_mes.IsChecked = true;

                vm.sync_report_scope_from_screen();
                ApplyReportPeriodVisibility();
            }
        }

        private void on_periodo_mes_checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountsReceivableViewModel vm)
            {
                vm.report_primary_mode = "Mes";
                vm.sync_report_scope_from_screen();
            }
            ApplyReportPeriodVisibility();
        }

        private void on_periodo_anio_checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountsReceivableViewModel vm)
            {
                vm.report_primary_mode = "Año";
                vm.sync_report_scope_from_screen();
            }
            ApplyReportPeriodVisibility();
        }

        private void on_periodo_zona_checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountsReceivableViewModel vm)
            {
                vm.report_primary_mode = "Por Zona";
                vm.sync_report_scope_from_screen();
            }
            ApplyReportPeriodVisibility();
        }

        private void on_zona_subperiodo_changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountsReceivableViewModel vm)
            {
                if (rbtn_zona_sub_todas.IsChecked == true) vm.report_zone_subperiod = "Todas";
                else if (rbtn_zona_sub_mes.IsChecked == true) vm.report_zone_subperiod = "Mes";
                else if (rbtn_zona_sub_anio.IsChecked == true) vm.report_zone_subperiod = "Año";
            }
            ApplyReportPeriodVisibility();
        }

        // El combo de mes, año o cascada de zona se muestra segun el radiobutton marcado en el popup.
        private void ApplyReportPeriodVisibility()
        {
            if (DataContext is not AccountsReceivableViewModel vm) return;

            bool isZona = vm.is_report_zone_mode;
            bool isAnio = vm.is_report_year_mode;
            bool isMes = vm.is_report_month_mode;

            panel_periodo_mes.Visibility = (!isZona && isMes) ? Visibility.Visible : Visibility.Collapsed;
            panel_periodo_anio.Visibility = (!isZona && isAnio) ? Visibility.Visible : Visibility.Collapsed;
            panel_periodo_zona.Visibility = isZona ? Visibility.Visible : Visibility.Collapsed;

            panel_agrupar_en.Visibility = isZona ? Visibility.Collapsed : Visibility.Visible;
            panel_filtro_vendedores.Visibility = isZona ? Visibility.Collapsed : Visibility.Visible;

            if (isZona)
            {
                string sub = vm.report_zone_subperiod;
                panel_zona_selectores_mes.Visibility = sub == "Mes" ? Visibility.Visible : Visibility.Collapsed;
                panel_zona_selectores_anio.Visibility = sub == "Año" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void on_preview_reporte_click(object sender, RoutedEventArgs e)
        {
            popup_reporte.IsOpen = false;
            if (DataContext is AccountsReceivableViewModel vm)
                vm.month_report_preview_command.Execute(null);
        }

        private void on_print_reporte_click(object sender, RoutedEventArgs e)
        {
            popup_reporte.IsOpen = false;
            if (DataContext is AccountsReceivableViewModel vm)
                vm.month_report_print_command.Execute(null);
        }

        private void on_descargar_reporte_click(object sender, RoutedEventArgs e)
        {
            popup_reporte.IsOpen = false;
            if (DataContext is AccountsReceivableViewModel vm)
                vm.month_report_command.Execute(null);
        }

        private void on_excel_reporte_click(object sender, RoutedEventArgs e)
        {
            popup_reporte.IsOpen = false;
            if (DataContext is AccountsReceivableViewModel vm)
                vm.month_report_excel_command.Execute(null);
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

            // Evita que el reciclado/virtualizacion del DataGrid (SelectedDate = null temporal)
            // borre la fecha de despacho ya guardada.
            if (picker.SelectedDate == null && row.dispatch_date != null) return;

            if (DataContext is AccountsReceivableViewModel vm)
                await vm.save_dispatch_date_async(row, picker.SelectedDate);
        }
    }
}