using System.Windows;
using NinOS.Domain.CargaMasiva;

namespace NinOS.UI.Views
{
    public partial class ElegirTipoCargaWindow : Window
    {
        public ElegirTipoCargaWindow()
        {
            InitializeComponent();
        }

        public TipoCargaCantidad tipo_elegido { get; private set; } = TipoCargaCantidad.Conteo;

        private void on_continuar_click(object sender, RoutedEventArgs e)
        {
            tipo_elegido = rb_ajuste.IsChecked == true ? TipoCargaCantidad.Ajuste : TipoCargaCantidad.Conteo;
            DialogResult = true;
        }
    }
}
