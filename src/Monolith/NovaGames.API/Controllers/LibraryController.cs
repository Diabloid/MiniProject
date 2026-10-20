using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaGames.API.Data;
using NovaGames.API.DTOs;
using NovaGames.API.Services;
using NovaGames.Core.Entities;
using NovaGames.Core.Models;
using System.Security.Claims;

namespace NovaGames.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class LibraryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLog;

        public LibraryController(AppDbContext context, IAuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        [HttpGet("my-games")]
        public async Task<ActionResult<IEnumerable<LibraryGameDto>>> GetMyLibrary()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            // 1. Беремо всі записи юзера з таблиці-зв'язки
            var libraryItems = await _context.UserLibraries
                .Where(ul => ul.UserId == userId)
                .ToListAsync();

            if (!libraryItems.Any()) return Ok(new List<LibraryGameDto>());

            // 2. БЕРЕМО ІГРИ (ЯК У ТВОЄМУ GamesController: без жодних Include)
            var gameIds = libraryItems.Select(l => l.ProductId).ToList();
            var games = await _context.Products
                .Where(g => gameIds.Contains(g.Id))
                .ToListAsync();

            // Базова URL сервера для WPF (щоб завантажити картинки з відносних шляхів)
            string baseUrl = "https://localhost:7232";

            // 3. Компонуємо результат
            var result = new List<LibraryGameDto>();
            foreach (var item in libraryItems)
            {
                var game = games.FirstOrDefault(g => g.Id == item.ProductId);
                if (game != null)
                {
                    double gameSize = 0;
                    string downloadUrl = string.Empty;

                    if (game is DigitalGame digitalGame)
                    {
                        gameSize = digitalGame.DownloadSize;
                        if (!string.IsNullOrEmpty(digitalGame.GameZipUrl))
                        {
                            downloadUrl = baseUrl + digitalGame.GameZipUrl;
                        }
                    }

                    // Формуємо ПОВНІ посилання для WPF. 
                    // Звертаємося до game.Media (як ти робиш це в CleanupOrphanedGameImages)
                    string icon = game.Media?.IconUrl ?? string.Empty;
                    string fullIconUrl = !string.IsNullOrEmpty(icon) ? baseUrl + icon : string.Empty;

                    string cover = game.Media?.CoverUrl ?? string.Empty;
                    string fullCoverUrl = !string.IsNullOrEmpty(cover) ? baseUrl + cover : string.Empty;

                    var fullScreenshots = new List<string>();
                    if (game.Media?.Screenshots != null)
                    {
                        foreach (var screen in game.Media.Screenshots)
                        {
                            fullScreenshots.Add(baseUrl + screen);
                        }
                    }

                    result.Add(new LibraryGameDto
                    {
                        Id = game.Id,
                        Title = game.Title,
                        Category = item.Category,
                        IsInstalled = false,
                        AverageRating = game.AverageRating,
                        TotalReviews = game.TotalReviews,
                        DownloadUrl = downloadUrl,
                        AcquiredAt = item.AcquiredAt,
                        LastPlayedAt = item.LastPlayedAt,
                        SizeGB = gameSize,
                        PlaytimeSeconds = item.PlaytimeSeconds,

                        // Віддаємо WPF вже готові посилання
                        IconUrl = fullIconUrl,
                        HeroImageUrl = fullCoverUrl,
                        ScreenshotPaths = fullScreenshots
                    });
                }
            }

            return Ok(result);
        }

        // Додаткові ендпоінти для керування категоріями
        [HttpPut("change-category")]
        public async Task<IActionResult> ChangeGameCategory([FromBody] ChangeGameCategoryRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            var libraryItem = await _context.UserLibraries
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.ProductId == request.ProductId);

            if (libraryItem == null) return NotFound("Гру не знайдено в бібліотеці.");

            string oldCat = libraryItem.Category;
            libraryItem.Category = string.IsNullOrWhiteSpace(request.NewCategory) ? "Без категорії" : request.NewCategory.Trim();

            await _context.SaveChangesAsync();
            await _auditLog.LogActionAsync(userId, "LIBRARY_CAT_CHANGE",
                $"Гра ID:{request.ProductId} переміщена з '{oldCat}' до '{libraryItem.Category}'");
            return Ok();
        }

        // Масове перейменування категорії
        [HttpPut("rename-category")]
        public async Task<IActionResult> RenameCategory([FromBody] RenameCategoryRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            // Знаходимо всі ігри користувача в старій категорії
            var libraryItems = await _context.UserLibraries
                .Where(ul => ul.UserId == userId && ul.Category == request.OldCategory)
                .ToListAsync();

            if (!libraryItems.Any()) return NotFound("Категорію не знайдено або вона порожня.");

            string updatedCategory = string.IsNullOrWhiteSpace(request.NewCategory) ? "Без категорії" : request.NewCategory.Trim();

            // Масово оновлюємо
            foreach (var item in libraryItems)
            {
                item.Category = updatedCategory;
            }

            await _context.SaveChangesAsync();
            await _auditLog.LogActionAsync(userId, "LIBRARY_CAT_RENAME",
                $"Категорію '{request.OldCategory}' перейменовано на '{updatedCategory}' для {libraryItems.Count} ігор.");
            return Ok();
        }
    }
}