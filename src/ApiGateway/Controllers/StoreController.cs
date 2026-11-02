using System.Security.Claims;
using ApiGateway.DTOs;
using ApiGateway.Exceptions;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class StoreController : ControllerBase
{
    private readonly IStoreAggregationService _aggregationService;
    private readonly ILogger<StoreController> _logger;

    public StoreController(
        IStoreAggregationService aggregationService,
        ILogger<StoreController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    /// <summary>
    /// Складений ендпоінт для екрана деталей гри (Catalog + Ordering)
    /// </summary>
    /// <param name="id">Ідентифікатор гри в каталозі</param>
    /// <param name="ct">Токен скасування операції</param>
    /// <returns>Агрегована інформація про гру та статус покупки користувачем</returns>
    [HttpGet("game-details/{id:int}")]
    [ProducesResponseType(typeof(GameStoreDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> GetGameStoreDetails(int id, CancellationToken ct)
    {
        int? userId = null;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claim, out var parsedId))
        {
            userId = parsedId;
        }

        string? bearerToken = null;
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerStr = authHeader.ToString();
            if (headerStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                bearerToken = headerStr["Bearer ".Length..].Trim();
            }
        }

        try
        {
            var result = await _aggregationService.GetGameStoreDetailsAsync(id, userId, bearerToken, ct);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Гру не знайдено",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (GatewayTimeoutException ex)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "Таймаут шлюзу",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (ServiceUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = ex.Title,
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
