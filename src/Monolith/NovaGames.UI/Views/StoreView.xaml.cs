using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.UI.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NovaGames.UI.Views
{
    public partial class StoreView : UserControl
    {
        private readonly GameApiService _gameService;
        private readonly CartApiService _cartService;
        private readonly UserApiService _userService;
        private readonly ILogger _logger;
        private List<Game> _allGames = new List<Game>();

        private string _currentCategory = "All";
        private string _currentSort = "Default";
        private string _currentSearch = "";

        public StoreView()
        {
            InitializeComponent();

            _gameService = App.AppHost!.Services.GetRequiredService<GameApiService>();
            _cartService = App.AppHost!.Services.GetRequiredService<CartApiService>();
            _userService = App.AppHost!.Services.GetRequiredService<UserApiService>();
            _logger = App.AppHost!.Services.GetRequiredService<ILogger>();
            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible)
                {
                    _ = LoadGamesFromServerAsync();
                }
            };
        }

        // Цей метод завантажує всі ігри з сервера і зберігає їх у _allGames, потім застосовує фільтри та сортування
        private async Task LoadGamesFromServerAsync()
        {
            try
            {
                _allGames = await _gameService.GetAllGamesAsync();

                // Завантажуємо кошик і бібліотеку
                await _cartService.LoadMyCartIdsAsync();
                var libraryGames = await _userService.GetLibraryGamesAsync();

                // Створюємо швидкий HashSet для перевірки куплених ігор
                var ownedGameIds = libraryGames.Select(g => g.Id).ToHashSet();

                foreach (var game in _allGames)
                {
                    game.IsInCart = _cartService.CartGameIds.Contains(game.Id);
                    game.IsOwned = ownedGameIds.Contains(game.Id);
                }

                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync($"Критичний збій завантаження вітрини: {ex.Message}", LogType.ERROR);

                MessageBox.Show($"Не вдалося завантажити ігри: {ex.Message}");
            }
        }

        // Цей метод застосовує поточні фільтри та сортування до списку ігор і оновлює UI
        private void ApplyFiltersAndSort()
        {
            if (listGames == null) return;

            var filteredGames = _allGames.AsEnumerable();

            if (_currentCategory != "All")
            {
                filteredGames = filteredGames.Where(g =>
                    (_currentCategory == "Digital" && g is DigitalGame) ||
                    (_currentCategory == "Physical" && g is PhysicalGame) ||
                    (_currentCategory == "Subscription" && g is SubscriptionGame));
            }

            if (!string.IsNullOrWhiteSpace(_currentSearch))
            {
                filteredGames = filteredGames.Where(g => g.Title.Contains(_currentSearch, StringComparison.OrdinalIgnoreCase));
            }

            var finalGamesList = SortGames(filteredGames.ToList());

            Application.Current.Dispatcher.Invoke(() =>
            {
                listGames.ItemsSource = finalGamesList;
                if (txtResultsCount != null)
                {
                    string ending = GetGameDeclension(finalGamesList.Count);
                    txtResultsCount.Text = $"{finalGamesList.Count} {ending}";
                }
            });
        }

        // Метод для сортування ігор відповідно до вибраного критерію
        private List<Game> SortGames(List<Game> list)
        {
            return _currentSort switch
            {
                "Ціною (від низької до високої)" => list.OrderBy(g => g.FinalPrice).ToList(),
                "Ціною (від високої до низької)" => list.OrderByDescending(g => g.FinalPrice).ToList(),
                "Назвою (А-Я)" => list.OrderBy(g => g.Title).ToList(),
                _ => list
            };
        }

        // Цей метод викликається з SearchBar, коли користувач вводить текст
        public void UpdateSearch(string searchText)
        {
            _currentSearch = searchText;
            ApplyFiltersAndSort();
        }

        // MainWindow буде викликати його, щоб отримати ігри для Popup
        public List<Game> GetSearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Game>();

            return _allGames
                .Where(g => g.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();
        }

        // Обробники подій для фільтрів та сортування
        private void Category_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                string clickedCategory = rb.Tag?.ToString() ?? "All";

                if (_currentCategory == clickedCategory)
                {
                    rb.IsChecked = false;
                    _currentCategory = "All";
                }
                else
                {
                    _currentCategory = clickedCategory;
                }

                ApplyFiltersAndSort();
            }
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboSort?.SelectedItem is ComboBoxItem item)
            {
                _currentSort = item.Content?.ToString() ?? "Default";
                ApplyFiltersAndSort();
            }
        }

        // Клік по всій картці гри
        private void Card_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 1. ЗАХИСТ: Перевіряємо, чи не клікнув юзер по якійсь Кнопці (наприклад, "В кошик" чи "✕")
            System.Windows.DependencyObject current = e.OriginalSource as System.Windows.DependencyObject;

            while (current != null)
            {
                if (current is System.Windows.Controls.Button)
                {
                    return;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            // 2. ЯКЩО ЦЕ КЛІК ПО САМІЙ КАРТЦІ — ВІДКРИВАЄМО ГРУ
            if (sender is FrameworkElement element && element.DataContext is NovaGames.Core.Entities.Game selectedGame)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToGameDetails(selectedGame);
            }
        }

        // Обробник для кнопки "В кошик"
        private async void BtnBuy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Game selectedGame)
            {
                btn.Content = "...";
                btn.IsEnabled = false;

                bool success = await _cartService.AddToCartAsync(selectedGame.Id);

                if (success)
                {
                    selectedGame.IsInCart = true;
                }
                else
                {
                    btn.Content = "В КОШИК";
                    btn.IsEnabled = true;
                    _ = _logger.LogAsync($"Невдала спроба додати гру ID:{selectedGame.Id} в кошик", LogType.WARNING);
                    MessageBox.Show("Помилка при додаванні в кошик.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnGoToCart_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToCart();
        }

        private void BtnGoToLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Game selectedGame)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                // Передаємо ID цієї гри в навігатор
                mainWindow?.NavigateToLibrary(selectedGame.Id);
            }
        }

        // Обробник для кліку на обкладинку гри
        private void SmallImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Child is Image smallImage)
            {
                imgPreview.Source = smallImage.Source;
                OverlayGrid.Visibility = Visibility.Visible;
            }
        }

        // Обробник для кліку на оверлей, щоб закрити його
        private void Overlay_MouseDown(object sender, RoutedEventArgs e)
        {
            OverlayGrid.Visibility = Visibility.Collapsed;
            imgPreview.Source = null;
        }

        // Метод для правильного відмінювання слова "гра" в залежності від кількості
        private string GetGameDeclension(int number)
        {
            if (number % 100 >= 11 && number % 100 <= 19) return "Доступних ігор";
            switch (number % 10)
            {
                case 1: return "Доступна гра";
                case 2: case 3: case 4: return "Доступні гри";
                default: return "Доступних ігор";
            }
        }
    }
}