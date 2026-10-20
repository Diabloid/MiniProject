using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NovaGames.UI.Views
{
    public partial class ConfirmWindow : Window
    {
        // Конструктор з опціональними параметрами
        public ConfirmWindow(
            string header,
            string message,
            string confirmBtnText,
            string warningText = "Цю дію неможливо буде скасувати.",
            bool showCancelButton = true,                      // Показувати кнопку скасування?
            string headerColorHex = "#FF5555",                 // Колір заголовка
            string confirmBtnStyle = "DialogRedButtonStyle"    // Стиль кнопки
        )
        {
            InitializeComponent();

            // 1. Заповнюємо базові тексти
            txtHeader.Text = header.ToUpper();
            txtMessage.Text = message;
            btnConfirm.Content = confirmBtnText.ToUpper();

            // 2. Попередження (ховаємо, якщо порожнє)
            if (string.IsNullOrEmpty(warningText))
            {
                txtWarning.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtWarning.Text = warningText;
            }

            // 3. Динамічний колір заголовка
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(headerColorHex);
                txtHeader.Foreground = new SolidColorBrush(color);
            }
            catch { /* Якщо hex кривий, залишається дефолтний з XAML */ }

            // 4. Динамічний стиль кнопки (шукає і в Window.Resources, і в App.xaml)
            var style = TryFindResource(confirmBtnStyle) as Style;
            if (style != null)
            {
                btnConfirm.Style = style;
            }

            // 5. Логіка однієї кнопки
            if (!showCancelButton)
            {
                btnCancel.Visibility = Visibility.Collapsed;

                Grid.SetColumn(btnConfirm, 0);
                Grid.SetColumnSpan(btnConfirm, 3);
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}