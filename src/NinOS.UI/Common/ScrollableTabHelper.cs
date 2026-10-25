using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace NinOS.UI.Common
{
    /// <summary>
    /// Helper que transforma cualquier TabControl en una barra de pestañas de una sola fila
    /// con desplazamiento horizontal suave, botones de navegación discretos y soporte para rueda del ratón.
    /// Evita que WPF cree múltiples filas de pestañas o intercambie filas, manteniendo la altura fija.
    /// </summary>
    public static class ScrollableTabHelper
    {
        public static readonly DependencyProperty IsScrollableProperty =
            DependencyProperty.RegisterAttached(
                "IsScrollable",
                typeof(bool),
                typeof(ScrollableTabHelper),
                new FrameworkPropertyMetadata(false, OnIsScrollableChanged));

        public static bool GetIsScrollable(DependencyObject obj) =>
            (bool)obj.GetValue(IsScrollableProperty);

        public static void SetIsScrollable(DependencyObject obj, bool value) =>
            obj.SetValue(IsScrollableProperty, value);

        private static void OnIsScrollableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TabControl tabControl) return;

            if ((bool)e.NewValue)
            {
                tabControl.Loaded += TabControl_Loaded;
                tabControl.SelectionChanged += TabControl_SelectionChanged;

                if (tabControl.IsLoaded)
                {
                    AttachToTemplate(tabControl);
                }
            }
            else
            {
                tabControl.Loaded -= TabControl_Loaded;
                tabControl.SelectionChanged -= TabControl_SelectionChanged;
            }
        }

        private static void TabControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TabControl tabControl) return;
            AttachToTemplate(tabControl);
        }

        private static void AttachToTemplate(TabControl tabControl)
        {
            tabControl.ApplyTemplate();
            if (tabControl.Template == null) return;

            var scrollViewer = tabControl.Template.FindName("PART_ScrollViewer", tabControl) as ScrollViewer;
            if (scrollViewer == null) return;

            // Evitar suscripciones duplicadas
            scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
            scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;

            scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
            scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;

            var btnLeft = tabControl.Template.FindName("PART_ButtonScrollLeft", tabControl) as RepeatButton;
            var btnRight = tabControl.Template.FindName("PART_ButtonScrollRight", tabControl) as RepeatButton;

            if (btnLeft != null)
            {
                btnLeft.Click -= BtnLeft_Click;
                btnLeft.Click += BtnLeft_Click;
            }

            if (btnRight != null)
            {
                btnRight.Click -= BtnRight_Click;
                btnRight.Click += BtnRight_Click;
            }

            UpdateButtons(scrollViewer, btnLeft, btnRight);
        }

        private static void BtnLeft_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            var tabControl = (el.TemplatedParent as TabControl) ?? FindParent<TabControl>(el);
            var sv = tabControl?.Template?.FindName("PART_ScrollViewer", tabControl) as ScrollViewer;
            if (sv != null)
            {
                sv.ScrollToHorizontalOffset(Math.Max(0, sv.HorizontalOffset - 120));
            }
        }

        private static void BtnRight_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            var tabControl = (el.TemplatedParent as TabControl) ?? FindParent<TabControl>(el);
            var sv = tabControl?.Template?.FindName("PART_ScrollViewer", tabControl) as ScrollViewer;
            if (sv != null)
            {
                sv.ScrollToHorizontalOffset(Math.Min(sv.ScrollableWidth, sv.HorizontalOffset + 120));
            }
        }

        private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            var tabControl = (sv.TemplatedParent as TabControl) ?? FindParent<TabControl>(sv);
            if (tabControl == null) return;

            var btnLeft = tabControl.Template?.FindName("PART_ButtonScrollLeft", tabControl) as RepeatButton;
            var btnRight = tabControl.Template?.FindName("PART_ButtonScrollRight", tabControl) as RepeatButton;
            UpdateButtons(sv, btnLeft, btnRight);
        }

        private static void UpdateButtons(ScrollViewer sv, RepeatButton? btnLeft, RepeatButton? btnRight)
        {
            bool canScroll = sv.ScrollableWidth > 1.0;

            if (btnLeft != null)
            {
                btnLeft.Visibility = canScroll ? Visibility.Visible : Visibility.Collapsed;
                btnLeft.IsEnabled = canScroll && sv.HorizontalOffset > 0.5;
            }

            if (btnRight != null)
            {
                btnRight.Visibility = canScroll ? Visibility.Visible : Visibility.Collapsed;
                btnRight.IsEnabled = canScroll && sv.HorizontalOffset < (sv.ScrollableWidth - 0.5);
            }
        }

        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            if (sv.ScrollableWidth <= 0.5) return;

            // Rueda hacia arriba/adelante (Delta > 0) -> desplazar a la izquierda
            // Rueda hacia abajo/atrás (Delta < 0) -> desplazar a la derecha
            double step = 80.0;
            double targetOffset = e.Delta > 0 
                ? sv.HorizontalOffset - step 
                : sv.HorizontalOffset + step;

            if (targetOffset < 0) targetOffset = 0;
            if (targetOffset > sv.ScrollableWidth) targetOffset = sv.ScrollableWidth;

            sv.ScrollToHorizontalOffset(targetOffset);
            e.Handled = true;
        }

        private static void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl tc || !ReferenceEquals(e.OriginalSource, tc)) return;

            tc.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (tc.SelectedItem != null)
                {
                    var container = tc.ItemContainerGenerator.ContainerFromItem(tc.SelectedItem) as FrameworkElement;
                    container?.BringIntoView();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typed) return typed;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}

