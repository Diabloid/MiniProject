using ApiGateway.DTOs;

namespace ApiGateway.Clients;

public interface ICatalogServiceClient
{
    Task<CatalogGameDto?> GetGameByIdAsync(int id, CancellationToken ct = default);
    Task<string?> GetCatalogItemsRawAsync(string queryString, CancellationToken ct = default);
}
