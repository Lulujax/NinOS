namespace NinOS.Domain
{
    public static class relacion_pagada_aviso
    {
        public static bool debe_pedir_confirmacion(resumen_relacion resumen, bool confirmada)
            => resumen.notas_vigentes > 0 && resumen.esta_pagada && !confirmada;
    }
}
