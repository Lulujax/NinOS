using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PaymentNoteHistoryWindow : Window
    {
        public PaymentNoteHistoryWindow(IEnumerable<payment_dto> payments)
        {
            InitializeComponent();
            var list = payments.ToList();
            if (list.Any())
            {
                HeaderText.Text = $"Historial de pagos - Nota: {list.First().note_number}";
            }
            foreach (var p in list)
                HistoryGrid.Items.Add(p);
        }
    }
}
