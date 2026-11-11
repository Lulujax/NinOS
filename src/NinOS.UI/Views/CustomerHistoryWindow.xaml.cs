using System;
using System.Collections.Generic;
using System.Windows;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CustomerHistoryWindow : Window
    {
        private readonly CustomerHistoryViewModel _vm;
        private readonly IServiceProvider _serviceProvider;

        public CustomerHistoryWindow(CustomerHistoryViewModel vm, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            WindowSizingHelper.FitToScreen(this, startMaximizedIfScreenSmall: true);
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            DataContext = _vm;

            SetupCallbacks();
        }

        private void SetupCallbacks()
        {
            _vm.OnRequestPreviewNote = (notePrint) =>
            {
                var doc = NotePdfGenerator.build_document(notePrint);
                var fileName = NotePdfGenerator.get_default_file_name(notePrint);
                var preview = new PdfPreviewWindow(
                    doc,
                    $"Nota de Entrega {notePrint.note_number}",
                    fileName,
                    () => NoteExcelExporter.export(notePrint))
                {
                    Owner = this
                };
                preview.ShowDialog();
            };

            _vm.OnRequestNotePayments = (note, payments) =>
            {
                var paymentsVm = _serviceProvider.GetService(typeof(PaymentsViewModel)) as PaymentsViewModel;
                if (paymentsVm != null)
                {
                    var window = new PaymentNoteHistoryWindow(paymentsVm, note, payments)
                    {
                        Owner = this
                    };
                    window.ShowDialog();
                }
            };

            _vm.OnRequestCreditNoteDetail = (creditNote) =>
            {
                var creditVm = _serviceProvider.GetService(typeof(CreditNotesViewModel)) as CreditNotesViewModel;
                if (creditVm != null)
                {
                    var window = new CreditNoteDetailWindow(creditVm, creditNote)
                    {
                        Owner = this
                    };
                    window.ShowDialog();
                }
            };
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

