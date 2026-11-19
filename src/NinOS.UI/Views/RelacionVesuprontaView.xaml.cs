using System.Windows;
using System.Windows.Controls;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class RelacionVesuprontaView : UserControl
    {
        public RelacionVesuprontaView()
        {
            InitializeComponent();
            IsVisibleChanged += on_visibility_changed;
        }

        private void on_visibility_changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && DataContext is RelacionVesuprontaViewModel view_model)
            {
                view_model.refresh_data();
            }
        }
    }
}
