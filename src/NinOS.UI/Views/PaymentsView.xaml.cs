using System.Windows;
using System.Windows.Controls;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PaymentsView : UserControl
    {
        public PaymentsView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => SetupEvents();
        }

        private void SetupEvents()
        {
            if (DataContext is PaymentsViewModel vm)
            {
                vm.on_request_payment_window = (note) =>
                {
                    var win = new PaymentWindow(vm, note);
                    win.Owner = Window.GetWindow(this);
                    win.ShowDialog();
                };

                vm.on_request_note_history_window = async (id_delivery_note) =>
                {
                    try
                    {
                        var payments = await vm.get_payments_by_note_async(id_delivery_note);
                        var win = new PaymentNoteHistoryWindow(payments);
                        win.Owner = Window.GetWindow(this);
                        win.ShowDialog();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Error al cargar historial: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
            }
        }
    }
}
