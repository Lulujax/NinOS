using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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