using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PromotionSalesHistoryWindow : Window
    {
        public PromotionSalesHistoryWindow(promotion promotion, IEnumerable<promotion_sales_history_dto> history)
        {
            InitializeComponent();

            PromoNameText.Text = promotion.name;
            PromoCodeText.Text = promotion.promotion_code;
            PromoCategoryText.Text = promotion.category;
            PromoPriceText.Text = promotion.unit_price_usd.ToString("N2");

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