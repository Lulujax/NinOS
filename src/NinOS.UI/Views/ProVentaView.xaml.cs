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

            view_model.on_request_relation_pdf = async (row) =>
            {
                await view_model.print_relation_pdf_async(row);
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