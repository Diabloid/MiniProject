using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Middleware;

public class GatewayAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayAuthenticationMiddleware> _logger;

    public GatewayAuthenticationMiddleware(RequestDelegate next, ILogger<GatewayAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Correlation ID
        if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers["X-Correlation-Id"] = correlationId;
        }
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var path = context.Request.Path.Value ?? string.Empty;

        // 2. Якщо користувач автентифікований на шлюзі, прокидаємо вниз перевірені заголовки
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roles = string.Join(",", context.User.FindAll(ClaimTypes.Role).Select(r => r.Value));

            if (!string.IsNullOrWhiteSpace(userId))
            {
                context.Request.Headers["X-User-Id"] = userId;
            }
            if (!string.IsNullOrWhiteSpace(roles))
            {
                context.Request.Headers["X-User-Roles"] = roles;
            }
        }
        else
        {
            // 3. Захищені маршрути: якщо запит без валідного токена, відбиваємо на шлюзі 401 ProblemDetails
            var isOrdersEndpoint = path.StartsWith("/api/v1/orders", StringComparison.OrdinalIgnoreCase);
            var isUsersEndpoint = path.StartsWith("/api/v1/users", StringComparison.OrdinalIgnoreCase);

            if (isOrdersEndpoint || isUsersEndpoint)
            {
                _logger.LogWarning("Спроба неавторизованого доступу до {Path}. Відбито на шлюзі кодом 401.", path);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Неавторизовано",
                    Detail = "Для доступу до цього ресурсу потрібен дійсний токен авторизації (Bearer).",
                    Instance = path
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
                return;
            }
        }

        await _next(context);
    }
}
