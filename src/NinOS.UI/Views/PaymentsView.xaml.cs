using System;
using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PaymentsView : UserControl
    {
        public PaymentsView()
        {
            InitializeComponent();
            DataContextChanged += PaymentsView_DataContextChanged;
        }

        private void PaymentsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is PaymentsViewModel vm)
            {
                vm.on_request_add_payment_window = () =>
                {
                    var window = new AddPaymentWindow(vm, vm.selected_month);
                    window.Owner = Window.GetWindow(this);
                    window.PaymentRegistered += (_, _) => vm.refresh_data();
                    window.ShowDialog();
                };

                txt_search.TextChanged += Txt_search_TextChanged;
            }
        }

        private void Txt_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_clear_search.Visibility = txt_search.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btn_clear_search_Click(object sender, RoutedEventArgs e)
        {
            txt_search.Clear();
        }

        private async void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is payment_row_dto row)
            {
                try
                {
                    if (DataContext is not PaymentsViewModel vm) return;

                    var payments = await vm.get_payments_by_note_async(row.id_delivery_note);
                    var window = new PaymentNoteHistoryWindow(payments);
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar historial: {ex.Message}", "Error");
                }
            }
        }
    }
}
