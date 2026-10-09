using System.Collections.Generic;

namespace NinOS.Domain
{
    /// <summary>
    /// Un producto que cambia de codigo al migrar el prefijo de su linea. El correlativo se
    /// conserva: solo cambia el prefijo (DEF30508 -> DEFX30508).
    /// </summary>
    public class product_code_migration_item
    {
        public int id_product { get; set; }
        public string old_code { get; set; } = string.Empty;
        public string new_code { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;

        /// <summary>
        /// El codigo no se pudo reescribir: no sigue el formato del prefijo viejo, o el nuevo
        /// ya lo usa otro producto. Esos productos se dejan como estan.
        /// </summary>
        public bool queda_igual { get; set; }
        public string motivo { get; set; } = string.Empty;
    }

    /// <summary>Lo que se veria tocar antes de confirmar la migracion.</summary>
    public class product_code_migration_preview
    {
        public string old_prefix { get; set; } = string.Empty;
        public string new_prefix { get; set; } = string.Empty;
        public List<product_code_migration_item> items { get; set; } = new();

        public int total_productos => items.Count;
        public int a_reescribir => items.Count(i => !i.queda_igual);
        public int quedan_iguales => items.Count(i => i.queda_igual);

        /// <summary>Lineas de notas y notas de credito con productos de esta linea.</summary>
        public int detalles_de_notas_afectados { get; set; }
        public int detalles_de_notas_de_credito_afectados { get; set; }
        public int ajustes_de_kardex_afectados { get; set; }

        public bool hay_conflicto => quedan_iguales > 0;
    }

    /// <summary>Resultado de la migracion ya aplicada.</summary>
    public class product_code_migration_result
    {
        public bool sucesso { get; set; }
        public string mensaje { get; set; } = string.Empty;
        public int productos_actualizados { get; set; }
        public int snapshots_de_notas_actualizados { get; set; }
        public int snapshots_de_notas_de_credito_actualizados { get; set; }
        public int ajustes_de_kardex_actualizados { get; set; }
    }
}