using System.Windows;
using System.Windows.Input;
using NinOS.UI.Common;

namespace NinOS.UI.Views
{
    public partial class AdminLoginWindow : Window
    {
        public bool Unlocked { get; private set; }

        public AdminLoginWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PassBox.Focus();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                OnEnterClick(sender, e);
            }
        }

        private void OnEnterClick(object sender, RoutedEventArgs e)
        {
            if (!AdminSecurity.verify(PassBox.Password))
            {
                int wait = AdminSecurity.remaining_delay_seconds();
                ErrorText.Text = wait > 0
                    ? $"Contraseña incorrecta. Inténtalo de nuevo en {wait} s."
                    : "Contraseña incorrecta.";
                PassBox.Clear();
                PassBox.Focus();
                return;
            }

            Unlocked = true;
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}