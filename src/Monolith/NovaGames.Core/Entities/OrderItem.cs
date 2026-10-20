using NovaGames.Core.Interfaces;

namespace NovaGames.Core.Entities
{
    public class OrderItem : IEntity
    {
        public int Id { get; set; }
        public int OrderId { get; set; }   // FK на замовлення
        public int ProductId { get; set; } // FK на гру
        public decimal PriceAtPurchase { get; set; }
        public int Quantity { get; set; }
    }
}