using NovaGames.Core.Interfaces;
using System;

namespace NovaGames.Core.Entities
{
    public class Order : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped
        public decimal TotalAmount { get; set; }
    }
}