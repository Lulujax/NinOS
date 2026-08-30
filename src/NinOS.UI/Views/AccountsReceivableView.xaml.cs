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

                    MessageBoxResult result = MessageBox.Show(
                        $"Esta seguro de anular la nota {viewModel.selected_note.note_number}?\nEl stock sera restituido.",
                        "Confirmar Anulacion",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _ = viewModel.confirm_annulation_async();
                    }
                };

                viewModel.on_request_add_payment_for_month = (month) => OpenPaymentWindow(viewModel, month);

                viewModel.on_request_add_payment_for_note = (note) =>
                {
                    string month = note.creation_date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-VE"));
                    OpenPaymentWindow(viewModel, month);
                };
            }
        }

        private void OpenPaymentWindow(AccountsReceivableViewModel ar_vm, string month)
        {
            if (PaymentsContext == null) return;

            var window = new AddPaymentWindow(PaymentsContext, month);
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
    }
}