using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NinOS.UI.Views
{
    public partial class AppDialogWindow : Window
    {
        private readonly MessageBoxButton _buttons;
        private readonly MessageBoxResult _default_result;
        private readonly string? _yes_text;
        private readonly string? _no_text;

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public AppDialogWindow(string title, string message, MessageBoxButton buttons, MessageBoxImage image, Window? owner)
            : this(title, message, buttons, image, owner, MessageBoxResult.None, null, null)
        {
        }

        public AppDialogWindow(
            string title,
            string message,
            MessageBoxButton buttons,
            MessageBoxImage image,
            Window? owner,
            MessageBoxResult default_result,
            string? yes_text,
            string? no_text)
        {
            InitializeComponent();
            Owner = owner;
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;

            _buttons = buttons;
            _default_result = default_result;
            _yes_text = yes_text;
            _no_text = no_text;
            Result = default_result;

            TitleText.Text = string.IsNullOrWhiteSpace(title) ? "NinOS" : title;
            MessageText.Text = message;
            SetIcon(image);
            BuildButtons();
        }

        private void SetIcon(MessageBoxImage image)
        {
            switch (image)
            {
                case MessageBoxImage.Error:
                    IconGlyph.Text = "\u2715";
                    IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D));
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));
                    break;
                case MessageBoxImage.Warning:
                    IconGlyph.Text = "!";
                    IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0xA2, 0x27));
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(0xC9, 0xA2, 0x27));
                    break;
                case MessageBoxImage.Question:
                    IconGlyph.Text = "?";
                    IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
                    break;
                case MessageBoxImage.Information:
                    IconGlyph.Text = "i";
                    IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x6F, 0xC4));
                    break;
                default:
                    IconGlyph.Text = "!";
                    IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0xA2, 0x27));
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(0xC9, 0xA2, 0x27));
                    break;
            }
        }

        private void BuildButtons()
        {
            switch (_buttons)
            {
                case MessageBoxButton.OKCancel:
                    AddButton("Aceptar", MessageBoxResult.OK, true);
                    AddButton("Cancelar", MessageBoxResult.Cancel);
                    break;
case MessageBoxButton.YesNo:
                                AddButton(_yes_text ?? "Sí", MessageBoxResult.Yes, true);
                                AddButton(_no_text ?? "No", MessageBoxResult.No);
                                break;
                            case MessageBoxButton.YesNoCancel:
                                AddButton(_yes_text ?? "Sí", MessageBoxResult.Yes, true);
                                AddButton(_no_text ?? "No", MessageBoxResult.No);
                                AddButton("Cancelar", MessageBoxResult.Cancel);
                                break;
                default:
                    AddButton("Aceptar", MessageBoxResult.OK, true);
                    break;
            }
        }

        private void AddButton(string text, MessageBoxResult result, bool primary = false)
        {
            var button = new Button
            {
                Content = text,
                Style = (Style)FindResource(primary ? "DialogPrimaryStyle" : "DialogButtonStyle")
            };
            button.Click += (s, e) =>
            {
                Result = result;
                Close();
            };
            ButtonPanel.Children.Add(button);
        }
    }
}