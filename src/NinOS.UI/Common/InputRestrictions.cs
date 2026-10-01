using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NinOS.UI.Common
{
    public static class InputRestrictions
    {
        public static void digits_only(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;
            e.Handled = !e.Text.All(char.IsDigit);
        }

        public static void numbers_only(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;

            // Allow only digits, dot, comma
            if (!e.Text.All(c => char.IsDigit(c) || c == '.' || c == ','))
            {
                e.Handled = true;
                return;
            }

            if (sender is TextBox tb)
            {
                string current = tb.Text ?? string.Empty;
                int selStart = tb.SelectionStart;
                int selLength = tb.SelectionLength;

                if (selStart > current.Length) selStart = current.Length;
                if (selStart + selLength > current.Length) selLength = current.Length - selStart;

                string proposed = current.Remove(selStart, selLength).Insert(selStart, e.Text);

                int sepCount = proposed.Count(c => c == '.' || c == ',');
                if (sepCount > 1 || !proposed.All(c => char.IsDigit(c) || c == '.' || c == ','))
                {
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                if (!e.Text.All(char.IsDigit))
                {
                    e.Handled = true;
                }
            }
        }

        public static void block_space(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        public static void on_paste_numbers(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pasteText = (string)e.DataObject.GetData(typeof(string));
                if (string.IsNullOrEmpty(pasteText)) return;

                if (sender is TextBox tb)
                {
                    string current = tb.Text ?? string.Empty;
                    int selStart = tb.SelectionStart;
                    int selLength = tb.SelectionLength;

                    if (selStart > current.Length) selStart = current.Length;
                    if (selStart + selLength > current.Length) selLength = current.Length - selStart;

                    string proposed = current.Remove(selStart, selLength).Insert(selStart, pasteText);
                    int sepCount = proposed.Count(c => c == '.' || c == ',');
                    if (sepCount > 1 || !proposed.All(c => char.IsDigit(c) || c == '.' || c == ','))
                    {
                        e.CancelCommand();
                    }
                }
                else
                {
                    if (!pasteText.All(char.IsDigit))
                    {
                        e.CancelCommand();
                    }
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        public static void on_paste_digits(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pasteText = (string)e.DataObject.GetData(typeof(string));
                if (string.IsNullOrEmpty(pasteText)) return;

                if (!pasteText.All(char.IsDigit))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        public static void attach_decimal(TextBox tb)
        {
            if (tb == null) return;
            tb.PreviewTextInput -= numbers_only;
            tb.PreviewTextInput += numbers_only;
            tb.PreviewKeyDown -= block_space;
            tb.PreviewKeyDown += block_space;
            DataObject.RemovePastingHandler(tb, on_paste_numbers);
            DataObject.AddPastingHandler(tb, on_paste_numbers);
        }

        public static void attach_integer(TextBox tb)
        {
            if (tb == null) return;
            tb.PreviewTextInput -= digits_only;
            tb.PreviewTextInput -= digits_only;
            tb.PreviewKeyDown -= block_space;
            tb.PreviewKeyDown += block_space;
            DataObject.RemovePastingHandler(tb, on_paste_digits);
            DataObject.AddPastingHandler(tb, on_paste_digits);
        }

        public static bool has_letters(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Any(char.IsLetter);
        }

        public static bool is_valid_decimal(string? text, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string norm = text.Trim().Replace(',', '.');
            return decimal.TryParse(norm, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        public static bool is_valid_integer(string? text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}