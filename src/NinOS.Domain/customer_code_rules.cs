namespace NinOS.Domain
{
    /// <summary>
    /// Formato del codigo de cliente: correlativo global de 5 digitos ("00001").
    /// Vive aparte para que el diagnostico del arranque y las pruebas usen la misma regla.
    /// </summary>
    public static class customer_code_rules
    {
        public const int CodeDigits = 5;

        public static bool es_valido(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            string limpio = code.Trim();
            if (limpio.Length != CodeDigits) return false;

            foreach (char caracter in limpio)
            {
                if (!char.IsDigit(caracter)) return false;
            }

            return true;
        }
    }
}
