using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CommissionHistoryWindow : Window
    {
        private readonly CommissionsViewModel _commissions_vm;
        private readonly PaymentsViewModel? _payments_vm;
        private readonly commission_row_dto _commission;

        public CommissionHistoryWindow(CommissionsViewModel commissions_vm, PaymentsViewModel? payments_vm, commission_row_dto commission)
        {
            InitializeComponent();
            _commissions_vm = commissions_vm;
            _payments_vm = payments_vm;
            _commission = commission;

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
                decimal note_total = 0;

                if (_payments_vm != null)
                {
                    var note = await _payments_vm.search_note_async(_commission.note_number);
                    if (note != null)
                    {
                        note_total = note.total_amount_usd;
                    }
                }

                var commission_payments = await _commissions_vm.get_commission_payments_async(_commission.id_commission);

                Refresh(commission_payments.ToList(), note_total);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh(List<commission_payment_dto> payments, decimal note_total)
        {
            HistoryGrid.ItemsSource = payments;
            NoHistoryBox.Visibility = payments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal commission_paid = payments.Sum(p => p.amount_usd);
            decimal commission_pending = Math.Max(0, _commission.amount_usd - commission_paid);

            TotalFacturadoText.Text = note_total.ToString("N2");
            CommissionPendingText.Text = commission_pending.ToString("N2");
            CommissionText.Text = _commission.amount_usd.ToString("N2");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}