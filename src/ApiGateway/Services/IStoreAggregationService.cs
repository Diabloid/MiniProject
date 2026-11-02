using ApiGateway.DTOs;

namespace ApiGateway.Services;

public interface IStoreAggregationService
{
    Task<GameStoreDetailsDto> GetGameStoreDetailsAsync(int gameId, int? userId, string? bearerToken, CancellationToken ct = default);
}
