using System;

namespace NinOS.UI.Common.ViewModels
{
    /// <summary>Fila del grid de la ventana de gestion de listas de precios.</summary>
    public class PriceListRowViewModel
    {
        public int IdPriceList { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string IsActiveDisplay => IsActive ? "Activa" : "Inactiva";
        public string ToggleActiveText => IsActive ? "Desactivar" : "Activar";
    }
}
