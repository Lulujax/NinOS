using System;
using System.Collections.Generic;

namespace NinOS.Domain.CargaMasiva
{
    public sealed class LecturaPlantilla
    {
        public LecturaPlantilla(CabeceraPlantilla cabecera, IReadOnlyList<FilaPlantilla> filas)
        {
            this.cabecera = cabecera ?? throw new ArgumentNullException(nameof(cabecera));
            this.filas = filas ?? throw new ArgumentNullException(nameof(filas));
        }

        public CabeceraPlantilla cabecera { get; }
        public IReadOnlyList<FilaPlantilla> filas { get; }
    }
}
