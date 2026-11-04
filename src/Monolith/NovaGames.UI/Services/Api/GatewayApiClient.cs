using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NovaGames.UI.Services.Api;

public class GatewayApiClient : IGatewayApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GatewayConfig _config;

    public GatewayApiClient(HttpClient httpClient, GatewayConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public GatewayApiClient(HttpClient httpClient, NovaGames.Core.Services.UserSession? session = null, TokenStorageService? tokenStorage = null)
        : this(httpClient, new GatewayConfig())
    {
    }

    public async Task<(bool Success, string? ErrorMessage, ClientAuthResponse? Data)> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var body = new ClientLoginRequest { Email = email, Password = password };
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", body, ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<ClientAuthResponse>(cancellationToken: ct);
                if (data != null && !string.IsNullOrEmpty(data.AccessToken))
                {
                    _config.AccessToken = data.AccessToken;
                    _config.RefreshToken = data.RefreshToken;
                }
                return (true, null, data);
            }

            var problem = await ParseProblemDetailsAsync(response, ct);
            var err = problem?.Detail ?? problem?.Title ?? $"Помилка входу (HTTP {(int)response.StatusCode})";
            return (false, err, null);
        }
        catch (RateLimitExceededException ex)
        {
            return (false, ex.Message, null);
        }
        catch (Exception ex)
        {
            return (false, $"Не вдалося підключитися до сервера: {ex.Message}", null);
        }
    }

    public Task LogoutAsync()
    {
        _config.ClearTokens();
        return Task.CompletedTask;
    }

    public async Task<ClientPagedResult<ClientGameDto>> GetGamesCatalogAsync(
        int page = 1,
        int pageSize = 12,
        string? search = null,
        int? categoryId = null,
        CancellationToken ct = default)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";
        if (categoryId.HasValue)
            query += $"&categoryId={categoryId.Value}";

        var response = await _httpClient.GetAsync($"/api/v1/catalog/items{query}", ct);
        if (response.IsSuccessStatusCode)
        {
            var paged = await response.Content.ReadFromJsonAsync<ClientPagedResult<ClientGameDto>>(cancellationToken: ct);
            return paged ?? new ClientPagedResult<ClientGameDto>();
        }

        return new ClientPagedResult<ClientGameDto>();
    }

    public async Task<ClientGameStoreDetailsDto?> GetGameDetailsAsync(int gameId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/store/game-details/{gameId}", ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ClientGameStoreDetailsDto>(cancellationToken: ct);
        }

        return null;
    }

    public async Task<(bool Success, string? ErrorMessage, ClientOrderDto? Order)> CreateOrderAsync(
        int gameId,
        string paymentMethod = "Card",
        string? notes = null,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        var orderRequest = new ClientCreateOrderRequest
        {
            PaymentMethod = paymentMethod,
            CustomerNotes = notes,
            Items = new List<ClientCreateOrderItemRequest>
            {
                new() { GameId = gameId, Quantity = 1 }
            }
        };

        return await CreateOrderAsync(orderRequest, idempotencyKey, ct);
    }

    public async Task<(bool Success, string? ErrorMessage, ClientOrderDto? Order)> CreateOrderAsync(
        ClientCreateOrderRequest orderRequest,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        idempotencyKey ??= Guid.NewGuid().ToString();

        var requestMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(orderRequest)
        };
        requestMsg.Headers.Add("Idempotency-Key", idempotencyKey);

        try
        {
            var response = await _httpClient.SendAsync(requestMsg, ct);
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<ClientOrderDto>(cancellationToken: ct);
                return (true, null, order);
            }

            var problem = await ParseProblemDetailsAsync(response, ct);
            if (problem?.Errors != null && problem.Errors.Any())
            {
                var validationMsg = string.Join("; ", problem.Errors.SelectMany(kv => kv.Value));
                return (false, validationMsg, null);
            }

            var err = problem?.Detail ?? problem?.Title ?? $"Помилка створення замовлення (HTTP {(int)response.StatusCode})";
            return (false, err, null);
        }
        catch (RateLimitExceededException ex)
        {
            return (false, ex.Message, null);
        }
        catch (Exception ex)
        {
            return (false, $"Помилка мережі при оформленні замовлення: {ex.Message}", null);
        }
    }

    public async Task<ClientPagedResult<ClientOrderDto>> GetMyOrdersAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/orders/my-orders?page={page}&pageSize={pageSize}", ct);
        if (response.IsSuccessStatusCode)
        {
            var paged = await response.Content.ReadFromJsonAsync<ClientPagedResult<ClientOrderDto>>(cancellationToken: ct);
            return paged ?? new ClientPagedResult<ClientOrderDto>();
        }

        return new ClientPagedResult<ClientOrderDto>();
    }

    public async Task<(bool Success, string? ErrorMessage)> CancelOrderAsync(int orderId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/orders/{orderId}", ct);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var problem = await ParseProblemDetailsAsync(response, ct);
            var err = problem?.Detail ?? problem?.Title ?? $"Не вдалося скасувати замовлення (HTTP {(int)response.StatusCode})";
            return (false, err);
        }
        catch (RateLimitExceededException ex)
        {
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            return (false, $"Помилка запиту: {ex.Message}");
        }
    }

    private static async Task<ClientProblemDetails?> ParseProblemDetailsAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ClientProblemDetails>(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }
}