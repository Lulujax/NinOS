using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NinOS.Domain.CargaMasiva;
using NinOS.UI.Common.CargaMasiva;

namespace NinOS.UI.Views
{
    public partial class VistaPreviaCargaWindow : Window
    {
        private readonly VistaPreviaCarga _vista;

        public VistaPreviaCargaWindow(VistaPreviaCarga vista)
        {
            _vista = vista ?? throw new ArgumentNullException(nameof(vista));
            InitializeComponent();

            pintar_encabezado();
            grilla.ItemsSource = filas_a_mostrar();
            btn_aplicar.IsEnabled = _vista.puede_aplicar;
        }

        public bool incluir_avisos => rb_aplicar_igual.IsChecked == true;

        private void pintar_encabezado()
        {
            txt_titulo.Text = _vista.cabecera.tipo == TipoPlantilla.Cantidad
                ? "Vista previa — " + (_vista.tipo_carga == TipoCargaCantidad.Conteo ? "Conteo" : "Ajuste") + " de cantidades"
                : "Vista previa — precios de " + (_vista.cabecera.es_principal ? "la lista Principal" : "la lista " + _vista.cabecera.nombre_lista);

            txt_resumen.Text = _vista.cambian + " cambian  ·  " + _vista.avisos + " para revisar  ·  "
                + _vista.errores + " con error  ·  " + _vista.iguales + " sin cambio";

            if (_vista.productos_en_otras_listas > 0)
            {
                txt_otras_listas.Text = "Este cambio también cambia el precio de " + _vista.productos_en_otras_listas
                    + " productos en otras listas, porque esas listas no tienen precio propio para ellos.";
                txt_otras_listas.Visibility = Visibility.Visible;
            }

            if (_vista.avisos > 0)
            {
                txt_avisos.Text = "Hay " + _vista.avisos + " filas en amarillo. ¿Qué desea hacer con ellas?";
                panel_avisos.Visibility = Visibility.Visible;
            }

            txt_errores.Visibility = _vista.errores > 0 ? Visibility.Visible : Visibility.Collapsed;
            txt_sin_cambios.Visibility = _vista.errores == 0 && _vista.cambian + _vista.avisos == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private List<FilaVistaPrevia> filas_a_mostrar()
        {
            return _vista.resultados
                .Where(r => r.estado != EstadoFila.Igual)
                .Select(r => FilaVistaPrevia.desde(r, _vista.cabecera.tipo))
                .ToList();
        }

        private void on_aplicar_click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
