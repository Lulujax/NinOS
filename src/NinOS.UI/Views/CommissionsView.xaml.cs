using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CommissionsView : UserControl
    {
        public CommissionsView()
        {
            InitializeComponent();
            DataContextChanged += UserControl_DataContextChanged;
        }

        private void SetupEvents()
        {
            if (DataContext is CommissionsViewModel viewModel)
            {
                viewModel.on_request_add_commission_payment_window = () =>
                {
                    try
                    {
                        var window = new AddCommissionPaymentWindow(viewModel);
                        window.Owner = Window.GetWindow(this);
                        window.ShowDialog();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Error al abrir Pago de Comision: {ex.Message}", "Error");
                    }
                };
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetupEvents();
        }

        private void txt_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_clear_search.Visibility = !string.IsNullOrEmpty(txt_search.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void btn_clear_search_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is CommissionsViewModel vm) vm.search_query = "";
        }
    }
}