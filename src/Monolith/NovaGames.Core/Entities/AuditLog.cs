using NovaGames.Core.Interfaces;
using System;

namespace NovaGames.Core.Entities
{
    public class AuditLog : IEntity
    {
        public int Id { get; set; }
        public int? UserId { get; set; } // Лог може бути системним (без юзера)
        public string ActionType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}