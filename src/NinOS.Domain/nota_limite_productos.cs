using System;

namespace NinOS.Domain
{
    public static class nota_limite_productos
    {
        public const int MAXIMO_PRODUCTOS = 20;

        public static void validar(int cantidad_renglones)
        {
            if (cantidad_renglones < 1)
                throw new InvalidOperationException("La nota no tiene productos.");
            if (cantidad_renglones > MAXIMO_PRODUCTOS)
                throw new InvalidOperationException($"Una nota admite como máximo {MAXIMO_PRODUCTOS} productos.");
        }
    }
}
