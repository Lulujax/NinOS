using System;
using System.Linq;

namespace NinOS.Domain
{
    public static class BusinessUnitRules
    {
        private static readonly string[] general_codes =
        {
            NoteTypeCodes.General,
            NoteTypeCodes.Promocion,
            NoteTypeCodes.Volumen
        };

        private static readonly string[] vesupronta_codes =
        {
            NoteTypeCodes.Vesupronta,
            NoteTypeCodes.PromocionVesupronta,
            NoteTypeCodes.VolumenVesupronta
        };

        public static bool is_known_code(string? note_type_code)
        {
            string normalized_code = (note_type_code ?? string.Empty).Trim();
            if (normalized_code.Length == 0) return false;

            return general_codes.Concat(NoteTypeCodes.pro_venta_codes).Concat(vesupronta_codes)
                .Contains(normalized_code, StringComparer.OrdinalIgnoreCase);
        }

        public static business_unit unit_of_note_type(string? note_type_code)
        {
            string normalized_code = (note_type_code ?? string.Empty).Trim();
            if (normalized_code.Length == 0) return business_unit.General;

            if (vesupronta_codes.Contains(normalized_code, StringComparer.OrdinalIgnoreCase))
            {
                return business_unit.Vesupronta;
            }

            if (NoteTypeCodes.is_pro_venta(normalized_code))
            {
                return business_unit.ProVenta;
            }

            return business_unit.General;
        }

        public static business_unit unit_of_zone(zona zone)
        {
            ArgumentNullException.ThrowIfNull(zone);

            if (zone.is_pro_venta && zone.is_vesupronta)
            {
                throw new InvalidOperationException("Una zona no puede ser Pro Venta y Vesupronta a la vez.");
            }

            if (zone.is_vesupronta) return business_unit.Vesupronta;
            if (zone.is_pro_venta) return business_unit.ProVenta;
            return business_unit.General;
        }

        public static business_unit unit_from_modalidad_index(int modalidad_index)
        {
            return modalidad_index switch
            {
                0 => business_unit.General,
                1 => business_unit.ProVenta,
                2 => business_unit.Vesupronta,
                _ => throw new ArgumentOutOfRangeException(nameof(modalidad_index), modalidad_index, "Modalidad desconocida."),
            };
        }

        public static int modalidad_index_of(business_unit unit)
        {
            return unit switch
            {
                business_unit.General => 0,
                business_unit.ProVenta => 1,
                business_unit.Vesupronta => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Empresa desconocida."),
            };
        }

        public static string label_of(business_unit unit)
        {
            return unit switch
            {
                business_unit.General => "General",
                business_unit.ProVenta => "Pro Venta",
                business_unit.Vesupronta => "Vesupronta",
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Empresa desconocida."),
            };
        }

        public static string[] codes_of_unit(business_unit unit)
        {
            return unit switch
            {
                business_unit.General => (string[])general_codes.Clone(),
                business_unit.ProVenta => (string[])NoteTypeCodes.pro_venta_codes.Clone(),
                business_unit.Vesupronta => (string[])vesupronta_codes.Clone(),
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Empresa desconocida."),
            };
        }
    }
}
