using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using NovaGames.UI.Models.DTOs;

namespace NovaGames.UI.Services
{
    public class GameApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:5000/api/v1/games";
        private const string UploadUrl = "http://localhost:5000/api/v1/upload/image";

        public GameApiService()
        {
            _httpClient = new HttpClient();
        }

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
                    throw new UnauthorizedAccessException("Сесія закінчилась. Будь ласка, авторизуйтесь знову.");
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Ваші права доступу були змінені. Дія заборонена.");
            }

            response.EnsureSuccessStatusCode();
            return response;
        }

        public async Task<List<Game>> GetAllGamesAsync(bool forAdmin = false)
        {
            try
            {
                string url = forAdmin ? $"{BaseUrl}/admin/all" : $"{BaseUrl}?page=1&pageSize=100";
                var response = await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Get, url), requireAuth: forAdmin);
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement itemsElement = root;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var pagedItems))
                {
                    itemsElement = pagedItems;
                }

                var games = new List<Game>();
                if (itemsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in itemsElement.EnumerateArray())
                    {
                        var id = el.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
                        var title = el.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? string.Empty : string.Empty;
                        var desc = el.TryGetProperty("description", out var dProp) ? dProp.GetString() ?? string.Empty : string.Empty;
                        var price = el.TryGetProperty("price", out var pProp) ? pProp.GetDecimal() : 0m;
                        var discount = el.TryGetProperty("discount", out var dcProp) ? dcProp.GetDecimal() : 0m;
                        var cover = el.TryGetProperty("coverUrl", out var cProp) ? cProp.GetString() : null;
                        var icon = el.TryGetProperty("iconUrl", out var iProp) ? iProp.GetString() : null;

                        var genres = new List<string>();
                        if (el.TryGetProperty("genres", out var gProp) && gProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var g in gProp.EnumerateArray())
                            {
                                var val = g.GetString();
                                if (!string.IsNullOrEmpty(val)) genres.Add(val);
                            }
                        }

                        var game = new DigitalGame
                        {
                            Id = id,
                            Title = title,
                            Description = desc,
                            Price = price,
                            Discount = discount,
                            Genres = genres,
                            Media = new GameMedia
                            {
                                CoverUrl = !string.IsNullOrEmpty(cover) ? cover : "/Assets/default_game.png",
                                IconUrl = !string.IsNullOrEmpty(icon) ? icon : "/Assets/default_icon.png"
                            }
                        };
                        games.Add(game);
                    }
                }
                return games;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження ігор з каталогу: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<Game?> GetGameByIdAsync(int id)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{id}"), requireAuth: false);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement target = root;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("game", out var nestedGame))
                {
                    target = nestedGame;
                }

                var gameId = target.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : id;
                var title = target.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? string.Empty : string.Empty;
                var desc = target.TryGetProperty("description", out var dProp) ? dProp.GetString() ?? string.Empty : string.Empty;
                var price = target.TryGetProperty("price", out var pProp) ? pProp.GetDecimal() : 0m;
                var discount = target.TryGetProperty("discount", out var dcProp) ? dcProp.GetDecimal() : 0m;
                var downloadSize = target.TryGetProperty("downloadSize", out var dsProp) ? dsProp.GetDouble() : 50.0;
                var cover = target.TryGetProperty("coverUrl", out var cProp) ? cProp.GetString() : null;
                var icon = target.TryGetProperty("iconUrl", out var iProp) ? iProp.GetString() : null;

                var genres = new List<string>();
                if (target.TryGetProperty("genres", out var gProp) && gProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var g in gProp.EnumerateArray())
                    {
                        var val = g.GetString();
                        if (!string.IsNullOrEmpty(val)) genres.Add(val);
                    }
                }

                return new DigitalGame
                {
                    Id = gameId,
                    Title = title,
                    Description = desc,
                    Price = price,
                    Discount = discount,
                    DownloadSize = downloadSize,
                    Genres = genres,
                    Media = new GameMedia
                    {
                        CoverUrl = !string.IsNullOrEmpty(cover) ? cover : "/Assets/default_game.png",
                        IconUrl = !string.IsNullOrEmpty(icon) ? icon : "/Assets/default_icon.png"
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження гри #{id}: {ex.Message}");
                return null;
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

        public async Task<string> UploadGameArchiveAsync(int gameId, string filePath)
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

            var response = await ExecuteWithAuthAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{gameId}/upload-archive");
                req.Content = content;
                return req;
            });

            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadFromJsonAsync<UploadArchiveResponse>();
                return res?.DownloadUrl ?? string.Empty;
            }
            return string.Empty;
        }

        public async Task<bool> HasGameOwnersAsync(int gameId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:5000/api/v1/games/{gameId}/has-owners");
                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(rawJson);
                    if (doc.RootElement.TryGetProperty("hasOwners", out var prop))
                    {
                        return prop.GetBoolean();
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ReviewResponseDto>> GetReviewsForGameAsync(int productId)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Get, $"http://localhost:5000/api/reviews/product/{productId}"), requireAuth: false);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ReviewResponseDto>>() ?? new List<ReviewResponseDto>();
                }
                return new List<ReviewResponseDto>();
            }
            catch
            {
                return new List<ReviewResponseDto>();
            }
        }

        public async Task<bool> AddReviewAsync(CreateReviewDto dto)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/api/reviews");
                    request.Content = JsonContent.Create(dto);
                    return request;
                }, requireAuth: true);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateReviewAsync(int reviewId, UpdateReviewDto dto)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, $"http://localhost:5000/api/reviews/{reviewId}");
                    request.Content = JsonContent.Create(dto);
                    return request;
                }, requireAuth: true);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId)
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"http://localhost:5000/api/reviews/{reviewId}"), requireAuth: true);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UploadResponse
    {
        public string Url { get; set; } = string.Empty;
    }

    public class UploadArchiveResponse
    {
        public string DownloadUrl { get; set; } = string.Empty;
    }
}