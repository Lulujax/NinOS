using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class SalesView : UserControl
    {
        public SalesView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => SetupEvents();
        }

        private void SetupEvents()
        {
            if (DataContext is SalesViewModel vm)
            {
                vm.on_request_preview_window = async (note) =>
                {
                    try
                    {
                        note_print_dto printable = await vm.get_printable_note_async(note.id_delivery_note);
                        NotePreviewWindow preview = new NotePreviewWindow(printable);
                        preview.Owner = Window.GetWindow(this);
                        preview.ShowDialog();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Error al cargar la nota: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
            }
        }
    }
}
