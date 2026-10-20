using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs;

public class CreateGameItemRequest
{
    [Required(ErrorMessage = "Назва товару є обов'язковою.")]
    [MaxLength(200, ErrorMessage = "Назва не може перевищувати 200 символів.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Опис товару є обов'язковим.")]
    [MaxLength(2000, ErrorMessage = "Опис не може перевищувати 2000 символів.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1_000_000, ErrorMessage = "Ціна повинна бути в діапазоні від 0 до 1 000 000.")]
    public decimal Price { get; set; }

    [Range(0, 100, ErrorMessage = "Знижка повинна бути в діапазоні від 0% до 100%.")]
    public decimal Discount { get; set; } = 0;

    [Required(ErrorMessage = "Категорія є обов'язковою.")]
    [Range(1, int.MaxValue, ErrorMessage = "Вкажіть коректний ідентифікатор категорії.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Тип товару є обов'язковим (Digital, Physical або Subscription).")]
    [MaxLength(50)]
    public string ItemType { get; set; } = "Digital";

    [MaxLength(500)]
    public string? CoverUrl { get; set; }

    [MaxLength(500)]
    public string? IconUrl { get; set; }

    public List<string> Genres { get; set; } = new();

    public double DownloadSize { get; set; }

    public int? StockQuantity { get; set; }
}
