using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiGateway.DTOs;
using ApiGateway.Exceptions;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Clients;

public class OrderingServiceClient : IOrderingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderingServiceClient> _logger;

    public OrderingServiceClient(HttpClient httpClient, ILogger<OrderingServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserGameStatusDto?> GetUserGameStatusAsync(
        int userId,
        int gameId,
        string? bearerToken,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orders/game-status/{gameId}");
        request.Headers.Add("X-User-Id", userId.ToString());

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new UserGameStatusDto
                {
                    HasPurchased = false,
                    IsAvailable = true
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("OrderingService returned {StatusCode}: {Error}", response.StatusCode, error);
                throw new ServiceUnavailableException("Помилка замовлень", $"Сервіс замовлень повернув статус {response.StatusCode}");
            }

            var dto = await response.Content.ReadFromJsonAsync<UserGameStatusDto>(cancellationToken: ct);
            if (dto != null)
            {
                dto.IsAvailable = true;
            }
            return dto;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogError("Timeout (2s) reached calling OrderingService for game {GameId}", gameId);
            throw new GatewayTimeoutException("Перевищено таймаут (2с) очікування відповіді від сервісу замовлень.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HttpRequestException calling OrderingService for game {GameId}", gameId);
            throw new ServiceUnavailableException("Сервіс замовлень недоступний", "Неможливо з'єднатися із сервісом замовлень.");
        }
    }
}
