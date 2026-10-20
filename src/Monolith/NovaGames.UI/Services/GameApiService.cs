using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using NovaGames.UI.Models.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace NovaGames.UI.Services
{
    public class GameApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7232/api/v1/games";
        private const string UploadUrl = "https://localhost:7232/api/v1/upload/image";

        public GameApiService()
        {
            _httpClient = new HttpClient();
        }

        // БАЗОВИЙ МЕТОД ДЛЯ ВСІХ АВТОРИЗОВАНИХ ЗАПИТІВ
        private async Task<HttpResponseMessage> ExecuteWithAuthAsync(Func<HttpRequestMessage> requestFactory, bool requireAuth = true)
        {
            var session = App.AppHost!.Services.GetRequiredService<UserSession>();
            var request = requestFactory();

            if (requireAuth && !string.IsNullOrEmpty(session.Token))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && requireAuth)
            {
                var authService = App.AppHost!.Services.GetRequiredService<AuthApiService>();
                if (await authService.RefreshTokensAsync())
                {
                    var retryRequest = requestFactory();
                    retryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);
                    response = await _httpClient.SendAsync(retryRequest);
                }
                else
                {
                    throw new UnauthorizedAccessException("Ваша сесія закінчилася. Будь ласка, увійдіть знову.");
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Ваші права доступу були змінені. Доступ заборонено.");
            }

            response.EnsureSuccessStatusCode();
            return response;
        }

        // ПУБЛІЧНІ МЕТОДИ
        public async Task<List<Game>> GetAllGamesAsync(bool forAdmin = false)
        {
            try
            {
                string url = forAdmin ? $"{BaseUrl}/admin/all" : BaseUrl;
                var response = await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Get, url), requireAuth: forAdmin);
                return await response.Content.ReadFromJsonAsync<List<Game>>() ?? new List<Game>();
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка підключення до API: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<Game?> CreateGameAsync(Game game)
        {
            var response = await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
                request.Content = JsonContent.Create(game);
                return request;
            });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Game>();
            }
            return null;
        }

        public async Task UpdateGameAsync(int id, Game game)
        {
            await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{id}");
                request.Content = JsonContent.Create(game);
                return request;
            });
        }

        public async Task DeleteGameAsync(int id)
        {
            await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{id}"));
        }

        public async Task<string> UploadFileAsync(string filePath, string gameTitle, string imageType)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, UploadUrl);
                    var form = new MultipartFormDataContent();

                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var fileContent = new StreamContent(fileStream);

                    var ext = Path.GetExtension(filePath).ToLower();
                    var mimeType = ext == ".png" ? "image/png" : ext == ".webp" ? "image/webp" : ext == ".ico" ? "image/x-icon" : "image/jpeg";
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

                    form.Add(fileContent, "file", Path.GetFileName(filePath));
                    form.Add(new StringContent(gameTitle), "gameTitle");
                    form.Add(new StringContent(imageType), "imageType");

                    request.Content = form;
                    return request;
                });

                var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
                return result?.Url ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження файлу: {ex.Message}");
                throw;
            }
        }

        public async Task<Game?> GetGameByIdAsync(int id)
        {
            try
            {
                // requireAuth: false, бо сторінку гри може дивитися будь-хто
                var response = await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{id}"), requireAuth: false);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Game>();
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження гри: {ex.Message}");
                return null;
            }
        }

        public async Task<string> UploadGameArchiveAsync(int gameId, string filePath)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{gameId}/upload-archive");
                    var form = new MultipartFormDataContent();

                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var fileContent = new StreamContent(fileStream);

                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
                    form.Add(fileContent, "file", Path.GetFileName(filePath));

                    request.Content = form;
                    return request;
                });

                var result = await response.Content.ReadFromJsonAsync<System.Collections.Generic.Dictionary<string, string>>();
                return result?["path"] ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження архіву: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> HasGameOwnersAsync(int gameId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/v1/games/{gameId}/has-owners");

                string rawJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
                    bool hasOwners = doc.RootElement.GetProperty("hasOwners").GetBoolean();

                    return hasOwners;
                }

                System.Diagnostics.Debug.WriteLine($"Помилка API: {response.StatusCode}. Відповідь: {rawJson}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in HasGameOwnersAsync: {ex.Message}");
                return true;
            }
        }

        private class UploadResponse
        {
            public string Url { get; set; } = string.Empty;
        }

        // МЕТОДИ ДЛЯ ВІДГУКІВ

        public async Task<List<ReviewResponseDto>> GetReviewsForGameAsync(int productId)
        {
            try
            {
                // requireAuth: false - читати можуть всі
                string url = $"https://localhost:7232/api/reviews/product/{productId}";
                var response = await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Get, url), requireAuth: false);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ReviewResponseDto>>() ?? new List<ReviewResponseDto>();
                }
                return new List<ReviewResponseDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження відгуків: {ex.Message}");
                return new List<ReviewResponseDto>();
            }
        }

        public async Task<bool> AddReviewAsync(CreateReviewDto dto)
        {
            try
            {
                // requireAuth: true - писати можуть тільки авторизовані юзери з дійсним JWT-токеном
                var response = await ExecuteWithAuthAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7232/api/reviews");
                    request.Content = JsonContent.Create(dto);
                    return request;
                }, requireAuth: true);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка додавання відгуку: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateReviewAsync(int reviewId, UpdateReviewDto dto)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, $"https://localhost:7232/api/reviews/{reviewId}");
                    request.Content = JsonContent.Create(dto);
                    return request;
                }, requireAuth: true);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка редагування відгуку: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Delete, $"https://localhost:7232/api/reviews/{reviewId}");
                }, requireAuth: true);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка видалення відгуку: {ex.Message}");
                return false;
            }
        }
    }
}