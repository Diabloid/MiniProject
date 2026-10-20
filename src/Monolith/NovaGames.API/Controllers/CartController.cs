using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaGames.API.Data;
using NovaGames.API.Services;
using NovaGames.Core.Entities;
using Stripe;
using System.Security.Claims;

namespace NovaGames.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLog;
        private readonly IConfiguration _config;

        public CartController(AppDbContext context, IAuditLogService auditLog, IConfiguration config)
        {
            _context = context;
            _auditLog = auditLog;
            _config = config;
        }

        // Отримуємо всі ID ігор у кошику поточного юзера
        [HttpGet("my-cart-ids")]
        public async Task<ActionResult<IEnumerable<int>>> GetMyCartItemIds()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var gameIds = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Select(c => c.ProductId)
                .ToListAsync();

            return Ok(gameIds);
        }

        // Додавання гри в кошик
        [HttpPost("{productId}")]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Перевіряємо, чи немає її вже там
            var existingItem = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (existingItem != null) return BadRequest("Гра вже у кошику.");

            // Якщо немає — додаємо новий запис у CartItems
            var cartItem = new CartItem
            {
                UserId = userId,
                ProductId = productId,
                Quantity = 1
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();
            await _auditLog.LogActionAsync(userId, "CART_ADD", $"Користувач додав товар ID:{productId} у кошик.");

            return Ok();
        }

        // Отримуємо всі ігри, які є в кошику поточного юзера (повні дані, а не тільки ID)
        [HttpGet("my-items")]
        public async Task<ActionResult<IEnumerable<Game>>> GetMyCartItems()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // 1. Спочатку дістаємо всі ID ігор, які є в кошику юзера
            var gameIds = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Select(c => c.ProductId)
                .ToListAsync();

            // 2. Тепер дістаємо самі ігри напряму з таблиці Products
            var games = await _context.Products
                .Where(p => gameIds.Contains(p.Id))
                .ToListAsync();

            return Ok(games);
        }

        // Видалення гри з кошика
        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (item == null) return NotFound();

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            await _auditLog.LogActionAsync(userId, "CART_REMOVE", $"Користувач видалив товар ID:{productId} з кошика.");

            return Ok();
        }

        // Оформлення замовлення
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound("Користувача не знайдено.");

                var cartItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
                if (!cartItems.Any()) return BadRequest("Ваш кошик порожній.");

                var productIds = cartItems.Select(c => c.ProductId).ToList();
                var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
                decimal totalAmount = products.Sum(p => p.FinalPrice);

                // ==========================================
                // СИСТЕМА ОПЛАТИ (STRIPE SANDBOX АБО ГАМАНЕЦЬ)
                // ==========================================
                // ==========================================
                // РЕАЛЬНА ІНТЕГРАЦІЯ ЗІ STRIPE (SANDBOX)
                // ==========================================
                string paymentStatus = "Paid_Wallet";

                if (request.PaymentMethod == "Wallet")
                {
                    if (user.WalletBalance < totalAmount)
                        return BadRequest("Недостатньо коштів на балансі гаманця.");

                    user.WalletBalance -= totalAmount;
                }
                else if (request.PaymentMethod == "Card")
                {
                    try
                    {
                        // 1. Ініціалізуємо клієнт Stripe з конфігу
                        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

                        // 2. ОБХІД БЛОКУВАННЯ STRIPE: 
                        // Використовуємо вбудовані тестові токени Stripe замість сирих номерів карток
                        string stripeTokenId;

                        if (request.CardNumber.StartsWith("4242"))
                            stripeTokenId = "tok_visa"; // Успішна Visa
                        else if (request.CardNumber.StartsWith("5555"))
                            stripeTokenId = "tok_mastercard"; // Успішна MasterCard
                        else if (request.CardNumber.StartsWith("4000000000000002"))
                            stripeTokenId = "tok_chargeDeclined"; // Імітація відмови банку
                        else if (request.CardNumber.StartsWith("4000000000000004"))
                            stripeTokenId = "tok_chargeDeclinedInsufficientFunds"; // Недостатньо коштів
                        else
                            return BadRequest("Транзакцію відхилено. Використовуйте авторизовані тестові картки (наприклад, 4242...).");

                        // 3. Здійснюємо реальний платіж (Charge)
                        var chargeOptions = new ChargeCreateOptions
                        {
                            Amount = (long)(totalAmount * 100), // Stripe приймає суму ТІЛЬКИ в копійках/центах!
                            Currency = "uah",
                            Description = $"NovaGames: Order by {user.Email}",
                            Source = stripeTokenId, // Передаємо готовий безпечний токен!
                            Capture = true
                        };

                        var chargeService = new ChargeService();
                        var charge = await chargeService.CreateAsync(chargeOptions);

                        // Перевіряємо відповідь від серверів Stripe
                        if (charge.Status != "succeeded")
                        {
                            return BadRequest($"Банк відхилив транзакцію. Статус: {charge.Status}");
                        }

                        // Зберігаємо реальний ID транзакції зі Stripe у нашу базу
                        paymentStatus = $"Stripe_Txn_{charge.Id}";
                    }
                    catch (StripeException stripeEx)
                    {
                        return BadRequest($"Помилка платіжного шлюзу: {stripeEx.StripeError.Message}");
                    }
                }
                else
                {
                    return BadRequest("Невідомий метод оплати.");
                }

                // Далі йде твій існуючий код створення замовлення...
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = totalAmount,
                    Status = paymentStatus, // Зберігаємо статус, як саме оплачено
                    OrderDate = DateTime.UtcNow
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                var addedToLibraryIds = new List<int>();
                foreach (var item in cartItems)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        PriceAtPurchase = product.FinalPrice,
                        Quantity = 1
                    });

                    if (product is DigitalGame)
                    {
                        _context.UserLibraries.Add(new UserLibrary
                        {
                            UserId = userId,
                            ProductId = product.Id,
                            AcquiredAt = DateTime.UtcNow,
                            Category = "Без категорії"
                        });
                        addedToLibraryIds.Add(product.Id);
                    }
                }

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditLog.LogActionAsync(userId, "CHECKOUT_SUCCESS",
                    $"Замовлення ID:{order.Id} сплачено через {request.PaymentMethod}. Додано ігор: {addedToLibraryIds.Count}");

                return Ok(new
                {
                    NewBalance = user.WalletBalance, // Повернеться без змін, якщо платили карткою
                    AddedToLibraryIds = addedToLibraryIds
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Помилка при оформленні замовлення: {ex.Message}");
            }
        }
    }
}