using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Exceptions;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.Core.Services;
using NovaGames.UI.Services;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NovaGames.UI.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthApiService _authService;    // Додано для роботи з користувачами
        private readonly ILogger _logger;                // Додано для логування
        private readonly UserSession _userSession;       // Додано для сесії користувача
        private readonly TokenStorageService _tokenStorage;
        private readonly INavigationService _navigationService;

        public LoginWindow(AuthApiService authService, ILogger logger, UserSession session, TokenStorageService tokenStorage, INavigationService navigationService)
        {
            InitializeComponent();
            _authService = authService;
            _logger = logger;
            _userSession = session;
            _tokenStorage = tokenStorage;
            _navigationService = navigationService;

            this.MouseDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
            this.Loaded += (s, e) => { txtUsername.Focus(); CheckCapsLock(); };
            this.KeyDown += Window_KeyDown;
        }

        private bool _isProcessing = false;
        private string ActualPassword => txtPassword.Visibility == Visibility.Visible ? txtPassword.Password : txtPasswordVisible.Text;

        private async Task AnimateButtonTextAsync()
        {
            int dotCount = 0;
            while (_isProcessing)
            {
                btnLogin.Content = $"ЗАЧЕКАЙТЕ{new string('.', dotCount)}";
                dotCount = (dotCount + 1) % 4;
                await Task.Delay(400);
            }
        }

        // --- ЛОГІКА UI ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 1. Очищення фокусу з полів вводу
            Keyboard.ClearFocus();

            // 2. Логіка перетягування вікна
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // --- ЛОГІКА АВТОРИЗАЦІЇ ---

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Скидаємо помилки
                lblError.Visibility = Visibility.Hidden;

                if (_authService == null)
                {
                    _ = _logger?.LogAsync("КРИТИЧНО: AuthApiService не був ініціалізований (DI failed).", LogType.ERROR);
                    ShowMessage("Сервіс даних недоступний.", true);
                    return;
                }

                _isProcessing = true;
                btnLogin.IsEnabled = false;
                _ = AnimateButtonTextAsync();

                string username = txtUsername.Text;
                string password = ActualPassword;

                // 1. Валідація вводу
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    ShowMessage("Введіть ім'я користувача та пароль.", true);
                    _isProcessing = false;
                    btnLogin.IsEnabled = true;
                    btnLogin.Content = "УВІЙТИ";
                    return;
                }

                // 2. Спроба входу
                var authResult = await _authService.AuthenticateAsync(username, password);

                if (authResult != null && authResult.User != null)
                {
                    _ = _logger?.LogAsync($"Користувач успішно увійшов: {username}", LogType.INFO);
                    ShowMessage("Успішний вхід! Завантаження...", false);
                    await Task.Delay(1500);

                    if (chkRememberMe.IsChecked == true)
                    {
                        _tokenStorage.SaveTokens(authResult.Token, authResult.RefreshToken);
                    }

                    _userSession.CurrentUser = authResult.User;
                    _userSession.Token = authResult.Token;
                    _userSession.RefreshToken = authResult.RefreshToken;
                    _navigationService.NavigateTo<MainWindow>(this);
                }
                else
                {
                    _ = _logger?.LogAsync($"Невдала спроба входу для логіну: {username}", LogType.WARNING);
                    ShowMessage("Невірне ім'я користувача або пароль.", true);
                }
            }
            // 3. Обробка помилок
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
                _ = _logger?.LogAsync($"Login Error: {ex.Message}", LogType.ERROR);
            }
            finally
            {
                if (btnLogin.IsEnabled == false && lblError.Foreground is SolidColorBrush brush && brush.Color != (Color)ColorConverter.ConvertFromString("#00E676"))
                {
                    _isProcessing = false;
                    btnLogin.IsEnabled = true;
                    btnLogin.Content = "УВІЙТИ";
                }
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {

            if (_authService == null) return;

            _navigationService.NavigateTo<RegisterWindow>(this);
        }

        // Допоміжний метод для виводу повідомлень користувачу
        private void ShowMessage(string message, bool isError)
        {
            lblError.Text = message;
            lblError.Foreground = isError
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
            lblError.Visibility = Visibility.Visible;
        }

        // Допоміжний метод для перемикання видимості пароля
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

        // Допоміжний метод для обробки натискання клавіш
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // 1. Оновлюємо статус Caps Lock при кожному натисканні будь-якої клавіші
            CheckCapsLock();

            // 2. Якщо натиснуто Enter і кнопка входу не заблокована - логінимось
            if (e.Key == Key.Enter && btnLogin.IsEnabled)
            {
                BtnLogin_Click(this, new RoutedEventArgs());
            }
        }

        // Допоміжний метод для перевірки статусу Caps Lock
        private void CheckCapsLock()
        {
            lblCapsLock.Visibility = Keyboard.IsKeyToggled(Key.CapsLock) ? Visibility.Visible : Visibility.Hidden;
        }
    }
}