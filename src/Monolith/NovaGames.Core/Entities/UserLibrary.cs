using System;

namespace NovaGames.Core.Entities
{
    public class UserLibrary
    {
        public int UserId { get; set; }    // PK, FK
        public int ProductId { get; set; } // PK, FK
        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = "Без категорії";
        public DateTime? LastPlayedAt { get; set; }
        public long PlaytimeSeconds { get; set; } = 0;
    }
}