using ApiGateway.DTOs;

namespace ApiGateway.Services;

public interface IGatewayCacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T?>> factory);
    void InvalidateCatalog();
    CacheMetricsDto GetMetrics();
}
