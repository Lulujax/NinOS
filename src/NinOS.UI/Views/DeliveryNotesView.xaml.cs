using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class DeliveryNotesView : UserControl
    {
        private DeliveryNotesViewModel? _vm;

        public DeliveryNotesView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => HookViewModel(e.NewValue as DeliveryNotesViewModel);
        }

        private void HookViewModel(DeliveryNotesViewModel? vm)
        {
            if (_vm != null)
            {
                _vm.sellers.CollectionChanged -= Sellers_CollectionChanged;
            }

            _vm = vm;
            SellerTabControl.Items.Clear();

            if (_vm != null)
            {
                foreach (seller s in _vm.sellers) AddTab(s);
                _vm.sellers.CollectionChanged += Sellers_CollectionChanged;

                if (_vm.selected_seller == null && SellerTabControl.Items.Count > 0)
                {
                    SellerTabControl.SelectedIndex = 0;
                }
            }
        }

        private void Sellers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SellerTabControl.Items.Clear();
                return;
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is seller s) AddTab(s);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is seller s) RemoveTab(s);
                }
            }

            if (_vm != null && _vm.selected_seller == null && SellerTabControl.Items.Count > 0)
            {
                SellerTabControl.SelectedIndex = 0;
            }
        }

        private void AddTab(seller s)
        {
            SellerTabControl.Items.Add(new TabItem { Header = s.full_name, Tag = s });
        }

        private void RemoveTab(seller s)
        {
            for (int i = SellerTabControl.Items.Count - 1; i >= 0; i--)
            {
                if ((SellerTabControl.Items[i] as TabItem)?.Tag == s)
                {
                    SellerTabControl.Items.RemoveAt(i);
                }
            }
        }

        private void SellerTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm == null) return;
            if (SellerTabControl.SelectedItem is TabItem ti && ti.Tag is seller s)
            {
                _vm.selected_seller = s;
            }
        }

        private void OnDiscountPercentPreview(object sender, TextCompositionEventArgs e)
        {
            InputRestrictions.numbers_only(sender, e);
        }
    }
}
