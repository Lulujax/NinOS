using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Logging;

namespace NinOS.Infrastructure.Data
{
    /// <summary>
    /// Lo que el arranque encontro en la base. Solo cuenta: no corrige nada.
    ///
    /// Antes el arranque "arreglaba" solo estas cosas (movia clientes de zona, renumeraba
    /// codigos, borraba pagos). Ahora unicamente las reporta, y las correcciones se hacen
    /// con un script supervisado y con respaldo previo.
    /// </summary>
    public sealed class db_diagnostic_report
    {
        public int clientes_sin_zona { get; init; }
        public int clientes_con_codigo_invalido { get; init; }
        public int notas_pro_venta_sin_relacion { get; init; }
        public int tipos_de_nota_obsoletos { get; init; }
        public int pagos_tipo_nota_de_credito { get; init; }
        public int tipos_de_nota_activos { get; init; }
        public int lineas_de_producto_activas { get; init; }
        public bool base_vacia { get; init; }

        /// <summary>
        /// Funcion pura: convierte los conteos en avisos legibles. Devuelve la lista vacia
        /// cuando no hay nada que revisar.
        /// </summary>
        public IReadOnlyList<string> advertencias()
        {
            List<string> avisos = new List<string>();

            if (base_vacia)
            {
                avisos.Add("La base no tiene vendedoras, clientes ni productos. Si es una base nueva, cargue los datos iniciales con --seed-initial-data.");
                return avisos;
            }

            if (clientes_sin_zona > 0)
                avisos.Add($"{clientes_sin_zona} cliente(s) sin zona asignada. El vendedor sale de la zona, asi que esos clientes no se pueden facturar.");

            if (clientes_con_codigo_invalido > 0)
                avisos.Add($"{clientes_con_codigo_invalido} cliente(s) con codigo fuera del formato de {customer_code_rules.CodeDigits} digitos.");

            if (notas_pro_venta_sin_relacion > 0)
                avisos.Add($"{notas_pro_venta_sin_relacion} nota(s) Pro Venta sin relacion semanal asociada.");

            if (tipos_de_nota_obsoletos > 0)
                avisos.Add($"{tipos_de_nota_obsoletos} tipo(s) de nota obsoletos todavia en la base ({string.Join(", ", DbDiagnostics.CodigosDeTipoObsoletos)}).");

            if (pagos_tipo_nota_de_credito > 0)
                avisos.Add($"{pagos_tipo_nota_de_credito} pago(s) de tipo NOTA DE CREDITO. Son restos del diseño anterior y hay que limpiarlos con un script supervisado.");

            if (tipos_de_nota_activos == 0)
                avisos.Add("No hay tipos de nota en la base: no se podra emitir ninguna nota.");

            if (lineas_de_producto_activas == 0)
                avisos.Add("No hay lineas de producto activas: los codigos de producto nuevos no se podran generar.");

            return avisos;
        }

        public bool hay_advertencias => advertencias().Count > 0;
    }

    public static class DbDiagnostics
    {
        /// <summary>Tipos de nota que el sistema dejo de usar.</summary>
        public static readonly string[] CodigosDeTipoObsoletos = { "PRO", "CAN", "HLS" };

        /// <summary>Revisa la base sin modificar nada. Todas las consultas son de solo lectura.</summary>
        public static db_diagnostic_report inspect(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));

            List<int> ids_pro_venta = db_context.note_types
                .AsNoTracking()
                .Where(t => NoteTypeCodes.pro_venta_codes.Contains(t.code))
                .Select(t => t.id_note_type)
                .ToList();

            int notas_pro_venta_sin_relacion = ids_pro_venta.Count == 0
                ? 0
                : db_context.delivery_notes
                    .AsNoTracking()
                    .Count(n => n.note_type_id != null
                             && ids_pro_venta.Contains(n.note_type_id.Value)
                             && n.id_relacion == null);

            // Los codigos se evaluan en memoria para usar la misma regla que las pruebas.
            // Son pocos cientos de filas, asi que no pesa.
            List<string> codigos_de_cliente = db_context.customers
                .AsNoTracking()
                .Select(c => c.customer_code)
                .ToList();

            bool hay_vendedoras = db_context.sellers.AsNoTracking().Any();
            bool hay_clientes = codigos_de_cliente.Count > 0;
            bool hay_productos = db_context.products.AsNoTracking().Any();

            return new db_diagnostic_report
            {
                clientes_sin_zona = db_context.customers.AsNoTracking().Count(c => c.id_zona == null),
                clientes_con_codigo_invalido = codigos_de_cliente.Count(c => !customer_code_rules.es_valido(c)),
                notas_pro_venta_sin_relacion = notas_pro_venta_sin_relacion,
                tipos_de_nota_obsoletos = db_context.note_types
                    .AsNoTracking()
                    .Count(t => CodigosDeTipoObsoletos.Contains(t.code)),
                pagos_tipo_nota_de_credito = db_context.payments
                    .AsNoTracking()
                    .Count(p => p.payment_type == "NOTA DE CREDITO"),
                tipos_de_nota_activos = db_context.note_types.AsNoTracking().Count(t => t.is_active),
                lineas_de_producto_activas = db_context.product_lines.AsNoTracking().Count(l => l.is_active),
                base_vacia = !hay_vendedoras && !hay_clientes && !hay_productos
            };
        }

        public static void log(db_diagnostic_report report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            IReadOnlyList<string> avisos = report.advertencias();

            if (avisos.Count == 0)
            {
                AppLog.Info("Diagnostico de arranque: sin nada que revisar");
                return;
            }

            AppLog.Warn($"Diagnostico de arranque: {avisos.Count} punto(s) que revisar");
            foreach (string aviso in avisos)
            {
                AppLog.Warn($"  - {aviso}");
            }
        }
    }
}
