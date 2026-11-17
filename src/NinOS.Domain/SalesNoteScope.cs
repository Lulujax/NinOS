namespace NinOS.Domain
{
    public static class SalesNoteScope
    {
        public static bool is_visible_in_sales(string? note_type_code, business_unit scope)
        {
            return BusinessUnitRules.unit_of_note_type(note_type_code) == scope;
        }
    }
}
