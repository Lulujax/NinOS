using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Data;
using NinOS.Infrastructure.Logging;

namespace NinOS.Infrastructure.Services
{
    public sealed class note_scope
    {
        public note_scope(business_unit unit, IEnumerable<int> excluded_type_ids)
        {
            this.unit = unit;
            this.excluded_type_ids = excluded_type_ids.Distinct().OrderBy(id => id).ToArray();
            includes_untyped_notes = unit == business_unit.General;
        }

        public business_unit unit { get; }

        public int[] excluded_type_ids { get; }

        public bool includes_untyped_notes { get; }
    }

    public static class NoteScopeFilter
    {
        public static note_scope build_scope(business_unit unit, IEnumerable<(int id_note_type, string? code)> note_types)
        {
            ArgumentNullException.ThrowIfNull(note_types);

            var excluded_type_ids = new List<int>();
            foreach (var (id_note_type, code) in note_types)
            {
                if (!string.IsNullOrWhiteSpace(code) && !BusinessUnitRules.is_known_code(code))
                {
                    AppLog.Warn($"Tipo de nota con codigo no reconocido, id {id_note_type}: cuenta como General y no como Vesupronta.");
                }

                if (BusinessUnitRules.unit_of_note_type(code) != unit)
                {
                    excluded_type_ids.Add(id_note_type);
                }
            }

            return new note_scope(unit, excluded_type_ids);
        }

        public static async Task<note_scope> load_scope_async(NinOSDbContext db_context, business_unit unit)
        {
            ArgumentNullException.ThrowIfNull(db_context);

            var note_types = await db_context.note_types
                .AsNoTracking()
                .Select(t => new { t.id_note_type, t.code })
                .ToListAsync();

            AppLog.Info($"Alcance de notas cargado: empresa {unit}, {note_types.Count} tipos de nota.");
            return build_scope(unit, note_types.Select(t => (t.id_note_type, t.code)));
        }

        public static bool belongs_to_scope(note_scope scope, int? note_type_id)
        {
            ArgumentNullException.ThrowIfNull(scope);

            if (note_type_id == null) return scope.includes_untyped_notes;
            return !scope.excluded_type_ids.Contains(note_type_id.Value);
        }

        public static Expression<Func<delivery_note, bool>> delivery_notes_in_scope(note_scope scope)
        {
            ArgumentNullException.ThrowIfNull(scope);

            int[] excluded_type_ids = scope.excluded_type_ids;
            bool includes_untyped_notes = scope.includes_untyped_notes;

            return note => (note.note_type_id == null && includes_untyped_notes)
                || (note.note_type_id != null && !excluded_type_ids.Contains(note.note_type_id.Value));
        }
    }
}
