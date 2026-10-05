using System.Collections.ObjectModel;

namespace NinOS.UI.Common.ViewModels
{
    public class SellerTabItem<T> : ViewModelBase
    {
        public int? IdSeller { get; set; }
        public string Header { get; set; } = string.Empty;
        public ObservableCollection<T> Items { get; } = new ObservableCollection<T>();

        public SellerTabItem() { }

        public SellerTabItem(int? idSeller, string header)
        {
            IdSeller = idSeller;
            Header = header;
        }
    }
}

