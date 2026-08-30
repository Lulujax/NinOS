using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CommissionHistoryWindow : Window
    {
        private readonly PaymentsViewModel _payments_vm;
        private readonly commission_row_dto _commission;
        private accounts_receivable_dto? _note;
        private decimal _total_note_usd;
        private decimal _total_paid_usd;

        public CommissionHistoryWindow(PaymentsViewModel payments_vm, commission_row_dto commission)
        {
            InitializeComponent();
            _payments_vm = payments_vm;
            _commission = commission;
            _total_note_usd = 0;

            NoteNumberText.Text = commission.note_number;
            CustomerText.Text = commission.customer_name;
            SellerText.Text = commission.seller_name;
            CommissionText.Text = commission.amount_usd.ToString("N2");

            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var note = await _payments_vm.search_note_async(_commission.note_number);
                if (note != null) _note = note;

                var payments = await _payments_vm.get_payments_by_note_async(_commission.id_delivery_note);
                Refresh(payments);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh(IEnumerable<payment_dto> payments)
        {
            var list = payments.ToList();
            HistoryGrid.ItemsSource = list;

            _total_paid_usd = list.Sum(p => p.amount_usd);
            _total_note_usd = _note?.total_amount_usd ?? 0;
            decimal balance = _total_note_usd - _total_paid_usd;
            decimal commission_pending = _total_note_usd > 0
                ? _commission.amount_usd * (balance / _total_note_usd)
                : _commission.amount_usd;

            TotalFacturadoText.Text = _total_note_usd.ToString("N2");
            TotalAbonadoText.Text = _total_paid_usd.ToString("N2");
            SaldoPendienteText.Text = balance.ToString("N2");
            SaldoPendienteText.Foreground = balance <= 0 ? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)) : new SolidColorBrush(Color.FromRgb(0xF5, 0x7C, 0x00));
            CommissionPendingText.Text = commission_pending.ToString("N2");

            BtnAddAbono.IsEnabled = balance > 0;
        }

        private async Task ReloadAsync()
        {
            await LoadAsync();
            _payments_vm.refresh_data();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is payment_dto payment)
            {
                try
                {
                    var window = new AddPaymentWindow(_payments_vm, "", payment);
                    window.Owner = Window.GetWindow(this);
                    window.PaymentRegistered += async (_, _) => await ReloadAsync();
                    window.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir edicion: {ex.Message}", "Error");
                }
            }
        }

        private async void AddAbonoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_note == null) return;
            var window = new AddPaymentWindow(_payments_vm, "", null);
            window.Owner = Window.GetWindow(this);
            window.PaymentRegistered += async (_, _) => await ReloadAsync();
            window.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}