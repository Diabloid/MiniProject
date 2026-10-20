using System.Net;
using System.Text.Json;
using CatalogService.DTOs;
using CatalogService.Entities;
using CatalogService.Repositories;
using Microsoft.Extensions.Logging;
using MiniProject.Shared.Common;

namespace CatalogService.Services;

public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _repository;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(ICatalogRepository repository, ILogger<CatalogService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<GameItemDto>>> GetItemsPagedAsync(CatalogQueryParameters parameters)
    {
        if (parameters.Page <= 0 || parameters.PageSize <= 0 || parameters.PageSize > 100)
        {
            return Result<PagedResult<GameItemDto>>.Failure(
                "Некоректні параметри пагінації. Номер сторінки має бути >= 1, а розмір від 1 до 100.",
                HttpStatusCode.BadRequest);
        }

        var pagedItems = await _repository.GetPagedAsync(
            parameters.Page,
            parameters.PageSize,
            parameters.Search,
            parameters.CategoryId,
            parameters.SortBy,
            parameters.Desc);

        var dtos = pagedItems.Items.Select(MapToDto).ToList();
        var result = new PagedResult<GameItemDto>(dtos, pagedItems.TotalCount, parameters.Page, parameters.PageSize);

        return Result<PagedResult<GameItemDto>>.Success(result);
    }

    public async Task<Result<GameItemDetailsDto>> GetItemByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null)
        {
            return Result<GameItemDetailsDto>.Failure("Позицію каталогу не знайдено.", HttpStatusCode.NotFound);
        }

        return Result<GameItemDetailsDto>.Success(MapToDetailsDto(item));
    }

    public async Task<Result<GameItemDetailsDto>> CreateItemAsync(CreateGameItemRequest request)
    {
        _logger.LogInformation("Створення нової позиції каталогу: {Title}", request.Title);

        if (!await _repository.CategoryExistsAsync(request.CategoryId))
        {
            _logger.LogWarning("Спроба створити позицію з неіснуючою категорією ID: {CategoryId}", request.CategoryId);
            return Result<GameItemDetailsDto>.Failure("Вказана категорія не існує.", HttpStatusCode.BadRequest);
        }

        var item = new GameItem
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Discount = request.Discount,
            CategoryId = request.CategoryId,
            ItemType = request.ItemType,
            CoverUrl = request.CoverUrl,
            IconUrl = request.IconUrl,
            GenresJson = JsonSerializer.Serialize(request.Genres ?? new List<string>()),
            DownloadSize = request.DownloadSize,
            StockQuantity = request.StockQuantity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(item);
        await _repository.SaveChangesAsync();

        var createdItem = await _repository.GetByIdAsync(item.Id);
        _logger.LogInformation("Позицію каталогу успішно створено з ID: {Id}", item.Id);

        return Result<GameItemDetailsDto>.Success(MapToDetailsDto(createdItem ?? item), HttpStatusCode.Created);
    }

    public async Task<Result> UpdateItemAsync(int id, UpdateGameItemRequest request)
    {
        _logger.LogInformation("Оновлення позиції каталогу ID: {Id}", id);

        var item = await _repository.GetByIdAsync(id);
        if (item == null)
        {
            return Result.Failure("Позицію каталогу не знайдено.", HttpStatusCode.NotFound);
        }

        if (!await _repository.CategoryExistsAsync(request.CategoryId))
        {
            return Result.Failure("Вказана категорія не існує.", HttpStatusCode.BadRequest);
        }

        item.Title = request.Title.Trim();
        item.Description = request.Description.Trim();
        item.Price = request.Price;
        item.Discount = request.Discount;
        item.CategoryId = request.CategoryId;
        item.CoverUrl = request.CoverUrl;
        item.IconUrl = request.IconUrl;
        item.GenresJson = JsonSerializer.Serialize(request.Genres ?? new List<string>());
        item.DownloadSize = request.DownloadSize;
        item.StockQuantity = request.StockQuantity;

        _repository.Update(item);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Позицію каталогу ID: {Id} успішно оновлено", id);
        return Result.Success(HttpStatusCode.NoContent);
    }

    public async Task<Result> DeleteItemAsync(int id)
    {
        _logger.LogInformation("М'яке видалення позиції каталогу ID: {Id}", id);

        var item = await _repository.GetByIdAsync(id);
        if (item == null)
        {
            return Result.Failure("Позицію каталогу не знайдено.", HttpStatusCode.NotFound);
        }

        item.IsActive = false;
        _repository.Update(item);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Позицію каталогу ID: {Id} позначено як неактивну", id);
        return Result.Success(HttpStatusCode.NoContent);
    }

    public async Task<Result<List<CategoryDto>>> GetCategoriesAsync()
    {
        var categories = await _repository.GetCategoriesAsync();
        var dtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description
        }).ToList();

        return Result<List<CategoryDto>>.Success(dtos);
    }

    private static GameItemDto MapToDto(GameItem item)
    {
        List<string> genres;
        try
        {
            genres = JsonSerializer.Deserialize<List<string>>(item.GenresJson) ?? new List<string>();
        }
        catch
        {
            genres = new List<string>();
        }

        return new GameItemDto
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Price = item.Price,
            Discount = item.Discount,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            ItemType = item.ItemType,
            CoverUrl = item.CoverUrl,
            IconUrl = item.IconUrl,
            Genres = genres,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt
        };
    }

    private static GameItemDetailsDto MapToDetailsDto(GameItem item)
    {
        var baseDto = MapToDto(item);
        return new GameItemDetailsDto
        {
            Id = baseDto.Id,
            Title = baseDto.Title,
            Description = baseDto.Description,
            Price = baseDto.Price,
            Discount = baseDto.Discount,
            CategoryId = baseDto.CategoryId,
            CategoryName = baseDto.CategoryName,
            ItemType = baseDto.ItemType,
            CoverUrl = baseDto.CoverUrl,
            IconUrl = baseDto.IconUrl,
            Genres = baseDto.Genres,
            IsActive = baseDto.IsActive,
            CreatedAt = baseDto.CreatedAt,
            DownloadSize = item.DownloadSize,
            StockQuantity = item.StockQuantity
        };
    }
}
