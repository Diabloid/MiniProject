using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NovaGames.API.Data;
using NovaGames.API.DTOs;
using NovaGames.API.Services;
using NovaGames.Core.Entities;
using System.Security.Claims;

namespace NovaGames.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly IAuditLogService _auditLog;

        public UsersController(AppDbContext context, IMemoryCache cache, IEmailService emailService, IWebHostEnvironment env, IAuditLogService auditLog)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
            _env = env;
            _auditLog = auditLog;
        }

        // АДМІНСЬКІ МЕТОДИ (Тільки для Admin та SuperAdmin)

        // GET: api/v1/users
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // PUT: api/v1/users/5/role
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserRole newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Користувача не знайдено");

            if (user.Role == UserRole.SuperAdmin)
                return BadRequest("Неможливо змінити права SuperAdmin'а!");

            user.Role = newRole;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/v1/users/5/block
        [HttpPost("{id}/block")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Користувача не знайдено");

            if (user.Role == UserRole.SuperAdmin)
                return BadRequest("Неможливо заблокувати SuperAdmin'а!");

            // user.IsBlocked = true; 

            await _context.SaveChangesAsync();

            int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLog.LogActionAsync(adminId, "USER_BLOCK", $"Адмін заблокував користувача ID:{id}");

            return NoContent();
        }

        // МЕТОДИ ПРОФІЛЮ (Доступні всім авторизованим користувачам)

        // GET: api/v1/users/check-email?email=test@test.com
        [HttpGet("check-email")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return BadRequest("Email порожній");

            bool exists = await _context.Users.AnyAsync(u => u.Email == email);
            return Ok(exists);
        }

        // 1. Відправка коду на пошту для підтвердження
        [HttpPost("{id}/send-email-code")]
        public async Task<IActionResult> SendEmailCode(int id, [FromBody] SendEmailCodeDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
                return BadRequest("Ця пошта вже зайнята іншим користувачем.");

            string code = new Random().Next(100000, 999999).ToString();
            _cache.Set($"EmailCode_{id}", code, TimeSpan.FromMinutes(10));

            // ВИКОРИСТОВУЄМО НОВИЙ ГЕНЕРАТОР ШАБЛОНІВ
            string message = _emailService.GenerateHtmlEmail(
                title: "ПІДТВЕРДЖЕННЯ ПОШТИ",
                message: $"Ваш код для прив'язки електронної пошти:<br><br><span style='font-size: 36px; font-weight: bold; color: #00ADEF; letter-spacing: 5px;'>{code}</span><br><br>Код дійсний протягом 10 хвилин. Нікому його не повідомляйте."
            );

            await _emailService.SendEmailAsync(dto.Email, "Підтвердження пошти - NovaGames", message);

            return Ok();
        }

        // PUT: api/v1/users/5/email
        // 2. Перевірка коду і оновлення пошти в БД
        [HttpPut("{id}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] UpdateEmailDto dto)
        {
            // Дістаємо код з кешу
            if (!_cache.TryGetValue($"EmailCode_{id}", out string? savedCode))
                return BadRequest("Код прострочений або не був відправлений. Спробуйте ще раз.");

            // Звіряємо коди
            if (savedCode != dto.Code)
                return BadRequest("Невірний код підтвердження.");

            // Якщо код вірний — оновлюємо БД
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Користувача не знайдено");

            string oldEmail = user.Email;
            user.Email = dto.Email;
            await _context.SaveChangesAsync();

            // Очищаємо кеш, щоб цей код більше не можна було використати
            _cache.Remove($"EmailCode_{id}");

            await _auditLog.LogActionAsync(id, "EMAIL_CHANGE", $"Пошту змінено з {oldEmail} на {dto.Email}");

            return NoContent();
        }

        // POST: api/v1/users/5/avatar-file
        [HttpPost("{id}/avatar-file")]
        public async Task<IActionResult> UploadAvatarFile(int id, IFormFile file)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Користувача не знайдено");

            if (file == null || file.Length == 0) return BadRequest("Файл порожній");

            string safeUsername = new string(user.Username.Where(char.IsLetterOrDigit).ToArray()).ToLower();
            string extension = Path.GetExtension(file.FileName);
            string newFileName = $"{safeUsername}_avatar{extension}";

            string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "avatars");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/images/avatars/"))
            {
                var oldRelativePath = user.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var oldFullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), oldRelativePath);
                if (System.IO.File.Exists(oldFullPath))
                {
                    System.IO.File.Delete(oldFullPath);
                }
            }

            string filePath = Path.Combine(uploadsFolder, newFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/images/avatars/{newFileName}";
            await _context.SaveChangesAsync();
            await _auditLog.LogActionAsync(id, "AVATAR_UPDATE", "Користувач оновив аватарку.");

            return Ok(new { AvatarUrl = user.AvatarUrl });
        }

        // ЗМІНА ПАРОЛЯ
        // 1. Відправка коду на пошту для підтвердження (Зміна пароля)
        [HttpPost("{id}/send-password-code")]
        public async Task<IActionResult> SendPasswordCode(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || string.IsNullOrEmpty(user.Email))
                return BadRequest("Користувач не знайдений або пошта не прив'язана.");

            string code = new Random().Next(100000, 999999).ToString();
            _cache.Set($"PassCode_{id}", code, TimeSpan.FromMinutes(10));

            // ВИКОРИСТОВУЄМО НОВИЙ ГЕНЕРАТОР ШАБЛОНІВ
            string message = _emailService.GenerateHtmlEmail(
                title: "ЗАПИТ НА ЗМІНУ ПАРОЛЯ",
                message: $"Ми отримали запит на зміну пароля для вашого акаунту <b>{user.Username}</b>.<br>Ваш код підтвердження:<br><br><span style='font-size: 36px; font-weight: bold; color: #00ADEF; letter-spacing: 5px;'>{code}</span><br><br>Код дійсний протягом 10 хвилин. Якщо це були не ви, негайно змініть пароль та перевірте безпеку акаунту."
            );

            await _emailService.SendEmailAsync(user.Email, "Зміна пароля - NovaGames", message);

            return Ok();
        }

        // 2. Перевірка коду і оновлення пароля
        [HttpPut("{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordDto dto)
        {
            // 1. Перевіряємо код
            if (!_cache.TryGetValue($"PassCode_{id}", out string? savedCode) || savedCode != dto.Code)
                return BadRequest("Невірний або прострочений код.");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Користувача не знайдено.");

            // 2. Валідація пароля
            if (dto.NewPassword.Length < 8 || !dto.NewPassword.Any(char.IsUpper) || !dto.NewPassword.Any(char.IsLower) || !dto.NewPassword.Any(char.IsDigit))
            {
                return BadRequest("Пароль не відповідає вимогам безпеки.");
            }

            // 3. Хешуємо і зберігаємо
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            // 4. Видаляємо код з кешу
            _cache.Remove($"PassCode_{id}");

            await _auditLog.LogActionAsync(id, "PASSWORD_CHANGE", "Користувач успішно змінив пароль.");

            return NoContent();
        }

        // Оновлення кольору рамки аватара
        [HttpPut("{id}/avatar-color")]
        public async Task<IActionResult> UpdateAvatarColor(int id, [FromBody] UpdateColorDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.AvatarBorderColorHex = dto.AvatarBorderColorHex;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}