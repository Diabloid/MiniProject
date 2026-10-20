using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaGames.API.Data;
using NovaGames.API.Services;
using NovaGames.Core.Entities;
using System.Security.Claims;

namespace NovaGames.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IAuditLogService _auditLog;

        public GamesController(AppDbContext context, IWebHostEnvironment env, IAuditLogService auditLog)
        {
            _context = context;
            _env = env;
            _auditLog = auditLog;
        }

        // GET (ЧИТАННЯ)
        // GET: api/games
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Game>>> GetGames()
        {
            var publishedGames = await _context.Products
                                               .Where(g => g.IsPublished == true)
                                               .ToListAsync();
            return Ok(publishedGames);
        }

        // GET: api/games/admin/all
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<Game>>> GetAllGamesForAdmin()
        {
            var allGames = await _context.Products.ToListAsync();
            return Ok(allGames);
        }

        // GET: api/games/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Game>> GetGame(int id)
        {
            var game = await _context.Products.FindAsync(id);
            if (game == null) return NotFound();

            return game;
        }

        // POST (СТВОРЕННЯ)
        // POST: api/games
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Game>> PostGame([FromBody] Game game)
        {
            _context.Products.Add(game);
            await _context.SaveChangesAsync();

            int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLog.LogActionAsync(adminId, "GAME_CREATE", $"Створено гру: {game.Title} (ID:{game.Id})");

            return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
        }

        // PUT (ОНОВЛЕННЯ)
        // PUT: api/games/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> PutGame(int id, [FromBody] Game updatedGame)
        {
            if (id != updatedGame.Id)
                return BadRequest("ID в URL не співпадає з ID в об'єкті.");

            var existingGame = await _context.Products.FindAsync(id);
            if (existingGame == null)
                return NotFound("Гру не знайдено.");

            CleanupOrphanedGameImages(existingGame.Media, updatedGame.Media);

            if (existingGame.GetType() == updatedGame.GetType())
            {
                _context.Entry(existingGame).CurrentValues.SetValues(updatedGame);

                existingGame.Genres = updatedGame.Genres;
                existingGame.Media = updatedGame.Media;

                if (existingGame is DigitalGame existingDigital && updatedGame is DigitalGame updatedDigital)
                {
                    existingDigital.Requirements = updatedDigital.Requirements;
                }
            }
            else
            {
                _context.Products.Remove(existingGame);
                _context.Products.Add(updatedGame);
            }

            try
            {
                await _context.SaveChangesAsync();

                int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _auditLog.LogActionAsync(adminId, "GAME_UPDATE", $"Оновлено гру ID:{id} ({updatedGame.Title})");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GameExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE (ВИДАЛЕННЯ)
        // DELETE: api/games/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _context.Products.FindAsync(id);
            if (game == null) return NotFound("Гру не знайдено.");

            string gameTitle = game.Title;
            DeletePhysicalImages(game.Media);

            _context.Products.Remove(game);
            await _context.SaveChangesAsync();

            int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLog.LogActionAsync(adminId, "GAME_DELETE", $"Видалено гру: {gameTitle} (ID:{id})");

            return NoContent();
        }

        // GET: api/v1/games/5/has-owners
        [HttpGet("{id}/has-owners")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> HasOwners(int id)
        {
            if (!GameExists(id))
                return NotFound(new { message = "Гру не знайдено." });

            bool hasOwners = await _context.UserLibraries.AnyAsync(l => l.ProductId == id);

            return Ok(new { hasOwners = hasOwners });
        }

        // Цей метод видаляє фізичні файли зображень, пов'язаних з грою, коли гра видаляється
        private void DeletePhysicalImages(GameMedia media)
        {
            if (media == null) return;

            string wwwRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var urlsToDelete = new List<string>();

            // 1. Додаємо обкладинку (якщо це не дефолтна картинка)
            if (!string.IsNullOrEmpty(media.CoverUrl) && media.CoverUrl != "/Assets/default_game.png")
            {
                urlsToDelete.Add(media.CoverUrl);
            }

            // 2. Додаємо всі скріншоти
            if (media.Screenshots != null && media.Screenshots.Any())
            {
                urlsToDelete.AddRange(media.Screenshots);
            }

            // 3. Додаємо іконку
            if (!string.IsNullOrEmpty(media.IconUrl) && media.IconUrl != "/Assets/default_icon.png")
            {
                urlsToDelete.Add(media.IconUrl);
            }

            // 3. Знищуємо файли
            foreach (var url in urlsToDelete)
            {
                try
                {
                    var relativePath = url.TrimStart('/');

                    relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

                    var fullPath = Path.Combine(wwwRootPath, relativePath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Помилка видалення файлу {url}: {ex.Message}");
                }
            }
        }

        // Цей метод викликається при оновленні гри, щоб видалити старі зображення, які більше не використовуються
        private void CleanupOrphanedGameImages(GameMedia oldMedia, GameMedia newMedia)
        {
            if (oldMedia == null || newMedia == null) return;

            string wwwRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filesToDelete = new List<string>();

            // 1. Перевіряємо обкладинку: якщо вона змінилася і це не дефолтна картинка
            if (!string.IsNullOrEmpty(oldMedia.CoverUrl) &&
                oldMedia.CoverUrl != "/Assets/default_game.png" &&
                oldMedia.CoverUrl != newMedia.CoverUrl)
            {
                filesToDelete.Add(oldMedia.CoverUrl);
            }

            // 2. Перевіряємо іконку: якщо вона змінилася і це не дефолтна картинка
            if (!string.IsNullOrEmpty(oldMedia.IconUrl) &&
                oldMedia.IconUrl != "/Assets/default_icon.png" &&
                oldMedia.IconUrl != newMedia.IconUrl)
            {
                filesToDelete.Add(oldMedia.IconUrl);
            }

            // 3. Перевіряємо скріншоти
            var oldScreenshots = oldMedia.Screenshots ?? new List<string>();
            var newScreenshots = newMedia.Screenshots ?? new List<string>();

            var orphanedScreenshots = oldScreenshots.Except(newScreenshots);
            filesToDelete.AddRange(orphanedScreenshots);

            // 4. Знищуємо знайдені файли-привиди
            foreach (var url in filesToDelete)
            {
                try
                {
                    var relativePath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(wwwRootPath, relativePath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch {}
            }
        }

        // POST: api/v1/games/5/upload-archive
        [HttpPost("{id}/upload-archive")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> UploadGameArchive(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не вибрано або він порожній.");

            var game = await _context.Products.OfType<DigitalGame>().FirstOrDefaultAsync(g => g.Id == id);
            if (game == null)
                return NotFound("Цифрову гру не знайдено.");

            // 1. Створюємо папку "games" всередині wwwroot на сервері
            string wwwRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string serverGamesFolder = Path.Combine(wwwRootPath, "games");
            if (!Directory.Exists(serverGamesFolder))
            {
                Directory.CreateDirectory(serverGamesFolder);
            }

            // 2. Формуємо ім'я файлу
            string safeTitle = string.Join("_", game.Title.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            string fileName = $"{game.Id}_{safeTitle}.zip";
            string fullPhysicalPath = Path.Combine(serverGamesFolder, fileName);
            string relativeDbPath = $"/games/{fileName}"; // Це піде в базу

            // 3. Знищуємо старий архів, ТІЛЬКИ якщо це внутрішній шлях сервера
            if (!string.IsNullOrEmpty(game.GameZipUrl) && game.GameZipUrl.StartsWith("/games/"))
            {
                string oldPhysicalPath = Path.Combine(wwwRootPath, game.GameZipUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPhysicalPath))
                {
                    System.IO.File.Delete(oldPhysicalPath);
                }
            }

            // 4. Зберігаємо новий файл на диску сервера
            using (var stream = new FileStream(fullPhysicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Оновлюємо дані гри
            game.GameZipUrl = relativeDbPath;

            // Автоматично вираховуємо розмір у ГБ (якщо не було задано)
            if (game.DownloadSize <= 0)
            {
                game.DownloadSize = Math.Round(file.Length / 1024.0 / 1024.0 / 1024.0, 3);
            }

            await _context.SaveChangesAsync();

            int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLog.LogActionAsync(adminId, "GAME_ARCHIVE_UPLOADED", $"Завантажено архів для гри ID:{id} ({game.DownloadSize} GB)");

            return Ok(new { path = relativeDbPath });
        }

        // Цей метод перевіряє, чи існує гра з вказаним ID
        private bool GameExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}