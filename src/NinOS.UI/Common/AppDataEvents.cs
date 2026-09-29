using System;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Permite que ventanas auxiliares (por ejemplo el Panel Administrador) avisen
    /// a los ViewModels ya cargados que los catalogos cambiaron, para que recarguen
    /// sin necesidad de reiniciar la aplicacion.
    /// </summary>
    public static class AppDataEvents
    {
        /// <summary>Clientes o productos fueron eliminados, restaurados o purgados.</summary>
        public static event Action? CatalogsChanged;

        public static void raise_catalogs_changed()
        {
            try
            {
                CatalogsChanged?.Invoke();
            }
            catch (Exception)
            {
                // un refresco fallido nunca debe cerrar la aplicacion
            }
        }
    }
}
