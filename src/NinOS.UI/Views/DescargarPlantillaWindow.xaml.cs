using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NinOS.Domain;
using NinOS.Domain.CargaMasiva;
using NinOS.UI.Common;

namespace NinOS.UI.Views
{
    public partial class DescargarPlantillaWindow : Window
    {
        private readonly List<price_list> _listas;

        public DescargarPlantillaWindow(List<price_list> listas)
        {
            _listas = listas ?? throw new ArgumentNullException(nameof(listas));
            InitializeComponent();
            cmb_lista.ItemsSource = _listas;
            if (_listas.Count > 0) cmb_lista.SelectedIndex = 0;
        }

        public TipoPlantilla tipo_elegido { get; private set; } = TipoPlantilla.Cantidad;

        public int? id_lista_elegida { get; private set; }

        private void on_tipo_changed(object sender, SelectionChangedEventArgs e)
        {
            if (panel_lista == null || cmb_tipo == null) return;
            panel_lista.Visibility = es_precio() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void on_descargar_click(object sender, RoutedEventArgs e)
        {
            price_list? lista_elegida = cmb_lista.SelectedItem as price_list;
            bool precio = es_precio();

            if (precio && lista_elegida == null)
            {
                AppDialog.Show("Elija la lista de precios.", "Descargar plantilla", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            tipo_elegido = precio ? TipoPlantilla.Precio : TipoPlantilla.Cantidad;
            id_lista_elegida = precio ? lista_elegida!.id_price_list : null;
            DialogResult = true;
        }

        private bool es_precio()
        {
            return cmb_tipo.SelectedItem is ComboBoxItem item && item.Tag as string == "precio";
        }
    }
}
