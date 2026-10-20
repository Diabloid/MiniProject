using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.Core.Services;
using NovaGames.UI.Models.DTOs;
using NovaGames.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NovaGames.UI.Views
{
    public partial class GameDetailsView : UserControl
    {
        private readonly GameApiService _gameService;
        private readonly UserSession _userSession;
        private readonly UserApiService _userApiService;
        private readonly CartApiService _cartService;
        private readonly ILogger _logger;
        private Game? _currentGame;

        private DispatcherTimer _slideshowTimer;
        private List<string> _screenshotUrls = new List<string>();

        private readonly GameApiService _gameApiService;
        private int _currentRating = 0;
        private int? _editingReviewId = null;
        public ObservableCollection<ReviewResponseDto> ReviewsList { get; set; } = new ObservableCollection<ReviewResponseDto>();

        public GameDetailsView()
        {
            InitializeComponent();
            InitTimer();

            _gameService = App.AppHost!.Services.GetRequiredService<GameApiService>();
            _userSession = App.AppHost!.Services.GetRequiredService<UserSession>();
            _userApiService = App.AppHost!.Services.GetRequiredService<UserApiService>();
            _cartService = App.AppHost!.Services.GetRequiredService<CartApiService>();
            _logger = App.AppHost!.Services.GetRequiredService<ILogger>();
            _gameApiService = App.AppHost!.Services.GetRequiredService<GameApiService>();
            listReviews.ItemsSource = ReviewsList;
            this.DataContextChanged += GameDetailsView_DataContextChanged;
        }

        private async void GameDetailsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is NovaGames.Core.Entities.Game currentGame)
            {
                txtReviewInput.Text = string.Empty;
                _currentRating = 0;
                _editingReviewId = null;
                btnSubmitReview.Content = "ВІДПРАВИТИ";
                HighlightStars(0);

                var libraryGames = await _userApiService.GetLibraryGamesAsync();
                currentGame.IsOwned = libraryGames.Any(g => g.Id == currentGame.Id);

                await LoadReviewsAsync(currentGame.Id);
            }
        }

        // Цей метод викликається з StoreView, коли користувач вибирає гру. Він відповідає за завантаження даних і оновлення UI
        public async Task LoadGameDataAsync(int gameId)
        {
            try
            {
                MainGrid.Visibility = Visibility.Hidden;
                this.DataContext = null;                // Миттєво вбиваємо всі старі тексти та ціни
                listScreenshots.ItemsSource = null;     // Вбиваємо старі картинки знизу
                _slideshowTimer?.Stop();                // ЗУПИНЯЄМО ТАЙМЕР СТАРОЇ ГРИ

                // 1. Отримуємо свіжі дані з сервера
                _currentGame = await _gameService.GetGameByIdAsync(gameId);

                if (_currentGame == null)
                {
                    _ = _logger?.LogAsync($"API повернуло null для гри ID:{gameId}", LogType.WARNING);
                    MessageBox.Show("Не вдалося завантажити дані гри з сервера.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _cartService.LoadMyCartIdsAsync();
                _currentGame.IsInCart = _cartService.CartGameIds.Contains(_currentGame.Id);

                // 2. Встановлюємо DataContext для біндингів у XAML (Title, Price, Genres і т.д.)
                this.DataContext = _currentGame;

                // 3. Логіка галереї скріншотів
                if (_currentGame.FullScreenshotPaths.Any())
                {
                    _screenshotUrls = _currentGame.FullScreenshotPaths;
                    listScreenshots.ItemsSource = _screenshotUrls;

                    listScreenshots.SelectedIndex = 0;

                    _slideshowTimer.Start();
                }
                else
                {
                    _slideshowTimer.Stop();
                    listScreenshots.ItemsSource = null;
                    SetMainScreenshot(_currentGame.ImagePath);
                }

                // 4. Скидання видимості панелей характеристик
                txtDetailsTitle.Text = "ДЕТАЛІ";
                panelDigital.Visibility = Visibility.Collapsed;
                panelPhysical.Visibility = Visibility.Collapsed;
                panelSubscription.Visibility = Visibility.Collapsed;
                btnAddToCart.IsEnabled = true;
                btnAddToCart.Content = "ДОДАТИ В КОШИК";

                var brandBlue = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ADEF"));
                var errorRed = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF5555"));

                // 5. Поліморфна обробка деталей гри
                switch (_currentGame)
                {
                    case DigitalGame dg:
                        txtDetailsTitle.Text = "ХАРАКТЕРИСТИКИ";
                        panelDigital.Visibility = Visibility.Visible;

                        if (dg.Requirements != null && (!string.IsNullOrEmpty(dg.Requirements.Minimum.OS) || !string.IsNullOrEmpty(dg.Requirements.Recommended.OS)))
                        {
                            borderRequirements.Visibility = Visibility.Visible;

                            // Мінімальні
                            lblMinOS.Text = string.IsNullOrEmpty(dg.Requirements.Minimum.OS) ? "-" : dg.Requirements.Minimum.OS;
                            lblMinProcessor.Text = string.IsNullOrEmpty(dg.Requirements.Minimum.Processor) ? "-" : dg.Requirements.Minimum.Processor;
                            lblMinMemory.Text = string.IsNullOrEmpty(dg.Requirements.Minimum.Memory) ? "-" : dg.Requirements.Minimum.Memory;
                            lblMinGraphics.Text = string.IsNullOrEmpty(dg.Requirements.Minimum.Graphics) ? "-" : dg.Requirements.Minimum.Graphics;
                            lblMinStorage.Text = string.IsNullOrEmpty(dg.Requirements.Minimum.Storage) ? "-" : dg.Requirements.Minimum.Storage;

                            // Рекомендовані
                            lblRecOS.Text = string.IsNullOrEmpty(dg.Requirements.Recommended.OS) ? "-" : dg.Requirements.Recommended.OS;
                            lblRecProcessor.Text = string.IsNullOrEmpty(dg.Requirements.Recommended.Processor) ? "-" : dg.Requirements.Recommended.Processor;
                            lblRecMemory.Text = string.IsNullOrEmpty(dg.Requirements.Recommended.Memory) ? "-" : dg.Requirements.Recommended.Memory;
                            lblRecGraphics.Text = string.IsNullOrEmpty(dg.Requirements.Recommended.Graphics) ? "-" : dg.Requirements.Recommended.Graphics;
                            lblRecStorage.Text = string.IsNullOrEmpty(dg.Requirements.Recommended.Storage) ? "-" : dg.Requirements.Recommended.Storage;
                        }
                        else
                        {
                            // Якщо вимог у базі немає (наприклад, стара гра), ховаємо табличку
                            borderRequirements.Visibility = Visibility.Collapsed;
                        }
                        break;

                    case PhysicalGame pg:
                        panelPhysical.Visibility = Visibility.Visible;
                        txtWeight.Text = $"Вага: {pg.ShippingWeight} кг";

                        if (pg.StockQuantity <= 0)
                        {
                            txtStock.Text = "Немає в наявності";
                            txtStock.Foreground = errorRed;
                            btnAddToCart.IsEnabled = false;
                            btnAddToCart.Content = "РОЗПРОДАНО";
                        }
                        else
                        {
                            txtStock.Text = $"В наявності: {pg.StockQuantity} шт.";
                            txtStock.Foreground = brandBlue;
                        }
                        break;

                    case SubscriptionGame sg:
                        panelSubscription.Visibility = Visibility.Visible;
                        txtDuration.Text = $"Тривалість: {sg.DurationMonths} міс.";
                        txtAutoRenew.Text = sg.IsAutoRenew ? "Автоподовження: Увімкнено" : "Автоподовження: Вимкнено";
                        break;
                }

                MainGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Збій завантаження деталей гри ID:{gameId}. Деталі: {ex.Message}", LogType.ERROR);
                MessageBox.Show("Відбулася критична помилка при завантаженні гри.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetMainScreenshot(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // Якщо картинка вже готова (з кешу)
                if (!bitmap.IsDownloading)
                {
                    imgMainScreenshot.Source = bitmap;
                }
                else
                {
                    // Чекаємо завантаження, щоб не було сірого фону
                    bitmap.DownloadCompleted += (s, e) =>
                    {
                        imgMainScreenshot.Dispatcher.Invoke(() => imgMainScreenshot.Source = bitmap);
                    };
                }
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Не вдалося завантажити зображення: {url}. Помилка: {ex.Message}", LogType.WARNING);
            }
        }

        private void ListScreenshots_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listScreenshots.SelectedItem is string url)
            {
                SetMainScreenshot(url);
                ResetTimer();
            }
        }

        private void NextScreenshot()
        {
            if (_screenshotUrls.Count <= 1) return;
            int nextIndex = listScreenshots.SelectedIndex + 1;
            listScreenshots.SelectedIndex = nextIndex >= _screenshotUrls.Count ? 0 : nextIndex;
        }

        private void PrevScreenshot()
        {
            if (_screenshotUrls.Count <= 1) return;
            int prevIndex = listScreenshots.SelectedIndex - 1;
            listScreenshots.SelectedIndex = prevIndex < 0 ? _screenshotUrls.Count - 1 : prevIndex;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e) => NextScreenshot();
        private void BtnPrev_Click(object sender, RoutedEventArgs e) => PrevScreenshot();

        private void ResetTimer()
        {
            _slideshowTimer.Stop();
            _slideshowTimer.Start();
        }

        private void InitTimer()
        {
            _slideshowTimer = new DispatcherTimer();
            _slideshowTimer.Interval = TimeSpan.FromSeconds(5); // Кожні 5 секунд
            _slideshowTimer.Tick += (s, e) => NextScreenshot();
        }

        private void Screenshot_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is string url)
            {
                SetMainScreenshot(url);
            }
        }

        // ОБРОБНИК ДЛЯ КНОПКИ "ДОДАТИ В КОШИК"
        private async void BtnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Game selectedGame)
            {
                btn.Content = "ДОДАВАННЯ...";
                btn.IsEnabled = false;

                bool success = await _cartService.AddToCartAsync(selectedGame.Id);

                if (success)
                {
                    selectedGame.IsInCart = true;
                }
                else
                {
                    btn.Content = "ДОДАТИ В КОШИК";
                    btn.IsEnabled = true;
                    _ = _logger?.LogAsync($"Невдала спроба додати гру ID:{selectedGame.Id} в кошик зі сторінки деталей", LogType.WARNING);
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
            if (sender is Button btn && btn.DataContext is NovaGames.Core.Entities.Game selectedGame)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToLibrary(selectedGame.Id);
            }
        }

        // ЛОГІКА ВІДГУКІВ

        private async Task LoadReviewsAsync(int gameId)
        {
            var reviews = await _gameApiService.GetReviewsForGameAsync(gameId);
            var currentUsername = _userSession.CurrentUser?.Username;

            ReviewsList.Clear();
            foreach (var review in reviews)
            {
                review.IsMine = (review.Username == currentUsername);
            }

            var sortedReviews = reviews.OrderByDescending(r => r.IsMine).ToList();
            foreach (var review in sortedReviews)
            {
                ReviewsList.Add(review);
            }

            if (this.DataContext is NovaGames.Core.Entities.Game currentGame)
            {
                currentGame.TotalReviews = ReviewsList.Count;

                if (ReviewsList.Any())
                {
                    currentGame.AverageRating = Math.Round(ReviewsList.Average(r => (double)r.Rating), 1);
                }
                else
                {
                    currentGame.AverageRating = 0.0;
                }
            }
        }

        private void Star_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && int.TryParse(rb.Tag.ToString(), out int rating))
            {
                _currentRating = rating;
                HighlightStars(_currentRating);
            }
        }

        private void Star_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is RadioButton rb && int.TryParse(rb.Tag.ToString(), out int hoverRating))
            {
                HighlightStars(hoverRating);
            }
        }

        private void Star_MouseLeave(object sender, MouseEventArgs e)
        {
            HighlightStars(_currentRating);
        }

        private void HighlightStars(int rating)
        {
            var gold = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFB900"));
            var gray = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#444444"));

            star1.Foreground = rating >= 1 ? gold : gray;
            star2.Foreground = rating >= 2 ? gold : gray;
            star3.Foreground = rating >= 3 ? gold : gray;
            star4.Foreground = rating >= 4 ? gold : gray;
            star5.Foreground = rating >= 5 ? gold : gray;
        }

        private async void BtnSubmitReview_Click(object sender, RoutedEventArgs e)
        {
            if (!(this.DataContext is NovaGames.Core.Entities.Game currentGame)) return;

            if (_currentRating < 1 || _currentRating > 5)
            {
                var warningDialog = new ConfirmWindow("УВАГА", "Будь ласка, оберіть оцінку (від 1 до 5 зірочок).", "ЗРОЗУМІЛО", "", false, "#F39C12") { Owner = Window.GetWindow(this) };
                warningDialog.ShowDialog();
                return;
            }

            string comment = txtReviewInput.Text?.Trim();
            btnSubmitReview.IsEnabled = false;
            btnSubmitReview.Content = "ОБРОБКА...";

            bool success;

            if (_editingReviewId.HasValue)
            {
                var updateDto = new UpdateReviewDto { Rating = _currentRating, Text = comment };
                success = await _gameApiService.UpdateReviewAsync(_editingReviewId.Value, updateDto);
            }
            else
            {
                var createDto = new CreateReviewDto { ProductId = currentGame.Id, Rating = _currentRating, Text = comment };
                success = await _gameApiService.AddReviewAsync(createDto);
            }

            if (success)
            {
                var successDialog = new ConfirmWindow("УСПІХ", _editingReviewId.HasValue ? "Відгук успішно оновлено!" : "Дякуємо! Ваш відгук успішно додано.", "ЧУДОВО", "", false, "#2ECC71") { Owner = Window.GetWindow(this) };
                successDialog.ShowDialog();

                txtReviewInput.Text = string.Empty;
                _currentRating = 0;
                _editingReviewId = null;
                HighlightStars(0);

                await LoadReviewsAsync(currentGame.Id);
            }
            else
            {
                var errorDialog = new ConfirmWindow("ПОМИЛКА", "Не вдалося зберегти відгук.", "ЗРОЗУМІЛО", "", false, "#FF5555") { Owner = Window.GetWindow(this) };
                errorDialog.ShowDialog();
            }

            btnSubmitReview.IsEnabled = true;
            btnSubmitReview.Content = "ВІДПРАВИТИ";
        }

        private void BtnEditReview_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ReviewResponseDto review)
            {
                _editingReviewId = review.Id;
                txtReviewInput.Text = review.Text;
                _currentRating = review.Rating;
                HighlightStars(_currentRating);

                btnSubmitReview.Content = "ЗБЕРЕГТИ ЗМІНИ";
                txtReviewInput.Focus();
            }
        }

        private async void BtnDeleteReview_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ReviewResponseDto review)
            {
                var confirmDialog = new ConfirmWindow(
                    header: "ВИДАЛЕННЯ ВІДГУКУ",
                    message: "Ви впевнені, що хочете назавжди видалити цей відгук?",
                    confirmBtnText: "ВИДАЛИТИ",
                    warningText: "Цю дію неможливо буде скасувати.",
                    showCancelButton: true,
                    headerColorHex: "#FF5555")
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() != true) return;

                bool success = await _gameApiService.DeleteReviewAsync(review.Id);

                if (success)
                {
                    if (_editingReviewId == review.Id)
                    {
                        txtReviewInput.Text = string.Empty;
                        _currentRating = 0;
                        _editingReviewId = null;
                        HighlightStars(0);
                        btnSubmitReview.Content = "ВІДПРАВИТИ";
                    }

                    int gameId = 0;
                    if (this is GameDetailsView)
                    {
                        gameId = (this.DataContext as NovaGames.Core.Entities.Game)?.Id ?? 0;
                    }

                    if (gameId != 0) await LoadReviewsAsync(gameId);
                }
                else
                {
                    var errorDialog = new ConfirmWindow("ПОМИЛКА", "Не вдалося видалити відгук.", "ЗРОЗУМІЛО", "", false, "#FF5555") { Owner = Window.GetWindow(this) };
                    errorDialog.ShowDialog();
                }
            }
        }
    }
}