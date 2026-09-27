using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CreditNotesView : UserControl
    {
        private CreditNotesViewModel? vm => DataContext as CreditNotesViewModel;

        public CreditNotesView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => WireUp();
        }

        private void WireUp()
        {
            if (vm != null)
            {
                vm.on_request_new_credit_note_window = OpenNewCreditNoteWindow;
            }
        }

        private void OpenNewCreditNoteWindow()
        {
            if (vm == null) return;

            var window = new AddCreditNoteWindow(vm, vm.selected_month)
            {
                Owner = Window.GetWindow(this)
            };
            window.CreditNoteCreated += (_, _) =>
            {
                vm.refresh_data();
                vm.OnCreditNoteSaved?.Invoke();
            };
            window.ShowDialog();
        }

        private void OnReporteMenuClick(object sender, RoutedEventArgs e)
        {
            if (ReportePopup.IsOpen)
            {
                ReportePopup.IsOpen = false;
                return;
            }

            BuildReporteMenu();
            ReportePopup.IsOpen = ReporteMenuItems.Children.Count > 0;
        }

        private void BuildReporteMenu()
        {
            ReporteMenuItems.Children.Clear();
            if (vm == null) return;

            AddReporteMenuItem("GENERAL (todo el historial)", null, true);
            ReporteMenuItems.Children.Add(new Separator());

            // El combo de la cabecera usa una entrada vacia para "todos los meses";
            // en el menu se omite porque su equivalente es la opcion GENERAL.
            foreach (var month in vm.credit_note_months)
            {
                if (string.IsNullOrWhiteSpace(month)) continue;
                AddReporteMenuItem(Capitalize(month), month, false);
            }
        }

        private void AddReporteMenuItem(string text, string? period, bool is_default_option)
        {
            var item = new Button
            {
                Content = text,
                Tag = period,
                Style = (Style)FindResource("ReporteMenuItem")
            };

            if (is_default_option)
            {
                item.FontWeight = FontWeights.Bold;
                item.Foreground = System.Windows.Media.Brushes.DarkGreen;
            }

            item.Click += OnReporteMenuItemClick;
            ReporteMenuItems.Children.Add(item);
        }

        private void OnReporteMenuItemClick(object sender, RoutedEventArgs e)
        {
            ReportePopup.IsOpen = false;

            string? period = (sender as Button)?.Tag as string;
            OpenReporteWindow(period);
        }

        private void OpenReporteWindow(string? period)
        {
            try
            {
                var services = (Application.Current as App)?.GetServiceProvider();
                if (services == null) return;

                var report_view_model = services.GetRequiredService<CreditNotesReportViewModel>();
                report_view_model.select_period(period);

                var window = new CreditNotesReportWindow(report_view_model)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private static string Capitalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return char.ToUpper(text[0], CultureInfo.InvariantCulture) + text.Substring(1);
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as DataGrid)?.SelectedItem is credit_note_dto selected) || vm == null) return;

            var window = new CreditNoteDetailWindow(vm, selected)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }

        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is credit_note_dto selected) || vm == null) return;

            try
            {
                var pair = await vm.get_printable_pair_async(selected);
                var window = new NotePreviewWindow(pair.credit, pair.original, view_only: true)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private async void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is credit_note_dto selected) || vm == null) return;

            try
            {
                var pair = await vm.get_printable_pair_async(selected);
                if (pair.original != null)
                    NotePdfGenerator.generate(pair.original, pair.credit);
                else
                    NotePdfGenerator.generate(pair.credit);
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }
    }
}