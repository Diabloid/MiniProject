using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.Core.Services;
using NovaGames.UI.Services;
using NovaGames.UI.Views;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace NovaGames.UI
{
    public partial class MainWindow : Window
    {
        private readonly GameApiService _gameService;

        // 1. ДОДАЄМО ПОЛЕ ДЛЯ СЕСІЇ ТУТ
        private readonly UserSession _userSession;
        private readonly User? _currentUser;
        private readonly UserApiService _userService;
        private readonly CartApiService _cartService;

        // Кешовані екземпляри сторінок
        private StoreView? _storeView;
        private LibraryView? _libraryView;
        private AdminView? _adminView;
        private ProfileView? _profileView;
        private GameDetailsView? _gameDetailsView;
        private CartView _cartView;
        private WindowState _previousState;
        private readonly ILogger _logger;

        private decimal _pendingReplenishAmount = 0;
        // Конструктор
        public MainWindow(UserSession session, GameApiService gameService)
        {
            InitializeComponent();
            _gameService = gameService;
            _userService = App.AppHost!.Services.GetRequiredService<UserApiService>();
            _logger = App.AppHost!.Services.GetRequiredService<ILogger>();

            // 2. ЗБЕРІГАЄМО СЕСІЮ В НАШЕ ПОЛЕ
            _userSession = session;

            // Дістаємо юзера з сесії
            if (_userSession.CurrentUser == null)
            {
                System.Windows.MessageBox.Show("Помилка сесії. Увійдіть знову.");
                Close();
                return;
            }
            _currentUser = _userSession.CurrentUser;

            if (_currentUser.Role == UserRole.Admin || _currentUser.Role == UserRole.SuperAdmin)
            {
                btnNavAdmin.Visibility = Visibility.Visible;
            }

            this.Loaded += (s, e) =>
            {
                btnNavStore.IsChecked = true;
                // Викликаємо метод при старті, щоб одразу намалювати аватарку!
                UpdateHeaderAvatar();
            };

            this.Closed += (s, e) =>
            {
                if (MyNotifyIcon != null)
                {
                    MyNotifyIcon.Visibility = Visibility.Collapsed;
                    MyNotifyIcon.Dispose(); // Примусово знищуємо ресурс Windows
                }
            };

            _cartService = App.AppHost!.Services.GetRequiredService<CartApiService>();
            // Підписуємося на оновлення з ProfileView
            _userSession.CurrentUserUpdated += UpdateHeaderAvatar;
            // ПІДПИСКА НА ОНОВЛЕННЯ КОШИКА
            _cartService.CartCountChanged += UpdateCartBadge;

            // Початкове завантаження при старті програми
            _ = _cartService.LoadMyCartIdsAsync();

            // Встановлюємо баланс гаманця
            txtBalance.Text = $"{_currentUser.WalletBalance:0.00} ₴";

            this.MouseDown += Window_MouseDown;
            // this.PreviewMouseDown += Window_PreviewMouseDown; // Якщо є такий метод, залишай
            this.StateChanged += MainWindow_StateChanged;
        }

        private void UpdateCartBadge(int count)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (count > 0)
                {
                    // Якщо badge світиться червоним - переконайся, що в XAML 
                    // він НЕ всередині <ControlTemplate> чи <Style>
                    badge.Visibility = Visibility.Visible;

                    // Звертаємося напряму до тексту бейджа (замість тих страшних кастів)
                    txtBadgeCount.Text = count.ToString();
                }
                else
                {
                    badge.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void UpdateHeaderAvatar()
        {
            var user = _userSession.CurrentUser;
            if (user == null) return;

            // 1. Оновлюємо букву і ім'я
            if (!string.IsNullOrEmpty(user.Username))
            {
                txtHeaderAvatarLetter.Text = user.Username.Substring(0, 1).ToUpper();
                btnUserProfile.Content = user.Username;
            }

            // 2. Оновлюємо колір рамки
            try
            {
                HeaderAvatarBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(user.AvatarBorderColorHex));
            }
            catch { /* Ігноруємо, якщо колір битий */ }

            // 3. Оновлюємо картинку
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();

                    if (user.AvatarUrl.StartsWith("/images/"))
                    {
                        bitmap.UriSource = new Uri($"https://localhost:7232{user.AvatarUrl}", UriKind.Absolute);
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    }
                    else
                    {
                        // Залишаємо для старих профілів з Base64
                        byte[] imageBytes = Convert.FromBase64String(user.AvatarUrl);
                        bitmap.StreamSource = new MemoryStream(imageBytes);
                    }

                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    imgHeaderAvatar.Source = bitmap;
                    imgHeaderAvatar.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Не вдалося завантажити міні-аватарку в хедері: {ex.Message}", LogType.WARNING);
                    imgHeaderAvatar.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                imgHeaderAvatar.Visibility = Visibility.Collapsed;
            }
        }

        // Зняття фокусу з полів вводу при кліку на вікно
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }


        // Пошук
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim();

            // 1. Оновлюємо сам магазин
            if (MainContent.Content is StoreView storeView)
                storeView.UpdateSearch(searchText);

            // 2. Лоіка для Popup з підказками
            if (string.IsNullOrEmpty(searchText) || _storeView == null)
            {
                SearchPopup.IsOpen = false;
                return;
            }

            // Звертаємося до методу у StoreView
            var suggestions = _storeView.GetSearchSuggestions(searchText);

            if (suggestions.Any())
            {
                listSearchSuggestions.ItemsSource = suggestions;
                SearchPopup.IsOpen = true;
            }
            else
            {
                SearchPopup.IsOpen = false;
            }
        }

        // Коли ми знову клікаємо в поле пошуку
        private void TxtSearch_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSearch.Text) && listSearchSuggestions.HasItems)
            {
                SearchPopup.IsOpen = true;
            }
        }

        // Обробник кліку по міні-картці в Popup
        private void ListSearchSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listSearchSuggestions.SelectedItem is Game selectedGame)
            {
                SearchPopup.IsOpen = false;
                txtSearch.Text = string.Empty;

                NavigateToGameDetails(selectedGame);
                 
                listSearchSuggestions.SelectedItem = null;
            }
        }

        // --- ВІКОННІ ОПЕРАЦІЇ ---

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Перевіряємо клік, бо якщо вікно на весь екран, перетягувати не можна
            if (e.ChangedButton == MouseButton.Left && this.WindowState == WindowState.Normal)
                this.DragMove();

            // Якщо подвійний клік по шапці - розгортаємо/згортаємо
            if (e.ClickCount == 2)
                ToggleMaximize();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            // Запам'ятовуємо стан для правильного розгортання потім
            _previousState = this.WindowState;

            // Миттєве згортання без анімацій
            this.WindowState = WindowState.Minimized;
        }

        // Логіка кнопки розгортання/згортання
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Якщо вікно розгортається (перестає бути Minimized)
            if (this.WindowState != WindowState.Minimized)
            {
                // 1. Вбиваємо стару анімацію і повертаємо 100% прозорості
                this.BeginAnimation(UIElement.OpacityProperty, null);
                this.Opacity = 1;

                // 2. Якщо вікно було на весь екран до згортання — повертаємо його на весь екран!
                if (this.WindowState == WindowState.Normal && _previousState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Maximized;
                    _previousState = WindowState.Normal; // Очищаємо пам'ять
                }
            }
        }

        // Плавне розгортання на весь екран / у вікно
        private void ToggleMaximize()
        {
            var fadeOut = new DoubleAnimation(1, 0.5, TimeSpan.FromMilliseconds(80));
            fadeOut.Completed += (s, ev) =>
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.WindowState = WindowState.Maximized;
                    btnMaximize.Content = "❐";
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    btnMaximize.Content = "☐";
                }

                var fadeIn = new DoubleAnimation(0.5, 1, TimeSpan.FromMilliseconds(150));
                this.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            MyNotifyIcon.ShowBalloonTip("Nova Games", "Додаток працює у фоновому режимі.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }

        private void BtnUserProfile_Click(object sender, RoutedEventArgs e)
        {
            // 1. Відкриваємо сторінку профілю
            if (_profileView == null) { _profileView = new ProfileView(); }
            MainContent.Content = _profileView;

            // 2. Примусово знімаємо виділення з усіх RadioButton
            btnNavStore.IsChecked = false;
            btnNavLibrary.IsChecked = false;
            btnNavAdmin.IsChecked = false;

            // 3. Ховаємо синій індикатор (щоб він не висів під пустою вкладкою)
            // Знімаємо блок від старої анімації(якщо вона була)
            SlidingIndicator.BeginAnimation(FrameworkElement.WidthProperty, null);

            // Робимо плавну анімацію зникнення лінії (вона стягнеться в 0 за 150 мілісекунд)
            var hideAnim = new System.Windows.Media.Animation.DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
            SlidingIndicator.BeginAnimation(FrameworkElement.WidthProperty, hideAnim);
        }

        // --- ЛОГІКА ТРЕЮ ---

        private void Tray_Restore_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate(); // Робить вікно активним (поверх інших)
        }

        private void Tray_Exit_Click(object sender, RoutedEventArgs e)
        {
            // Повне закриття
            Application.Current.Shutdown();
        }

        // Метод для плавної анімації лінії в навігації
        private void MoveIndicatorTo(UIElement target)
        {
            if (target == null || !target.IsVisible || !IsLoaded) return;

            double targetWidth = target.RenderSize.Width;

            Point relativeLocation = target.TranslatePoint(new Point(0, 0), NavButtonsGrid);

            // Анімація руху по осі X
            DoubleAnimation moveAnimation = new DoubleAnimation()
            {
                To = relativeLocation.X,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut } // Плавне сповільнення в кінці
            };

            // Анімація зміни ширини
            DoubleAnimation resizeAnimation = new DoubleAnimation()
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
            };

            IndicatorTransform.BeginAnimation(TranslateTransform.XProperty, moveAnimation);
            SlidingIndicator.BeginAnimation(FrameworkElement.WidthProperty, resizeAnimation);
        }

        // Обробник для всіх кнопок навігації (ЄДИНИЙ)
        private void NavMenu_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn)
            {
                // Запускаємо анімацію синьої лінії
                MoveIndicatorTo(btn);

                // Відкриваємо відповідну сторінку
                if (btn.Name == "btnNavStore")
                {
                    if (_storeView == null) _storeView = new StoreView();
                    MainContent.Content = _storeView;
                }
                else if (btn.Name == "btnNavLibrary")
                {
                    if (_libraryView == null) _libraryView = new LibraryView();
                    MainContent.Content = _libraryView;
                }
                else if (btn.Name == "btnNavAdmin")
                {
                    if (_adminView == null) _adminView = new AdminView();
                    MainContent.Content = _adminView;
                }
            }
        }

        public void NavigateToGameDetails(Game selectedGame)
        {
            btnNavStore.IsChecked = false;
            btnNavLibrary.IsChecked = false;
            btnNavAdmin.IsChecked = false;

            SlidingIndicator.BeginAnimation(FrameworkElement.WidthProperty, null);
            SlidingIndicator.Width = 0;

            if (_gameDetailsView == null)
            {
                _gameDetailsView = new GameDetailsView();
            }

            // Викликаємо асинхронний запит до API за свіжими даними
            _ = _gameDetailsView.LoadGameDataAsync(selectedGame.Id);

            MainContent.Content = _gameDetailsView;
        }

        // Обробник кліку по кошику
        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            // Знімаємо виділення з усіх радіо-кнопок меню
            btnNavStore.IsChecked = false;
            btnNavLibrary.IsChecked = false;
            btnNavAdmin.IsChecked = false;

            // Ховаємо лінію підкреслення меню
            SlidingIndicator.BeginAnimation(FrameworkElement.WidthProperty, null);
            SlidingIndicator.Width = 0;

            if (_cartView == null)
            {
                _cartView = new CartView();
            }

            _cartView.LoadCart();
            MainContent.Content = _cartView;
        }

        // Цей метод можуть викликати інші вікна, щоб примусово відкрити кошик
        public void NavigateToCart()
        {
            // Знімаємо виділення з усіх радіо-кнопок головного меню
            btnNavStore.IsChecked = false;
            btnNavLibrary.IsChecked = false;
            btnNavAdmin.IsChecked = false;

            // Ховаємо лінію підкреслення меню
            SlidingIndicator.BeginAnimation(FrameworkElement.WidthProperty, null);
            SlidingIndicator.Width = 0;

            if (_cartView == null)
            {
                _cartView = new CartView();
            }

            _cartView.LoadCart();
            MainContent.Content = _cartView;
        }

        // Метод для примусового повернення в магазин (наприклад, з порожнього кошика)
        public void NavigateToStore()
        {
            // Робимо кнопку магазину в хедері активною
            btnNavStore.IsChecked = true;

            // Повертаємо лінію підкреслення (SlidingIndicator) до кнопки магазину
            // Якщо в тебе є метод MoveIndicatorTo, можеш використати його, 
            // або просто скинути анімацію (як ми робили в інших місцях)

            if (_storeView == null)
            {
                _storeView = new StoreView();
            }

            MainContent.Content = _storeView;
        }

        // Глобальне перехоплення всіх кліків у вікні
        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Перевіряємо: якщо Попап ВІДКРИТИЙ, але ми клікаємо НЕ по полю пошуку і НЕ по самому попапу
            if (SearchPopup.IsOpen && !SearchBorder.IsMouseOver && !SearchPopup.IsMouseOver)
            {
                // Примусово його закриваємо
                SearchPopup.IsOpen = false;
            }
        }

        // 1. ВІДКРИТТЯ ВІКНА (Твій клік по плюсику в хідері - переконайся, що метод прив'язаний до кнопки)
        private async void BtnAddFunds_Click(object sender, RoutedEventArgs e)
        {
            OverlayWallet.Visibility = Visibility.Visible;

            try
            {
                // Завантажуємо збережені картки
                var savedCards = await _userService.GetSavedCardsAsync();
                if (savedCards.Any())
                {
                    PanelSavedCards.Visibility = Visibility.Visible;
                    comboSavedCards.ItemsSource = savedCards;
                    // Показуємо маску в списку, наприклад "**** **** **** 1234 (12/26)"
                    comboSavedCards.DisplayMemberPath = "MaskedCardNumber";
                }
                else
                {
                    PanelSavedCards.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Помилка завантаження збережених карток: {ex.Message}", LogType.ERROR);
                // Продовжуємо роботу, просто без збережених карток
                PanelSavedCards.Visibility = Visibility.Collapsed;
            }
        }

        // 2. ЗАКРИТТЯ ВІКНА
        private void OverlayWallet_Close_Click(object sender, RoutedEventArgs e)
        {
            OverlayWallet.Visibility = Visibility.Collapsed;

            // Скидаємо стан на Крок 1 при закритті
            PanelStep1_Amount.Visibility = Visibility.Visible;
            PanelStep2_Payment.Visibility = Visibility.Collapsed;
            _pendingReplenishAmount = 0;

            // 2. ОЧИЩАЄМО ВСІ ПОЛЯ
            txtCardNumber.Text = string.Empty;
            txtCardExpiry.Text = string.Empty;
            txtCardCvv.Text = string.Empty;

            // 3. Розблоковуємо поля (якщо вони були заблоковані вибором збереженої картки)
            txtCardNumber.IsEnabled = true;
            txtCardExpiry.IsEnabled = true;

            // 4. Скидаємо комбобокс та галочку
            comboSavedCards.SelectedItem = null;
            chkSaveCard.IsChecked = false;
            chkSaveCard.IsEnabled = true;
        }

        // 3. КНОПКИ ШВИДКОЇ СУМИ (100, 500, 1000)
        private void BtnQuickAmount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
            {
                txtReplenishAmount.Text = btn.Tag.ToString();
            }
        }

        // 4. ЗАХИСТ ВІД ВВЕДЕННЯ БУКВ (тільки цифри)
        private void TxtAmount_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // 5. КЛІК "ПЕРЕЙТИ ДО ОПЛАТИ"
        // КРОК 1: Натиснули "ДАЛІ"
        private void BtnConfirmReplenish_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtReplenishAmount.Text, out decimal amount) && amount > 0)
            {
                _pendingReplenishAmount = amount;

                // Перемикаємо візуал на дані картки
                PanelStep1_Amount.Visibility = Visibility.Collapsed;
                PanelStep2_Payment.Visibility = Visibility.Visible;

                // Пишемо точну суму на фінальній кнопці
                btnProcessPayment.Content = $"ОПЛАТИТИ {_pendingReplenishAmount:N2} ₴";
            }
            else
            {
                var dialog = new ConfirmWindow("ПОМИЛКА", "Будь ласка, введіть коректну суму.", "ЗРОЗУМІЛО")
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
            }
        }

        // КРОК 2: Натиснули "НАЗАД"
        private void BtnBackToAmount_Click(object sender, RoutedEventArgs e)
        {
            PanelStep2_Payment.Visibility = Visibility.Collapsed;
            PanelStep1_Amount.Visibility = Visibility.Visible;
        }

        // КРОК 2: ФІНАЛЬНА ОПЛАТА
        private async void BtnProcessPayment_Click(object sender, RoutedEventArgs e)
        {
            if (txtCardNumber.Text.Length < 16 || txtCardCvv.Text.Length < 3)
            {
                var errorDialog = new ConfirmWindow(
                    header: "ПОМИЛКА ВВЕДЕННЯ",
                    message: "Будь ласка, перевірте правильність введених даних.",
                    confirmBtnText: "ЗРОЗУМІЛО",
                    warningText: "",
                    showCancelButton: false // Ховаємо скасування
                )
                {
                    Owner = Window.GetWindow(this)
                };
                errorDialog.ShowDialog();
                return;
            }

            // Показуємо стан завантаження (можна просто змінити текст на кнопці)
            btnProcessPayment.Content = "ОБРОБКА...";
            btnProcessPayment.IsEnabled = false;

            try
            {
                var request = new TopUpRequest
                {
                    Amount = _pendingReplenishAmount,
                    CardNumber = txtCardNumber.Text.Replace(" ", ""), // Прибираємо пробіли, якщо є
                    ExpiryDate = txtCardExpiry.Text,
                    Cvv = txtCardCvv.Text,
                    SaveCard = chkSaveCard.IsChecked ?? false
                };

                // ВИКЛИКАЄМО НАШ НОВИЙ API-МЕТОД
                decimal newBalance = await _userService.TopUpWalletAsync(request);
                _ = _logger?.LogAsync($"Користувач успішно поповнив гаманець на {_pendingReplenishAmount:N2} ₴", LogType.INFO);

                // Оновлюємо баланс в локальній сесії та в інтерфейсі (Хідері)
                _userSession.CurrentUser!.WalletBalance = newBalance;

                txtBalance.Text = $"{newBalance:N2} ₴";

                var successDialog = new ConfirmWindow(
                    header: "УСПІШНА ОПЛАТА",
                    message: $"Транзакція успішна!\nВаш новий баланс: {newBalance:N2} ₴",
                    confirmBtnText: "ЧУДОВО",
                    warningText: "",
                    showCancelButton: false,               // Одна кнопка
                    headerColorHex: "#00ADEF",             // Синій заголовок
                    confirmBtnStyle: "ModernButtonStyle"   // Твоя синя кнопка з App.xaml
                )
                {
                    Owner = Window.GetWindow(this)
                };
                successDialog.ShowDialog();

                OverlayWallet_Close_Click(null, null); // Закриваємо модалку
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Збій під час поповнення гаманця: {ex.Message}", LogType.ERROR);

                var errorDialog = new ConfirmWindow(
                    header: "ПОМИЛКА ТРАНЗАКЦІЇ",
                    message: "",
                    confirmBtnText: "ЗРОЗУМІЛО",
                    warningText: "",
                    showCancelButton: false // Ховаємо скасування
                )
                {
                    Owner = Window.GetWindow(this)
                };
                errorDialog.ShowDialog();
            }
            finally
            {
                // Повертаємо кнопку в нормальний стан
                btnProcessPayment.Content = "ОПЛАТИТИ";
                btnProcessPayment.IsEnabled = true;
            }
        }

        // 6. ВИБІР ЗБЕРЕЖЕНОЇ КАРТКИ
        private void ComboSavedCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboSavedCards.SelectedItem is SavedCardDto selectedCard)
            {
                // Підставляємо маску і дату
                txtCardNumber.Text = selectedCard.MaskedCardNumber;
                txtCardExpiry.Text = selectedCard.ExpiryDate;
                txtCardNumber.IsEnabled = false;
                txtCardExpiry.IsEnabled = false;

                // Знімаємо галочку збереження, бо ця картка вже збережена
                chkSaveCard.IsChecked = false;
                chkSaveCard.IsEnabled = false;

                // Фокус на CVV, бо його треба вводити завжди
                txtCardCvv.Focus();
            }
        }

        // Автоматичне форматування дати в MM/YY
        private void TxtCardExpiry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                // 1. Тимчасово відписуємось від події, щоб уникнути нескінченного циклу при зміні тексту
                textBox.TextChanged -= TxtCardExpiry_TextChanged;

                // 2. Видаляємо всі існуючі слеші, щоб залишились ТІЛЬКИ цифри
                string cleanText = textBox.Text.Replace("/", "");

                // 3. Якщо цифр більше двох, ставимо між ними слеш
                if (cleanText.Length > 2)
                {
                    cleanText = cleanText.Insert(2, "/");
                }

                // 4. Оновлюємо текст і ставимо курсор у самий кінець
                textBox.Text = cleanText;
                textBox.CaretIndex = textBox.Text.Length;

                // 5. Підписуємось назад
                textBox.TextChanged += TxtCardExpiry_TextChanged;
            }
        }

        public void NavigateToLibrary(int? targetGameId = null)
        {
            if (btnNavLibrary != null)
            {
                btnNavLibrary.IsChecked = true;
            }

            if (_libraryView == null)
            {
                _libraryView = new LibraryView();
            }

            MainContent.Content = _libraryView;

            if (targetGameId.HasValue)
            {
                _libraryView.SelectGame(targetGameId.Value);
            }
        }

        // Публічний метод для оновлення балансу з інших сторінок
        public void UpdateWalletBalance(decimal newBalance)
        {
            if (_userSession.CurrentUser != null)
            {
                _userSession.CurrentUser.WalletBalance = newBalance;
            }

            // Обов'язково через Dispatcher, щоб UI оновився без конфліктів потоків
            Application.Current.Dispatcher.Invoke(() =>
            {
                txtBalance.Text = $"{newBalance:N2} ₴";
            });
        }
    }
}