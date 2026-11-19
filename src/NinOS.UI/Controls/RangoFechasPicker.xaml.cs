using System;
using System.Windows;
using System.Windows.Controls;
using NinOS.Domain;
using NinOS.Infrastructure.Logging;
using NinOS.UI.Common;

namespace NinOS.UI.Controls
{
    public partial class RangoFechasPicker : UserControl
    {
        private const string FormatoFecha = "dd/MM/yyyy";

        public static readonly DependencyProperty RangoProperty = DependencyProperty.Register(
            nameof(Rango),
            typeof(rango_fechas),
            typeof(RangoFechasPicker),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnRangoCambiadoDesdeFuera));

        public event EventHandler? RangoCambiado;

        private bool _sincronizando;
        private DateTime? _desde_valido;
        private DateTime? _hasta_valido;

        public RangoFechasPicker()
        {
            InitializeComponent();
            ActualizarVisualesDesdeEstado();
        }

        public rango_fechas? Rango
        {
            get => (rango_fechas?)GetValue(RangoProperty);
            set => SetValue(RangoProperty, value);
        }

        private static void OnRangoCambiadoDesdeFuera(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is RangoFechasPicker picker)
            {
                picker.MostrarRango(e.NewValue as rango_fechas);
            }
        }

        private void MostrarRango(rango_fechas? rango)
        {
            if (_sincronizando) return;

            _sincronizando = true;
            try
            {
                DesdePicker.SelectedDate = rango?.desde;
                HastaPicker.SelectedDate = rango?.hasta;
                RecordarValidos(rango?.desde, rango?.hasta);
                ActualizarVisualesDesdeEstado();
            }
            finally
            {
                _sincronizando = false;
            }
        }

        private void OnFechaCambiada(object sender, SelectionChangedEventArgs e)
        {
            if (_sincronizando) return;

            DateTime? desde = DesdePicker.SelectedDate;
            DateTime? hasta = HastaPicker.SelectedDate;

            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            {
                AppLog.Warn("Filtro de fechas: Desde posterior a Hasta; se conserva el rango anterior.");
                AppDialog.Show("La fecha Desde no puede ser posterior a la fecha Hasta.", "Rango de fechas",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MostrarRango(ObtenerRangoValido());
                return;
            }

            PublicarRango(new rango_fechas(desde, hasta));
        }

        private void OnFechaInvalida(object sender, DatePickerDateValidationErrorEventArgs e)
        {
            e.ThrowException = false;
            AppLog.Warn("Filtro de fechas: texto de fecha no válido.");
            AppDialog.Show("La fecha escrita no es válida. Use el formato dd/mm/aaaa o elíjala en el calendario.",
                "Rango de fechas", MessageBoxButton.OK, MessageBoxImage.Warning);

            _sincronizando = true;
            try
            {
                if (sender == DesdePicker) DesdePicker.Text = _desde_valido?.ToString(FormatoFecha) ?? string.Empty;
                else if (sender == HastaPicker) HastaPicker.Text = _hasta_valido?.ToString(FormatoFecha) ?? string.Empty;
            }
            finally
            {
                _sincronizando = false;
            }
        }

        private void OnLimpiarDesde(object sender, RoutedEventArgs e)
        {
            DesdePicker.SelectedDate = null;
        }

        private void OnLimpiarHasta(object sender, RoutedEventArgs e)
        {
            HastaPicker.SelectedDate = null;
        }

        private void PublicarRango(rango_fechas nuevo_rango)
        {
            _sincronizando = true;
            try
            {
                RecordarValidos(nuevo_rango.desde, nuevo_rango.hasta);
                ActualizarVisualesDesdeEstado();
                Rango = nuevo_rango;
                AppLog.Info($"Filtro de fechas: {nuevo_rango.texto_titulo()}.");
                RangoCambiado?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _sincronizando = false;
            }
        }

        private rango_fechas ObtenerRangoValido()
        {
            return new rango_fechas(_desde_valido, _hasta_valido);
        }

        private void RecordarValidos(DateTime? desde, DateTime? hasta)
        {
            _desde_valido = desde;
            _hasta_valido = hasta;
        }

        private void ActualizarVisualesDesdeEstado()
        {
            DesdeMarcador.Visibility = DesdePicker.SelectedDate.HasValue ? Visibility.Collapsed : Visibility.Visible;
            HastaMarcador.Visibility = HastaPicker.SelectedDate.HasValue ? Visibility.Collapsed : Visibility.Visible;
            DesdeLimpiar.Visibility = DesdePicker.SelectedDate.HasValue ? Visibility.Visible : Visibility.Collapsed;
            HastaLimpiar.Visibility = HastaPicker.SelectedDate.HasValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
