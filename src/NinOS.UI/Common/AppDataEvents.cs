using System;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    public static class AppDataEvents
    {
        public static event Action? DataChanged;

        public static event Action? CatalogsChanged;

        public static void raise_data_changed()
        {
            AppLog.Info("Datos cambiados: recargando pantallas.");
            try
            {
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                AppLog.Error("Fallo el refresco de pantallas tras un cambio de datos.", ex);
            }
        }

        public static void raise_catalogs_changed()
        {
            try
            {
                CatalogsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                AppLog.Error("Fallo el refresco de catalogos.", ex);
            }

            raise_data_changed();
        }

        public static void raise_payments_changed() => raise_data_changed();

        public static void raise_receivables_changed() => raise_data_changed();
    }
}
