namespace CatalogService.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<GameItem> Items { get; set; } = new List<GameItem>();
}
