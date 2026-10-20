namespace CatalogService.DTOs;

public class GameItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal FinalPrice => Price * (1 - (Discount / 100));
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public string? IconUrl { get; set; }
    public List<string> Genres { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
