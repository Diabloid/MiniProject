namespace NovaGames.UI.Services.Api;

public interface IGatewayApiClient
{
    Task<(bool Success, string? ErrorMessage, ClientAuthResponse? Data)> LoginAsync(string email, string password, CancellationToken ct = default);
    Task LogoutAsync();

    Task<ClientPagedResult<ClientGameDto>> GetGamesCatalogAsync(int page = 1, int pageSize = 12, string? search = null, int? categoryId = null, CancellationToken ct = default);

    Task<ClientGameStoreDetailsDto?> GetGameDetailsAsync(int gameId, CancellationToken ct = default);

    Task<(bool Success, string? ErrorMessage, ClientOrderDto? Order)> CreateOrderAsync(int gameId, string paymentMethod = "Card", string? notes = null, string? idempotencyKey = null, CancellationToken ct = default);

    Task<(bool Success, string? ErrorMessage, ClientOrderDto? Order)> CreateOrderAsync(ClientCreateOrderRequest request, string? idempotencyKey = null, CancellationToken ct = default);

    Task<ClientPagedResult<ClientOrderDto>> GetMyOrdersAsync(int page = 1, int pageSize = 10, CancellationToken ct = default);

    Task<(bool Success, string? ErrorMessage)> CancelOrderAsync(int orderId, CancellationToken ct = default);
}
