using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Logging;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CreditNoteDetailWindow : Window
    {
        private readonly CreditNotesViewModel _vm;
        private readonly credit_note_dto _note;

        public CreditNoteDetailWindow(CreditNotesViewModel vm, credit_note_dto note)
        {
            InitializeComponent();
            WindowSizingHelper.FitToScreen(this);
            _vm = vm;
            _note = note;

            TitleText.Text = note.note_number;
            SourceText.Text = note.source_note_number;
            CustomerText.Text = note.customer_name;
            SellerText.Text = note.seller_name;
            DateText.Text = AppTimeZone.to_local(note.creation_date).ToString("dd/MM/yyyy");
            TotalText.Text = $"{note.total_amount_usd:N2} USD";

            if (note.status == "Anulada")
            {
                BtnPdf.IsEnabled = false;
            }

            Loaded += async (_, _) => await LoadDetailsAsync();
        }

        private async Task LoadDetailsAsync()
        {
            try
            {
                AppLog.Info($"Abriendo detalle de nota de credito {_note.id_credit_note} desde su documento imprimible");
                var pair = await _vm.get_printable_pair_async(_note);
                var printable = pair.credit;

                CustomerText.Text = printable.customer_business_name;
                SellerText.Text = printable.seller_name;
                DateText.Text = printable.creation_date.ToString("dd/MM/yyyy");

                var details = printable.details.ToList();
                DetailsGrid.ItemsSource = details;
                QuantityText.Text = $"{details.Sum(d => d.quantity)} uni.";
                AppLog.Info($"Detalle de nota de credito {_note.id_credit_note} cargado con {details.Count} renglones");
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }

        private async void OnPdfClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var pair = await _vm.get_printable_pair_async(_note);
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