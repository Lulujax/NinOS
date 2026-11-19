using System;

namespace NinOS.Domain.CargaMasiva
{
    public sealed class CargaCambiadaException : InvalidOperationException
    {
        public CargaCambiadaException(string mensaje)
            : base(mensaje)
        {
        }
    }
}
