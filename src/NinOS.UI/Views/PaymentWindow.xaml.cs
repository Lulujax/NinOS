using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PaymentWindow : Window
    {
        private readonly PaymentsViewModel _view_model;
        private readonly accounts_receivable_dto _note;
        private bool _is_bs_mode = true;

        public PaymentWindow(PaymentsViewModel view_model, accounts_receivable_dto note)
        {
            InitializeComponent();
            _view_model = view_model;
            _note = note;

            Title = $"Registrar Pago - {note.note_number}";
            NoteNumberText.Text = $"{note.note_number} - {note.customer_name}";
            BalanceText.Text = $"{note.balance_due_usd:N2}";
            PaymentDatePicker.SelectedDate = DateTime.Now;
        }

        private void OnPreviewNumericInput(object sender, TextCompositionEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            string currentText = textBox?.Text ?? string.Empty;

            foreach (char c in e.Text)
            {
                if (char.IsDigit(c)) continue;

                bool isDecimalSeparator = c == ',' || c == '.';
                bool alreadyHasDecimal = currentText.Contains(',') || currentText.Contains('.');

                if (isDecimalSeparator && !alreadyHasDecimal) continue;

                e.Handled = true;
                return;
            }
        }

        private decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            string normalized = text.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;
            return 0;
        }

        private void OnPaymentTypeChanged(object sender, RoutedEventArgs e)
        {
            if (RadioBS == null || RadioEfectivo == null || BSPanel == null || EfectivoPanel == null || ReferencePanel == null) return;

            _is_bs_mode = RadioBS.IsChecked == true;
            BSPanel.Visibility = _is_bs_mode ? Visibility.Visible : Visibility.Collapsed;
            EfectivoPanel.Visibility = _is_bs_mode ? Visibility.Collapsed : Visibility.Visible;
            ReferencePanel.Visibility = _is_bs_mode ? Visibility.Visible : Visibility.Collapsed;

            UpdateFeedback();
        }

        private void OnBsChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateFeedback();
        }

        private void OnEfectivoChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateFeedback();
        }

        private void UpdateFeedback()
        {
            if (EquivUsdText == null || EquivEfectivoText == null) return;

            decimal balance = _note.balance_due_usd;

            if (_is_bs_mode)
            {
                EquivEfectivoText.Text = "-";
                decimal bs = ParseDecimal(AmountBsBox.Text);
                decimal rate = ParseDecimal(ExchangeRateBox.Text);

                if (bs > 0 && rate > 0)
                {
                    decimal usd = bs / rate;
                    decimal remaining = usd - balance;
                    EquivUsdText.Text = $"{usd:N2}  |  saldo: {remaining:N2}";
                }
                else
                {
                    EquivUsdText.Text = "-";
                }
            }
            else
            {
                EquivUsdText.Text = "-";
                decimal usd = ParseDecimal(AmountUsdBox.Text);

                if (usd > 0)
                {
                    decimal remaining = usd - balance;
                    EquivEfectivoText.Text = $"{usd:N2}  |  saldo: {remaining:N2}";
                }
                else
                {
                    EquivEfectivoText.Text = "-";
                }
            }
        }

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            if (PaymentDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Seleccione la fecha del pago.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime payment_date = PaymentDatePicker.SelectedDate.Value;
            decimal amount_usd;
            decimal? exchange_rate;
            string payment_type;
            string reference_number;

            if (_is_bs_mode)
            {
                decimal amount_bs = ParseDecimal(AmountBsBox.Text);
                if (amount_bs <= 0)
                {
                    MessageBox.Show("Ingrese un monto en BS valido.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                decimal rate = ParseDecimal(ExchangeRateBox.Text);
                if (rate <= 0)
                {
                    MessageBox.Show("Ingrese una tasa de cambio valida.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(ReferenceBox.Text))
                {
                    MessageBox.Show("Ingrese una referencia.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                amount_usd = amount_bs / rate;
                exchange_rate = rate;
                payment_type = "Bolivares";
                reference_number = ReferenceBox.Text.Trim();
            }
            else
            {
                decimal usd = ParseDecimal(AmountUsdBox.Text);
                if (usd <= 0)
                {
                    MessageBox.Show("Ingrese un monto en efectivo valido.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                amount_usd = usd;
                exchange_rate = null;
                payment_type = "Efectivo";
                reference_number = string.IsNullOrWhiteSpace(ReferenceBox.Text) ? "Efectivo" : ReferenceBox.Text.Trim();
            }

            await _view_model.confirm_payment_async(_note, amount_usd, exchange_rate, payment_type, reference_number, payment_date);
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
