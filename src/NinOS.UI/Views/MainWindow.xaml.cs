using System.Windows;
using NinOS.UI.Common.ViewModels;

namespace NinOS.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel main_view_model)
        {
            InitializeComponent();
            DataContext = main_view_model;
        }
    }
}