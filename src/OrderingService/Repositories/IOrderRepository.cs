using MiniProject.Shared.Common;
using OrderingService.Entities;

namespace OrderingService.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
    Task<Order?> GetUserActiveOrderForGameAsync(int userId, int gameId, CancellationToken ct = default);
    Task<PagedResult<Order>> GetPagedOrdersAsync(int? userId, int page, int pageSize, string? status, string? sortBy, bool desc, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    void Update(Order order);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
