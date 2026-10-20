using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace NovaGames.UI.Services
{
    public class CartApiService
    {
        private readonly HttpClient _httpClient;
        private readonly UserSession _userSession;
        private const string BaseUrl = "https://localhost:7232/api/v1/cart";

        // КЕШ КОШИКА
        public HashSet<int> CartGameIds { get; private set; } = new HashSet<int>();

        public CartApiService(UserSession userSession)
        {
            _httpClient = new HttpClient();
            _userSession = userSession;
        }

        private void SetAuthHeader()
        {
            if (!string.IsNullOrEmpty(_userSession.Token))
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userSession.Token);
        }

        public async Task LoadMyCartIdsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/my-cart-ids");
                if (response.IsSuccessStatusCode)
                {
                    var ids = await response.Content.ReadFromJsonAsync<List<int>>();
                    if (ids != null)
                    {
                        CartGameIds = new HashSet<int>(ids);
                        CartCountChanged?.Invoke(CartGameIds.Count);
                    }
                }
            }
            catch {}
        }

        // ПОДІЯ ДЛЯ UI: викликається, коли кількість товарів змінилася
        public event Action<int>? CartCountChanged;

        public async Task<List<Game>> GetCartGamesAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/my-items");
                if (response.IsSuccessStatusCode)
                {
                    // Використовуємо стандартний парсер, як у GameApiService
                    var games = await response.Content.ReadFromJsonAsync<List<Game>>();
                    if (games != null)
                    {
                        CartGameIds = new HashSet<int>(games.Select(g => g.Id));
                        CartCountChanged?.Invoke(CartGameIds.Count);
                        return games;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження кошика: {ex.Message}");
            }
            return new List<Game>();
        }

        // Метод додавання в кошик
        public async Task<bool> AddToCartAsync(int productId)
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsync($"{BaseUrl}/{productId}", null);
            if (response.IsSuccessStatusCode)
            {
                CartGameIds.Add(productId);
                CartCountChanged?.Invoke(CartGameIds.Count);
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveFromCartAsync(int productId)
        {
            SetAuthHeader();
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{productId}");
            if (response.IsSuccessStatusCode)
            {
                CartGameIds.Remove(productId);
                CartCountChanged?.Invoke(CartGameIds.Count); // Оновлюємо бейдж у реальному часі
                return true;
            }
            return false;
        }

        // Метод оформлення замовлення (Оплата гаманцем)
        // Метод оформлення замовлення (Оплата гаманцем або карткою)
        public async Task<(bool Success, string ErrorMessage, CheckoutResponse? Data)> CheckoutAsync(CheckoutRequest request)
        {
            SetAuthHeader();
            try
            {
                // Відправляємо наш request у тілі POST-запиту!
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/checkout", request);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
                    CartGameIds.Clear();
                    CartCountChanged?.Invoke(0);
                    return (true, string.Empty, data);
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    return (false, errorMsg, null);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Помилка з'єднання: {ex.Message}", null);
            }
        }
    }

    // Модель відповіді після успішної покупки
    public class CheckoutResponse
    {
        public decimal NewBalance { get; set; }
        public List<int> AddedToLibraryIds { get; set; } = new();
    }

    public class CheckoutRequest
    {
        public string PaymentMethod { get; set; } = "Wallet"; // "Wallet" або "Card"

        // Дані картки (потрібні тільки якщо PaymentMethod == "Card")
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public bool SaveCard { get; set; }
    }
}