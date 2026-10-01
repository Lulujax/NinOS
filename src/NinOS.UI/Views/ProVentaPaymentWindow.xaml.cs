using System;
using System.Globalization;
using System.Windows;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;
using NinOS.UI.Common;

namespace NinOS.UI.Views
{
    public partial class ProVentaPaymentWindow : Window
    {
        private readonly ProVentaViewModel _vm;
        private readonly pro_venta_relation_row _row;

        public ProVentaPaymentWindow(ProVentaViewModel vm, pro_venta_relation_row row)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _row = row ?? throw new ArgumentNullException(nameof(row));

            RelationInfoText.Text = $"{row.relation_label}\n" +
                $"MONTO: {row.amount:N2}  |  ABONADO: {row.paid_amount_usd:N2}  |  SALDO PENDIENTE: {row.balance_due_usd:N2}";

            PaymentDatePicker.SelectedDate = DateTime.Now;

            Loaded += (_, _) =>
            {
                InputRestrictions.attach_decimal(AmountBox);
            };
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
                    ShowError("Debes llenar los campos marcados con * (Fecha y Monto USD).");
                    return;
                }

                if (string.IsNullOrWhiteSpace(AmountBox.Text))
                {
                    ShowError("Debes llenar los campos marcados con * (Fecha y Monto USD).");
                    return;
                }

                if (InputRestrictions.has_letters(AmountBox.Text))
                {
                    ShowError("No se permiten letras en el monto USD. Ingrese solo números.");
                    return;
                }

                if (!InputRestrictions.is_valid_decimal(AmountBox.Text, out decimal amount_usd) || amount_usd <= 0)
                {
                    ShowError("El monto USD debe ser un número válido mayor a 0.");
                    return;
                }

                if (amount_usd > _row.balance_due_usd + 2.00m)
                {
                    ShowError("Se excedió del monto máximo.");
                    return;
                }

                MessageBoxResult result = AppDialog.Show(
                    $"Relacion: {_row.relation_label}\nMonto: {amount_usd:N2}\n\nDesea registrar este pago?",
                    "Confirmar pago",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    ShowError("Pago cancelado.");
                    return;
                }

                await _vm.register_relation_payment_async(_row.id_relacion, amount_usd, PaymentDatePicker.SelectedDate.Value, ObservationsBox.Text);

                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Error: {NinOS.UI.Common.ErrorText.Get(ex)}");
            }
            finally
            {
                BtnRegistrar.IsEnabled = true;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
    }
}