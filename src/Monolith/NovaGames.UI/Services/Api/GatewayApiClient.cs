using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using NovaGames.Core.Services;

namespace NovaGames.UI.Services.Api;

public class GatewayApiClient : IGatewayApiClient
{
    private readonly HttpClient _httpClient;
    private readonly UserSession _userSession;
    private readonly TokenStorageService _tokenStorage;

    public GatewayApiClient(
        HttpClient httpClient,
        UserSession userSession,
        TokenStorageService tokenStorage)
    {
        _httpClient = httpClient;
        _userSession = userSession;
        _tokenStorage = tokenStorage;
    }

    public async Task<(bool Success, string? ErrorMessage, ClientAuthResponse? Data)> LoginAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        try
        {
            var req = new ClientLoginRequest { Email = email.Trim(), Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", req, ct);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<ClientAuthResponse>(cancellationToken: ct);
                if (data != null)
                {
                    _userSession.Token = data.AccessToken;
                    _userSession.RefreshToken = data.RefreshToken;
                    _tokenStorage.SaveTokens(data.AccessToken, data.RefreshToken);
                    return (true, null, data);
                }
            }

            var problem = await ParseProblemDetailsAsync(response, ct);
            var error = problem?.Detail ?? problem?.Title ?? "Невірний email або пароль.";
            return (false, error, null);
        }
        catch (RateLimitExceededException ex)
        {
            return (false, $"Забагато спроб входу. Зачекайте {ex.RetryAfterSeconds} с перед повтором.", null);
        }
        catch (ApiException ex)
        {
            return (false, ex.Message, null);
        }
        catch (Exception ex)
        {
            return (false, "Помилка зв'язку зі шлюзом. Перевірте, чи запущено API Gateway (порт 5000).", null);
        }
    }

    public Task LogoutAsync()
    {
        _userSession.Token = string.Empty;
        _userSession.RefreshToken = string.Empty;
        _userSession.CurrentUser = null;
        _tokenStorage.ClearToken();
        return Task.CompletedTask;
    }

    public async Task<ClientPagedResult<ClientGameDto>> GetGamesCatalogAsync(
        int page = 1,
        int pageSize = 12,
        string? search = null,
        int? categoryId = null,
        CancellationToken ct = default)
    {
        var url = $"/api/v1/games?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search.Trim())}";
        }
        if (categoryId.HasValue)
        {
            url += $"&categoryId={categoryId.Value}";
        }

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ClientPagedResult<ClientGameDto>>(cancellationToken: ct);
                return result ?? new ClientPagedResult<ClientGameDto>();
            }
        }
        catch (Exception)
        {
            // Повертаємо порожній список при збої
        }

        return new ClientPagedResult<ClientGameDto>();
    }

    public async Task<ClientGameStoreDetailsDto?> GetGameDetailsAsync(int gameId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/store/game-details/{gameId}", ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ClientGameStoreDetailsDto>(cancellationToken: ct);
            }
        }
        catch (Exception)
        {
            // У разі помилки віддаємо null для переходу екрана в стан Error
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
        idempotencyKey ??= Guid.NewGuid().ToString();

        var orderRequest = new ClientCreateOrderRequest
        {
            PaymentMethod = paymentMethod,
            CustomerNotes = notes,
            Items = new List<ClientCreateOrderItemRequest>
            {
                new() { GameId = gameId, Quantity = 1 }
            }
        };

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

            var err = problem?.Detail ?? problem?.Title ?? $"Помилка оформлення (HTTP {(int)response.StatusCode})";
            return (false, err, null);
        }
        catch (RateLimitExceededException ex)
        {
            return (false, $"Перевищено ліміт запитів. Зачекайте {ex.RetryAfterSeconds} с.", null);
        }
        catch (ApiException ex)
        {
            return (false, ex.Message, null);
        }
        catch (Exception ex)
        {
            return (false, "Не вдалося з'єднатися зі шлюзом для створення замовлення.", null);
        }
    }

    public async Task<ClientPagedResult<ClientOrderDto>> GetMyOrdersAsync(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/orders?page={page}&pageSize={pageSize}", ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ClientPagedResult<ClientOrderDto>>(cancellationToken: ct);
                return result ?? new ClientPagedResult<ClientOrderDto>();
            }
        }
        catch (Exception)
        {
            // Помилка обробляється клієнтським екраном
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
            return (false, problem?.Detail ?? "Не вдалося скасувати замовлення.");
        }
        catch (Exception ex)
        {
            return (false, "Помилка зв'язку зі шлюзом.");
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
