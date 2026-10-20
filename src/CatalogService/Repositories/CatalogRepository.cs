using CatalogService.Data;
using CatalogService.Entities;
using Microsoft.EntityFrameworkCore;
using MiniProject.Shared.Common;

namespace CatalogService.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly CatalogDbContext _context;

    public CatalogRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<GameItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? categoryId = null,
        string? sortBy = null,
        bool desc = false)
    {
        var query = _context.Items
            .Include(g => g.Category)
            .Where(g => g.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(g => EF.Functions.Like(g.Title, $"%{term}%") || EF.Functions.Like(g.Description, $"%{term}%"));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(g => g.CategoryId == categoryId.Value);
        }

        query = (sortBy?.Trim().ToLower(), desc) switch
        {
            ("title", false) => query.OrderBy(g => g.Title),
            ("title", true) => query.OrderByDescending(g => g.Title),
            ("price", false) => query.OrderBy(g => g.Price),
            ("price", true) => query.OrderByDescending(g => g.Price),
            ("discount", false) => query.OrderBy(g => g.Discount),
            ("discount", true) => query.OrderByDescending(g => g.Discount),
            ("createdat", false) => query.OrderBy(g => g.CreatedAt),
            ("createdat", true) => query.OrderByDescending(g => g.CreatedAt),
            _ => desc ? query.OrderByDescending(g => g.Id) : query.OrderBy(g => g.Id)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GameItem>(items, totalCount, page, pageSize);
    }

    public async Task<GameItem?> GetByIdAsync(int id)
    {
        return await _context.Items
            .Include(g => g.Category)
            .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);
    }

    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _context.Categories.AnyAsync(c => c.Id == categoryId);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task AddAsync(GameItem item)
    {
        await _context.Items.AddAsync(item);
    }

    public void Update(GameItem item)
    {
        _context.Items.Update(item);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
