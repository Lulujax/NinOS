using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AddCommissionPaymentWindow : Window
    {
        private readonly CommissionsViewModel _vm;
        private readonly List<commission_row_dto> _available;
        private readonly ObservableCollection<commission_row_dto> _to_pay = new();
        private bool _loading_list;

        public event EventHandler? CommissionPaid;

        public AddCommissionPaymentWindow(CommissionsViewModel vm, List<commission_row_dto> available)
        {
            InitializeComponent();
            _vm = vm;
            _available = available ?? new List<commission_row_dto>();

            Loaded += (_, _) => Setup();
        }

        private void Setup()
        {
            NotesGrid.ItemsSource = _to_pay;
            PaymentDatePicker.SelectedDate = DateTime.Today;
            RefreshTotals();
        }

        private void RefreshTotals()
        {
            decimal total_pending = _to_pay.Sum(c => c.remaining_amount_usd);
            string seller = _to_pay.FirstOrDefault()?.seller_name ?? string.Empty;
            SummaryText.Text = $"VENDEDORA: {seller}    |    {_to_pay.Count} NOTA(S)    |    TOTAL PENDIENTE: {total_pending:0.00}";
            AmountBox.Text = total_pending.ToString("0.##", CultureInfo.InvariantCulture);
            BtnRegistrar.IsEnabled = _to_pay.Count > 0;
        }

        private List<commission_row_dto> filter_matches(string query)
        {
            IEnumerable<commission_row_dto> rows = _available;
            if (_to_pay.Count > 0)
            {
                int seller_id = _to_pay[0].id_seller;
                rows = rows.Where(c => c.id_seller == seller_id);
            }
            if (!string.IsNullOrWhiteSpace(query))
            {
                string q = query.Trim().ToLowerInvariant();
                rows = rows.Where(c =>
                    (c.note_number != null && c.note_number.ToLowerInvariant().Contains(q)) ||
                    (c.customer_name != null && c.customer_name.ToLowerInvariant().Contains(q)));
            }
            return rows.ToList();
        }

        private void OnNoteSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading_list) return;
            var matches = filter_matches(NoteTextBox.Text);
            _loading_list = true;
            NoteListBox.ItemsSource = matches;
            _loading_list = false;
            NotePopup.IsOpen = matches.Count > 0;
        }

        private void OnToggleDropdown(object sender, RoutedEventArgs e)
        {
            if (NotePopup.IsOpen)
            {
                NotePopup.IsOpen = false;
                return;
            }
            var matches = filter_matches(NoteTextBox.Text);
            _loading_list = true;
            NoteListBox.ItemsSource = matches;
            _loading_list = false;
            NotePopup.IsOpen = matches.Count > 0;
        }

        private void OnNoteListSelected(object sender, SelectionChangedEventArgs e)
        {
            if (_loading_list) return;
            if (!(NoteListBox.SelectedItem is commission_row_dto row)) return;

            ClearError();
            if (_to_pay.Count > 0 && row.id_seller != _to_pay[0].id_seller)
            {
                ShowError($"La nota {row.note_number} es de otra vendedora ({row.seller_name}). Solo puede añadir notas de {_to_pay[0].seller_name}.");
                NotePopup.IsOpen = false;
                _loading_list = true;
                NoteListBox.SelectedItem = null;
                _loading_list = false;
                return;
            }

            if (!_to_pay.Any(c => c.id_commission == row.id_commission))
            {
                _to_pay.Add(row);
            }

            _loading_list = true;
            NoteTextBox.Text = "";
            NoteListBox.ItemsSource = null;
            _loading_list = false;
            NotePopup.IsOpen = false;
            RefreshTotals();
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is commission_row_dto row)
            {
                _to_pay.Remove(row);
                RefreshTotals();
            }
        }

        private void OnPreviewNumeric(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text);
        }

        private bool IsNumeric(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != '.') return false;
            }
            return true;
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

        private void ClearError()
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            BtnRegistrar.IsEnabled = false;
            try
            {
                ClearError();

                if (_to_pay.Count == 0)
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }

                int sellers = _to_pay.Select(c => c.id_seller).Distinct().Count();
                if (sellers > 1)
                {
                    ShowError("Las notas añadidas son de vendedoras distintas. Añada solo notas de una misma vendedora.");
                    return;
                }

                decimal rate = ParseDecimal(RateBox.Text);
                if (rate <= 0)
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }
                if (rate > 10_000_000m)
                {
                    ShowError("La tasa ingresada es demasiado grande. Revise el campo Tasa.");
                    return;
                }

                decimal amount_usd = ParseDecimal(AmountBox.Text);
                if (amount_usd <= 0)
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }
                if (amount_usd > 10_000_000m)
                {
                    ShowError("El monto USD ingresado es demasiado grande.");
                    return;
                }

                decimal total_pending = _to_pay.Sum(c => c.remaining_amount_usd);
                if (total_pending > 10_000_000m)
                {
                    ShowError("El total pendiente es demasiado grande.");
                    return;
                }

                // No se permite pagar por partes: el monto debe ser el total de las comisiones seleccionadas.
                AmountBox.Text = total_pending.ToString("0.##", CultureInfo.InvariantCulture);
                amount_usd = total_pending;
                if (total_pending <= 0)
                {
                    ShowError("No hay saldo pendiente en las comisiones seleccionadas.");
                    return;
                }

                decimal amount_bs = ParseDecimal(BsAmountBox.Text);
                if (amount_bs <= 0)
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }

                string reference = ReferenceBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(reference))
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }

                string payment_type = (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pago Movil";

                DateTime? selected_date = PaymentDatePicker.SelectedDate;
                DateTime payment_date = selected_date ?? DateTime.Today;

                string bank_name = BankBox.Text?.Trim() ?? string.Empty;
                string observations = ObsBox.Text?.Trim() ?? string.Empty;

                string seller = _to_pay.FirstOrDefault()?.seller_name ?? string.Empty;

                var ordered = _to_pay.OrderBy(c => c.remaining_amount_usd).ToList();
                decimal remain = amount_usd;
                int fully = 0;
                foreach (var c in ordered)
                {
                    if (remain <= 0) break;
                    decimal r = c.remaining_amount_usd;
                    if (remain >= r) { remain -= r; fully++; }
                    else { remain = 0; }
                }
                decimal pending_after = total_pending - amount_usd;

                string plan = $"FECHA: {payment_date:dd/MM/yyyy}\n" +
                    $"TASA: {rate:0.##}\nMONTO BS: {amount_bs:0.##}\nTIPO: {payment_type}\nREFERENCIA: {reference}\n" +
                    (string.IsNullOrWhiteSpace(bank_name) ? "" : $"BANCO: {bank_name}\n") +
                    (string.IsNullOrWhiteSpace(observations) ? "" : $"OBSERVACION: {observations}\n") +
                    $"\nNotas que quedan pagas: {fully}\n";
                if (pending_after > 0)
                    plan += $"Queda pendiente por pagar: {pending_after:0.##}\n";
                else
                    plan += "No quedan saldos pendientes.\n";

                var result = MessageBox.Show(
                    $"VENDEDORA: {seller}\nNOTAS A PAGAR: {_to_pay.Count}\nTOTAL PENDIENTE: {total_pending:0.##}\nA PAGAR: {amount_usd:0.##}\n\n" +
                    plan +
                    "\n¿Confirmar liquidacion de comisiones?",
                    "Confirmar Liquidacion", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                int[] ids = _to_pay.Select(c => c.id_commission).ToArray();
                bool paid = await _vm.pay_commissions_async(ids, amount_usd, rate, payment_type, reference, amount_bs, payment_date, bank_name, observations);

                if (paid)
                {
                    var pdf_result = MessageBox.Show(
                        "La comision fue liquidada.\n\n¿Desea generar el PDF del comprobante de pago de comision?",
                        "Generar PDF",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (pdf_result == MessageBoxResult.Yes)
                    {
                        var receipt = await _vm.get_commission_receipt_async(ids, reference);
                        if (receipt == null || receipt.rows.Count == 0)
                        {
                            MessageBox.Show("No se encontro informacion del comprobante.", "Comprobante",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            CommissionPdfGenerator.generate(receipt);
                        }
                    }
                }

                CommissionPaid?.Invoke(this, EventArgs.Empty);
                Close();
            }
            finally
            {
                BtnRegistrar.IsEnabled = true;
            }
        }
    }
}