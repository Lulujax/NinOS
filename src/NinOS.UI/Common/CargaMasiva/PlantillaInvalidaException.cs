using System;

namespace NinOS.UI.Common.CargaMasiva
{
    public sealed class PlantillaInvalidaException : InvalidOperationException
    {
        public PlantillaInvalidaException(string mensaje)
            : base(mensaje)
        {
        }
    }
}
