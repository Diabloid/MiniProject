using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using NovaGames.Core.Services;
using NovaGames.UI.Services;
using NovaGames.UI.Services.Api;

namespace Client.UnitTests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}

public class ClientApiLayerTests
{
    [Fact]
    public async Task AuthHandler_On429TooManyRequests_ParsesRetryAfterAndThrowsRateLimitException()
    {
        // Arrange: сервер повертає 429 Too Many Requests із заголовком Retry-After: 45
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            resp.Headers.Add("Retry-After", "45");
            resp.Content = JsonContent.Create(new ClientProblemDetails
            {
                Status = 429,
                Title = "Забагато запитів",
                Detail = "Перевищено ліміт запитів. Зачекайте 45 с."
            });
            return resp;
        });

        var session = new UserSession { Token = "test_token" };
        var storage = new TokenStorageService();
        var config = new GatewayConfig();

        var authHandler = new AuthHandler(session, storage, config)
        {
            InnerHandler = mockHandler
        };

        var client = new HttpClient(authHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        // Act & Assert: перевіряємо, що клієнтський шар викидає типізовану помилку з 45 секундами
        var ex = await Assert.ThrowsAsync<RateLimitExceededException>(async () =>
        {
            await client.GetAsync("/api/v1/orders");
        });

        Assert.Equal(45, ex.RetryAfterSeconds);
        Assert.NotNull(ex.ProblemDetails);
        Assert.Equal(429, ex.ProblemDetails.Status);
    }

    [Fact]
    public async Task GatewayApiClient_OnValidationError_ParsesProblemDetailsErrors()
    {
        // Arrange: сервер повертає 400 Bad Request із полем errors
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            var problem = new ClientProblemDetails
            {
                Status = 400,
                Title = "Помилка валідації",
                Errors = new Dictionary<string, string[]>
                {
                    ["quantity"] = new[] { "Кількість має бути більшою за нуль" }
                }
            };

            var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(problem)
            };
            return resp;
        });

        var client = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        var session = new UserSession();
        var storage = new TokenStorageService();
        var apiClient = new GatewayApiClient(client, session, storage);

        // Act: намагаємося створити замовлення
        var (success, errorMsg, order) = await apiClient.CreateOrderAsync(gameId: 1);

        // Assert: клієнтський шар коректно розібрав помилку валідації по полях
        Assert.False(success);
        Assert.Null(order);
        Assert.NotNull(errorMsg);
        Assert.Contains("Кількість має бути більшою за нуль", errorMsg);
    }

    [Fact]
    public async Task GatewayApiClient_CreateOrder_AttachesIdempotencyKeyHeader()
    {
        // Arrange
        string? capturedIdempotencyKey = null;

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            if (req.Headers.TryGetValues("Idempotency-Key", out var vals))
            {
                capturedIdempotencyKey = vals.FirstOrDefault();
            }

            var resp = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new ClientOrderDto
                {
                    Id = 101,
                    Status = "Completed",
                    TotalAmount = 1499.00m
                })
            };
            return resp;
        });

        var client = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        var session = new UserSession();
        var storage = new TokenStorageService();
        var apiClient = new GatewayApiClient(client, session, storage);

        var myClientKey = "my-unique-key-12345";

        // Act
        var (success, errorMsg, order) = await apiClient.CreateOrderAsync(gameId: 1, idempotencyKey: myClientKey);

        // Assert: перевіряємо, що клієнт передав Idempotency-Key у заголовок
        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal(myClientKey, capturedIdempotencyKey);
    }

    [Fact]
    public async Task AuthHandler_AttachesBearerToken_WhenTokenExists()
    {
        // Arrange
        string? capturedAuthHeader = null;

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            capturedAuthHeader = req.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var session = new UserSession { Token = "valid_jwt_access_token_123" };
        var storage = new TokenStorageService();
        var config = new GatewayConfig();

        var authHandler = new AuthHandler(session, storage, config)
        {
            InnerHandler = mockHandler
        };

        var client = new HttpClient(authHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        // Act
        var resp = await client.GetAsync("/api/v1/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("Bearer valid_jwt_access_token_123", capturedAuthHeader);
    }
}
