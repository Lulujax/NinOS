using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class EditCommissionPaymentWindow : Window
    {
        private readonly CommissionsViewModel _vm;
        private readonly commission_payment_dto _payment;

        public event EventHandler? PaymentUpdated;

        public EditCommissionPaymentWindow(CommissionsViewModel vm, commission_payment_dto payment, string note_number, string seller_name)
        {
            InitializeComponent();
            _vm = vm;
            _payment = payment;

            InfoText.Text = $"NOTA: {note_number}    |    VENDEDORA: {seller_name}";

            AmountBox.Text = payment.amount_usd.ToString("0.##", CultureInfo.InvariantCulture);
            RateBox.Text = payment.exchange_rate.ToString("0.##", CultureInfo.InvariantCulture);
            BsAmountBox.Text = payment.amount_bs.ToString("0.##", CultureInfo.InvariantCulture);
            ReferenceBox.Text = payment.reference_number;
            BankBox.Text = payment.bank_name;
            ObsBox.Text = payment.notes;
            if (payment.payment_date != default) PaymentDatePicker.SelectedDate = payment.payment_date;

            SelectType(payment.payment_type);
        }

        private void SelectType(string type)
        {
            foreach (var item in TypeCombo.Items)
            {
                if (item is ComboBoxItem combo && string.Equals(combo.Content?.ToString(), type, StringComparison.OrdinalIgnoreCase))
                {
                    TypeCombo.SelectedItem = combo;
                    return;
                }
            }
            TypeCombo.SelectedIndex = 0;
        }

        private decimal ParseDecimal(string? text)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
            {
                return value;
            }
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal current))
            {
                return current;
            }
            return 0;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            BtnSave.IsEnabled = false;
            try
            {
                ErrorText.Visibility = Visibility.Collapsed;

                decimal amount_usd = ParseDecimal(AmountBox.Text);
                if (amount_usd <= 0)
                {
                    ShowError("El monto USD debe ser mayor a cero.");
                    return;
                }

                decimal rate = ParseDecimal(RateBox.Text);
                if (rate < 0)
                {
                    ShowError("La tasa BS/USD no puede ser negativa.");
                    return;
                }

                decimal amount_bs = ParseDecimal(BsAmountBox.Text);
                if (amount_bs < 0)
                {
                    ShowError("El monto en Bs no puede ser negativo.");
                    return;
                }

                string reference = ReferenceBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(reference))
                {
                    ShowError("La referencia es obligatoria.");
                    return;
                }

                string payment_type = (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pago Movil";
                DateTime payment_date = PaymentDatePicker.SelectedDate ?? _payment.payment_date;
                string bank = BankBox.Text?.Trim() ?? string.Empty;
                string observations = ObsBox.Text?.Trim() ?? string.Empty;

                await _vm.update_commission_payment_async(
                    _payment.id_commission_payment, amount_usd, rate, payment_type, reference, amount_bs, payment_date, bank, observations);

                PaymentUpdated?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"No se pudo guardar el pago: {ex.Message}");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }
    }
}