using System.Linq;
using System.Windows;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Reemplaza Message box del sistema por un dialogo con el diseño de la aplicacion.
    /// Mantiene la misma firma que System.Windows.MessageBox.
    /// </summary>
    public static class AppDialog
    {
        public static MessageBoxResult Show(string message)
        {
            return Show(message, string.Empty, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string message, string title)
        {
            return Show(message, title, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons)
        {
            return Show(message, title, buttons, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            return Show(message, title, buttons, image, MessageBoxResult.None, null, null);
        }

        /// <summary>
        /// Igual que Show pero permite poner el texto de los botones. Se usa donde la accion
        /// importa: en el arranque el boton dice "Reintentar" y no "OK", porque la app todavia
        /// no abrio ninguna ventana a la que un OK le sirva.
        /// </summary>
        public static MessageBoxResult Show(
            string message,
            string title,
            MessageBoxButton buttons,
            MessageBoxImage image,
            MessageBoxResult default_result,
            string? yes_text,
            string? no_text)
        {
            Window? owner = null;
            if (Application.Current != null && Application.Current.Windows.Count > 0)
            {
                owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                        ?? Application.Current.MainWindow;
            }

            var dialog = new Views.AppDialogWindow(title, message, buttons, image, owner, default_result, yes_text, no_text);
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}