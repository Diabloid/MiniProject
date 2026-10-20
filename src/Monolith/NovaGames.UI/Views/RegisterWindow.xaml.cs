using NovaGames.Core.Exceptions;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.UI.Services;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace NovaGames.UI.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly AuthApiService _authService;
        private readonly ILogger _logger;
        private bool _isProcessing = false;
        private readonly INavigationService _navigationService;

        public RegisterWindow(AuthApiService authService, ILogger logger, INavigationService navigationService)
        {
            InitializeComponent();
            _authService = authService;
            _logger = logger;
            _navigationService = navigationService;

            // Логіка перетягування вікна
            this.MouseDown += Window_MouseDown;
            this.Loaded += (s, e) => { txtUsername.Focus(); CheckCapsLock(); };
            this.KeyDown += Window_KeyDown;
        }

        // Перетягування та зняття фокусу
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {

            if (_authService == null) return;

            _navigationService.NavigateTo<LoginWindow>(this);
        }

        // Допоміжні властивості для отримання актуального пароля
        private string ActualPassword => txtPassword.Visibility == Visibility.Visible ? txtPassword.Password : txtPasswordVisible.Text;
        private string ActualConfirmPassword => txtConfirmPassword.Visibility == Visibility.Visible ? txtConfirmPassword.Password : txtConfirmPasswordVisible.Text;

        // Анімація кнопки під час обробки
        private async Task AnimateButtonTextAsync()
        {
            int dotCount = 0;
            while (_isProcessing)
            {
                btnRegister.Content = $"ЗАЧЕКАЙТЕ{new string('.', dotCount)}";
                dotCount = (dotCount + 1) % 4;
                await Task.Delay(400);
            }
        }

        // ЛОГІКА РЕЄСТРАЦІЇ
        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Блокуємо UI
                lblError.Visibility = Visibility.Hidden;
                _isProcessing = true;
                btnRegister.IsEnabled = false;
                _ = AnimateButtonTextAsync();

                string username = txtUsername.Text;
                string password = ActualPassword;
                string confirmPass = ActualConfirmPassword;

                // Валідація на рівні UI
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    throw new ValidationException("Усі поля є обов'язковими.");
                }

                if (!Regex.IsMatch(username, @"^[a-zA-Z][a-zA-Z0-9_]{3,19}$"))
                {
                    throw new ValidationException("Логін: 4-20 символів. Лише латина, цифри та '_'. Перша - літера.");
                }

                if (password != confirmPass)
                {
                    throw new ValidationException("Паролі не співпадають.");
                }

                if (password.Length < 8 ||
                    !password.Any(char.IsUpper) ||
                    !password.Any(char.IsLower) ||
                    !password.Any(char.IsDigit))
                {
                    throw new ValidationException("Пароль: мін. 8 символів, велика і мала літери, та цифра.");
                }

                // Йдемо на сервер
                bool isRegistered = await _authService.RegisterAsync(username, password);

                if (isRegistered)
                {
                    // 2. УСПІХ: Безшовний перехід
                    _ = _logger?.LogAsync($"Користувач успішно пройшов флоу реєстрації: {username}", LogType.INFO);
                    ShowMessage("Обліковий запис успішно створено! Переходимо...", isError: false);

                    // Чекаємо 1.5 секунди, щоб користувач побачив зелене повідомлення
                    await Task.Delay(1500);

                    _navigationService.NavigateTo<LoginWindow>(this);
                }
                else
                {
                    _ = _logger?.LogAsync($"Спроба реєстрації на вже існуючий логін: {username}", LogType.WARNING);
                    ShowMessage("Користувач з таким ім'ям вже існує.", isError: true);
                }
            }
            catch (ValidationException ex)
            {
                ShowMessage(ex.Message, isError: true);
            }
            catch (Exception ex)
            {
                ShowMessage("Помилка підключення до сервера.", isError: true);
                _ = _logger?.LogAsync($"Registration error: {ex.Message}", LogType.ERROR);
            }
            finally
            {
                _isProcessing = false;
                btnRegister.IsEnabled = true;
                btnRegister.Content = "ЗАРЕЄСТРУВАТИСЯ";
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            lblError.Text = message;
            lblError.Foreground = isError
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));

            lblError.Visibility = Visibility.Visible;
        }

        // Допоміжні методи для перемикання видимості пароля
        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (btnTogglePassword.IsChecked == true)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
            }
        }

        private void BtnToggleConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            if (btnToggleConfirmPassword.IsChecked == true)
            {
                txtConfirmPasswordVisible.Text = txtConfirmPassword.Password;
                txtConfirmPassword.Visibility = Visibility.Collapsed;
                txtConfirmPasswordVisible.Visibility = Visibility.Visible;
            }
            else
            {
                txtConfirmPassword.Password = txtConfirmPasswordVisible.Text;
                txtConfirmPasswordVisible.Visibility = Visibility.Collapsed;
                txtConfirmPassword.Visibility = Visibility.Visible;
            }
        }

        // Допоміжний метод для обробки натискання клавіш
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // 1. Оновлюємо статус Caps Lock при кожному натисканні будь-якої клавіші
            CheckCapsLock();

            // 2. Якщо натиснуто Enter і кнопка входу не заблокована - логінимось
            if (e.Key == Key.Enter && btnRegister.IsEnabled)
            {
                BtnRegister_Click(this, new RoutedEventArgs());
            }
        }

        // Допоміжний метод для перевірки статусу Caps Lock
        private void CheckCapsLock()
        {
            lblCapsLock.Visibility = Keyboard.IsKeyToggled(Key.CapsLock) ? Visibility.Visible : Visibility.Hidden;
        }
    }
}