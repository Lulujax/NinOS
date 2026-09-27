using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CreditNotesView : UserControl
    {
        private CreditNotesViewModel? vm => DataContext as CreditNotesViewModel;

        private CreditNotesReportViewModel? _reporte_vm;

        private CreditNotesReportViewModel? get_reporte_vm()
        {
            if (_reporte_vm != null) return _reporte_vm;

            var service = (Application.Current as App)?.GetServiceProvider()
                ?.GetService(typeof(ICreditNoteService)) as ICreditNoteService;

            if (service == null) return null;

            _reporte_vm = new CreditNotesReportViewModel(service);
            panel_reporte.DataContext = _reporte_vm;
            return _reporte_vm;
        }

        private async void on_reporte_click(object sender, RoutedEventArgs e)
        {
            var reporte_vm = get_reporte_vm();
            if (reporte_vm == null) return;

            if (!popup_reporte.IsOpen)
            {
                await reporte_vm.load_options_async();
            }

            popup_reporte.IsOpen = !popup_reporte.IsOpen;
        }

        private async void on_descargar_reporte_click(object sender, RoutedEventArgs e)
        {
            var reporte_vm = get_reporte_vm();
            if (reporte_vm == null) return;

            popup_reporte.IsOpen = false;

            await reporte_vm.descargar_reporte_async();
        }

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
