using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using NovaGames.Core.Services;
using NovaGames.UI.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NovaGames.UI.Views
{
    public partial class CartView : UserControl
    {
        private readonly GameApiService _gameService;
        private readonly UserSession _userSession;
        private readonly CartApiService _cartService;
        private readonly ILogger _logger;

        // Для тесту UI використовуємо ObservableCollection
        public ObservableCollection<Game> CartItems { get; set; } = new ObservableCollection<Game>();

        public CartView()
        {
            InitializeComponent();
            _gameService = App.AppHost!.Services.GetRequiredService<GameApiService>();
            _userSession = App.AppHost!.Services.GetRequiredService<UserSession>();
            _cartService = App.AppHost!.Services.GetRequiredService<CartApiService>();
            _logger = App.AppHost!.Services.GetRequiredService<ILogger>();

            rbPayWallet.Checked += PaymentMethod_Changed;
            rbPayCard.Checked += PaymentMethod_Changed;
        }

        // Цей метод завантажує ігри в кошик з сервера і оновлює UI відповідно до того, чи є товари в кошику
        public async void LoadCart()
        {
            try
            {
                var games = await _cartService.GetCartGamesAsync();
                CartItems = new ObservableCollection<Game>(games);

                if (CartItems.Count == 0)
                {
                    panelEmptyCart.Visibility = Visibility.Visible;
                    gridCartContent.Visibility = Visibility.Collapsed;
                }
                else
                {
                    panelEmptyCart.Visibility = Visibility.Collapsed;
                    gridCartContent.Visibility = Visibility.Visible;
                    listCartItems.ItemsSource = CartItems;
                    CalculateTotals();
                }

                // Оновлюємо баланс гаманця в тексті радіокнопки
                if (_userSession.CurrentUser != null)
                {
                    rbPayWallet.Content = $"Мій Гаманець ({_userSession.CurrentUser.WalletBalance:N2} ₴)";
                }
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Помилка завантаження кошика: {ex.Message}", LogType.ERROR);
                MessageBox.Show("Не вдалося завантажити кошик. Перевірте з'єднання.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Цей метод рахує підсумкову вартість, знижку та загальну суму до оплати
        private void CalculateTotals()
        {
            decimal subtotal = CartItems.Sum(g => g.Price);
            decimal totalDiscount = CartItems.Sum(g => g.Price - g.FinalPrice); // Різниця між базовою і фінальною
            decimal totalToPay = CartItems.Sum(g => g.FinalPrice);

            txtSubtotal.Text = $"{subtotal:N2} ₴";
            txtTotalDiscount.Text = $"{totalDiscount:N2} ₴";
            txtTotalPrice.Text = $"{totalToPay:N2} ₴";
        }

        // Цей метод показує або ховає панель з деталями картки в залежності від вибраного способу оплати
        private void PaymentMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (panelCardDetails == null) return;

            if (rbPayCard.IsChecked == true)
            {
                panelCardDetails.Visibility = Visibility.Visible;
            }
            else
            {
                panelCardDetails.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Game gameToRemove)
            {
                try
                {
                    // Викликаємо API для видалення
                    bool success = await _cartService.RemoveFromCartAsync(gameToRemove.Id);

                    if (success)
                    {
                        // Видаляємо з візуального списку
                        CartItems.Remove(gameToRemove);

                        // Якщо це була остання гра, показуємо "Кошик порожній"
                        if (CartItems.Count == 0)
                        {
                            panelEmptyCart.Visibility = Visibility.Visible;
                            gridCartContent.Visibility = Visibility.Collapsed;
                        }

                        CalculateTotals();
                    }
                    else
                    {
                        _ = _logger?.LogAsync($"Невдала спроба видалення гри ID:{gameToRemove.Id} з кошика", LogType.WARNING);
                    }
                }
                catch (Exception ex)
                {
                    _ = _logger?.LogAsync($"Критична помилка при видаленні гри ID:{gameToRemove.Id}: {ex.Message}", LogType.ERROR);
                }
            }
        }

        private void Card_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 1. ЗАХИСТ: Перевіряємо, чи не клікнув юзер по якійсь Кнопці (наприклад, "В кошик" чи "✕")
            // Беремо елемент, по якому реально клікнули (наприклад, текст або іконка всередині кнопки)
            System.Windows.DependencyObject current = e.OriginalSource as System.Windows.DependencyObject;

            // Йдемо вгору по дереву елементів
            while (current != null)
            {
                if (current is System.Windows.Controls.Button)
                {
                    return; // Це клік по кнопці! Ігноруємо його, хай кнопка сама робить свою роботу.
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

        private void BtnGoToStore_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToStore();
        }

        // Головний метод для обробки чекауту
        private async void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            // Блокуємо кнопку
            if (sender is Button btn)
            {
                btn.IsEnabled = false;
                btn.Content = "З'ЄДНАННЯ З БАНКОМ...";
            }

            try
            {
                // Збираємо дані для запиту
                var request = new CheckoutRequest
                {
                    PaymentMethod = rbPayCard.IsChecked == true ? "Card" : "Wallet"
                };

                // Якщо вибрано картку - валідуємо поля на фронтенді
                if (request.PaymentMethod == "Card")
                {
                    string cardNum = txtCheckoutCardNumber.Text.Replace(" ", "");
                    if (cardNum.Length < 16 || txtCheckoutCardCvv.Text.Length < 3 || txtCheckoutCardExpiry.Text.Length < 5)
                    {
                        MessageBox.Show("Будь ласка, введіть коректні дані картки.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    request.CardNumber = cardNum;
                    request.ExpiryDate = txtCheckoutCardExpiry.Text;
                    request.Cvv = txtCheckoutCardCvv.Text;
                }

                // Викликаємо API
                var (success, errorMessage, data) = await _cartService.CheckoutAsync(request);

                if (success && data != null)
                {
                    // Отримуємо посилання на головне вікно
                    var mainWindow = Window.GetWindow(this) as MainWindow;

                    // Якщо оплата була з гаманця - оновлюємо баланс сесії ТА В ІНТЕРФЕЙСІ (Хідері)
                    if (request.PaymentMethod == "Wallet")
                    {
                        // Це оновить змінну в пам'яті
                        _userSession.CurrentUser!.WalletBalance = data.NewBalance;

                        // А ЦЕ ОНОВИТЬ ЦИФРУ В ПРАВОМУ ВЕРХНЬОМУ КУТІ
                        mainWindow?.UpdateWalletBalance(data.NewBalance);
                    }

                    CartItems.Clear();
                    panelEmptyCart.Visibility = Visibility.Visible;
                    gridCartContent.Visibility = Visibility.Collapsed;

                    string msg = request.PaymentMethod == "Wallet"
                        ? $"Ігри успішно придбано!\nВаш новий баланс: {data.NewBalance:N2} ₴"
                        : "Оплата через шлюз Stripe успішна!\nКошти переказано на рахунок NovaGames.";

                    var successDialog = new ConfirmWindow("УСПІШНА ПОКУПКА", msg, "В БІБЛІОТЕКУ", "", false, "#00ADEF", "ModernButtonStyle")
                    { Owner = Window.GetWindow(this) };
                    successDialog.ShowDialog();

                    // Переходимо в бібліотеку
                    mainWindow?.NavigateToLibrary();
                }
                else
                {
                    _ = _logger?.LogAsync($"Чекаут відхилено: {errorMessage}", LogType.WARNING);
                    var errorDialog = new ConfirmWindow("ПОМИЛКА ОПЛАТИ", string.IsNullOrEmpty(errorMessage) ? "Відхилено банком." : errorMessage, "ЗРОЗУМІЛО", "", false)
                    { Owner = Window.GetWindow(this) };
                    errorDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                _ = _logger?.LogAsync($"Критичний збій під час чекауту: {ex.Message}", LogType.ERROR);
                MessageBox.Show("Технічна помилка під час оплати.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (sender is Button finalBtn)
                {
                    finalBtn.IsEnabled = true;
                    finalBtn.Content = "ОПЛАТИТИ ЗАМОВЛЕННЯ";
                }
            }
        }

        // Автоматичне форматування дати в MM/YY
        private void TxtCardExpiry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                // 1. Тимчасово відписуємось від події, щоб уникнути нескінченного циклу
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
    }
}