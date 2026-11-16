using System;

namespace NinOS.Domain
{
    public enum update_status
    {
        al_dia,
        recomendada,
        obligatoria
    }

    public static class UpdatePolicyEvaluator
    {
        public static Version parse_version(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("La version no puede estar vacia.", nameof(text));

            string clean = text.Trim();
            int metadata_index = clean.IndexOfAny(new[] { '+', '-' });
            if (metadata_index >= 0) clean = clean.Substring(0, metadata_index);

            if (!Version.TryParse(clean, out Version? parsed) || parsed == null)
                throw new ArgumentException($"La version '{text}' no tiene un formato valido (por ejemplo 1.0.10).", nameof(text));

            return new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
        }

        public static bool has_installed_version(string? current_version)
        {
            return !string.IsNullOrWhiteSpace(current_version);
        }

        public static update_status evaluate(string? current_version, string? minimum_version, string? recommended_version)
        {
            Version current = parse_version(current_version);
            Version minimum = parse_version(minimum_version);
            Version recommended = parse_version(recommended_version);

            update_status result;
            if (current < minimum) result = update_status.obligatoria;
            else if (current < recommended) result = update_status.recomendada;
            else result = update_status.al_dia;

            if (result == update_status.al_dia && (current < minimum || current < recommended))
                throw new InvalidOperationException("El estado al dia no puede darse con una version menor a la minima o la recomendada.");

            return result;
        }
    }
}
