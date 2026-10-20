using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace NovaGames.UI.Helpers
{
    public static class TextHighlightHelper
    {
        public static readonly DependencyProperty HighlightedTextProperty =
            DependencyProperty.RegisterAttached("HighlightedText", typeof(string), typeof(TextHighlightHelper), new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty SearchTermProperty =
            DependencyProperty.RegisterAttached("SearchTerm", typeof(string), typeof(TextHighlightHelper), new PropertyMetadata(string.Empty, OnTextChanged));

        public static string GetHighlightedText(DependencyObject obj) => (string)obj.GetValue(HighlightedTextProperty);
        public static void SetHighlightedText(DependencyObject obj, string value) => obj.SetValue(HighlightedTextProperty, value);

        public static string GetSearchTerm(DependencyObject obj) => (string)obj.GetValue(SearchTermProperty);
        public static void SetSearchTerm(DependencyObject obj, string value) => obj.SetValue(SearchTermProperty, value);

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                string text = GetHighlightedText(textBlock) ?? "";
                string searchTerm = GetSearchTerm(textBlock) ?? "";

                textBlock.Inlines.Clear();

                // Якщо пошук порожній або збігів немає - виводимо звичайний білий текст
                if (string.IsNullOrEmpty(searchTerm) || text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    textBlock.Inlines.Add(new Run(text) { Foreground = Brushes.White });
                    return;
                }

                // Шукаємо, де починається наш пошуковий запит
                int index = text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
                var highlightBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ADEF"));

                // 1. Частина ДО збігу
                if (index > 0)
                    textBlock.Inlines.Add(new Run(text.Substring(0, index)) { Foreground = Brushes.White });

                // 2. Сам збіг
                textBlock.Inlines.Add(new Run(text.Substring(index, searchTerm.Length)) { Foreground = highlightBrush });

                // 3. Частина ПІСЛЯ збігу
                if (index + searchTerm.Length < text.Length)
                    textBlock.Inlines.Add(new Run(text.Substring(index + searchTerm.Length)) { Foreground = Brushes.White });
            }
        }
    }
}