using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NinOS.Domain;

namespace NinOS.UI.Views
{
    public partial class StockSearchWindow : Window
    {
        private List<product> _allProducts;
        public product? SelectedProduct { get; private set; }
        public bool IsConfirmed { get; private set; }

        public StockSearchWindow(IEnumerable<product> products)
        {
            InitializeComponent();
            _allProducts = products.ToList();
            ProductsGrid.ItemsSource = _allProducts;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text?.ToLower() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
            {
                ProductsGrid.ItemsSource = _allProducts;
                return;
            }
            ProductsGrid.ItemsSource = _allProducts.Where(p =>
                (p.product_code ?? string.Empty).ToLower().Contains(query) ||
                (p.name ?? string.Empty).ToLower().Contains(query) ||
                (p.category ?? string.Empty).ToLower().Contains(query)
            ).ToList();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is product p)
            {
                SelectedProduct = p;
                IsConfirmed = true;
                Close();
            }
            else
            {
                MessageBox.Show("Seleccione un producto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }

        private void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Select_Click(sender, e);
        }
    }
}
