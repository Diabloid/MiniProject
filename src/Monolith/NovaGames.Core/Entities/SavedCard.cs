using NovaGames.Core.Interfaces;
using System.Text.Json.Serialization;

namespace NovaGames.Core.Entities
{
    public class SavedCard : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // Зберігаємо ТІЛЬКИ маску, наприклад "**** **** **** 1234"
        public string MaskedCardNumber { get; set; } = string.Empty;

        // Термін дії "12/26"
        public string ExpiryDate { get; set; } = string.Empty;

        // Токен платежу
        public string PaymentToken { get; set; } = Guid.NewGuid().ToString();
        [JsonIgnore]
        public User? User { get; set; }
    }
}