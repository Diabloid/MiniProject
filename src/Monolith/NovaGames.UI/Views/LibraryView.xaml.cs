using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.Core.Services;
using NovaGames.UI.Models.DTOs;
using NovaGames.UI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Wpf.Ui.Input;

namespace NovaGames.UI.Views
{
    public partial class LibraryView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly UserApiService _userApiService;
        private readonly ILogger _logger;
        private int? _pendingGameIdToSelect = null;
        private readonly GameInstallerService _installerService;
        private readonly GameApiService _gameApiService;
        private int _currentRating = 0;
        private string _currentAverageRating = "0.0";
        private int? _editingReviewId = null;
        private readonly UserSession _userSession;

        public string CurrentAverageRating
        {
            get => _currentAverageRating;
            set { _currentAverageRating = value; OnPropertyChanged(nameof(CurrentAverageRating)); }
        }

        private double _downloadPercentage;
        public double DownloadPercentage
        {
            get => _downloadPercentage;
            set { _downloadPercentage = value; OnPropertyChanged(nameof(DownloadPercentage)); }
        }

        private string _downloadStatusText = "Підготовка...";
        public string DownloadStatusText
        {
            get => _downloadStatusText;
            set { _downloadStatusText = value; OnPropertyChanged(nameof(DownloadStatusText)); }
        }

        private ObservableCollection<ReviewResponseDto> _reviewsList = new ObservableCollection<ReviewResponseDto>();
        public ObservableCollection<ReviewResponseDto> ReviewsList
        {
            get => _reviewsList;
            set { _reviewsList = value; OnPropertyChanged(nameof(ReviewsList)); }

        }

        public ObservableCollection<LibraryGameDto> LibraryGames { get; set; } = new ObservableCollection<LibraryGameDto>();

        public LibraryView()
        {
            InitializeComponent();
            DataContext = this;
            ActionCommand = new RelayCommand(ExecuteGameAction);
            listLibraryGames.SelectionChanged += ListLibraryGames_SelectionChanged;
            ListLibraryGames_SelectionChanged(null, null);

            _userApiService = App.AppHost!.Services.GetRequiredService<UserApiService>();
            _userSession = App.AppHost!.Services.GetRequiredService<UserSession>();
            _logger = App.AppHost!.Services.GetRequiredService<ILogger>();
            _installerService = App.AppHost!.Services.GetRequiredService<GameInstallerService>();

            _gameApiService = App.AppHost!.Services.GetRequiredService<GameApiService>();

            this.Loaded += LibraryView_Loaded;
        }

