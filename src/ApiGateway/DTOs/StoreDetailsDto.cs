namespace ApiGateway.DTOs;

public class CatalogGameDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
}

public class UserGameStatusDto
{
    public bool HasPurchased { get; set; }
    public int? OrderId { get; set; }
    public string? Status { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? Message { get; set; }
}

public class GameStoreDetailsDto
{
    public CatalogGameDto Game { get; set; } = null!;
    public UserGameStatusDto UserStatus { get; set; } = new();
    public DateTime AggregatedAt { get; set; } = DateTime.UtcNow;
}

public class CacheMetricsDto
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public double HitRatioPercentage => Hits + Misses == 0 ? 0 : Math.Round((double)Hits / (Hits + Misses) * 100, 2);
    public int CachedKeysCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
