using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class ProVentaEditNotesWindow : Window
    {
        private readonly ProVentaViewModel _vm;
        private readonly pro_venta_relation_row _relation;
        private List<pro_venta_weekly_row> _notes = new();
        private pro_venta_weekly_row? _selectedNote;
        private bool _isUpdatingInputs;

        public class NoteOptionItem
        {
            public pro_venta_weekly_row Row { get; }
            public string DisplayLabel => $"Nota {Row.note_number} - {Row.customer_name} (Base: ${Row.gross_total_usd:N2} | Dcto: {Row.discount_percentage_text} | Cobro: ${Row.amount:N2})";

            public NoteOptionItem(pro_venta_weekly_row row)
            {
                Row = row;
            }
        }

        public ProVentaEditNotesWindow(ProVentaViewModel vm, pro_venta_relation_row relation)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _relation = relation ?? throw new ArgumentNullException(nameof(relation));

            RelationInfoText.Text = $"{relation.relation_label}  |  " +
                $"MONTO POR PAGAR: ${relation.amount:N2}  |  ABONADO: ${relation.paid_amount_usd:N2}  |  SALDO PENDIENTE: ${relation.balance_due_usd:N2}";

            Loaded += async (_, _) =>
            {
                InputRestrictions.attach_decimal(TxtDiscount);
                InputRestrictions.attach_decimal(TxtAmount);
                await LoadNotesAsync();
            };
        }

        private async Task LoadNotesAsync(int? selectNoteId = null)
        {
            try
            {
                _notes = await _vm.get_relation_notes_async(_relation.id_relacion);
                NotesGrid.ItemsSource = null;
                NotesGrid.ItemsSource = _notes;

                var options = _notes.Select(n => new NoteOptionItem(n)).ToList();
                CmbNotes.ItemsSource = options;

                // Actualizar totales
                decimal totalGross = _notes.Sum(n => n.gross_total_usd);
                decimal totalAdjusted = _notes.Sum(n => n.amount);
                TxtTotalGross.Text = $"${totalGross:N2}";
                TxtTotalAdjusted.Text = $"${totalAdjusted:N2}";

                if (options.Count > 0)
                {
                    if (selectNoteId.HasValue)
                    {
                        var match = options.FirstOrDefault(o => o.Row.id_delivery_note == selectNoteId.Value);
                        CmbNotes.SelectedItem = match ?? options[0];
                    }
                    else
                    {
                        CmbNotes.SelectedIndex = 0;
                    }
                }
                else
                {
                    _selectedNote = null;
                    ClearEditor();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error al cargar notas: {ErrorText.Get(ex)}", isError: true);
            }
        }

        private void ClearEditor()
        {
            TxtCustomer.Text = "-";
            TxtGrossAmount.Text = "$0.00";
            TxtCurrentDiscount.Text = "0%";
            TxtCurrentAdjusted.Text = "$0.00";
            _isUpdatingInputs = true;
            TxtDiscount.Text = "0";
            TxtAmount.Text = "0.00";
            _isUpdatingInputs = false;
        }

        private void CmbNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbNotes.SelectedItem is NoteOptionItem item)
            {
                _selectedNote = item.Row;
                PopulateSelectedNote();

                // Sincronizar selección en la grilla
                if (NotesGrid.SelectedItem != item.Row)
                {
                    NotesGrid.SelectedItem = item.Row;
                }
            }
        }

        private void NotesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NotesGrid.SelectedItem is pro_venta_weekly_row row)
            {
                if (CmbNotes.ItemsSource is List<NoteOptionItem> options)
                {
                    var opt = options.FirstOrDefault(o => o.Row.id_delivery_note == row.id_delivery_note);
                    if (opt != null && CmbNotes.SelectedItem != opt)
                    {
                        CmbNotes.SelectedItem = opt;
                    }
                }
            }
        }

        private void PopulateSelectedNote()
        {
            if (_selectedNote == null) return;

            TxtCustomer.Text = string.IsNullOrWhiteSpace(_selectedNote.customer_name) ? "(Sin nombre)" : _selectedNote.customer_name;
            TxtGrossAmount.Text = $"${_selectedNote.gross_total_usd:N2}";
            TxtCurrentDiscount.Text = _selectedNote.discount_percentage_text;
            TxtCurrentAdjusted.Text = $"${_selectedNote.amount:N2}";

            _isUpdatingInputs = true;
            decimal currentDcto = _selectedNote.discount_percentage ?? 0m;
            TxtDiscount.Text = currentDcto.ToString("0.##", CultureInfo.InvariantCulture);
            TxtAmount.Text = _selectedNote.amount.ToString("N2", CultureInfo.InvariantCulture);
            _isUpdatingInputs = false;

            StatusMessage.Visibility = Visibility.Collapsed;
        }

        private void TxtDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs || _selectedNote == null) return;

            if (decimal.TryParse(TxtDiscount.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dcto))
            {
                if (dcto < 0) dcto = 0;
                if (dcto > 100) dcto = 100;

                decimal gross = _selectedNote.gross_total_usd;
                decimal calculated = Money.round(gross * (1m - (dcto / 100m)));

                _isUpdatingInputs = true;
                TxtAmount.Text = calculated.ToString("N2", CultureInfo.InvariantCulture);
                _isUpdatingInputs = false;
            }
        }

        private void TxtAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs || _selectedNote == null) return;

            if (decimal.TryParse(TxtAmount.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto))
            {
                decimal gross = _selectedNote.gross_total_usd;
                if (gross > 0)
                {
                    decimal effectiveDcto = Math.Max(0m, Math.Round((1m - (monto / gross)) * 100m, 2));
                    if (effectiveDcto > 100m) effectiveDcto = 100m;

                    _isUpdatingInputs = true;
                    TxtDiscount.Text = effectiveDcto.ToString("0.##", CultureInfo.InvariantCulture);
                    _isUpdatingInputs = false;
                }
            }
        }

        private void QuickDiscount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr && _selectedNote != null)
            {
                if (decimal.TryParse(tagStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pct))
                {
                    _isUpdatingInputs = true;
                    TxtDiscount.Text = pct.ToString("0.##", CultureInfo.InvariantCulture);

                    decimal gross = _selectedNote.gross_total_usd;
                    decimal calculated = Money.round(gross * (1m - (pct / 100m)));
                    TxtAmount.Text = calculated.ToString("N2", CultureInfo.InvariantCulture);
                    _isUpdatingInputs = false;
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNote == null)
            {
                ShowStatus("Debe seleccionar una nota primero.", isError: true);
                return;
            }

            if (_selectedNote.esta_anulada)
            {
                ShowStatus("No se puede editar una nota anulada.", isError: true);
                return;
            }

            if (string.Equals(_selectedNote.status?.Trim(), "Pagada", StringComparison.OrdinalIgnoreCase))
            {
                ShowStatus("Esta nota ya se encuentra pagada y no puede modificarse.", isError: true);
                return;
            }

            if (!decimal.TryParse(TxtAmount.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal newAmount) || newAmount < 0)
            {
                ShowStatus("El monto USD debe ser un número válido mayor o igual a 0.", isError: true);
                return;
            }

            decimal? newDcto = null;
            if (decimal.TryParse(TxtDiscount.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedDcto))
            {
                if (parsedDcto < 0 || parsedDcto > 100)
                {
                    ShowStatus("El porcentaje de descuento debe estar entre 0 y 100.", isError: true);
                    return;
                }
                newDcto = parsedDcto;
            }

            BtnSave.IsEnabled = false;
            try
            {
                await _vm.update_note_total_async(_selectedNote.id_delivery_note, newAmount, newDcto);

                int savedId = _selectedNote.id_delivery_note;
                string noteNum = _selectedNote.note_number;

                await LoadNotesAsync(savedId);

                ShowStatus($"¡Nota {noteNum} actualizada correctamente a ${newAmount:N2} (Dcto: {newDcto ?? 0:0.##}%)!", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Error al guardar: {ErrorText.Get(ex)}", isError: true);
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void ShowStatus(string msg, bool isError)
        {
            StatusMessage.Text = msg;
            StatusMessage.Foreground = isError ? new SolidColorBrush(Color.FromRgb(198, 40, 40)) : new SolidColorBrush(Color.FromRgb(46, 125, 50));
            StatusMessage.Visibility = Visibility.Visible;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
