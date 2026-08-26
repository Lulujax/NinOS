using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public class note_combo_item
    {
        public int id_delivery_note { get; set; }
        public string note_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;
        public decimal balance_due_usd { get; set; }
        public decimal total_amount_usd { get; set; }
        public decimal paid_amount_usd { get; set; }
        public string status { get; set; } = string.Empty;
        public string Display => $"{note_number} - {customer_name}";
    }

    public partial class AddPaymentWindow : Window
    {
        private readonly PaymentsViewModel _vm;
        private readonly string _current_month;
        private accounts_receivable_dto? _selected_note;
        private bool _is_bs_mode = true;
        private List<note_combo_item> _all_combo_items = new();

        public event EventHandler? PaymentRegistered;

        public AddPaymentWindow(PaymentsViewModel vm, string current_month)
        {
            InitializeComponent();
            _vm = vm;
            _current_month = current_month;
            PaymentDatePicker.SelectedDate = DateTime.Now;
            Loaded += async (_, _) => await LoadNotesAsync();
        }

        private async System.Threading.Tasks.Task LoadNotesAsync()
        {
            try
            {
                var all_months = await _vm.get_all_months_async();
                var all_notes = new List<accounts_receivable_dto>();

                if (!string.IsNullOrEmpty(_current_month))
                {
                    var notes = await _vm.get_notes_by_month_async(_current_month);
                    all_notes.AddRange(notes);
                }
                else
                {
                    foreach (var month in all_months)
                    {
                        var notes = await _vm.get_notes_by_month_async(month);
                        all_notes.AddRange(notes);
                    }
                }

                _all_combo_items = all_notes
                    .Where(n => n.status != "Anulada" && n.status != "Pagada")
                    .OrderBy(n => n.note_number)
                    .Select(n => new note_combo_item
                    {
                        id_delivery_note = n.id_delivery_note,
                        note_number = n.note_number,
                        customer_name = n.customer_name,
                        balance_due_usd = n.balance_due_usd,
                        total_amount_usd = n.total_amount_usd,
                        paid_amount_usd = n.paid_amount_usd,
                        status = n.status
                    }).ToList();

                FilterNotes(string.Empty);
            }
            catch (Exception ex)
            {
                ShowError($"Error al cargar notas: {ex.Message}");
            }
        }

        private void OnNoteSearchChanged(object sender, TextChangedEventArgs e)
        {
            string query = NoteTextBox.Text?.Trim().ToLower() ?? string.Empty;
            FilterNotes(query);
        }

        private void FilterNotes(string query)
        {
            List<note_combo_item> filtered;
            if (string.IsNullOrEmpty(query))
            {
                filtered = _all_combo_items;
            }
            else
            {
                filtered = _all_combo_items
                    .Where(n => (n.note_number?.ToLower().Contains(query) ?? false) ||
                                (n.customer_name?.ToLower().Contains(query) ?? false))
                    .ToList();
            }

            NoteListBox.ItemsSource = filtered;
            NotePopup.IsOpen = filtered.Count > 0 && NoteTextBox.IsFocused;
        }

        private void OnNoteListSelected(object sender, SelectionChangedEventArgs e)
        {
            if (NoteListBox.SelectedItem is note_combo_item item)
            {
                _selected_note = new accounts_receivable_dto
                {
                    id_delivery_note = item.id_delivery_note,
                    note_number = item.note_number,
                    customer_name = item.customer_name,
                    total_amount_usd = item.total_amount_usd,
                    paid_amount_usd = item.paid_amount_usd,
                    balance_due_usd = item.balance_due_usd,
                    status = item.status
                };

                NoteTextBox.Text = item.Display;
                NotePopup.IsOpen = false;

                ErrorText.Visibility = Visibility.Collapsed;
                NoteInfoBorder.Visibility = Visibility.Visible;
                NoteInfoText.Text = $"{item.Display}\nTOTAL: {item.total_amount_usd:N2}  |  ABONADO: {item.paid_amount_usd:N2}  |  SALDO PENDIENTE: {item.balance_due_usd:N2}";
                UpdateEquiv();
            }
        }

        private void OnPaymentTypeChanged(object sender, RoutedEventArgs e)
        {
            if (RadioBS == null || BsPanel == null || RefPanel == null || ObsBox == null) return;
            _is_bs_mode = RadioBS.IsChecked == true;
            BsPanel.Visibility = _is_bs_mode ? Visibility.Visible : Visibility.Collapsed;
            RefPanel.Visibility = _is_bs_mode ? Visibility.Visible : Visibility.Collapsed;
            ObsBox.Text = "";
            ReferenceBox.Text = "";
            BankBox.Text = "";
            UpdateEquiv();
        }

        private void OnAmountChanged(object sender, TextChangedEventArgs e) => UpdateEquiv();

        private void OnPreviewNumeric(object sender, TextCompositionEventArgs e)
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

        private void UpdateEquiv()
        {
            if (_selected_note == null) { EquivText.Text = ""; return; }
            decimal balance = _selected_note.balance_due_usd;

            if (_is_bs_mode)
            {
                decimal bs = ParseDecimal(AmountBox.Text);
                decimal rate = ParseDecimal(RateBox.Text);
                if (bs > 0 && rate > 0)
                {
                    decimal usd = bs / rate;
                    decimal remaining = usd - balance;
                    EquivText.Text = $"{usd:N2}  |  saldo pendiente: {remaining:N2}";
                }
                else EquivText.Text = "";
            }
            else
            {
                decimal usd = ParseDecimal(AmountBox.Text);
                if (usd > 0)
                {
                    decimal remaining = usd - balance;
                    EquivText.Text = $"saldo pendiente: {remaining:N2}";
                }
                else EquivText.Text = "";
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
                if (_selected_note == null) { ShowError("Seleccione una nota."); return; }
                if (PaymentDatePicker.SelectedDate == null) { ShowError("Seleccione la fecha."); return; }

                DateTime payDate = PaymentDatePicker.SelectedDate.Value;

                var refreshed = await _vm.search_note_async(_selected_note.note_number);
                if (refreshed == null) { ShowError("La nota ya no existe."); return; }
                if (refreshed.status == "Pagada") { ShowError("Esta nota ya fue pagada."); return; }
                if (refreshed.status == "Anulada") { ShowError("Esta nota fue anulada."); return; }
                _selected_note = refreshed;

                decimal amount_usd;
                decimal? exchange_rate;
                string payType;
                string reference;
                string bank;
                string obs = ObsBox.Text?.Trim() ?? "";

                if (_is_bs_mode)
                {
                    decimal bs = ParseDecimal(AmountBox.Text);
                    if (bs <= 0) { ShowError("Ingrese monto BS valido."); return; }
                    decimal rate = ParseDecimal(RateBox.Text);
                    if (rate <= 0) { ShowError("Ingrese tasa valida."); return; }
                    string refInput = ReferenceBox.Text?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(refInput)) { ShowError("Ingrese referencia."); return; }
                    if (!Regex.IsMatch(refInput, @"^\d+$")) { ShowError("La referencia BS debe ser solo numeros."); return; }
                    amount_usd = bs / rate;
                    exchange_rate = rate;
                    payType = "Bolivares";
                    reference = $"REF-{refInput}";
                    bank = BankBox.Text?.Trim() ?? "";
                }
                else
                {
                    decimal usd = ParseDecimal(AmountBox.Text);
                    if (usd <= 0) { ShowError("Ingrese monto valido."); return; }
                    amount_usd = usd;
                    exchange_rate = null;
                    payType = "Efectivo";
                    reference = $"EF-{usd:0.##}";
                    bank = "";
                }

                decimal nuevo_saldo = _selected_note.balance_due_usd - amount_usd;
                string msg = $"Monto: {amount_usd:N2}\nSaldo actual: {_selected_note.balance_due_usd:N2}\n";
                msg += nuevo_saldo <= 0
                    ? "El saldo quedara en 0. La nota se marcara como PAGADA."
                    : $"Nuevo saldo: {nuevo_saldo:N2}";

                var result = MessageBox.Show(
                    msg + "\n\nDesea registrar este pago?",
                    "Confirmar pago",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    ShowError("Pago cancelado.");
                    return;
                }

                await _vm.confirm_payment_async(_selected_note.id_delivery_note, amount_usd, exchange_rate, payType, reference, payDate, bank, obs);

                PaymentRegistered?.Invoke(this, EventArgs.Empty);
                ShowError("Pago registrado.");
                ErrorText.Foreground = System.Windows.Media.Brushes.Green;

                if (_selected_note != null)
                {
                    var updated = await _vm.search_note_async(_selected_note.note_number);
                    if (updated != null)
                    {
                        _selected_note = updated;
                        NoteInfoText.Text = $"{_selected_note.note_number} - {_selected_note.customer_name}\nTOTAL: {_selected_note.total_amount_usd:N2}  |  ABONADO: {_selected_note.paid_amount_usd:N2}  |  SALDO PENDIENTE: {_selected_note.balance_due_usd:N2}";
                    }
                }

                AmountBox.Text = "";
                RateBox.Text = "";
                ReferenceBox.Text = "";
                BankBox.Text = "";
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
