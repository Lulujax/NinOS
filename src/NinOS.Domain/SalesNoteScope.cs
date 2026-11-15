using System;

namespace NinOS.Domain
{
    public static class SalesNoteScope
    {
        public static bool is_visible_in_sales(string? note_type_code)
        {
            if (string.IsNullOrWhiteSpace(note_type_code)) return true;
            return !NoteTypeCodes.is_pro_venta(note_type_code.Trim());
        }
    }
}
