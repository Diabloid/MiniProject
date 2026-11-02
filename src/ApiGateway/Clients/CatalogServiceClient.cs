using System.Net;
using System.Net.Http.Json;
using ApiGateway.DTOs;
using ApiGateway.Exceptions;
using ApiGateway.Services;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Clients;

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IGatewayCacheService _cacheService;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(
        HttpClient httpClient,
        IGatewayCacheService cacheService,
        ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<CatalogGameDto?> GetGameByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = $"catalog:item:{id}";
        return await _cacheService.GetOrCreateAsync(cacheKey, TimeSpan.FromSeconds(60), async () =>
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/catalog/items/{id}", ct);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("CatalogService returned {StatusCode}: {Error}", response.StatusCode, error);
                    throw new ServiceUnavailableException("Помилка каталогу", $"Сервіс каталогу повернув помилку: {response.StatusCode}");
                }

                return await response.Content.ReadFromJsonAsync<CatalogGameDto>(cancellationToken: ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogError("Timeout (2s) reached calling CatalogService for item {Id}", id);
                throw new GatewayTimeoutException("Перевищено таймаут (2с) очікування відповіді від сервісу каталогу.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException calling CatalogService for item {Id}", id);
                throw new ServiceUnavailableException("Каталог недоступний", "Неможливо з'єднатися із сервісом каталогу.");
            }
        });
    }

    public async Task<string?> GetCatalogItemsRawAsync(string queryString, CancellationToken ct = default)
    {
        var cacheKey = $"catalog:items:{queryString}";
        return await _cacheService.GetOrCreateAsync(cacheKey, TimeSpan.FromSeconds(60), async () =>
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(queryString)
                    ? "/api/v1/catalog/items"
                    : $"/api/v1/catalog/items{queryString}";

                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching catalog items raw");
                return null;
            }
        });
    }
}
