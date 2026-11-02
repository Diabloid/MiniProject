using ApiGateway.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Middleware;

public record CachedHttpResponse(byte[] Body, string ContentType, int StatusCode);

public class GatewayCatalogCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayCatalogCacheMiddleware> _logger;

    public GatewayCatalogCacheMiddleware(RequestDelegate next, ILogger<GatewayCatalogCacheMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IGatewayCacheService cacheService, IMemoryCache memoryCache)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        var isCatalogEndpoint = path.StartsWith("/api/v1/catalog", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/api/v1/games", StringComparison.OrdinalIgnoreCase);

        // 1. Кешування тільки для спільних даних каталогу (GET)
        if (HttpMethods.IsGet(method) && isCatalogEndpoint)
        {
            var cacheKey = $"catalog:http:{path}:{context.Request.QueryString}";

            if (memoryCache.TryGetValue(cacheKey, out CachedHttpResponse? cached) && cached != null)
            {
                _logger.LogInformation("Cache HIT for {Path}{Query}", path, context.Request.QueryString);
                context.Response.Headers["X-Cache"] = "HIT";
                context.Response.ContentType = cached.ContentType;
                context.Response.StatusCode = cached.StatusCode;
                await context.Response.Body.WriteAsync(cached.Body);
                return;
            }

            _logger.LogInformation("Cache MISS for {Path}{Query}", path, context.Request.QueryString);
            context.Response.Headers["X-Cache"] = "MISS";

            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status200OK)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var bodyBytes = responseBodyStream.ToArray();
                var contentType = context.Response.ContentType ?? "application/json";

                var cachedEntry = new CachedHttpResponse(bodyBytes, contentType, context.Response.StatusCode);
                await cacheService.GetOrCreateAsync(cacheKey, TimeSpan.FromSeconds(60), () => Task.FromResult<CachedHttpResponse?>(cachedEntry));

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            else
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }

            return;
        }

        // 2. Інвалідизація після мутацій (POST, PUT, DELETE, PATCH)
        if ((HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method))
            && isCatalogEndpoint)
        {
            await _next(context);

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                _logger.LogInformation("Інвалідація кешу каталогу після успішної операції {Method} {Path}", method, path);
                cacheService.InvalidateCatalog();
            }

            return;
        }

        // Всі інші запити (персональні дані: замовлення, профіль, тощо) йдуть без кешування
        await _next(context);
    }
}