        private async void LibraryView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLibraryAsync();
        }

        public async Task LoadLibraryAsync()
        {
            try
            {
                var games = await _userApiService.GetLibraryGamesAsync();

                LibraryGames.Clear();
                foreach (var game in games)
                {
                    game.IsInstalled = _installerService.IsGameInstalled(game.Title);

                    LibraryGames.Add(game);
                }

                // Отримуємо спеціальне представлення колекції
                ICollectionView view = CollectionViewSource.GetDefaultView(LibraryGames);

                // 1. Групування
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

                // 2. Встановлюємо наш власний метод фільтрації
                view.Filter = LibraryFilter;

                listLibraryGames.ItemsSource = view;

                // 3. Застосовуємо сортування при завантаженні
                ApplySorting();

                if (_pendingGameIdToSelect.HasValue)
                {
                    PerformSelection(_pendingGameIdToSelect.Value);
                    _pendingGameIdToSelect = null; // Скидаємо, щоб не виділялося при наступних оновленнях
                }
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Критична помилка завантаження бібліотеки: {ex.Message}", LogType.ERROR);
                MessageBox.Show("Не вдалося завантажити вашу бібліотеку ігор. Перевірте з'єднання з сервером.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // СТЕЙТ-МАШИНА ТА ВЛАСТИВОСТІ
        public enum GameInstallState
        {
            NotInstalled,
            Downloading,
            ReadyToPlay,
            Running,
            UpdateAvailable
        }

        private LibraryGameDto _selectedGame;
        public LibraryGameDto SelectedGame
        {
            get => _selectedGame;
            set { _selectedGame = value; OnPropertyChanged(nameof(SelectedGame)); }
        }

        private GameInstallState _currentInstallState;
        public GameInstallState CurrentInstallState
        {
            get => _currentInstallState;
            set
            {
                _currentInstallState = value;
                UpdateStateUI();
                OnPropertyChanged(nameof(CurrentInstallState));
            }
        }

        private string _actionButtonText = "ЗАВАНТАЖИТИ";
        public string ActionButtonText
        {
            get => _actionButtonText;
            set { _actionButtonText = value; OnPropertyChanged(nameof(ActionButtonText)); }
        }

        public ICommand ActionCommand { get; private set; }


        // ================= ЛОГІКА ФІЛЬТРАЦІЇ =================
        private bool LibraryFilter(object obj)
        {
            if (obj is LibraryGameDto game)
            {
                // 1. Перевірка кнопки "Тільки встановлені"
                if (btnFilterInstalled.IsChecked == true && !game.IsInstalled)
                    return false;

                // 2. Перевірка поля Пошуку (завжди порівнюємо в нижньому регістрі)
                string searchText = txtSearch.Text?.Trim().ToLower();
                if (!string.IsNullOrEmpty(searchText) && !game.Title.ToLower().Contains(searchText))
                    return false;

                // Якщо гра пройшла всі перевірки — показуємо її!
                return true;
            }
            return false;
        }

        // Коли користувач щось друкує в пошуку
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Просто кажемо колекції "оновися", і вона сама прожене всі ігри через метод LibraryFilter
            if (listLibraryGames?.ItemsSource != null)
                CollectionViewSource.GetDefaultView(LibraryGames).Refresh();
        }

        // Коли натиснуто або відтиснуто кнопку "Встановлені"
        private void BtnFilterInstalled_Changed(object sender, RoutedEventArgs e)
        {
            if (listLibraryGames?.ItemsSource != null)
                CollectionViewSource.GetDefaultView(LibraryGames).Refresh();
        }

        // ================= ЛОГІКА СОРТУВАННЯ =================
        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySorting();
        }

        private void ApplySorting()
        {
            if (listLibraryGames?.ItemsSource == null || comboSort == null) return;

            ICollectionView view = CollectionViewSource.GetDefaultView(LibraryGames);
            view.SortDescriptions.Clear();

            switch (comboSort.SelectedIndex)
            {
                case 0: // Останні зіграні (Спочатку ті, де дата найбільша/найновіша)
                    view.SortDescriptions.Add(new SortDescription("LastPlayedAt", ListSortDirection.Descending));
                    break;
                case 1: // Назвою (А-Я)
                    view.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));
                    break;
                case 2: // Назвою (Я-А)
                    view.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Descending));
                    break;
                case 3: // Розміром (від більшого до меншого)
                    view.SortDescriptions.Add(new SortDescription("SizeGB", ListSortDirection.Descending));
                    break;
                case 4: // Датою придбання (Спочатку найновіші покупки)
                    view.SortDescriptions.Add(new SortDescription("AcquiredAt", ListSortDirection.Descending));
                    break;
            }
        }

        // ================= ЛОГІКА КАТЕГОРІЙ =================

        // 1. Динамічне створення меню для ГРИ
        // 1. Динамічне створення меню для ГРИ
        private void GameItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is LibraryGameDto game)
            {
                var menu = new ContextMenu();

                // Витягуємо твої стилі з XAML
                var menuStyle = this.TryFindResource("DarkContextMenuStyle") as Style;
                var itemStyle = this.TryFindResource("DarkMenuItemStyle") as Style;
                var separatorStyle = this.TryFindResource("DarkSeparatorStyle") as Style;

                if (menuStyle != null) menu.Style = menuStyle;

                // Це змусить всі вкладені пункти автоматично підхопити темний стиль
                if (itemStyle != null) menu.ItemContainerStyle = itemStyle;

                // СТВОРЮЄМО БАТЬКІВСЬКИЙ ПУНКТ (І тепер ОБОВ'ЯЗКОВО даємо йому стиль!)
                var addToMenuItem = new MenuItem { Header = "Додати в категорію..." };
                if (itemStyle != null)
                {
                    addToMenuItem.Style = itemStyle;
                    addToMenuItem.ItemContainerStyle = itemStyle; // Щоб підменю теж було темним
                }

                var existingCategories = LibraryGames.Select(g => g.Category)
                                                     .Distinct()
                                                     .Where(c => c != "Без категорії")
                                                     .ToList();

                foreach (var cat in existingCategories)
                {
                    var catItem = new MenuItem { Header = cat };
                    if (itemStyle != null) catItem.Style = itemStyle; // На всякий випадок задаємо прямо

                    catItem.Click += async (s, args) => await ChangeGameCategoryAsync(game, cat);
                    addToMenuItem.Items.Add(catItem);
                }

                if (game.Category != "Без категорії")
                {
                    if (existingCategories.Any())
                    {
                        var sep1 = new Separator();
                        if (separatorStyle != null) sep1.Style = separatorStyle;
                        addToMenuItem.Items.Add(sep1);
                    }

                    var removeCatItem = new MenuItem { Header = "Прибрати з категорії" };
                    if (itemStyle != null) removeCatItem.Style = itemStyle;

                    removeCatItem.Click += async (s, args) => await ChangeGameCategoryAsync(game, "Без категорії");
                    addToMenuItem.Items.Add(removeCatItem);
                }

                var sep2 = new Separator();
                if (separatorStyle != null) sep2.Style = separatorStyle;
                addToMenuItem.Items.Add(sep2);

                if (_installerService.IsGameInstalled(game.Title))
                {
                    var uninstallItem = new MenuItem { Header = "Видалити з комп'ютера", Foreground = System.Windows.Media.Brushes.IndianRed };
                    if (itemStyle != null) uninstallItem.Style = itemStyle;

                    uninstallItem.Click += (s, args) =>
                    {
                        var dialog = new ConfirmWindow(
                            "ВИДАЛЕННЯ ГРИ",
                            $"Ви дійсно хочете видалити гру\n«{game.Title}» з вашого комп'ютера?\nЗбереження гри залишаться на диску.",
                            "ВИДАЛИТИ")
                        {
                            Owner = Window.GetWindow(this)
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            try
                            {
                                _installerService.UninstallGame(game.Title);
                                game.IsInstalled = false;

                                // Якщо ми видаляємо ту гру, яка зараз відкрита на екрані детального перегляду
                                if (SelectedGame != null && SelectedGame.Id == game.Id)
                                {
                                    CurrentInstallState = GameInstallState.NotInstalled;
                                }

                                CollectionViewSource.GetDefaultView(LibraryGames).Refresh();
                            }
                            catch (Exception ex)
                            {
                                // Тут можна залишити MessageBox, бо це системний краш (наприклад, екзешник ще висить у процесах), 
                                // або теж замінити на твоє кастомне вікно помилки, якщо воно в тебе є
                                MessageBox.Show($"Не вдалося видалити гру. Переконайтеся, що вона не запущена.\nПомилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    };

                    menu.Items.Add(uninstallItem);

                    var sep3 = new Separator();
                    if (separatorStyle != null) sep3.Style = separatorStyle;
                    menu.Items.Add(sep3);
                }

                var createCatItem = new MenuItem { Header = "+ ДОДАТИ КАТЕГОРІЮ", Foreground = System.Windows.Media.Brushes.DeepSkyBlue };
                if (itemStyle != null) createCatItem.Style = itemStyle;

                createCatItem.Click += async (s, args) => await CreateNewCategoryAsync(game);
                addToMenuItem.Items.Add(createCatItem);

                menu.Items.Add(addToMenuItem);

                item.ContextMenu = menu;
                menu.IsOpen = true; // Примусове відкриття з першого кліку
                e.Handled = true;
            }
        }

        // 2. Метод зміни категорії для гри
        private async Task ChangeGameCategoryAsync(LibraryGameDto game, string newCategory)
        {
            try
            {
                bool success = await _userApiService.ChangeGameCategoryAsync(game.Id, newCategory);
                if (success)
                {
                    game.Category = newCategory;
                    CollectionViewSource.GetDefaultView(LibraryGames).Refresh();
                }
                else
                {
                    _ = _logger?.LogAsync($"API відмовило у зміні категорії для гри ID:{game.Id} на '{newCategory}'", LogType.WARNING);
                }
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Збій під час зміни категорії для гри ID:{game.Id}: {ex.Message}", LogType.ERROR);
            }
        }

        // 3. Метод створення нової категорії з прив'язкою гри
        private async Task CreateNewCategoryAsync(LibraryGameDto game)
        {
            var dialog = new InputWindow("Введіть назву нової категорії:") { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                await ChangeGameCategoryAsync(game, dialog.InputText);
            }
        }

        // 4. Перейменування цілої категорії
        private async void RenameCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is CollectionViewGroup group)
            {
                string oldCategory = group.Name.ToString();

                // Забороняємо перейменовувати дефолтну категорію
                if (oldCategory == "Без категорії") return;

                var dialog = new InputWindow("Введіть нову назву категорії:", oldCategory) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText) && dialog.InputText != oldCategory)
                {
                    try
                    {
                        bool success = await _userApiService.RenameCategoryAsync(oldCategory, dialog.InputText);
                        if (success)
                        {
                            // Оновлюємо категорію у всіх іграх локально
                            foreach (var game in LibraryGames.Where(g => g.Category == oldCategory))
                            {
                                game.Category = dialog.InputText;
                            }
                            CollectionViewSource.GetDefaultView(LibraryGames).Refresh();
                        }
                        else
                        {
                            // АНОМАЛІЯ: API не дозволило перейменування
                            _ = _logger?.LogAsync($"API відмовило у перейменуванні категорії з '{oldCategory}' на '{dialog.InputText}'", LogType.WARNING);
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = _logger?.LogAsync($"Збій під час перейменування категорії '{oldCategory}': {ex.Message}", LogType.ERROR);
                        MessageBox.Show("Сталася технічна помилка під час перейменування.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Публічний метод, який викликає MainWindow
        public void SelectGame(int gameId)
        {
            // ЗАВЖДИ запам'ятовуємо ID! Навіть якщо ігри вже є. 
            // Якщо зараз відпрацює подія Loaded (через перехід між вкладками), 
            // вона очистить список, але в кінці знову виділить гру завдяки цій змінній.
            _pendingGameIdToSelect = gameId;

            // Пробуємо виділити одразу (якщо ми викликали метод, не перемикаючи вкладку)
            if (LibraryGames.Any())
            {
                PerformSelection(gameId);
            }
        }

        // Внутрішній метод, який робить магію в інтерфейсі
        private void PerformSelection(int gameId)
        {
            var targetGame = LibraryGames.FirstOrDefault(g => g.Id == gameId);
            if (targetGame != null)
            {
                // 1. Виділяємо візуально
                listLibraryGames.SelectedItem = targetGame;

                // 2. Магія WPF: чекаємо, поки рендер-потік (UI Thread) закінчить 
                // малювати всі групи та контейнери, і тільки тоді крутимо список!
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    listLibraryGames.UpdateLayout(); // Примусово просимо WPF перерахувати розміри
                    listLibraryGames.ScrollIntoView(targetGame);
                }, System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }

        // ================= ЛОГІКА ДЕТАЛЕЙ ГРИ =================

        private async void ListLibraryGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listLibraryGames.SelectedItem is LibraryGameDto selected)
            {
                SelectedGame = selected;

                // ДИНАМІЧНА ПЕРЕВІРКА ВСТАНОВЛЕННЯ ЧЕРЕЗ ФАЙЛОВУ СИСТЕМУ
                bool isLocallyInstalled = _installerService.IsGameInstalled(selected.Title);
                CurrentInstallState = isLocallyInstalled ? GameInstallState.ReadyToPlay : GameInstallState.NotInstalled;

                ((StackPanel)panelGameDetails.Children[0]).Visibility = Visibility.Collapsed;
                ((ScrollViewer)panelGameDetails.Children[1]).Visibility = Visibility.Visible;

                txtReviewInput.Text = string.Empty;
                _currentRating = 0;
                HighlightStars(0);

                await LoadReviewsAsync(selected.Id);
            }
            else
            {
                SelectedGame = null;
                ReviewsList.Clear();
                if (panelGameDetails.Children.Count > 1)
                {
                    ((StackPanel)panelGameDetails.Children[0]).Visibility = Visibility.Visible;
                    ((ScrollViewer)panelGameDetails.Children[1]).Visibility = Visibility.Collapsed;
                }
            }
        }

        private void UpdateStateUI()
        {
            // Цей метод автоматично змінює текст кнопки при зміні стану гри
            switch (CurrentInstallState)
            {
                case GameInstallState.NotInstalled:
                    ActionButtonText = "ЗАВАНТАЖИТИ";
                    break;
                case GameInstallState.Downloading:
                    ActionButtonText = "ВСТАНОВЛЕННЯ...";
                    break;
                case GameInstallState.ReadyToPlay:
                    ActionButtonText = "ГРАТИ";
                    break;
                case GameInstallState.Running:
                    ActionButtonText = "ЗАПУЩЕНО";
                    break;
                case GameInstallState.UpdateAvailable:
                    ActionButtonText = "ОНОВИТИ";
                    break;
            }
        }

        private async void ExecuteGameAction(object obj)
        {
            if (SelectedGame == null) return;

            if (CurrentInstallState == GameInstallState.NotInstalled)
            {
                CurrentInstallState = GameInstallState.Downloading;

                // Налаштовуємо IProgress для оновлення UI з іншого потоку
                var progressIndicator = new Progress<double>(value => DownloadPercentage = value);
                var statusIndicator = new Progress<string>(text => DownloadStatusText = text);

                try
                {
                    // Викликаємо наш сервіс викачування та розпакування!
                    await _installerService.DownloadAndInstallAsync(SelectedGame.DownloadUrl, SelectedGame.Title, progressIndicator, statusIndicator);

                    CurrentInstallState = GameInstallState.ReadyToPlay;
                    SelectedGame.IsInstalled = true; // Локально помічаємо
                    CollectionViewSource.GetDefaultView(LibraryGames).Refresh(); // Оновлюємо фільтри
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Збій встановлення {SelectedGame.Title}: {ex.Message}", LogType.ERROR);
                    MessageBox.Show($"Помилка встановлення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentInstallState = GameInstallState.NotInstalled; // Повертаємо стан у разі помилки
                }
            }
            else if (CurrentInstallState == GameInstallState.ReadyToPlay)
            {
                CurrentInstallState = GameInstallState.Running;

                try
                {
                    // 1. Засікаємо час початку
                    var sessionStartTime = DateTime.Now;

                    // Оновлюємо дату останнього запуску локально для UI
                    SelectedGame.LastPlayedAt = sessionStartTime;

                    // 2. Запускаємо гру і чекаємо, поки потік гри не завершиться (гравець не вийде)
                    await _installerService.LaunchGameAsync(SelectedGame.Title);

                    // 3. Гра закрилася! Вираховуємо, скільки секунд минали
                    var sessionDuration = DateTime.Now - sessionStartTime;
                    SelectedGame.PlaytimeSeconds += (long)sessionDuration.TotalSeconds;

                    // Примусово змушуємо UI оновити статистику на екрані (перепризначаючи змінну)
                    var tempGame = SelectedGame;
                    SelectedGame = null;
                    SelectedGame = tempGame;

                    // TODO (в майбутньому): Відправити ці нові дані (PlaytimeSeconds та LastPlayedAt) на API, щоб вони збереглися в БД!
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Помилка запуску {SelectedGame.Title}: {ex.Message}", LogType.ERROR);
                    MessageBox.Show($"Не вдалося запустити гру: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    CurrentInstallState = GameInstallState.ReadyToPlay;
                }
            }
        }

        private void BtnGoToStore_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGame == null) return;

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var gameToPass = new NovaGames.Core.Entities.DigitalGame
                {
                    Id = SelectedGame.Id
                };

                mainWindow.NavigateToGameDetails(gameToPass);
            }
        }

        // ЛОГІКА ВІДГУКІВ

        // Завантаження відгуків з бекенду
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

            if (ReviewsList.Any())
            {
                double avg = ReviewsList.Average(r => (double)r.Rating);
                CurrentAverageRating = avg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                CurrentAverageRating = "0.0";
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
                HighlightStars(hoverRating); // Підсвічуємо до тієї, на яку навели
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

        // Відправка відгуку
        private async void BtnSubmitReview_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGame == null) return;

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

            // РЕДАГУВАННЯ ЧИ СТВОРЕННЯ
            if (_editingReviewId.HasValue)
            {
                // РЕДАГУВАННЯ
                var updateDto = new UpdateReviewDto { Rating = _currentRating, Text = comment };
                success = await _gameApiService.UpdateReviewAsync(_editingReviewId.Value, updateDto);
            }
            else
            {
                // СТВОРЕННЯ
                var createDto = new CreateReviewDto { ProductId = SelectedGame.Id, Rating = _currentRating, Text = comment };
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

                await LoadReviewsAsync(SelectedGame.Id);
            }
            else
            {
                var errorDialog = new ConfirmWindow("ПОМИЛКА", "Не вдалося зберегти відгук. Перевірте з'єднання або спробуйте пізніше.", "ЗРОЗУМІЛО", "", false, "#FF5555") { Owner = Window.GetWindow(this) };
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

                    if (SelectedGame != null)
                    {
                        await LoadReviewsAsync(SelectedGame.Id);
                    }
                }
                else
                {
                    var errorDialog = new ConfirmWindow("ПОМИЛКА", "Не вдалося видалити відгук.", "ЗРОЗУМІЛО", "", false, "#FF5555") { Owner = Window.GetWindow(this) };
                    errorDialog.ShowDialog();
                }
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }
}