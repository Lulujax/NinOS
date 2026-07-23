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
                seller seller_1 = new seller("Sandra", "S-001");
                seller seller_2 = new seller("Anais", "A-001");
                seller seller_3 = new seller("Alejandra", "A-002");

                db_context.sellers.AddRange(seller_1, seller_2, seller_3);
            }

            if (!db_context.customers.Any())
            {
                customer customer_1 = new customer("C-001", "Carlos Perez", "0414-1234567", "Valencia");
                customer customer_2 = new customer("C-002", "Maria Gomez", "0412-7654321", "Naguanagua");

                db_context.customers.AddRange(customer_1, customer_2);
            }

            if (!db_context.products.Any())
            {
                // 3 Productos Defile (Cuidado Capilar)
                product product_1 = new product("DEF-001", "Defile Shampoo Protector", "Defile", 15.00m, 20);
                product product_2 = new product("DEF-002", "Defile Mascarilla Capilar", "Defile", 18.50m, 15);
                product product_3 = new product("DEF-003", "Defile Tratamiento Intensivo", "Defile", 22.00m, 10);

                // 3 Productos Óleos
                product product_4 = new product("OLE-001", "Óleo de Argán Puro", "Óleos", 25.00m, 12);
                product product_5 = new product("OLE-002", "Óleo de Coco Hidratante", "Óleos", 20.00m, 25);
                product product_6 = new product("OLE-003", "Óleo Reparador Puntas", "Óleos", 28.00m, 8);

                // 3 Productos Rembrandt
                product product_7 = new product("REM-001", "Rembrandt Pasta 50ml", "Rembrandt", 30.00m, 30);
                product product_8 = new product("REM-002", "Rembrandt Pasta 100ml", "Rembrandt", 50.00m, 15);
                product product_9 = new product("REM-003", "Rembrandt Kit Blanqueador", "Rembrandt", 45.00m, 10);

                // 3 Productos Otros (Solicitados explícitamente)
                product product_10 = new product("OTR-001", "Resma de Hojas Carta", "Otros", 5.00m, 50);
                product product_11 = new product("OTR-002", "Cinta de Embalaje Transparente", "Otros", 2.00m, 100);
                product product_12 = new product("OTR-003", "Canecalon Castaño Oscuro", "Otros", 12.00m, 35);

                db_context.products.AddRange(product_1, product_2, product_3, product_4, product_5, product_6, product_7, product_8, product_9, product_10, product_11, product_12);
            }

            db_context.SaveChanges();
        }
    }
}