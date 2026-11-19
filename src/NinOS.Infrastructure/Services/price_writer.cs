using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;

namespace NinOS.Infrastructure.Services
{
    public static class price_writer
    {
        public static async Task guardar_precio_async(NinOSDbContext db_context, int id_product, int id_price_list, decimal precio)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));
            if (id_product <= 0) throw new ArgumentException("El producto del precio es inválido.", nameof(id_product));
            if (id_price_list <= 0) throw new ArgumentException("La lista del precio es inválida.", nameof(id_price_list));

            decimal precio_redondeado = Money.round(precio);
            if (precio_redondeado <= 0) throw new ArgumentException("El precio debe ser mayor que cero.", nameof(precio));

            product_price? fila = await db_context.product_prices
                .FirstOrDefaultAsync(pp => pp.id_product == id_product && pp.id_price_list == id_price_list);

            if (fila == null)
            {
                db_context.product_prices.Add(new product_price
                {
                    id_product = id_product,
                    id_price_list = id_price_list,
                    unit_price_usd = precio_redondeado
                });
            }
            else
            {
                fila.unit_price_usd = precio_redondeado;
            }
        }
    }
}
