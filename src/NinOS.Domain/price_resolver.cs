namespace NinOS.Domain
{
    /// <summary>
    /// Resolucion del precio efectivo de un producto segun la lista elegida.
    /// Regla de herencia: si el producto tiene precio propio en esa lista se usa
    /// ese; si no, se usa el precio original del producto. Con lista null (o una
    /// lista desconocida) siempre aplica el original.
    /// </summary>
    public static class price_resolver
    {
        public static decimal resolve(decimal original_price, System.Collections.Generic.IReadOnlyDictionary<int, decimal>? own_prices, int? id_price_list)
        {
            if (id_price_list.HasValue
                && own_prices != null
                && own_prices.TryGetValue(id_price_list.Value, out decimal precio_propio))
            {
                return precio_propio;
            }
            return original_price;
        }
    }
}
