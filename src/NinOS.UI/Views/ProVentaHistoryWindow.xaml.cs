using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class ProVentaHistoryWindow : Window
    {
        private readonly ProVentaViewModel _vm;
        private readonly pro_venta_relation_row _row;

        public ProVentaHistoryWindow(ProVentaViewModel vm, pro_venta_relation_row row)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _row = row ?? throw new ArgumentNullException(nameof(row));

            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            var notes = await _vm.get_relation_notes_async(_row.id_relacion);
            NotesGrid.ItemsSource = notes;

            var payments = (await _vm.get_relation_payments_async(_row.id_relacion))
                .OrderBy(p => p.payment_date)
                .ToList();
            HistoryGrid.ItemsSource = payments;

            decimal amount = notes.Sum(n => n.amount);
            decimal paid = payments.Sum(p => p.amount_usd);
            decimal balance = amount - paid;
            if (balance < 0) balance = 0;

            RelationInfoText.Text = $"{_row.relation_label}\n" +
                $"MONTO: {amount:N2}  |  ABONADO: {paid:N2}  |  SALDO PENDIENTE: {balance:N2}";
        }

        private async void EditPayment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not payment_dto dto) return;

            var result = PromptEdit(dto);
            if (result == null) return;

            try
            {
                await _vm.update_relation_payment_async(dto.id_payment, result.Value.amount, result.Value.date, result.Value.observations);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al modificar el pago: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (decimal amount, DateTime date, string observations)? PromptEdit(payment_dto dto)
        {
            var window = new Window
            {
                Title = "Editar pago",
                Width = 380,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White,
                FontSize = 12
            };

            var grid = new Grid { Margin = new Thickness(14) };
            for (int i = 0; i < 3; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var dateLabel = new TextBlock { Text = "Fecha:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(dateLabel, 0); Grid.SetColumn(dateLabel, 0);
            grid.Children.Add(dateLabel);

            var datePicker = new DatePicker { SelectedDate = dto.payment_date, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(datePicker, 0); Grid.SetColumn(datePicker, 1);
            grid.Children.Add(datePicker);

            var amountLabel = new TextBlock { Text = "Monto USD:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(amountLabel, 1); Grid.SetColumn(amountLabel, 0);
            grid.Children.Add(amountLabel);

            var amountBox = new TextBox
            {
                Text = dto.amount_usd.ToString("0.##", CultureInfo.InvariantCulture),
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(amountBox, 1); Grid.SetColumn(amountBox, 1);
            grid.Children.Add(amountBox);

            var obsLabel = new TextBlock { Text = "Observacion:", VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 0, 8) };
            Grid.SetRow(obsLabel, 2); Grid.SetColumn(obsLabel, 0);
            grid.Children.Add(obsLabel);

            var obsBox = new TextBox
            {
                Text = dto.notes,
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalContentAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(obsBox, 2); Grid.SetColumn(obsBox, 1);
            grid.Children.Add(obsBox);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(buttons, 3); Grid.SetColumnSpan(buttons, 2);

            var cancel = new Button { Content = "Cancelar", Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 0, 6, 0), Cursor = System.Windows.Input.Cursors.Hand };
            var ok = new Button { Content = "Guardar", Padding = new Thickness(12, 5, 12, 5), Background = Brushes.SeaGreen, Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };

            buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            grid.Children.Add(buttons);

            window.Content = grid;

            (decimal amount, DateTime date, string observations)? result = null;

            cancel.Click += (_, _) => { window.DialogResult = false; };
            ok.Click += (_, _) =>
            {
                if (datePicker.SelectedDate == null)
                {
                    MessageBox.Show("Seleccione una fecha.", "Fecha invalida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string norm = (amountBox.Text ?? string.Empty).Trim().Replace(',', '.');
                if (!decimal.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) || value <= 0)
                {
                    MessageBox.Show("Ingrese un monto valido mayor a 0.", "Monto invalido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                result = (value, datePicker.SelectedDate.Value, obsBox.Text ?? string.Empty);
                window.DialogResult = true;
            };

            window.ShowDialog();
            return result;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
