using System.Globalization;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PaymentWindow : Window
    {
        private readonly AccountsReceivableViewModel _view_model;
        private readonly accounts_receivable_dto _note;

        public PaymentWindow(AccountsReceivableViewModel view_model, accounts_receivable_dto note)
        {
            InitializeComponent();
            _view_model = view_model;
            _note = note;

            Title = $"Registrar Abono - {note.note_number}";
            NoteNumberText.Text = note.note_number;
            BalanceText.Text = $"{note.balance_due_usd:N2} USD";
            AmountUsdBox.Text = note.balance_due_usd.ToString("F2", CultureInfo.InvariantCulture);
        }

        private void OnPreviewNumericInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OnExchangeRateChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateAmountBs();
        }

        private void UpdateAmountBs()
        {
            if (decimal.TryParse(AmountUsdBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount_usd) &&
                decimal.TryParse(ExchangeRateBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchange_rate) &&
                exchange_rate > 0)
            {
                AmountBsText.Text = $"{amount_usd * exchange_rate:N2} Bs";
            }
            else
            {
                AmountBsText.Text = "-";
            }
        }

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AmountUsdBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount_usd) || amount_usd <= 0)
            {
                MessageBox.Show("Ingrese un monto valido mayor a cero.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ExchangeRateBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchange_rate) || exchange_rate <= 0)
            {
                MessageBox.Show("Ingrese una tasa de cambio valida.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (amount_usd > _note.balance_due_usd)
            {
                MessageBox.Show($"El monto no puede superar el saldo pendiente ({_note.balance_due_usd:N2} USD).", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _view_model.confirm_payment_async(amount_usd, exchange_rate);
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}