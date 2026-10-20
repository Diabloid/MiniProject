namespace CatalogService.DTOs;

public class GameItemDetailsDto : GameItemDto
{
    public double DownloadSize { get; set; }
    public int? StockQuantity { get; set; }
}
