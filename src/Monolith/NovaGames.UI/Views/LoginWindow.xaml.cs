using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.Core.Services;
using NovaGames.UI.Services;
using NovaGames.UI.Services.Api;

namespace NovaGames.UI.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthApiService _authService;
        private readonly ILogger _logger;
        private readonly UserSession _userSession;
        private readonly TokenStorageService _tokenStorage;
        private readonly INavigationService _navigationService;

        private DispatcherTimer? _countdownTimer;
        private int _remainingSeconds;

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
                btnLogin.Content = $"Вхід{new string('.', dotCount)}";
                dotCount = (dotCount + 1) % 4;
                await Task.Delay(400);
            }
        }

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

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblError.Visibility = Visibility.Hidden;

                if (_authService == null)
                {
                    _ = _logger?.LogAsync("Помилка: AuthApiService не знайдено (DI failed).", LogType.ERROR);
                    ShowMessage("Сервіс авторизації недоступний.", true);
                    return;
                }

                _isProcessing = true;
                btnLogin.IsEnabled = false;
                _ = AnimateButtonTextAsync();

                string username = txtUsername.Text;
                string password = ActualPassword;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    ShowMessage("Введіть ім'я користувача та пароль.", true);
                    _isProcessing = false;
                    btnLogin.IsEnabled = true;
                    btnLogin.Content = "Увійти";
                    return;
                }

                var authResult = await _authService.AuthenticateAsync(username, password);

                if (authResult != null && authResult.User != null)
                {
                    _ = _logger?.LogAsync($"Користувач успішно увійшов: {username}", LogType.INFO);
                    ShowMessage("Успішний вхід! Завантаження...", false);
                    await Task.Delay(800);

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
                    _ = _logger?.LogAsync($"Невірна спроба входу для: {username}", LogType.WARNING);
                    ShowMessage("Невірне ім'я користувача або пароль.", true);
                }
            }
            catch (RateLimitExceededException ex)
            {
                StartRateLimitCountdown(ex.RetryAfterSeconds);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
                _ = _logger?.LogAsync($"Login Error: {ex.Message}", LogType.ERROR);
            }
            finally
            {
                if (_countdownTimer == null || !_countdownTimer.IsEnabled)
                {
                    if (btnLogin.IsEnabled == false && lblError.Foreground is SolidColorBrush brush && brush.Color != (Color)ColorConverter.ConvertFromString("#00E676"))
                    {
                        _isProcessing = false;
                        btnLogin.IsEnabled = true;
                        btnLogin.Content = "Увійти";
                    }
                }
            }
        }

        private void StartRateLimitCountdown(int seconds)
        {
            _remainingSeconds = seconds > 0 ? seconds : 60;
            _isProcessing = false;
            btnLogin.IsEnabled = false;

            _countdownTimer?.Stop();
            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += (s, e) =>
            {
                _remainingSeconds--;
                if (_remainingSeconds <= 0)
                {
                    _countdownTimer.Stop();
                    btnLogin.IsEnabled = true;
                    btnLogin.Content = "Увійти";
                    lblError.Visibility = Visibility.Hidden;
                }
                else
                {
                    ShowMessage($"🚫 Забагато спроб входу! Зачекайте {_remainingSeconds} с.", true);
                    btnLogin.Content = $"Блокування ({_remainingSeconds}с)";
                }
            };

            ShowMessage($"🚫 Забагато спроб входу! Зачекайте {_remainingSeconds} с.", true);
            btnLogin.Content = $"Блокування ({_remainingSeconds}с)";
            _countdownTimer.Start();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (_authService == null) return;
            _navigationService.NavigateTo<RegisterWindow>(this);
        }

        private void ShowMessage(string message, bool isError)
        {
            lblError.Text = message;
            lblError.Foreground = isError
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
            lblError.Visibility = Visibility.Visible;
        }

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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            CheckCapsLock();
            if (e.Key == Key.Enter && btnLogin.IsEnabled)
            {
                BtnLogin_Click(this, new RoutedEventArgs());
            }
        }

        private void CheckCapsLock()
        {
            lblCapsLock.Visibility = Keyboard.IsKeyToggled(Key.CapsLock) ? Visibility.Visible : Visibility.Hidden;
        }
    }
}