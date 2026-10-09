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
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is Common.ViewModels.DeliveryNotesViewModel oldVm)
            {
                oldVm.RequestRevertSellerSelection = null;
                oldVm.RequestRevertNoteTypeSelection = null;
            }
            if (e.NewValue is Common.ViewModels.DeliveryNotesViewModel newVm)
            {
                newVm.RequestRevertSellerSelection = () =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        SellerComboBox.GetBindingExpression(ComboBox.SelectedItemProperty)?.UpdateTarget();
                    }, System.Windows.Threading.DispatcherPriority.Loaded);
                };
                newVm.RequestRevertNoteTypeSelection = () =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        NoteTypeComboBox.GetBindingExpression(ComboBox.SelectedItemProperty)?.UpdateTarget();
                    }, System.Windows.Threading.DispatcherPriority.Loaded);
                };
            }
        }

        private void OnDiscountPercentPreview(object sender, TextCompositionEventArgs e)
        {
            InputRestrictions.numbers_only(sender, e);
        }

        /// <summary>
        /// El campo de credito solo admite digitos: ni letras ni signos. El plazo se
        /// escribe en dias (21) y la fecha de vencimiento se calcula sola.
        /// </summary>
        private void OnCreditDaysPreview(object sender, TextCompositionEventArgs e)
        {
            InputRestrictions.numbers_only(sender, e);
        }
    }
}