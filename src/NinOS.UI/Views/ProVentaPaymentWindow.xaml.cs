using System;
using System.Globalization;
using System.Windows;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class ProVentaPaymentWindow : Window
    {
        private readonly ProVentaViewModel _vm;
        private readonly pro_venta_pending_row _row;

        public ProVentaPaymentWindow(ProVentaViewModel vm, pro_venta_pending_row row)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _row = row ?? throw new ArgumentNullException(nameof(row));

            RelationInfoText.Text = $"{row.note_number} - {row.customer_name}\n" +
                $"MONTO NOTA: {row.amount:N2}  |  ABONADO: {row.paid_amount_usd:N2}  |  SALDO PENDIENTE: {row.balance_due_usd:N2}";

            PaymentDatePicker.SelectedDate = DateTime.Now;
        }

        private decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            string norm = text.Replace(',', '.');
            if (decimal.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)) return result;
            return 0;
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            BtnRegistrar.IsEnabled = false;
            try
            {
                ErrorText.Visibility = Visibility.Collapsed;

                if (PaymentDatePicker.SelectedDate == null)
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }

                decimal amount_usd = ParseDecimal(AmountBox.Text);
                if (amount_usd <= 0)
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }

                if (amount_usd > _row.balance_due_usd)
                {
                    ShowError("El monto no puede ser mayor al saldo pendiente.");
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Relacion: {_row.note_number}\nMonto: {amount_usd:N2}\n\nDesea registrar este pago?",
                    "Confirmar pago",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    ShowError("Pago cancelado.");
                    return;
                }

                await _vm.register_payment_async(_row.id_delivery_note, _row.note_number, amount_usd, PaymentDatePicker.SelectedDate.Value);

                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
                BtnRegistrar.IsEnabled = true;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
    }
}