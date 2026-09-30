using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PromotionSalesHistoryWindow : Window
    {
        public PromotionSalesHistoryWindow(promotion promotion, IEnumerable<promotion_sales_history_dto> history)
        {
            InitializeComponent();

            PromoNameText.Text = promotion.name ?? string.Empty;
            PromoCodeText.Text = promotion.promotion_code ?? string.Empty;
            PromoCategoryText.Text = promotion.category ?? string.Empty;
            PromoPriceText.Text = promotion.unit_price_usd.ToString("N2");

            var componentes = promotion.items?
                .Where(i => i != null && i.product != null && i.quantity_required > 0)
                .ToList() ?? new List<promotion_item>();

            bool es_oferta_individual = componentes.Count == 1 && componentes[0].quantity_required == 1;
            PromoTypeText.Text = InventoryViewModel.promo_type_name(
                InventoryViewModel.promo_type_from_code(promotion.promotion_code, componentes.Count, es_oferta_individual));

            // Muestra de que esta compuesta la promocion.
            var lineas = componentes
                .Select(i =>
                {
                    string codigo = string.IsNullOrWhiteSpace(i.product!.product_code) ? "" : i.product.product_code!;
                    string nombre = string.IsNullOrWhiteSpace(i.product.name) ? codigo : i.product.name!;
                    return i.quantity_required + " x  " + nombre.Trim() + (string.IsNullOrWhiteSpace(codigo) ? "" : "   (" + codigo + ")");
                })
                .ToList();

            if (lineas.Count == 0)
            {
                CompositionEmptyText.Visibility = Visibility.Visible;
            }
            else
            {
                CompositionItems.ItemsSource = lineas;
            }

            var list = history?.ToList() ?? new List<promotion_sales_history_dto>();
            HistoryGrid.ItemsSource = list;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
