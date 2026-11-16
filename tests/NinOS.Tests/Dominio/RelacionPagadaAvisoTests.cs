using NinOS.Domain;

namespace NinOS.Tests.Dominio;

public class RelacionPagadaAvisoTests
{
    [Fact]
    public void feliz_relacion_abierta_no_pide_confirmacion()
    {
        var pendiente = new resumen_relacion(100m, 20m, 80m, "Pendiente", 2);

        Assert.False(relacion_pagada_aviso.debe_pedir_confirmacion(pendiente, false));
    }

    [Fact]
    public void limite_relacion_sin_notas_no_pide_confirmacion_aunque_figure_pagada()
    {
        var vacia = new resumen_relacion(0m, 0m, 0m, "Pagada", 0);

        Assert.False(relacion_pagada_aviso.debe_pedir_confirmacion(vacia, false));
    }

    [Fact]
    public void limite_relacion_pagada_ya_confirmada_no_vuelve_a_pedir()
    {
        var pagada = new resumen_relacion(100m, 100m, 0m, "Pagada", 1);

        Assert.False(relacion_pagada_aviso.debe_pedir_confirmacion(pagada, true));
    }

    [Fact]
    public void error_relacion_pagada_sin_confirmar_pide_confirmacion()
    {
        var pagada = new resumen_relacion(100m, 100m, 0m, "Pagada", 1);

        Assert.True(relacion_pagada_aviso.debe_pedir_confirmacion(pagada, false));
    }
}
