using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Helper que permite abrir un ContextMenu asociado a un Button
    /// con un clic izquierdo normal, posicionándolo debajo del botón.
    /// </summary>
    public static class DropdownMenuHelper
    {
        public static readonly DependencyProperty IsOpenOnClickProperty =
            DependencyProperty.RegisterAttached(
                "IsOpenOnClick",
                typeof(bool),
                typeof(DropdownMenuHelper),
                new FrameworkPropertyMetadata(false, OnIsOpenOnClickChanged));

        public static bool GetIsOpenOnClick(DependencyObject obj) =>
            (bool)obj.GetValue(IsOpenOnClickProperty);

        public static void SetIsOpenOnClick(DependencyObject obj, bool value) =>
            obj.SetValue(IsOpenOnClickProperty, value);

        private static void OnIsOpenOnClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn)
            {
                btn.Click -= OnButtonClick;
                if ((bool)e.NewValue)
                {
                    btn.Click += OnButtonClick;
                }
            }
        }

        private static void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = PlacementMode.Bottom;
                if (btn.ContextMenu.DataContext == null || !ReferenceEquals(btn.ContextMenu.DataContext, btn.DataContext))
                {
                    btn.ContextMenu.DataContext = btn.DataContext;
                }
                btn.ContextMenu.IsOpen = !btn.ContextMenu.IsOpen;
                e.Handled = true;
            }
        }
    }
}
