using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CommissionsView : UserControl
    {
        public static readonly DependencyProperty PaymentsContextProperty =
            DependencyProperty.Register(nameof(PaymentsContext), typeof(PaymentsViewModel), typeof(CommissionsView),
                new PropertyMetadata(null));

        public PaymentsViewModel? PaymentsContext
        {
            get => (PaymentsViewModel?)GetValue(PaymentsContextProperty);
            set => SetValue(PaymentsContextProperty, value);
        }

        public CommissionsView()
        {
            InitializeComponent();
            DataContextChanged += UserControl_DataContextChanged;
        }

        private void SetupEvents()
        {
            if (DataContext is CommissionsViewModel viewModel)
            {
                viewModel.on_request_add_commission_payment_window = (commissions) =>
                {
                    try
                    {
                        var window = new AddCommissionPaymentWindow(viewModel, commissions);
                        window.Owner = Window.GetWindow(this);
                        window.CommissionPaid += (_, _) => viewModel.refresh_data();
                        window.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir Pago de Comision: {ex.Message}", "Error");
                    }
                };

                viewModel.on_request_commission_history_window = (row) =>
                {
                    try
                    {
                        if (PaymentsContext == null) return;
                        var window = new CommissionHistoryWindow(viewModel, PaymentsContext, row);
                        window.Owner = Window.GetWindow(this);
                        window.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir historial: {ex.Message}", "Error");
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

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = (e.OriginalSource as FrameworkElement)?.DataContext as commission_row_dto
                ?? (sender as DataGrid)?.SelectedItem as commission_row_dto;

            if (row == null)
            {
                MessageBox.Show("No se pudo identificar la comision seleccionada.", "Historial");
                return;
            }

            if (DataContext is not CommissionsViewModel vm)
            {
                MessageBox.Show("Contexto de comisiones no disponible.", "Historial");
                return;
            }

            try
            {
                var window = new CommissionHistoryWindow(vm, PaymentsContext, row);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir historial: {ex.Message}", "Error");
            }
        }
    }
}