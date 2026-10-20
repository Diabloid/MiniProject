using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace NovaGames.UI.Services
{
    // Реалізація пульта
    public class WindowNavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowNavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo<TDestination>(Window currentWindow) where TDestination : Window
        {
            var targetWindow = _serviceProvider.GetRequiredService<TDestination>();

            // Тільки якщо ми переходимо в MainWindow, робимо його на весь екран
            if (targetWindow is MainWindow)
            {
                targetWindow.WindowState = WindowState.Maximized;
            }
            else
            {
                // Для логіну та реєстрації ставимо Normal
                targetWindow.WindowState = WindowState.Normal;
                targetWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            targetWindow.Show();
            currentWindow?.Close();
        }
    }
}