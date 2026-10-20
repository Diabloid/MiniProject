using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Services;
using NovaGames.Core.Models;
using NovaGames.UI.Services;
using NovaGames.UI.Views;
using System.Windows;

namespace NovaGames.UI
{
    public partial class App : Application
    {
        // Головний контейнер програми (DI Container)
        public static IHost? AppHost { get; private set; }

        // Зберігаємо логер окремо для обробки аварійних помилок
        private ILogger? _appLogger;

        public App()
        {
            // Налаштування DI контейнера
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // 1. РЕЄСТРУЄМО СЕРВІСИ
                    services.AddSingleton<ILogger, JsonLogger>();
                    services.AddSingleton<UserSession>();
                    services.AddSingleton<AuthApiService>();
                    services.AddSingleton<GameApiService>();
                    services.AddSingleton<TokenStorageService>();
                    services.AddSingleton<INavigationService, WindowNavigationService>();
                    services.AddSingleton<UserApiService>();
                    services.AddSingleton<CartApiService>();
                    services.AddSingleton<GameInstallerService>();

                    // 2. РЕЄСТРУЄМО ВІКНА
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<RegisterWindow>();
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Глобальний перехоплювач помилок
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Запускаємо хост
            await AppHost!.StartAsync();

            try
            {
                _appLogger = AppHost.Services.GetRequiredService<ILogger>();
                await _appLogger.LogAsync("Application Host started successfully.", LogType.INFO);

                // 1. Дістаємо сервіси для перевірки токенів
                var tokenStorage = AppHost.Services.GetRequiredService<TokenStorageService>();
                var authService = AppHost.Services.GetRequiredService<AuthApiService>();
                var userSession = AppHost.Services.GetRequiredService<UserSession>();
                var navigationService = AppHost.Services.GetRequiredService<INavigationService>();

                // 2. Отримуємо збережені токени
                var (savedToken, savedRefreshToken) = tokenStorage.GetTokens();

                if (!string.IsNullOrEmpty(savedToken) && !string.IsNullOrEmpty(savedRefreshToken))
                {
                    await _appLogger.LogAsync("Found saved tokens. Attempting auto-login...", LogType.INFO);

                    // Метод RefreshTokensAsync читає токени з сесії, тому ми спочатку кладемо зашифровані токени з диску в сесію:
                    userSession.Token = savedToken;
                    userSession.RefreshToken = savedRefreshToken;

                    // Тепер викликаємо метод (БЕЗ АРГУМЕНТІВ), і він поверне true або false
                    bool isSuccess = await authService.RefreshTokensAsync();

                    if (isSuccess)
                    {
                        await _appLogger.LogAsync($"Auto-login successful for user: {userSession.CurrentUser?.Username}", LogType.INFO);

                        // Відкриваємо MainWindow одразу в максимізованому стані
                        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                        mainWindow.WindowState = WindowState.Maximized;
                        mainWindow.Show();
                        return; // Виходимо, щоб не відкривати LoginWindow
                    }
                    else
                    {
                        // Якщо токен прострочений або сервер відхилив - чистимо сесію
                        userSession.Token = null;
                        userSession.RefreshToken = null;
                    }
                }

                // 3. Якщо автологін не вдався або токенів немає — відкриваємо LoginWindow
                await _appLogger.LogAsync("Auto-login skipped or failed. Showing LoginWindow.", LogType.INFO);
                var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                await _appLogger?.LogAsync($"CRITICAL STARTUP ERROR: {ex.Message}", LogType.ERROR);
                MessageBox.Show($"Критична помилка під час запуску: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost != null)
            {
                await AppHost.StopAsync();
            }
            base.OnExit(e);
        }

        // Цей метод ловить ВСІ помилки, які ми забули огорнути в try-catch
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (_appLogger != null)
            {
                _ = _appLogger.LogAsync($"UNHANDLED EXCEPTION: {e.Exception.Message}\nStack Trace: {e.Exception.StackTrace}", LogType.ERROR);
            }

            MessageBox.Show($"Сталася неочікувана помилка: {e.Exception.Message}", "Ауч!", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}