using System.Windows.Controls;
using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class CustomerView : UserControl
    {
        public CustomerView()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (DataContext is CustomerViewModel view_model)
                {
                    view_model.on_request_add_customer_window = () =>
                    {
                        AddCustomerWindow window = new AddCustomerWindow();
                        window.DataContext = view_model;
                        window.Owner = Application.Current.MainWindow;
                        window.ShowDialog();
                    };

                    view_model.on_close_add_customer_window = () =>
                    {
                        foreach (Window open_window in Application.Current.Windows)
                        {
                            if (open_window is AddCustomerWindow)
                            {
                                open_window.Close();
                                break;
                            }
                        }
                    };
                }
            };

        }
    }
}