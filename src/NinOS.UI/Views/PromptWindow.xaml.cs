using System.Windows;
using System.Windows.Input;

namespace NinOS.UI.Views
{
    public partial class PromptWindow : Window
    {
        public string Value { get; private set; } = string.Empty;
        public bool Accepted { get; private set; }

        public PromptWindow(string title, string message, string? default_value = null)
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
            ValueBox.Text = default_value ?? string.Empty;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ValueBox.Focus();
            ValueBox.SelectAll();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            Value = (ValueBox.Text ?? string.Empty).Trim();
            Accepted = true;
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

        public static string? ask(Window owner, string title, string message, string? default_value = null)
        {
            var prompt = new PromptWindow(title, message, default_value);
            prompt.Owner = owner;
            prompt.ShowDialog();
            return prompt.Accepted ? prompt.Value : null;
        }
    }
}