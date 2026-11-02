using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Services;
using NovaGames.Core.Models;
using NovaGames.UI.Services;
using NovaGames.UI.Services.Api;
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
                    // 1. РЕЄСТРУЄМО БАЗОВІ СЕРВІСИ
                    services.AddSingleton<ILogger, JsonLogger>();
                    services.AddSingleton<UserSession>();
                    services.AddSingleton<TokenStorageService>();
                    services.AddSingleton<INavigationService, WindowNavigationService>();
                    services.AddSingleton<GameInstallerService>();

                    // 2. ЄДИНИЙ КЛІЄНТСЬКИЙ ШАР ДЛЯ РОБОТИ ЧЕРЕЗ API GATEWAY (ЛР4)
                    services.AddSingleton<GatewayConfig>();
                    services.AddTransient<AuthHandler>();
                    services.AddHttpClient<IGatewayApiClient, GatewayApiClient>((sp, client) =>
                    {
                        var config = sp.GetRequiredService<GatewayConfig>();
                        client.BaseAddress = new Uri(config.BaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(10);
                    }).AddHttpMessageHandler<AuthHandler>();

                    // 3. Сервіси моноліту для зворотної сумісності
                    services.AddSingleton<AuthApiService>();
                    services.AddSingleton<GameApiService>();
                    services.AddSingleton<UserApiService>();
                    services.AddSingleton<CartApiService>();

                    // 4. РЕЄСТРУЄМО ВІКНА
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
                var userSession = AppHost.Services.GetRequiredService<UserSession>();
                var authService = AppHost.Services.GetRequiredService<AuthApiService>();

                // 2. Зчитуємо токени із захищеного сховища
                var (token, refreshToken) = tokenStorage.GetTokens();

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refreshToken))
                {
                    await _appLogger.LogAsync("Знайдено збережені токени. Спроба автологіну...", LogType.INFO);

                    userSession.Token = token;
                    userSession.RefreshToken = refreshToken;

                    bool refreshed = await authService.RefreshTokensAsync();

                    if (refreshed)
                    {
                        await _appLogger.LogAsync($"Автологін успішний! Вітаємо, {userSession.CurrentUser?.Username}.", LogType.INFO);

                        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                        mainWindow.Show();
                        return;
                    }
                    else
                    {
                        await _appLogger.LogAsync("Токен застарів і не зміг оновитися. Очищаємо сесію.", LogType.WARNING);
                        tokenStorage.ClearToken();
                    }
                }

                // 3. Якщо автологін не вдався - показуємо вікно логіну
                var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                await _appLogger!.LogAsync($"Помилка під час ініціалізації додатку: {ex.Message}", LogType.ERROR);
                var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
                loginWindow.Show();
            }
        }

        private async void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (_appLogger != null)
            {
                await _appLogger.LogAsync($"НЕПЕРЕХОПЛЕНА КРИТИЧНА ПОМИЛКА: {e.Exception.Message}\nСтек: {e.Exception.StackTrace}", LogType.ERROR);
            }

            MessageBox.Show(
                "Сталася неочікувана помилка в роботі додатку. Ми записали деталі у журнал.",
                "Критична помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost != null)
            {
                await AppHost.StopAsync();
                AppHost.Dispose();
            }
            base.OnExit(e);
        }
    }
}
