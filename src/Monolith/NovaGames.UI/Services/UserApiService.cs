using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace NovaGames.UI.Services
{
    public class UserApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7232/api/v1/users";
        private const string WalletUrl = "https://localhost:7232/api/wallet";

        public UserApiService()
        {
            _httpClient = new HttpClient();
        }

        // БАЗОВИЙ МЕТОД ДЛЯ ВСІХ АВТОРИЗОВАНИХ ЗАПИТІВ
        private async Task<HttpResponseMessage> ExecuteWithAuthAsync(Func<HttpRequestMessage> requestFactory)
        {
            var session = App.AppHost!.Services.GetRequiredService<UserSession>();
            var request = requestFactory();

            if (!string.IsNullOrEmpty(session.Token))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await _httpClient.SendAsync(request);

            // ПЕРЕХОПЛЕННЯ 401 (ТОКЕН ПРОТУХ)
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
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

            // ПЕРЕХОПЛЕННЯ 403 (ПРАВА ЗАБРАЛИ)
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Ваші права доступу були змінені. Доступ заборонено.");
            }

            response.EnsureSuccessStatusCode();
            return response;
        }

        // ПУБЛІЧНІ МЕТОДИ
        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Get, BaseUrl));
                return await response.Content.ReadFromJsonAsync<List<User>>() ?? new List<User>();
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка підключення до API користувачів: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task UpdateUserRoleAsync(int userId, UserRole newRole)
        {
            await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{userId}/role");
                request.Content = JsonContent.Create(newRole);
                return request;
            });
        }

        public async Task BlockUserAsync(int userId)
        {
            await ExecuteWithAuthAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{userId}/block"));
        }

        // 1. Перевірка чи існує така пошта в базі
        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            try
            {
                // Викликаємо GET /api/v1/users/check-email?email=test@test.com
                var response = await ExecuteWithAuthAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/check-email?email={Uri.EscapeDataString(email)}"));

                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка перевірки пошти: {ex.Message}");
                return false;
            }
        }

        // 2. Оновлення пошти користувача
        public async Task UpdateEmailAsync(int userId, string newEmail)
        {
            await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{userId}/email");
                request.Content = JsonContent.Create(new { Email = newEmail });
                return request;
            });
        }

        // Відправка коду
        public async Task SendVerificationCodeAsync(int userId, string email)
        {
            await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{userId}/send-email-code");
                request.Content = JsonContent.Create(new { Email = email });
                return request;
            });
        }

        // Фінальна перевірка і збереження
        public async Task UpdateEmailAsync(int userId, string newEmail, string code)
        {
            await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{userId}/email");
                request.Content = JsonContent.Create(new { Email = newEmail, Code = code });
                return request;
            });
        }

        // Запит на відправку коду для зміни пароля
        public async Task SendPasswordResetCodeAsync(int userId)
        {
            await ExecuteWithAuthAsync(() =>
                new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{userId}/send-password-code"));
        }

        // Фінальна зміна пароля з перевіркою коду
        public async Task UpdatePasswordAsync(int userId, string newPassword, string code)
        {
            await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{userId}/password");
                request.Content = JsonContent.Create(new { NewPassword = newPassword, Code = code });
                return request;
            });
        }

        // Оновлення кольору рамки
        public async Task UpdateAvatarColorAsync(int userId, string hexColor)
        {
            await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{userId}/avatar-color");
                request.Content = JsonContent.Create(new { AvatarBorderColorHex = hexColor });
                return request;
            });
        }

        // Завантаження файлу аватарки
        public async Task<string> UploadAvatarFileAsync(int userId, string filePath)
        {
            var response = await ExecuteWithAuthAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{userId}/avatar-file");

                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));

                // Вказуємо тип (image/png або image/jpeg)
                string ext = Path.GetExtension(filePath).TrimStart('.');
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue($"image/{ext}");

                content.Add(fileContent, "file", Path.GetFileName(filePath));
                request.Content = content;

                return request;
            });

            // Отримуємо нове посилання від сервера
            var result = await response.Content.ReadFromJsonAsync<AvatarUploadResponse>();
            return result?.AvatarUrl ?? string.Empty;
        }

        // Отримання списку збережених карток
        public async Task<List<SavedCardDto>> GetSavedCardsAsync()
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, $"{WalletUrl}/saved-cards"));

                return await response.Content.ReadFromJsonAsync<List<SavedCardDto>>() ?? new List<SavedCardDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження карток: {ex.Message}");
                return new List<SavedCardDto>();
            }
        }

        // Поповнення балансу
        public async Task<decimal> TopUpWalletAsync(TopUpRequest request)
        {
            var response = await ExecuteWithAuthAsync(() =>
            {
                var reqMessage = new HttpRequestMessage(HttpMethod.Post, $"{WalletUrl}/top-up");
                reqMessage.Content = JsonContent.Create(request);
                return reqMessage;
            });

            var result = await response.Content.ReadFromJsonAsync<TopUpResponse>();
            return result?.NewBalance ?? 0;
        }

        // Отримання ігрової бібліотеки користувача
        public async Task<List<LibraryGameDto>> GetLibraryGamesAsync()
        {
            try
            {
                var response = await ExecuteWithAuthAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, "https://localhost:7232/api/v1/library/my-games"));

                return await response.Content.ReadFromJsonAsync<List<LibraryGameDto>>() ?? new List<LibraryGameDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження бібліотеки: {ex.Message}");
                return new List<LibraryGameDto>();
            }
        }

        // Зміна категорії гри в бібліотеці
        public async Task<bool> ChangeGameCategoryAsync(int productId, string newCategory)
        {
            try
            {
                var requestBody = new { ProductId = productId, NewCategory = newCategory };
                var response = await ExecuteWithAuthAsync(() =>
                    new HttpRequestMessage(HttpMethod.Put, "https://localhost:7232/api/v1/library/change-category")
                    {
                        Content = JsonContent.Create(requestBody)
                    });

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка зміни категорії: {ex.Message}");
                return false;
            }
        }

        // Перейменування категорії в бібліотеці
        public async Task<bool> RenameCategoryAsync(string oldCategory, string newCategory)
        {
            try
            {
                var requestBody = new { OldCategory = oldCategory, NewCategory = newCategory };
                var response = await ExecuteWithAuthAsync(() =>
                    new HttpRequestMessage(HttpMethod.Put, "https://localhost:7232/api/v1/library/rename-category")
                    {
                        Content = JsonContent.Create(requestBody)
                    });

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка перейменування категорії: {ex.Message}");
                return false;
            }
        }
    }

    // DTO для відповіді після завантаження аватарки
    public class AvatarUploadResponse
    {
        public string AvatarUrl { get; set; } = string.Empty;
    }

    // DTO для відправки запиту на поповнення
    public class TopUpRequest
    {
        public decimal Amount { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public bool SaveCard { get; set; }
    }

    // DTO для отримання нового балансу
    public class TopUpResponse
    {
        public decimal NewBalance { get; set; }
    }

    // DTO для збережених карток
    public class SavedCardDto
    {
        public int Id { get; set; }
        public string MaskedCardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string PaymentToken { get; set; } = string.Empty;
    }

    // Модель для відображення гри в Бібліотеці (UI)
    public class LibraryGameDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Category { get; set; } = "Без категорії";
        public bool IsInstalled { get; set; } = false;
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime AcquiredAt { get; set; } // Дата покупки
        public double SizeGB { get; set; }       // Розмір для сортування
        public DateTime? LastPlayedAt { get; set; } // Дата останньої гри

        public string HeroImageUrl { get; set; } = string.Empty; // Велика обкладинка-банер
        public List<string> ScreenshotPaths { get; set; } = new List<string>();
        public long PlaytimeSeconds { get; set; } = 0; // Награний час

        // Робить з секунд красивий текст "12 год. 30 хв."
        public string DisplayPlaytime
        {
            get
            {
                if (PlaytimeSeconds == 0) return "Ще не запускалась";
                TimeSpan time = TimeSpan.FromSeconds(PlaytimeSeconds);
                if (time.TotalHours < 1) return $"{time.Minutes} хв.";
                return $"{(int)time.TotalHours} год. {time.Minutes} хв.";
            }
        }

        // Красивий формат дати (напр. "21 травня 2026")
        public string LastPlayedDate => LastPlayedAt.HasValue
            ? LastPlayedAt.Value.ToString("dd MMMM yyyy")
            : "Ніколи";
    }

}