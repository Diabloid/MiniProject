using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NovaGames.Core.Entities
{
    public class Review
    {
        public int Id { get; set; }

        // Хто залишив відгук
        public int UserId { get; set; }
        public User User { get; set; }

        // На який продукт залишили відгук
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Game Game { get; set; }

        // Оцінка від 1 до 5
        public int Rating { get; set; }

        public string? Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsEdited { get; set; } = false;
    }
}