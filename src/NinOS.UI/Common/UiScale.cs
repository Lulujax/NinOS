using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Adapta el tamano de la interfaz al de cada pantalla, sin que el usuario toque nada.
    ///
    /// ESTADO ACTUAL: la compensacion esta ACTIVA (DESIGN_DPI = 144). La app fue diseñada
    /// para verse bien a 150% de Windows, y la mayoria de la gente no cambia la escala, asi
    /// que se queda en 100% y la veia diminuta. Para eso se agranda lo que se ve chico:
    ///
    ///     Windows al 100%  ->  se dibuja al 150%  (se ve igual que a 150%)
    ///     Windows al 125%  ->  se dibuja al 120%
    ///     Windows al 150%  ->  se dibuja al 100%  (sin cambios, ya se ve bien)
    ///
    /// Lo que NO hace, y es la regla que no se negocia: nunca achica. Si el usuario ya subio
    /// la escala de Windows, el texto que ve es el que pidio y la app no se lo reduce:
    ///
    ///     Windows al 200%  ->  se dibuja al 100%  (ve su 200% entero, no recortado)
    ///     Windows al 300%  ->  se dibuja al 100%  (ve su 300% entero)
    ///
    /// Esa distincion importa: sin el tope, (144/dpi) * (dpi/96) da 1.5 exacto para
    /// cualquier escala, y un usuario al 200% recibia 1.5x en vez de 2x. Un techo de 1.5x
    /// para siempre, del que no se puede escapar subiendo Windows. Ver compute_factor.
    ///
    /// Lo que queda igual desde siempre: no se tocan el registro ni la escala del sistema,
    /// y cada ventana se mide con el DPI y el area de trabajo del monitor en el que esta.
    ///
    /// En pantallas chicas (1366x768) no alcanza para dibujarlo todo a 150%, asi que la
    /// escala se baja lo justo para que la interfaz siga entrando completa. Eso agranda lo
    /// que el usuario pidio, no lo achica, asi que no choca con la regla de arriba.
    ///
    /// Esa compensacion se aplica con LayoutTransform sobre la raiz de la ventana: WPF
    /// arma el diseno suponiendo que hay menos espacio y despues lo dibuja mas grande,
    /// por lo que no se cortan las tablas ni aparecen barras de scroll nuevas. Es una
    /// transformacion global, que es justo lo que la guia pide evitar, asi que por
    /// defecto no se usa.
    ///
    /// Al margen de eso, este archivo tambien hace el trabajo de diagnostico: registra
    /// en el log, ventana por ventana, que DPI y que area util detecto. Eso es lo que
    /// hay que mirar para ver como se ve la app en cada maquina.
    /// </summary>
    public static class UiScale
    {
        /// <summary>
        /// Escala de Windows (en DPI) para la que fue disenada la interfaz.
        /// 0 = sin compensacion: la app se dibuja tal cual Windows la escala.
        /// 144 = compensar hasta verse como a 150%.
        /// </summary>
        private static readonly double DESIGN_DPI = 144;

        /// <summary>Tamano logico minimo comodo. Si la pantalla no da para mas, se reduce la escala.</summary>
        private const double MIN_LOGICAL_WIDTH = 1150;
        private const double MIN_LOGICAL_HEIGHT = 620;

        private const double MIN_FACTOR = 0.6;

        /// <summary>
        /// Tope de seguridad, no de diseno. Con 3.0 alcanza para que Windows al 50% (que
        /// existe en algunas notebooks) llegue a 1.5x en vez de quedarse en 1.0x. Un tope mas
        /// bajo terminaria achicando a esos usuarios, que es justo lo que no se quiere.
        /// El limite que manda de verdad es el espacio disponible en el monitor, que se
        /// calcula mas arriba con MIN_LOGICAL_WIDTH y MIN_LOGICAL_HEIGHT.
        /// </summary>
        private const double MAX_FACTOR = 3.0;

        private const int WM_DPICHANGED = 0x02E0;
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const uint MONITOR_DEFAULTTOPRIMARY = 1;

        /// <summary>
        /// Estado de una ventana: la escala que le toca segun donde este, mas el tamano
        /// con el que se creo (para no multiplicarlo cada vez).
        /// </summary>
        private sealed class window_state
        {
            public double factor = 1.0;
            public double dpi;
            public int work_width;
            public int work_height;
            public double original_width;
            public double original_height;
            public bool size_saved;
        }

        private static readonly ConditionalWeakTable<Window, window_state> states = new();
        private static readonly ConditionalWeakTable<HwndSource, object> hooked_sources = new();

        public static void init()
        {
            // Un solo punto de entrada: cualquier ventana que se abra en el futuro
            // (incluidos los popups y menus desplegables) queda escalada sola.
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(on_window_loaded));
            EventManager.RegisterClassHandler(typeof(Popup), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(on_popup_loaded));
        }

        // ============ Calculo ============

        /// <summary>
        /// Escala que le corresponde a una ventana. dpi y el area de trabajo son los del
        /// monitor donde esta, no los del primario: por eso cada ventana va por su cuenta.
        ///
        /// REGLA QUE NO SE NEGOCIA: la compensacion solo agranda, nunca achica.
        ///
        /// Si la persona ya subio la escala de Windows, el texto que ve es el que pidio, y
        /// la app no tiene derecho a reducirselo. Por eso el factor nunca baja de 1.0.
        ///
        /// Eso tiene una consecuencia importante: sin este tope, la app quedaba con un
        /// techo de 1.5x para siempre, porque (144 / dpi) * (dpi / 96) siempre da 1.5
        /// exacto, para cualquier escala de Windows. Una persona con baja vision que
        /// pondria Windows al 200% recibia 1.5x en vez de 2.0x, y al 300% seguia
        /// recibiendo 1.5x. Subir Windows no habria servido de nada: era un muro.
        ///
        /// Con el tope, Windows al 200% da 2.0x y al 300% da 3.0x, que es lo pedido.
        /// </summary>
        private static double compute_factor(double dpi, int work_width_px, int work_height_px)
        {
            if (DESIGN_DPI <= 0) return 1.0;
            if (dpi <= 0) dpi = 96;

            // Compensar la escala que Windows ya aplica, para verse como a 150%.
            double target = DESIGN_DPI / dpi;

            // Tope para que en una pantalla pequena la interfaz entre completa.
            // Windows ya se come su parte (dpi/96), asi que el espacio logico real es:
            // pixeles / (escala_de_windows * escala_interna).
            double windows_scale = dpi / 96.0;
            double fit = Math.Max(target, MIN_FACTOR);
            if (work_width_px > 0) fit = Math.Min(fit, work_width_px / (windows_scale * MIN_LOGICAL_WIDTH));
            if (work_height_px > 0) fit = Math.Min(fit, work_height_px / (windows_scale * MIN_LOGICAL_HEIGHT));

            // Math.Max con 1.0 es el tope de "nunca achicar": si la compensacion de arriba
            // daria menos de 1.0, es decir achicando, no se aplica y se respeta Windows.
            return Math.Clamp(Math.Max(1.0, Math.Min(target, fit)), MIN_FACTOR, MAX_FACTOR);
        }

        // ============ Aplicacion por ventana ============

        private static void on_window_loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Window window) return;

            refresh(window);

            // El aviso de cambio de escala de Windows solo se puede escuchar en el handle
            // de la ventana, y recien existe despues de inicializarla.
            if (PresentationSource.FromVisual(window) is not HwndSource source) return;
            if (hooked_sources.TryGetValue(source, out _)) return;
            hooked_sources.Add(source, new object());

            var weak_window = new WeakReference<Window>(window);
            source.AddHook((hwnd, msg, wparam, lparam, ref handled) =>
            {
                if (!weak_window.TryGetTarget(out Window? target)) return IntPtr.Zero;
                return on_dpi_changed(target, msg, wparam, ref handled);
            });
        }

        /// <summary>
        /// Vuelve a medir la ventana y le aplica la escala que le toca. Se llama al
        /// mostrarse y cada vez que Windows avisa que cambio la escala del monitor.
        /// </summary>
        private static void refresh(Window window, double? dpi_from_message = null)
        {
            try
            {
                window_state state = state_for(window);
                IntPtr handle = new WindowInteropHelper(window).Handle;

                // El propio mensaje trae el DPI nuevo, asi que no hay ni que preguntarlo.
                double dpi = dpi_from_message ?? GetDpiForWindow(handle);
                if (dpi <= 0) dpi = state.dpi > 0 ? state.dpi : 96;

                GetWorkAreaForWindow(handle, out int width_px, out int height_px);
                double factor = compute_factor(dpi, width_px, height_px);

                // Se registra cuando cambia el monitor, no solo cuando cambia la escala.
                // Con la compensacion apagada la escala siempre es 100%, asi que si se
                // mirara solo eso, mover la ventana entre monitores no dejaria rastro
                // ninguno y no habria forma de verificar que el DPI se este detectando.
                bool first_time = state.dpi <= 0;
                bool dpi_changed = !first_time && Math.Abs(dpi - state.dpi) > 0.001;
                bool factor_changed = Math.Abs(factor - state.factor) > 0.001;

                // El cambio de DPI va en su propia linea, con el antes y el despues, para
                // que la prueba de dos monitores se pueda verificar leyendo el log sin
                // tener que comparar numeros a ojo.
                if (dpi_changed)
                {
                    AppLog.Info(
                        $"Cambio de DPI en la ventana \"{SafeTitle(window)}\": " +
                        $"Windows al {state.dpi / 96.0 * 100:N0}% -> {dpi / 96.0 * 100:N0}% " +
                        $"(area util {state.work_width}x{state.work_height} -> {width_px}x{height_px} px)");
                }
                else if (first_time || factor_changed)
                {
                    AppLog.Info(
                        $"Escala de la ventana \"{SafeTitle(window)}\": {factor * 100:N0}% " +
                        $"(Windows al {dpi / 96.0 * 100:N0}%, area util {width_px}x{height_px} px)");
                }

                state.dpi = dpi;
                state.factor = factor;
                state.work_width = width_px;
                state.work_height = height_px;

                apply(window, state, dpi, width_px, height_px);
            }
            catch (Exception ex)
            {
                AppLog.Error("No se pudo calcular la escala de la interfaz", ex);
            }
        }

        private static window_state state_for(Window window)
        {
            if (states.TryGetValue(window, out window_state? state)) return state;

            state = new window_state();
            states.Add(window, state);
            return state;
        }

        private static void apply(Window window, window_state state, double dpi, int work_width_px, int work_height_px)
        {
            // Con la compensacion apagada, o cuando el factor da 1.0 (que es el caso de
            // Windows al 150% o mas), no se toca absolutamente nada: ni transformaciones ni
            // tamanos. La app se dibuja tal cual Windows la escala.
            if (Math.Abs(state.factor - 1.0) < 0.001) return;

            if (window.Content is FrameworkElement root) apply_transform(root, state.factor);

            // Las ventanas con SizeToContent calculan su alto solas (dialogos y avisos);
            // agrandarlas a mano romperia ese calculo.
            if (window.SizeToContent != SizeToContent.Manual) return;
            if (window.WindowState != WindowState.Normal) return;
            if (double.IsNaN(window.Width) || double.IsNaN(window.Height)) return;

            if (!state.size_saved)
            {
                state.original_width = window.Width;
                state.original_height = window.Height;
                state.size_saved = true;
            }

            if (state.original_width <= 0 || state.original_height <= 0) return;

            // El tamano de la ventana se mide en DIPs, pero lo que manda es que quepa
            // en el monitor donde esta ahora, no en el primario.
            double max_width = work_width_px * 96.0 / dpi / state.factor;
            double max_height = work_height_px * 96.0 / dpi / state.factor;

            window.Width = Math.Min(state.original_width * state.factor, max_width);
            window.Height = Math.Min(state.original_height * state.factor, max_height);
        }

        private static void on_popup_loaded(object sender, RoutedEventArgs e)
        {
            // Los desplegables viven fuera de la ventana: si no se escalan, las listas
            // se verian chicas dentro de una interfaz grande. Toman la escala de la
            // ventana Dueña, que es la que los contiene.
            if (sender is not Popup popup || popup.Child is not FrameworkElement child) return;
            if (Window.GetWindow(child) is not Window owner) return;
            if (!states.TryGetValue(owner, out window_state? state)) return;
            if (Math.Abs(state.factor - 1.0) < 0.001) return;

            apply_transform(child, state.factor);
        }

        private static void apply_transform(FrameworkElement element, double factor)
        {
            // Si ya hay un ScaleTransform (la ventana se recargo) se actualiza en vez
            // de envolverlo en otro, que multiplicaria la escala.
            if (element.LayoutTransform is ScaleTransform existing)
            {
                existing.ScaleX = factor;
                existing.ScaleY = factor;
                return;
            }

            element.LayoutTransform = new ScaleTransform(factor, factor);
        }

        private static IntPtr on_dpi_changed(Window window, int msg, IntPtr wparam, ref bool handled)
        {
            if (msg != WM_DPICHANGED) return IntPtr.Zero;

            // wParam trae el DPI nuevo: el WORD bajo es el eje X.
            double new_dpi = (uint)(wparam.ToInt64() & 0xFFFF);

            // Se aplaza para no pelear con el cambio de tamano que Windows esta
            // haciendo en este mismo momento.
            Application.Current?.Dispatcher.BeginInvoke(new Action(() => refresh(window, new_dpi)));
            return IntPtr.Zero;
        }

        private static string SafeTitle(Window window)
        {
            try { return window.Title; } catch { return "ventana"; }
        }

        // ============ Windows ============

        /// <summary>
        /// DPI real de la ventana. GetDpiForWindow es Windows 10 1607 en adelante; si no
        /// esta, se cae a GetDpiForMonitor de Windows 8.1.
        /// </summary>
        private static double GetDpiForWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return 0;

            try
            {
                uint dpi = GetDpiForWindowNative(handle);
                if (dpi > 0) return dpi;
            }
            catch (EntryPointNotFoundException)
            {
            }

            try
            {
                IntPtr monitor = MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero) return 0;
                if (GetDpiForMonitor(monitor, 0, out uint dpi_x, out _) == 0) return dpi_x;
            }
            catch (EntryPointNotFoundException)
            {
            }

            return 0;
        }

        /// <summary>Area de trabajo (sin barra de tareas) del monitor de esa ventana.</summary>
        private static void GetWorkAreaForWindow(IntPtr handle, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (handle != IntPtr.Zero)
            {
                IntPtr monitor = MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero && TryGetWorkArea(monitor, out width, out height)) return;
            }

            // Si no se pudo leer el monitor de la ventana, se usa el principal.
            if (width > 0 && height > 0) return;
            if (!TryGetWorkArea(MonitorFromPoint(default, MONITOR_DEFAULTTOPRIMARY), out width, out height))
            {
                width = 0;
                height = 0;
            }
        }

        private static bool TryGetWorkArea(IntPtr monitor, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (monitor == IntPtr.Zero) return false;

            MONITORINFO info = new() { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, ref info)) return false;

            width = info.rcWork.right - info.rcWork.left;
            height = info.rcWork.bottom - info.rcWork.top;
            return width > 0 && height > 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindowNative(IntPtr handle);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpi_x, out uint dpi_y);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT point, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    }
}
