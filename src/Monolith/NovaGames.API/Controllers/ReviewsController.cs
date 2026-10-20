using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaGames.API.Data;
using NovaGames.API.DTOs;
using NovaGames.Core.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NovaGames.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. ОТРИМАТИ ВСІ ВІДГУКИ ДЛЯ КОНКРЕТНОЇ ГРИ
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetReviews(int productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    Username = r.User.Username,
                    AvatarUrl = r.User.AvatarUrl,
                    AvatarBorderColorHex = r.User.AvatarBorderColorHex,
                    Rating = r.Rating,
                    Text = r.Text,
                    CreatedAt = r.CreatedAt,
                    IsEdited = r.IsEdited,

                })
                .ToListAsync();

            return Ok(reviews);
        }

        // 2. ДОДАТИ ВІДГУК
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized(new { Message = "Помилка авторизації." });

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { Message = "Оцінка має бути від 1 до 5." });

            // Чи володіє юзер цією грою?
            var ownsGame = await _context.UserLibraries
                .AnyAsync(ul => ul.UserId == userId && ul.ProductId == dto.ProductId);

            if (!ownsGame)
                return BadRequest(new { Message = "Ви повинні придбати цю гру, щоб залишити відгук." });

            // Чи не залишав він відгук раніше?
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == dto.ProductId);

            if (existingReview != null)
                return BadRequest(new { Message = "Ви вже залишили відгук на цю гру." });

            var review = new Review
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Rating = dto.Rating,
                Text = dto.Text,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            await _context.SaveChangesAsync();

            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

            if (product != null && product.Reviews.Any())
            {
                product.TotalReviews = product.Reviews.Count;
                product.AverageRating = Math.Round(product.Reviews.Average(r => (double)r.Rating), 1);

                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Відгук успішно додано!" });
        }

        // 3. РЕДАГУВАТИ ВІДГУК
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized(new { Message = "Помилка авторизації." });

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { Message = "Оцінка має бути від 1 до 5." });

            // Шукаємо відгук, який належить саме цьому юзеру
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (review == null)
                return NotFound(new { Message = "Відгук не знайдено або у вас немає прав на його редагування." });

            review.Rating = dto.Rating;
            review.Text = dto.Text;
            review.IsEdited = true;

            await _context.SaveChangesAsync();

            // ОНОВЛЮЄМО СЕРЕДНЮ ОЦІНКУ ГРИ
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == review.ProductId);

            if (product != null && product.Reviews.Any())
            {
                product.AverageRating = Math.Round(product.Reviews.Average(r => (double)r.Rating), 1);
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Відгук успішно оновлено!" });
        }

        // 4. ВИДАЛИТИ ВІДГУК
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized(new { Message = "Помилка авторизації." });

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (review == null)
                return NotFound(new { Message = "Відгук не знайдено або у вас немає прав." });

            int productId = review.ProductId;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            var product = await _context.Products.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.TotalReviews = product.Reviews.Count;
                product.AverageRating = product.Reviews.Any() ? Math.Round(product.Reviews.Average(r => (double)r.Rating), 1) : 0.0;
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Відгук успішно видалено!" });
        }
    }
}