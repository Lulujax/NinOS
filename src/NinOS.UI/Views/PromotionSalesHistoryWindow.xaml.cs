using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Logging;
using NinOS.UI.Common;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class PromotionSalesHistoryWindow : Window
    {
        private readonly promotion _promotion;
        private readonly List<promotion_sales_history_dto> _history;

        public PromotionSalesHistoryWindow(promotion promotion, IEnumerable<promotion_sales_history_dto> history)
        {
            InitializeComponent();

            _promotion = promotion;

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

            _history = history?.ToList() ?? new List<promotion_sales_history_dto>();
            HistoryGrid.ItemsSource = _history;
        }

        /// <summary>
        /// Respaldo imprimible del historial del combo. Usa el mismo generador que el
        /// historial de un producto, para que los dos PDF se lean igual.
        /// </summary>
        private void PrintPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var model = new InventoryHistoryPdfModel
                {
                    ItemLabel = "PROMOCION",
                    ItemName = _promotion.name ?? string.Empty,
                    ItemCode = _promotion.promotion_code ?? string.Empty,
                    Category = _promotion.category ?? string.Empty,
                    UnitPriceUsd = _promotion.unit_price_usd,
                    MostrarSaldos = false,
                    Rows = _history.Select(ToPdfRow).ToList()
                };

                InventoryHistoryPdfGenerator.generate(model);
            }
            catch (Exception ex)
            {
                AppLog.Error($"Historial de promocion: no se pudo generar el PDF de '{_promotion.promotion_code}'.", ex);
                AppDialog.Show(
                    "No se pudo generar el PDF del historial.\n\n" + ErrorText.Get(ex, "historial de inventario") +
                    "\n\nCierra la ventana e inténtalo otra vez.",
                    "No se pudo generar el PDF");
            }
        }

        private static InventoryHistoryPdfRow ToPdfRow(promotion_sales_history_dto dto)
        {
            return new InventoryHistoryPdfRow
            {
                NoteNumber = dto.note_number ?? string.Empty,
                FechaLocal = AppTimeZone.to_local(dto.creation_date),
                Documento = dto.documento_display,
                Motivo = dto.motivo_display,
                Cliente = dto.customer_name ?? string.Empty,
                Vendedor = dto.seller_name ?? string.Empty,
                Movimiento = dto.movement_type,
                Unidades = dto.units_sold,
                PrecioUsd = dto.unit_price_usd,
                SubtotalUsd = dto.line_subtotal_usd,
                Estado = dto.status ?? string.Empty
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
