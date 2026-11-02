using Microsoft.EntityFrameworkCore;
using MiniProject.Shared.Common;
using OrderingService.Data;
using OrderingService.Entities;

namespace OrderingService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderingDbContext _context;

    public OrderRepository(OrderingDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);
    }

    public async Task<PagedResult<Order>> GetPagedOrdersAsync(
        int? userId,
        int page,
        int pageSize,
        string? status,
        string? sortBy,
        bool desc,
        CancellationToken ct = default)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        query = (sortBy?.Trim().ToLower(), desc) switch
        {
            ("totalamount", false) => query.OrderBy(o => o.TotalAmount),
            ("totalamount", true) => query.OrderByDescending(o => o.TotalAmount),
            ("createdat", false) => query.OrderBy(o => o.CreatedAt),
            ("createdat", true) => query.OrderByDescending(o => o.CreatedAt),
            _ => desc ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id)
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Order>(items, totalCount, page, pageSize);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
    }

    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
