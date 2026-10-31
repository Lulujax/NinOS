namespace NinOS.UI.Common.ViewModels
{
    /// <summary>Fila del grid de la ventana Editar Listas.</summary>
    public class PriceListRowViewModel
    {
        public int IdPriceList { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsPrincipal { get; set; }

        /// <summary>La Principal se puede editar, pero nunca eliminar.</summary>
        public bool CanDelete => !IsPrincipal;
    }
}
