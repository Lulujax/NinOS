using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NinOS.Domain;
using NinOS.Infrastructure.Logging;
using NinOS.Infrastructure.Services.Interfaces;

namespace NinOS.UI.Common
{
    public class export_result
    {
        public int rows { get; set; }
        public string file_path { get; set; } = string.Empty;
    }

    public static class DatabaseExporter
    {
        private static string escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\"", "'")
                        .Replace(";", ",")
                        .Replace("\r", " ")
                        .Replace("\n", " ");
        }

        private static string money(decimal value) => value.ToString("0.00");

        public static async Task<export_result> export_customers_csv_async(
            ICustomerService customer_service, string file_path)
        {
            List<customer> customers = new(await customer_service.GetAllCustomersAsync());

            var lines = new List<string>
            {
                "Codigo;Razon Social;Contacto;RIF;Telefono;Direccion Fiscal;Direccion Entrega;Vendedor"
            };

            foreach (customer c in customers)
            {
                lines.Add(string.Join(";", new[]
                {
                    escape(c.customer_code),
                    escape(c.business_name),
                    escape(c.contact_name),
                    escape(c.rif),
                    escape(c.phone_number),
                    escape(c.fiscal_address),
                    escape(c.delivery_address),
                    escape(c.seller_name)
                }));
            }

            await File.WriteAllTextAsync(file_path, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
            AppLog.Info($"Exportacion de clientes: {file_path} ({customers.Count} filas)");

            return new export_result { rows = customers.Count, file_path = file_path };
        }

        public static async Task<export_result> export_products_csv_async(
            IInventoryService inventory_service, string file_path)
        {
            List<product> products = new(await inventory_service.get_all_products_async());

            var lines = new List<string>
            {
                "Codigo;Nombre;Categoria;Precio USD;Stock;Activo"
            };

            foreach (product p in products)
            {
                lines.Add(string.Join(";", new[]
                {
                    escape(p.product_code),
                    escape(p.name),
                    escape(p.category),
                    money(p.unit_price_usd),
                    p.stock_quantity.ToString(),
                    p.is_active ? "Si" : "No"
                }));
            }

            await File.WriteAllTextAsync(file_path, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
            AppLog.Info($"Exportacion de productos: {file_path} ({products.Count} filas)");

            return new export_result { rows = products.Count, file_path = file_path };
        }
    }
}
