using NovaGames.Core.Interfaces;

namespace NovaGames.Core.Entities
{
    public class CartItem : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }     // FK на користувача
        public int ProductId { get; set; }  // FK на гру
        public int Quantity { get; set; }
    }
}