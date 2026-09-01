using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace NinOS.UI.Common
{
    public static class InputRestrictions
    {
        public static void digits_only(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        public static void numbers_only(object sender, TextCompositionEventArgs e)
        {
            if (!e.Text.All(c => char.IsDigit(c) || c == '.' || c == ',')) { e.Handled = true; return; }
            if (sender is TextBox tb && (tb.Text.Contains('.') || tb.Text.Contains(','))) e.Handled = true;
        }
    }
}