using System.Net;
using Microsoft.Extensions.Logging;
using MiniProject.Shared.Common;
using OrderingService.Clients;
using OrderingService.DTOs;
using OrderingService.Entities;
using OrderingService.Repositories;

namespace OrderingService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository repository,
        ICatalogClient catalogClient,
        ILogger<OrderService> logger)
    {
        _repository = repository;
        _catalogClient = catalogClient;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(
        int userId,
        CreateOrderRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return Result<OrderDto>.Failure("Замовлення повинно містити щонайменше один товар.", HttpStatusCode.BadRequest);
        }

        // 1. Перевірка ідемпотентності
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingOrder = await _repository.GetByIdempotencyKeyAsync(idempotencyKey.Trim(), ct);
            if (existingOrder != null)
            {
                _logger.LogInformation("Повернено існуюче замовлення за IdempotencyKey: {Key}", idempotencyKey);
                return Result<OrderDto>.Success(MapToDto(existingOrder), HttpStatusCode.OK);
            }
        }

        // 2. Валідація кількості
        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
            {
                return Result<OrderDto>.Failure("Кількість товару повинна бути не менше 1.", HttpStatusCode.BadRequest);
            }
        }

        // 3. Міжсервісна перевірка каталогу та отримання ItemSnapshot
        var orderItems = new List<OrderItem>();
        decimal calculatedTotal = 0;

        foreach (var itemReq in request.Items)
        {
            CatalogItemResponse? catalogItem;
            try
            {
                catalogItem = await _catalogClient.GetItemByIdAsync(itemReq.GameId, ct);
            }
            catch (CatalogServiceException ex)
            {
                _logger.LogError("Помилка зв'язку з каталогом: {Message}", ex.Message);
                return Result<OrderDto>.Failure(ex.Message, ex.StatusCode);
            }

            if (catalogItem == null)
            {
                return Result<OrderDto>.Failure($"Товар з ID {itemReq.GameId} не знайдено в каталозі.", HttpStatusCode.NotFound);
            }

            if (!catalogItem.IsActive)
            {
                return Result<OrderDto>.Failure($"Товар '{catalogItem.Title}' наразі недоступний для покупки.", HttpStatusCode.BadRequest);
            }

            var unitPrice = catalogItem.FinalPrice > 0 ? catalogItem.FinalPrice : catalogItem.Price;
            var subtotal = unitPrice * itemReq.Quantity;
            calculatedTotal += subtotal;

            orderItems.Add(new OrderItem
            {
                GameId = catalogItem.Id,
                Title = catalogItem.Title,
                UnitPrice = unitPrice,
                Quantity = itemReq.Quantity
            });
        }

        // 4. Створення замовлення
        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Completed,
            TotalAmount = calculatedTotal,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Card" : request.PaymentMethod,
            CustomerNotes = request.CustomerNotes?.Trim(),
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            CreatedAt = DateTime.UtcNow,
            Items = orderItems
        };

        await _repository.AddAsync(order, ct);
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation("Створено замовлення ID {OrderId} для користувача {UserId} на суму {Total}", order.Id, userId, order.TotalAmount);
        return Result<OrderDto>.Success(MapToDto(order), HttpStatusCode.Created);
    }

    public async Task<Result<OrderDto>> GetOrderByIdAsync(int id, int currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(id, ct);

        // Безпека: якщо замовлення чуже і користувач не адмін, повертаємо 404 (замість 403)
        if (order == null || (!isAdmin && order.UserId != currentUserId))
        {
            return Result<OrderDto>.Failure("Замовлення не знайдено.", HttpStatusCode.NotFound);
        }

        return Result<OrderDto>.Success(MapToDto(order));
    }

    public async Task<Result<UserGameOrderStatusDto>> GetUserGameOrderStatusAsync(int userId, int gameId, CancellationToken ct = default)
    {
        var order = await _repository.GetUserActiveOrderForGameAsync(userId, gameId, ct);
        if (order == null)
        {
            return Result<UserGameOrderStatusDto>.Success(new UserGameOrderStatusDto
            {
                HasPurchased = false,
                OrderId = null,
                Status = null,
                PurchaseDate = null
            });
        }

        return Result<UserGameOrderStatusDto>.Success(new UserGameOrderStatusDto
        {
            HasPurchased = true,
            OrderId = order.Id,
            Status = order.Status.ToString(),
            PurchaseDate = order.CreatedAt
        });
    }

    public async Task<Result<PagedResult<OrderDto>>> GetUserOrdersPagedAsync(
        int userId,
        OrderQueryParameters parameters,
        CancellationToken ct = default)
    {
        var page = parameters.Page < 1 ? 1 : parameters.Page;
        var pageSize = parameters.PageSize is < 1 or > 100 ? 10 : parameters.PageSize;

        var paged = await _repository.GetPagedOrdersAsync(
            userId,
            page,
            pageSize,
            parameters.Status,
            parameters.SortBy,
            parameters.Desc,
            ct);

        var dtos = paged.Items.Select(MapToDto).ToList();
        return Result<PagedResult<OrderDto>>.Success(new PagedResult<OrderDto>(dtos, paged.TotalCount, page, pageSize));
    }

    public async Task<Result<PagedResult<OrderDto>>> GetAllOrdersPagedAsync(
        OrderQueryParameters parameters,
        CancellationToken ct = default)
    {
        var page = parameters.Page < 1 ? 1 : parameters.Page;
        var pageSize = parameters.PageSize is < 1 or > 100 ? 10 : parameters.PageSize;

        var paged = await _repository.GetPagedOrdersAsync(
            null,
            page,
            pageSize,
            parameters.Status,
            parameters.SortBy,
            parameters.Desc,
            ct);

        var dtos = paged.Items.Select(MapToDto).ToList();
        return Result<PagedResult<OrderDto>>.Success(new PagedResult<OrderDto>(dtos, paged.TotalCount, page, pageSize));
    }

    public async Task<Result> CancelOrderAsync(int id, int currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(id, ct);
        if (order == null || (!isAdmin && order.UserId != currentUserId))
        {
            return Result.Failure("Замовлення не знайдено.", HttpStatusCode.NotFound);
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return Result.Failure("Замовлення вже було скасовано раніше.", HttpStatusCode.BadRequest);
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        _repository.Update(order);
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation("Замовлення {OrderId} скасовано користувачем {UserId}", id, currentUserId);
        return Result.Success(HttpStatusCode.OK);
    }

    public async Task<Result<OrderDto>> UpdateOrderAsync(int id, int currentUserId, UpdateOrderRequest request, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(id, ct);
        if (order == null || order.UserId != currentUserId)
        {
            return Result<OrderDto>.Failure("Замовлення не знайдено.", HttpStatusCode.NotFound);
        }

        order.CustomerNotes = request.CustomerNotes?.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        _repository.Update(order);
        await _repository.SaveChangesAsync(ct);

        return Result<OrderDto>.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        Status = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        PaymentMethod = o.PaymentMethod,
        CustomerNotes = o.CustomerNotes,
        OrderDate = o.CreatedAt,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            GameId = i.GameId,
            Title = i.Title,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };
}
