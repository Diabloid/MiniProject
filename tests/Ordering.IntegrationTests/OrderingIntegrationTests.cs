using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MiniProject.Shared.Common;
using OrderingService.DTOs;

namespace Ordering.IntegrationTests;

public class OrderingIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public OrderingIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetAuthorizationHeader(int userId = 1, string role = "User")
    {
        var token = _factory.GenerateJwtToken(userId, role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // Сценарій 1: Створення з коректними даними (201 Created + Location + запис)
    [Fact]
    public async Task Scenario1_Create_WithValidData_Returns201AndLocation()
    {
        SetAuthorizationHeader(userId: 1);

        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { GameId = 1, Quantity = 1 }
            },
            PaymentMethod = "Card",
            CustomerNotes = "Термінова доставка ліцензійного ключа"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.True(order.Id > 0);
        Assert.Equal(1, order.UserId);
        Assert.Equal("Completed", order.Status);
        Assert.Equal(1019.15m, order.TotalAmount);
        Assert.Single(order.Items);
        Assert.Equal("Cyberpunk 2077: Phantom Liberty", order.Items[0].Title);
    }

    // Сценарій 2: Створення без токена (401 Unauthorized)
    [Fact]
    public async Task Scenario2_Create_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { GameId = 1, Quantity = 1 } }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Сценарій 3: Створення з неіснуючим ItemId (404 Not Found)
    [Fact]
    public async Task Scenario3_Create_WithNonExistentItem_Returns404()
    {
        SetAuthorizationHeader(userId: 1);

        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { GameId = 99999, Quantity = 1 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Сценарій 4: Створення з порушенням бізнес-правила (Quantity <= 0 -> 400 Bad Request)
    [Fact]
    public async Task Scenario4_Create_WithInvalidQuantity_Returns400()
    {
        SetAuthorizationHeader(userId: 1);

        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { GameId = 1, Quantity = 0 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Сценарій 5: Повтор запиту з тим самим Idempotency-Key (повертає те саме замовлення без дублювання)
    [Fact]
    public async Task Scenario5_Create_WithSameIdempotencyKey_ReturnsSameOrder()
    {
        SetAuthorizationHeader(userId: 2);
        var idempotencyKey = Guid.NewGuid().ToString();

        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { GameId = 1, Quantity = 1 } }
        };

        var requestMsg1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        requestMsg1.Headers.Add("Idempotency-Key", idempotencyKey);
        var response1 = await _client.SendAsync(requestMsg1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var order1 = await response1.Content.ReadFromJsonAsync<OrderDto>();

        // Повторний запит із тим самим ключем
        var requestMsg2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        requestMsg2.Headers.Add("Idempotency-Key", idempotencyKey);
        var response2 = await _client.SendAsync(requestMsg2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var order2 = await response2.Content.ReadFromJsonAsync<OrderDto>();

        Assert.NotNull(order1);
        Assert.NotNull(order2);
        Assert.Equal(order1.Id, order2.Id);
        Assert.Equal(order1.TotalAmount, order2.TotalAmount);
    }

    // Сценарій 6: Отримання свого замовлення (200 OK)
    [Fact]
    public async Task Scenario6_GetById_ForOwnOrder_Returns200()
    {
        SetAuthorizationHeader(userId: 1);

        // ID 1 є у seed-даних для UserId = 1
        var response = await _client.GetAsync("/api/v1/orders/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal(1, order.Id);
        Assert.Equal(1, order.UserId);
    }

    // Сценарій 7: Спроба отримати чуже замовлення (404 Not Found — не розкриває існування)
    [Fact]
    public async Task Scenario7_GetById_ForOtherUserOrder_Returns404()
    {
        // Авторизуємося як користувач 2, пробуємо відкрити замовлення 1 (яке належить користувачу 1)
        SetAuthorizationHeader(userId: 2);

        var response = await _client.GetAsync("/api/v1/orders/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Сценарій 8: Скасування замовлення (зміна статусу на Cancelled)
    [Fact]
    public async Task Scenario8_CancelOrder_ChangesStatusToCancelled()
    {
        SetAuthorizationHeader(userId: 1);

        // Створюємо нове замовлення
        var createResp = await _client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { GameId = 1, Quantity = 1 } }
        });
        var created = await createResp.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        // Скасовуємо
        var cancelResp = await _client.PostAsync($"/api/v1/orders/{created.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        // Перевіряємо статус
        var getResp = await _client.GetAsync($"/api/v1/orders/{created.Id}");
        var updated = await getResp.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(updated);
        Assert.Equal("Cancelled", updated.Status);
    }

    // Сценарій 9: Каталог недоступний (повертає 503)
    [Fact]
    public async Task Scenario9_Create_WhenCatalogUnavailable_Returns503()
    {
        SetAuthorizationHeader(userId: 1);

        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { GameId = 503, Quantity = 1 } // FakeCatalogClient кине 503
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // Сценарій 10: Отримання списку з пагінацією та фільтром (200 OK + PagedResult)
    [Fact]
    public async Task Scenario10_GetPaged_WithStatusFilter_Returns200()
    {
        SetAuthorizationHeader(userId: 1);

        var response = await _client.GetAsync("/api/v1/orders?page=1&pageSize=5&status=Completed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        Assert.NotNull(paged);
        Assert.True(paged.Items.Count > 0);
        Assert.All(paged.Items, o => Assert.Equal("Completed", o.Status));
    }
}
