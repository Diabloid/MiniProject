namespace OrderingService.Entities;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Card";
    public string? CustomerNotes { get; set; }

    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
