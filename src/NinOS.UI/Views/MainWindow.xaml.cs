using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl) return;
            if (_viewModel == null) return;

            int tab_index = MainTabControl.SelectedIndex;

            switch (tab_index)
            {
                case 0:
                    _viewModel.delivery_notes_vm?.refresh_data();
                    break;
                case 1:
                    _viewModel.accounts_receivable_vm?.refresh_data();
                    break;
                case 2:
                    _viewModel.sales_vm?.refresh_data();
                    break;
                case 3:
                    _viewModel.payments_vm?.refresh_data();
                    break;
                case 4:
                    _viewModel.commissions_vm?.refresh_data();
                    break;
                case 6:
                    _viewModel.inventory_vm?.refresh_data();
                    break;
            }
        }
    }
}
