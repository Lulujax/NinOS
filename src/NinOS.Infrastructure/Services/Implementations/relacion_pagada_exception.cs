using System;

namespace NinOS.Infrastructure.Services.Implementations
{
    public class relacion_pagada_exception : InvalidOperationException
    {
        public int id_relacion { get; }

        public relacion_pagada_exception(int id_relacion)
            : base($"La relación {id_relacion} ya está pagada. Confirme para agregar la nota y reabrirla.")
        {
            this.id_relacion = id_relacion;
        }
    }
}
