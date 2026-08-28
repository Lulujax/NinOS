using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public class commission_combo_item
    {
        public int id_commission { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
        public decimal commission_percentage { get; set; }
        public decimal amount_usd { get; set; }
        public decimal sale_amount_usd => commission_percentage > 0 ? Math.Round(amount_usd / commission_percentage, 2) : amount_usd;
        public string Display => $"{note_number} - {customer_name} ({seller_name})";
    }

    public partial class AddCommissionPaymentWindow : Window
    {
        private readonly CommissionsViewModel _vm;
        private commission_combo_item? _selected_item;
        private List<commission_combo_item> _all_combo_items = new();

        public event EventHandler? CommissionPaid;

        public AddCommissionPaymentWindow(CommissionsViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            Loaded += async (_, _) => await LoadPendingCommissionsAsync();
        }

        private async System.Threading.Tasks.Task LoadPendingCommissionsAsync()
        {
            try
            {
                var all = await _vm.get_all_commissions_async();
                _all_combo_items = all
                    .Where(c => !c.is_paid)
                    .OrderBy(c => c.note_number)
                    .Select(c => new commission_combo_item
                    {
                        id_commission = c.id_commission,
                        note_number = c.note_number,
                        customer_name = c.customer_name,
                        seller_name = c.seller_name,
                        commission_percentage = c.commission_percentage,
                        amount_usd = c.amount_usd
                    })
                    .ToList();

                FilterNotes(string.Empty);
            }
            catch (Exception ex)
            {
                ShowError($"Error al cargar comisiones: {ex.Message}");
            }
        }

        private void OnNoteSearchChanged(object sender, TextChangedEventArgs e)
        {
            string query = NoteTextBox.Text?.Trim().ToLower() ?? string.Empty;
            FilterNotes(query);
            NotePopup.IsOpen = !string.IsNullOrEmpty(query) && NoteListBox.Items.Count > 0;
        }

        private void OnToggleDropdown(object sender, RoutedEventArgs e)
        {
            if (NotePopup.IsOpen)
            {
                NotePopup.IsOpen = false;
            }
            else
            {
                string query = NoteTextBox.Text?.Trim().ToLower() ?? string.Empty;
                FilterNotes(query);
                NotePopup.IsOpen = NoteListBox.ItemsSource != null;
            }
        }

        private void FilterNotes(string query)
        {
            List<commission_combo_item> filtered;
            if (string.IsNullOrEmpty(query))
                filtered = _all_combo_items;
            else
                filtered = _all_combo_items
                    .Where(n => (n.note_number?.ToLower().Contains(query) ?? false) ||
                                (n.customer_name?.ToLower().Contains(query) ?? false) ||
                                (n.seller_name?.ToLower().Contains(query) ?? false))
                    .ToList();

            NoteListBox.ItemsSource = filtered;
        }

        private void OnNoteListSelected(object sender, SelectionChangedEventArgs e)
        {
            if (NoteListBox.SelectedItem is commission_combo_item item)
            {
                _selected_item = item;
                NoteTextBox.Text = item.Display;
                NotePopup.IsOpen = false;

                ErrorText.Visibility = Visibility.Collapsed;
                NoteInfoBorder.Visibility = Visibility.Visible;
                NoteInfoText.Text =
                    $"NOTA: {item.note_number}  |  VENDEDORA: {item.seller_name}\n" +
                    $"VENTA: {item.sale_amount_usd:0.00}  |  COMISION ({item.commission_percentage * 100:0}%): {item.amount_usd:0.00}";
                BtnRegistrar.IsEnabled = true;
            }
        }

        private void OnRateChanged(object sender, TextChangedEventArgs e) => UpdateEquiv();

        private void OnAmountChanged(object sender, TextChangedEventArgs e) => UpdateEquiv();

        private void UpdateEquiv()
        {
            if (_selected_item == null) { EquivText.Text = ""; return; }

            decimal bs = ParseDecimal(AmountBox.Text);
            decimal rate = ParseDecimal(RateBox.Text);
            if (bs > 0 && rate > 0)
            {
                decimal usd = Math.Round(bs / rate, 2);
                EquivText.Text = $"{usd.ToString("0.00", CultureInfo.InvariantCulture)}  |  comision: {_selected_item.amount_usd.ToString("0.00", CultureInfo.InvariantCulture)}";
            }
            else
            {
                EquivText.Text = "";
            }
        }

        private void OnPreviewNumeric(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            TextBox? tb = sender as TextBox;
            string current = tb?.Text ?? string.Empty;
            foreach (char c in e.Text)
            {
                if (char.IsDigit(c)) continue;
                bool isSep = c == ',' || c == '.';
                bool hasSep = current.Contains(',') || current.Contains('.');
                if (isSep && !hasSep) continue;
                e.Handled = true;
                return;
            }
        }

        private decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            string norm = text.Replace(',', '.');
            if (decimal.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r)) return r;
            return 0;
        }

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            BtnRegistrar.IsEnabled = false;
            try
            {
                if (_selected_item == null) { ShowError("Seleccione una nota."); return; }

                decimal rate = ParseDecimal(RateBox.Text);
                if (rate <= 0) { ShowError("Ingrese tasa BS/USD valida."); return; }

                decimal amount_bs = ParseDecimal(AmountBox.Text);
                if (amount_bs <= 0) { ShowError("Ingrese el monto pagado."); return; }

string payment_type = (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pago Movil";
            string reference = ReferenceBox.Text?.Trim() ?? "";

            var result = MessageBox.Show(
                $"Nota: {_selected_item.note_number}\nComision: {_selected_item.amount_usd:0.00}\n" +
                $"Monto pagado: {amount_bs.ToString("0.00", CultureInfo.InvariantCulture)}  (tasa {rate:0.00})\n\n" +
                $"Desea liquidar esta comision?",
                    "Confirmar liquidacion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) { ShowError("Liquidacion cancelada."); return; }

                await _vm.pay_commission_async(_selected_item.id_commission, rate, payment_type, reference, amount_bs);

                CommissionPaid?.Invoke(this, EventArgs.Empty);
                Close();
            }
            finally
            {
                BtnRegistrar.IsEnabled = true;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}