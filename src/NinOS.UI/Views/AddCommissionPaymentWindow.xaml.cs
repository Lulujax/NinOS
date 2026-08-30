using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AddCommissionPaymentWindow : Window
    {
        private readonly CommissionsViewModel _vm;
        private readonly List<commission_row_dto> _available;
        private readonly ObservableCollection<commission_row_dto> _to_pay = new();
        private commission_row_dto? _selected_note;
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
            RefreshTotals();
        }

        private void RefreshTotals()
        {
            decimal total = _to_pay.Sum(c => c.amount_usd);
            string seller = _to_pay.FirstOrDefault()?.seller_name ?? string.Empty;
            SummaryText.Text = $"VENDEDORA: {seller}    |    {_to_pay.Count} NOTA(S)    |    TOTAL COMISION: {total:0.00}";
            AmountBox.Text = total.ToString("0.##", CultureInfo.InvariantCulture);
            BtnRegistrar.IsEnabled = _to_pay.Count > 0;
        }

        private List<commission_row_dto> filter_matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return _available.ToList();
            }
            string q = query.Trim().ToLowerInvariant();
            return _available.Where(c =>
                (c.note_number != null && c.note_number.ToLowerInvariant().Contains(q)) ||
                (c.customer_name != null && c.customer_name.ToLowerInvariant().Contains(q))).ToList();
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
            if (NoteListBox.SelectedItem is commission_row_dto row)
            {
                _selected_note = row;
                NoteInfoText.Text = $"Nota {row.note_number}  |  {row.customer_name}  |  VENTA {row.sale_amount_usd:0.##}  |  COMISION {row.amount_usd:0.##}";
                NoteInfoBorder.Visibility = Visibility.Visible;
                NotePopup.IsOpen = false;
            }
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var note = _selected_note;
            if (note == null)
            {
                ShowError("Busque y seleccione una nota para añadir.");
                return;
            }
            ClearError();
            if (_to_pay.Any(c => c.id_commission == note.id_commission))
            {
                ShowError($"La nota {note.note_number} ya fue añadida.");
                return;
            }
            _to_pay.Add(note);
            _selected_note = null;
            _loading_list = true;
            NoteTextBox.Text = "";
            NoteListBox.ItemsSource = null;
            _loading_list = false;
            NotePopup.IsOpen = false;
            NoteInfoBorder.Visibility = Visibility.Collapsed;
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
                    ShowError("Añada al menos una nota para liquidar.");
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
                    ShowError("La tasa BS/USD es obligatoria.");
                    return;
                }

                decimal amount_usd = ParseDecimal(AmountBox.Text);
                if (amount_usd <= 0)
                {
                    ShowError("Ingrese el monto USD.");
                    return;
                }

                decimal amount_bs = ParseDecimal(BsAmountBox.Text);
                if (amount_bs <= 0)
                {
                    ShowError("El monto BS es obligatorio.");
                    return;
                }

                string reference = ReferenceBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(reference))
                {
                    ShowError("Ingrese la referencia.");
                    return;
                }

                string payment_type = (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pago Movil";

                decimal total = _to_pay.Sum(c => c.amount_usd);
                string seller = _to_pay.FirstOrDefault()?.seller_name ?? string.Empty;

                var result = MessageBox.Show(
                    $"VENDEDORA: {seller}\nNOTAS A PAGAR: {_to_pay.Count}\nTOTAL COMISION USD: {total:0.##}\n\n" +
                    $"TASA: {rate:0.##}\nMONTO BS: {amount_bs:0.##}\nTIPO: {payment_type}\nREFERENCIA: {reference}\n\n" +
                    "¿Confirmar liquidacion de comisiones?",
                    "Confirmar Liquidacion", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                int[] ids = _to_pay.Select(c => c.id_commission).ToArray();
                await _vm.pay_commissions_async(ids, rate, payment_type, reference, amount_bs);

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