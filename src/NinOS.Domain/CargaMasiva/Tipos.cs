namespace NinOS.Domain.CargaMasiva
{
    public enum TipoPlantilla
    {
        Cantidad = 1,
        Precio = 2
    }

    public enum TipoCargaCantidad
    {
        Conteo = 1,
        Ajuste = 2
    }

    public enum EstadoFila
    {
        Cambia = 1,
        Igual = 2,
        Aviso = 3,
        Error = 4
    }

    public sealed record ProductoPlantilla(
        int id_product,
        string codigo,
        string marca,
        string descripcion,
        decimal stock_quantity,
        decimal precio_principal,
        decimal precio_lista);

    public sealed record ProductoActual(int id_product, string codigo, bool activo, decimal valor_actual);
}
