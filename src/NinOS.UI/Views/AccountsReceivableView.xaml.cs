using System.Windows;
using System.Windows.Controls;
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
                viewModel.on_request_payment_window = () =>
                {
                    if (viewModel.selected_note == null) return;

                    PaymentWindow window = new PaymentWindow(viewModel, viewModel.selected_note);
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                };

                viewModel.on_request_confirmation_window = () =>
                {
                    if (viewModel.selected_note == null) return;

                    MessageBoxResult result = MessageBox.Show(
                        $"¿Esta seguro de anular la nota {viewModel.selected_note.note_number}?\nEl stock de los productos sera restituido.",
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