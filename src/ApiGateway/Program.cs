using System.Security.Claims;
using System.Threading.RateLimiting;
using ApiGateway.Clients;
using ApiGateway.Middleware;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniProject.Shared.OpenApi;
using MiniProject.Shared.Security;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Конфігурація Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 2. Додавання контролерів та OpenAPI (Scalar UI)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddConfiguredOpenApi();

// 3. Автентифікація JWT на шлюзі
builder.Services.AddJwtAuth(builder.Configuration);

// 4. Локальне кешування в пам'яті (IMemoryCache)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IGatewayCacheService, GatewayCacheService>();

// 5. Міжсервісні HTTP-клієнти з жорстким таймаутом у 2 секунди
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    var catalogUrl = builder.Configuration["Services:Catalog"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(catalogUrl);
    client.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddHttpClient<IOrderingServiceClient, OrderingServiceClient>(client =>
{
    var orderingUrl = builder.Configuration["Services:Ordering"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(orderingUrl);
    client.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddScoped<IStoreAggregationService, StoreAggregationService>();

// 6. Обмеження частоти запитів (Rate Limiting)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Забагато запитів",
            Detail = "Перевищено ліміт запитів до API Gateway. Зачекайте 60 секунд перед наступною спробою.",
            Instance = context.HttpContext.Request.Path
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: ct);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        // А. Жорсткий ліміт на логін (5 запитів на хвилину)
        if (path.StartsWith("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"login_{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        // Б. Авторизовані користувачі (60 запитів на хвилину)
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ip;
            return RateLimitPartition.GetSlidingWindowLimiter(
                $"user_{userId}",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        // В. Анонімні користувачі (20 запитів на хвилину)
        return RateLimitPartition.GetSlidingWindowLimiter(
            $"anon_{ip}",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// 7. Ocelot Gateway
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Маршрутизація
app.UseRouting();

// Rate Limiter
app.UseRateLimiter();

// Автентифікація та авторизація на шлюзі
app.UseAuthentication();
app.UseAuthorization();

// Кастомні Middleware шлюзу
app.UseMiddleware<GatewayAuthenticationMiddleware>();
app.UseMiddleware<GatewayCatalogCacheMiddleware>();

// Явна реєстрація ендпоінтів контролерів шлюзу перед передачею в Ocelot
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapOpenApi();
    endpoints.MapScalarApiReference();

    endpoints.MapGet("/", async context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html lang='uk'>
<head>
    <meta charset='utf-8'/>
    <title>NovaGames API Gateway (Lab 3)</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0f172a; color: #f8fafc; padding: 40px; margin: 0; }
        .card { background: #1e293b; border-radius: 12px; padding: 24px; max-width: 800px; margin: 0 auto; box-shadow: 0 10px 25px rgba(0,0,0,0.5); border: 1px solid #334155; }
        h1 { color: #38bdf8; margin-top: 0; }
        p { color: #94a3b8; font-size: 16px; }
        ul { list-style: none; padding: 0; }
        li { margin-bottom: 12px; }
        a { display: inline-block; padding: 10px 16px; background: #2563eb; color: #fff; text-decoration: none; border-radius: 8px; font-weight: 600; transition: background 0.2s; }
        a:hover { background: #1d4ed8; }
        .badge { display: inline-block; padding: 4px 8px; border-radius: 4px; font-size: 12px; margin-left: 8px; font-weight: bold; background: #334155; color: #38bdf8; }
        .code-block { background: #090d16; padding: 12px; border-radius: 6px; font-family: monospace; color: #a5f3fc; font-size: 13px; margin-top: 15px; }
    </style>
</head>
<body>
    <div class='card'>
        <h1>🎮 NovaGames API Gateway (Lab 3 Expanded)</h1>
        <p>Шлюз розширено: Складений запит (Composite API), Кешування, Rate Limiting, Автентифікація.</p>
        <ul>
            <li>
                <a href='/scalar/v1' target='_blank'>Gateway API Documentation (Scalar UI)</a>
                <span class='badge'>Port 5000</span>
            </li>
            <li>
                <a href='/api/v1/store/game-details/1' target='_blank'>Composite Endpoint: Game Details</a>
                <span class='badge'>Parallel Catalog + Ordering</span>
            </li>
            <li>
                <a href='/api/v1/gateway/cache-metrics' target='_blank'>Gateway Cache Metrics</a>
                <span class='badge'>Hits / Misses</span>
            </li>
            <li>
                <a href='/health' style='background: #10b981;'>Aggregated Health Check</a>
                <span class='badge'>/health</span>
            </li>
        </ul>
        <div class='code-block'>
            Маршрутизація шлюзу:<br/>
            GET  /api/v1/store/game-details/{id} ➔ Composite (Catalog + Ordering in parallel)<br/>
            POST /api/v1/auth/login ➔ AuthService (5001) [RateLimit: 5 req/min]<br/>
            GET  /api/v1/catalog/items ➔ CatalogService (5002) [MemoryCache TTL 60s]<br/>
            POST /api/v1/orders ➔ OrderingService (5003) [X-User-Id, Idempotency-Key]<br/>
            ANY  /api/** ➔ Monolith (5042)
        </div>
    </div>
</body>
</html>");
    });
});

// Ocelot перехоплює всі запити, які не збіглися з локальними ендпоінтами шлюзу
await app.UseOcelot();

app.Run();

// Робимо Program доступним для WebApplicationFactory в інтеграційних тестах
public partial class Program { }
