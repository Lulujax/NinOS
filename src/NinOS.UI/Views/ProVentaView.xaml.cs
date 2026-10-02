using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;
using NinOS.UI.Common;

namespace NinOS.UI.Views
{
    public partial class ProVentaView : UserControl
    {
        public ProVentaView()
        {
            InitializeComponent();
            SetupGridSync();
            Loaded += async (s, e) =>
            {
                if (DataContext is ProVentaViewModel view_model)
                {
                    view_model.initialize();
                    SetupEvents();
                    SyncWeeklyTotalsGrid();
                }
            };
            DataContextChanged += (_, _) => SetupEvents();
        }

        private void RelationGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IsInsideButton(e.OriginalSource as DependencyObject)) return;

            if (sender is DataGrid dg
                && dg.SelectedItem is pro_venta_relation_row row
                && DataContext is ProVentaViewModel vm)
            {
                ProVentaHistoryWindow window = new ProVentaHistoryWindow(vm, row) { Owner = Window.GetWindow(this) };
                window.ShowDialog();
            }
        }

        private static bool IsInsideButton(DependencyObject? source)
        {
            try
            {
                while (source != null)
                {
                    if (source is Button) return true;
                    source = System.Windows.Media.VisualTreeHelper.GetParent(source);
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private void SetupEvents()
        {
            if (DataContext is not ProVentaViewModel view_model) return;

            view_model.on_request_relation_pdf = async (row) =>
            {
                await view_model.print_relation_pdf_async(row);
            };

            view_model.on_request_note_preview = async (row) =>
            {
                try
                {
                    note_print_dto printable = await view_model.get_printable_note_async(row.id_delivery_note);
                    NotePreviewWindow preview = new NotePreviewWindow(printable, view_only: true);
                    preview.Owner = Window.GetWindow(this);
                    preview.ShowDialog();
                }
                catch (System.Exception ex)
                {
                    AppDialog.Show($"Error al cargar la nota: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            view_model.on_request_payment_window = (row) =>
            {
                ProVentaPaymentWindow window = new ProVentaPaymentWindow(view_model, row);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            };

            view_model.on_request_annul_note = async (row) =>
            {
                string seller_line = string.IsNullOrWhiteSpace(row.seller_name)
                        ? string.Empty
                        : $"Vendedor: {row.seller_name} ({row.seller_code})\n";

                    string message =
                        $"Esta seguro de anular la nota {row.note_number}?\n\n" +
                        $"Cliente: {row.customer_name}\n" +
                        seller_line +
                        $"Monto: {row.amount:N2}\n\n" +
                        $"El stock sera restituido y la nota quedara marcada como Anulada.\n" +
                        $"Se anotara en el historial de la relacion como un abono en rojo.";

                MessageBoxResult result = AppDialog.Show(
                    message,
                    "Confirmar Anulacion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await view_model.confirm_annul_note_async(row);
                }
            };
        }

        private System.Windows.Threading.DispatcherTimer? _relationSearchTimer;

        private void CmbRelation_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _relationSearchTimer?.Stop();
                CommitRelationText();
                e.Handled = true;
            }
        }

        private void CmbRelation_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Tab || e.Key == Key.Down || e.Key == Key.Up)
                return;

            _relationSearchTimer?.Stop();
            _relationSearchTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(350) };
            _relationSearchTimer.Tick += (s, args) =>
            {
                _relationSearchTimer.Stop();
                CommitRelationText();
            };
            _relationSearchTimer.Start();
        }

        private void CmbRelation_LostFocus(object sender, RoutedEventArgs e)
        {
            _relationSearchTimer?.Stop();
            CommitRelationText();
        }

        private void CommitRelationText()
        {
            if (DataContext is not ProVentaViewModel vm) return;
            string text = CmbRelation.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(text))
            {
                if (vm.select_relation_by_text(text))
                {
                    CmbRelation.SelectedItem = vm.selected_relation;
                    if (vm.selected_relation != null)
                    {
                        CmbRelation.Text = vm.selected_relation.label;
                    }
                }
                else if (vm.selected_relation != null)
                {
                    CmbRelation.Text = vm.selected_relation.label;
                }
            }
            else if (vm.selected_relation != null)
            {
                CmbRelation.Text = vm.selected_relation.label;
            }
        }

        private bool _isSyncingWeeklyTotals;

        private void SetupGridSync()
        {
            WeeklyDataGrid.SizeChanged += (s, e) => SyncWeeklyTotalsGrid();
            WeeklyDataGrid.Loaded += (s, e) => SyncWeeklyTotalsGrid();
            WeeklyDataGrid.LayoutUpdated += (s, e) => SyncWeeklyTotalsGrid();
            WeeklyDataGrid.IsVisibleChanged += (s, e) =>
            {
                if (WeeklyDataGrid.IsVisible) SyncWeeklyTotalsGrid();
            };

            var actualWidthProp = DependencyPropertyDescriptor.FromProperty(
                DataGridColumn.ActualWidthProperty, typeof(DataGridColumn));

            if (actualWidthProp != null)
            {
                foreach (var col in WeeklyDataGrid.Columns)
                {
                    actualWidthProp.AddValueChanged(col, (_, _) => SyncWeeklyTotalsGrid());
                }
            }
        }

        private void SyncWeeklyTotalsGrid()
        {
            if (_isSyncingWeeklyTotals) return;
            if (WeeklyDataGrid == null || WeeklyTotalsGrid == null) return;
            if (WeeklyDataGrid.Columns.Count < 9 || WeeklyTotalsGrid.ColumnDefinitions.Count < 9) return;

            try
            {
                _isSyncingWeeklyTotals = true;
                for (int i = 0; i < 8; i++)
                {
                    double colWidth = WeeklyDataGrid.Columns[i].ActualWidth;
                    if (colWidth > 0)
                    {
                        var colDef = WeeklyTotalsGrid.ColumnDefinitions[i];
                        if (colDef.Width.GridUnitType != GridUnitType.Pixel || Math.Abs(colDef.Width.Value - colWidth) > 0.5)
                        {
                            colDef.Width = new GridLength(colWidth, GridUnitType.Pixel);
                        }
                    }
                }

                double col8Width = WeeklyDataGrid.Columns[8].ActualWidth;
                if (col8Width > 0)
                {
                    var colDef8 = WeeklyTotalsGrid.ColumnDefinitions[8];
                    if (colDef8.Width.GridUnitType != GridUnitType.Pixel || Math.Abs(colDef8.Width.Value - col8Width) > 0.5)
                    {
                        colDef8.Width = new GridLength(col8Width, GridUnitType.Pixel);
                    }
                }
            }
            catch
            {
                // Ignore layout sync exceptions
            }
            finally
            {
                _isSyncingWeeklyTotals = false;
            }
        }
    }
}