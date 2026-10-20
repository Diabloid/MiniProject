using NovaGames.Core.Entities;

namespace NovaGames.Core.Services
{
    // Цей клас живе в пам'яті WPF-клієнта і тримає дані про поточну сесію
    public class UserSession
    {
        public User? CurrentUser { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public event Action? CurrentUserUpdated;
        public void NotifyUserUpdated()
        {
            CurrentUserUpdated?.Invoke();
        }
        public bool IsLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(Token);
        public void Clear()
        {
            CurrentUser = null;
            Token = null;
            RefreshToken = null;
        }
    }
}