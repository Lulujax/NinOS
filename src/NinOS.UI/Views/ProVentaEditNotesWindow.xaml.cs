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
        private decimal _monto_por_pagar_actual;
        private decimal _abonado_actual;

        public class NoteOptionItem
        {
            public pro_venta_weekly_row Row { get; }
            public string DisplayLabel => $"Nota {Row.note_number} - {Row.customer_name} (Original: ${Row.monto_original:N2} | Dcto: {Row.discount_percentage_text} | Cobro: ${Row.amount:N2})";

            public NoteOptionItem(pro_venta_weekly_row row)
            {
                Row = row;
            }
        }

        private readonly int? _id_nota_inicial;

        public ProVentaEditNotesWindow(ProVentaViewModel vm, pro_venta_relation_row relation, int? id_nota_inicial = null)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _relation = relation ?? throw new ArgumentNullException(nameof(relation));
            _id_nota_inicial = id_nota_inicial;
            _abonado_actual = relation.paid_amount_usd;
            _monto_por_pagar_actual = relation.amount;

            Loaded += async (_, _) =>
            {
                InputRestrictions.attach_decimal(TxtDiscount);
                InputRestrictions.attach_decimal(TxtAmount);
                await LoadNotesAsync(_id_nota_inicial);
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

                _monto_por_pagar_actual = _notes.Where(n => !n.esta_anulada).Sum(n => n.amount);
                RefrescarEncabezado();

                TxtTotalGross.Text = $"${_notes.Sum(n => n.monto_original):N2}";
                TxtTotalAdjusted.Text = $"${_monto_por_pagar_actual:N2}";

                if (options.Count > 0)
                {
                    var match = selectNoteId.HasValue
                        ? options.FirstOrDefault(o => o.Row.id_delivery_note == selectNoteId.Value)
                        : null;
                    CmbNotes.SelectedItem = match ?? options[0];
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

        private void RefrescarEncabezado()
        {
            decimal saldo = Math.Max(0m, Money.round(_monto_por_pagar_actual - _abonado_actual));
            RelationInfoText.Text = $"{_relation.relation_label}  |  " +
                $"MONTO POR PAGAR: ${_monto_por_pagar_actual:N2}  |  ABONADO: ${_abonado_actual:N2}  |  SALDO PENDIENTE: ${saldo:N2}";
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

                if (NotesGrid.SelectedItem != item.Row)
                {
                    NotesGrid.SelectedItem = item.Row;
                }
            }
        }

        private void NotesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NotesGrid.SelectedItem is pro_venta_weekly_row row
                && CmbNotes.ItemsSource is List<NoteOptionItem> options)
            {
                var opt = options.FirstOrDefault(o => o.Row.id_delivery_note == row.id_delivery_note);
                if (opt != null && CmbNotes.SelectedItem != opt)
                {
                    CmbNotes.SelectedItem = opt;
                }
            }
        }

        private void PopulateSelectedNote()
        {
            if (_selectedNote == null) return;

            TxtCustomer.Text = string.IsNullOrWhiteSpace(_selectedNote.customer_name) ? "(Sin nombre)" : _selectedNote.customer_name;
            TxtGrossAmount.Text = $"${_selectedNote.monto_original:N2}";
            TxtCurrentDiscount.Text = _selectedNote.discount_percentage_text;
            TxtCurrentAdjusted.Text = $"${_selectedNote.amount:N2}";

            _isUpdatingInputs = true;
            TxtDiscount.Text = (_selectedNote.discount_percentage ?? 0m).ToString("0.##", CultureInfo.InvariantCulture);
            TxtAmount.Text = _selectedNote.amount.ToString("N2", CultureInfo.InvariantCulture);
            _isUpdatingInputs = false;

            MostrarVistaPrevia();
        }

        private void TxtDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs || _selectedNote == null) return;
            MostrarVistaPreviaDescuento();
        }

        private void TxtAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs || _selectedNote == null) return;
            MostrarVistaPrevia();
        }

        private async void BtnApplyDiscount_Click(object sender, RoutedEventArgs e)
        {
            decimal descuento = pro_venta_weekly_row.parse_numeric(TxtDiscount.Text);
            if (descuento < pro_venta_note_edit.DESCUENTO_MINIMO || descuento > pro_venta_note_edit.DESCUENTO_MAXIMO)
            {
                ShowStatus("El descuento debe estar entre 0 y 100.", isError: true);
                return;
            }

            await aplicar_descuento_async(descuento);
        }

        private async void BtnApplyAmount_Click(object sender, RoutedEventArgs e)
        {
            decimal monto = pro_venta_weekly_row.parse_numeric(TxtAmount.Text);
            if (monto < 0m)
            {
                ShowStatus("Escriba un monto a cobrar de 0,00 o más.", isError: true);
                return;
            }

            await aplicar_monto_async(monto);
        }

        private async void PagoConDescuento_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNote == null) return;
            await aplicar_descuento_async(_selectedNote.emission_discount_percentage ?? 0m);
        }

        private async void PagoSinDescuento_Click(object sender, RoutedEventArgs e)
        {
            await aplicar_descuento_async(0m);
        }

        private async Task aplicar_descuento_async(decimal descuento)
        {
            if (_selectedNote == null)
            {
                ShowStatus("Debe seleccionar una nota primero.", isError: true);
                return;
            }

            int id_nota = _selectedNote.id_delivery_note;
            string numero_nota = _selectedNote.note_number;
            decimal monto_esperado = _selectedNote.amount;

            await ejecutar_guardado_async(
                id_nota,
                () => _vm.update_note_discount_async(id_nota, descuento, monto_esperado),
                $"Nota {numero_nota} con descuento {descuento:0.##}% aplicado.");
        }

        private async Task aplicar_monto_async(decimal monto)
        {
            if (_selectedNote == null)
            {
                ShowStatus("Debe seleccionar una nota primero.", isError: true);
                return;
            }

            int id_nota = _selectedNote.id_delivery_note;
            string numero_nota = _selectedNote.note_number;
            decimal monto_esperado = _selectedNote.amount;

            await ejecutar_guardado_async(
                id_nota,
                () => _vm.update_note_amount_async(id_nota, monto, monto_esperado),
                $"Nota {numero_nota} con monto a cobrar ${monto:N2} guardado.");
        }

        private async Task ejecutar_guardado_async(int id_nota, Func<Task> guardar, string mensaje_exito)
        {
            HabilitarBotonesDeGuardado(false);
            try
            {
                await guardar();
                await LoadNotesAsync(id_nota);
                ShowStatus(mensaje_exito, isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"No se guardó: {ErrorText.Get(ex)}", isError: true);
                await LoadNotesAsync(id_nota);
            }
            finally
            {
                HabilitarBotonesDeGuardado(true);
            }
        }

        private void HabilitarBotonesDeGuardado(bool habilitado)
        {
            BtnApplyDiscount.IsEnabled = habilitado;
            BtnApplyAmount.IsEnabled = habilitado;
            BtnPagoConDescuento.IsEnabled = habilitado;
            BtnPagoSinDescuento.IsEnabled = habilitado;
        }

        private void MostrarVistaPrevia()
        {
            if (_selectedNote == null) return;

            decimal monto_nuevo = pro_venta_weekly_row.parse_numeric(TxtAmount.Text);
            if (monto_nuevo < 0m)
            {
                ShowStatus("Escriba un monto a cobrar de 0,00 o más.", isError: true);
                return;
            }

            decimal original = _selectedNote.monto_original;
            decimal diferencia = monto_nuevo - _selectedNote.amount;
            decimal monto_por_pagar_despues = _monto_por_pagar_actual + diferencia;
            decimal saldo_antes = Math.Max(0m, Money.round(_monto_por_pagar_actual - _abonado_actual));
            decimal saldo_despues = Math.Max(0m, Money.round(monto_por_pagar_despues - _abonado_actual));

            ShowStatus(
                $"Comisión: ${pro_venta_calculator.comision(original):N2}  |  Gastos: ${pro_venta_calculator.gastos_operativos(original) + pro_venta_calculator.gastos_administrativos(original):N2}  |  " +
                $"Monto por pagar: ${_monto_por_pagar_actual:N2} → ${monto_por_pagar_despues:N2}  |  " +
                $"Saldo de la relación: ${saldo_antes:N2} → ${saldo_despues:N2}",
                isError: false,
                isPreview: true);
        }

        private void MostrarVistaPreviaDescuento()
        {
            if (_selectedNote == null || _selectedNote.monto_original <= 0m) return;

            decimal descuento = pro_venta_weekly_row.parse_numeric(TxtDiscount.Text);
            if (descuento < pro_venta_note_edit.DESCUENTO_MINIMO || descuento > pro_venta_note_edit.DESCUENTO_MAXIMO)
            {
                ShowStatus("El descuento debe estar entre 0 y 100.", isError: true);
                return;
            }

            decimal monto = pro_venta_note_edit.monto_desde_descuento(_selectedNote.monto_original, descuento);
            ShowStatus(
                $"Con {descuento:0.##}% el monto a cobrar sería ${monto:N2}. Pulse «Aplicar descuento» para guardarlo.",
                isError: false,
                isPreview: true);
        }

        private void ShowStatus(string msg, bool isError, bool isPreview = false)
        {
            StatusMessage.Text = msg;
            StatusMessage.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(198, 40, 40))
                : isPreview
                    ? new SolidColorBrush(Color.FromRgb(21, 101, 192))
                    : new SolidColorBrush(Color.FromRgb(46, 125, 50));
            StatusMessage.Visibility = Visibility.Visible;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
