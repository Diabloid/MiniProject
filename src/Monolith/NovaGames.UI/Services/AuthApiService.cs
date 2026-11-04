using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using NovaGames.UI.Services.Api;

namespace NovaGames.UI.Services
{
    public class LoginRequest
    {
        [JsonPropertyName("email")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

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

                if ((int)response.StatusCode == 429)
                {
                    int retrySeconds = 60;
                    if (response.Headers.RetryAfter?.Delta.HasValue == true)
                    {
                        retrySeconds = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
                    }
                    else if (response.Headers.TryGetValues("Retry-After", out var vals) && int.TryParse(vals.FirstOrDefault(), out var parsed))
                    {
                        retrySeconds = parsed;
                    }
                    throw new RateLimitExceededException(retrySeconds);
                }

                return null;
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                throw new Exception("Не вдалося з'єднатися з API Gateway (порт 5000). Перевірте з'єднання.");
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
                throw new Exception("Не вдалося з'єднатися з API Gateway (порт 5000). Перевірте з'єднання.");
            }
        }

        public async Task<bool> RefreshTokensAsync()
        {
            try
            {
                var tokenStorage = new TokenStorageService();
                var (_, refreshToken) = tokenStorage.GetTokens();

                if (string.IsNullOrEmpty(refreshToken)) return false;

                var request = new RefreshRequest { RefreshToken = refreshToken };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/refresh", request);

                if (response.IsSuccessStatusCode)
                {
                    var remote = await response.Content.ReadFromJsonAsync<RemoteAuthResponse>();
                    if (remote != null)
                    {
                        tokenStorage.SaveTokens(remote.AccessToken, remote.RefreshToken);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}