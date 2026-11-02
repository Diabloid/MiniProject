using System.Collections.Concurrent;
using ApiGateway.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace ApiGateway.Services;

public class GatewayCacheService : IGatewayCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _catalogKeys = new();
    private long _hits;
    private long _misses;

    public GatewayCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T?>> factory)
    {
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            Interlocked.Increment(ref _hits);
            return cachedValue;
        }

        Interlocked.Increment(ref _misses);
        var value = await factory();
        if (value != null)
        {
            _memoryCache.Set(key, value, ttl);
            _catalogKeys.TryAdd(key, 0);
        }

        return value;
    }

    public void InvalidateCatalog()
    {
        foreach (var key in _catalogKeys.Keys)
        {
            _memoryCache.Remove(key);
        }
        _catalogKeys.Clear();
    }

    public CacheMetricsDto GetMetrics()
    {
        return new CacheMetricsDto
        {
            Hits = Interlocked.Read(ref _hits),
            Misses = Interlocked.Read(ref _misses),
            CachedKeysCount = _catalogKeys.Count,
            Timestamp = DateTime.UtcNow
        };
    }
}
