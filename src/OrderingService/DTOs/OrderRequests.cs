using System.ComponentModel.DataAnnotations;

namespace OrderingService.DTOs;

public class OrderItemRequest
{
    [Required(ErrorMessage = "Ідентифікатор товару є обов'язковим.")]
    [Range(1, int.MaxValue, ErrorMessage = "Ідентифікатор товару має бути більшим за 0.")]
    public int GameId { get; set; }

    [Range(1, 100, ErrorMessage = "Кількість товару повинна бути не менше 1.")]
    public int Quantity { get; set; } = 1;
}

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Список товарів не може бути порожнім.")]
    [MinLength(1, ErrorMessage = "Замовлення повинно містити щонайменше один товар.")]
    public List<OrderItemRequest> Items { get; set; } = new();

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Card";

    [MaxLength(500, ErrorMessage = "Коментар до замовлення не може перевищувати 500 символів.")]
    public string? CustomerNotes { get; set; }
}

public class UpdateOrderRequest
{
    [MaxLength(500, ErrorMessage = "Коментар до замовлення не може перевищувати 500 символів.")]
    public string? CustomerNotes { get; set; }
}
