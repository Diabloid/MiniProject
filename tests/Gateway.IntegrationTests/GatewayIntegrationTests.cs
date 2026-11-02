using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiGateway.DTOs;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.IntegrationTests;

public class GatewayIntegrationTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;
    private readonly HttpClient _client;

    public GatewayIntegrationTests(GatewayApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetAuthToken(int userId, string email, string role = "User")
    {
        var token = GatewayApiFactory.GenerateJwtToken(userId, email, role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ClearAuthToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Сценарій 1: Складений ендпоінт, усі сервіси доступні -> Повна відповідь, усі частини на місці
    /// </summary>
    [Fact]
    public async Task Scenario1_CompositeEndpoint_AllServicesAvailable_ReturnsFullResponse()
    {
        // Arrange
        _factory.FakeOrdering.ShouldThrow = false;
        _factory.FakeOrdering.UserStatuses[42] = new UserGameStatusDto
        {
            HasPurchased = true,
            OrderId = 105,
            Status = "Confirmed",
            PurchaseDate = DateTime.UtcNow.AddDays(-1),
            IsAvailable = true
        };

        SetAuthToken(42, "gamer42@novagames.com");

        // Act
        var response = await _client.GetAsync("/api/v1/store/game-details/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<GameStoreDetailsDto>();
        Assert.NotNull(dto);

        // Перевіряємо частину Каталогу
        Assert.NotNull(dto.Game);
        Assert.Equal(1, dto.Game.Id);
        Assert.Equal("S.T.A.L.K.E.R. 2: Heart of Chornobyl", dto.Game.Title);
        Assert.Equal(1499.00m, dto.Game.FinalPrice);

        // Перевіряємо частину Замовлень
        Assert.NotNull(dto.UserStatus);
        Assert.True(dto.UserStatus.IsAvailable);
        Assert.True(dto.UserStatus.HasPurchased);
        Assert.Equal(105, dto.UserStatus.OrderId);
        Assert.Equal("Confirmed", dto.UserStatus.Status);
    }

    /// <summary>
    /// Сценарій 2: Один сервіс недоступний (Ordering кидає помилку) -> Свідома часткова відповідь (graceful degradation)
    /// </summary>
    [Fact]
    public async Task Scenario2_CompositeEndpoint_OrderingServiceUnavailable_AppliesGracefulDegradation()
    {
        // Arrange
        _factory.FakeOrdering.ShouldThrow = true; // Імітуємо падіння сервісу замовлень
        SetAuthToken(99, "unlucky@novagames.com");

        // Act
        var response = await _client.GetAsync("/api/v1/store/game-details/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<GameStoreDetailsDto>();
        Assert.NotNull(dto);

        // Каталог успішно повернувся
        Assert.NotNull(dto.Game);
        Assert.Equal(1, dto.Game.Id);

        // Статус замовлення свідомо позначений як недоступний, а не мовчки false/null
        Assert.NotNull(dto.UserStatus);
        Assert.False(dto.UserStatus.IsAvailable);
        Assert.Equal("Сервіс замовлень тимчасово недоступний", dto.UserStatus.Message);
    }

    /// <summary>
    /// Сценарій 3: Запит без токена на захищений ендпоінт -> 401 ProblemDetails на шлюзі, у сервіси нічого не пішло
    /// </summary>
    [Fact]
    public async Task Scenario3_ProtectedEndpoint_WithoutToken_Returns401ProblemDetails_NothingSentDownstream()
    {
        // Arrange
        ClearAuthToken();
        var initialOrderingCalls = _factory.FakeOrdering.CallCount;

        // Act: запит на захищений маршрут замовлень без Bearer-токена
        var response = await _client.GetAsync("/api/v1/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Неавторизовано", problem.Title);

        // У захищений сервіс замовлень запит навіть не намагався йти
        Assert.Equal(initialOrderingCalls, _factory.FakeOrdering.CallCount);
    }

    /// <summary>
    /// Сценарій 4: Перевищення ліміту запитів (Rate Limiting) -> 429 і заголовок Retry-After + ProblemDetails
    /// </summary>
    [Fact]
    public async Task Scenario4_RateLimiting_ExceededLimit_Returns429WithRetryAfter()
    {
        // Arrange: створюємо окремий HttpClient для перевірки жорсткого ліміту на логін (5 req/min)
        var client = _factory.CreateClient();

        HttpResponseMessage lastResponse = null!;

        // Act: здійснюємо 6 запитів підряд на логін
        for (int i = 0; i < 7; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "pass" });
            if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        // Assert: лімітер заблокував запит кодом 429
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        Assert.True(lastResponse.Headers.Contains("Retry-After"));

        var problem = await lastResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(429, problem.Status);
        Assert.Equal("Забагато запитів", problem.Title);
    }

    /// <summary>
    /// Сценарій 5: Повторний запит тих самих спільних даних -> Другий раз сервіс не викликається (кеш спрацював)
    /// </summary>
    [Fact]
    public async Task Scenario5_RepeatedRequest_SharedData_CachedByGateway()
    {
        // Arrange
        ClearAuthToken();
        _factory.FakeOrdering.ShouldThrow = false;

        // Ініціалізуємо фабрику та гарантуємо скидання кешу перед тестом
        var ping = await _client.GetAsync("/api/v1/gateway/cache-metrics");
        Assert.Equal(HttpStatusCode.OK, ping.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<IGatewayCacheService>();
            cache.InvalidateCatalog();
        }

        var catalogFake = _factory.FakeCatalog!;
        var initialDownstreamCalls = catalogFake.DownstreamCallCount;

        // Act 1: Перший запит деталей гри ID 1
        var resp1 = await _client.GetAsync("/api/v1/store/game-details/1");
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        var callsAfterFirst = catalogFake.DownstreamCallCount;
        Assert.True(callsAfterFirst > initialDownstreamCalls);

        // Act 2: Повторний запит тих самих спільних даних гри
        var resp2 = await _client.GetAsync("/api/v1/store/game-details/1");
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        var callsAfterSecond = catalogFake.DownstreamCallCount;

        // Assert: лічильник викликів сервісу каталогу не змінився (дані віддано з кешу IMemoryCache)
        Assert.Equal(callsAfterFirst, callsAfterSecond);
    }

    /// <summary>
    /// Сценарій 6: Два різні користувачі, той самий маршрут -> Кожен отримує свої дані, персональне не кешується
    /// </summary>
    [Fact]
    public async Task Scenario6_TwoDifferentUsers_SameRoute_PersonalDataNotCached()
    {
        // Arrange
        _factory.FakeOrdering.ShouldThrow = false;

        // Користувач 101 купив гру
        _factory.FakeOrdering.UserStatuses[101] = new UserGameStatusDto
        {
            HasPurchased = true,
            OrderId = 201,
            Status = "Completed",
            PurchaseDate = DateTime.UtcNow.AddDays(-2),
            IsAvailable = true
        };

        // Користувач 102 НЕ купував гру
        _factory.FakeOrdering.UserStatuses[102] = new UserGameStatusDto
        {
            HasPurchased = false,
            OrderId = null,
            Status = null,
            PurchaseDate = null,
            IsAvailable = true
        };

        // Act 1: Запит від Користувача 101
        SetAuthToken(101, "buyer@novagames.com");
        var respUser1 = await _client.GetAsync("/api/v1/store/game-details/1");
        var dtoUser1 = await respUser1.Content.ReadFromJsonAsync<GameStoreDetailsDto>();

        // Act 2: Запит від Користувача 102
        SetAuthToken(102, "guest@novagames.com");
        var respUser2 = await _client.GetAsync("/api/v1/store/game-details/1");
        var dtoUser2 = await respUser2.Content.ReadFromJsonAsync<GameStoreDetailsDto>();

        // Assert: Спільні дані гри однакові
        Assert.Equal(dtoUser1!.Game.Title, dtoUser2!.Game.Title);

        // Але персональні статуси абсолютно різні (персональні дані не змішалися в кеші)
        Assert.True(dtoUser1.UserStatus.HasPurchased);
        Assert.Equal(201, dtoUser1.UserStatus.OrderId);

        Assert.False(dtoUser2.UserStatus.HasPurchased);
        Assert.Null(dtoUser2.UserStatus.OrderId);
    }
}
