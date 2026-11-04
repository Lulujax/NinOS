using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Logging;
using NinOS.UI.Common;

namespace NinOS.UI.Views
{
    public partial class ProductSalesHistoryWindow : Window
    {
        private readonly product _product;
        private readonly List<product_sales_history_dto> _history;

        public ProductSalesHistoryWindow(product product, IEnumerable<product_sales_history_dto> history)
        {
            InitializeComponent();

            _product = product;
            _history = history?.ToList() ?? new List<product_sales_history_dto>();

            int saldo = product.stock_quantity;
            foreach (var item in _history)
            {
                item.subtotal_stock = saldo;
                saldo -= item.signed_units;
            }

            ProductNameText.Text = product.name;
            ProductCodeText.Text = product.product_code;
            ProductCategoryText.Text = product.category;
            ProductPriceText.Text = product.unit_price_usd.ToString("N2");
            ProductStockText.Text = product.stock_quantity.ToString();

            HistoryGrid.ItemsSource = _history;
        }

        private InventoryHistoryPdfModel BuildModel()
        {
            return new InventoryHistoryPdfModel
            {
                ItemLabel = "PRODUCTO",
                ItemName = _product.name,
                ItemCode = _product.product_code,
                Category = _product.category,
                UnitPriceUsd = _product.unit_price_usd,
                CurrentStock = _product.stock_quantity,
                Rows = _history.Select(ToPdfRow).ToList()
            };
        }

        private void PrintPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var model = BuildModel();
                InventoryHistoryPdfGenerator.generate(model);
            }
            catch (Exception ex)
            {
                AppLog.Error($"Historial de producto: no se pudo generar el PDF de '{_product.product_code}'.", ex);
                AppDialog.Show(
                    "No se pudo generar el PDF del historial.\n\n" + ErrorText.Get(ex, "historial de inventario") +
                    "\n\nCierra la ventana e inténtalo otra vez.",
                    "No se pudo generar el PDF");
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var model = BuildModel();
                var doc = InventoryHistoryPdfGenerator.build_document(model);
                string fileName = InventoryHistoryPdfGenerator.get_default_file_name(model);
                var preview = new PdfPreviewWindow(
                    doc,
                    $"Historial - {model.ItemCode}",
                    fileName,
                    () => InventoryHistoryExcelExporter.export(model));
                preview.Owner = this;
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                AppLog.Error($"Historial de producto: error en vista previa de '{_product.product_code}'.", ex);
                AppDialog.Show("No se pudo abrir la vista previa: " + ErrorText.Get(ex), "Error");
            }
        }

        private void DirectPrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var model = BuildModel();
                var doc = InventoryHistoryPdfGenerator.build_document(model);
                string fileName = InventoryHistoryPdfGenerator.get_default_file_name(model);
                ReportPrintService.DirectPrint(doc, this, $"Historial - {model.ItemCode}", fileName);
            }
            catch (Exception ex)
            {
                AppLog.Error($"Historial de producto: error al imprimir '{_product.product_code}'.", ex);
                AppDialog.Show("No se pudo imprimir: " + ErrorText.Get(ex), "Error");
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var model = BuildModel();
                InventoryHistoryExcelExporter.export(model);
            }
            catch (Exception ex)
            {
                AppLog.Error($"Historial de producto: error al exportar a Excel '{_product.product_code}'.", ex);
                AppDialog.Show("No se pudo exportar a Excel: " + ErrorText.Get(ex), "Error");
            }
        }

        private static InventoryHistoryPdfRow ToPdfRow(product_sales_history_dto dto)
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
