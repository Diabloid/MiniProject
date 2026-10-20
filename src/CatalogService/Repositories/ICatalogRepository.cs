using CatalogService.Entities;
using MiniProject.Shared.Common;

namespace CatalogService.Repositories;

public interface ICatalogRepository
{
    Task<PagedResult<GameItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? categoryId = null,
        string? sortBy = null,
        bool desc = false);

    Task<GameItem?> GetByIdAsync(int id);
    Task<bool> CategoryExistsAsync(int categoryId);
    Task<List<Category>> GetCategoriesAsync();
    Task AddAsync(GameItem item);
    void Update(GameItem item);
    Task<int> SaveChangesAsync();
}
