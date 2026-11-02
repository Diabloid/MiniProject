using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NovaGames.UI.Services
{
    // Допоміжний клас для пакування даних логіну
    public class LoginRequest
    {
        [JsonPropertyName("email")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    // Допоміжний клас для пакування даних реєстрації
    public class RegisterRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    // Допоміжний клас для пакування даних оновлення токенів
    public class RefreshRequest
    {
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    // DTO відповіді від AuthService
    public class RemoteAuthResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public RemoteUserDto User { get; set; } = null!;
    }

    public class RemoteUserDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    // Клас для сумісності з WPF вікнами
    public class AuthResponse
    {
        public User User { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:5000/api/v1/auth";

        public AuthApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<AuthResponse?> AuthenticateAsync(string username, string password)
        {
            try
            {
                var request = new LoginRequest { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var remote = await response.Content.ReadFromJsonAsync<RemoteAuthResponse>();
                    if (remote != null)
                    {
                        var user = new User
                        {
                            Id = remote.User.Id,
                            Username = !string.IsNullOrWhiteSpace(remote.User.DisplayName) ? remote.User.DisplayName : remote.User.Email,
                            Email = remote.User.Email,
                            Role = remote.User.Roles.Contains("Admin") ? UserRole.Admin : UserRole.Client
                        };

                        return new AuthResponse
                        {
                            Token = remote.AccessToken,
                            RefreshToken = remote.RefreshToken,
                            User = user
                        };
                    }
                }

                return null;
            }
            catch (HttpRequestException)
            {
                throw new System.Exception("Не вдалося підключитися до API Gateway (порт 5000). Перевірте підключення.");
            }
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            try
            {
                var email = username.Contains("@") ? username : $"{username}@novagames.com";
                var request = new RegisterRequest { Email = email, Username = username, Password = password };

                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/register", request);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                throw new System.Exception("Не вдалося підключитися до API Gateway (порт 5000). Перевірте підключення.");
            }
        }

        public async Task<bool> RefreshTokensAsync()
        {
            var session = App.AppHost!.Services.GetRequiredService<UserSession>();
            if (string.IsNullOrEmpty(session.RefreshToken))
                return false;

            var request = new RefreshRequest
            {
                RefreshToken = session.RefreshToken
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/refresh-token", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RemoteAuthResponse>();
                    if (result != null)
                    {
                        session.Token = result.AccessToken;
                        session.RefreshToken = result.RefreshToken;

                        var tokenStorage = App.AppHost!.Services.GetRequiredService<TokenStorageService>();
                        tokenStorage.SaveTokens(result.AccessToken, result.RefreshToken);

                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
