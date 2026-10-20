using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaGames.API.DTOs;
using NovaGames.API.Services;
using System.Security.Claims;

namespace NovaGames.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IAuditLogService _auditLog;  

        public UploadController(IWebHostEnvironment env, IAuditLogService auditLog)
        {
            _env = env;
            _auditLog = auditLog;
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("Файл не знайдено.");

            try
            {
                // Очищаємо назву гри від пробілів та спецсимволів
                string safeTitle = new string(dto.GameTitle.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLower();
                string extension = Path.GetExtension(dto.File.FileName);

                // 1. Визначаємо папку для збереження в залежності від типу зображення
                string subFolder = dto.ImageType.ToLower() switch
                {
                    "cover" => "covers",
                    "icon" => "icons",
                    _ => "screenshots"
                };

                string newFileName = dto.ImageType.ToLower() switch
                {
                    "cover" => $"{safeTitle}_cover{extension}",
                    "icon" => $"{safeTitle}_icon{extension}",
                    _ => $"{safeTitle}_screenshot_{Guid.NewGuid().ToString().Substring(0, 4)}{extension}"
                };

                // 2. Формуємо повний шлях до папки
                string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "games", subFolder);

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string filePath = Path.Combine(uploadsFolder, newFileName);

                // Зберігаємо файл на жорсткий диск сервера
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _auditLog.LogActionAsync(adminId, "UPLOAD_IMAGE",
                    $"Завантажено зображення для гри '{dto.GameTitle}'. Тип: {dto.ImageType}, Файл: {newFileName}");

                // 3. Повертаємо URL для доступу до зображення
                return Ok(new { url = $"/images/games/{subFolder}/{newFileName}" });
                }
            catch (Exception ex)
            {
                int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _auditLog.LogActionAsync(adminId, "UPLOAD_FAILED", $"Помилка завантаження файлу для '{dto.GameTitle}': {ex.Message}");

                return StatusCode(500, "Внутрішня помилка сервера при збереженні файлу.");
            }
        }
    }
}