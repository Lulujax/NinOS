using System;

namespace NinOS.Domain
{
    public static class AppVersionText
    {
        public static string format(Version? version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            if (version.Build < 0) throw new ArgumentException("La version no tiene numero de compilacion.", nameof(version));

            return $"NinOS v{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
