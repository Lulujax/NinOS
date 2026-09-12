using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class ProVentaView : UserControl
    {
        public ProVentaView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is ProVentaViewModel view_model)
                {
                    view_model.initialize();
                    SetupEvents();
                }
            };
            DataContextChanged += (_, _) => SetupEvents();
        }

        private void SetupEvents()
        {
            if (DataContext is not ProVentaViewModel view_model) return;

            view_model.on_request_preview_window = async (row) =>
            {
                try
                {
                    note_print_dto printable = await view_model.get_printable_note_async(row.id_delivery_note);
                    NotePreviewWindow preview = new NotePreviewWindow(printable);
                    preview.Owner = Window.GetWindow(this);
                    preview.ShowDialog();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error al cargar la nota: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            view_model.on_request_payment_window = (row) =>
            {
                ProVentaPaymentWindow window = new ProVentaPaymentWindow(view_model, row);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            };
        }
    }
}