using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CustomerView : UserControl
    {
        public CustomerView()
        {
            InitializeComponent();
            DataContextChanged += UserControl_DataContextChanged;
        }

        public CustomerView(CustomerViewModel viewModel) : this()
        {
            DataContext = viewModel;
            SetupEvents();
        }

        private void SetupEvents()
        {
            if (DataContext is CustomerViewModel viewModel)
            {
                viewModel.OnRequestAddCustomerWindow = () =>
                {
                    AddCustomerWindow window = new AddCustomerWindow();
                    window.DataContext = viewModel;
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                };

                viewModel.OnRequestEditCustomerWindow = (CustomerRowDto selected) =>
                {
                    AddCustomerWindow window = new AddCustomerWindow();
                    window.DataContext = viewModel;
                    window.Owner = Window.GetWindow(this);
                    viewModel.StartEditCustomer(selected);
                    window.ShowDialog();
                };
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetupEvents();
        }
    }
}