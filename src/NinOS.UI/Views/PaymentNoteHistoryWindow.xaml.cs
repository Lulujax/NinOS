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
    public partial class PaymentNoteHistoryWindow : Window
    {
        private readonly PaymentsViewModel _vm;
        private accounts_receivable_dto? _note;
        private decimal _total_note_usd;
        private decimal _total_paid_usd;

        public PaymentNoteHistoryWindow(PaymentsViewModel vm, accounts_receivable_dto note, IEnumerable<payment_dto> payments)
        {
            InitializeComponent();
            _vm = vm;
            _note = note;
            _total_note_usd = note.total_amount_usd;
            _total_paid_usd = note.paid_amount_usd;

            NoteNumberText.Text = note.note_number;
            CustomerText.Text = note.customer_name;
            SellerText.Text = note.seller_name;

            RefreshPayments(payments);
        }

        private void RefreshPayments(IEnumerable<payment_dto> payments)
        {
            HistoryGrid.ItemsSource = payments.ToList();

            var list = payments.ToList();
            _total_paid_usd = list.Sum(p => p.amount_usd);
            decimal balance = _total_note_usd - _total_paid_usd;

            TotalFacturadoText.Text = _total_note_usd.ToString("N2");
            TotalAbonadoText.Text = _total_paid_usd.ToString("N2");
            SaldoPendienteText.Text = balance.ToString("N2");
            SaldoPendienteText.Foreground = balance <= 0 ? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)) : new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00));

            BtnAddAbono.IsEnabled = balance > 0;

            if (_note != null)
            {
                _note.paid_amount_usd = _total_paid_usd;
                _note.balance_due_usd = balance;
            }
        }

        private async Task ReloadAsync()
        {
            if (_note == null) return;
            var payments = await _vm.get_payments_by_note_async(_note.id_delivery_note);
            RefreshPayments(payments);
            _vm.refresh_data();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is payment_dto payment)
            {
                try
                {
                    var window = new AddPaymentWindow(_vm, "", payment);
                    window.Owner = Window.GetWindow(this);
                    window.PaymentRegistered += async (_, _) => await ReloadAsync();
                    window.ShowDialog();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error al abrir edicion: {ex.Message}", "Error");
                }
            }
        }

        private async void AddAbonoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_note == null) return;
            var window = new AddPaymentWindow(_vm, "", null, _note);
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