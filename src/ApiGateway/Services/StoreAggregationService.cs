using ApiGateway.Clients;
using ApiGateway.DTOs;
using ApiGateway.Exceptions;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Services;

public class StoreAggregationService : IStoreAggregationService
{
    private readonly ICatalogServiceClient _catalogClient;
    private readonly IOrderingServiceClient _orderingClient;
    private readonly ILogger<StoreAggregationService> _logger;

    public StoreAggregationService(
        ICatalogServiceClient catalogClient,
        IOrderingServiceClient orderingClient,
        ILogger<StoreAggregationService> logger)
    {
        _catalogClient = catalogClient;
        _orderingClient = orderingClient;
        _logger = logger;
    }

    public async Task<GameStoreDetailsDto> GetGameStoreDetailsAsync(
        int gameId,
        int? userId,
        string? bearerToken,
        CancellationToken ct = default)
    {
        // 1. Ініціалізуємо паралельні завдання з безпечною обробкою синхронних та асинхронних викликів
        Task<CatalogGameDto?> catalogTask;
        try
        {
            catalogTask = _catalogClient.GetGameByIdAsync(gameId, ct);
        }
        catch (Exception ex)
        {
            catalogTask = Task.FromException<CatalogGameDto?>(ex);
        }

        Task<UserGameStatusDto?> orderingTask;
        if (userId.HasValue && userId.Value > 0)
        {
            try
            {
                orderingTask = _orderingClient.GetUserGameStatusAsync(userId.Value, gameId, bearerToken, ct);
            }
            catch (Exception ex)
            {
                orderingTask = Task.FromException<UserGameStatusDto?>(ex);
            }
        }
        else
        {
            orderingTask = Task.FromResult<UserGameStatusDto?>(new UserGameStatusDto
            {
                HasPurchased = false,
                IsAvailable = true,
                Message = "Анонімний користувач"
            });
        }

        // 2. Паралельний виклик обох сервісів (Task.WhenAll)
        try
        {
            await Task.WhenAll(catalogTask, orderingTask);
        }
        catch (Exception)
        {
            // Окремі результати будуть перевірені нижче для вибіркової деградації
        }

        // 3. Перевірка результату Каталогу (критична складова екрана)
        CatalogGameDto? game;
        try
        {
            game = await catalogTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критична помилка отримання даних каталогу для гри ID {GameId}", gameId);
            throw new ServiceUnavailableException("Каталог недоступний", "Сервіс каталогу не відповідає або перевищено таймаут (2с).");
        }

        if (game == null)
        {
            throw new NotFoundException($"Гру з ID {gameId} не знайдено в каталозі.");
        }

        // 4. Перевірка результату Замовлень (часткова деградація, якщо сервіс ліг)
        UserGameStatusDto userStatus;
        try
        {
            var orderingResult = await orderingTask;
            userStatus = orderingResult ?? new UserGameStatusDto
            {
                HasPurchased = false,
                IsAvailable = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Сервіс замовлень недоступний для гри ID {GameId}. Застосовано часткову деградацію.", gameId);
            userStatus = new UserGameStatusDto
            {
                HasPurchased = false,
                IsAvailable = false,
                Message = "Сервіс замовлень тимчасово недоступний"
            };
        }

        return new GameStoreDetailsDto
        {
            Game = game,
            UserStatus = userStatus,
            AggregatedAt = DateTime.UtcNow
        };
    }
}
