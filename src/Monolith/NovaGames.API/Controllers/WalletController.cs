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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLog;

        public WalletController(AppDbContext context, IAuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        [HttpPost("top-up")]
        public async Task<IActionResult> TopUpWallet([FromBody] TopUpRequest request)
        {
            try
            {
                // 1. Отримуємо ID юзера з його JWT токена
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
                int userId = int.Parse(userIdStr);

                // 2. Шукаємо юзера в базі
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound("Користувача не знайдено.");

                // Ми імітуємо, що банк завжди схвалює транзакцію:

                // 3. Зараховуємо гроші на гаманець
                user.WalletBalance += request.Amount;

                // 4. Якщо стоїть галочка "Запам'ятати картку"
                if (request.SaveCard)
                {
                    // Генеруємо безпечну маску (беремо тільки останні 4 цифри)
                    string last4 = request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : "0000";
                    string mask = $"**** **** **** {last4}";

                    // Перевіряємо, чи немає вже такої картки у цього юзера (щоб не плодити дублікати)
                    bool cardExists = await _context.SavedCards
                        .AnyAsync(c => c.UserId == userId && c.MaskedCardNumber == mask);

                    if (!cardExists)
                    {
                        var savedCard = new SavedCard
                        {
                            UserId = userId,
                            MaskedCardNumber = mask,
                            ExpiryDate = request.ExpiryDate,
                            // Генеруємо "фейковий" токен банку, за яким ми нібито зможемо списувати гроші далі
                            PaymentToken = Guid.NewGuid().ToString()
                        };
                        _context.SavedCards.Add(savedCard);
                    }
                }

                // 5. Зберігаємо всі зміни в БД за одну транзакцію
                await _context.SaveChangesAsync();

                await _auditLog.LogActionAsync(userId, "WALLET_TOPUP",
                    $"Користувач поповнив гаманець на {request.Amount:N2} ₴. Новий баланс: {user.WalletBalance:N2} ₴.");

                return Ok(new { NewBalance = user.WalletBalance });
            }
            catch (Exception ex)
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _auditLog.LogActionAsync(userId, "WALLET_TOPUP_FAILED", $"Помилка поповнення на {request.Amount}: {ex.Message}");
                return StatusCode(500, "Технічна помилка при обробці платежу.");
            }
        }

        // Отримуємо всі збережені картки поточного юзера
        [HttpGet("saved-cards")]
        public async Task<ActionResult<IEnumerable<SavedCard>>> GetSavedCards()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            var cards = await _context.SavedCards
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return Ok(cards);
        }
    }
}