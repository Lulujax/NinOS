using System;
using System.Windows;
using System.Windows.Input;

namespace NinOS.UI.Common
{
    public static class WindowSizingHelper
    {
        /// <summary>
        /// Ajusta una ventana para que nunca sobrepase el área de trabajo visible del monitor activo (SystemParameters.WorkArea),
        /// impidiendo que la barra de título, los botones de acción o el botón de cerrar queden fuera de la pantalla.
        /// Además agrega soporte universal para la tecla Escape para cerrar la ventana.
        /// </summary>
        public static void FitToScreen(
            Window window,
            double maxWorkAreaWidthRatio = 0.95,
            double maxWorkAreaHeightRatio = 0.94,
            bool startMaximizedIfScreenSmall = false)
        {
            if (window == null) return;

            // 1. Tecla Escape como salida de emergencia universal
            window.PreviewKeyDown += (sender, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    try
                    {
                        window.Close();
                    }
                    catch
                    {
                        // En caso de que ya esté cerrándose
                    }
                }
            };

            // 2. Establecer restricciones estrictas de MaxHeight y MaxWidth
            var workArea = SystemParameters.WorkArea;
            window.MaxHeight = workArea.Height;
            window.MaxWidth = workArea.Width;

            // Si es pantalla chica (alto <= 768px) y se pide maximizar (ej. vistas previas)
            if (startMaximizedIfScreenSmall && workArea.Height <= 768)
            {
                window.WindowState = WindowState.Maximized;
            }
            else
            {
                ApplyClamping(window, maxWorkAreaWidthRatio, maxWorkAreaHeightRatio);
            }

            // Aplicar clamping nuevamente en SourceInitialized / Loaded para cuando se calculen las dimensiones reales
            window.SourceInitialized += (s, e) =>
            {
                var currentWorkArea = SystemParameters.WorkArea;
                window.MaxHeight = currentWorkArea.Height;
                window.MaxWidth = currentWorkArea.Width;

                if (!startMaximizedIfScreenSmall || currentWorkArea.Height > 768)
                {
                    ApplyClamping(window, maxWorkAreaWidthRatio, maxWorkAreaHeightRatio);
                }
            };
        }

        private static void ApplyClamping(Window window, double maxWidthRatio, double maxHeightRatio)
        {
            var workArea = SystemParameters.WorkArea;
            double maxAllowedH = Math.Floor(workArea.Height * maxHeightRatio);
            double maxAllowedW = Math.Floor(workArea.Width * maxWidthRatio);

            if (window.Height > maxAllowedH || double.IsNaN(window.Height))
                window.Height = maxAllowedH;

            if (window.Width > maxAllowedW || double.IsNaN(window.Width))
                window.Width = maxAllowedW;

            // Asegurar que la coordenada Top no sea negativa ni sobresalga
            if (window.Top < workArea.Top)
                window.Top = workArea.Top;

            if (window.Top + window.Height > workArea.Bottom)
                window.Top = Math.Max(workArea.Top, workArea.Bottom - window.Height);

            if (window.Left < workArea.Left)
                window.Left = workArea.Left;

            if (window.Left + window.Width > workArea.Right)
                window.Left = Math.Max(workArea.Left, workArea.Right - window.Width);
        }
    }
}
