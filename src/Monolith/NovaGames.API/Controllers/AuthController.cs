using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NovaGames.API.Data;
using NovaGames.API.DTOs;
using NovaGames.API.Services;
using NovaGames.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NovaGames.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IAuditLogService _auditLog;

        public AuthController(AppDbContext context, IConfiguration config, IAuditLogService auditLog)
        {
            _context = context;
            _config = config;
            _auditLog = auditLog;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!Regex.IsMatch(request.Username, @"^[a-zA-Z][a-zA-Z0-9_]{3,19}$"))
            {
                return BadRequest("Невалідний формат логіну. Логін має містити 4-20 символів, починатися з літери і містити лише латину, цифри та підкреслення.");
            }

            // 1. Перевіряємо, чи немає вже такого юзера
            var userExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
            if (userExists)
            {
                return BadRequest("Користувач з таким ім'ям вже існує.");
            }

            if (request.Password.Length < 8 ||
                !request.Password.Any(char.IsUpper) ||
                !request.Password.Any(char.IsLower) ||
                !request.Password.Any(char.IsDigit))
            {
                return BadRequest("Пароль: мін. 8 символів, велика і мала літери, та цифра.");
            }

            // 2. Хешуємо пароль за допомогою BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Створюємо нового користувача
            var newUser = new User
            {
                Username = request.Username,
                PasswordHash = passwordHash,
                Role = UserRole.Client,
                WalletBalance = 0m,
                LastLoginDate = DateTime.Now
            };

            // 4. Зберігаємо в базу
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            await _auditLog.LogActionAsync(newUser.Id, "REGISTER", $"Користувач {newUser.Username} успішно зареєструвався.");

            return Ok(new { Message = "Реєстрація успішна!" });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] LoginRequest request)
        {
            // 1. Шукаємо користувача за логіном
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            // 2. Якщо юзера немає АБО пароль не збігається з хешем у базі
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                await _auditLog.LogActionAsync(user?.Id, "LOGIN_FAILED", $"Невдала спроба входу для користувача: {request.Username}");

                // Повертаємо 401 Unauthorized
                return Unauthorized("Невірний логін або пароль.");
            }

            // 3. Оновлюємо дату останнього входу
            user.LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // 4. Генеруємо JWT-токен та Refresh-токен
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Зберігаємо Refresh-токен у базу
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();
            await _auditLog.LogActionAsync(user.Id, "LOGIN_SUCCESS", $"Користувач {user.Username} увійшов у систему.");

            // 5. Повертаємо юзера та токени
            return Ok(new { User = user, Token = token, RefreshToken = refreshToken });
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request) // ЗРОБИТИ В КУКАХ
        {
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest("Невалідний запит");

            // Знаходимо юзера за Refresh-токеном
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                await _auditLog.LogActionAsync(user?.Id, "REFRESH_TOKEN_FAILED", "Спроба оновлення токена з простроченим або недійсним RefreshToken.");
                return Unauthorized("Refresh token недійсний або прострочений. Потрібна повторна авторизація.");
            }

            var newJwtToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();
            await _auditLog.LogActionAsync(user.Id, "REFRESH_TOKEN_SUCCESS", "Токен успішно оновлено.");

            return Ok(new { Token = newJwtToken, RefreshToken = newRefreshToken, User = user });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Інформація, яку ми "зашиваємо" всередину токена
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
               new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Генерує безпечний рандомний рядок довжиною 64 байти
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}