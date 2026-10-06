using System;

namespace NinOS.UI.Common.ViewModels
{
    public class ZonaViewModel
    {
        public int IdZona { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsProVenta { get; set; }
        public string TipoOperacionDisplay => IsProVenta ? "Pro Venta" : "General";
        public DateTime? DeletedAt { get; set; }
        public string? DeletedReason { get; set; }
        public string DeletedAtDisplay => DeletedAt?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;
    }
}
