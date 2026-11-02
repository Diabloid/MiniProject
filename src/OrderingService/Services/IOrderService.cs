using MiniProject.Shared.Common;
using OrderingService.DTOs;

namespace OrderingService.Services;

public interface IOrderService
{
    Task<Result<OrderDto>> CreateOrderAsync(int userId, CreateOrderRequest request, string? idempotencyKey = null, CancellationToken ct = default);
    Task<Result<OrderDto>> GetOrderByIdAsync(int id, int currentUserId, bool isAdmin, CancellationToken ct = default);
    Task<Result<PagedResult<OrderDto>>> GetUserOrdersPagedAsync(int userId, OrderQueryParameters parameters, CancellationToken ct = default);
    Task<Result<PagedResult<OrderDto>>> GetAllOrdersPagedAsync(OrderQueryParameters parameters, CancellationToken ct = default);
    Task<Result> CancelOrderAsync(int id, int currentUserId, bool isAdmin, CancellationToken ct = default);
    Task<Result<OrderDto>> UpdateOrderAsync(int id, int currentUserId, UpdateOrderRequest request, CancellationToken ct = default);
}
