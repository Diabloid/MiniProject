using ApiGateway.DTOs;

namespace ApiGateway.Clients;

public interface IOrderingServiceClient
{
    Task<UserGameStatusDto?> GetUserGameStatusAsync(int userId, int gameId, string? bearerToken, CancellationToken ct = default);
}
