namespace OrderingService.Clients;

public class CatalogItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal FinalPrice { get; set; }
    public bool IsActive { get; set; }
}

public interface ICatalogClient
{
    Task<CatalogItemResponse?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default);
}
