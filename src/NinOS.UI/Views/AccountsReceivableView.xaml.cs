using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AccountsReceivableView : UserControl
    {
        public AccountsReceivableView()
        {
            InitializeComponent();
            DataContextChanged += UserControl_DataContextChanged;
        }

        private void SetupEvents()
        {
            if (DataContext is AccountsReceivableViewModel viewModel)
            {
                viewModel.on_request_preview_window = async (note) =>
                {
                    try
                    {
                        note_print_dto printable = await viewModel.get_printable_note_async(note.id_delivery_note);
                        NotePreviewWindow preview = new NotePreviewWindow(printable);
                        preview.Owner = Window.GetWindow(this);
                        preview.ShowDialog();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Error al cargar la nota: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                viewModel.on_request_confirmation_window = () =>
                {
                    if (viewModel.selected_note == null) return;

                    MessageBoxResult result = MessageBox.Show(
                        $"Esta seguro de anular la nota {viewModel.selected_note.note_number}?\nEl stock sera restituido.",
                        "Confirmar Anulacion",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _ = viewModel.confirm_annulation_async();
                    }
                };
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetupEvents();
        }
    }
}
