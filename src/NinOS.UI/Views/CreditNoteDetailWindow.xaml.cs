using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NinOS.Domain.ViewModels;
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
            _vm = vm;
            _note = note;

            TitleText.Text = note.note_number;
            SourceText.Text = note.source_note_number;
            CustomerText.Text = note.customer_name;
            SellerText.Text = note.seller_name;
            DateText.Text = note.creation_date.ToString("dd/MM/yyyy");
            TotalText.Text = $"{note.total_amount_usd:N2} USD";
            StatusText.Text = note.status;

            if (note.status == "Anulada")
            {
                StatusText.Foreground = Brushes.Gray;
                BtnPdf.IsEnabled = false;
            }
            else
            {
                StatusText.Foreground = Brushes.SeaGreen;
            }

            Loaded += async (_, _) => await LoadDetailsAsync();
        }

        private async Task LoadDetailsAsync()
        {
            try
            {
                var details = (await _vm.get_credit_note_details_async(_note.id_credit_note)).ToList();
                DetailsGrid.ItemsSource = details;
                QuantityText.Text = $"{details.Sum(d => d.quantity)} uni.";
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
                NotePdfGenerator.generate(pair.original, pair.credit);
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "Error");
            }
        }
    }
}