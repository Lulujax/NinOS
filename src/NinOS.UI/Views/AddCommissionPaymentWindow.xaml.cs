using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NinOS.Domain;
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
        private bool _loading_sellers;
        private int? _selected_seller_id;
        private string _selected_seller_name = string.Empty;

        /// <summary>Opcion del combo de vendedora: solo el id y el nombre, que es lo que se usa.</summary>
        private sealed class SellerOption
        {
            public int id_seller { get; }
            public string full_name { get; }

            public SellerOption(int id_seller, string full_name)
            {
                this.id_seller = id_seller;
                this.full_name = full_name;
            }

            public override string ToString() => full_name;
        }

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
            InputRestrictions.attach_decimal(RateBox);
            InputRestrictions.attach_decimal(BsAmountBox);

            LoadSellers();

            // Sin vendedora elegida no se buscan notas: la lista arranca vacia a proposito.
            SearchBoxHint();
            RefreshTotals();
        }

        /// <summary>
        /// Arma el combo de vendedoras con las que tienen comision pendiente. Arranca vacio:
        /// el usuario tiene que elegir quien antes de buscar notas, asi no mezclan notas de
        /// varias vendedoras en la misma liquidacion.
        /// Se arma desde las propias notas disponibles y no desde la lista de vendedores del
        /// ViewModel: esa todavia puede no estar cargada cuando se abre esta ventana desde
        /// una fila, y el combo salia vacio sin ninguna nota que buscar.
        /// </summary>
        private void LoadSellers()
        {
            var seller_ids = _available
                .Select(c => c.id_seller)
                .Where(id => id > 0)
                .ToHashSet();

            var sellers = seller_ids
                .Select(id =>
                {
                    var full_name = _available
                        .Where(c => c.id_seller == id)
                        .Select(c => c.seller_name)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                        ?? string.Empty;

                    return new SellerOption(id, full_name);
                })
                .Where(o => o.full_name.Length > 0)
                .OrderBy(o => o.full_name)
                .ToList();

            _loading_sellers = true;
            SellerCombo.ItemsSource = sellers;
            SellerCombo.SelectedItem = null;
            _loading_sellers = false;
        }

        private void OnSellerChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading_sellers) return;

            _selected_seller_id = (SellerCombo.SelectedItem as SellerOption)?.id_seller;
            _selected_seller_name = (SellerCombo.SelectedItem as SellerOption)?.full_name ?? string.Empty;

            // Cambiar de vendedora con notas ya cargadas dejaria una liquidacion mezclada.
            if (_to_pay.Count > 0)
            {
                ShowError($"Ya tiene {_to_pay.Count} nota(s) de otra vendedora. Quite las notas antes de cambiar.");
                _loading_sellers = true;
                SellerCombo.SelectedItem = null;
                _loading_sellers = false;
                _selected_seller_id = null;
                return;
            }

            ClearError();
            SearchBoxHint();
        }

        /// <summary>
        /// Pista que se muestra en el buscador. Sin vendedora elegida dice que hay que elegir
        /// una; con vendedora dice cuantas notas hay de ella.
        /// </summary>
        private void SearchBoxHint()
        {
            _loading_list = true;
            if (_selected_seller_id == null)
            {
                NoteTextBox.Text = "";
                NoteTextBox.ToolTip = "Primero elija la vendedora.";
                NoteListBox.ItemsSource = null;
            }
            else
            {
                NoteTextBox.ToolTip = $"Buscando notas de {_selected_seller_name}.";
                NoteListBox.ItemsSource = null;
            }
            _loading_list = false;
            NotePopup.IsOpen = false;
        }

        private void RefreshTotals()
        {
            decimal total_pending = _to_pay.Sum(c => c.remaining_amount_usd);
            string seller_name = !string.IsNullOrEmpty(_selected_seller_name)
                ? _selected_seller_name
                : _to_pay.FirstOrDefault()?.seller_name ?? string.Empty;

            SummaryText.Text = _selected_seller_id == null
                ? $"Elija la vendedora y luego agregue sus notas.    |    TOTAL PENDIENTE: {total_pending:0.00}"
                : $"VENDEDORA: {seller_name}    |    {_to_pay.Count} NOTA(S)    |    TOTAL PENDIENTE: {total_pending:0.00}";

            AmountBox.Text = total_pending.ToString("0.##", CultureInfo.InvariantCulture);
            BtnRegistrar.IsEnabled = _to_pay.Count > 0;
        }

        private List<commission_row_dto> filter_matches(string query)
        {
            // Sin vendedora elegida no se muestra ninguna nota. La lista se limita a las notas
            // de esa vendedora, que es el punto del combo: evita agregar la nota equivocada.
            if (_selected_seller_id == null) return new List<commission_row_dto>();

            IEnumerable<commission_row_dto> rows = _available.Where(c => c.id_seller == _selected_seller_id.Value);

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

            // Todavia no eligio vendedora: no se busca nada.
            if (_selected_seller_id == null)
            {
                NotePopup.IsOpen = false;
                NoteListBox.ItemsSource = null;
                return;
            }

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

            // Sin vendedora elegida el desplegable no abre: primero hay que elegir quien.
            if (_selected_seller_id == null)
            {
                ShowError("Primero seleccione la vendedora de la liquidacion.");
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

            // Red de seguridad: la lista ya viene filtrada por vendedora, pero si por un error de
            // datos apareciera una nota de otra, no se deja agregar.
            if (_selected_seller_id == null || row.id_seller != _selected_seller_id.Value)
            {
                string elegida = string.IsNullOrEmpty(_selected_seller_name) ? "la vendedora seleccionada" : _selected_seller_name;
                ShowError($"La nota {row.note_number} es de otra vendedora ({row.seller_name}). Solo puede añadir notas de {elegida}.");
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

                if (string.IsNullOrWhiteSpace(RateBox.Text))
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }
                if (InputRestrictions.has_letters(RateBox.Text))
                {
                    ShowError("No se permiten letras en la tasa. Ingrese solo números.");
                    return;
                }
                if (!InputRestrictions.is_valid_decimal(RateBox.Text, out decimal rate) || rate <= 0)
                {
                    ShowError("La tasa debe ser un número mayor a 0.");
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

                if (string.IsNullOrWhiteSpace(BsAmountBox.Text))
                {
                    ShowError("Tienes que llenar los campos obligatorios.");
                    return;
                }
                if (InputRestrictions.has_letters(BsAmountBox.Text))
                {
                    ShowError("No se permiten letras en el monto en Bs. Ingrese solo números.");
                    return;
                }
                if (!InputRestrictions.is_valid_decimal(BsAmountBox.Text, out decimal amount_bs) || amount_bs <= 0)
                {
                    ShowError("El monto en Bs debe ser un número mayor a 0.");
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

                var result = AppDialog.Show(
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
                    var pdf_result = AppDialog.Show(
                        "La comision fue liquidada.\n\n¿Desea generar el PDF del comprobante de pago de comision?",
                        "Generar PDF",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (pdf_result == MessageBoxResult.Yes)
                    {
                        var receipt = await _vm.get_commission_receipt_async(ids, reference);
                        if (receipt == null || receipt.rows.Count == 0)
                        {
                            AppDialog.Show("No se encontro informacion del comprobante.", "Comprobante",
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