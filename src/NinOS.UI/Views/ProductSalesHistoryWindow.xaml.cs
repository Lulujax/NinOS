using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Views
{
    public partial class ProductSalesHistoryWindow : Window
    {
        public ProductSalesHistoryWindow(product product, IEnumerable<product_sales_history_dto> history)
        {
            InitializeComponent();

            ProductNameText.Text = product.name;
            ProductCodeText.Text = product.product_code;
            ProductCategoryText.Text = product.category;
            ProductPriceText.Text = product.unit_price_usd.ToString("N2");
            ProductStockText.Text = product.stock_quantity.ToString();

            var list = history.ToList();
            HistoryGrid.ItemsSource = list;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
