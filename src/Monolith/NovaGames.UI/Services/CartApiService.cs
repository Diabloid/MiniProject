using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using NovaGames.UI.Services.Api;

namespace NovaGames.UI.Services
{
    public class CartApiService
    {
        private readonly UserSession _userSession;
        private readonly IGatewayApiClient _gatewayClient;
        private readonly GameApiService _gameApiService;

        private readonly List<Game> _cartGames = new();
        public HashSet<int> CartGameIds { get; private set; } = new HashSet<int>();

        public event Action<int>? CartCountChanged;

        public CartApiService(UserSession userSession, IGatewayApiClient gatewayClient, GameApiService gameApiService)
        {
            _userSession = userSession;
            _gatewayClient = gatewayClient;
            _gameApiService = gameApiService;
        }

        public async Task LoadMyCartIdsAsync()
        {
            // У клієнтському рішенні кошик синхронізований у пам'яті
            CartGameIds = new HashSet<int>(_cartGames.Select(g => g.Id));
            CartCountChanged?.Invoke(CartGameIds.Count);
            await Task.CompletedTask;
        }

        public async Task<List<Game>> GetCartGamesAsync()
        {
            return await Task.FromResult(_cartGames.ToList());
        }

        public async Task<bool> AddToCartAsync(int productId)
        {
            if (CartGameIds.Contains(productId)) return true;

            var game = await _gameApiService.GetGameByIdAsync(productId);
            if (game != null)
            {
                _cartGames.Add(game);
                CartGameIds.Add(productId);
                CartCountChanged?.Invoke(CartGameIds.Count);
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveFromCartAsync(int productId)
        {
            var existing = _cartGames.FirstOrDefault(g => g.Id == productId);
            if (existing != null)
            {
                _cartGames.Remove(existing);
            }
            CartGameIds.Remove(productId);
            CartCountChanged?.Invoke(CartGameIds.Count);
            return await Task.FromResult(true);
        }

        public async Task<(bool Success, string ErrorMessage, CheckoutResponse? Data)> CheckoutAsync(CheckoutRequest request)
        {
            try
            {
                if (_cartGames.Count == 0)
                {
                    return (false, "Кошик порожній. Додайте гру перед оформленням.", null);
                }

                // Створення замовлення в OrderingService через API Gateway з Idempotency-Key
                var createOrderRequest = new ClientCreateOrderRequest
                {
                    PaymentMethod = request.PaymentMethod,
                    CustomerNotes = $"Оплата через {request.PaymentMethod} з NovaGames Client",
                    Items = _cartGames.Select(g => new ClientCreateOrderItemRequest
                    {
                        GameId = g.Id,
                        Quantity = 1
                    }).ToList()
                };

                var (success, error, order) = await _gatewayClient.CreateOrderAsync(createOrderRequest);

                if (success && order != null)
                {
                    var addedIds = _cartGames.Select(g => g.Id).ToList();
                    _cartGames.Clear();
                    CartGameIds.Clear();
                    CartCountChanged?.Invoke(0);

                    return (true, string.Empty, new CheckoutResponse
                    {
                        NewBalance = 0,
                        AddedToLibraryIds = addedIds
                    });
                }
                else
                {
                    return (false, error ?? "Не вдалося створити замовлення в OrderingService.", null);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Помилка оформлення: {ex.Message}", null);
            }
        }
    }

    public class CheckoutResponse
    {
        public decimal NewBalance { get; set; }
        public List<int> AddedToLibraryIds { get; set; } = new();
    }

    public class CheckoutRequest
    {
        public string PaymentMethod { get; set; } = "Wallet";
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public bool SaveCard { get; set; }
    }
}