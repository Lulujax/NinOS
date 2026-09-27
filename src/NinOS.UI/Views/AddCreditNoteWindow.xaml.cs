using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public class credit_note_edit_row : ViewModelBase
    {
        public int? id_product { get; set; }
        public int? id_promotion { get; set; }
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public decimal unit_price_usd { get; set; }
        public int delivered_quantity { get; set; }
        public int already_returned_quantity { get; set; }
        public int remaining_quantity { get; set; }

        private int _return_quantity;
        public int return_quantity
        {
            get => _return_quantity;
            set
            {
                int clamped = Math.Clamp(value, 0, remaining_quantity);
                if (_return_quantity == clamped) return;
                _return_quantity = clamped;
                on_property_changed();
                on_property_changed(nameof(subtotal_usd));
            }
        }

        public decimal subtotal_usd => return_quantity * unit_price_usd;
    }

    public partial class AddCreditNoteWindow : Window
    {
        private readonly CreditNotesViewModel _vm;
        private readonly string _current_month;
        private credit_note_source_dto? _source;
        private List<note_combo_item> _all_combo_items = new();

        public event EventHandler? CreditNoteCreated;

        public AddCreditNoteWindow(CreditNotesViewModel vm, string? current_month = null)
        {
            InitializeComponent();
            _vm = vm;
            _current_month = current_month ?? string.Empty;
            CreditDatePicker.SelectedDate = DateTime.Now;

            Loaded += async (_, _) => await LoadNotesAsync();
        }

        private async Task LoadNotesAsync()
        {
            try
            {
                var all_notes = new List<accounts_receivable_dto>();

                if (!string.IsNullOrEmpty(_current_month))
                {
                    var notes = await _vm.get_notes_by_month_async(_current_month);
                    all_notes.AddRange(notes);
                }
                else
                {
                    var all_months = await _vm.get_all_months_async();
                    foreach (var month in all_months)
                    {
                        var notes = await _vm.get_notes_by_month_async(month);
                        all_notes.AddRange(notes);
                    }
                }

                _all_combo_items = all_notes
                    .Where(n => n.status != "Anulada")
                    .OrderByDescending(n => n.creation_date)
                    .ThenBy(n => n.note_number)
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
                AppDialog.Show(ErrorText.Get(ex), "Error");
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
                FilterNotes(string.Empty);
                NotePopup.IsOpen = NoteListBox.Items.Count > 0;
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
                _ = LoadSourceAsync(item);
            }
        }

        private async Task LoadSourceAsync(note_combo_item item)
        {
            try
            {
                var source = await _vm.get_credit_source_async(item.note_number);
                if (source == null)
                {
                    AppDialog.Show($"No se encontro la nota de entrega {item.note_number}.", "Aviso");
                    return;
                }
                if (source.status == "Anulada")
                {
                    AppDialog.Show("La nota de entrega esta anulada; no se pueden registrar devoluciones.", "Aviso");
                    return;
                }

                _source = source;
                NoteTextBox.Text = item.Display;
                NotePopup.IsOpen = false;
                NoteTextBox.IsReadOnly = true;
                BtnClearNote.Visibility = Visibility.Visible;

                CustomerText.Text = $"{source.customer_name}  ({source.customer_code})";
                SellerText.Text = "Vendedora: " + source.seller_name;
                NoteInfoText.Text = $"Nota: {source.note_number}   |   Fecha: {source.creation_date:dd/MM/yyyy}   |   Total: {source.adjusted_total_usd:N2} USD   |   Ya devuelto: {source.already_returned_usd:N2} USD";

                var rows = source.lines
                    .Where(l => l.remaining_quantity > 0)
                    .Select(line => new credit_note_edit_row
                    {
                        id_product = line.id_product,
                        id_promotion = line.id_promotion,
                        code = line.code,
                        name = line.name,
                        unit_price_usd = line.unit_price_usd,
                        delivered_quantity = line.delivered_quantity,
                        already_returned_quantity = line.already_returned_quantity,
                        remaining_quantity = line.remaining_quantity
                    })
                    .ToList();

                foreach (var row in rows) row.PropertyChanged += OnRowPropertyChanged;

                ItemsGrid.ItemsSource = rows;
                RecalcTotal();

                if (rows.Count == 0)
                    AppDialog.Show("Esta nota ya fue devuelta por completo; no quedan cantidades disponibles.", "Aviso");
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private void OnClearNoteClick(object sender, RoutedEventArgs e)
        {
            _source = null;
            NoteTextBox.IsReadOnly = false;
            NoteTextBox.Text = "";
            BtnClearNote.Visibility = Visibility.Collapsed;
            CustomerText.Text = "";
            SellerText.Text = "";
            NoteInfoText.Text = "";
            ItemsGrid.ItemsSource = null;
            RecalcTotal();
            FilterNotes(string.Empty);
            NotePopup.IsOpen = NoteListBox.Items.Count > 0;
        }

        private void OnQuantityPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(credit_note_edit_row.return_quantity)) return;
            RecalcTotal();
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e) => RecalcTotal();

        private void RecalcTotal()
        {
            var rows = (ItemsGrid.ItemsSource as IEnumerable<credit_note_edit_row>) ?? Enumerable.Empty<credit_note_edit_row>();
            TotalText.Text = rows.Sum(r => r.subtotal_usd).ToString("N2");
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e) => await SaveAsync(false);

        private async void OnSavePdfClick(object sender, RoutedEventArgs e) => await SaveAsync(true);

        private async Task SaveAsync(bool with_pdf)
        {
            try
            {
                if (_source == null)
                {
                    AppDialog.Show("Busque primero la nota de entrega a la que corresponde la devolucion.", "Aviso");
                    return;
                }

                var rows = ((ItemsGrid.ItemsSource as IEnumerable<credit_note_edit_row>) ?? Enumerable.Empty<credit_note_edit_row>())
                    .Where(r => r.return_quantity > 0)
                    .ToList();

                if (rows.Count == 0)
                {
                    AppDialog.Show("Debe indicar al menos una cantidad a devolver.", "Aviso");
                    return;
                }

                string correlative = await _vm.generate_credit_correlative_async(_source.id_seller);
                decimal total = rows.Sum(r => r.subtotal_usd);

                var new_note = new credit_note(
                    note_number: correlative,
                    creation_date: CreditDatePicker.SelectedDate ?? DateTime.Now,
                    id_delivery_note: _source.id_delivery_note,
                    id_seller: _source.id_seller,
                    id_customer: _source.id_customer,
                    total_amount_usd: total,
                    status: "Registrada")
                {
                    observations = ObsBox.Text?.Trim()
                };

                var details = rows.Select(r => new credit_note_detail(
                    id_credit_note: 0,
                    id_product: r.id_product,
                    id_promotion: r.id_promotion,
                    quantity: r.return_quantity,
                    unit_price_usd: r.unit_price_usd,
                    subtotal_usd: r.subtotal_usd)).ToList();

                var created = await _vm.save_credit_note_async(new_note, details);

                if (with_pdf)
                {
                    var pair = await _vm.get_printable_pair_async(created);
                    NotePdfGenerator.generate(pair.original, pair.credit);
                }

                CreditNoteCreated?.Invoke(this, EventArgs.Empty);
                AppDialog.Show($"Nota de credito {created.note_number} registrada por {total:N2} USD.", "Exito");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }
    }
}