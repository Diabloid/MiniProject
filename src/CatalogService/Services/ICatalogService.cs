using CatalogService.DTOs;
using MiniProject.Shared.Common;

namespace CatalogService.Services;

public interface ICatalogService
{
    Task<Result<PagedResult<GameItemDto>>> GetItemsPagedAsync(CatalogQueryParameters parameters);
    Task<Result<GameItemDetailsDto>> GetItemByIdAsync(int id);
    Task<Result<GameItemDetailsDto>> CreateItemAsync(CreateGameItemRequest request);
    Task<Result> UpdateItemAsync(int id, UpdateGameItemRequest request);
    Task<Result> DeleteItemAsync(int id);
    Task<Result<List<CategoryDto>>> GetCategoriesAsync();
}
