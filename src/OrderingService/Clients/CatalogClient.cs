using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace OrderingService.Clients;

public class CatalogServiceException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public CatalogServiceException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class CatalogClient : ICatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(HttpClient httpClient, ILogger<CatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CatalogItemResponse?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/catalog/items/{id}", linkedCts.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CatalogService returned non-success status code {StatusCode} for item {Id}", response.StatusCode, id);
                throw new CatalogServiceException("Сервіс каталогу тимчасово недоступний або повернув помилку.", HttpStatusCode.ServiceUnavailable);
            }

            return await response.Content.ReadFromJsonAsync<CatalogItemResponse>(cancellationToken: linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Timeout calling CatalogService for item {Id}", id);
            throw new CatalogServiceException("Перевищено час очікування відповіді від Catalog-сервісу (дедлайн 2с).", HttpStatusCode.GatewayTimeout);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling CatalogService for item {Id}", id);
            throw new CatalogServiceException("Помилка зв'язку із сервісом каталогу. Сервіс недоступний.", HttpStatusCode.ServiceUnavailable);
        }
    }
}
