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
        private readonly payment_dto? _edit_payment;
        private accounts_receivable_dto? _selected_note;
        private bool _is_bs_mode = true;
        private bool _is_edit_mode;
        private bool _is_preloaded;
        private List<note_combo_item> _all_combo_items = new();

        public event EventHandler? PaymentRegistered;

        public AddPaymentWindow(PaymentsViewModel vm, string current_month, payment_dto? edit_payment = null, accounts_receivable_dto? preselected_note = null)
        {
            InitializeComponent();
            _vm = vm;
            _current_month = current_month;
            _edit_payment = edit_payment;
            _is_edit_mode = edit_payment != null;
            _is_preloaded = !_is_edit_mode && preselected_note != null;

            if (_is_edit_mode)
            {
                Title = "Editar Pago";
                BtnRegistrar.Content = "Guardar";
                SetupEditMode(edit_payment!);
            }
            else if (_is_preloaded)
            {
                SetupPreloadedNote(preselected_note!);
            }
            else
            {
                PaymentDatePicker.SelectedDate = DateTime.Now;
            }

            Loaded += async (_, _) =>
            {
                if (!_is_edit_mode && !_is_preloaded) await LoadNotesAsync();
            };
        }

        private void SetupPreloadedNote(accounts_receivable_dto note)
        {
            _selected_note = note;
            PaymentDatePicker.SelectedDate = DateTime.Now;

            NoteTextBox.Text = $"{note.note_number} - {note.customer_name}";
            NoteTextBox.IsReadOnly = true;
            BtnToggleDropdown.IsEnabled = false;

            NoteInfoBorder.Visibility = Visibility.Visible;
            NoteInfoText.Text = $"{note.note_number} - {note.customer_name}\nTOTAL: {note.total_amount_usd:N2}  |  ABONADO: {note.paid_amount_usd:N2}  |  SALDO PENDIENTE: {note.balance_due_usd:N2}";
            UpdateEquiv();
        }

        private void SetupEditMode(payment_dto p)
        {
            _selected_note = new accounts_receivable_dto
            {
                id_delivery_note = p.id_delivery_note,
                note_number = p.note_number,
                customer_name = p.customer_name,
                total_amount_usd = p.total_note_usd,
                paid_amount_usd = p.amount_usd,
                balance_due_usd = p.balance_due_usd,
                status = ""
            };

            NoteTextBox.Text = $"{p.note_number} - {p.customer_name}";
            NoteTextBox.IsReadOnly = true;
            BtnToggleDropdown.IsEnabled = false;

            NoteInfoBorder.Visibility = Visibility.Visible;
            NoteInfoText.Text = $"{p.note_number} - {p.customer_name}\nTOTAL: {p.total_note_usd:N2}  |  ABONADO: {p.amount_usd:N2}  |  SALDO PENDIENTE: {p.balance_due_usd:N2}";

            if (p.payment_date != default) PaymentDatePicker.SelectedDate = p.payment_date;

            ReferenceBox.Text = p.reference_number.Replace("REF-", "").Replace("EF-", "");
            BankBox.Text = p.bank_name;
            ObsBox.Text = p.notes;

            if (p.payment_type == "Efectivo")
            {
                RadioEfectivo.IsChecked = true;
                RadioBS.IsChecked = false;
                AmountBox.Text = p.amount_usd.ToString("0.##", CultureInfo.InvariantCulture);
            }
            else
            {
                RadioBS.IsChecked = true;
                RadioEfectivo.IsChecked = false;
                AmountBox.Text = p.amount_usd.ToString("0.##", CultureInfo.InvariantCulture);
                if (p.exchange_rate.HasValue) RateBox.Text = p.exchange_rate.Value.ToString("0.##", CultureInfo.InvariantCulture);
                if (p.amount_bs > 0) BsAmountBox.Text = p.amount_bs.ToString("0.##", CultureInfo.InvariantCulture);
            }
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
            BsAmountBox.Text = "";
            RateBox.Text = "";
            UpdateEquiv();
        }

        private void OnAmountChanged(object sender, TextChangedEventArgs e) => UpdateEquiv();

        private void UpdateEquiv()
        {
            if (_selected_note == null) { EquivText.Text = ""; return; }
            decimal balance = _selected_note.balance_due_usd;
            decimal usd = ParseDecimal(AmountBox.Text);
            if (usd > 0)
            {
                decimal remaining = usd - balance;
                EquivText.Text = $"saldo pendiente: {remaining:N2}";
            }
            else EquivText.Text = "";
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
                if (_selected_note == null) { ShowError("Tienes que llenar los campos obligatorios."); return; }
                if (PaymentDatePicker.SelectedDate == null) { ShowError("Tienes que llenar los campos obligatorios."); return; }

                DateTime payDate = PaymentDatePicker.SelectedDate.Value;

                if (!_is_edit_mode)
                {
                    var refreshed = await _vm.search_note_async(_selected_note.note_number);
                    if (refreshed == null) { ShowError("La nota ya no existe."); return; }
                    if (refreshed.status == "Pagada") { ShowError("Esta nota ya fue pagada."); return; }
                    if (refreshed.status == "Anulada") { ShowError("Esta nota fue anulada."); return; }
                    _selected_note = refreshed;
                }

                decimal amount_usd;
                decimal amount_bs = 0;
                decimal? exchange_rate;
                string payType;
                string reference;
                string bank;
                string obs = ObsBox.Text?.Trim() ?? "";

                if (_is_bs_mode)
                {
                    amount_usd = ParseDecimal(AmountBox.Text);
                    bool required_filled = amount_usd > 0 &&
                                           ParseDecimal(BsAmountBox.Text) > 0 &&
                                           !string.IsNullOrWhiteSpace(ReferenceBox.Text) &&
                                           !string.IsNullOrWhiteSpace(BankBox.Text);
                    if (!required_filled) { ShowError("Tienes que llenar los campos obligatorios."); return; }
                    decimal rate = ParseDecimal(RateBox.Text);
                    decimal bs = ParseDecimal(BsAmountBox.Text);
                    string refInput = ReferenceBox.Text?.Trim() ?? "";
                    bank = BankBox.Text?.Trim() ?? "";
                    exchange_rate = rate > 0 ? rate : null;
                    amount_bs = bs;
                    payType = "Bolivares";
                    reference = $"REF-{refInput}";
                }
                else
                {
                    decimal usd = ParseDecimal(AmountBox.Text);
                    if (usd <= 0) { ShowError("Tienes que llenar los campos obligatorios."); return; }
                    amount_usd = usd;
                    exchange_rate = null;
                    payType = "Efectivo";
                    reference = $"EF-{usd:0.##}";
                    bank = "";
                }

                if (_is_edit_mode && _edit_payment != null)
                {
                    string msg = $"Monto: {amount_usd:N2}\nDesea guardar los cambios de este abono?";
                    var edit_result = MessageBox.Show(
                        msg,
                        "Confirmar edicion",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (edit_result != MessageBoxResult.Yes) { ShowError("Edicion cancelada."); return; }

                    await _vm.update_payment_async(_edit_payment, amount_usd, exchange_rate, payType, reference, payDate, amount_bs, bank, obs);

                    PaymentRegistered?.Invoke(this, EventArgs.Empty);
                    Close();
                    return;
                }

                decimal nuevo_saldo = _selected_note.balance_due_usd - amount_usd;
                string nmsg = $"Monto: {amount_usd:N2}\nSaldo actual: {_selected_note.balance_due_usd:N2}\n";
                nmsg += nuevo_saldo <= 0
                    ? "El saldo quedara en 0. La nota se marcara como PAGADA."
                    : $"Nuevo saldo: {nuevo_saldo:N2}";

                var result = MessageBox.Show(
                    nmsg + "\n\nDesea registrar este pago?",
                    "Confirmar pago",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    ShowError("Pago cancelado.");
                    return;
                }

                await _vm.confirm_payment_async(_selected_note.id_delivery_note, amount_usd, exchange_rate, payType, reference, payDate, amount_bs, bank, obs);

                PaymentRegistered?.Invoke(this, EventArgs.Empty);
                Close();

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
                BsAmountBox.Text = "";
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
