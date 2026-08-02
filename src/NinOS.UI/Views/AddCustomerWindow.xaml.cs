using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class AddCustomerWindow : Window
    {
        public AddCustomerWindow()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                if (DataContext is CustomerViewModel viewModel)
                {
                    viewModel.OnCloseAddCustomerWindow = () =>
                    {
                        Close();
                    };
                }
            };
        }
    }
}