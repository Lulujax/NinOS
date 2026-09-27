using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as DataGrid)?.SelectedItem is credit_note_dto selected) || vm == null) return;

            var window = new CreditNoteDetailWindow(vm, selected)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }

        private async void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is credit_note_dto selected) || vm == null) return;

            try
            {
                var pair = await vm.get_printable_pair_async(selected);
                NotePdfGenerator.generate(pair.original, pair.credit);
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }
    }
}