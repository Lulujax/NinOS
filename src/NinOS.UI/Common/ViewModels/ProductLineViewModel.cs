using System;

namespace NinOS.UI.Common.ViewModels
{
    public class ProductLineViewModel
    {
        public int IdProductLine { get; set; }
        public string CodePrefix { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string EstadoDisplay => IsActive ? "Activa" : "Oculta";
        public string ToggleVisibilityText => IsActive ? "Ocultar" : "Mostrar";
        public DateTime? DeletedAt { get; set; }
        public string? DeletedReason { get; set; }
        public string DeletedAtDisplay => DeletedAt?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;
        public bool HasProducts { get; set; }
    }
}
