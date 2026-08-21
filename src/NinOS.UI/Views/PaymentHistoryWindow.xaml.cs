using System.Collections.Generic;
using System.Windows;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PaymentHistoryWindow : Window
    {
        public PaymentHistoryWindow(IEnumerable<payment_dto> payments)
        {
            InitializeComponent();
            foreach (var p in payments)
                HistoryGrid.Items.Add(p);
        }
    }
}
