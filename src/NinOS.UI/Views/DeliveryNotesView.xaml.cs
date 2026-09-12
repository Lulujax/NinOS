using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NinOS.UI.Common;

namespace NinOS.UI.Views
{
    public partial class DeliveryNotesView : UserControl
    {
        public DeliveryNotesView()
        {
            InitializeComponent();
        }

        private void OnDiscountPercentPreview(object sender, TextCompositionEventArgs e)
        {
            InputRestrictions.numbers_only(sender, e);
        }
    }
}