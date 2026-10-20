using NovaGames.Core.Interfaces;
using System.Text.Json.Serialization;

namespace NovaGames.Core.Entities
{
    public class User : IEntity
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Client;
        public decimal WalletBalance { get; set; } = 0.0m;
        public DateTime LastLoginDate { get; set; } = DateTime.UtcNow;
        public string? AvatarUrl { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? Email { get; set; }
        public string AvatarBorderColorHex { get; set; } = "#00ADEF";
        public ICollection<SavedCard> SavedCards { get; set; } = new List<SavedCard>();
    }
}