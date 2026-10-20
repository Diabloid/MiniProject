using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using System.Net.Http;
using System.Net.Http.Json;

namespace NovaGames.UI.Services
{
    // Допоміжний клас для пакування даних логіну
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Допоміжний клас для пакування даних реєстрації
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Допоміжний клас для пакування даних оновлення токенів
    public class RefreshRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    // Допоміжний клас для пакування відповіді з токеном
    public class AuthResponse
    {
        public User User { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7232/api/v1/auth";

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
                    return await response.Content.ReadFromJsonAsync<AuthResponse>();
                }

                return null;
            }
            catch (HttpRequestException)
            {
                throw new System.Exception("Не вдалося підключитися до сервера. Перевірте підключення.");
            }
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            try
            {
                var request = new RegisterRequest { Username = username, Password = password };

                // Робимо POST запит на реєстрацію
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/register", request);

                // Якщо статус 200-299, значить успіх
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                throw new System.Exception("Не вдалося підключитися до сервера. Перевірте підключення.");
            }
        }

        public async Task<bool> RefreshTokensAsync()
        {
            var session = App.AppHost!.Services.GetRequiredService<UserSession>();
            if (string.IsNullOrEmpty(session.Token) || string.IsNullOrEmpty(session.RefreshToken))
                return false;

            var request = new RefreshRequest
            {
                Token = session.Token,
                RefreshToken = session.RefreshToken
            };

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/refresh", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    session.Token = result.Token;
                    session.RefreshToken = result.RefreshToken;
                    session.CurrentUser = result.User;

                    var tokenStorage = App.AppHost!.Services.GetRequiredService<TokenStorageService>();
                    tokenStorage.SaveTokens(result.Token, result.RefreshToken);

                    return true;
                }
            }
            return false;
        }
    }
}