using System;
using System.Linq;
using NinOS.Domain;

namespace NinOS.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void initialize(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));

            db_context.Database.EnsureCreated();

            if (!db_context.sellers.Any())
            {
                seller seller_1 = (seller)Activator.CreateInstance(typeof(seller), nonPublic: true);
                typeof(seller).GetProperty("full_name")?.SetValue(seller_1, "Sandra");
                typeof(seller).GetProperty("seller_code")?.SetValue(seller_1, "S-001");

                seller seller_2 = (seller)Activator.CreateInstance(typeof(seller), nonPublic: true);
                typeof(seller).GetProperty("full_name")?.SetValue(seller_2, "Anais");
                typeof(seller).GetProperty("seller_code")?.SetValue(seller_2, "A-001");

                seller seller_3 = (seller)Activator.CreateInstance(typeof(seller), nonPublic: true);
                typeof(seller).GetProperty("full_name")?.SetValue(seller_3, "Alejandra");
                typeof(seller).GetProperty("seller_code")?.SetValue(seller_3, "A-002");

                db_context.sellers.AddRange(seller_1, seller_2, seller_3);
            }

            if (!db_context.customers.Any())
            {
                customer customer_1 = (customer)Activator.CreateInstance(typeof(customer), nonPublic: true);
                typeof(customer).GetProperty("customer_code")?.SetValue(customer_1, "C-001");
                typeof(customer).GetProperty("full_name")?.SetValue(customer_1, "Carlos Perez");
                typeof(customer).GetProperty("phone_number")?.SetValue(customer_1, "0414-1234567");
                typeof(customer).GetProperty("address")?.SetValue(customer_1, "Valencia");

                customer customer_2 = (customer)Activator.CreateInstance(typeof(customer), nonPublic: true);
                typeof(customer).GetProperty("customer_code")?.SetValue(customer_2, "C-002");
                typeof(customer).GetProperty("full_name")?.SetValue(customer_2, "Maria Gomez");
                typeof(customer).GetProperty("phone_number")?.SetValue(customer_2, "0412-7654321");
                typeof(customer).GetProperty("address")?.SetValue(customer_2, "Naguanagua");

                db_context.customers.AddRange(customer_1, customer_2);
            }

            if (!db_context.products.Any())
            {
                product product_1 = (product)Activator.CreateInstance(typeof(product), nonPublic: true);
                typeof(product).GetProperty("product_code")?.SetValue(product_1, "CH-A");
                typeof(product).GetProperty("name")?.SetValue(product_1, "Cadena con Inicial A");
                typeof(product).GetProperty("category")?.SetValue(product_1, "cadenas_con_iniciales");
                typeof(product).GetProperty("unit_price_usd")?.SetValue(product_1, 15.50m);
                typeof(product).GetProperty("stock_quantity")?.SetValue(product_1, 20);

                product product_2 = (product)Activator.CreateInstance(typeof(product), nonPublic: true);
                typeof(product).GetProperty("product_code")?.SetValue(product_2, "CH-B");
                typeof(product).GetProperty("name")?.SetValue(product_2, "Cadena con Inicial B");
                typeof(product).GetProperty("category")?.SetValue(product_2, "cadenas_con_iniciales");
                typeof(product).GetProperty("unit_price_usd")?.SetValue(product_2, 15.50m);
                typeof(product).GetProperty("stock_quantity")?.SetValue(product_2, 15);

                product product_3 = (product)Activator.CreateInstance(typeof(product), nonPublic: true);
                typeof(product).GetProperty("product_code")?.SetValue(product_3, "CH-C");
                typeof(product).GetProperty("name")?.SetValue(product_3, "Cadena con Inicial C");
                typeof(product).GetProperty("category")?.SetValue(product_3, "cadenas_con_iniciales");
                typeof(product).GetProperty("unit_price_usd")?.SetValue(product_3, 15.50m);
                typeof(product).GetProperty("stock_quantity")?.SetValue(product_3, 10);

                product product_4 = (product)Activator.CreateInstance(typeof(product), nonPublic: true);
                typeof(product).GetProperty("product_code")?.SetValue(product_4, "CH-M");
                typeof(product).GetProperty("name")?.SetValue(product_4, "Cadena con Inicial M");
                typeof(product).GetProperty("category")?.SetValue(product_4, "cadenas_con_iniciales");
                typeof(product).GetProperty("unit_price_usd")?.SetValue(product_4, 15.50m);
                typeof(product).GetProperty("stock_quantity")?.SetValue(product_4, 25);

                product product_5 = (product)Activator.CreateInstance(typeof(product), nonPublic: true);
                typeof(product).GetProperty("product_code")?.SetValue(product_5, "CH-S");
                typeof(product).GetProperty("name")?.SetValue(product_5, "Cadena con Inicial S");
                typeof(product).GetProperty("category")?.SetValue(product_5, "cadenas_con_iniciales");
                typeof(product).GetProperty("unit_price_usd")?.SetValue(product_5, 15.50m);
                typeof(product).GetProperty("stock_quantity")?.SetValue(product_5, 30);

                db_context.products.AddRange(product_1, product_2, product_3, product_4, product_5);
            }

            db_context.SaveChanges();
        }
    }
}