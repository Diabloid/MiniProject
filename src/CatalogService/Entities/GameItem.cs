namespace CatalogService.Entities;

public class GameItem
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string ItemType { get; set; } = "Digital";
    public string? CoverUrl { get; set; }
    public string? IconUrl { get; set; }
    public string GenresJson { get; set; } = "[]";
    public double DownloadSize { get; set; }
    public int? StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
