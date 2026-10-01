using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Pone en mayusculas el texto de los botones de la aplicacion, solo en pantalla.
    ///
    /// Es puramente visual: no cambia lo que se guarda en la base de datos, ni lo que se
    /// busca, ni lo que se imprime en los PDF.
    ///
    /// Por que no alcanza con un estilo global: casi todos los botones de la app tienen un
    /// estilo con x:Key propio (RowEditButton, HeaderAddButton, RowDeleteButton, ...), y un
    /// estilo implicito no los alcanza. Tampoco sirve enganchar el evento Loaded de cada
    /// boton: en esta aplicacion ese evento no se enruta como los demas. Por eso se espera
    /// a que la ventana termine de cargar y ahi se recorren los botones que ya existen; para
    /// los que crean las tablas al hacer scroll se escucha LoadingRow.
    ///
    /// Un boton cuyo texto viene de un binding no se toca, para que siga actualizandose
    /// cuando cambie el dato.
    /// </summary>
    public static class UpperCaseText
    {
        private static bool _globally_enabled;
        private static readonly ConditionalWeakTable<DataGrid, object> _hooked_grids = new();
        private static readonly ConditionalWeakTable<TabControl, object> _hooked_tabs = new();

        /// <summary>Permite activar las mayusculas en un boton suelto, sin el hook global.</summary>
        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(UpperCaseText),
            new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);

        public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

        /// <summary>
        /// Activa las mayusculas en todos los botones de la app. Se llama una sola vez
        /// desde App.OnStartup.
        /// </summary>
        public static void EnableGlobally()
        {
            if (_globally_enabled) return;
            _globally_enabled = true;

            // Igual que la escala: se registra en Window para cubrir toda ventana que se
            // abra, ahora o despues (ventana principal, dialogos y panel secreto).
            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWindowLoaded));
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Window window) return;

            // Se espera a que el layout este armado para que el recorrido encuentre los
            // botones; a partir de ahi, las tablas avisan cuando crean filas nuevas.
            window.Dispatcher.BeginInvoke(
                new Action(() => ApplyTree(window)),
                DispatcherPriority.Loaded);
        }

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Button button) return;

            button.Loaded += OnSingleLoaded;
            if (e.NewValue is true) Apply(button);
        }

        private static void OnSingleLoaded(object sender, RoutedEventArgs e) => Apply((Button)sender);

        /// <summary>Recorre el arbol y pone en mayusculas todos los botones que encuentra.</summary>
        private static void ApplyTree(DependencyObject root)
        {
            if (root is null) return;

            if (root is Button button) Apply(button);
            if (root is DataGrid grid) HookGrid(grid);
            if (root is TabControl tabs) HookTabs(tabs);

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                ApplyTree(VisualTreeHelper.GetChild(root, i));
            }
        }

        private static void HookTabs(TabControl tabs)
        {
            if (_hooked_tabs.TryGetValue(tabs, out _)) return;
            _hooked_tabs.Add(tabs, new object());

            // Las vistas de las pestanas se crean recien cuando el usuario las abre, asi
            // que sus botones todavia no existen cuando carga la ventana.
            tabs.SelectionChanged += (s, e) =>
            {
                if (tabs.SelectedContent is not DependencyObject content) return;

                content.Dispatcher.BeginInvoke(
                    new Action(() => ApplyTree(content)),
                    DispatcherPriority.Loaded);
            };
        }

        private static void HookGrid(DataGrid grid)
        {
            if (_hooked_grids.TryGetValue(grid, out _)) return;
            _hooked_grids.Add(grid, new object());

            // Las tablas crean y reciclan filas al hacer scroll: cada fila nueva vuelve a
            // pasar por aca para que sus botones tambien queden en mayusculas.
            grid.LoadingRow += (s, e) =>
            {
                e.Row.Dispatcher.BeginInvoke(
                    new Action(() => ApplyTree(e.Row)),
                    DispatcherPriority.Loaded);
            };
        }

        private static void Apply(Button button)
        {
            if (button.Content is not string text || text.Length == 0) return;

            // Si el texto viene de un binding no se toca: pisarlo dejaria el boton con un
            // valor fijo y dejaria de actualizarse cuando cambie el dato.
            if (BindingOperations.GetBindingExpression(button, ContentControl.ContentProperty) != null) return;

            string upper = CultureInfo.CurrentUICulture.TextInfo.ToUpper(text);
            if (text == upper) return;

            button.Content = upper;
        }
    }
}