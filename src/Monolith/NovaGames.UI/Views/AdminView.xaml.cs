using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.Core.Services;
using NovaGames.UI.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace NovaGames.UI.Views
{
    public partial class AdminView : UserControl
    {
        private readonly GameApiService _gameService;
        private readonly UserApiService _userService;
        private readonly UserSession _userSession;
        private readonly ILogger _logger;
        private List<Game> _allGames = new List<Game>();
        private List<User> _allUsers = new List<User>();
        private string? _selectedCoverFilePath = null;
        private string? _selectedIconFilePath = null;
        private string? _selectedZipFilePath = null;
        private List<string> _selectedScreenshotPaths = new List<string>();
        private Game? _editingGame = null;
        private User? _selectedUser = null;

        public AdminView()
        {
            InitializeComponent();
            _gameService = App.AppHost!.Services.GetRequiredService<GameApiService>();
            _userService = App.AppHost!.Services.GetRequiredService<UserApiService>();
            _userSession = App.AppHost!.Services.GetRequiredService<UserSession>();
            _logger = App.AppHost!.Services.GetRequiredService<ILogger>();

            this.Loaded += (s, e) =>
            {
                if (rbTabGames.IsChecked == true) MoveIndicatorTo(rbTabGames);
                else if (rbTabUsers.IsChecked == true) MoveIndicatorTo(rbTabUsers);
            };

            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible)
                {
                    if (rbTabGames.IsChecked == true)
                    {
                        _ = LoadGamesAsync();
                    }
                    else if (rbTabUsers.IsChecked == true)
                    {
                        _ = LoadUsersAsync();
                    }
                }
            };
        }

        // ЗАВАНТАЖЕННЯ ДАНИХ З СЕРВЕРА
        private async Task LoadGamesAsync()
        {
            try
            {
                _allGames = await _gameService.GetAllGamesAsync(true);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(_allGames);
                    view.Filter = AdminGamesFilter;
                    listAdminGames.ItemsSource = view;
                });
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Помилка завантаження списку ігор для адмінки: {ex.Message}", LogType.ERROR);
                MessageBox.Show($"Помилка завантаження ігор: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var usersEnum = await _userService.GetAllUsersAsync();
                _allUsers = usersEnum.ToList(); // Зберігаємо локально для фільтрації

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Підключаємо фільтрацію
                    ICollectionView view = CollectionViewSource.GetDefaultView(_allUsers);
                    view.Filter = AdminUsersFilter;
                    listAdminUsers.ItemsSource = view;
                });
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Помилка завантаження користувачів: {ex.Message}", LogType.ERROR);
                MessageBox.Show($"Помилка завантаження користувачів: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ЛОГІКА ПОШУКУ В АДМІНЦІ

        private void TxtSearchGames_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (listAdminGames?.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(_allGames).Refresh();
            }
        }

        private bool AdminGamesFilter(object obj)
        {
            if (obj is Game game)
            {
                string searchText = txtSearchGames.Text?.Trim().ToLower();
                if (string.IsNullOrEmpty(searchText)) return true;

                // Шукаємо за назвою гри
                return game.Title.ToLower().Contains(searchText);
            }
            return false;
        }

        private void TxtSearchUsers_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (listAdminUsers?.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(_allUsers).Refresh();
            }
        }

        private bool AdminUsersFilter(object obj)
        {
            if (obj is User user)
            {
                string searchText = txtSearchUsers.Text?.Trim().ToLower();
                if (string.IsNullOrEmpty(searchText)) return true;


                return user.Username.ToLower().Contains(searchText);
            }
            return false;
        }

        // ЛОГІКА ПЕРЕКЛЮЧЕННЯ ВКЛАДОК (ІГРИ/КОРИСТУВАЧІ)
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (GamesContainer == null || UsersContainer == null) return;

            if (sender is RadioButton rb)
                MoveIndicatorTo(rb);

            if (rbTabGames.IsChecked == true)
            {
                GamesContainer.Visibility = Visibility.Visible;
                UsersContainer.Visibility = Visibility.Collapsed;
            }
            else if (rbTabUsers.IsChecked == true)
            {
                GamesContainer.Visibility = Visibility.Collapsed;
                UsersContainer.Visibility = Visibility.Visible;

                listAdminUsers.ItemsSource = null;
                _ = LoadUsersAsync();
            }
        }

        //ЛОГІКА КНОПОК НА КАРТЦІ ГРИ
        // Метод для кнопки "РЕДАГУВАТИ"
        // Метод для кнопки "РЕДАГУВАТИ"
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Game gameToEdit)
            {
                // 1. НАЙГОЛОВНІШЕ: Очищаємо весь "брудний" стан від попередніх редагувань!
                ClearForm();

                _editingGame = gameToEdit;

                // 2. Заповнюємо текстові поля
                txtTitle.Text = gameToEdit.Title;
                txtPrice.Text = gameToEdit.Price.ToString("0.00");
                txtDiscount.Text = gameToEdit.Discount.ToString("0");
                txtDescription.Text = gameToEdit.Description;
                chkIsPublished.IsChecked = gameToEdit.IsPublished;

                // Використовуємо нову структуру Media, а не старе ImagePath
                txtCoverPath.Text = gameToEdit.Media?.CoverUrl ?? "";

                if (gameToEdit.Genres != null)
                {
                    txtGenres.Text = string.Join(", ", gameToEdit.Genres);
                }

                // 3. ПІДТЯГУЄМО ОБКЛАДИНКУ В ПРЕВ'Ю З СЕРВЕРА
                if (!string.IsNullOrEmpty(gameToEdit.Media?.CoverUrl) && gameToEdit.Media.CoverUrl != "/Assets/default_game.png")
                {
                    try
                    {
                        var coverUri = gameToEdit.Media.CoverUrl.StartsWith("/images/")
                            ? new Uri($"https://localhost:7232{gameToEdit.Media.CoverUrl}", UriKind.Absolute)
                            : new Uri(gameToEdit.Media.CoverUrl, UriKind.RelativeOrAbsolute);

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = coverUri;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Ігноруємо кеш WPF!
                        bitmap.EndInit();

                        imgCoverPreview.Source = bitmap;
                        borderCoverPreview.Visibility = Visibility.Visible;
                    }
                    catch (Exception ex)
                    {
                        _ = _logger?.LogAsync($"Не вдалося завантажити прев'ю обкладинки для редагування. URL: {gameToEdit.Media.CoverUrl}, Помилка: {ex.Message}", LogType.WARNING);
                    }
                }

                // --- ПІДТЯГУЄМО ІКОНКУ ---
                txtIconPath.Text = gameToEdit.Media?.IconUrl ?? "";
                if (!string.IsNullOrEmpty(gameToEdit.Media?.IconUrl) && gameToEdit.Media.IconUrl != "/Assets/default_icon.png")
                {
                    try
                    {
                        var iconUri = gameToEdit.Media.IconUrl.StartsWith("/images/")
                            ? new Uri($"https://localhost:7232{gameToEdit.Media.IconUrl}", UriKind.Absolute)
                            : new Uri(gameToEdit.Media.IconUrl, UriKind.RelativeOrAbsolute);

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = iconUri;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.EndInit();

                        imgIconPreview.Source = bitmap;
                        borderIconPreview.Visibility = Visibility.Visible;
                    }
                    catch (Exception ex)
                    {
                        _ = _logger?.LogAsync($"Не вдалося завантажити прев'ю іконки для редагування. URL: {gameToEdit.Media.IconUrl}, Помилка: {ex.Message}", LogType.WARNING);
                    }
                }

                // 4. ПІДТЯГУЄМО СКРІНШОТИ В ПРЕВ'Ю З СЕРВЕРА
                if (gameToEdit.Media?.Screenshots != null && gameToEdit.Media.Screenshots.Any())
                {
                    txtSelectedScreenshotsCount.Text = $"На сервері: {gameToEdit.Media.Screenshots.Count}";
                    txtSelectedScreenshotsCount.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 173, 239));

                    // === ДОДАЄМО ЦІ ДВА РЯДКИ ===
                    // Витягуємо тільки назви файлів з довгих серверних шляхів
                    var fileNames = gameToEdit.Media.Screenshots.Select(s => System.IO.Path.GetFileName(s)).ToList();
                    // Вписуємо перші 3 файли в TextBox, а далі ставимо "..."
                    txtScreenshotsPath.Text = string.Join("; ", fileNames.Take(3)) + (fileNames.Count > 3 ? " ..." : "");
                    // ============================

                    // Формуємо повні посилання для WPF, щоб він зміг їх завантажити по HTTP
                    var fullScreenshotUrls = gameToEdit.Media.Screenshots.Select(url =>
                        url.StartsWith("/images/") ? $"https://localhost:7232{url}" : url
                    ).ToList();

                    listScreenshotsPreview.ItemsSource = fullScreenshotUrls;
                }

                // 5. Підтягування специфічних полів
                if (gameToEdit is DigitalGame dg)
                {
                    comboGameType.SelectedIndex = 0; // Digital
                    txtDownloadSize.Text = dg.DownloadSize.ToString();
                    txtGameZipUrl.Text = dg.GameZipUrl;

                    if (dg.Requirements != null)
                    {
                        txtMinOS.Text = dg.Requirements.Minimum?.OS;
                        txtMinProcessor.Text = dg.Requirements.Minimum?.Processor;
                        txtMinMemory.Text = dg.Requirements.Minimum?.Memory;
                        txtMinGraphics.Text = dg.Requirements.Minimum?.Graphics;
                        txtMinStorage.Text = dg.Requirements.Minimum?.Storage;

                        txtRecOS.Text = dg.Requirements.Recommended?.OS;
                        txtRecProcessor.Text = dg.Requirements.Recommended?.Processor;
                        txtRecMemory.Text = dg.Requirements.Recommended?.Memory;
                        txtRecGraphics.Text = dg.Requirements.Recommended?.Graphics;
                        txtRecStorage.Text = dg.Requirements.Recommended?.Storage;
                    }
                }
                else if (gameToEdit is PhysicalGame pg)
                {
                    comboGameType.SelectedIndex = 1; // Physical
                    txtShippingWeight.Text = pg.ShippingWeight.ToString();
                    txtStockQuantity.Text = pg.StockQuantity.ToString();
                }
                else if (gameToEdit is SubscriptionGame sg)
                {
                    comboGameType.SelectedIndex = 2; // Subscription
                    txtDurationMonths.Text = sg.DurationMonths.ToString();
                    chkAutoRenew.IsChecked = sg.IsAutoRenew;
                }

                // 6. Оновлення візуалу кнопок та заголовків
                txtFormTitle.Text = "РЕДАГУВАННЯ ГРИ";
                txtFormTitle.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFB900"));
                btnSubmitGame.Content = "ЗБЕРЕГТИ ОНОВЛЕННЯ";
                btnCancelEdit.Visibility = Visibility.Visible;
            }
        }

        // Метод для кнопки "ВИДАЛИТИ"
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Game gameToDelete)
            {
                // 1. ЗАПОБІЖНИК: ПЕРЕВІРКА НАЯВНОСТІ ВЛАСНИКІВ
                btn.IsEnabled = false;

                try
                {
                    bool hasOwners = await _gameService.HasGameOwnersAsync(gameToDelete.Id);

                    if (hasOwners)
                    {
                        var warningDialog = new ConfirmWindow(
                            "ВИДАЛЕННЯ ЗАБОРОНЕНО",
                            $"Гру «{gameToDelete.Title}» вже придбали користувачі.\n\nБудь ласка, зніміть гру з продажу (приховайте) замість видалення.",
                            "ЗРОЗУМІЛО",
                            "",
                            false,
                            "#F39C12")
                        {
                            Owner = Window.GetWindow(this)
                        };

                        warningDialog.ShowDialog();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Помилка перевірки власників гри ID:{gameToDelete.Id}. Деталі: {ex.Message}", LogType.ERROR);
                    MessageBox.Show("Не вдалося перевірити статус гри в базі даних.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                finally
                {
                    btn.IsEnabled = true;
                }

                // 2. СТАНДАРТНЕ ВИДАЛЕННЯ (якщо власників немає)
                var dialog = new ConfirmWindow(
                                "ПІДТВЕРДЖЕННЯ ВИДАЛЕННЯ",
                                $"Ви дійсно хочете назавжди видалити гру\n«{gameToDelete.Title}» з бази даних?",
                                "ВИДАЛИТИ")
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        await _gameService.DeleteGameAsync(gameToDelete.Id);
                        _ = _logger?.LogAsync($"Адміністратор {_userSession.CurrentUser?.Username} видалив гру ID:{gameToDelete.Id}", LogType.INFO);

                        if (_editingGame != null && _editingGame.Id == gameToDelete.Id)
                        {
                            ClearForm();
                        }

                        await LoadGamesAsync();
                    }
                    catch (Exception ex)
                    {
                        _ = _logger?.LogAsync($"Критична помилка при видаленні гри ID:{gameToDelete.Id}. Деталі: {ex.Message}", LogType.ERROR);
                        MessageBox.Show($"Не вдалося видалити гру: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // ЛОГІКА БОКОВОЇ ПАНЕЛІ (ФОРМИ) КЕРУВАННЯ ІГРАМИ (CRUD)
        // Метод для кнопки "ДОДАТИ/ЗБЕРЕГТИ"
        private async void BtnSubmitGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnSubmitGame.IsEnabled = false;
                btnSubmitGame.Content = "ЗБЕРЕЖЕННЯ...";

                // Завантаження файлів (якщо вибрано)
                string finalCoverPath = txtCoverPath.Text;

                if (!string.IsNullOrEmpty(_selectedCoverFilePath))
                {
                    finalCoverPath = await _gameService.UploadFileAsync(_selectedCoverFilePath, txtTitle.Text, "cover");
                }

                // ФОРМУВАННЯ ОБ'ЄКТА ГРИ
                var selectedType = (comboGameType.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                Game newGame;

                if (selectedType == "Physical")
                {
                    newGame = new PhysicalGame
                    {
                        ShippingWeight = double.TryParse(txtShippingWeight.Text, out var w) ? w : 0,
                        StockQuantity = int.TryParse(txtStockQuantity.Text, out var sq) ? sq : 0
                    };
                }
                else if (selectedType == "Subscription")
                {
                    newGame = new SubscriptionGame
                    {
                        DurationMonths = int.TryParse(txtDurationMonths.Text, out var dm) ? dm : 1,
                        IsAutoRenew = chkAutoRenew.IsChecked ?? false
                    };
                }
                else // Digital (За замовчуванням)
                {
                    newGame = new DigitalGame
                    {
                        DownloadSize = double.TryParse(txtDownloadSize.Text, out var ds) ? ds : 0,

                        GameZipUrl = (_editingGame as DigitalGame)?.GameZipUrl ?? string.Empty,

                        Requirements = new GameRequirements
                        {
                            Minimum = new SystemRequirement
                            {
                                OS = txtMinOS.Text,
                                Processor = txtMinProcessor.Text,
                                Memory = txtMinMemory.Text,
                                Graphics = txtMinGraphics.Text,
                                Storage = txtMinStorage.Text
                            },
                            Recommended = new SystemRequirement
                            {
                                OS = txtRecOS.Text,
                                Processor = txtRecProcessor.Text,
                                Memory = txtRecMemory.Text,
                                Graphics = txtRecGraphics.Text,
                                Storage = txtRecStorage.Text
                            }
                        }
                    };
                }

                // Заповнення спільних властивостей
                newGame.Title = txtTitle.Text;
                newGame.Description = txtDescription.Text;
                newGame.IsPublished = chkIsPublished.IsChecked ?? false;

                // Безпечний парсинг чисел
                newGame.Price = decimal.TryParse(txtPrice.Text, out var p) ? p : 0;
                newGame.Discount = decimal.TryParse(txtDiscount.Text, out var d) ? d : 0;

                // Парсинг жанрів (розділення по комі)
                if (!string.IsNullOrWhiteSpace(txtGenres.Text))
                {
                    newGame.Genres = txtGenres.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(g => g.Trim()).ToList();
                }

                newGame.Media = new GameMedia();

                if (!string.IsNullOrEmpty(_selectedCoverFilePath))
                {
                    newGame.Media.CoverUrl = await _gameService.UploadFileAsync(_selectedCoverFilePath, txtTitle.Text, "cover");
                }
                else if (_editingGame != null)
                {
                    newGame.Media.CoverUrl = _editingGame.Media.CoverUrl;
                }
                else
                {
                    newGame.Media.CoverUrl = string.IsNullOrWhiteSpace(txtCoverPath.Text) ? "/Assets/default_game.png" : txtCoverPath.Text;
                }

                // НОВИЙ КОД ДЛЯ ІКОНКИ:
                if (!string.IsNullOrEmpty(_selectedIconFilePath))
                {
                    newGame.Media.IconUrl = await _gameService.UploadFileAsync(_selectedIconFilePath, txtTitle.Text, "icon");
                }
                else if (_editingGame != null)
                {
                    newGame.Media.IconUrl = _editingGame.Media.IconUrl;
                }
                else
                {
                    newGame.Media.IconUrl = string.IsNullOrWhiteSpace(txtIconPath.Text) ? "/Assets/default_icon.png" : txtIconPath.Text;
                }

                if (_selectedScreenshotPaths.Any())
                {
                    foreach (var path in _selectedScreenshotPaths)
                    {
                        string screenUrl = await _gameService.UploadFileAsync(path, txtTitle.Text, "screenshot");
                        if (!string.IsNullOrEmpty(screenUrl))
                        {
                            newGame.Media.Screenshots.Add(screenUrl);
                        }
                    }
                }
                else if (_editingGame != null)
                {
                    newGame.Media.Screenshots = _editingGame.Media.Screenshots;
                }

                // ВІДПРАВКА НА АПІ ТА ОТРИМАННЯ ID ГРИ
                int targetGameId = 0;

                if (_editingGame == null)
                {
                    // СТВОРЕННЯ
                    var createdGame = await _gameService.CreateGameAsync(newGame);
                    if (createdGame != null)
                    {
                        targetGameId = createdGame.Id;
                    }
                    _ = _logger?.LogAsync($"Успішно створено нову гру: {newGame.Title}", LogType.INFO);
                }
                else
                {
                    // ОНОВЛЕННЯ
                    newGame.Id = _editingGame.Id;
                    await _gameService.UpdateGameAsync(newGame.Id, newGame);
                    targetGameId = newGame.Id;
                    _ = _logger?.LogAsync($"Успішно оновлено гру ID:{newGame.Id} ({newGame.Title})", LogType.INFO);
                }

                // ==========================================
                // НАЙГОЛОВНІШЕ: ЗАВАНТАЖУЄМО .ZIP АРХІВ
                // ==========================================
                if (targetGameId > 0 && !string.IsNullOrEmpty(_selectedZipFilePath))
                {
                    btnSubmitGame.Content = "ЗАВАНТАЖЕННЯ АРХІВУ (ЦЕ МОЖЕ ЗАЙНЯТИ ЧАС)...";

                    string serverPath = await _gameService.UploadGameArchiveAsync(targetGameId, _selectedZipFilePath);

                    if (string.IsNullOrEmpty(serverPath))
                    {
                        MessageBox.Show("Гру збережено, але архів не вдалося завантажити.\nМожливо, файл занадто великий або сервер відхилив запит.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                ClearForm();
                await LoadGamesAsync();
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Збій при збереженні гри. Деталі: {ex.Message}", LogType.ERROR);
                MessageBox.Show($"Виникла помилка при збереженні: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSubmitGame.IsEnabled = true;
            }
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Метод, який повертає форму до початкового стану
        private void ClearForm()
        {
            _editingGame = null;

            // Очищаємо текстові поля
            txtTitle.Text = string.Empty;
            txtPrice.Text = string.Empty;
            txtDiscount.Text = "0";
            txtGenres.Text = string.Empty;
            txtDescription.Text = string.Empty;

            // Скидаємо ComboBox
            comboGameType.SelectedIndex = 0;

            // Очищаємо шляхи до файлів
            txtCoverPath.Text = string.Empty;
            txtScreenshotsPath.Text = string.Empty;
            _selectedCoverFilePath = null;
            _selectedScreenshotPaths.Clear();
            _selectedZipFilePath = string.Empty;

            // Очищаємо нові динамічні поля
            txtDownloadSize.Text = string.Empty;
            txtGameZipUrl.Text = string.Empty;
            txtShippingWeight.Text = string.Empty;
            txtStockQuantity.Text = string.Empty;
            txtDurationMonths.Text = string.Empty;
            chkAutoRenew.IsChecked = false;
            chkIsPublished.IsChecked = true;

            // Очищаємо поля системних вимог
            txtMinOS.Text = string.Empty; txtMinProcessor.Text = string.Empty; txtMinMemory.Text = string.Empty; txtMinGraphics.Text = string.Empty; txtMinStorage.Text = string.Empty;
            txtRecOS.Text = string.Empty; txtRecProcessor.Text = string.Empty; txtRecMemory.Text = string.Empty; txtRecGraphics.Text = string.Empty; txtRecStorage.Text = string.Empty;

            // Очищаємо картинки прев'ю
            imgCoverPreview.Source = null;
            borderCoverPreview.Visibility = Visibility.Collapsed;
            listScreenshotsPreview.ItemsSource = null;

            // Скидаємо іконку
            txtIconPath.Text = string.Empty;
            _selectedIconFilePath = null;
            imgIconPreview.Source = null;
            borderIconPreview.Visibility = Visibility.Collapsed;

            // Скидаємо лічильник скріншотів
            txtSelectedScreenshotsCount.Text = "Вибрано: 0";
            txtSelectedScreenshotsCount.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));

            // Повертаємо візуал форми
            txtFormTitle.Text = "ДОДАТИ НОВУ ГРУ";
            txtFormTitle.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ADEF"));

            btnSubmitGame.Content = "ДОДАТИ ГРУ";
            btnCancelEdit.Visibility = Visibility.Collapsed;
        }

        // ЛОГІКА ВИБОРУ МЕДІА (ОБКЛАДИНКА + СКРІНШОТИ)
        // Вибір обкладинки
        private void BtnBrowseCover_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Виберіть обкладинку гри",
                Filter = "Зображення (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedCoverFilePath = openFileDialog.FileName;
                txtCoverPath.Text = openFileDialog.FileName;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(openFileDialog.FileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgCoverPreview.Source = bitmap;
                borderCoverPreview.Visibility = Visibility.Visible;
            }
        }

        // Метод для вибору іконки
        private void BtnBrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Виберіть іконку гри",
                Filter = "Зображення (*.png, *.jpg, *.jpeg, *.ico)|*.png;*.jpg;*.jpeg;*.ico"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedIconFilePath = openFileDialog.FileName;
                txtIconPath.Text = openFileDialog.FileName;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(openFileDialog.FileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgIconPreview.Source = bitmap;
                borderIconPreview.Visibility = Visibility.Visible;
            }
        }

        private void BtnBrowseZip_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Архіви ігор (*.zip)|*.zip|Всі файли (*.*)|*.*",
                Title = "Виберіть .zip архів з грою"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedZipFilePath = openFileDialog.FileName;
                txtGameZipUrl.Text = openFileDialog.FileName;
            }
        }

        // Метод для вибору скріншотів
        private void BtnBrowseScreenshots_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Виберіть скріншоти (до 15 шт.)",
                Filter = "Зображення|*.jpg;*.jpeg;*.png;*.webp",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedScreenshotPaths = openFileDialog.FileNames.Take(15).ToList();

                txtScreenshotsPath.Text = string.Join("; ", openFileDialog.SafeFileNames.Take(3)) + " ...";
                txtSelectedScreenshotsCount.Text = $"Вибрано: {_selectedScreenshotPaths.Count}";
                txtSelectedScreenshotsCount.Foreground = _selectedScreenshotPaths.Count > 0
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 173, 239))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));

                listScreenshotsPreview.ItemsSource = null;
                listScreenshotsPreview.ItemsSource = _selectedScreenshotPaths;
            }
        }

        // Метод для горизонтальної прокрутки коліщатком миші в області скріншотів
        private void ScreenshotsScroll_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }

        // ЛОГІКА КОРИСТУВАЧІВ
        private void ListAdminUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listAdminUsers.SelectedItem is User selectedUser)
            {
                _selectedUser = selectedUser;

                txtNoUserSelected.Visibility = Visibility.Collapsed;
                panelUserActions.Visibility = Visibility.Visible;

                txtSelectedUsername.Text = selectedUser.Username;
                txtSelectedUserRole.Text = selectedUser.Role.ToString().ToUpper();

                btnMakeAdmin.Visibility = Visibility.Collapsed;
                btnMakeModerator.Visibility = Visibility.Collapsed;
                btnRemovePrivileges.Visibility = Visibility.Collapsed;

                if (_userSession.CurrentUser != null && selectedUser.Id == _userSession.CurrentUser.Id)
                {
                    panelUserActions.Visibility = Visibility.Collapsed;
                    txtNoUserSelected.Text = "ВИ НЕ МОЖЕТЕ ЗМІНЮВАТИ ПРАВА ВЛАСНОГО АКАУНТА";
                    txtNoUserSelected.Visibility = Visibility.Visible;
                    return;
                }

                if (selectedUser.Role == UserRole.SuperAdmin)
                {
                    panelUserActions.Visibility = Visibility.Collapsed;
                    txtNoUserSelected.Text = "ВИ НЕ МОЖЕТЕ ЗМІНЮВАТИ ПРАВА СУПЕРАДМІНІСТРАТОРА";
                    txtNoUserSelected.Visibility = Visibility.Visible;
                    return;
                }

                switch (selectedUser.Role)
                {
                    case UserRole.Client:
                        btnMakeAdmin.Visibility = Visibility.Visible;
                        btnMakeModerator.Visibility = Visibility.Visible;
                        break;
                    case UserRole.Moderator:
                        btnMakeAdmin.Visibility = Visibility.Visible;
                        btnRemovePrivileges.Visibility = Visibility.Visible;
                        break;
                    case UserRole.Admin:
                        btnRemovePrivileges.Visibility = Visibility.Visible;
                        break;
                }
            }
            else
            {
                txtNoUserSelected.Text = "ОБЕРІТЬ КОРИСТУВАЧА ЗІ СПИСКУ";
                txtNoUserSelected.Visibility = Visibility.Visible;
                panelUserActions.Visibility = Visibility.Collapsed;
                _selectedUser = null;
            }
        }

        private async void BtnUserAction_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            try
            {
                if (sender == btnMakeAdmin)
                    await _userService.UpdateUserRoleAsync(_selectedUser.Id, UserRole.Admin);
                else if (sender == btnMakeModerator)
                    await _userService.UpdateUserRoleAsync(_selectedUser.Id, UserRole.Moderator);
                else if (sender == btnRemovePrivileges)
                    await _userService.UpdateUserRoleAsync(_selectedUser.Id, UserRole.Client);
                else if (sender == btnBlockUser)
                    await _userService.BlockUserAsync(_selectedUser.Id);

                await LoadUsersAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                _ = _logger?.LogAsync($"Адмін втратив права під час дії з користувачем: {ex.Message}", LogType.WARNING);
                MessageBox.Show(ex.Message, "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Помилка зміни ролі користувача ID:{_selectedUser.Id}. Деталі: {ex.Message}", LogType.ERROR);
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для плавної анімації лінії у вкладках адмінки
        private void MoveIndicatorTo(UIElement target)
        {
            if (target == null || !target.IsVisible || !this.IsLoaded) return;

            double targetWidth = target.RenderSize.Width;
            Point relativeLocation = target.TranslatePoint(new Point(0, 0), AdminNavGrid);

            var moveAnimation = new System.Windows.Media.Animation.DoubleAnimation()
            {
                To = relativeLocation.X,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new System.Windows.Media.Animation.CubicEase() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            var resizeAnimation = new System.Windows.Media.Animation.DoubleAnimation()
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new System.Windows.Media.Animation.CubicEase() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            AdminIndicatorTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, moveAnimation);
            AdminSlidingIndicator.BeginAnimation(FrameworkElement.WidthProperty, resizeAnimation);
        }
    }
}