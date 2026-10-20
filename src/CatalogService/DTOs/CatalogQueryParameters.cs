using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs;

public class CatalogQueryParameters
{
    [Range(1, int.MaxValue, ErrorMessage = "Номер сторінки повинен бути не менше 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Розмір сторінки повинен бути в межах від 1 до 100.")]
    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public string? SortBy { get; set; }

    public bool Desc { get; set; } = false;
}
