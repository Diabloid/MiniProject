using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32; // Для діалогу вибору файлу (аватарки)
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.Core.Services;
using NovaGames.UI.Services;
using System.IO; // Для роботи з файлами та MemoryStream
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging; // Для BitmapImage
using System.Windows.Shapes;

namespace NovaGames.UI.Views
{
    public partial class ProfileView : UserControl
    {
        private readonly UserSession _userSession;
        private readonly UserApiService _userService;
        private SubscriptionsView _subsView;
        private PurchasesView _purchasesView;
        private readonly ILogger _logger;

        private bool _isWaitingForCode = false;
        private bool _isWaitingForPasswordCode = false;
        private bool _isEmailVerified = false;


        public ProfileView()
        {
            InitializeComponent();

            _userSession = App.AppHost!.Services.GetRequiredService<UserSession>();
            _userService = App.AppHost!.Services.GetRequiredService<UserApiService>();
            _logger = App.AppHost.Services.GetRequiredService<ILogger>();

            this.Loaded += (s, e) =>
            {
                if (rbSettings.IsChecked == true) MoveIndicatorTo(rbSettings);
                else if (rbSubs.IsChecked == true) MoveIndicatorTo(rbSubs);
                else if (rbPurchases.IsChecked == true) MoveIndicatorTo(rbPurchases);
            };

            // Ловимо переходи між Магазином, Бібліотекою та Профілем
            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible)
                {
                    ResetAllStates(); // Очищаємо весь мусор і підтягуємо свіжі дані
                }
            };
        }

        // ІНІЦІАЛІЗАЦІЯ ДАНИХ (ВКЛЮЧАЮЧИ АВАТАРКУ)
        private void InitializeProfile()
        {
            if (_userSession.CurrentUser == null) return;

            var user = _userSession.CurrentUser;

            // 1. Встановлюємо нік та баланс
            txtUsernameLabel.Text = user.Username;
            txtBalanceLabel.Text = $"Баланс: {user.WalletBalance:N2} ₴";

            // 2. Встановлюємо букву аватарки
            if (!string.IsNullOrEmpty(user.Username))
            {
                txtProfileAvatarLetter.Text = user.Username.Substring(0, 1).ToUpper();
            }

            // 3. Встановлюємо колір рамки та індикатора
            try
            {
                string savedHex = user.AvatarBorderColorHex;
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(savedHex));

                // Фарбуємо рамку навколо аватарки
                ProfileAvatarCircle.BorderBrush = brush;

                // Вписуємо колір у TextBox
                txtHexColor.Text = savedHex;

                // Пробігаємося по всіх пресетах (кружечках) і виділяємо той, що зберігся в БД
                foreach (var child in ColorPresetsPanel.Children)
                {
                    if (child is System.Windows.Shapes.Rectangle rect)
                    {
                        // Якщо колір кружечка співпадає зі збереженим — робимо білу рамку
                        if (rect.Tag is string tagHex && tagHex.Equals(savedHex, StringComparison.OrdinalIgnoreCase))
                        {
                            rect.Stroke = System.Windows.Media.Brushes.White;
                            rect.StrokeThickness = 2;
                        }
                        else
                        {
                            rect.StrokeThickness = 0; // З інших знімаємо рамку
                        }
                    }
                }
            }
            catch { /* Ігноруємо, якщо колір пошкоджений або відсутній */ }

            // 4. Завантажуємо картинку, якщо вона є
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                LoadProfileAvatarImage(user.AvatarUrl);
            }
            else
            {
                imgProfileAvatar.Visibility = Visibility.Collapsed;
                txtProfileAvatarLetter.Visibility = Visibility.Visible;
            }

            // 5. Логіка пошти
            if (!string.IsNullOrEmpty(user.Email))
            {
                _isEmailVerified = true;
                txtProfileEmail.Text = user.Email;

                txtProfileEmail.IsReadOnly = true;
                txtProfileEmail.Focusable = false;
                txtProfileEmail.Cursor = System.Windows.Input.Cursors.Arrow;

                iconEmailVerified.Visibility = Visibility.Visible;
                btnBindEmail.Content = "ПРИВ'ЯЗАТИ ІНШУ ПОШТУ";
                btnBindEmail.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            }
        }

        // Допоміжний метод: Конвертує Base64 у картинку для UI
        private void LoadProfileAvatarImage(string avatarData)
        {
            try
            {
                if (string.IsNullOrEmpty(avatarData)) throw new Exception();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();

                // Перевіряємо, чи це старий формат Base64 (для сумісності) чи новий URL
                if (avatarData.StartsWith("/images/"))
                {
                    // Формуємо повне посилання до нашого бекенду
                    string fullUrl = $"https://localhost:7232{avatarData}";
                    bitmap.UriSource = new Uri(fullUrl, UriKind.Absolute);

                    // Змушуємо WPF ігнорувати кеш, щоб аватарка точно оновилась!
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                }
                else
                {
                    // Запасний варіант для старих Base64 аватарок
                    byte[] bytes = Convert.FromBase64String(avatarData);
                    bitmap.StreamSource = new MemoryStream(bytes);
                }

                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgProfileAvatar.Source = bitmap;
                imgProfileAvatar.Visibility = Visibility.Visible;
                txtProfileAvatarLetter.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Не вдалося завантажити аватарку користувача ID:{_userSession.CurrentUser?.Id}. Деталі: {ex.Message}", LogType.WARNING);
                imgProfileAvatar.Visibility = Visibility.Collapsed;
                txtProfileAvatarLetter.Visibility = Visibility.Visible;
            }
        }

        // КНОПКИ КАСТОМІЗАЦІЇ (КОЛІР ТА ФОТО)
        private void ColorPreset_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Shapes.Rectangle clickedRect && clickedRect.Tag is string hexColor)
            {
                // 1. Оновлюємо текст в TextBox
                txtHexColor.Text = hexColor;

                // 2. Знімаємо біле обведення з УСІХ пресетів
                foreach (var child in ColorPresetsPanel.Children)
                {
                    if (child is System.Windows.Shapes.Rectangle rect)
                    {
                        rect.StrokeThickness = 0;
                    }
                }

                // 3. Додаємо біле обведення тільки тому, на який клікнули
                clickedRect.Stroke = System.Windows.Media.Brushes.White;
                clickedRect.StrokeThickness = 2;
            }
        }

        // Якщо юзер вирішить сам вписати HEX-код вручну
        private async void TxtHexColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || _userSession?.CurrentUser == null) return;

            string hexColor = txtHexColor.Text.Trim();

            // Валідація HEX-коду
            if (hexColor.StartsWith("#") && (hexColor.Length == 7 || hexColor.Length == 9))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hexColor);
                    var brush = new SolidColorBrush(color);

                    // 1. Оновлюємо UI поточного вікна профілю миттєво
                    // 1.1 Оновлюємо велику рамку в самому профілі
                    ProfileAvatarCircle.BorderBrush = brush;

                    // 2. Оновлюємо локальну сесію
                    _userSession.CurrentUser.AvatarBorderColorHex = hexColor;

                    // 3. Зберігаємо в БД
                    await _userService.UpdateAvatarColorAsync(_userSession.CurrentUser.Id, hexColor);

                    // 4. Смикаємо івент
                    _userSession.NotifyUserUpdated();
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Помилка збереження кольору рамки (HEX: {hexColor}): {ex.Message}", LogType.WARNING);
                }
            }
        }

        private async void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Виберіть аватарку",
                Filter = "Зображення (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 2 * 1024 * 1024)
                    {
                        MessageBox.Show("Файл занадто великий. Максимальний розмір 2 МБ.");
                        return;
                    }

                    // ВІДПРАВЛЯЄМО ФАЙЛ
                    string newUrl = await _userService.UploadAvatarFileAsync(_userSession.CurrentUser!.Id, openFileDialog.FileName);

                    // Оновлюємо сесію і синхронізуємо з хідером MainWindow
                    _userSession.CurrentUser.AvatarUrl = newUrl;
                    _userSession.NotifyUserUpdated();

                    // Оновлюємо картинку локально
                    LoadProfileAvatarImage(newUrl);
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Збій під час завантаження файлу аватарки: {ex.Message}", LogType.ERROR);
                    MessageBox.Show($"Помилка завантаження: {ex.Message}");
                }
            }
        }

        // НАВІГАЦІЯ ТА АНІМАЦІЇ
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedTab = sender as RadioButton;
            MoveIndicatorTo(selectedTab);

            // ЛОГІКА ПЕРЕМИКАННЯ КОНТЕНТУ
            if (selectedTab == rbSettings)
            {
                // Показуємо налаштування, ховаємо інші вкладки
                if (SettingsContentGrid != null) SettingsContentGrid.Visibility = Visibility.Visible;
                if (DynamicTabContent != null) DynamicTabContent.Visibility = Visibility.Collapsed;
            }
            else if (selectedTab == rbSubs)
            {
                // Ховаємо налаштування, показуємо підписки
                SettingsContentGrid.Visibility = Visibility.Collapsed;
                DynamicTabContent.Visibility = Visibility.Visible;

                if (_subsView == null) _subsView = new SubscriptionsView();
                DynamicTabContent.Content = _subsView;
            }
            else if (selectedTab == rbPurchases)
            {
                // Ховаємо налаштування, показуємо покупки
                SettingsContentGrid.Visibility = Visibility.Collapsed;
                DynamicTabContent.Visibility = Visibility.Visible;

                if (_purchasesView == null) _purchasesView = new PurchasesView();
                DynamicTabContent.Content = _purchasesView;
            }
        }

        private void MoveIndicatorTo(UIElement target)
        {
            if (target == null || !target.IsVisible || !IsLoaded) return;

            double targetWidth = target.RenderSize.Width - 10;

            if (targetWidth < 10) targetWidth = target.RenderSize.Width;

            Point relativeLocation = target.TranslatePoint(new Point(0, 0), ProfileNavGrid);

            double centeredX = relativeLocation.X + (target.RenderSize.Width - targetWidth) / 2;

            DoubleAnimation moveAnimation = new DoubleAnimation()
            {
                To = centeredX,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation resizeAnimation = new DoubleAnimation()
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
            };

            ProfileIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, moveAnimation);
            ProfileIndicator.BeginAnimation(FrameworkElement.WidthProperty, resizeAnimation);
        }

        // ЛОГІКА ПОШТИ ТА ПАРОЛЯ
        private void TxtProfileEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtEmailError != null) txtEmailError.Visibility = Visibility.Collapsed;
        }

        private void TxtVerificationCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtVerificationCode != null)
                txtVerificationCode.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
        }

        private async void BtnBindEmail_Click(object sender, RoutedEventArgs e)
        {
            txtEmailError.Visibility = Visibility.Collapsed;

            // СТАН 1: СКИДАННЯ
            if (_isEmailVerified)
            {
                _isEmailVerified = false;
                txtProfileEmail.IsReadOnly = false;
                txtProfileEmail.Focusable = true;
                txtProfileEmail.Cursor = System.Windows.Input.Cursors.IBeam;

                iconEmailVerified.Visibility = Visibility.Collapsed;
                btnBindEmail.Content = "ПРИВ'ЯЗАТИ ПОШТУ";
                txtVerificationCode.Text = "";
                btnBindEmail.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ADEF"));
                return;
            }

            // СТАН 2: ВІДПРАВКА КОДУ
            if (!_isWaitingForCode)
            {
                string newEmail = txtProfileEmail.Text.Trim();

                if (string.IsNullOrWhiteSpace(newEmail))
                {
                    txtEmailError.Text = "Введіть пошту!";
                    txtEmailError.Visibility = Visibility.Visible;
                    return;
                }

                if (newEmail == _userSession.CurrentUser?.Email)
                {
                    txtEmailError.Text = "Це вже ваша поточна пошта!";
                    txtEmailError.Visibility = Visibility.Visible;
                    return;
                }

                btnBindEmail.IsEnabled = false;

                try
                {
                    await _userService.SendVerificationCodeAsync(_userSession.CurrentUser!.Id, newEmail);

                    _isWaitingForCode = true;
                    txtProfileEmail.IsReadOnly = true;
                    txtProfileEmail.Focusable = false;

                    panelVerificationCode.Visibility = Visibility.Visible;
                    btnBindEmail.Content = "ПІДТВЕРДИТИ";
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("зайнята"))
                    {
                        _ = _logger?.LogAsync($"Спроба прив'язки вже існуючої пошти: {newEmail}", LogType.WARNING);
                        txtEmailError.Text = "Така пошта вже прив'язана!";
                        txtEmailError.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        _ = _logger?.LogAsync($"Помилка відправки коду на пошту {newEmail}: {ex.Message}", LogType.ERROR);
                        MessageBox.Show($"Помилка: {ex.Message}");
                    }
                }
                finally
                {
                    btnBindEmail.IsEnabled = true;
                }
            }
            // СТАН 3: ПЕРЕВІРКА КОДУ
            else
            {
                string code = txtVerificationCode.Text.Trim();
                btnBindEmail.IsEnabled = false;
                try
                {
                    string confirmedEmail = txtProfileEmail.Text.Trim();

                    await _userService.UpdateEmailAsync(_userSession.CurrentUser!.Id, confirmedEmail, code);

                    _userSession.CurrentUser.Email = confirmedEmail;
                    _isWaitingForCode = false;
                    _isEmailVerified = true;

                    panelVerificationCode.Visibility = Visibility.Collapsed;
                    iconEmailVerified.Visibility = Visibility.Visible;
                    txtVerificationCode.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));

                    txtProfileEmail.IsReadOnly = true;
                    txtProfileEmail.Focusable = false;
                    txtProfileEmail.Cursor = System.Windows.Input.Cursors.Arrow;

                    btnBindEmail.Content = "ПРИВ'ЯЗАТИ ІНШУ ПОШТУ";
                    btnBindEmail.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Невірний код або збій API під час підтвердження пошти: {ex.Message}", LogType.WARNING);
                    txtVerificationCode.BorderBrush = new SolidColorBrush(Colors.Red);
                }
                finally
                {
                    btnBindEmail.IsEnabled = true;
                }
            }
        }

        private void TxtProfilePass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPasswordMessage != null) txtPasswordMessage.Visibility = Visibility.Collapsed;
        }

        private void TxtPasswordVerificationCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPasswordVerificationCode != null)
                txtPasswordVerificationCode.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
        }

        private async void BtnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            txtPasswordMessage.Visibility = Visibility.Collapsed;
            string newPassword = txtProfilePass.Password;

            if (!_isEmailVerified || string.IsNullOrEmpty(_userSession.CurrentUser?.Email))
            {
                txtPasswordMessage.Text = "Потрібна прив'язана пошта!";
                txtPasswordMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555"));
                txtPasswordMessage.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                txtPasswordMessage.Text = "Введіть новий пароль!";
                txtPasswordMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555"));
                txtPasswordMessage.Visibility = Visibility.Visible;
                return;
            }

            if (!_isWaitingForPasswordCode)
            {
                btnUpdatePassword.IsEnabled = false;
                try
                {
                    await _userService.SendPasswordResetCodeAsync(_userSession.CurrentUser!.Id);

                    _isWaitingForPasswordCode = true;
                    panelPasswordCode.Visibility = Visibility.Visible;
                    btnUpdatePassword.Content = "ПІДТВЕРДИТИ ЗМІНУ";
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Помилка запиту на зміну пароля: {ex.Message}", LogType.ERROR);
                    MessageBox.Show($"Помилка відправки коду: {ex.Message}");
                }
                finally
                {
                    btnUpdatePassword.IsEnabled = true;
                }
            }
            else
            {
                string code = txtPasswordVerificationCode.Text.Trim();
                if (string.IsNullOrWhiteSpace(code)) return;

                btnUpdatePassword.IsEnabled = false;
                try
                {
                    await _userService.UpdatePasswordAsync(_userSession.CurrentUser!.Id, newPassword, code);

                    _isWaitingForPasswordCode = false;
                    panelPasswordCode.Visibility = Visibility.Collapsed;

                    txtProfilePass.Password = "";
                    txtPasswordVerificationCode.Text = "";
                    btnUpdatePassword.Content = "ОНОВИТИ ПАРОЛЬ";

                    txtPasswordMessage.Text = "Пароль успішно змінено!";
                    txtPasswordMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00D26A"));
                    txtPasswordMessage.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Помилка зміни пароля (невірний код або валідація): {ex.Message}", LogType.WARNING); 
                    txtPasswordVerificationCode.BorderBrush = new SolidColorBrush(Colors.Red);
                }
                finally
                {
                    btnUpdatePassword.IsEnabled = true;
                }
            }
        }

        // ОЧИЩЕННЯ
        private void ResetAllStates()
        {
            _isWaitingForPasswordCode = false;

            if (txtProfilePass != null) txtProfilePass.Password = "";
            if (txtPasswordVerificationCode != null)
            {
                txtPasswordVerificationCode.Text = "";
                txtPasswordVerificationCode.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            }
            if (txtPasswordMessage != null) txtPasswordMessage.Visibility = Visibility.Collapsed;
            if (panelPasswordCode != null) panelPasswordCode.Visibility = Visibility.Collapsed;

            if (btnUpdatePassword != null)
            {
                btnUpdatePassword.Content = "ОНОВИТИ ПАРОЛЬ";
                btnUpdatePassword.IsEnabled = true;
            }

            if (txtVerificationCode != null)
            {
                txtVerificationCode.Text = "";
                txtVerificationCode.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            }
            if (txtEmailError != null) txtEmailError.Visibility = Visibility.Collapsed;

            if (_isWaitingForCode)
            {
                _isWaitingForCode = false;
                if (panelVerificationCode != null) panelVerificationCode.Visibility = Visibility.Collapsed;
                if (btnBindEmail != null)
                {
                    btnBindEmail.Content = "ПРИВ'ЯЗАТИ ПОШТУ";
                    btnBindEmail.IsEnabled = true;
                }
            }

            InitializeProfile();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfirmWindow(
                "ВИХІД З АКАУНТА",
                "Ви дійсно хочете вийти з профілю?",
                "ВИЙТИ",
                "Для доступу до бібліотеки доведеться авторизуватись знову."
            )
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                // 1. Очищаємо сесію
                _userSession.CurrentUser = null;
                _userSession.Token = null;
                _userSession.RefreshToken = null;

                // 2. Видаляємо з пам'яті
                var tokenStorage = App.AppHost!.Services.GetRequiredService<TokenStorageService>();
                tokenStorage.ClearToken();

                // 3. Перехід на логін
                var navigationService = App.AppHost!.Services.GetRequiredService<INavigationService>();
                navigationService.NavigateTo<LoginWindow>(Window.GetWindow(this));
            }
        }
    }
}